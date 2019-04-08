module DutyRotation.CreateGroupTests

open Xunit
open DutyRotation.CreateGroup.Contract
open DutyRotation.CreateGroup.Types
open DutyRotation.CreateGroup.Implementation
open FsUnit.Xunit

let shouldReturnValidationError command =
  let result = getGroupSettings command
  let hasValidationError = match result with
                            | Error [error] -> true
                            | _ -> false
  hasValidationError |> should be True
    
[<Fact>]
let ``When empty group name get group settings returns error`` () =
  { GroupName = ""; RotationCronRule = "0 0 0 ? * 2/3 *"; DutiesCount = 2; RotationStartDate = None }
  |> shouldReturnValidationError
     
[<Fact>]
let ``When empty rotation cron rule returns error`` () =
  { GroupName = "nice name"; RotationCronRule = ""; DutiesCount = 3; RotationStartDate = None }
  |> shouldReturnValidationError

[<Fact>]
let ``When duties count lower than 1 returns error`` () =
  { GroupName = "Nice name"; RotationCronRule = "0 0 0 ? * 2/3 *"; DutiesCount = 0; RotationStartDate = None }
  |> shouldReturnValidationError
  
[<Fact>]  
let ``All command parameters are invalid, result contains all errors`` () =
  let command = { GroupName = ""; RotationCronRule = ""; DutiesCount = -2; RotationStartDate = None }
  let hasThreeValidationErrors = match getGroupSettings command with
                                  | Error [fst; scnd; thrd] -> true
                                  | _ -> false
  hasThreeValidationErrors |> should be True
  
[<Fact>]  
let ``When no validation errors calls save`` () =
  let mutable saveCalled = false
  let save : SaveGroup = fun group ->
    saveCalled <- true
    Async.retn ()
  
  let command = { GroupName = "nice name";
                  RotationCronRule = "0 0 0 ? * 2/3 *";
                  DutiesCount = 3;
                  RotationStartDate = None }
  
  let result = createSimpleGroup save command |> Async.RunSynchronously  
  let successAndSaveCalled = match result, saveCalled with | Ok _, true -> true | _ -> false
  
  successAndSaveCalled |> should be True
  
[<Fact>]  
let ``When has validation errors does not call save`` () =
  let mutable saveCalled = false
  let save : SaveGroup = fun group ->
    saveCalled <- true
    Async.retn ()
  
  let command = { GroupName = "";
                  RotationCronRule = "0 0 0 ? * 2/3 *";
                  DutiesCount = 3;
                  RotationStartDate = None }
  
  let result = createSimpleGroup save command |> Async.RunSynchronously  
  let successAndSaveCalled = match result, saveCalled with | Ok _, true -> true | _ -> false
  
  successAndSaveCalled |> should be False