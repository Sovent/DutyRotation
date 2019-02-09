namespace DutyRotation.Common

type GroupSettings = {
  Name: GroupName
  RotationCronRule: RotationCronRule
  RotationStartDate: RotationStartDate option
  DutiesCount: DutiesCount
}

type Group = {
  Id: GroupId
  Settings: GroupSettings
}

type GroupMember = {
  Id: GroupMemberId
  Name : GroupMemberName
  Position: GroupMemberQueuePosition
}
