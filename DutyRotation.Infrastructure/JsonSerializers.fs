module DutyRotation.Infrastructure.JsonSerializers

open System
open System.IO
open FSharp.Reflection

open Newtonsoft.Json

module Reflection =

  // swap these commented lines to get logging
  let logfn fmt =
    Printf.kprintf ignore fmt
    //printfn fmt

  open Newtonsoft.Json.Linq
  
  let isUnion t = FSharpType.IsUnion(t, allowAccessToPrivateRepresentation = true)
  let getCases t = FSharpType.GetUnionCases(t, allowAccessToPrivateRepresentation = true)
  let getUnionFields x = FSharpValue.GetUnionFields(x, x.GetType(), allowAccessToPrivateRepresentation = true)
  let allCasesEmpty (y: System.Type) = y |> getCases |> Array.forall (fun case -> case.GetFields() |> Array.isEmpty)
  let isList (y: System.Type) = y.IsGenericType && typedefof<List<_>> = y.GetGenericTypeDefinition ()
  let isOption (y: System.Type) = y.IsGenericType && typedefof<_ option> = y.GetGenericTypeDefinition ()

  let (|MapKey|_|) (key: 'a) map = map |> Map.tryFind key

  let advance (reader: JsonReader) =
    reader.Read() |> ignore
    reader

  let tokToReader (tok: #JToken) = tok.CreateReader ()

  let rec readValueIntoJToken   (reader: JsonReader): JToken * JsonReader =
      logfn "reading token of type %s" (string reader.TokenType)
      let result =
        match reader.TokenType with
        | JsonToken.Boolean     -> (reader.Value :?> bool)    |> JToken.op_Implicit
        | JsonToken.Float       -> (reader.Value :?> float)   |> JToken.op_Implicit
        | JsonToken.Integer     -> (reader.Value :?> int64)   |> JToken.op_Implicit
        | JsonToken.String      -> (reader.Value :?> string)  |> JToken.op_Implicit
        | JsonToken.Null        -> JValue.CreateNull()        :> JToken
        | x                     -> failwithf "don't know how to read value of type %s into a JToken" (string x)
      result, advance reader
  and readObjectIntoJObject     (reader: JsonReader): JObject * JsonReader =
    let rec loop (reader: JsonReader) (o: JObject): JObject * JsonReader =
      match reader.TokenType with
      | JsonToken.EndObject ->
        o, advance reader
      | JsonToken.PropertyName ->
        let name = reader.Value :?> string
        let value, reader' = readPropertyIntoJToken (advance reader)
        o.[name] <- value
        loop reader' o
      | x -> failwithf "wasn't expecting token of type %s in object" (string x)
    logfn "about to read object, token is of type %s" (string reader.TokenType)
    loop (advance reader) (JObject())
  and readArrayIntoJArray       (reader: JsonReader): JArray * JsonReader  =
    let reader' = advance reader
    // what kind of items are we pulling out?
    let readF =
      match reader'.TokenType with
      | JsonToken.Boolean
      | JsonToken.Float
      | JsonToken.Integer
      | JsonToken.String ->
        logfn "reading array of %ss" (string reader'.TokenType)
        readValueIntoJToken
        // read many values until end of array
      | JsonToken.StartArray ->
        logfn "reading array of arrays"
        readArrayIntoJArray >> fun (a, reader) -> a :> _, reader
      | JsonToken.StartObject ->
        logfn "reading array of objects"
        readObjectIntoJObject >> fun (o, reader) -> o :> _, reader
      | n -> failwithf "don't know how to read multiples of %s" (string n)

    let rec loop (reader: JsonReader) (arr: JArray) =
      match reader.TokenType with
      | JsonToken.EndArray ->
        arr, advance reader
      | _ ->
        let item, reader' = readF reader
        logfn "next token is of type %s" (string reader'.TokenType)
        arr.Add item
        loop reader' arr

    loop reader (JArray())

  and readPropertyIntoJToken (reader: JsonReader): JToken * JsonReader =
    match reader.TokenType with
    | JsonToken.Boolean
    | JsonToken.Float
    | JsonToken.Integer
    | JsonToken.String
    | JsonToken.Null        -> readValueIntoJToken reader
    | JsonToken.StartArray  -> readArrayIntoJArray reader |> fun (tok, reader) -> tok :> _ , reader
    | JsonToken.StartObject -> readObjectIntoJObject reader |> fun (tok, reader) -> tok :> _, reader
    | x                     -> failwithf "value reader doesn't know how to handle JToken of type %s" (string x)

  /// needs to read a jsonreader's properties and buffer the various json property values into separate JsonReaders,
  /// so that we can use the property names later to find the case info containing those properties,
  /// so that we can use the case reflection info to actually deserialize the jsonreaders into the correct types
  let getPropsAndReaders (reader: JsonReader) : Map<string, JsonReader> =
    let rec loop (reader: JsonReader) propMap =
      match reader.TokenType with
      | JsonToken.None -> propMap
      | JsonToken.EndObject ->
        loop (advance reader) propMap
      | JsonToken.PropertyName ->
        let name = reader.Value :?> string
        logfn "reading '%s'" name
        let reader' = advance reader
        let (property, reader'') = readPropertyIntoJToken reader'
        loop (reader'') (propMap |> Map.add name (tokToReader property))
      | x -> failwithf "outer property reader loop doesn't know how to handle JToken of type %s" (string x)

    loop (advance reader) Map.empty

open Reflection

/// F# options-converter
type OptionConverter() =
  inherit JsonConverter()
  let optionTy = typedefof<option<_>>

  override __.CanConvert t =
    t.IsGenericType
    && optionTy.Equals (t.GetGenericTypeDefinition())

  override __.WriteJson(writer, value, serializer) =
    let value =
      if isNull value then
        null
      else
        let _,fields = getUnionFields value
        fields.[0]
    serializer.Serialize(writer, value)

  override __.ReadJson(reader, t, _existingValue, serializer) =
    let innerType = t.GetGenericArguments().[0]

    let innerType =
      if innerType.IsValueType then
        typedefof<Nullable<_>>.MakeGenericType([| innerType |])
      else
        innerType

    let value = serializer.Deserialize(reader, innerType)
    let cases = getCases t

    if isNull value then
      FSharpValue.MakeUnion(cases.[0], [||])
    else
      FSharpValue.MakeUnion(cases.[1], [|value|])

/// A converter that seamlessly converts enum-style discriminated unions, that is unions where every case has no data attached to it
type DuConverter() =
    inherit JsonConverter()

    override __.WriteJson(writer, value, serializer) =
        let unionType = value.GetType()
        let unionCases = getCases unionType 
        let case, fields = getUnionFields value

        let allSingle = unionCases |> Seq.forall (fun c -> c.GetFields() |> Seq.length = 1)

        match allSingle,fields with
        //simplies case no parameters - just like an enumeration
        | _,[||] -> writer.WriteRawValue(sprintf "\"%s\"" case.Name)
        //all single values - discriminate between record types - so we just serialize the record
        | true,[| singleValue |] -> serializer.Serialize(writer,singleValue)
        //diferent types in same discriminated union - write the case and the items as tuples
        | false,values ->
            writer.WriteStartObject()
            writer.WritePropertyName "Case"
            writer.WriteRawValue(sprintf "\"%s\"" case.Name)
            let valuesCount = Seq.length values
            for i in 1 .. valuesCount do
                let itemName = sprintf "Item%i" i
                writer.WritePropertyName itemName
                serializer.Serialize(writer,values.[i-1])
            writer.WriteEndObject()
        | _,_ -> failwith "Handle this new case"




    override __.ReadJson(reader, destinationType, existingValue, serializer) =
        let parts =
            if reader.TokenType <> JsonToken.StartObject then [| (JsonToken.Undefined, obj()), (reader.TokenType, reader.Value) |]
            else
                seq {
                    yield! reader |> Seq.unfold (fun reader ->
                                         if reader.Read() then Some((reader.TokenType, reader.Value), reader)
                                         else None)
                }
                |> Seq.takeWhile(fun (token, _) -> token <> JsonToken.EndObject)
                |> Seq.pairwise
                |> Seq.mapi (fun id value -> id, value)
                |> Seq.filter (fun (id, _) -> id % 2 = 0)
                |> Seq.map snd
                |> Seq.toArray

        //get simplified key value collection
        let fieldsValues =
            parts
                |> Seq.map (fun ((_, fieldName), (fieldType,fieldValue)) -> fieldName,fieldType,fieldValue)
                |> Seq.toArray
        //all cases of the targe discriminated union
        let unionCases = getCases destinationType

        //the first simple case - this DU contains just simple values - as enum - get the value
        let _,_,firstFieldValue = fieldsValues.[0]

        let fieldsCount = fieldsValues |> Seq.length

        let valuesOnly = fieldsValues |> Seq.skip 1 |> Seq.map (fun (_,_,v) -> v) |> Array.ofSeq

        let foundDirectCase = unionCases |> Seq.tryFind (fun uc -> uc.Name = (firstFieldValue.ToString()))

        let jsonToValue valueType value =
            match valueType with
                                | JsonToken.Date ->
                                    let dateTimeValue = Convert.ToDateTime(value :> Object)
                                    dateTimeValue.ToString("o")
                                | _ -> value.ToString()

        match foundDirectCase, fieldsCount with
            //simpliest case - just like an enum
            | Some case, 1 -> FSharpValue.MakeUnion(case,[||])
            //case is specified - just create the case with the values as parameters
            | Some case, n -> FSharpValue.MakeUnion(case,valuesOnly)
            //case not specified - look up the record type which suites the best
            | None, _ ->
                //this is the second case - this disc union is not of simple value - it may be records or multiple values
                let reconstructedJson = (Seq.fold (fun acc (name,valueType,value) -> acc + String.Format("\t\"{0}\":\"{1}\",\n",name,(jsonToValue valueType value))) "{\n" fieldsValues) + "}"

                //if it is a record lets try to find the case by looking at the present fields
                let implicitCase = unionCases |> Seq.tryPick (fun uc ->
                    //if the case of the discriminated union is a record then this case will contain just one field which will be the record
                    let ucDef = uc.GetFields() |> Seq.head
                    //we need the get the record type and look at the fields
                    let recordType = ucDef.PropertyType
                    let recordFields = recordType.GetProperties()
                    let matched = fieldsValues |> Seq.forall ( fun (fieldName,_,fieldValue) ->
                        recordFields |> Array.exists(fun f-> f.Name = (fieldName :?> string))
                    )    
                    //if we have found a match onthe record let's keep the union case and type of the record
                    match matched with
                        | true -> Some (uc,recordType)
                        | false -> None
                )

                match implicitCase with
                    | Some (case,recordType) ->
                        use stringReader = new StringReader(reconstructedJson)
                        use jsonReader = new JsonTextReader(stringReader)
                        //creating the record - Json.NET can handle that already
                        let unionCaseValue = serializer.Deserialize(jsonReader,recordType)
                        //convert the record to the parent discrimianted union
                        let parentDUValue = FSharpValue.MakeUnion(case,[|unionCaseValue|])
                        parentDUValue
                    | None -> failwith "can't find such disc union type"

    override __.CanConvert(objectType) =
        isUnion objectType &&
        //it seems that both option and list are implemented using discriminated unions, so we tell json.net to ignore them and use different serializer
        not (objectType.IsGenericType  && objectType.GetGenericTypeDefinition() = typedefof<list<_>>) &&
        not (objectType.IsGenericType  && objectType.GetGenericTypeDefinition() = typedefof<option<_>>) &&
        not (FSharpType.IsRecord objectType)

let jsonSerializationSettings = JsonSerializerSettings()
jsonSerializationSettings.Converters.Add(new OptionConverter ())
jsonSerializationSettings.Converters.Add(new DuConverter ())