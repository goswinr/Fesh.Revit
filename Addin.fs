namespace Seff.Revit

open Autodesk.Revit.UI
open Autodesk.Revit.DB
open Autodesk.Revit.Attributes
open System
open System.Windows
open Seff

// to acces the current UIControlledApplication
type Current private () =
    static member val UiApp: UIControlledApplication = null with get ,set
    static member val App: UIApplication = null with get ,set
    static member val Doc: Document = null with get ,set

[<Transaction(TransactionMode.Manual)>]
type StartEditorCommand() =
    
    let mutable win :Window = null

    interface IExternalCommand with
        member this.Execute(commandData: ExternalCommandData, message: byref<string>, elements: ElementSet): Result = 
            try
                
                if isNull win then 
                    
                    Current.App <- commandData.Application
                    Current.Doc <- commandData.Application.ActiveUIDocument.Document

                    //https://thebuildingcoder.typepad.com/blog/2018/11/revit-window-handle-and-parenting-an-add-in-form.html
                    let winHandle = Diagnostics.Process.GetCurrentProcess().MainWindowHandle
                    
                    TaskDialog.Show("Seff", sprintf "creating" ) |> ignore 
                    let seff = Seff.App.runEditorHosted(winHandle,"Revit")
                    win <- seff.Window
                    
                    win.Closing.Add (fun e -> 
                        match seff.Fsi.AskAndCancel() with
                        |Evaluating                      -> e.Cancel <- true // no closing
                        |Ready | Initalizing | NotLoaded -> 
                            win.Visibility <- Visibility.Hidden 
                            TaskDialog.Show("Seff", sprintf "win.Visibility: %A" win.Visibility ) |> ignore 
                            //TODO add option to menu to actually close, not just hide ??
                            e.Cancel <- true) 
                     
                    win.Show()
                    Result.Succeeded
                else
                    TaskDialog.Show("Seff", sprintf "win.Visibility: %A" win.Visibility ) |> ignore 
                    win.Visibility <- Visibility.Visible
                    Result.Succeeded

            with ex ->
                TaskDialog.Show("Seff", sprintf "%A" ex ) |> ignore 
                Result.Failed


type SeffAddin() = // don't rename !
    interface IExternalApplication with
        member this.OnStartup(app:UIControlledApplication) =
            
            Sync.installSynchronizationContext()
            Current.UiApp <- app
            
            // Method to add Tab and Panel 
            let tabId = "Seff"
            app.CreateRibbonTab(tabId)

            let thisAssemblyPath = Reflection.Assembly.GetExecutingAssembly().Location

     
            let button = new PushButtonData("Seff", "Open Fsharp Editor", thisAssemblyPath, "Seff.Revit.StartEditorCommand")
            button.ToolTip <- "This will open the Seff Editor Window"
            try
                let uriImage = new Uri("pack://application:,,,/Seff.Revit;component/Media/LogoCursorTr32.png") // <Resource Include="Media\LogoCursorTr32.png" />  
                let largeImage = new System.Windows.Media.Imaging.BitmapImage(uriImage)
                button.LargeImage <- largeImage
            with ex ->
                TaskDialog.Show("Seff", sprintf "%A" ex ) |> ignore 
                

            let panel = app.CreateRibbonPanel(tabId,"Seff")
            
            panel.AddItem(button) |> ignore 


            Result.Succeeded
        
        member this.OnShutdown(app:UIControlledApplication) =           

            Result.Succeeded

            // System.Guid.NewGuid().ToString().ToUpper() // for FSI