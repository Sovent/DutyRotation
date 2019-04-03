module DutyRotation.Infrastructure.Db

open System.Data
open System.Data.SqlClient
open Daffer

let private connectionString = @"Data Source=den1.mssql8.gear.host;Initial Catalog=dutyrotation;Persist Security Info=True;User ID=dutyrotation;Password=Yo845n_L?4DA"

type Execute<'a> = IDbConnection -> Async<'a>

let execute (exec: Execute<'a>) : Async<'a> =
  addOptionHandlers ()
  let connection = new SqlConnection (connectionString)
  async {
    do! connection.OpenAsync () |> Async.AwaitTask
    let! res = exec connection
    connection.Close()
    return res
  }
  
