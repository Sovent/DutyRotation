namespace DutyRotation.GetGroupInfo

open System
open DutyRotation.Common

module Contract =
  type GetGroupInfoQuery = {
    GroupId: Guid
  }
  
  type GroupMemberInfo = {
    Id: GroupMemberId
    Name: GroupMemberName
    Position: int
  }
  
  type GroupInfo = {
    Group: Group
    Members: GroupMemberInfo list
    Triggers: Trigger list
  }
  
  type GetGroupInfoError =
    | GroupNotFound of GroupNotFoundError
    | Validation of ValidationError list
    
  type GetGroupInfo = GetGroupInfoQuery -> AsyncResult<GroupInfo, GetGroupInfoError>

module Types =
  open Contract
  
  type GetGroupId = GetGroupInfoQuery -> Result<GroupId, ValidationError>
  
  type RetrieveGroupInfo = GroupId -> AsyncResult<Group * GroupMember list * Trigger list, GroupNotFoundError>
  
  type BuildGroupInfoView = Group * GroupMember list * Trigger list -> GroupInfo
  
module Implementation =
  open Contract
  open Types
  
  let buildView : BuildGroupInfoView =
    fun (group, members, triggers) ->
      let membersInfo =
        members
        |> GroupMember.sortInQueue
        |> List.mapi (fun index membr -> {GroupMemberInfo.Id = membr.Id; Name = membr.Name; Position = index + 1 })
        
      { Group = group; Members = membersInfo; Triggers = triggers }
      
  let getGroupInfo (retrieveGroupInfo: RetrieveGroupInfo) : GetGroupInfo =
    fun query ->
      asyncResult {
        let! groupId = query.GroupId
                       |> GroupId.TryParse
                       |> AsyncResult.ofResult
                       |> AsyncResult.mapError Validation
        let! (group, members, triggers) = groupId |> retrieveGroupInfo |> AsyncResult.mapError GroupNotFound
        return buildView (group, members, triggers)
      }
  

