module DutyRotation.Infrastructure.Json

open System
open System.IO
open FSharp.Reflection
open Newtonsoft.Json.Converters.FSharp

open Newtonsoft.Json

let jsonSerializationSettings = Settings.CreateCorrect (converters = [|
  OptionConverter ();
  TypeSafeEnumConverter ();
  UnionConverter ("$type");
|])