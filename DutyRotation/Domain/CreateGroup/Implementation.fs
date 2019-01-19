namespace DutyRotation.CreateGroup

open DutyRotation.Common

module GroupCreation =
  let getGroupSettings : GetGroupSettings =
    fun command ->
      let (groupName, rotationLength, dutiesCount) = match command with
        | CreateSimpleGroupCommand comm -> (comm.GroupName, comm.RotationLength, comm.DutiesCount) 
        | CreateSlackGroupCommand comm -> (comm.GroupName, comm.RotationLength, comm.DutiesCount)

      result {
        let! groupName = GroupName.TryParse groupName |> Result.toSingleErrorList
        let! rotationLength = RotationLength.TryGet rotationLength |> Result.toSingleErrorList
        let! dutiesCount = DutiesCount.TryGet dutiesCount |> Result.toSingleErrorList
        return {
          Name = groupName
          RotationLength = rotationLength
          DutiesCount = dutiesCount
        }
      }

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
        let! groupSettings = CreateSimpleGroupCommand command |> getGroupSettings |> AsyncResult.ofResult
        let groupId = createGroupId ()
        let group = createGroup groupSettings groupId
        let! saveGroup = saveGroup group |> AsyncResult.ofAsync
        return groupId
      }
