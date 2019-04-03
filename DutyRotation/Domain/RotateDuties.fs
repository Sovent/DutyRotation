namespace DutyRotation.RotateDuties

open DutyRotation.Common
open System

module Contract =
  type RotateDutiesCommand = {
    GroupId: Guid
  }
  
  type CurrentDuties = GroupMember list
  
  type InvalidQueueError = {
    GroupId: GroupId
  }
  
  type RotateDutiesError =
    | Validation of ValidationError list
    | GroupNotFound of GroupNotFoundError
    | InvalidQueue of InvalidQueueError
    
  type RotateDuties = RotateDutiesCommand -> AsyncResult<CurrentDuties, RotateDutiesError>
  
module Types =
  open Contract

  type GetGroupId = RotateDutiesCommand -> Result<GroupId, ValidationError list>
  
  type GetGroupDutiesCount = GroupId -> AsyncResult<DutiesCount, GroupNotFoundError>
  
  type GetGroupMembers = GroupId -> Async<GroupMember list>
  
  type SortGroupMembersByQueuePosition = GroupMember list -> GroupMember list
  
  type CheckIfRotationNeeded = GroupMember list -> DutiesCount -> bool
  
  type GetCurrentDuties = DutiesCount -> GroupMember list -> CurrentDuties
  
  type NewFirst = GroupMember
  type NewTail = GroupMember
  type SetFirstToTail = DutiesCount -> GroupMember list -> NewFirst * NewTail * GroupMember list
  
  type SaveMembers = GroupId -> NewFirst * NewTail -> Async<unit>
  
module Implementation =
  open Contract
  open Types
  
  let getGroupId : GetGroupId = fun command -> GroupId.TryParse command.GroupId
  
  let sortGroupMembers : SortGroupMembersByQueuePosition = GroupMember.sortInQueue
  
  let checkIfRotationNeeded : CheckIfRotationNeeded =
    fun members dutiesCount -> List.length members > dutiesCount.Value
  
  let getCurrentDuties : GetCurrentDuties = fun dutiesCount  -> List.truncate dutiesCount.Value
  
  let setFirstToTail : SetFirstToTail = fun dutiesCount members ->
     let (newTail :: othersFinishedDuty), (newFirst :: othersWaitingForDuty) = List.splitAt dutiesCount.Value members
     let exTail = match othersWaitingForDuty with
                    | [] -> newFirst
                    | someone -> List.last someone
     let updatedNewTail = {newTail with QueuePosition = Following exTail.Id}
     let updatedNewFirst = {newFirst with QueuePosition = First}
     let updatedQueue = (updatedNewFirst :: othersWaitingForDuty) @ (updatedNewTail :: othersFinishedDuty)
     updatedNewFirst, updatedNewTail, updatedQueue     

  let rotateDuties
    (getGroupDutiesCount:GetGroupDutiesCount)
    (getGroupMembers: GetGroupMembers)
    (saveMembers: SaveMembers) : RotateDuties =
    fun command ->
      asyncResult {
        let! groupId = command |> getGroupId |> Result.mapError Validation |> AsyncResult.ofResult
        let! dutiesCount = getGroupDutiesCount groupId |> AsyncResult.mapError GroupNotFound
        let! groupMembers = getGroupMembers groupId |> AsyncResult.ofAsync
        if checkIfRotationNeeded groupMembers dutiesCount then          
          let membersQueue = sortGroupMembers groupMembers
          let newFirst, newTail, updatedQueue = setFirstToTail dutiesCount membersQueue
          do! saveMembers groupId (newFirst, newTail) |> AsyncResult.ofAsync
          return getCurrentDuties dutiesCount updatedQueue
        else
          return getCurrentDuties dutiesCount groupMembers
      }
      