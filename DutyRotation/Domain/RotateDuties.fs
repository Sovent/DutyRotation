namespace DutyRotation.RotateDuties

open DutyRotation.Common
open System

module Contract =
  type RotateDutiesCommand = {
    GroupId: Guid
  }
  
  type CurrentDuties = GroupMember seq
  
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
  
  type GetGroupMembers = GroupId -> Async<GroupMember seq>
  
  type GroupMembersQueue = GroupMember seq
  
  type SortGroupMembers = GroupMember seq -> GroupMembersQueue
  
  type GetCurrentDuties = DutiesCount -> GroupMembersQueue -> CurrentDuties
  
  type GetMembersNotOnDuty = DutiesCount -> GroupMembersQueue -> GroupMembersQueue
  
  type SendCurrentDutiesToQueueEnd = CurrentDuties -> GroupMembersQueue -> GroupMembersQueue
  
  type MoveForwardMembers = GroupId -> DutiesCount -> GroupMembersQueue -> Result<GroupMember list, InvalidQueueError>
    
  type SaveMembers = GroupId -> GroupMember list -> Async<unit>
  
module Implementation =
  open Contract
  open Types
  
  let getGroupId : GetGroupId = fun command -> GroupId.TryParse command.GroupId
  
  let sortGroupMembers: SortGroupMembers = fun members -> members |> Seq.sortBy (fun membr -> membr.Position)
  
  let getCurrentDuties: GetCurrentDuties = fun dutiesCount -> Seq.truncate dutiesCount.Value
  
  let getMembersNotOnDuty : GetMembersNotOnDuty = fun dutiesCount -> Seq.trySkip dutiesCount.Value
  
  let sendCurrentDutiesToQueueEnd: SendCurrentDutiesToQueueEnd =
    fun currentDuties notDuties ->
      let lastQueuePosition = notDuties
                              |> Seq.tryLast
                              |> Option.map (fun membr -> membr.Position.Value + 1)
                              |> Option.defaultValue 0
      let newTail = currentDuties |> Seq.map (fun membr ->
          {membr with Position = membr.Position |> GroupMemberQueuePosition.shiftOn lastQueuePosition })
      Seq.append notDuties newTail     
      
  let moveForwardMembers : MoveForwardMembers =
    fun groupId dutiesCount members ->
      let tryPushInQueue groupMember =
        let newPosition = groupMember.Position.Value - dutiesCount.Value |> GroupMemberQueuePosition.tryGet
        match newPosition with
          | Ok position -> Some {groupMember with Position = position }
          | _ -> None
      
      let rec tryMoveForward notUpdatedMembers updatedMembers =
        match notUpdatedMembers with
          | [] -> Ok updatedMembers
          | head :: next :: tail when next.Position <> GroupMemberQueuePosition.nextAfter head.Position ->
            Error { GroupId = groupId }
          | head :: tail ->
              match tryPushInQueue head with
              | Some updatedMember -> updatedMember :: updatedMembers |> tryMoveForward tail
              | None -> Error { GroupId = groupId }
      
      tryMoveForward (Seq.toList members) List.empty 

  let rotateDuties
    (getGroupDutiesCount:GetGroupDutiesCount)
    (getGroupMembers: GetGroupMembers)
    (saveMembers: SaveMembers) : RotateDuties =
    fun command ->
      asyncResult {
        let! groupId = command |> getGroupId |> Result.mapError Validation |> AsyncResult.ofResult
        let! dutiesCount = getGroupDutiesCount groupId |> AsyncResult.mapError GroupNotFound
        let! groupMembers = getGroupMembers groupId |> AsyncResult.ofAsync
        let membersQueue = sortGroupMembers groupMembers
        let currentDuties = getCurrentDuties dutiesCount membersQueue
        let notOnDutyMembers = getMembersNotOnDuty dutiesCount membersQueue
        let! pushedQueue = sendCurrentDutiesToQueueEnd currentDuties notOnDutyMembers
                          |> moveForwardMembers groupId dutiesCount
                          |> Result.mapError InvalidQueue
                          |> AsyncResult.ofResult
        let! smth = saveMembers groupId pushedQueue |> AsyncResult.ofAsync
        return getCurrentDuties dutiesCount pushedQueue
      }
      