namespace DutyRotation.Common

open System

type GroupId = private | GroupId of string
with
  member this.Value = let (GroupId groupId) = this in groupId
  static member TryParse = ConstrainedType.createDefaultConstrainedString GroupId
  static member New = Guid.NewGuid().ToString() |> GroupId

type GroupName = private GroupName of string
with
  member this.Value = let (GroupName groupName) = this in groupName
  static member TryParse = ConstrainedType.createDefaultConstrainedString GroupName

type RotationLength = private RotationLength of TimeSpan
with
  member this.Value = let (RotationLength rotationLength) = this in rotationLength
  static member TryGet(rotationLength:TimeSpan) =
    if rotationLength > TimeSpan.Zero then RotationLength rotationLength |> Ok
    else ValidationError.create "Rotation length should be positive" rotationLength

type DutiesCount = private DutiesCount of int
with
  member this.Value = let (DutiesCount dutiesCount) = this in dutiesCount
  static member TryGet(dutiesCount:int) = 
    ConstrainedType.createInt DutiesCount 1 Int32.MaxValue dutiesCount

type SlackChannel = private SlackChannel of string

type SlackUserGroup = private SlackUserGroup of string