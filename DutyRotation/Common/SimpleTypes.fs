namespace DutyRotation.Common

open System

type GroupId = private | GroupId of Guid
with
  member this.Value = let (GroupId groupId) = this in groupId
  static member TryParse = ConstrainedType.createGuid GroupId
  static member New = Guid.NewGuid() |> GroupId
  
type GroupNotFoundError = {
  GroupId : GroupId
}

type GroupName = private GroupName of string
with
  member this.Value = let (GroupName groupName) = this in groupName
  static member TryParse = ConstrainedType.createDefaultConstrainedString GroupName

type RotationCronRule = private RotationCronRule of string
with
  member this.Value = let (RotationCronRule groupName) = this in groupName
  static member TryParse = ConstrainedType.createDefaultConstrainedString RotationCronRule

type RotationStartDate = RotationStartDate of DateTimeOffset
with
  member this.Value = let (RotationStartDate startDate) = this in startDate

type DutiesCount = private DutiesCount of int
with
  member this.Value = let (DutiesCount dutiesCount) = this in dutiesCount
  static member TryGet(dutiesCount:int) = 
    ConstrainedType.createInt DutiesCount 1 Int32.MaxValue dutiesCount

type GroupMemberId = private GroupMemberId of Guid
with
  member this.Value = let (GroupMemberId id) = this in id
  static member TryGet = ConstrainedType.createGuid GroupMemberId
  static member New = Guid.NewGuid() |> GroupMemberId

type GroupMemberName = private GroupMemberName of string
with
  member this.Value = let (GroupMemberName groupMemberName) = this in groupMemberName
  static member TryParse = ConstrainedType.createDefaultConstrainedString GroupMemberName

type GroupMemberQueuePosition = private GroupMemberQueuePosition of int
with
  member this.Value = let (GroupMemberQueuePosition queuePosition) = this in queuePosition

module GroupMemberQueuePosition =  
  let tryGet = ConstrainedType.createInt GroupMemberQueuePosition 0 Int32.MaxValue
  let first = GroupMemberQueuePosition 0
  let shiftOn n (GroupMemberQueuePosition queuePosition) = queuePosition + n |> min 0 |> GroupMemberQueuePosition
  let nextAfter = shiftOn 1
  
type SlackChannel = private SlackChannel of string

type SlackUserGroup = private SlackUserGroup of string