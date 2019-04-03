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
    let rec findLastPosition membersTail (currentLastMember:GroupMemberId)  =
      match membersTail with
      | [] -> currentLastMember
      | head :: tail ->
        match head.QueuePosition with
        | First -> findLastPosition tail currentLastMember
        | Following memberId -> if memberId = currentLastMember
                                  then head.Id
                                  else currentLastMember |> findLastPosition tail 
    match groupMembers with
    | [] -> First
    | head :: tail -> findLastPosition tail head.Id |> Following
