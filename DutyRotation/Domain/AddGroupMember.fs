namespace DutyRotation.AddGroupMember

open System
open DutyRotation.Common

module Contract =  
  [<CLIMutable>]
  type AddGroupMemberCommand = {
    GroupId: Guid
    MemberName: string
  }
  
  type MemberWithSameNameExists = {
    GroupId : GroupId
    MemberName : GroupMemberName
  }
  
  type AddGroupMemberError =
    | Validation of ValidationError list
    | GroupNotFound of GroupNotFoundError
    | MemberNameConflict of MemberWithSameNameExists
  
  type AddGroupMember = AddGroupMemberCommand -> AsyncResult<GroupMemberId, AddGroupMemberError>

module Types =
  open Contract

  type GetGroupId = AddGroupMemberCommand -> Result<GroupId, ValidationError list>

  type GetMemberName = AddGroupMemberCommand -> Result<GroupMemberName, ValidationError list>
  
  type GetGroupMembers = GroupId -> AsyncResult<GroupMember list, GroupNotFoundError>
  
  type GetNewMemberId = unit -> GroupMemberId
  
  type GetQueuePosition = GroupMember list -> QueuePosition
  
  type CreateNewMember =
    GroupMember list -> GroupId -> GroupMemberName -> QueuePosition -> Result<GroupMember, MemberWithSameNameExists>
  
  type SaveMember = GroupId -> GroupMember -> Async<unit>

module Implementation =
  open Contract
  open Types
  
  let getGroupId : GetGroupId = fun command -> GroupId.TryParse command.GroupId

  let getMemberName : GetMemberName = fun command -> GroupMemberName.TryParse command.MemberName
  
  let getNewMemberId : GetNewMemberId = fun _ -> GroupMemberId.New
  
  let getQueuePosition : GetQueuePosition = QueuePosition.tail
    
  let createNewMember (getNewMemberId:GetNewMemberId) : CreateNewMember =
    fun members groupId newMemberName queuePosition ->
      if Seq.exists (fun groupMember -> groupMember.Name = newMemberName) members then
        {
          GroupId = groupId
          MemberName = newMemberName
        } |> Error
      else
        let newMemberId = getNewMemberId ()
        {
          Id = newMemberId
          Name = newMemberName
          QueuePosition = queuePosition
        } |> Ok
    
  let addGroupMember (getGroupMembers: GetGroupMembers) (saveMember: SaveMember) : AddGroupMember =
    fun command ->
      let groupId = getGroupId command
      let memberName = getMemberName command
      let (<!>),(<*>) = Result.map, Result.apply
      asyncResult {
        let! groupId, memberName = (fun id name -> (id, name)) <!> groupId <*> memberName
                                   |> AsyncResult.ofResult
                                   |> AsyncResult.mapError Validation
        let! groupMembers = getGroupMembers groupId
                            |> AsyncResult.mapError GroupNotFound
        let groupPosition = getQueuePosition groupMembers
        let! newMember = createNewMember getNewMemberId groupMembers groupId memberName groupPosition
                         |> AsyncResult.ofResult
                         |> AsyncResult.mapError MemberNameConflict
        let! saveMember = saveMember groupId newMember |> AsyncResult.ofAsync
        return newMember.Id
      }