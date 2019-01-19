namespace DutyRotation.Infrastructure

module CompositionRoot =
  open DutyRotation.CreateGroup.GroupCreation
       
  let createSimpleGroup = createSimpleGroup (fun group -> printf "Group is used to be saved" |> Async.retn)