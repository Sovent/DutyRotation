namespace DutyRotation.CreateGroup

open DutyRotation.Common
open System

[<CLIMutable>]
type CreateSimpleGroupCommand = {
  GroupName : string
  RotationLength: TimeSpan
  DutiesCount : int
}

type CreateSimpleGroup = CreateSimpleGroupCommand -> AsyncResult<GroupId, ValidationError list>

type CreateSlackGroupCommand = {
  GroupName : string
  RotationLength: TimeSpan
  DutiesCount : int
  SlackChannel : string
  SlackUserGroup : string
}

type CreateSlackGroup = CreateSlackGroupCommand ->  AsyncResult<GroupId, ValidationError list>

