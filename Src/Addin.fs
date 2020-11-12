namespace Seff.Revit

open Autodesk.Revit.UI
open Autodesk.Revit.DB
open Autodesk.Revit.Attributes
open System
open System.Windows
open Seff
open Seff.Model
open Seff.Config
open System.Windows.Input
open System.Reflection
open System.Collections.Concurrent
open System.Diagnostics


// example of modeless dialog: https://github.com/pierpaolo-canini/Lame-Duck

module Debug =   
    let log prefix (content:string) =
        let time = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff")        
        let filename = sprintf "%sSeff.Revit.Log-%s.txt" prefix time
        let file = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),filename)
        async {try  IO.File.WriteAllText(file, content) with _ -> ()} |> Async.Start // file might be open and locked

 [<AbstractClass; Sealed>]
 type Current private ()=     
     static member val internal SeffWasEverShown: bool      = false with get,set
     static member val          SeffWindow      : Window    = null  with get, set 


type internal RunEvHandler(seff:Seff, queue: ConcurrentQueue< UIApplication->unit >) =    
    member this.GetName() = "Run in Seff"
    member this.Execute(app:UIApplication) = 
        let ok,f = queue.TryDequeue()
        if ok then
            try
                f(app) 
            with e ->
                seff.Log.PrintFsiErrorMsg "Error caught in IExternalEventHandler: %A" e
                
    
    interface IExternalEventHandler with 
        member this.Execute(app:UIApplication)  = this.Execute(app)
        member this.GetName() = this.GetName()


[<Regeneration(RegenerationOption.Manual)>]
[<Transaction(TransactionMode.Manual)>]
[<AllowNullLiteralAttribute>]
type SeffAddin() = // don't rename ! string referenced in in seff.addin file 
    
    let mutable exEvent:ExternalEvent = null 
    
    let queue = ConcurrentQueue<UIApplication->unit>()
    
    static let mutable instance : SeffAddin = null

    static member Instance = instance
    
    //member this.Queue = queue

    //member this.ExternalEvent  with get() = exEvent   and set(e) = exEvent <- e

    member this.RunApp (f:UIApplication->unit) =  
        queue.Enqueue(f)
        exEvent.Raise() |> ignore 
    
    member this.RunDoc (f:Document->unit) =  
        queue.Enqueue(fun app -> f(app.ActiveUIDocument.Document))
        exEvent.Raise() |> ignore 
    
   
    interface IExternalApplication with
        member this.OnStartup(uiConApp:UIControlledApplication) =
            try
                Sync.installSynchronizationContext()
                instance <- this     
                
                // ------------------- create Ribbon and button -------------------------------------------
                let thisAssemblyPath = Reflection.Assembly.GetExecutingAssembly().Location     
                let button = new PushButtonData("Seff", "Open Fsharp Editor", thisAssemblyPath, "Seff.Revit.StartEditorCommand")
                button.ToolTip <- "This will open the Seff Editor Window"
            
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
                let canRun = fun () ->  true // TODO check if in command, or enqued anyway?  !!
                let seff= Seff.App.createEditorForHosting({ hostName= "Revit" ; mainWindowHandel= winHandle; fsiCanRun=canRun  })
                Current.SeffWindow <- seff.Window

                //TODO make a C# plugin that loads Seff.addin once uiConApp.ControlledApplication.ApplicationInitialized to avoid missing method exceptions in FSI
                //uiConApp.LoadAddIn
                seff.Fsi.OnRuntimeError.Add (fun e -> 
                    match e with 
                    | :? MissingMethodException -> seff.Log.PrintFsiErrorMsg "*** To avoid this MissingMethodException exception try restarting Revit without a document.\r\n*** Then from within Revit open your desired project.\r\n*** If the error persits please report it!"
                    | _ -> ()
                    )


                (* //TODO Alt enter does not work !
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
                let handler = RunEvHandler(seff, queue)
                exEvent <- ExternalEvent.Create(handler)                
                           
                              
                Result.Succeeded

            with ex ->
                TaskDialog.Show("OnStartup of Seff.Revit.dll", sprintf "%A" ex ) |> ignore 
                Result.Failed

        
        member this.OnShutdown(app:UIControlledApplication) =           

            Result.Succeeded



[<Regeneration(RegenerationOption.Manual)>]  
[<Transaction(TransactionMode.Manual)>]
type StartEditorCommand() = // don't rename ! string referenced in  PushButtonData //new instance is created on every button click    
    interface IExternalCommand with
        member this.Execute(commandData: ExternalCommandData, message: byref<string>, elements: ElementSet): Result = 
            try                
                if not Current.SeffWasEverShown then 
                    
                    //--------- now show() -------------------
                    Current.SeffWindow.Show()
                    Current.SeffWasEverShown <- true
                    Result.Succeeded
                else                    
                    Current.SeffWindow.Visibility <- Visibility.Visible
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