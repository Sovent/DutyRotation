namespace DutyRotation

open Hedgehog
open DutyRotation.Common

module Gen =
  let single (gen:Gen<'a>) : 'a =
    Gen.sample (Size.MaxValue) 1 gen |> List.head

module Generators =
  open System

  let groupMemberId : Gen<GroupMemberId> =
    gen {
      return GroupMemberId.New
    }
  
  let groupMemberName : Gen<GroupMemberName> =
    gen {
      let range = Range.constant 3 10
      let allowedChar = Gen.char 'A' 'z'
      let! groupMemberName = Gen.string (Range.constant 3 10) allowedChar
      return GroupMemberName.TryParse groupMemberName |> Result.value
    }
    
  let groupMember : Gen<GroupMember> =
    gen {
      let! id = groupMemberId
      let! name = groupMemberName
      return { GroupMember.Id = id; Name = name; QueuePosition = First }
    }
  
  let orderedGroupMembers : Gen<GroupMember list> =
    gen {
      let! first::rest = Gen.list (Range.constant 2 10) groupMember
      let queuedRest,_ = rest
                      |> List.mapFold (fun lastMemberId currentMember ->
                        {currentMember with QueuePosition = Following lastMemberId},currentMember.Id) first.Id
      return first :: queuedRest
    }
    
  let shuffledGroupMembers : Gen<GroupMember list> =
    gen {
      let! orderedGroupMembers = orderedGroupMembers
      return orderedGroupMembers |> List.sortBy (fun _ -> Guid.NewGuid())
    }