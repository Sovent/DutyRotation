module DutyRotation.RotateDutiesTests

open DutyRotation
open Hedgehog
open Xunit
open DutyRotation.RotateDuties.Types
open DutyRotation.RotateDuties.Contract
open DutyRotation.RotateDuties.Implementation
open DutyRotation.Common
open FsUnit.Xunit

[<Fact>]
let ``Get current duties when duties count is more than members count return all`` () =
  property {
    let! members = Generators.orderedGroupMembers 0
    let! dutiesCount = members |> List.length |> (+) 1 |> Generators.dutiesCount
    
    let currentDuties = getCurrentDuties dutiesCount members
    
    return currentDuties = members
  } |> Property.check
  
[<Fact>]
let ``Get 1 current duty when many group members returns first`` () =
  let members = Generators.orderedGroupMembers 2 |> Gen.single
  let dutiesCount = 1 |> DutiesCount.TryGet |> Result.value
  let expectedMember = members |> List.head
  
  let [currentDuty] = getCurrentDuties dutiesCount members
  
  currentDuty |> should equal expectedMember
  