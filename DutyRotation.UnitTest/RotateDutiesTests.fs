module DutyRotation.RotateDutiesTests

open DutyRotation
open Hedgehog
open Xunit
open DutyRotation.RotateDuties.Contract
open DutyRotation.RotateDuties.Implementation
open DutyRotation.Common
open FsUnit.Xunit

[<Fact>]
let ``Get current duties when duties count is more than members count return all`` () =
  property {
    let! members = Generators.orderedGroupMembers 0 5
    let! dutiesCount = members |> List.length |> (+) 1 |> Generators.dutiesCount
    
    let currentDuties = getCurrentDuties dutiesCount members
    
    return currentDuties = members
  } |> Property.check
  
[<Fact>]
let ``Get 1 current duty when many group members returns first`` () =
  let members = Generators.orderedGroupMembers 2 20 |> Gen.single
  let dutiesCount = 1 |> DutiesCount.TryGet |> Result.value
  let expectedMember = members |> List.head
  
  let [currentDuty] = getCurrentDuties dutiesCount members
  
  currentDuty |> should equal expectedMember

[<Fact>]
let ``When members count is twice as much as duties count, former duties are never current duties after rotation`` () =
  let toIdsSet = List.map (fun membr -> membr.Id) >> Set.ofList
  property {    
    let! dutiesCount = Generators.dutiesCount 1    
    let! members = Generators.orderedGroupMembers (dutiesCount.Value * 2) 20
    let dutiesBeforeRotation = getCurrentDuties dutiesCount members |> toIdsSet
    let rotate = rotateDuties
                   (fun _ -> dutiesCount |> AsyncResult.retn)
                   (fun _ -> members |> Async.retn)
                   (fun _ _ -> Async.retn ())
                   >> Async.RunSynchronously
                   >> Result.value
    let dutiesAfterRotation = { RotateDutiesCommand.GroupId = GroupId.New.Value } |> rotate |> toIdsSet
    return Set.intersect dutiesBeforeRotation dutiesAfterRotation |> Set.isEmpty
  } |> Property.check

[<Fact>]  
let ``With duties count D, members count M (M>D), greatest common divisor G = gcd(D, M), set first to tail G/M times results in the same collection`` () =
  let rec gcd d m = if m = 0 then d else gcd m (d % m)
  property {
    let! dutiesCount = Generators.dutiesCount 1
    let! members = Generators.orderedGroupMembers (dutiesCount.Value + 1) 20
    let membersCount = members |> List.length
    let repeatsToGetSameCollection = membersCount / gcd dutiesCount.Value membersCount
    let extractResult (_, _, result) = result
    let result = seq { 1 .. repeatsToGetSameCollection }
                 |> Seq.fold (fun membrs _ -> setFirstToTail dutiesCount membrs |> extractResult) members
    return members = result
  } |> Property.check
  