namespace Fesh.Revit

open Autodesk.Revit.UI
open Autodesk.Revit.DB
open Autodesk.Revit.Attributes
open System
open System.IO
open System.Windows
open Fesh
open Fesh.Config
open System.Collections.Concurrent
open Velopack
open Velopack.Sources
open Velopack.Locators
open System.Reflection
open Microsoft.Extensions.Logging

module AppName =
    let get(year:string) =
        let year = year.Replace("Revit","").Trim() // just in case
        $"Revit {year}"

module DefaultCode =
    let get(appName:string) =
        $"""#r "C:/Program Files/Autodesk/{appName}/RevitAPI.dll"
#r "C:/Program Files/Autodesk/{appName}/RevitAPIUI.dll"
#r "Fesh.Revit"
open Autodesk.Revit
open Autodesk.Revit.DB
open Autodesk.Revit.UI

// Run your Revit code inside a transaction:
Fesh.Revit.ScriptingSyntax.runApp (fun (app:UIApplication)  ->
    let doc = app.ActiveUIDocument.Document
    // ...
    // ...your code
    // ...
    printfn "Done"
    )"""


// example of mode-less dialog: https://github.com/pierpaolo-canini/Lame-Duck



/// A static class to provide logging and  access to the Fesh Editor
[<AbstractClass; Sealed>]
type App private () =

    static let mutable logFileOnDesktopCount = ref 0

    /// for managing visibility state when showing and hiding the editor window
    static member val internal FeshWasEverShown: bool =  false with get,set

    /// a static reference to the current Fesh Editor
    static member val Fesh : Fesh option =
        None with get,set

    /// creates a log or debug txt file on the desktop
    /// file name includes datetime to be unique
    /// sprintf "%sFesh.Revit.Log-%s.txt" filePrefix time
    static member logToFile filePrefix (content:string) =
       let checkedPrefix = if isNull filePrefix then "NULLPREFIX" else filePrefix
       let checkedContent = if String.IsNullOrWhiteSpace content then "content is String.IsNullOrWhiteSpace" else content
       let time = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff") // ensure unique name
       let filename = sprintf "%sFesh.Revit.Log-%s.txt" checkedPrefix time
       async {
           try
               let file = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),filename)
               IO.File.WriteAllText(file, checkedContent)
           with _ ->
               ()
        } |> Async.Start

    /// logs text to Fesh editor window in red
    /// if Fesh is null it writes a text file to desktop instead and shows a Task Dialog.
    static member alert msg =
       Printf.kprintf (fun s ->
           match App.Fesh with
           |Some fesh when fesh.Window.IsLoaded ->
                try
                    fesh.Log.PrintnColor 180 100 10 s
                with e -> // in case the logging fails
                    incr logFileOnDesktopCount
                    let printE = sprintf "\r\nLog.PrintnColor error:\r\n%A" e
                    App.logToFile "App.alertFailed-" (s+printE)
                    TaskDialog.Show("Fesh AddIn App.alertFailed", s+printE) |> ignore

           | _ when logFileOnDesktopCount.Value < 10 ->
                incr logFileOnDesktopCount
                App.logToFile "App.alert-" s
                TaskDialog.Show("Fesh AddIn App.alert", s) |> ignore

           | _ -> () // do nothing , we already have 10 log files on the desktop
           ) msg


    /// logs text to Fesh editor window in green
    /// does nothing if Fesh is null
    static member log msg =
       Printf.kprintf (fun s ->
           match App.Fesh with
           |None -> ()
           |Some fesh ->  fesh.Log.PrintnColor 50 100 10 s
           ) msg


[<Transaction(TransactionMode.Manual)>]
type internal FsiRunEventHandler (fesh:Fesh, queue: ConcurrentQueue< UIApplication->unit >) =
    member this.GetName() = "Run in Fesh"
    member this.Execute(app:UIApplication) =
        let f = ref Unchecked.defaultof<UIApplication->unit>
        while queue.TryDequeue(f) do //using a queue is only needed if a single script calls into a transaction more than once
            try
                f.Value(app)
            with e ->
                fesh.Log.PrintfnFsiErrorMsg "Error caught in FsiRunEventHandler(a IExternalEventHandler) in this.Execute(app:UIApplication):\r\n%A" e

    interface IExternalEventHandler with
        member this.GetName() = this.GetName()
        member this.Execute(app:UIApplication)  = this.Execute(app)


[<Regeneration(RegenerationOption.Manual)>]
[<Transaction(TransactionMode.Manual)>]
[<AllowNullLiteralAttribute>]
type FeshAddin()= // : IExternalApplication = // don't rename ! This is referenced via string in Fesh.addin file

    member val RequestQueue = ConcurrentQueue<UIApplication->unit>()

    member val ExternalEv: ExternalEvent option = None with get, set

    static member val Instance = null with get,set


    /// Runs a F# function via the IExternalEventHandler pattern for mode-less dialogs
    /// This is the only way to run code from mode-less dialogs such as Fesh editor
    member this.RunOnApp (f:UIApplication -> unit) =
        this.RequestQueue.Enqueue(f)
        match this.ExternalEv with
        |None ->
            App.alert "ExternalEvent not set up yet"
        | Some exEvent ->
            match exEvent.Raise() with
            |ExternalEventRequest.Accepted -> ()
            |ExternalEventRequest.Denied   -> App.alert "exEvent.Raise() returned ExternalEventRequest.Denied"
            |ExternalEventRequest.Pending  -> App.alert "exEvent.Raise() returned ExternalEventRequest.Pending"
            |ExternalEventRequest.TimedOut -> App.alert "exEvent.Raise() returned ExternalEventRequest.TimedOut"
            |x -> App.alert "exEvent.Raise() returned unknown ExternalEventRequest: %A" x

    /// runs a F# function via the IExternalEventHandler pattern for mode-less dialogs
    /// this is the only way to run code from mode-less dialogs such as Fesh editor
    member this.RunOnDoc (f:Document->unit) =
        this.RunOnApp (fun app -> f app.ActiveUIDocument.Document)


    member this.OnStartup(uiConApp:UIControlledApplication) =
        try
            //ResolveFSharpCore.setup()   // needed! (at least in Revit 2023)
            FeshAddin.Instance <- this

            // ------------------- create Ribbon and button -------------------------------------------
            let thisAssemblyPath = Reflection.Assembly.GetExecutingAssembly().Location
            let button = new PushButtonData("Fesh", "Open Fsharp Editor", thisAssemblyPath, "Fesh.Revit.StartEditorCommand")
            button.ToolTip <- "This will open the Fesh F# Script Editor Window"

            let uriImage32 = new Uri("pack://application:,,,/Fesh.Revit;component/Media32/logo32.png") // build from VS not via "dotnet build"  to include. <Resource Include="Media\LogoCursorTr32.png" />
            let uriImage16 = new Uri("pack://application:,,,/Fesh.Revit;component/Media32/logo16.png")
            button.LargeImage <- Media.Imaging.BitmapImage(uriImage32)//for ribbon in tab
            button.Image      <- Media.Imaging.BitmapImage(uriImage16)//for quick access toolbar

            let tabId = "Fesh"
            uiConApp.CreateRibbonTab(tabId)
            let panel = uiConApp.CreateRibbonPanel(tabId,"Fesh")
            panel.AddItem(button) |> ignore

            Result.Succeeded

        with ex ->
            App.alert "OnStartup of Fesh.Revit.dll:\r\n%A" ex
            Result.Failed



    member this.OnShutdown(_app:UIControlledApplication) =
        // https://forums.autodesk.com/t5/revit-api-forum/how-stop-or-cancel-revit-closing/td-p/5983643
        // Your add-in OnShutdown method should be called when and only when Revit is closing.
        // That will at least give you a chance to display the message to the user and "force her to press a button",
        // if you really think that is a good idea, even if it does not enable you to prevent Revit from closing.
        match App.Fesh with
        |None -> ()
        |Some fesh ->
            fesh.Tabs.AskForFileSavingToKnowIfClosingWindowIsOk()  |> ignore // this will try to save files too. ignore result since it not possible to prevent Revit from closing eventually
            fesh.Fsi.ShutDown()
            fesh.Window.Close()

        Result.Succeeded



    interface IExternalApplication with
        member this.OnStartup(uiConApp:UIControlledApplication) = this.OnStartup(uiConApp)
        member this.OnShutdown(app:UIControlledApplication)     = this.OnShutdown(app)

        //member this.Queue = queue

        //member this.ExternalEvent  with get() = exEvent   and set(e) = exEvent <- e



[<Regeneration(RegenerationOption.Manual)>]
[<Transaction(TransactionMode.Manual)>]
type StartEditorCommand() = // don't rename ! string referenced in  OnStartup -> PushButtonData //new instance is created on every button click

    member this.Execute(commandData: ExternalCommandData, message: byref<string>, elements: ElementSet): Result =
        // let assemblyNames = // for debugging loaded assemblies
        //     AppDomain.CurrentDomain.GetAssemblies()
        //     |> Array.map (fun asm ->
        //         let loc = if asm.IsDynamic then "*Dynamic*" else asm.Location
        //         $"{asm.FullName} , {loc}" )
        //     |> Array.sort
        //     |> String.concat Environment.NewLine
        // let desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        // let filePath = Path.Combine(desktopPath, "Revit 2024 LoadedAssemblies-2.csv")
        // File.WriteAllText(filePath, assemblyNames)

        try
            let fesh =
                match App.Fesh with
                |None ->

                    //-------------- Start Fesh -------------------------------------------------------------
                    // originally this was done in the OnStartup event but some how there was a problem getting a synchronization context.
                    // so we do it here on the first button click
                    //https://thebuildingcoder.typepad.com/blog/2018/11/revit-window-handle-and-parenting-an-add-in-form.html
                    let winHandle = Diagnostics.Process.GetCurrentProcess().MainWindowHandle
                    let canRun = fun () ->
                        // this is a workaround to check if a transaction is open in the document.
                        if commandData.Application.ActiveUIDocument.Document.IsModifiable then
                            eprintfn "Document.IsModifiable is true, there is an active transaction open in the document. This is not allowed for Fesh to run. Please close the transaction first."
                            false
                        else
                            true
                        // If Document.IsModifiable returns TRUE, then there is an active transaction open in that document.
                        // https://thebuildingcoder.typepad.com/blog/2015/06/archsample-active-transaction-and-adnrme-for-revit-mep-2016.html#3

                    let appName = AppName.get(commandData.Application.Application.VersionNumber)
                    let logo = new Uri("pack://application:,,,/Fesh.Revit;component/Media32/logo.ico")
                    let hostData = {
                        hostName = appName
                        mainWindowHandel = winHandle
                        fsiCanRun =  canRun
                        logo = Some logo
                        defaultCode = Some (DefaultCode.get(appName))
                        hostAssembly = Some (Reflection.Assembly.GetAssembly(typeof<FeshAddin>))
                        }

                    let feshApp = Fesh.App.createEditorForHosting(hostData)
                    App.Fesh <- Some feshApp

                    //TODO make a C# plugin that loads Fesh.addin once uiConApp.ControlledApplication.ApplicationInitialized to avoid missing method exceptions in FSI
                    feshApp.Fsi.OnRuntimeError.Add (fun e ->
                        match e with
                        | :? MissingMethodException ->
                            feshApp.Log.PrintfnFsiErrorMsg "*** To avoid this MissingMethodException exception try restarting Revit without a document.\r\n*** Then from within Revit open your desired project.\r\n*** If the error persist please report it!"
                        | _ -> ()
                        )

                    // just keep everything alive:
                    feshApp.Window.Closing.Add (fun e ->
                        if not e.Cancel then // closing might be already cancelled in Fesh.fs in main Fesh lib.
                            // even if closing is not canceled, don't close, just hide window
                            feshApp.Window.Visibility <- Windows.Visibility.Hidden
                            e.Cancel <- true
                            )


                    feshApp.Window.Loaded.Add (fun _ ->
                        Velo.checkForNewVelopackRelease(feshApp)
                        Velo.updateOnRevitClose(commandData.Application, App.alert "%s")
                        )


                    //-------------- Hook up Fesh -------------------------------------------------------------
                    if isNull FeshAddin.Instance then
                        App.alert "%s" "FeshAddin.Instance not set up yet"
                    else
                        FeshAddin.Instance.ExternalEv <- Some <| ExternalEvent.Create(FsiRunEventHandler(feshApp, FeshAddin.Instance.RequestQueue))

                    feshApp

                |Some s ->
                    s

                    (* //TODO Alt enter does not work !?!
                    Fesh.Window.KeyDown.Add(fun e -> //to avoid pressing alt to focus on menu and the disabling Alt+Enter for Evaluating selection in FSI
                        fesh.Log.PrintDebugMsg "key: %A, system key: %A, mod: %A " e.Key e.SystemKey Keyboard.Modifiers
                        //if e.Key = Key.LeftAlt || e.Key = Key.RightAlt then
                        //    e.Handled <- true
                        //elif (Keyboard.Modifiers = ModifierKeys.Alt && e.Key = Key.Enter) ||
                        //   (Keyboard.Modifiers = ModifierKeys.Alt && e.Key = Key.Return) then
                        //        Fesh.Fsi.Evaluate{code = Fesh.Tabs.CurrAvaEdit.SelectedText ; file=Fesh.Tabs.Current.FilePath; allOfFile=false}
                        //        e.Handled <- true
                        )
                    Fesh.Tabs.Control.PreviewKeyDown.Add (fun e ->
                        if Keyboard.Modifiers = ModifierKeys.Alt && Keyboard.IsKeyDown(Key.Enter) then
                            fesh.Log.PrintDebugMsg "Alt+Enter"
                        elif Keyboard.Modifiers = ModifierKeys.Alt && Keyboard.IsKeyDown(Key.Return) then
                            fesh.Log.PrintDebugMsg "Alt+Return"
                        else
                            fesh.Log.PrintDebugMsg "not Alt+Enter"
                            )
                    *)

            if not App.FeshWasEverShown then
                fesh.Window.Show()
                App.FeshWasEverShown <- true
                Result.Succeeded
            else
                fesh.Window.Visibility <- Visibility.Visible
                Result.Succeeded

        with ex ->
            TaskDialog.Show("Execute Button Fesh", sprintf "StartEditorCommand: message %s\r\ncommandData:%A\r\nelements:%A\r\nException:%A" message commandData elements ex ) |> ignore
            Result.Failed


    interface IExternalCommand with
        member this.Execute(commandData: ExternalCommandData, message: byref<string>, elements: ElementSet): Result =
            this.Execute(commandData, &message, elements)




   (* let uiApp =
        let versionNumber = int uiConApp.ControlledApplication.VersionNumber
        let fieldName = if versionNumber >= 2017 then  "m_uiapplication" else "m_application"
        let fi = uiConApp.GetType().GetField(fieldName, BindingFlags.NonPublic ||| BindingFlags.Instance)
        fi.GetValue(uiConApp) :?> UIApplication



    let checkForNewRelease(fesh:Fesh) =
        async {
            try
                use client = new HttpClient()
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Fesh")
                let! response = client.GetStringAsync("https://api.github.com/repos/goswinr/Fesh.Revit/releases/latest") |> Async.AwaitTask
                let v = response |> Fesh.Util.Str.between "\"tag_name\":\"" "\""
                //let json = JObject.Parse(response)
                //return json.["tag_name"].ToString()
                do! Async.SwitchToContext Fittings.SyncWpf.context
                match v with
                | None -> fesh.Log.PrintfnInfoMsg "Could not check for updates on https://github.com/goswinr/Fesh.Revit/releases. \r\nAre you offline?"
                | Some v ->
                    let cv = Reflection.Assembly.GetAssembly(typeof<FeshAddin>).GetName().Version.ToString()
                    let cv = if cv.EndsWith(".0") then cv[..^2] else cv
                    if v <> cv then
                        fesh.Log.PrintfnAppErrorMsg $"A newer version of Fesh.Revit is available: {v} , you are using {cv} \r\nPlease visit https://github.com/goswinr/Fesh.Revit/releases"
                    else
                        fesh.Log.PrintfnInfoMsg $"You are using the latest version of Fesh.Revit: {cv}"
            with e ->
                fesh.Log.PrintfnInfoMsg "Could not check for updates on https://github.com/goswinr/Fesh.Revit/releases. \r\nAre you offline?"
                fesh.Log.PrintfnInfoMsg "The Error was: {e}"
        }
        |> Async.Start
        ()
    *)

// module ResolveFSharpCore =
//     open System.Reflection
//     open System.Globalization
// adapted from https://stackoverflow.com/questions/245825/what-does-initializecomponent-do-and-how-does-it-work-in-wpf
// let tarVer = Assembly.GetAssembly([].GetType()).GetName().Version
//let setup() =
//    // to fix  Could not load file or assembly 'FSharp.Core, Version=4.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
//    let handler = ResolveEventHandler (fun sender args ->
//        //gets the name of the assembly being requested by the plugin
//        let requestedAssembly = new AssemblyName(args.Name)
//        //if it is not the assembly we are trying to redirect, return null
//        if requestedAssembly.Name <> "FSharp.Core" then
//            null
//        else
//            //if it IS the assembly we are trying to redirect, change it's version and public key token information
//            requestedAssembly.Version <- tarVer
//            //requestedAssembly.SetPublicKeyToken(new AssemblyName("x, PublicKeyToken=" + pubTok).GetPublicKeyToken())
//            //requestedAssembly.CultureInfo <- CultureInfo.InvariantCulture
//            //finally, load the assembly
//            Assembly.Load(requestedAssembly)
//        )
//    AppDomain.CurrentDomain.add_AssemblyResolve handler