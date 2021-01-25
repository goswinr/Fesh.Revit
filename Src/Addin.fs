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


// example of modeless dialog: https://github.com/pierpaolo-canini/Lame-Duck

 [<AbstractClass; Sealed>]
 type App private () =     
     
     static member val internal seffWasEverShown: bool      =  false        with get,set     
     
     static member val  seff  = Unchecked.defaultof<Seff>  with get, set 
     
     static member alert msg =  
        Printf.kprintf (fun s -> 
            if not <|  Object.ReferenceEquals(App.seff,null) then  App.seff.Log.PrintnColor 180 100 10 s
            else                                                   TaskDialog.Show("Seff Addin App.alert", s) |> ignore 
            ) msg
     
     static member log msg =  
        Printf.kprintf (fun s -> 
            if not <|  Object.ReferenceEquals(App.seff,null) then  App.seff.Log.PrintnColor 50 100 10 s            
            ) msg

     static member logToFile prefix (content:string) =
        let time = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff") // ensure unique name       
        let filename = sprintf "%sSeff.Revit.Log-%s.txt" prefix time
        let file = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),filename)
        async {try  IO.File.WriteAllText(file, content) with _ -> ()} |> Async.Start 


[<Transaction(TransactionMode.Manual)>]
type internal FsiRunEventHandler (seff:Seff, queue: ConcurrentQueue< UIApplication->unit >) =    
    member this.GetName() = "Run in Seff"
    member this.Execute(app:UIApplication) = 
        let f = ref Unchecked.defaultof<UIApplication->unit>
        while queue.TryDequeue(f) do 
            try
                (!f)(app) 
            with e ->
                seff.Log.PrintfnFsiErrorMsg "Error caught in IExternalEventHandler: %A" e
       
                
    
    interface IExternalEventHandler with 
        member this.Execute(app:UIApplication)  = this.Execute(app)
        member this.GetName() = this.GetName()


[<Regeneration(RegenerationOption.Manual)>]
[<Transaction(TransactionMode.Manual)>]
[<AllowNullLiteralAttribute>]
type SeffAddin() = // don't rename ! string referenced in in seff.addin file     

    let mutable exEvent:ExternalEvent = null 
    
    let queue = ConcurrentQueue<UIApplication->unit>()    

    static member val Instance = null with get,set
   
    member this.RunOnApp (f:UIApplication -> unit) =  
        queue.Enqueue(f)
        match exEvent.Raise() with 
        |ExternalEventRequest.Accepted -> ()
        |ExternalEventRequest.Denied   -> App.alert "exEvent.Raise() returned ExternalEventRequest.Denied"
        |ExternalEventRequest.Pending  -> App.alert "exEvent.Raise() returned ExternalEventRequest.Pending"
        |ExternalEventRequest.TimedOut -> App.alert "exEvent.Raise() returned ExternalEventRequest.TimedOut"
        |x -> App.alert "exEvent.Raise() returned unknown ExternalEventRequest: %A" x
    
    member this.RunOnDoc (f:Document->unit) =  
        this.RunOnApp (fun app -> f app.ActiveUIDocument.Document)    
   
    interface IExternalApplication with
        member this.OnStartup(uiConApp:UIControlledApplication) =
            try
                Sync.installSynchronizationContext()
                SeffAddin.Instance <- this     
                
                // ------------------- create Ribbon and button -------------------------------------------
                let thisAssemblyPath = Reflection.Assembly.GetExecutingAssembly().Location     
                let button = new PushButtonData("Seff", "Open Fsharp Editor", thisAssemblyPath, "Seff.Revit.StartEditorCommand")
                button.ToolTip <- "This will open the Seff F# Script Editor Window"
            
                let uriImage32 = new Uri("pack://application:,,,/Seff.Revit;component/Media/LogoCursorTr32.png") // build from VS not via "dotnet build"  to include. <Resource Include="Media\LogoCursorTr32.png" /> 
                let uriImage16 = new Uri("pack://application:,,,/Seff.Revit;component/Media/LogoCursorTr16.png")              
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
                let seff= Seff.App.createEditorForHosting({ hostName= "Revit 2018" ; mainWindowHandel = winHandle; fsiCanRun =  canRun  })                
                App.seff <- seff

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
                        )            *)

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
                TaskDialog.Show("OnStartup of 2018 Seff.Revit.dll", sprintf "%A" ex ) |> ignore 
                Result.Failed

        
        member this.OnShutdown(app:UIControlledApplication) =  
            Result.Succeeded

            
        //member this.Queue = queue

        //member this.ExternalEvent  with get() = exEvent   and set(e) = exEvent <- e       



[<Regeneration(RegenerationOption.Manual)>]  
[<Transaction(TransactionMode.Manual)>]
type StartEditorCommand() = // don't rename ! string referenced in  PushButtonData //new instance is created on every button click    
    interface IExternalCommand with
        member this.Execute(commandData: ExternalCommandData, message: byref<string>, elements: ElementSet): Result = 
            try                
                if not App.seffWasEverShown then
                    App.seff.Window.Show()
                    App.seffWasEverShown <- true
                    Result.Succeeded
                else                    
                    App.seff.Window.Visibility <- Visibility.Visible
                    Result.Succeeded

            with ex ->
                TaskDialog.Show("Execute Button Seff", sprintf "%A" ex ) |> ignore 
                Result.Failed


            (* let uiApp =
                 let versionNumber = int uiConApp.ControlledApplication.VersionNumber
                 let fieldName = if versionNumber >= 2017 then  "m_uiapplication" else "m_application"
                 let fi = uiConApp.GetType().GetField(fieldName, BindingFlags.NonPublic ||| BindingFlags.Instance)
                 fi.GetValue(uiConApp) :?> UIApplication  *)
                        

// System.Guid.NewGuid().ToString().ToUpper() // for FSI