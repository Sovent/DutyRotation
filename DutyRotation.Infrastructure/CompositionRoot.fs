namespace DutyRotation.Infrastructure

module CompositionRoot =
  open DutyRotation.CreateGroup.Implementation        
  let createSimpleGroup = createSimpleGroup GroupRepository.saveGroup
  
  open DutyRotation.AddGroupMember.Implementation
  let addGroupMember = addGroupMember GroupRepository.getGroupMember GroupRepository.saveMember