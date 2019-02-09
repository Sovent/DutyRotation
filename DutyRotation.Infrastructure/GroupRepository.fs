module DutyRotation.Infrastructure.GroupRepository

open System
open FSharp.Data.Sql
open DutyRotation.Common
open DutyRotation.CreateGroup.Types
open DutyRotation.AddGroupMember.Types
open DutyRotation.RotateDuties.Types
open DutyRotation.Common

[<Literal>]
let connectionString = @"Data Source=den1.mssql8.gear.host;Initial Catalog=dutyrotation;Persist Security Info=True;User ID=dutyrotation;Password=Yo845n_L?4DA"

type sql = SqlDataProvider<
            ConnectionString = connectionString,
            DatabaseVendor = Common.DatabaseProviderTypes.MSSQLSERVER,
            UseOptionTypes = true>

FSharp.Data.Sql.Common.QueryEvents.SqlQueryEvent |> Event.add (printfn "Executing SQL: %O")  

let tryGetGroup (ctx: sql.dataContext) (groupId:GroupId) =
  async {
    let! groupEntity = Seq.tryHeadAsync ( query {
        for group in ctx.Dbo.Groups do
        where (group.Id = groupId.Value)
        select (group)
      })
    return groupEntity |> Option.map (fun entity ->
      {
        Group.Id = GroupId.TryParse entity.Id |> Result.value
        Settings = {
          DutiesCount = DutiesCount.TryGet entity.DutiesCount |> Result.value
          RotationCronRule = RotationCronRule.TryParse entity.RotationCronRule |> Result.value
          Name = GroupName.TryParse entity.Name |> Result.value
          RotationStartDate = entity.RotationStartDate
                              |> Option.map (fun date -> date |> DateTimeOffset |> RotationStartDate)
        }
      })
  }
  
let getGroup (ctx: sql.dataContext) (groupId: GroupId) =
  asyncResult {
    let! groupOption = groupId |> tryGetGroup ctx |> AsyncResult.ofAsync
    return! match groupOption with
            | Some group -> AsyncResult.retn group
            | None -> { GroupId = groupId } |> AsyncResult.ofError
  }
  
let getGroupMembers (ctx: sql.dataContext) (groupId:GroupId) =
  let rows = Seq.executeQueryAsync (query {
        for groupMember in ctx.Dbo.GroupMembers do
        where (groupMember.GroupId = groupId.Value)
        select (groupMember)
      })
  rows |> Async.map (Seq.map (fun row -> {
        Id = GroupMemberId.TryGet row.Id |> Result.value
        Name = GroupMemberName.TryParse row.Name |> Result.value
        Position = row.QueuePosition |> GroupMemberQueuePosition.tryGet |> Result.value
      }))
  
let saveGroup : SaveGroup =
  fun group ->
    let ctx = sql.GetDataContext()
    let row = ctx.Dbo.Groups.Create()
    row.Id <- group.Id.Value
    row.Name <- group.Settings.Name.Value
    row.DutiesCount <- group.Settings.DutiesCount.Value
    row.RotationCronRule <- group.Settings.RotationCronRule.Value
    row.RotationStartDate <- group.Settings.RotationStartDate |> Option.map (fun date -> date.Value.UtcDateTime)
    row.CreatedDate <- DateTime.UtcNow   
    ctx.SubmitUpdatesAsync ()

let getGroupMember: DutyRotation.AddGroupMember.Types.GetGroupMembers =
  fun groupId ->
    let ctx = sql.GetDataContext()
    let groupMembers = asyncResult {
      let! group = getGroup ctx groupId
      return! getGroupMembers ctx groupId |> AsyncResult.ofAsync
    }
    groupMembers |> AsyncResult.map Seq.toList
    
  
let saveMember: SaveMember =
  fun groupId groupMember ->
    let ctx = sql.GetDataContext()
    let row = ctx.Dbo.GroupMembers.Create()
    row.Id <- groupMember.Id.Value
    row.Name <- groupMember.Name.Value
    row.GroupId <- groupId.Value
    row.QueuePosition <- groupMember.Position.Value
    ctx.SubmitUpdatesAsync ()
    
let getGroupDutiesCount: GetGroupDutiesCount =
  fun groupId ->
    let ctx = sql.GetDataContext()
    asyncResult {
      let! group = groupId |> getGroup ctx
      return group.Settings.DutiesCount
    }
    
let getGroupMembersForRotation : DutyRotation.RotateDuties.Types.GetGroupMembers =
  fun groupId -> let ctx = sql.GetDataContext() in getGroupMembers ctx groupId

let saveMembers : SaveMembers =
  fun groupId groupMembers ->
    let ctx = sql.GetDataContext()
    async {
      for groupMember in groupMembers do
          let! row = Seq.headAsync ( query {
            for entity in ctx.Dbo.GroupMembers do
            where (entity.Id = groupMember.Id.Value)
            select (entity)
          })
          row.QueuePosition <- groupMember.Position.Value
      do! ctx.SubmitUpdatesAsync ()
    }
    
    