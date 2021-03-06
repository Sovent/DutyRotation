module DutyRotation.GroupMemberTests

open DutyRotation
open Hedgehog
open Xunit
open DutyRotation.Common

[<Fact>]
let ``Sort any shuffled group returns ordered group`` () =
  let rec isSorted (groupMembers: GroupMember list) =
      match groupMembers with
      | [] -> true
      | [single] -> true
      | head :: ({QueuePosition = Following id} as next) :: rest when id = head.Id ->
        next :: rest |> isSorted
      | _ -> false
      
  property {
    let! groupMembers = Generators.shuffledGroupMembers 0 20
    
    let sortedGroupMembers = GroupMember.sortInQueue groupMembers
    
    return isSorted sortedGroupMembers
  } |> Property.check