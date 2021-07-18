namespace Seff.Revit

open Autodesk.Revit.UI
open Autodesk.Revit.DB
open Autodesk.Revit.Attributes
open System
open System.Windows
open Seff
open Seff.Util.General
open Seff.Model
open Seff.Config
open System.Windows.Input
open System.Reflection
open System.Collections.Concurrent
open System.Diagnostics


module Version = 
    let name =

#if REVIT2019 
        "Revit 2019"
#else
    #if REVIT2021 
        "Revit 2021"
    #else
        "Revit"
    #endif
#endif

// example of modeless dialog: https://github.com/pierpaolo-canini/Lame-Duck

/// A static class to provide logging and  access to the Seff Editor
[<AbstractClass; Sealed>]
type App private () =     
    
    static let mutable logFileOnDesktopCount = ref 0

    static let mutable seff  = Unchecked.defaultof<Seff> 

    /// for managing visibility state when showing and hiding the editor window
    static member val internal seffWasEverShown: bool =  false with get,set     
    
    /// a static reference to the current Seff Editor 
    static member Seff 
       with get() = seff 
       and internal set v = seff <- v // set is internal only
    
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
           with _ -> () } |> Async.Start 
    
    /// logs text to Seff editor window in red
    /// if Seff is null it writes a text file to desktop instead and shows a Task Dialog.
    static member alert msg =  
       Printf.kprintf (fun s -> 
           if not <|  Object.ReferenceEquals(seff,null) && seff.Window.IsLoaded then  
               try
                   seff.Log.PrintnColor 180 100 10 s
               with e -> // in case the logging fails
                   incr logFileOnDesktopCount
                   let printE = sprintf "\r\nLog.PrintnColor error:\r\n%A" e
                   App.logToFile "App.alertFailed-" (s+printE)                
                   TaskDialog.Show("Seff Addin App.alertFailed", s+printE) |> ignore 
           elif !logFileOnDesktopCount < 10 then 
               incr logFileOnDesktopCount
               App.logToFile "App.alert-" s                
               TaskDialog.Show("Seff Addin App.alert", s) |> ignore 
           ) msg     
    
    
    /// logs text to Seff editor window in green
    /// does nothing if Seff is null
    static member log msg =  
       Printf.kprintf (fun s -> 
           if not <|  Object.ReferenceEquals(seff,null) then  seff.Log.PrintnColor 50 100 10 s            
           ) msg



[<Transaction(TransactionMode.Manual)>]
type internal FsiRunEventHandler (seff:Seff, queue: ConcurrentQueue< UIApplication->unit >) =    
    member this.GetName() = "Run in Seff"
    member this.Execute(app:UIApplication) = 
        let f = ref Unchecked.defaultof<UIApplication->unit>
        while queue.TryDequeue(f) do //using a queue is only needed if a single script calls into a transaction more than once
            try
                (!f)(app) 
            with e ->
                seff.Log.PrintfnFsiErrorMsg "Error caught in FsiRunEventHandler(a IExternalEventHandler) in  this.Execute(app:UIApplication): %A" e
    
    interface IExternalEventHandler with 
        member this.GetName() = this.GetName()
        member this.Execute(app:UIApplication)  = this.Execute(app)


[<Regeneration(RegenerationOption.Manual)>]
[<Transaction(TransactionMode.Manual)>]
[<AllowNullLiteralAttribute>]
type SeffAddin() = // don't rename ! string referenced in in seff.addin file     

    let mutable exEvent:ExternalEvent = null 
    
    let queue = ConcurrentQueue<UIApplication->unit>()    

    static member val Instance = null with get,set
    
    /// runs a F# function via the IExternalEventHandler pattern for modeless dialogs
    /// this is the only way to run code from modless dialogs such as Seff editor
    member this.RunOnApp (f:UIApplication -> unit) =  
        queue.Enqueue(f)
        match exEvent.Raise() with 
        |ExternalEventRequest.Accepted -> ()
        |ExternalEventRequest.Denied   -> App.alert "exEvent.Raise() returned ExternalEventRequest.Denied"
        |ExternalEventRequest.Pending  -> App.alert "exEvent.Raise() returned ExternalEventRequest.Pending"
        |ExternalEventRequest.TimedOut -> App.alert "exEvent.Raise() returned ExternalEventRequest.TimedOut"
        |x -> App.alert "exEvent.Raise() returned unknown ExternalEventRequest: %A" x
    
    /// runs a F# function via the IExternalEventHandler pattern for modeless dialogs
    /// this is the only way to run code from modless dialogs such as Seff editor
    member this.RunOnDoc (f:Document->unit) =  
        this.RunOnApp (fun app -> f app.ActiveUIDocument.Document)  

    
    member this.OnStartup(uiConApp:UIControlledApplication) =
        try
            Sync.installSynchronizationContext()
            SeffAddin.Instance <- this     
                
            // ------------------- create Ribbon and button -------------------------------------------
            let thisAssemblyPath = Reflection.Assembly.GetExecutingAssembly().Location     
            let button = new PushButtonData("Seff", "Open Fsharp Editor", thisAssemblyPath, "Seff.Revit.StartEditorCommand")
            button.ToolTip <- "This will open the Seff F# Script Editor Window"
            
            let uriImage32 = new Uri("pack://application:,,,/Seff.Revit;component/Media/logo32.png") // build from VS not via "dotnet build"  to include. <Resource Include="Media\LogoCursorTr32.png" /> 
            let uriImage16 = new Uri("pack://application:,,,/Seff.Revit;component/Media/logo16.png")              
            button.LargeImage <- Media.Imaging.BitmapImage(uriImage32)//for ribbon in tab
            button.Image      <- Media.Imaging.BitmapImage(uriImage16)//for quick acess toolbar
            
            let tabId = "Seff"
            uiConApp.CreateRibbonTab(tabId)
            let panel = uiConApp.CreateRibbonPanel(tabId,"Seff")            
            panel.AddItem(button) |> ignore
                

            //-------------- start Seff -------------------------------------------------------------                
            //https://thebuildingcoder.typepad.com/blog/2018/11/revit-window-handle-and-parenting-an-add-in-form.html
            let winHandle = Diagnostics.Process.GetCurrentProcess().MainWindowHandle
            let canRun = fun () ->  true // TODO check if in command, or enqued anyway ?  !!
            let logo = new Uri("pack://application:,,,/Seff.Revit;component/Media/logo.ico") 
            let hostData = { 
                hostName = Version.name 
                mainWindowHandel = winHandle
                fsiCanRun =  canRun 
                logo = Some logo
                }
            let seff = Seff.App.createEditorForHosting(hostData)                
            App.Seff <- seff

            //TODO make a C# plugin that loads Seff.addin once uiConApp.ControlledApplication.ApplicationInitialized to avoid missing method exceptions in FSI                
            seff.Fsi.OnRuntimeError.Add (fun e -> 
                match e with 
                | :? MissingMethodException -> seff.Log.PrintfnFsiErrorMsg "*** To avoid this MissingMethodException exception try restarting Revit without a document.\r\n*** Then from within Revit open your desired project.\r\n*** If the error persits please report it!"
                | _ -> ()
                )


            (* //TODO Alt enter does not work !?!
            seff.Window.KeyDown.Add(fun e -> //to avoid pressing alt to focus on menu and the diabeling Alt+Enter for Evaluationg selection in FSI                       
                seff.Log.PrintDebugMsg "key: %A, sytem key: %A, mod: %A " e.Key e.SystemKey Keyboard.Modifiers 
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
                    )   *)

            seff.Window.Closing.Add (fun e -> 
                match seff.Fsi.AskAndCancel() with
                |Evaluating -> e.Cancel <- true // no closing
                |Ready | Initalizing | NotLoaded -> 
                    seff.Window.Visibility <- Visibility.Hidden                           
                    e.Cancel <- true) // no closing
                           
                        
            //-------------- hook up Seff -------------------------------------------------------------                 
            exEvent <- ExternalEvent.Create(FsiRunEventHandler(seff, queue))                               
            Result.Succeeded

        with ex ->
            App.alert "OnStartup of Seff.Revit.dll:\r\n%A" ex 
            Result.Failed

        
    member this.OnShutdown(app:UIControlledApplication) =  
        // https://forums.autodesk.com/t5/revit-api-forum/how-stop-or-cancel-revit-closing/td-p/5983643
        //Your add-in OnShutdown method should be called when and only when Revit is closing. 
        //That will at least give you a chance to display the message to the user and "force her to press a button", 
        //if you really think that is a good idea, even if it does not enable you to prevent Revit from closing.
        App.Seff.Tabs.AskIfClosingWindowIsOk()  |> ignore // this will try to save files too. ignore result since it not possible to prevent revit from closing eventually
        Result.Succeeded
        //Result.Cancelled //TODO use this to dispose resouces correctly ?
        
    interface IExternalApplication with
        member this.OnStartup(uiConApp:UIControlledApplication) = this.OnStartup(uiConApp)
        member this.OnShutdown(app:UIControlledApplication)     = this.OnShutdown(app)
        
        //member this.Queue = queue

        //member this.ExternalEvent  with get() = exEvent   and set(e) = exEvent <- e       



[<Regeneration(RegenerationOption.Manual)>]  
[<Transaction(TransactionMode.Manual)>]
type StartEditorCommand() = // don't rename ! string referenced in  PushButtonData //new instance is created on every button click   
    member this.Execute(commandData: ExternalCommandData, message: byref<string>, elements: ElementSet): Result = 
            try                
                if not App.seffWasEverShown then
                    App.Seff.Window.Show()
                    App.seffWasEverShown <- true
                    Result.Succeeded
                else                    
                    App.Seff.Window.Visibility <- Visibility.Visible
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