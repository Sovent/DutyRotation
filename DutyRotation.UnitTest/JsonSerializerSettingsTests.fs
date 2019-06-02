module DutyRotation.JsonSerializerSettingsTests

open System.Text.RegularExpressions
open Xunit
open DutyRotation.Infrastructure.Json
open DutyRotation.Common
open DutyRotation.Common
open Newtonsoft.Json
open FsUnit.Xunit

let serialize obj = JsonConvert.SerializeObject(obj, formatting = Formatting.None, settings = jsonSerializationSettings)
let deserialize<'a> input = JsonConvert.DeserializeObject<'a>(input, settings = jsonSerializationSettings)
let trim input = Regex.Replace(input, @"\s+", "")
let shouldEqualTrimmed expected actual = should equal (trim expected) (trim actual)

[<Fact>]
let ``Serialize DU with cases with no payload, serialized as plain string`` () =
  let triggerTarget = RotateDuties
  let serialized = serialize triggerTarget
  serialized |> shouldEqualTrimmed "\"RotateDuties\""

[<Fact>]
let ``Deserialize DU with cases with no payload, deserialized from plain string`` () =
  let input = "\"AddMembers\""
  let deserialized = deserialize<TriggerTarget> input
  let correct = match deserialized with | AddMembers -> true | _ -> false
  correct |> should be True
  
[<Fact>]  
let ``Deserialize DU with cases with payload, case is chosen from $type field`` () =
  let expected = {
    Description = "cooldescription"
    Channel = "nicechannel"
  }
  let input = """
  {
    "$type": "SendMembersToSlack",
    "Description":"cooldescription",
    "Channel":"nicechannel"
  }
  """
  let deserialized = deserialize<TriggerAction> input
  let correct = match deserialized with
                | SendMembersToSlack i when i = expected -> true
                | _ -> false
  correct |> should be True

[<Fact>] 
let ``Serialize DU with cases with payload, case is stored in $type field`` () =
  let expected = """
  {
    "$type": "SendMembersToSlack",
    "Description":"cooldescription",
    "Channel":"nicechannel"
  }
  """
  let input = SendMembersToSlack {
    Description = "cooldescription"
    Channel = "nicechannel"
  }
  let serialized = serialize input
  serialized |> shouldEqualTrimmed expected

[<Fact>]  
let ``Serialize SCDU with private ctor still manages to get underlying type`` () =
  let groupName = GroupName.TryParse "grouppy" |> Result.value
  let expected = "\"grouppy\""
  
  serialize groupName |> should equal expected
  
[<Fact>]
let ``Deserialize SCDU with private ctor creates scdu`` () =
  let expectedDutiesCount = DutiesCount.TryGet 5 |> Result.value
  
  deserialize<DutiesCount> "5" |> should equal expectedDutiesCount

[<Fact>]  
let ``Some case in option serialized as plain value`` () =
  let value = Some(2)
  
  serialize value |> should equal "2"

[<Fact>]  
let ``None case in option serialized as null`` () =
  let emptyValue: int option = None
  
  serialize emptyValue |> should equal "null"