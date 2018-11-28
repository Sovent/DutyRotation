namespace DutyRotation.CreateGroup

open DutyRotation.Common

type CreateGroupCommand =
  | CreateSimpleGroupCommand of CreateSimpleGroupCommand
  | CreateSlackGroupCommand of CreateSlackGroupCommand

type GetGroupSettings = CreateGroupCommand -> Result<GroupSettings, ValidationError list>

type CreateGroupId = unit -> GroupId

type CreateGroup = GroupSettings -> GroupId -> Group

type SaveGroup = Group -> Async<unit>


