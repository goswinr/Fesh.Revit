namespace Seff.Revit

open Autodesk.Revit.UI
open Autodesk.Revit.DB
open Autodesk.Revit.Attributes
open System
open System.Windows
open Seff
open System.Windows.Input


module Current =
    let mutable UiApp: UIControlledApplication = null 
    let mutable App: UIApplication = null
    let mutable Doc: Document = null 
    let mutable SeffWin: Window = null 
    let run(f:Document -> unit) = f(Doc)


    
[<Transaction(TransactionMode.Manual)>]
type StartEditorCommand() = //new instance is created on every button click
    
       interface IExternalCommand with
        member this.Execute(commandData: ExternalCommandData, message: byref<string>, elements: ElementSet): Result = 
            try                
                if isNull Current.SeffWin then                     
                    Current.App <- commandData.Application
                    Current.Doc <- commandData.Application.ActiveUIDocument.Document
                    
                    //https://thebuildingcoder.typepad.com/blog/2018/11/revit-window-handle-and-parenting-an-add-in-form.html
                    let winHandle = Diagnostics.Process.GetCurrentProcess().MainWindowHandle
                    let seff = Seff.App.runEditorHosted(winHandle,"Revit")
                    Current.SeffWin <- seff.Window

                    seff.Window.KeyDown.Add(fun e -> if e.Key = Key.System then  e.Handled <- true) //to avoid pressing alt to focus on menu and the diabeling Alt+Enter for Evaluationg selection in FSI
                    
                    seff.Window.Closing.Add (fun e -> 
                        match seff.Fsi.AskAndCancel() with
                        |Evaluating -> e.Cancel <- true // no closing
                        |Ready | Initalizing | NotLoaded -> 
                            Current.SeffWin.Visibility <- Visibility.Hidden                           
                            e.Cancel <- true) // no closing
                    
                    seff.Window.Show()
                    Result.Succeeded
                else                    
                    Current.SeffWin.Visibility <- Visibility.Visible
                    Result.Succeeded

            with ex ->
                TaskDialog.Show("Execute Button Seff", sprintf "%A" ex ) |> ignore 
                Result.Failed


type SeffAddin() = // don't rename !
    interface IExternalApplication with
        member this.OnStartup(app:UIControlledApplication) =
            try
                Sync.installSynchronizationContext()
                Current.UiApp <- app            

                let thisAssemblyPath = Reflection.Assembly.GetExecutingAssembly().Location     
                let button = new PushButtonData("Seff", "Open Fsharp Editor", thisAssemblyPath, "Seff.Revit.StartEditorCommand")
                button.ToolTip <- "This will open the Seff Editor Window"
            
                let uriImage = new Uri("pack://application:,,,/Seff.Revit;component/Media/LogoCursorTr32.png") // build from VS not via "dotnet build"  to include. <Resource Include="Media\LogoCursorTr32.png" />  
                let largeImage = new System.Windows.Media.Imaging.BitmapImage(uriImage)
                button.LargeImage <- largeImage
            
                let tabId = "Seff"
                app.CreateRibbonTab(tabId)
                let panel = app.CreateRibbonPanel(tabId,"Seff")            
                panel.AddItem(button) |> ignore
                
                Result.Succeeded

            with ex ->
                TaskDialog.Show("OnStartup of Seff.Revit.dll", sprintf "%A" ex ) |> ignore 
                Result.Failed

        
        member this.OnShutdown(app:UIControlledApplication) =           

            Result.Succeeded




// System.Guid.NewGuid().ToString().ToUpper() // for FSI