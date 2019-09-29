namespace DutyRotation.AddTriggerAction

open DutyRotation.Common
open System

module Contract =
  type AddTriggerActionCommand = {
    GroupId: Guid
    Action: TriggerAction
    Target: TriggerTarget
  }
    
  type AddTriggerActionError =
    | Validation of ValidationError list
    | GroupNotFound of GroupNotFoundError
    
  type AddTriggerAction = AddTriggerActionCommand -> AsyncResult<Trigger, AddTriggerActionError>
  
open Contract
module Types =
  type GetGroupId = AddTriggerActionCommand -> Result<GroupId, ValidationError list>
  
  type CheckIfGroupExists = GroupId -> AsyncResult<unit, GroupNotFoundError>
  
  type ValidationResult = AsyncResult<unit, ValidationError list>
  type ValidateTriggerAction = TriggerAction -> ValidationResult
  
  type ConstructTrigger = TriggerTarget -> TriggerAction -> Trigger
  
  type SaveAction = GroupId -> Trigger -> Async<unit>
  
  type SlackChannelName = string
  type DoesSlackChannelExists = SlackChannelName -> Async<bool>
  
open Types  
module Implementation =
  let getGroupId : GetGroupId = fun command -> GroupId.TryParse command.GroupId
  
  let private validateSlackChannelExistence (doesExist : DoesSlackChannelExists) (channel:SlackChannelName)
    : ValidationResult =
    async {
      let! exists = doesExist channel
      return if exists
        then Ok ()
        else ValidationError.createSingle "Channel does not exist" channel
    }
  
  let private validateSlackMessage slackMessage : ValidationResult =    
    let slackMessageMaxLength = 300
    ConstrainedType.createString id 0 slackMessageMaxLength slackMessage
      |> Result.map (fun _ -> ())
      |> AsyncResult.ofResult
      
  let private sumValidation = AsyncResult.sequenceA >> AsyncResult.map (fun _ -> ())
    
  let validateTriggerAction (doesChannelExists: DoesSlackChannelExists) : ValidateTriggerAction = fun action ->
    match action with
      | SendMembersToSlack a ->
        sumValidation [
          validateSlackMessage a.Description;
          validateSlackChannelExistence doesChannelExists a.Channel
        ]
      | SendMessageToSlack a ->
        sumValidation [
          validateSlackMessage a.Message;
          validateSlackChannelExistence doesChannelExists a.Channel
        ]
        
  let constructTrigger : ConstructTrigger =
    fun target action ->
      {
        Id = Guid.NewGuid()
        Target = target
        Action = action
      }
      
  let addTriggerAction
    (doesChannelExists: DoesSlackChannelExists)
    (checkIfGroupExists: CheckIfGroupExists)
    (saveAction:SaveAction)
    : AddTriggerAction = fun command ->
    asyncResult {
      let! groupId = command |> getGroupId |> AsyncResult.ofResult |> AsyncResult.mapError Validation
      do! checkIfGroupExists groupId |> AsyncResult.mapError GroupNotFound
      do! validateTriggerAction doesChannelExists command.Action |> AsyncResult.mapError Validation
      let trigger = constructTrigger command.Target command.Action
      do! saveAction groupId trigger |> AsyncResult.ofAsync
      return trigger
    }
        
  