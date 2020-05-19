namespace Seff.Revit

open System
open System.Windows
open Seff
open Autodesk
open Autodesk.Revit
open Autodesk.Revit.ApplicationServices
open Autodesk.Revit.Attributes
open Autodesk.Revit.DB
open Autodesk.Revit.UI
open Autodesk.Revit.UI.Events
open Autodesk.Revit.UI



type SeffAddin() =
    interface IExternalApplication with
        member this.OnStartup(app:UIControlledApplication) =
            
            // Create a custom ribbon tab
            String tabName = "This Tab Name";
            application.CreateRibbonTab(tabName);

            // Create two push buttons
            PushButtonData button1 = new PushButtonData("Button1", "My Button #1",
                @"C:\ExternalCommands.dll", "Revit.Test.Command1");
            PushButtonData button2 = new PushButtonData("Button2", "My Button #2",
                @"C:\ExternalCommands.dll", "Revit.Test.Command2");

            // Create a ribbon panel
            RibbonPanel m_projectPanel = application.CreateRibbonPanel(tabName, "This Panel Name"); 

            // Add the buttons to the panel
            List projectButtons = new List();
            projectButtons.AddRange(m_projectPanel.AddStackedItems(button1, button2));

            return Result.Succeeded;


            // Method to add Tab and Panel 
            let panel = app.CreateRibbonPanel("Seff")
            app.CreateRibbonTab("seff Tab")
            let thisAssemblyPath = Reflection.Assembly.GetExecutingAssembly().Location

            // BUTTON FOR THE SINGLE-THREADED WPF OPTION
           
            let button = new PushButtonData("Seff", "Seff", thisAssemblyPath,"RevitTemplate.EntryCommand")
            
            // defines the tooltip displayed when the button is hovered over in Revit's ribbon
            button.ToolTip <- "Visual interface for debugging applications.";
            
            //let uriImage = new Uri("pack://application:,,,/RevitTemplate;component/Resources/code-small.png");
            //let largeImage = new System.Windows.Media.Imaging.BitmapImage(uriImage);
            //button.LargeImage <- largeImage
            panel.AddItem(button) |> ignore 

            //https://thebuildingcoder.typepad.com/blog/2018/11/revit-window-handle-and-parenting-an-add-in-form.html
            let winHandle = Diagnostics.Process.GetCurrentProcess().MainWindowHandle

            let seff = Seff.App.runEditorHosted(winHandle,"Revit")
            //seff.Window.Show()


            Result.Succeeded
        
        member this.OnShutdown(app:UIControlledApplication) =           

            Result.Succeeded