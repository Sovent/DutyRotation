namespace DutyRotation.CreateGroup

open DutyRotation.Common

type GetGroupSettings = CreateSimpleGroupCommand -> Result<GroupSettings, ValidationError list>

type CreateGroupId = unit -> GroupId

type CreateGroup = GroupSettings -> GroupId -> Group

type SaveGroup = Group -> Async<unit>


