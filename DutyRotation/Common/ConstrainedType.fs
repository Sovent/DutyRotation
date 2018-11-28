namespace DutyRotation.Common

module ConstrainedType =
  open System

  let createString ctor minLength maxLength input =
    if String.IsNullOrEmpty(input) then
      let msg = "Value must not be null or empty"
      ValidationError.create msg input
    elif input.Length > maxLength then
      let msg = sprintf "Value must not be more than %i chars" maxLength 
      ValidationError.create msg input 
    elif input.Length < minLength then
      let msg = sprintf "Value must not be less than %i chars" minLength 
      ValidationError.create msg input
    else
      Ok (ctor input)

  let createLike ctor pattern value = 
    if String.IsNullOrEmpty(value) then
      let msg = sprintf "Value must not be null or empty" 
      ValidationError.create msg value
    elif System.Text.RegularExpressions.Regex.IsMatch(value,pattern) then
      Ok (ctor value)
    else
      let msg = sprintf "Value must match the pattern '%s'" pattern
      ValidationError.create msg value
  
  let createDate ctor minValue maxValue (input:DateTimeOffset) =
    if input < minValue then
      let msg = sprintf "Value must not be less than min value %O" minValue
      ValidationError.create msg input
    elif input > maxValue then
      let msg = sprintf "Value must not be more than max value %O" maxValue
      ValidationError.create msg input
    else
      Ok (ctor input)

  let createInt ctor minValue maxValue input = 
    if input < minValue then
      let msg = sprintf "Value must not be less than %i" minValue
      ValidationError.create msg input
    elif input > maxValue then
      let msg = sprintf "Value must not be greater than %i" maxValue
      ValidationError.create msg input
    else
      Ok (ctor input)

  let createDefaultConstrainedString ctor value =
    createString ctor 1 50 value
