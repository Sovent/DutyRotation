module DutyRotation.Infrastructure.TriggerActionsRepository

open System
open System.Data
open Daffer
open DutyRotation.Common

let private targetToStringMap =
  Map.empty
    .Add(AddMembers, "addMembers")
    .Add(RotateDuties, "rotateDuties")

let private stringToTargetMap = targetToStringMap
                                       |> Seq.map (fun pair -> (pair.Value, pair.Key))
                                       |> Map.ofSeq

let private sendMembersToSlackDiscriminator = "sendMembersToSlack";
  
let private parametersForAction =
  function
  | SendMembersToSlack action -> [
    "Discriminator" => sendMembersToSlackDiscriminator;
    "SlackMessage" => action.Description
    "SlackChannel" => action.Channel
  ]
  
let saveAction (connection:IDbConnection) : DutyRotation.AddTriggerAction.Types.SaveAction =
  fun groupId target action ->    
    executeAsync
      connection
      "INSERT INTO GroupTriggerActions(Id, GroupId, Target, Discriminator, SlackMessage, SlackChannel)
      VALUES (@Id, @GroupId, @Target, @Discriminator, @SlackMessage, @SlackChannel)"
      (
       ["Id" => Guid.NewGuid()
        "GroupId" => groupId.Value
        "Target" => targetToStringMap.[target]]
       @ (parametersForAction action)
      ) |> Async.map (fun _ -> ())