namespace DutyRotation.CreateGroup

open DutyRotation.Common

module GroupCreation =
  let getGroupSettings : GetGroupSettings =
    fun command ->
      let createGroupSettings groupName rotationCronRule dutiesCount rotationStartDate = {
          Name = groupName
          RotationCronRule = rotationCronRule
          DutiesCount = dutiesCount
          RotationStartDate = rotationStartDate
      }
      let (<!>) = Result.map
      let (<*>) = Result.apply
      let groupName = GroupName.TryParse command.GroupName
      let rotationCronRule = RotationCronRule.TryParse command.RotationCronRule
      let rotationStartDate = command.RotationStartDate |> Option.map RotationStartDate |> Ok
      let dutiesCount = DutiesCount.TryGet command.DutiesCount
      createGroupSettings <!> groupName <*> rotationCronRule <*> dutiesCount <*> rotationStartDate

  let createGroupId : CreateGroupId = fun () -> GroupId.New

  let createGroup : CreateGroup =
    fun settings id ->
      {
        Id = id
        Settings = settings
      }

  let createSimpleGroup (saveGroup : SaveGroup) : CreateSimpleGroup =
    fun command ->
      asyncResult {
        let! groupSettings = command |> getGroupSettings |> AsyncResult.ofResult
        let groupId = createGroupId ()
        let group = createGroup groupSettings groupId
        let! saveGroup = saveGroup group |> AsyncResult.ofAsync
        return groupId
      }
