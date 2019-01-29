namespace DutyRotation.Api

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Hosting
open DutyRotation.Infrastructure.JsonSerializers
open Giraffe
open Giraffe.Serialization
open GroupsController

module Program =
    open System

    let exitCode = 0
    
    let webApp =
      choose [
        route "/ping"   >=> text "pong"
        POST >=> choose [
          routeCif "/groups/%O/members" addGroupMember
          route "/groups" >=> createSimpleGroup
        ]
      ]

    let configureApp (app : IApplicationBuilder) =
      app.UseGiraffe webApp

    let configureServices (services : IServiceCollection) =
      services.AddGiraffe() |> ignore
      
      services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer(jsonSerializationSettings)) |> ignore
        
    let CreateWebHostBuilder args =
        WebHostBuilder()
          .UseKestrel()
          .Configure(Action<IApplicationBuilder> configureApp)
          .ConfigureServices(configureServices)

    [<EntryPoint>]
    let main args =
        CreateWebHostBuilder(args).Build().Run()

        exitCode
