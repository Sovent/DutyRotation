namespace DutyRotation

open Hedgehog
open DutyRotation.Common

module Gen =
  let single (gen:Gen<'a>) : 'a =
    Gen.sample (Size.MaxValue) 1 gen |> List.head

module Generators =
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