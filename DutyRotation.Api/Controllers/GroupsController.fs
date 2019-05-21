module GroupsController

open Giraffe
open DutyRotation.CreateGroup.Contract
open DutyRotation.AddGroupMember.Contract
open DutyRotation.RotateDuties.Contract
open DutyRotation.GetGroupInfo.Contract
open DutyRotation.Infrastructure
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

let createSimpleGroup : HttpHandler =
  fun (next: HttpFunc) (context: HttpContext) ->
    task {
      try
        let! model = context.BindModelAsync<CreateSimpleGroupCommand>()
        let! commandResult = CompositionRoot.createSimpleGroup model |> Async.StartAsTask
        let httpResult = match commandResult with
                               | Ok groupId -> Successful.CREATED groupId.Value
                               | Error errors -> RequestErrors.BAD_REQUEST errors
        return! httpResult next context
      with
        | exc -> return! RequestErrors.BAD_REQUEST exc next context     
    }

[<CLIMutable>]
type AddGroupMemberModel = { MemberName:string }

let addGroupMember groupId : HttpHandler =
  fun (next: HttpFunc) (context: HttpContext) ->
    task {
      try
        let! model = context.BindModelAsync<AddGroupMemberModel>()
        let command = { AddGroupMemberCommand.GroupId = groupId; MemberName = model.MemberName }
        let! commandResult = CompositionRoot.addGroupMember command |> Async.StartAsTask
        let httpResult = match commandResult with
                             | Ok memberId -> Successful.CREATED memberId.Value
                             | Error errors -> RequestErrors.BAD_REQUEST errors
        return! httpResult next context
      with
        | exc -> return! RequestErrors.BAD_REQUEST exc next context    
    }

let rotateDuties groupId : HttpHandler =
  fun (next: HttpFunc) (context: HttpContext) ->
    task {
      try
        let command = { RotateDutiesCommand.GroupId = groupId }
        let! commandResult = CompositionRoot.rotateDuties command |> Async.StartAsTask
        let httpResult = match commandResult with
                             | Ok currentDuties -> Successful.OK currentDuties
                             | Error errors -> RequestErrors.BAD_REQUEST errors
        return! httpResult next context
      with
        | exc -> return! RequestErrors.BAD_REQUEST exc next context    
    }
    
let getGroupInfo groupId : HttpHandler =
  fun (next:HttpFunc) (context: HttpContext) ->
    task {
      try
        let query = { GetGroupInfoQuery.GroupId = groupId }
        let! commandResult = query |> CompositionRoot.getGroupInfo |> Async.StartAsTask
        let httpResult = match commandResult with
                             | Ok groupInfo -> Successful.OK groupInfo
                             | Error errors -> RequestErrors.BAD_REQUEST errors
        return! httpResult next context
      with
        | exc -> return! RequestErrors.BAD_REQUEST exc next context
    }