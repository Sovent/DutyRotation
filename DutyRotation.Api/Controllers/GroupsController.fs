module GroupsController

open Giraffe
open DutyRotation.CreateGroup.Contract
open DutyRotation.AddGroupMember.Contract
open DutyRotation.RotateDuties.Contract
open DutyRotation.GetGroupInfo.Contract
open DutyRotation.AddTriggerAction.Contract
open DutyRotation.Common
open DutyRotation.Infrastructure
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

let simplyProceedRequest (execute: HttpContext -> AsyncResult<'a, 'b>): HttpHandler =
  fun (next: HttpFunc) (context: HttpContext) ->
    task {
      try
        let! commandResult = execute context |> Async.StartAsTask
        let httpResult = match commandResult with
                               | Ok ok -> Successful.CREATED ok
                               | Error error -> RequestErrors.BAD_REQUEST error
        return! httpResult next context
      with
        | exc -> return! RequestErrors.BAD_REQUEST exc next context     
    }

let bindModel<'a> (context:HttpContext) = context.BindModelAsync<'a> () |> Async.AwaitTask |> AsyncResult.ofAsync 
let createSimpleGroup : HttpHandler =
  simplyProceedRequest <| fun context ->
    asyncResult {
      let! model = bindModel<CreateSimpleGroupCommand> context
      return! CompositionRoot.createSimpleGroup model
    }

[<CLIMutable>]
type AddGroupMemberModel = { MemberName:string }

let addGroupMember groupId : HttpHandler =
  simplyProceedRequest <| fun context ->
    asyncResult {
      let! model = bindModel<AddGroupMemberModel> context |> AsyncResult.mapError AddGroupMemberError.Validation
      let command = { AddGroupMemberCommand.GroupId = groupId; MemberName = model.MemberName }
      return! CompositionRoot.addGroupMember command
    }

let rotateDuties groupId : HttpHandler =
  simplyProceedRequest <| fun _ -> CompositionRoot.rotateDuties { GroupId = groupId }
    
let getGroupInfo groupId : HttpHandler =
  simplyProceedRequest <| fun _ -> CompositionRoot.getGroupInfo { GroupId = groupId }
  
  
[<CLIMutable>]
type AddTriggerActionModel = {
  Action: TriggerAction
  Target: TriggerTarget  
}

let addTriggerAction groupId : HttpHandler =
  simplyProceedRequest <| fun context ->
    asyncResult {
      let! model = bindModel<AddTriggerActionModel> context |> AsyncResult.mapError AddTriggerActionError.Validation
      let command = {
        GroupId = groupId
        Action = model.Action
        Target = model.Target
      }
      
      return! CompositionRoot.addTriggerAction command
    }