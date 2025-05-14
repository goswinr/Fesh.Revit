namespace Fesh.Revit.Bootstrapper

open System
open System.Windows
open Velopack
open System.Windows.Media
open AvalonLog

module Main =

    let mkWindow(log:AvalonLog) =
        let win = Window()
        win.Content <- log
        win.Title <- "Fesh.Revit.Bootstrapper"
        win.Width <- 800.0
        win.Height <- 600.0
        win


    let greet (log:AvalonLog) =
        if Revit.isNetFramework then
            log.printfnBrush Brushes.Blue "Fesh.Revit.Bootstrapper on .NET Framework 4.8"
            log.printfnBrush Brushes.Blue "for Registration and Updating Fesh.Revit with Revit 2024 or earlier versions."
        else
            log.printfnBrush Brushes.Blue "Fesh.Revit.Bootstrapper on .NET 8"
            log.printfnBrush Brushes.Blue "for Registration and Updating Fesh.Revit with Revit 2025 or later versions."


    let velo(log:AvalonLog) =
        let vpk = VelopackApp.Build()

        vpk.SetAutoApplyOnStartup(false)
            |> ignore // to not install updates if they are downloaded



        vpk.OnFirstRun(fun _ ->
            Revit.register log
            ) |> ignore // register the app with Revit via xml file

        vpk.OnBeforeUninstallFastCallback(fun _ ->
            Revit.unregister()
            ) |> ignore // delete xml file

        vpk.OnRestarted( fun a ->
            log.printfnBrush Brushes.DarkGreen $"Fesh.Revit was updated to {a.Major}.{a.Minor}.{a.Patch} !"
            log.printfnBrush Brushes.Black $"\r\n\r\nYou can close this window now."
            ) |> ignore

        vpk.Run() //https://docs.velopack.io/getting-started/csharp


    [< EntryPoint >]
    [< STAThread >]
    let main (_args: string []) : int =
        try
            let log = AvalonLog()
            log.ShowLineNumbers <- false
            Console.SetOut   (log.GetTextWriter(Brushes.DarkGray))
            Console.SetError (log.GetTextWriter(Brushes.Red))
            greet log
            velo log // this might kill the app early !

            let win = mkWindow log
            // win.ContentRendered.Add(fun _ -> velo(log)) // delay till after render ?
            let app  = Application() // do first so that pack Uris work
            app.Run win

        with e ->
            eprintfn $"Fesh.Revit.Bootstrapper Start Up Error:\r\n{e}" // can this ever be seen ?
            1



