namespace DutyRotation.AddTriggerAction

open DutyRotation.Common
open System

module Contract =
  type AddTriggerActionInput = {
    GroupId: Guid
    TriggerAction: TriggerAction
  }
  
  type AddTriggerActionCommand =
    | AddMember of AddTriggerActionInput
    | RotateDuties of AddTriggerActionInput
    
  type AddTriggerActionError =
    | Validation of ValidationError list
    | GroupNotFound of GroupNotFoundError
    
  type AddTriggerAction = AddTriggerActionCommand -> AsyncResult<unit, AddTriggerActionError>  