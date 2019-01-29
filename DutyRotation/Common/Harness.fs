namespace DutyRotation.Common

type ValidationError = {
  Description: string
  Value: string
}
module ValidationError =
  let createSingle message value =
    let validationError : ValidationError = {
      Description = message
      Value = if value = Unchecked.defaultof<'a> then "null" else value.ToString()
    }
    validationError |> List.singleton |> Error
    
