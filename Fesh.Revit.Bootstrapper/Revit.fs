namespace Fesh.Revit.Bootstrapper

open System
open System.IO
open System.Security
open System.Text.RegularExpressions
open Microsoft.Win32

/// A message about the registration, shown in the window of the bootstrapper.
type Message =
    | Info of string
    | Done of string
    | Problem of string

/// Registers Fesh.Revit with every installed Revit from 2026 on, with a Fesh.addin in the add-in folder of the current user.
/// see https://help.autodesk.com/view/RVT/2026/ENU/?guid=Revit_API_Revit_API_Developers_Guide_Introduction_Add_In_Integration_Add_in_Registration_html
module Revit =

    /// Revit 2026 runs on .NET 10 from its .NET 10 update on, Revit 2027 and later too.
    /// Revit 2025 runs on .NET 8, Revit 2024 and earlier on .NET Framework 4.8, for these use Fesh.Revit 0.32.3 or earlier.
    let firstYear = 2026

    /// The last release of Fesh.Revit that supports Revit 2025 and earlier.
    let lastReleaseForOlder = "0.32.3"

    /// The release page of the last Fesh.Revit installer for this older Revit version.
    let olderReleaseUrl (year: int) =
        if year = 2025 then $"https://github.com/goswinr/Fesh.Revit/releases/tag/{lastReleaseForOlder}"       // .NET 8
        else                $"https://github.com/goswinr/Fesh.Revit/releases/tag/{lastReleaseForOlder}-net48" // .NET Framework 4.8

    /// Revit's installer creates a folder for its version here, for the add-ins of all users.
    /// Fesh.Revit 0.34.0 and earlier registered here.
    let private machineAddins =
        Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.CommonApplicationData, "Autodesk", "Revit", "Addins")

    /// Fesh.Revit is installed for the current user only, so it is registered for the current user only.
    let private userAddins =
        Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.ApplicationData, "Autodesk", "Revit", "Addins")

    /// Velopack's 'current' folder, with this exe and the folder net10.0 with Fesh.Revit.dll. It stays the same across updates.
    let installFolder = AppDomain.CurrentDomain.BaseDirectory

    /// This program, to start it again for a Revit installed later.
    let exePath = Path.Combine(installFolder, "Fesh.Revit.Bootstrapper.exe")

    /// The dll that Revit loads.
    let private dllPath = Path.Combine(installFolder, "net10.0", "Fesh.Revit.dll")

    /// Revit identifies the add-in by its AddInId, e.g. for the position of its window, don't change it.
    let private addinXml (year: int) =
        $"""<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>Fesh | F# Editor and Scripting Host for Revit {year}</Name>
    <Assembly>{SecurityElement.Escape dllPath}</Assembly>
    <FullClassName>Fesh.Revit.FeshAddin</FullClassName>
    <AddInId>5B83A504-FF2D-4BAE-97CB-0DEB1046A5C2</AddInId>
    <VendorId>Goswin Rothenthal</VendorId>
    <VendorDescription>Fesh | F# Editor and Scripting Host for Revit {year}</VendorDescription>
  </AddIn>
</RevitAddIns>
"""

    let private userFile (year: int) = Path.Combine(userAddins, string year, "Fesh.addin")

    let private machineFile (year: int) = Path.Combine(machineAddins, string year, "Fesh.addin")

    /// True if the .addin file registers this installation, not e.g. Fesh.Revit 0.32.3 for .NET 8.
    /// Fesh.Revit 0.34.0 wrote the path without escaping it.
    let private isOwn (file: string) =
        try
            let xml = File.ReadAllText file
            xml.IndexOf(SecurityElement.Escape installFolder, StringComparison.OrdinalIgnoreCase) >= 0
            || xml.IndexOf(installFolder, StringComparison.OrdinalIgnoreCase) >= 0
        with _ ->
            false

    /// The versions of the installed Revits, e.g. 2026, from the names of their add-in folders.
    let installedYears () =
        if Directory.Exists machineAddins then
            Directory.GetDirectories machineAddins
            |> Array.choose (fun dir ->
                match Int32.TryParse(Path.GetFileName dir) with
                | true, year when year > 2000 -> Some year
                | _ -> None)
            |> Array.sort
        else
            [||]

    /// The folder of Revit, from the registry of its installer, e.g. C:\Program Files\Autodesk\Revit 2026\
    let private revitFolder (year: int) =
        let fromRegistry =
            try
                use hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                use key = hklm.OpenSubKey($@"SOFTWARE\Autodesk\Revit\{year}")
                if isNull key then
                    None
                else
                    key.GetSubKeyNames()
                    |> Array.filter (fun name -> name.StartsWith("REVIT-", StringComparison.OrdinalIgnoreCase)) // e.g. REVIT-05:0409, per language
                    |> Array.tryPick (fun name ->
                        use sub = key.OpenSubKey name
                        match (if isNull sub then null else sub.GetValue "InstallationLocation") with
                        | :? string as dir when Directory.Exists dir -> Some dir
                        | _ -> None)
            with _ ->
                None
        match fromRegistry with
        | Some dir -> Some dir
        | None ->
            let dir = Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.ProgramFiles, "Autodesk", $"Revit {year}")
            if Directory.Exists dir then Some dir else None

    /// The major version of .NET that Revit runs on, from the tfm in its RevitAPI.runtimeconfig.json, e.g. 10 for "net10.0".
    /// Revit 2026 runs on .NET 8 until its .NET 10 update. None if not found.
    let private dotnetVersion (year: int) =
        revitFolder year
        |> Option.bind (fun dir ->
            try
                let json = File.ReadAllText(Path.Combine(dir, "RevitAPI.runtimeconfig.json"))
                let m = Regex.Match(json, "\"tfm\"\\s*:\\s*\"net(\\d+)\\.")
                if m.Success then Some(int m.Groups.[1].Value) else None
            with _ ->
                None)

    let private delete (log: Message -> unit) (file: string) =
        try
            File.Delete file
            log (Done $"Deleted {file}")
        with e ->
            log (Problem $"Deleting {file} failed: {e.Message}")

    /// Writes Fesh.addin for every installed Revit from 2026 on that runs on .NET 10.
    /// Moves the registration of this installation by Fesh.Revit 0.34.0 for all users to the current user.
    /// Replaces the registration of another installation, e.g. of Fesh.Revit 0.32.3 for .NET 8, only in a Revit that runs on .NET 10,
    /// Revit would load only one of them.
    let register (log: Message -> unit) =
        match installedYears () with
        | [||] -> log (Problem $"No Revit is installed: {machineAddins} has no folder for a Revit version.")
        | years ->
            for year in years do
                let machine = machineFile year
                let file = userFile year
                if year < firstYear then
                    if not (File.Exists machine) then // else an older Fesh.Revit is registered there
                        log (Info $"Revit {year} is installed too. This Fesh.Revit is for Revit {firstYear} and later, for Revit {year} install Fesh.Revit {lastReleaseForOlder} from {olderReleaseUrl year}")
                else
                    match dotnetVersion year with
                    | Some v when v < 10 ->
                        log (Problem $"Fesh.Revit was not registered with Revit {year}: it runs on .NET {v}, this Fesh.Revit needs .NET 10. Update Revit {year} to its .NET 10 version, then start this program again. Until then you can use Fesh.Revit {lastReleaseForOlder} for .NET 8 from {olderReleaseUrl 2025}")
                        if File.Exists file && isOwn file then
                            delete log file // it would fail to load
                    | None when File.Exists machine && not (isOwn machine) ->
                        log (Problem $"Fesh.Revit was not registered with Revit {year}: {machine} registers another installation of Fesh.Revit, e.g. {lastReleaseForOlder} for .NET 8, and Revit loads only one of them. If Revit {year} runs on .NET 10, delete that file, then start this program again.")
                    | _ ->
                        if File.Exists machine then
                            if not (isOwn machine) then
                                log (Info $"Revit {year} runs on .NET 10, so this Fesh.Revit replaces the other installation of Fesh.Revit registered in {machine}")
                            delete log machine
                        if File.Exists machine then // deleting it failed
                            log (Problem $"Fesh.Revit was not registered with Revit {year}: Revit loads only one of {machine} and {file}.")
                        else
                            try
                                Directory.CreateDirectory(Path.GetDirectoryName file) |> ignore
                                File.WriteAllText(file, addinXml year, Text.Encoding.UTF8)
                                log (Done $"Registered Fesh.Revit with Revit {year} in {file}")
                            with e ->
                                log (Problem $"Registering Fesh.Revit with Revit {year} failed: {e.Message}")

    /// Deletes the Fesh.addin files that register this installation, for the current user and, from Fesh.Revit 0.34.0, for all users.
    let unregister (log: Message -> unit) =
        for addins in [ userAddins; machineAddins ] do
            try
                if Directory.Exists addins then
                    for dir in Directory.GetDirectories addins do
                        let file = Path.Combine(dir, "Fesh.addin")
                        if File.Exists file && isOwn file then
                            delete log file
            with e ->
                log (Problem $"Unregistering Fesh.Revit in {addins} failed: {e.Message}")
