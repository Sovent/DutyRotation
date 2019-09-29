module DutyRotation.Infrastructure.TriggersRepository

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
  [<Literal>]
  let sendMembersToSlack = "sendMembersToSlack";
  [<Literal>]
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
  
  let private allParameters (groupId:GroupId) trigger =
    seq {
      yield "Id" => trigger.Id
      yield "GroupId" => groupId.Value
      yield "Target" => targetToStringMap.[trigger.Target]
      yield! (trigger.Action |> parametersForAction |> (fun p -> p.AsSeq))
    } |> Seq.toList
    
  let saveAction (connection:IDbConnection) : DutyRotation.AddTriggerAction.Types.SaveAction =
    fun groupId trigger ->    
      executeAsync
        connection
        "INSERT INTO GroupTriggerActions(Id, GroupId, Target, Discriminator, SlackMessage, SlackChannel)
        VALUES (@Id, @GroupId, @Target, @Discriminator, @SlackMessage, @SlackChannel)"
        (allParameters groupId trigger) |> Async.map (fun _ -> ())

module Loading =
  [<CLIMutable>]
  type private TriggerRow = {
    Id: Guid
    GroupId: Guid
    Target: string
    Discriminator: string
    SlackMessage: string
    SlackChannel: string
  }
  
  let private mapToTriggerAction (row: TriggerRow) =
    match row.Discriminator with
    | Discriminators.sendMessageToSlack ->
      SendMessageToSlack {
        Message = row.SlackMessage
        Channel = row.SlackChannel
      }
    | Discriminators.sendMembersToSlack ->
      SendMembersToSlack {
        Description = row.SlackMessage
        Channel = row.SlackChannel
      }
      
  let private mapToTrigger row =
    {
      Id = row.Id
      Action = mapToTriggerAction row
      Target = stringToTargetMap.[row.Target]
    }
    
  let getTriggers (connection:IDbConnection) (groupId:GroupId) =
    async {
      let! rows =
        queryAsync<TriggerRow>
          connection
          "select * from GroupTriggerActions where GroupId=@GroupId"
          ["GroupId" => groupId.Value]
      return rows |> List.map mapToTrigger        
    }