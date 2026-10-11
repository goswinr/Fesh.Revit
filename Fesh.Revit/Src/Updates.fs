namespace Fesh.Revit

open System.Diagnostics
open System.IO

/// Updates of Fesh.Revit installed by its installer, from the releases on GitHub. Velopack runs in the app of the installer,
/// Fesh.Revit.Bootstrapper.exe in the folder above the folder of this dll, not in Revit: Revit loads only one version of an assembly,
/// so Velopack in Revit could conflict with the one of another add-in, e.g. of Caddis.
module Updates =

    type private Dummy = class end

    let private bootstrapper =
        Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(typeof<Dummy>.Assembly.Location)), "Fesh.Revit.Bootstrapper.exe")

    /// With the shell, so that the bootstrapper inherits no handles of Revit.
    let private start (args: string) (logError: string -> unit) =
        try Process.Start(ProcessStartInfo(bootstrapper, args, UseShellExecute = true)) |> ignore
        with e -> logError $"Starting {bootstrapper} {args} failed: {e.Message}"

    /// When Fesh opens: the bootstrapper checks for a newer release and asks whether to download it.
    let check (logError: string -> unit) =
        if File.Exists bootstrapper then
            start "--check-for-update" logError
        else // this is expected when running a local build of Fesh.Revit not installed by its installer
            logError $"Automatic updates are not available because Fesh.Revit.Bootstrapper.exe was not found at:\r\n{bootstrapper}\r\nPlease re-install from https://github.com/goswinr/Fesh.Revit/releases"

    /// When the last Revit closes: the bootstrapper lets Velopack install a downloaded release once this Revit has exited.
    /// While another Revit is running it may use Fesh.Revit, then the next one to close does it. Called in OnShutdown.
    /// A Revit that has exited may still be listed for a while, it doesn't count.
    let installOnExit (logError: string -> unit) =
        let running (p: Process) = try not p.HasExited with _ -> false
        if File.Exists bootstrapper && (Process.GetProcessesByName "Revit" |> Array.filter running).Length = 1 then
            start $"--install-update {Process.GetCurrentProcess().Id}" logError
