namespace DutyRotation.Infrastructure

module CompositionRoot =
  open DutyRotation.CreateGroup.Implementation
  open DutyRotation.CreateGroup.Contract
  let createSimpleGroup : CreateSimpleGroup =
    fun command ->
      Db.execute <| fun conn ->
        let saveGroup = GroupRepository.saveGroup conn
        createSimpleGroup saveGroup command
  
  open DutyRotation.AddGroupMember.Implementation
  open DutyRotation.AddGroupMember.Contract
  let addGroupMember : AddGroupMember  =
    fun command ->
      Db.execute <| fun conn ->
        let getGroupMembers = GroupRepository.getGroupMembers conn
        let saveMember = GroupRepository.saveMember conn
        addGroupMember getGroupMembers saveMember command
  
  open DutyRotation.RotateDuties.Implementation
  open DutyRotation.RotateDuties.Contract
  let rotateDuties : RotateDuties =
    fun command ->
      Db.execute <| fun conn ->
        let getGroupDutiesCount = GroupRepository.getGroupDutiesCount conn
        let getGroupMembersForRotation = GroupRepository.getGroupMembersForRotation conn
        let saveMembers = GroupRepository.saveMembers conn
        rotateDuties getGroupDutiesCount getGroupMembersForRotation saveMembers command