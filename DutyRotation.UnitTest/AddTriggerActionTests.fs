module DutyRotation.AddTriggerActionTests

open System
open Xunit
open DutyRotation.AddTriggerAction.Contract
open DutyRotation.AddTriggerAction.Implementation
open DutyRotation.Common
open FsUnit.Xunit

let saveActionStub _ _ _ = Async.retn ()
let doesChannelExistsSuccessfulCheck _ = Async.retn true
let doesChannelExistsFailureCheck _ = Async.retn false
let checkGroupExistsSuccessfulCheck _ = AsyncResult.retn ()
let checkGroupExistsFailureCheck g = { GroupId = g }|> AsyncResult.ofError
let addAction x y c = addTriggerAction x y saveActionStub c |> Async.RunSynchronously
let createSendSlackMembersCommandWithDescription description = {
    GroupId = Guid.NewGuid()
    Target = RotateDuties
    Action = SendMembersToSlack {
      SendMembersToSlack.Description = description
      Channel = "Channel"
    }
  }

[<Fact>]
let ``Add "send members to slack" trigger with channel that does not exists - one validation error`` () =
  let command = createSendSlackMembersCommandWithDescription "Description"
  let result = addAction doesChannelExistsFailureCheck checkGroupExistsSuccessfulCheck command
  let properError = match result with    
                    | Error (Validation [_]) -> true
                    | _ -> false
  properError |> should be True
  
[<Fact>]
let ``Add "send members to slack" trigger with empty description - one validation error`` () =
  let command = createSendSlackMembersCommandWithDescription null
  let result = addAction doesChannelExistsSuccessfulCheck checkGroupExistsSuccessfulCheck command
  let properError = match result with    
                    | Error (Validation [_]) -> true
                    | _ -> false
  properError |> should be True
 
[<Fact>]
let ``Add "send members to slack" trigger both with no description and invalid channel - two validation errors`` () =
  let command = createSendSlackMembersCommandWithDescription null
  let result = addAction doesChannelExistsFailureCheck checkGroupExistsSuccessfulCheck command
  let properErrors = match result with    
                     | Error (Validation [_;_]) -> true
                     | _ -> false
  properErrors |> should be True
  
[<Fact>]
let ``Add "send members to slack" trigger to non existant group returns in group not found error even with no description`` () =
  let command = createSendSlackMembersCommandWithDescription null
  let result = addAction doesChannelExistsSuccessfulCheck checkGroupExistsFailureCheck command
  let properError = match result with
                     | Error (GroupNotFound _) -> true
                     | _ -> false
  properError |> should be True

[<Fact>]  
let ``Add "send members to slack" trigger happy path returns no errors`` () =
  let command = createSendSlackMembersCommandWithDescription "Description"
  let result = addAction doesChannelExistsSuccessfulCheck checkGroupExistsSuccessfulCheck command
  let isOk = match result with | Ok _ -> true | _ -> false
  isOk |> should be True