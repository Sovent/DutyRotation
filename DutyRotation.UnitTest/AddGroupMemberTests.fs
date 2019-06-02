module DutyRotation.AddGroupMemberTests

open System
open DutyRotation
open Hedgehog
open Xunit
open DutyRotation.AddGroupMember.Implementation
open DutyRotation.AddGroupMember.Contract
open DutyRotation.AddGroupMember.Types
open DutyRotation.Common
open FsUnit.Xunit

[<Fact>]
let ``Trying to create member with name existing in group fails with error`` () =
  property {
    let! groupId = Generators.groupId
    let! members = Generators.shuffledGroupMembers 1 20
    let existingMember = Gen.randomElement members
    let existingName = existingMember.Name
    let tail = QueuePosition.tail members
    let createNewMemberResult = createNewMember (fun () -> GroupMemberId.New) members groupId existingName tail
    return match createNewMemberResult with
            | Error {MemberName = memberName} -> memberName = existingName
            | _ -> false
  } |> Property.check
  
[<Fact>]
let ``Create member with unique name adds member to tail`` () =
  property {
    let! groupId = Generators.groupId
    let! members = Generators.shuffledGroupMembers 0 20
    let! uniqueName = Generators.groupMemberName
                      |> Gen.filter (fun name -> members |> List.forall (fun membr -> membr.Name <> name))
    let tail = QueuePosition.tail members
    let! groupMemberId = Generators.groupMemberId
    let expectedGroupMember = {Id=groupMemberId; Name = uniqueName; QueuePosition = tail}
    let createNewMemberResult = createNewMember (fun () -> groupMemberId) members groupId uniqueName tail
    return match createNewMemberResult with
                  | Ok groupMember when groupMember = expectedGroupMember -> true
                  | _ -> false
  } |> Property.check

[<Fact>]
let ``When group id is invalid, add group member returns validation error`` () =
  let command = { AddGroupMemberCommand.GroupId = Guid.Empty; MemberName = "Participant" }
  let addGroupMemberResult = addGroupMember (fun _ -> AsyncResult.retn []) (fun _ _ -> Async.retn ()) command
                             |> Async.RunSynchronously
  
  let isValidationError = match addGroupMemberResult with | Error (Validation [error]) -> true | _ -> false
  
  isValidationError |> should be True
  
[<Fact>]
let ``When group member name is invalid, add group member returns validation error`` () =
  let command = { AddGroupMemberCommand.GroupId = Guid.NewGuid(); MemberName = "" }
  let addGroupMemberResult = addGroupMember (fun _ -> AsyncResult.retn []) (fun _ _ -> Async.retn ()) command
                             |> Async.RunSynchronously
  
  let isValidationError = match addGroupMemberResult with | Error (Validation [error]) -> true | _ -> false
  
  isValidationError |> should be True
  
[<Fact>]
let ``When both command parameters is invalid, add group member results in two validation errors`` () =
  let command = { AddGroupMemberCommand.GroupId = Guid.Empty; MemberName = "" }
  let addGroupMemberResult =
    addGroupMember (fun _ -> AsyncResult.retn []) (fun _ _ -> Async.retn ()) command
    |> Async.RunSynchronously
  
  let isValidationError = match addGroupMemberResult with | Error (Validation [error1;error2]) -> true | _ -> false
  
  isValidationError |> should be True

[<Fact>]
let ``When member added successfully, save member called`` () =
  let command = { AddGroupMemberCommand.GroupId = Guid.NewGuid(); MemberName = "Participant" }
  let mutable savedMember : GroupMember option = None
  let saveMember : SaveMember = fun id membr -> (savedMember <- Some membr) |> Async.retn
  let addGroupMemberResult = addGroupMember (fun _ -> AsyncResult.retn []) saveMember command
                             |> Async.RunSynchronously
  
  let isSuccess = match addGroupMemberResult with | Ok id -> true | _ -> false
  let isSavedMemberCorrect = match savedMember with
                              | Some membr when membr.Name.Value = command.MemberName -> true
                              | _ -> false
  
  (isSuccess && isSavedMemberCorrect) |> should be True
  
[<Fact>]
let ``When get group members return error, add group member returns group not found`` () =
  let command = { AddGroupMemberCommand.GroupId = Guid.NewGuid(); MemberName = "Participant" }
  let getGroupMembers : GetGroupMembers =
    fun id -> AsyncResult.ofError { GroupNotFoundError.GroupId = command.GroupId |> GroupId.TryParse |> Result.value }
  let addGroupMemberResult = addGroupMember getGroupMembers (fun _ _ -> Async.retn ()) command |> Async.RunSynchronously
  
  let isGroupNotFound = match addGroupMemberResult with | Error (GroupNotFound err) -> true | _ -> false
  
  isGroupNotFound |> should be True