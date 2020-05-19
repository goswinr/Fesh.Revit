namespace Seff.Revit

open System
open System.Windows
//open Seff
open Autodesk
open Autodesk.Revit
open Autodesk.Revit.ApplicationServices
open Autodesk.Revit.Attributes
open Autodesk.Revit.DB
open Autodesk.Revit.UI
open Autodesk.Revit.UI.Events
open Autodesk.Revit.UI

[<Transaction( TransactionMode.Manual )>]
type OpenEditorCommand()= // don't rename !
    interface IExternalCommand with
        member x.Execute(commandData: ExternalCommandData, message: byref<string>, elements: ElementSet) =
            

            TaskDialog.Show("Revit", "Hello Seff World") |> ignore 

            //https://thebuildingcoder.typepad.com/blog/2018/11/revit-window-handle-and-parenting-an-add-in-form.html
            //let winHandle = Diagnostics.Process.GetCurrentProcess().MainWindowHandle
            
            //let seff = Seff.App.runEditorHosted(winHandle,"Revit")
            //seff.Window.Show()

            Result.Succeeded



type SeffAddin() = // don't rename !
    interface IExternalApplication with
        member this.OnStartup(app:UIControlledApplication) =
            
            // Method to add Tab and Panel 
            let tabId = "Seff Tab"
            app.CreateRibbonTab(tabId)

            let thisAssemblyPath = Reflection.Assembly.GetExecutingAssembly().Location

                     
            let button = new PushButtonData("Seff", "Open Seff Editor Window", thisAssemblyPath, "Seff.Revit.OpenEditorCommand")
            button.ToolTip <- "This will open the Seff Editor Window"
            //let uriImage = new Uri("pack://application:,,,/RevitTemplate;component/Resources/code-small.png");
            //let largeImage = new System.Windows.Media.Imaging.BitmapImage(uriImage);
            //button.LargeImage <- largeImage


            let panel = app.CreateRibbonPanel(tabId,"Seff Panel")
            
            panel.AddItem(button) |> ignore 


            Result.Succeeded
        
        member this.OnShutdown(app:UIControlledApplication) =           

            Result.Succeeded