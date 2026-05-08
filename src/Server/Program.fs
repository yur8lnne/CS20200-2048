namespace Server

open System
open System.IO
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Giraffe
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Shared

module Program =
    let private databasePath () =
        match Environment.GetEnvironmentVariable("DB_PATH") with
        | null
        | "" -> "data/leaderboard.sqlite"
        | value -> value

    let private remotingHandler store =
        Remoting.createApi ()
        |> Remoting.withRouteBuilder Route.builder
        |> Remoting.fromValue (Api.createGameApi store)
        |> Remoting.buildHttpHandler

    let private indexHandler : HttpHandler =
        fun next ctx ->
            let environment = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>()
            let indexPath = Path.Combine(environment.WebRootPath, "index.html")

            if File.Exists indexPath then
                htmlFile indexPath next ctx
            else
                text "Client assets have not been built. Run npm run dev:client or npm run build:client." next ctx

    let private webApp store : HttpHandler =
        choose
            [ remotingHandler store
              GET
              >=> choose
                      [ route "/health" >=> text "ok"
                        route "/" >=> indexHandler
                        routeCi "/index.html" >=> indexHandler ]
              setStatusCode 404 >=> text "Not found" ]

    [<EntryPoint>]
    let main args =
        let builder = WebApplication.CreateBuilder(args)
        builder.Services.AddGiraffe() |> ignore

        let store = LeaderboardStore(databasePath ())
        store.Initialize()

        let app = builder.Build()

        if app.Environment.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore

        app.UseDefaultFiles() |> ignore
        app.UseStaticFiles() |> ignore
        app.UseGiraffe(webApp store)
        app.Run()
        0
