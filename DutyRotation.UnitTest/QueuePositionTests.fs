module DutyRotation.QueuePositionTests

open DutyRotation.Common
open Xunit
open FsUnit.Xunit

[<Fact>]
let ``Finding tail in empty members list returns first`` () =
  let queuePosition = QueuePosition.tail []
  
  (match queuePosition with | First -> true | _ -> false)
  |> should be True

[<Fact>]
let ``Finding tail in single member list`` () =
  let groupMember = Gen.single Generators.groupMember
  let expected = Following groupMember.Id
  
  let actual = QueuePosition.tail [groupMember]
  
  actual |> should equal expected