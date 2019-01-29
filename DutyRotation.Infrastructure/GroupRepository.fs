module DutyRotation.Infrastructure.GroupRepository

open System
open FSharp.Data.Sql
open DutyRotation.Common
open DutyRotation.CreateGroup.Types
open DutyRotation.AddGroupMember.Types

[<Literal>]
let connectionString = @"Data Source=den1.mssql8.gear.host;Initial Catalog=dutyrotation;Persist Security Info=True;User ID=dutyrotation;Password=Yo845n_L?4DA"

type sql = SqlDataProvider<Common.DatabaseProviderTypes.MSSQLSERVER, connectionString>

FSharp.Data.Sql.Common.QueryEvents.SqlQueryEvent |> Event.add (printfn "Executing SQL: %O")

let saveGroup : SaveGroup =
  fun group ->
    let ctx = sql.GetDataContext()
    let row = ctx.Dbo.Groups.Create()
    row.Id <- group.Id.Value
    row.Name <- group.Settings.Name.Value
    row.DutiesCount <- group.Settings.DutiesCount.Value
    row.RotationCronRule <- group.Settings.RotationCronRule.Value
    group.Settings.RotationStartDate |> Option.iter (fun date -> row.RotationStartDate <- date.Value.UtcDateTime)
    row.CreatedDate <- DateTime.UtcNow   
    ctx.SubmitUpdatesAsync ()

let getGroupMember: GetGroupMembers =
  fun groupId ->
    let ctx = sql.GetDataContext()
    //todo: add check for missing group
    let groupMembers = async {      
      let! rows = Seq.executeQueryAsync (query {
        for groupMember in ctx.Dbo.GroupMembers do
        where (groupMember.GroupId = groupId.Value)
        select (groupMember)
      })
      return rows |> Seq.map (fun row -> {
        Id = GroupMemberId.TryGet row.Id |> Result.value
        Name = GroupMemberName.TryParse row.Name |> Result.value
      })
    }
    groupMembers |> AsyncResult.ofAsync |> AsyncResult.map Seq.toList
    
  
let saveMember: SaveMember =
  fun groupId groupMember ->
    let ctx = sql.GetDataContext()
    let row = ctx.Dbo.GroupMembers.Create()
    row.Id <- groupMember.Id.Value
    row.Name <- groupMember.Name.Value
    row.GroupId <- groupId.Value
    ctx.SubmitUpdatesAsync ()