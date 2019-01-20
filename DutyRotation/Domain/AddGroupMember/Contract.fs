namespace DutyRotation.AddGroupMember

open DutyRotation.Common
open System

[<CLIMutable>]
type AddGroupMember = {
  GroupId: Guid
  MemberName: string option
}

type CreateSimpleGroup = AddGroupMember -> AsyncResult<GroupMemberId, ValidationError list>

