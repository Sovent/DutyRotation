module GroupsController

open Giraffe
open DutyRotation.CreateGroup
open DutyRotation.AddGroupMember
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

let addGroupMember : HttpHandler =
  fun (next: HttpFunc) (context: HttpContext) ->
    task {
      try
          let! model = context.BindModelAsync<AddGroupMember>()
          return! json model next context
        with
          | exc -> return! RequestErrors.BAD_REQUEST exc next context    
    }