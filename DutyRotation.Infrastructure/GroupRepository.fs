module DutyRotation.Infrastructure.GroupRepository

open System
open FSharp.Data.Sql
open DutyRotation.CreateGroup

[<Literal>]
let connectionString = @"Data Source=den1.mssql8.gear.host;Initial Catalog=dutyrotation;Persist Security Info=True;User ID=dutyrotation;Password=Yo845n_L?4DA"

type sql = SqlDataProvider<Common.DatabaseProviderTypes.MSSQLSERVER, connectionString>

let saveGroup : SaveGroup =
  fun group ->
    let ctx = sql.GetDataContext()
    let row = ctx.Dbo.Groups.Create()
    row.Id <- Guid.Parse group.Id.Value
    row.Name <- group.Settings.Name.Value
    row.DutiesCount <- group.Settings.DutiesCount.Value
    row.RotationCronRule <- group.Settings.RotationCronRule.Value
    group.Settings.RotationStartDate |> Option.iter (fun date -> row.RotationStartDate <- date.Value.UtcDateTime)
    row.CreatedDate <- DateTime.UtcNow   
    ctx.SubmitUpdatesAsync ()
