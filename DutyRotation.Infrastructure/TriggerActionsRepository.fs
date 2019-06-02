module DutyRotation.Infrastructure.TriggerActionsRepository

open System
open System.Data
open Daffer
open DutyRotation.Common
open FSharp.Nullable

let private targetToStringMap =
  Map.empty
    .Add(AddMembers, "addMembers")
    .Add(RotateDuties, "rotateDuties")

let private stringToTargetMap = targetToStringMap
                                       |> Seq.map (fun pair -> (pair.Value, pair.Key))
                                       |> Map.ofSeq

module private Discriminators = 
  let sendMembersToSlack = "sendMembersToSlack";
  let sendMessageToSlack = "sendMessageToSlack";

module private Parameters =
  let discriminator = "Discriminator"
  let slackMessage = "SlackMessage"
  let slackChannel = "SlackChannel"
  
module Saving =
  type CaseParameters(dicriminator: string, ?slackMessage: string, ?slackChannel: string) =
    member this.AsSeq =
      [
        Parameters.discriminator => dicriminator
        Parameters.slackMessage => (slackMessage |> Option.toNullableRef)
        Parameters.slackChannel => (slackChannel |> Option.toNullableRef)
      ] :> seq<string*obj>
  
  let private parametersForAction =
    function
    | SendMembersToSlack action ->
       CaseParameters (
         dicriminator = Discriminators.sendMembersToSlack,
         slackMessage = action.Description,
         slackChannel = action.Channel
       )
    | SendMessageToSlack _ ->
      CaseParameters (
         dicriminator = Discriminators.sendMessageToSlack
       )
  
  let private allParameters (groupId:GroupId) target action =
    seq {
      yield "Id" => Guid.NewGuid()
      yield "GroupId" => groupId.Value
      yield "Target" => targetToStringMap.[target]
      yield! (action |> parametersForAction |> (fun p -> p.AsSeq))
    } |> Seq.toList
    
  let saveAction (connection:IDbConnection) : DutyRotation.AddTriggerAction.Types.SaveAction =
    fun groupId target action ->    
      executeAsync
        connection
        "INSERT INTO GroupTriggerActions(Id, GroupId, Target, Discriminator, SlackMessage, SlackChannel)
        VALUES (@Id, @GroupId, @Target, @Discriminator, @SlackMessage, @SlackChannel)"
        (allParameters groupId target action) |> Async.map (fun _ -> ())