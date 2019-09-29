module DutyRotation.Infrastructure.GroupRepository

open System
open System.Data
open DutyRotation.Common
open DutyRotation.CreateGroup.Types
open DutyRotation.AddGroupMember.Types
open DutyRotation.RotateDuties.Types
open Daffer

[<CLIMutable>]
type private GroupRow = {
  Id: Guid
  Name: string
  DutiesCount: int
  RotationCronRule: string
  RotationStartDate: DateTime option
  CreatedDate: DateTime
}

[<CLIMutable>]
type private GroupMemberRow = {
  Id: Guid
  Name: string
  GroupId: Guid
  Follows: Guid option
}

let private rowToGroup (row: GroupRow) = {
  Id = GroupId.TryParse row.Id |> Result.value
  Settings = {
    Name = GroupName.TryParse row.Name |> Result.value
    RotationCronRule = RotationCronRule.TryParse row.RotationCronRule |> Result.value
    RotationStartDate = row.RotationStartDate
                        |> Option.map (fun date -> new DateTimeOffset(date))
                        |> Option.map RotationStartDate
    DutiesCount = DutiesCount.TryGet row.DutiesCount |> Result.value
  }
}

let private rowToGroupMember row = {
  Id = GroupMemberId.TryGet row.Id |> Result.value
  Name = GroupMemberName.TryParse row.Name |> Result.value
  QueuePosition = match row.Follows with
                    | None -> First
                    | Some id -> GroupMemberId.TryGet id |> Result.value |> Following
}

let private tryGetGroup (connection:IDbConnection) (groupId:GroupId) =
  async {
    let! groupRow = 
      querySingleMaybeAsync<GroupRow>
        connection
        "select top (1) * from Groups where Id=@GroupId"
        ["GroupId" => groupId.Value]
      
    return groupRow |> Option.map rowToGroup
  }

let private mapGroupOrError<'a>
  (mapAsync: Group -> Async<'a>)
  (connection: IDbConnection)
  (groupId:GroupId)
  : AsyncResult<'a, GroupNotFoundError> =
  asyncResult {
    let! group = tryGetGroup connection groupId |> AsyncResult.ofAsync
    return! match group with
                | None -> { GroupNotFoundError.GroupId = groupId } |> AsyncResult.ofError
                | Some g -> g |> mapAsync |> AsyncResult.ofAsync
  }
  
let private asyncGetGroupMembers (connection:IDbConnection) (groupId:GroupId) =
  async {
    let! groupRows =
      queryAsync<GroupMemberRow>
        connection
        "select * from GroupMembers where GroupId=@GroupId"
        ["GroupId" => groupId.Value]
    return groupRows |> List.map rowToGroupMember
  }
  
let saveGroup (connection:IDbConnection) : SaveGroup =
  fun group ->
    let rotationStartDate = group.Settings.RotationStartDate
                            |> Option.map (fun date -> date.Value.DateTime)
                            |> Option.toNullable
    executeAsync
      connection
      "INSERT INTO Groups (Id, Name, DutiesCount, RotationCronRule, RotationStartDate, CreatedDate)
      VALUES (@GroupId, @GroupName, @DutiesCount, @RotationCronRule, @RotationStartDate, @CreatedDate)"
      ["GroupId" => group.Id.Value;
        "GroupName" => group.Settings.Name.Value;
        "DutiesCount" => group.Settings.DutiesCount.Value;
        "RotationCronRule" => group.Settings.RotationCronRule.Value;
        "RotationStartDate" => rotationStartDate;
        "CreatedDate" => DateTime.UtcNow] |> Async.map (fun _ -> ())

let getGroupMembers (connection:IDbConnection): DutyRotation.AddGroupMember.Types.GetGroupMembers =
  fun groupId ->
    mapGroupOrError (fun _ -> asyncGetGroupMembers connection groupId |> Async.map Seq.toList) connection groupId
  
let saveMember (connection:IDbConnection) : SaveMember =
  fun groupId groupMember ->
    executeAsync
      connection
      "INSERT INTO GroupMembers
      VALUES (@MemberId, @Name, @GroupId, @Follows)"
      ["MemberId" => groupMember.Id.Value;
        "Name" => groupMember.Name.Value;
        "GroupId" => groupId.Value;
        "Follows" => match groupMember.QueuePosition with
                      | First -> Nullable()
                      | Following memberId -> Nullable memberId.Value]
    |> Async.map (fun _ -> ())
    
let getGroupDutiesCount (connection:IDbConnection) : GetGroupDutiesCount =
  fun groupId ->
    mapGroupOrError (fun group -> group.Settings.DutiesCount |> Async.retn) connection groupId
   
let getGroupMembersForRotation (connection:IDbConnection) : DutyRotation.RotateDuties.Types.GetGroupMembers =
  asyncGetGroupMembers connection

let saveMembers (connection:IDbConnection): SaveMembers =
  fun groupId (newFirst, newTail) ->
    let executeAsync sql parameters = executeAsync connection sql parameters |> Async.map (fun _ -> ())
    async {
      do! executeAsync
            "UPDATE GroupMembers SET Follows=null WHERE Id = @Id"
            ["Id" => newFirst.Id.Value] 
      do! executeAsync
            "UPDATE GroupMembers SET Follows=@FollowedId WHERE Id = @Id"
            ["Id" => newTail.Id.Value;
             "FollowedId" => let (Following followed) = newTail.QueuePosition in followed.Value]
    }
    
let retrieveGroupInfo (connection:IDbConnection) (asyncGetTriggers:GroupId -> Async<Trigger list>)
  : DutyRotation.GetGroupInfo.Types.RetrieveGroupInfo =
  fun groupId ->
    mapGroupOrError
      (fun group ->
        async {
          let! groupMembers = asyncGetGroupMembers connection groupId
          let! triggers = asyncGetTriggers groupId
          return group, groupMembers, triggers
        })
      connection
      groupId
    
let checkIfGroupExists (connection:IDbConnection) : DutyRotation.AddTriggerAction.Types.CheckIfGroupExists =
  fun groupId ->
    mapGroupOrError (fun _ -> Async.retn ()) connection groupId