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
    ctx.Dbo.Groups.Create(
       group.Settings.DutiesCount.Value, 
       Guid.Parse group.Id.Value, 
       group.Settings.Name.Value,
       group.Settings.RotationLength.Value.Ticks
    ) |> ignore
    ctx.SubmitUpdatesAsync ()
