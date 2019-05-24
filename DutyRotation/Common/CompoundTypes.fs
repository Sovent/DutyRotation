namespace DutyRotation.Common

type GroupSettings = {
  Name: GroupName
  RotationCronRule: RotationCronRule
  RotationStartDate: RotationStartDate option
  DutiesCount: DutiesCount
}

type Group = {
  Id: GroupId
  Settings: GroupSettings
}

type QueuePosition =
  | First
  | Following of GroupMemberId
  
type GroupMember = {
  Id: GroupMemberId
  Name : GroupMemberName
  QueuePosition: QueuePosition
}

module GroupMember =
  let sortInQueue (members:GroupMember list) =
    let membersMap = members |> List.fold (fun map membr -> Map.add membr.QueuePosition membr map) Map.empty
    let rec collectMembers accum currentQueuePosition =
      match Map.tryFind currentQueuePosition membersMap with
      | None -> accum
      | Some membr -> Following membr.Id |> collectMembers (membr :: accum)
    collectMembers List.empty First |> List.rev


module QueuePosition =
  let tail groupMembers =
    match groupMembers |> GroupMember.sortInQueue |> List.tryLast with
    | Some item -> Following item.Id
    | None -> First

type SendMembersToSlack = {
  Description: string
  Channel: string
}

type TriggerAction =
  | SendMembersToSlack of SendMembersToSlack
  
type TriggerTarget =
  | AddMembers
  | RotateDuties