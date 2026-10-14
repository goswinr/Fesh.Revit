namespace Fesh.Revit.Bootstrapper

open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Windows
open System.Windows.Controls
open System.Windows.Documents
open System.Windows.Media
open System.Windows.Threading
open Velopack

/// The app that Velopack installs, updates and uninstalls. It registers Fesh.Revit with Revit and shows what it did.
/// Fesh.Revit itself runs in Revit. It starts this app to check for updates and to install them when the last Revit has closed.
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

    /// A button that counts down the seconds until the window closes. Clicking it keeps the window open.
    let private autoClose (win: Window) (seconds: int) =
        let left = ref seconds
        let text () = $"Keep open (closing in {left.Value} s)"
        let button = Button(Content = text (), Padding = Thickness(12.0, 4.0, 12.0, 4.0), Margin = Thickness 8.0, HorizontalAlignment = HorizontalAlignment.Right)
        let timer = DispatcherTimer(Interval = TimeSpan.FromSeconds 1.0)
        timer.Tick.Add(fun _ ->
            left.Value <- left.Value - 1
            if left.Value > 0 then
                button.Content <- text ()
            else
                timer.Stop()
                win.Close())
        button.Click.Add(fun _ ->
            timer.Stop()
            button.Visibility <- Visibility.Collapsed)
        win.Loaded.Add(fun _ -> timer.Start())
        button

    /// The installer that has just installed Fesh.Revit. Velopack's Setup.exe doesn't pass its path to this app,
    /// but writes it to its log, before the id of the package it installs, e.g.:
    /// [setup:23084] [16:14:13] [INFO]     Location: "C:\\Users\\me\\Downloads\\Fesh.Revit-win-Setup.exe"
    /// [setup:23084] [16:14:13] [INFO]     Package ID: Fesh.Revit
    let private installerPath () =
        try
            let file = Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.LocalApplicationData, "velopack", "velopack.log")
            use stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ||| FileShare.Delete)
            use reader = new StreamReader(stream)
            let lines = reader.ReadToEnd().Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
            lines
            |> Array.tryFindIndexBack (fun l -> l.StartsWith "[setup:" && l.TrimEnd().EndsWith "Package ID: Fesh.Revit")
            |> Option.bind (fun i ->
                let setup = lines.[i].Substring(0, lines.[i].IndexOf ']' + 1) // e.g. [setup:23084], the id of its process
                lines.[.. i] |> Array.tryFindBack (fun l -> l.StartsWith setup && l.Contains "Location: "))
            |> Option.map (fun l -> l.Substring(l.IndexOf "Location: " + 10).Trim().Trim('"').Replace(@"\\", @"\"))
            |> Option.filter File.Exists
        with _ -> None

    /// Registers Fesh.Revit again, to show where, and for a Revit installed since, and shows what it did.
    let private showWindow (updatedTo: string option) (installer: string option) =
        let messages = ResizeArray [ Info $"Fesh.Revit {version}, the F# Editor and Scripting Host for Revit {Revit.firstYear} and later." ]
        updatedTo |> Option.iter (fun v -> messages.Add(Done $"Fesh.Revit was updated to {v}."))
        Revit.register messages.Add
        messages.Add(Info "Start Revit and use the Fesh tab on its ribbon. If Revit asks whether to load Fesh, choose 'Always Load'.")
        messages.Add(Info $"To register Fesh.Revit with a Revit that you install later, start this program again: {Revit.exePath}")
        installer |> Option.iter (fun path -> messages.Add(Info $"The installer is not needed anymore, you can delete it: {path}"))
        messages.Add(Info "You can close this window now. Read more about Fesh.Revit at:")
        let win = Window(Title = "Fesh.Revit", Width = 820.0, Height = 480.0, WindowStartupLocation = WindowStartupLocation.CenterScreen)
        let button = autoClose win 30
        DockPanel.SetDock(button, Dock.Bottom)
        let panel = DockPanel()
        panel.Children.Add button |> ignore
        panel.Children.Add(FlowDocumentScrollViewer(Document = document messages, VerticalScrollBarVisibility = ScrollBarVisibility.Auto)) |> ignore
        win.Content <- panel
        Application().Run win

    [<EntryPoint; STAThread>]
    let main args =
        try
            // Velopack starts this exe with arguments when it installs, updates or uninstalls Fesh.Revit, then Run calls the hook and exits.
            // After installing, Setup.exe starts it again without arguments to show the window, but not when Setup.exe has the --silent flag.
            let updatedTo = ref None
            let installed = ref false
            VelopackApp
                .Build()
                .SetAutoApplyOnStartup(false) // Revit may be using Fesh.Revit, updates are installed when the last Revit has closed
                .OnAfterInstallFastCallback(fun _ -> Revit.register ignore)
                .OnAfterUpdateFastCallback(fun _ -> Revit.register ignore) // e.g. for a Revit installed since
                .OnBeforeUninstallFastCallback(fun _ -> Revit.unregister ignore)
                .OnRestarted(fun v -> updatedTo.Value <- Some(string v)) // by Update.exe, after installing an update
                .OnFirstRun(fun _ -> installed.Value <- true) // started by Setup.exe, after installing
                .Run()
            // Fesh.Revit in Revit starts this exe with these arguments, see Fesh.Revit/Src/Updates.fs
            match args with
            | [| "--check-for-update" |] ->
                Updates.check ()
                0
            | [| "--install-update"; revitId |] ->
                Updates.installOnExit (int revitId)
                0
            | _ -> showWindow updatedTo.Value (if installed.Value then installerPath () else None)
        with e ->
            MessageBox.Show($"Fesh.Revit.Bootstrapper failed:{Environment.NewLine}{e}", "Fesh.Revit", MessageBoxButton.OK, MessageBoxImage.Error) |> ignore
            1
