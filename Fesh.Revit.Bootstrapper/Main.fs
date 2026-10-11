namespace Fesh.Revit.Bootstrapper

open System
open System.Diagnostics
open System.Reflection
open System.Windows
open System.Windows.Controls
open System.Windows.Documents
open System.Windows.Media
open Velopack

/// The app that Velopack installs, updates and uninstalls. It registers Fesh.Revit with Revit and shows what it did.
/// Fesh.Revit itself runs in Revit.
module Main =

    let private version = Assembly.GetExecutingAssembly().GetName().Version.ToString 3

    let private readme = "https://github.com/goswinr/Fesh.Revit"

    /// The messages as a read-only document, problems in red, with a link to the README.
    let private document (messages: Message seq) =
        let doc = FlowDocument(PagePadding = Thickness 16.0, FontFamily = FontFamily "Segoe UI", FontSize = 13.0)
        let add (brush: Brush) (inline': Inline) =
            doc.Blocks.Add(Paragraph(inline', Foreground = brush, Margin = Thickness(0.0, 0.0, 0.0, 8.0)))
        for m in messages do
            match m with
            | Info text -> add Brushes.Black (Run text)
            | Done text -> add Brushes.DarkGreen (Run text)
            | Problem text -> add Brushes.Red (Run text)
        let link = Hyperlink(Run readme, NavigateUri = Uri readme)
        link.RequestNavigate.Add(fun e -> Process.Start(ProcessStartInfo(e.Uri.AbsoluteUri, UseShellExecute = true)) |> ignore)
        add Brushes.Black link
        doc

    /// Registers Fesh.Revit again, to show where, and for a Revit installed since, and shows what it did.
    let private showWindow (updatedTo: string option) =
        let messages = ResizeArray [ Info $"Fesh.Revit {version}, the F# Editor and Scripting Host for Revit {Revit.firstYear} and later." ]
        updatedTo |> Option.iter (fun v -> messages.Add(Done $"Fesh.Revit was updated to {v}."))
        Revit.register messages.Add
        messages.Add(Info "Start Revit and use the Fesh tab on its ribbon. If Revit asks whether to load Fesh, choose 'Always Load'.")
        messages.Add(Info $"To register Fesh.Revit with a Revit that you install later, start this program again: {Revit.exePath}")
        messages.Add(Info "You can close this window now. Read more about Fesh.Revit at:")
        let win =
            Window(
                Title = "Fesh.Revit",
                Width = 820.0,
                Height = 440.0,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = FlowDocumentScrollViewer(Document = document messages, VerticalScrollBarVisibility = ScrollBarVisibility.Auto)
            )
        Application().Run win

    [<EntryPoint; STAThread>]
    let main _args =
        try
            // Velopack starts this exe with arguments when it installs, updates or uninstalls Fesh.Revit, then Run calls the hook and exits.
            let updatedTo = ref None
            VelopackApp
                .Build()
                .SetAutoApplyOnStartup(false) // Revit may be using Fesh.Revit, updates are installed when the last Revit has closed
                .OnAfterInstallFastCallback(fun _ -> Revit.register ignore)
                .OnAfterUpdateFastCallback(fun _ -> Revit.register ignore) // e.g. for a Revit installed since
                .OnBeforeUninstallFastCallback(fun _ -> Revit.unregister ignore)
                .OnRestarted(fun v -> updatedTo.Value <- Some(string v)) // by Update.exe, after installing an update
                .Run()
            showWindow updatedTo.Value
        with e ->
            MessageBox.Show($"Fesh.Revit.Bootstrapper failed:{Environment.NewLine}{e}", "Fesh.Revit", MessageBoxButton.OK, MessageBoxImage.Error) |> ignore
            1
