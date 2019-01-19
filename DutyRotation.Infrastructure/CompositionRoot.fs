namespace DutyRotation.Infrastructure

module CompositionRoot =
  open DutyRotation.CreateGroup.GroupCreation
       
  let createSimpleGroup = createSimpleGroup GroupRepository.saveGroup