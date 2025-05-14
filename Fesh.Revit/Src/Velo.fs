namespace Fesh.Revit

open Autodesk.Revit.UI
open System
open System.Windows
open Fesh

open Velopack
open Velopack.Sources
open Velopack.Locators
open System.Reflection
// open Microsoft.Extensions.Logging

module Velo =

    type FeshRevitDummy = class end

    let mutable updatesDownloaded = false

    let mutable updateManager = None

    let checkForNewVelopackRelease( fesh:Fesh) =
        if not updatesDownloaded then
            async {
                try
                    let ass = Assembly.GetAssembly(typeof<FeshRevitDummy>)
                    let cv = ass.GetName().Version.ToString()
                    let cv = if cv.EndsWith(".0") then cv[..^2] else cv

                    // The GitHub access token to use with the request to download releases.
                    // If left empty, the GitHub rate limit for unauthenticated requests allows for up to 60 requests per hour, limited by IP address.
                    // only needs fine-grained access to content in readonly mode
                    // https://docs.velopack.io/reference/cs/Velopack/Sources/GithubSource/constructors
                    let readOnlyToken = ""
                    let source = new GithubSource("https://github.com/goswinr/Fesh.Revit", accessToken = readOnlyToken, prerelease = false)

                    let exePath = ass.Location.Replace("Fesh.Revit.dll", "Fesh.Revit.Bootstrapper.exe" )

                    if IO.File.Exists exePath then

                        // Use reflection to call the internal constructor of WindowsVelopackLocator
                        // https://github.com/velopack/velopack/issues/461
                        // let loc =
                        //     try
                        //         let locType = typeof<WindowsVelopackLocator>
                        //         let ctor = locType.GetConstructor(BindingFlags.Instance ||| BindingFlags.NonPublic, null, [| typeof<string>; typeof<ILogger> |], null)
                        //         ctor.Invoke([| exePath; null |]) :?> WindowsVelopackLocator
                        //     with e ->
                        //         failwithf $"Reflection Invoking the Constructor of WindowsVelopackLocator(\"{exePath}\", null) failed:\r\n{e}"

                        let processId = uint <|  System.Diagnostics.Process.GetCurrentProcess().Id
                        let iLogger = null
                        let loc = WindowsVelopackLocator(exePath, processId, iLogger)

                        updateManager <- Some (new UpdateManager(source, locator = loc))

                        match updateManager.Value.CheckForUpdatesAsync().Result with
                        | null ->
                            fesh.Log.PrintfnInfoMsg $"You are using the latest version of Fesh for Revit: {cv}"
                            updateManager <- None
                        | upInfo ->
                            if isNull updateManager.Value.UpdatePendingRestart then
                                let nv = upInfo.TargetFullRelease.Version.ToString()
                                fesh.Log.PrintfnInfoMsg $"A newer version of Fesh for Revit is available: {nv} , you are using {cv}"
                                let exe = ass.Location
                                let exeFolder = IO.Path.GetDirectoryName(exe)
                                let parentFolder = IO.Path.GetDirectoryName(exeFolder)
                                let updater = IO.Path.Combine(parentFolder, "Update.exe")
                                if not (IO.File.Exists updater) then
                                    // this is expected when running a local build of Fesh not packaged with Velopack
                                    fesh.Log.PrintfnIOErrorMsg "Automatic updates are not available because Update.exe was not found at:"
                                    fesh.Log.PrintfnIOErrorMsg $"{updater}"
                                    fesh.Log.PrintfnIOErrorMsg "Please re-install from https://github.com/goswinr/Fesh.Revit/releases"
                                else
                                    do! Async.SwitchToContext Fittings.SyncWpf.context
                                    match MessageBox.Show(
                                        fesh.Window,
                                        $"Update Fesh.Revit from {cv} to {nv} after the next Revit restart?",
                                        "Fesh for Revit | Updates available!",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question,
                                        MessageBoxResult.Yes, // default result
                                        MessageBoxOptions.None) with
                                            | MessageBoxResult.No  ->
                                                fesh.Log.PrintfnInfoMsg "Updating Fesh for Revit was skipped."
                                            | MessageBoxResult.Yes ->
                                                fesh.Log.PrintfnInfoMsg "Downloading Updates for Fesh ..."
                                                do! Async.AwaitTask (updateManager.Value.DownloadUpdatesAsync(upInfo))
                                                updatesDownloaded <- true
                                                fesh.Log.PrintfnInfoMsg "Updates are downloaded and will be applied when all running Revit instances are closed."
                                            | r ->
                                                fesh.Log.PrintfnInfoMsg $"Fesh for Revit Updates are available, Unknown result from MessageBox.Show: {r}"
                            else
                                updatesDownloaded <- true
                                fesh.Log.PrintfnInfoMsg "Updates are downloaded and will be applied when all running Revit instances are closed."
                    else
                        // this is expected when running a local build of Fesh not packaged with Velopack
                        fesh.Log.PrintfnIOErrorMsg "Automatic updates are not available because Fesh.Revit.Bootstrapper.exe was not found at:"
                        fesh.Log.PrintfnIOErrorMsg $"{exePath}"
                        fesh.Log.PrintfnIOErrorMsg "Please re-install from https://github.com/goswinr/Fesh.Revit/releases"

                with e ->
                    updateManager <- None
                    fesh.Log.PrintfnInfoMsg "Could not check for Velopack updates:\r\n%A" e

            } |> Async.Start


    let  updateOnRevitClose(revit:UIApplication, alert: string -> unit) =
        // This event is raised when the Revit application is just about to be closed.
        // Event is not cancellable. The 'Cancellable' property of event's argument is always False.
        // No document may be modified at the time of the event.
        // The sender object of this event is UIControlledApplication object.
        revit.ApplicationClosing.Add(fun _ ->
            if updatesDownloaded then
                let revitProcesses = System.Diagnostics.Process.GetProcessesByName("Revit")
                if revitProcesses.Length = 1 then
                    let exe = Reflection.Assembly.GetAssembly(typeof<FeshRevitDummy>).Location
                    let exeFolder = IO.Path.GetDirectoryName(exe)
                    let parentFolder = IO.Path.GetDirectoryName(exeFolder)
                    let updater = IO.Path.Combine(parentFolder, "Update.exe")
                    if IO.File.Exists(updater) then
                        match updateManager with
                        | Some um  ->
                            if isNull um.UpdatePendingRestart then
                                alert "Fesh.Revit updateManager: UpdatePendingRestart is null."
                            else
                                um.WaitExitThenApplyUpdates(um.UpdatePendingRestart, silent = false, restart = true, restartArgs = null)
                        | _ ->
                            alert "Fesh.Revit updateManager or updateInfo not found."
                    else
                        alert $"Fesh.Revit Update.exe not found in {parentFolder}"
                else
                    alert "Fesh.Revit Updates are downloaded but not applied yet because there are still other Revit instances running."
                )

