module DutyRotation.QueuePositionTests

open System
open DutyRotation.Common
open Xunit
open FsUnit.Xunit
open Hedgehog

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
  
[<Fact>]
let ``For list with 2 or more members no one holds found tail queue position`` () =
  property {
    let! groupMemberList = Generators.shuffledGroupMembers 2 20
    
    let tailPosition = QueuePosition.tail groupMemberList
    
    return not <| List.exists (fun membr -> membr.QueuePosition = tailPosition) groupMemberList
  } |> Property.check
  
[<Fact>] 
let ``For list with 2 or members tail should follow someone inside the queue`` () =
  property {
    let! groupMemberList = Generators.shuffledGroupMembers 2 20
    
    let (Following memberId) = QueuePosition.tail groupMemberList
    
    return groupMemberList |> List.exists (fun membr -> membr.Id = memberId)
  } |> Property.check
  
[<Fact>]
let ``Find queue position when input queue order is 1->3->2`` () =
  let id (input: string) = Guid.Parse(input) |> GroupMemberId.TryGet |> Result.value
  let name = GroupMemberName.TryParse >> Result.value
  let groupMembers = [
    { Id = id "0e2e8d03-8604-4967-90ad-74386734ad26"
      Name = name "AAA"
      QueuePosition = First };
    { Id = id "a4f117db-7b51-48e7-8bf3-0351f051ebc5"
      Name = name "AAA"
      QueuePosition = id "fd7e239e-4fd9-4162-beec-bf480e56dccc" |> Following};
    { Id = id "fd7e239e-4fd9-4162-beec-bf480e56dccc";
      Name = name "AAA";
      QueuePosition = id "0e2e8d03-8604-4967-90ad-74386734ad26" |> Following}    
  ]
  let expectedPosition = Following (id "a4f117db-7b51-48e7-8bf3-0351f051ebc5")
  
  let tailPosition = QueuePosition.tail groupMembers
  
  tailPosition |> should equal expectedPosition