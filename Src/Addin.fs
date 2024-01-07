namespace Seff.Revit

open Autodesk.Revit.UI
open Autodesk.Revit.DB
open Autodesk.Revit.Attributes
open System
open System.Windows
open Seff
open Seff.Config
open System.Collections.Concurrent


module Version = 
    let name = 

#if REVIT2019
        "Revit 2019"
#else
    #if REVIT2021
        "Revit 2021"
    #else
        #if REVIT2023
            "Revit 2023"
        #else
            "Revit"
        #endif
    #endif
#endif


module ResolveFSharpCore = 
    open System.Reflection
    open System.Globalization
    
    // adapted from https://stackoverflow.com/questions/245825/what-does-initializecomponent-do-and-how-does-it-work-in-wpf
    
    //let reqVer = new Version("4.5.0.0") //because of Fittings ??
    //let pubTok = "b03f5f7f11d50a3a" // PublicKeyToken
    let tarVer = Reflection.Assembly.GetAssembly([].GetType()).GetName().Version //new Version("7.0.0.0"); //  Reflection.Assembly.GetAssembly([].GetType()).GetName().Version

    let setup() =   
        // to fix  Could not load file or assembly 'FSharp.Core, Version=4.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
        
        let handler = ResolveEventHandler (fun sender args ->
            //gets the name of the assembly being requested by the plugin
            let requestedAssembly = new AssemblyName(args.Name)

            //if it is not the assembly we are trying to redirect, return null
            if requestedAssembly.Name <> "FSharp.Core" then 
                null
            else
                //if it IS the assembly we are trying to redirect, change it's version and public key token information
                requestedAssembly.Version <- tarVer
                //requestedAssembly.SetPublicKeyToken(new AssemblyName("x, PublicKeyToken=" + pubTok).GetPublicKeyToken())
                //requestedAssembly.CultureInfo <- CultureInfo.InvariantCulture

                //finally, load the assembly
                Assembly.Load(requestedAssembly)
            )
        AppDomain.CurrentDomain.add_AssemblyResolve handler


// example of mode-less dialog: https://github.com/pierpaolo-canini/Lame-Duck

/// A static class to provide logging and  access to the Seff Editor
[<AbstractClass; Sealed>]
type App private () = 

    static let mutable logFileOnDesktopCount = ref 0

    /// for managing visibility state when showing and hiding the editor window
    static member val internal seffWasEverShown: bool =  false with get,set

    /// a static reference to the current Seff Editor
    static member val Seff : Seff option = None with get,set      

    /// creates a log or debug txt file on the desktop
    /// file name includes datetime to be unique
    /// sprintf "%sSeff.Revit.Log-%s.txt" filePrefix time
    static member logToFile filePrefix (content:string) = 
       let checkedPrefix = if isNull filePrefix then "NULLPREFIX" else filePrefix
       let checkedContent = if String.IsNullOrWhiteSpace content then "content is String.IsNullOrWhiteSpace" else content
       let time = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff") // ensure unique name
       let filename = sprintf "%sSeff.Revit.Log-%s.txt" checkedPrefix time
       async {
           try
               let file = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),filename)
               IO.File.WriteAllText(file, checkedContent)
           with _ -> 
               () 
        } |> Async.Start

    /// logs text to Seff editor window in red
    /// if Seff is null it writes a text file to desktop instead and shows a Task Dialog.
    static member alert msg = 
       Printf.kprintf (fun s ->
           match App.Seff with 
           |Some seff when seff.Window.IsLoaded -> 
                try
                    seff.Log.PrintnColor 180 100 10 s
                with e -> // in case the logging fails
                    incr logFileOnDesktopCount
                    let printE = sprintf "\r\nLog.PrintnColor error:\r\n%A" e
                    App.logToFile "App.alertFailed-" (s+printE)
                    TaskDialog.Show("Seff AddIn App.alertFailed", s+printE) |> ignore
           
           | _ when logFileOnDesktopCount.Value < 10 ->            
                incr logFileOnDesktopCount
                App.logToFile "App.alert-" s
                TaskDialog.Show("Seff AddIn App.alert", s) |> ignore

           | _ -> () // do nothing , we already have 10 log files on the desktop
           ) msg


    /// logs text to Seff editor window in green
    /// does nothing if Seff is null
    static member log msg = 
       Printf.kprintf (fun s ->
           match App.Seff with 
           |None -> () 
           |Some seff ->  seff.Log.PrintnColor 50 100 10 s
           ) msg
           


[<Transaction(TransactionMode.Manual)>]
type internal FsiRunEventHandler (seff:Seff, queue: ConcurrentQueue< UIApplication->unit >) = 
    member this.GetName() = "Run in Seff"
    member this.Execute(app:UIApplication) = 
        let f = ref Unchecked.defaultof<UIApplication->unit>
        while queue.TryDequeue(f) do //using a queue is only needed if a single script calls into a transaction more than once
            try
                f.Value(app)
            with e ->
                seff.Log.PrintfnFsiErrorMsg "Error caught in FsiRunEventHandler(a IExternalEventHandler) in  this.Execute(app:UIApplication): %A" e

    interface IExternalEventHandler with
        member this.GetName() = this.GetName()
        member this.Execute(app:UIApplication)  = this.Execute(app)


[<Regeneration(RegenerationOption.Manual)>]
[<Transaction(TransactionMode.Manual)>]
[<AllowNullLiteralAttribute>]
type SeffAddin()= // : IExternalApplication = // don't rename ! This is referenced via string in seff.addin file

    member val RequestQueue = ConcurrentQueue<UIApplication->unit>()

    member val ExternalEv: ExternalEvent option = None with get, set
    
    static member val Instance = null with get,set

    /// Runs a F# function via the IExternalEventHandler pattern for mode-less dialogs
    /// This is the only way to run code from mode-less dialogs such as Seff editor
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
    /// this is the only way to run code from mode-less dialogs such as Seff editor
    member this.RunOnDoc (f:Document->unit) = 
        this.RunOnApp (fun app -> f app.ActiveUIDocument.Document)


    member this.OnStartup(uiConApp:UIControlledApplication) = 
        try
            ResolveFSharpCore.setup()   // needed! (at least in Revit 2023)
            SeffAddin.Instance <- this

            // ------------------- create Ribbon and button -------------------------------------------
            let thisAssemblyPath = Reflection.Assembly.GetExecutingAssembly().Location
            let button = new PushButtonData("Seff", "Open Fsharp Editor", thisAssemblyPath, "Seff.Revit.StartEditorCommand")
            button.ToolTip <- "This will open the Seff F# Script Editor Window"

            let uriImage32 = new Uri("pack://application:,,,/Seff.Revit;component/Media/logo32.png") // build from VS not via "dotnet build"  to include. <Resource Include="Media\LogoCursorTr32.png" />
            let uriImage16 = new Uri("pack://application:,,,/Seff.Revit;component/Media/logo16.png")
            button.LargeImage <- Media.Imaging.BitmapImage(uriImage32)//for ribbon in tab
            button.Image      <- Media.Imaging.BitmapImage(uriImage16)//for quick access toolbar

            let tabId = "Seff"
            uiConApp.CreateRibbonTab(tabId)
            let panel = uiConApp.CreateRibbonPanel(tabId,"Seff")
            panel.AddItem(button) |> ignore

            Result.Succeeded

        with ex ->
            App.alert "OnStartup of Seff.Revit.dll:\r\n%A" ex
            Result.Failed

    

    member this.OnShutdown(app:UIControlledApplication) = 
        // https://forums.autodesk.com/t5/revit-api-forum/how-stop-or-cancel-revit-closing/td-p/5983643
        //Your add-in OnShutdown method should be called when and only when Revit is closing.
        //That will at least give you a chance to display the message to the user and "force her to press a button",
        //if you really think that is a good idea, even if it does not enable you to prevent Revit from closing.
        match App.Seff with 
        |None -> () 
        |Some seff -> seff.Tabs.AskForFileSavingToKnowIfClosingWindowIsOk()  |> ignore // this will try to save files too. ignore result since it not possible to prevent Revit from closing eventually
        Result.Succeeded
        //Result.Cancelled //TODO use this to dispose resources correctly ?

    interface IExternalApplication with
        member this.OnStartup(uiConApp:UIControlledApplication) = this.OnStartup(uiConApp)
        member this.OnShutdown(app:UIControlledApplication)     = this.OnShutdown(app)

        //member this.Queue = queue

        //member this.ExternalEvent  with get() = exEvent   and set(e) = exEvent <- e



[<Regeneration(RegenerationOption.Manual)>]
[<Transaction(TransactionMode.Manual)>]
type StartEditorCommand() = // don't rename ! string referenced in  PushButtonData //new instance is created on every button click
    member this.Execute(commandData: ExternalCommandData, message: byref<string>, elements: ElementSet): Result = 
        let seff =
            match App.Seff with 
            |None -> 
                
                //-------------- start Seff -------------------------------------------------------------
                // originally this was done in the OnStartup event but some how there was a problem getting a synchronisation context.
                // so we do it here on the first button click
                //https://thebuildingcoder.typepad.com/blog/2018/11/revit-window-handle-and-parenting-an-add-in-form.html
                let winHandle = Diagnostics.Process.GetCurrentProcess().MainWindowHandle
                let canRun = fun () ->  true // TODO check if in command, or enqueued anyway ?  !!
                let logo = new Uri("pack://application:,,,/Seff.Revit;component/Media/logo.ico")
                let hostData = {
                    hostName = Version.name
                    mainWindowHandel = winHandle
                    fsiCanRun =  canRun
                    logo = Some logo
                    }
                
                (*  This is needed since FCS 34. it solves https://github.com/dotnet/fsharp/issues/9064
                FCS takes the current Directory which might be the one of the hosting App and will then probably not contain FSharp.Core.
                at https://github.com/dotnet/fsharp/blob/7b46dad60df8da830dcc398c0d4a66f6cdf75cb1/src/Compiler/Interactive/fsi.fs#L3213   *)
                //let prevDir = Environment.CurrentDirectory
                //IO.Directory.SetCurrentDirectory(IO.Path.GetDirectoryName(Reflection.Assembly.GetAssembly([].GetType()).Location))              
                let sff = Seff.App.createEditorForHosting(hostData)
                //IO.Directory.SetCurrentDirectory(prevDir)
                App.Seff <- Some sff

                //TODO make a C# plugin that loads Seff.addin once uiConApp.ControlledApplication.ApplicationInitialized to avoid missing method exceptions in FSI
                sff.Fsi.OnRuntimeError.Add (fun e ->
                    match e with
                    | :? MissingMethodException -> sff.Log.PrintfnFsiErrorMsg "*** To avoid this MissingMethodException exception try restarting Revit without a document.\r\n*** Then from within Revit open your desired project.\r\n*** If the error persits please report it!"
                    | _ -> ()
                    )
                
                // just keep everything alive:
                sff.Window.Closing.Add (fun e ->
                    if not e.Cancel then // closing might be already cancelled in Seff.fs in main Seff lib.               
                        // even if closing is not canceled, don't close, just hide window
                        sff.Window.Visibility <- Windows.Visibility.Hidden
                        e.Cancel <- true
                        )           


                //-------------- hook up Seff -------------------------------------------------------------
                if isNull SeffAddin.Instance then 
                    App.alert "%s" "SeffAddin.Instance not set up yet"
                else
                    SeffAddin.Instance.ExternalEv <- Some <| ExternalEvent.Create(FsiRunEventHandler(sff, SeffAddin.Instance.RequestQueue))

                sff
            
            |Some s -> 
                s      
                
                (* //TODO Alt enter does not work !?!
                seff.Window.KeyDown.Add(fun e -> //to avoid pressing alt to focus on menu and the disabling Alt+Enter for Evaluating selection in FSI
                    seff.Log.PrintDebugMsg "key: %A, system key: %A, mod: %A " e.Key e.SystemKey Keyboard.Modifiers
                    //if e.Key = Key.LeftAlt || e.Key = Key.RightAlt then
                    //    e.Handled <- true
                    //elif (Keyboard.Modifiers = ModifierKeys.Alt && e.Key = Key.Enter) ||
                    //   (Keyboard.Modifiers = ModifierKeys.Alt && e.Key = Key.Return) then
                    //        seff.Fsi.Evaluate{code = seff.Tabs.CurrAvaEdit.SelectedText ; file=seff.Tabs.Current.FilePath; allOfFile=false}
                    //        e.Handled <- true
                    )
                seff.Tabs.Control.PreviewKeyDown.Add (fun e ->
                    if Keyboard.Modifiers = ModifierKeys.Alt && Keyboard.IsKeyDown(Key.Enter) then
                        seff.Log.PrintDebugMsg "Alt+Enter"
                    elif Keyboard.Modifiers = ModifierKeys.Alt && Keyboard.IsKeyDown(Key.Return) then
                        seff.Log.PrintDebugMsg "Alt+Return"
                    else
                        seff.Log.PrintDebugMsg "not Alt+Enter"
                        )   
                *)
        
        try
            if not App.seffWasEverShown then
                seff.Window.Show()
                App.seffWasEverShown <- true
                Result.Succeeded
            else
                seff.Window.Visibility <- Visibility.Visible
                Result.Succeeded

        with ex ->
            TaskDialog.Show("Execute Button Seff", sprintf "StartEditorCommand: message %s\r\ncommandData:%A\r\nelements:%A\r\nException:%A" message commandData elements ex ) |> ignore
            Result.Failed


    interface IExternalCommand with
        member this.Execute(commandData: ExternalCommandData, message: byref<string>, elements: ElementSet): Result = 
            this.Execute(commandData, &message, elements)


            (* let uiApp = 
                 let versionNumber = int uiConApp.ControlledApplication.VersionNumber
                 let fieldName = if versionNumber >= 2017 then  "m_uiapplication" else "m_application"
                 let fi = uiConApp.GetType().GetField(fieldName, BindingFlags.NonPublic ||| BindingFlags.Instance)
                 fi.GetValue(uiConApp) :?> UIApplication  *)


// System.Guid.NewGuid().ToString().ToUpper() // for FSI
