namespace DutyRotation.Common

type GroupSettings = {
  Name: GroupName
  RotationLength: RotationLength
  DutiesCount: DutiesCount
}

type Group = {
  Id: GroupId
  Settings: GroupSettings
}
