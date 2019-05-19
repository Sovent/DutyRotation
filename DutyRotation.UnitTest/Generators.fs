namespace DutyRotation

open System
open Hedgehog
open DutyRotation.Common

module Gen =
  let single (gen:Gen<'a>) : 'a =
    Gen.sample (Size.MaxValue) 1 gen |> List.head
  let randomElement (list:'a list) : 'a =
    list |> List.sortBy (fun _ -> Guid.NewGuid()) |> List.head

module Generators =
  let groupMemberId : Gen<GroupMemberId> =
    gen {
      return GroupMemberId.New
    }
  
  let groupId : Gen<GroupId> =
    gen {
      return GroupId.New
    }
    
  let groupMemberName : Gen<GroupMemberName> =
    gen {
      let allowedChar = Gen.char 'A' 'z'
      let! groupMemberName = Gen.string (Range.linear 3 10) allowedChar
      return GroupMemberName.TryParse groupMemberName |> Result.value
    }
    
  let groupMember : Gen<GroupMember> =
    gen {
      let! id = groupMemberId
      let! name = groupMemberName
      return { GroupMember.Id = id; Name = name; QueuePosition = First }
    }  
  
  let dutiesCount minimum: Gen<DutiesCount> =
    gen {
      let! count = Range.linear minimum 10 |> Gen.int
      return DutiesCount.TryGet count |> Result.value
    }
    
  let orderedGroupMembers minimum : Gen<GroupMember list> =
    gen {
      let! unqueuedMembersWithUniqueNames =
        Gen.list (Range.linear minimum 20) groupMember
        |> Gen.filter (fun members -> members
                                      |> List.groupBy (fun membr -> membr.Name)
                                      |> List.forall (fun (key, group) -> List.length group = 1))
      match unqueuedMembersWithUniqueNames with
      | [] -> return []
      | first :: rest ->
        let queuedRest,_ = rest
                        |> List.mapFold (fun lastMemberId currentMember ->
                          {currentMember with QueuePosition = Following lastMemberId},currentMember.Id) first.Id
        return first :: queuedRest
    }
    
  let shuffledGroupMembers minimum : Gen<GroupMember list> =
    gen {
      let! orderedGroupMembers = orderedGroupMembers minimum
      return orderedGroupMembers |> List.sortBy (fun _ -> Guid.NewGuid())
    }