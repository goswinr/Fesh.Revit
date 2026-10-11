namespace Fesh.Revit.Bootstrapper

open System
open System.IO
open System.Security

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

    let private delete (log: Message -> unit) (file: string) =
        try
            File.Delete file
            log (Done $"Deleted {file}")
        with e ->
            log (Problem $"Deleting {file} failed: {e.Message}")

    /// Writes Fesh.addin for every installed Revit from 2026 on.
    /// Moves the registration of this installation by Fesh.Revit 0.34.0 for all users to the current user.
    /// Doesn't touch the registration of another installation, e.g. of Fesh.Revit 0.32.3 for .NET 8, Revit would load only one of them.
    let register (log: Message -> unit) =
        match installedYears () with
        | [||] -> log (Problem $"No Revit is installed: {machineAddins} has no folder for a Revit version.")
        | years ->
            for year in years do
                let machine = machineFile year
                if year < firstYear then
                    if not (File.Exists machine) then // else an older Fesh.Revit is registered there
                        log (Info $"Revit {year} is installed too. This Fesh.Revit is for Revit {firstYear} and later, for Revit {year} install Fesh.Revit {lastReleaseForOlder} from {olderReleaseUrl year}")
                elif File.Exists machine && not (isOwn machine) then
                    log (Problem $"Fesh.Revit was not registered with Revit {year}: {machine} registers another installation of Fesh.Revit, e.g. {lastReleaseForOlder} for .NET 8, and Revit loads only one of them. If Revit {year} runs on .NET 10, delete that file and start this program again: {exePath}")
                else
                    if File.Exists machine then
                        delete log machine
                    let file = userFile year
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
