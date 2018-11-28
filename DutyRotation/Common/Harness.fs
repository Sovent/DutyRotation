namespace DutyRotation.Common

open System

//type DomainEvent<'a> = {
//  Id: Guid
//  OccuredOn: DateTimeOffset
//  Payload: 'a
//}

type ValidationError = {
  Type: string
  Description: string
  Value: string
}
module ValidationError =
  let create message value =
    let validationError : ValidationError = {
      Type = typeof<'a>.Name
      Description = message
      Value = value.ToString()
    }
    Error validationError
    
