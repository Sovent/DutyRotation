namespace DutyRotation.CreateGroup

open DutyRotation.Common
open System

[<CLIMutable>]
type CreateSimpleGroupCommand = {
  GroupName : string
  RotationCronRule: string
  DutiesCount : int
  RotationStartDate : DateTimeOffset option
}

type CreateSimpleGroup = CreateSimpleGroupCommand -> AsyncResult<GroupId, ValidationError list>

