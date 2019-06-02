namespace DutyRotation.CreateGroup

open DutyRotation.Common
open System

module Contract =  
  [<CLIMutable>]
  type CreateSimpleGroupCommand = {
    GroupName : string
    RotationCronRule: string
    DutiesCount : int
    RotationStartDate : DateTimeOffset option
  }
  
  type CreateSimpleGroup = CreateSimpleGroupCommand -> AsyncResult<GroupId, ValidationError list>

module Types =
  open Contract
  
  type GetGroupSettings = CreateSimpleGroupCommand -> Result<GroupSettings, ValidationError list>
  
  type CreateGroupId = unit -> GroupId
  
  type CreateGroup = GroupSettings -> GroupId -> Group
  
  type SaveGroup = Group -> Async<unit>

module Implementation =
  open Types
  open Contract
  
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

  let private createGroupId : CreateGroupId = GroupId.New

  let private createGroup : CreateGroup =
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