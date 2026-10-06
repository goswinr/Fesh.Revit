namespace Fesh.Revit.Bootstrapper

open System
open System.Windows.Media

module Result =
    let ofOption msg = function
        | Some v -> Ok v
        | None -> Error msg

module Revit =

    // https://help.autodesk.com/view/RVT/2025/ENU/?guid=Revit_API_Revit_API_Developers_Guide_Introduction_Add_In_Integration_Add_in_Registration_html
    let getXml (year:string) (dllPath:string) = $"""<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>Fesh | F# Editor and Scripting Host for Revit {year}</Name>
    <Assembly>{dllPath}</Assembly>
    <FullClassName>Fesh.Revit.FeshAddin</FullClassName>
    <AddInId>5B83A504-FF2D-4BAE-97CB-0DEB1046A5C2</AddInId>
    <VendorId>Goswin Rothenthal</VendorId>
    <VendorDescription>Fesh | F# Editor and Scripting Host for Revit {year}</VendorDescription>
  </AddIn>
</RevitAddIns>
"""

    /// Revit 2026 runs on .NET 10 from its .NET 10 update on, Revit 2027 and later too.
    let yearsNet10 = [| 2026 .. 2035 |]

    /// Revit 2025 runs on .NET 8, Revit 2024 and earlier on .NET Framework 4.8.
    /// For these use Fesh.Revit 0.32.3 or earlier.
    let yearsOlder = [| 2018 .. 2025 |]

    /// The last release of Fesh.Revit that supports Revit 2025 and earlier.
    let lastReleaseForOlder = "0.32.3"

    /// The release page of the last Fesh.Revit installer for this older Revit version.
    let olderReleaseUrl (year:string) =
        if year = "2025" then $"https://github.com/goswinr/Fesh.Revit/releases/tag/{lastReleaseForOlder}"       // .NET 8
        else                  $"https://github.com/goswinr/Fesh.Revit/releases/tag/{lastReleaseForOlder}-net48" // .NET Framework 4.8

    let searchAddinFolder =
         // https://help.autodesk.com/view/RVT/2024/ENU/?guid=Revit_API_Revit_API_Developers_Guide_Introduction_Add_In_Integration_Add_in_Registration_html
         @"C:\ProgramData\Autodesk\Revit\Addins"

    /// The Fesh.Revit.dll next to this exe, the .addin files point to it.
    let dllPath =
        let exeDir = Reflection.Assembly.GetExecutingAssembly().Location |> IO.Path.GetDirectoryName
        IO.Path.Combine(exeDir, "Fesh.Revit.dll")

    /// Checks if the .addin file points to the Fesh.Revit.dll of this installation.
    let isOwnAddinFile (addinFile:string) =
        try IO.File.ReadAllText(addinFile).Contains($"<Assembly>{dllPath}</Assembly>", StringComparison.OrdinalIgnoreCase)
        with _ -> false

    let findAddinFolders() =
        if not (IO.Directory.Exists searchAddinFolder) then
            eprintfn $"Revit Addin root directory not found:\r\n{searchAddinFolder}\r\nis Revit installed?"
            None
        else
            let ok    = yearsNet10 |> Array.map (fun year -> IO.Path.Combine(searchAddinFolder, $"{year}")) |> Array.filter IO.Directory.Exists
            let notOk = yearsOlder |> Array.map (fun year -> IO.Path.Combine(searchAddinFolder, $"{year}")) |> Array.filter IO.Directory.Exists

            if ok.Length = 0 then
                if notOk.Length = 0 then
                    eprintfn $"No Revit version subfolder found in {searchAddinFolder}. Is Revit installed?"
                else
                    printfn $"These Revit addin folders for Revit 2025 and earlier exist:"
                    for no in notOk do printfn $"    {no}"
                    eprintfn $"This version of Fesh.Revit is for Revit 2026 and later only."
                    eprintfn $"For Revit 2025 and earlier please use the installers of Fesh.Revit {lastReleaseForOlder} from:"
                    eprintfn "%s for Revit 2025" (olderReleaseUrl "2025")
                    eprintfn "%s for Revit 2024 and earlier" (olderReleaseUrl "2024")
                None
            else
                for dir in notOk do
                    let addinFile = IO.Path.Combine(dir, "Fesh.addin")
                    let year = IO.Path.GetFileName(dir)
                    if not (IO.File.Exists addinFile) then
                        eprintfn $"There is a folder called {dir}."
                        eprintfn $"It looks like you also have Revit {year} installed."
                        eprintfn $"To also use Fesh there, please install Fesh.Revit {lastReleaseForOlder} from:"
                        eprintfn $"{olderReleaseUrl year}"
                    elif isOwnAddinFile addinFile then
                        eprintfn $"The file {addinFile} points to this installation."
                        eprintfn $"But this version of Fesh.Revit is for Revit 2026 and later only, it will fail to load in Revit {year}."
                        eprintfn $"Please delete this file and install Fesh.Revit {lastReleaseForOlder} for Revit {year} from:"
                        eprintfn $"{olderReleaseUrl year}"
                    else
                        () // this folder has an older Fesh.Revit installed, all good
                Some ok


    let register(log:AvalonLog.AvalonLog) =
        match findAddinFolders() with
        | Some dirs ->
            for dir in dirs do
                let addinFile = IO.Path.Combine(dir, "Fesh.addin")
                let year = IO.Path.GetFileName(dir)
                let xml = getXml year dllPath
                IO.File.WriteAllText(addinFile, xml, Text.Encoding.UTF8)
                log.printfBrush  Brushes.DarkGreen $"The file {addinFile} was created."
            log.printfBrush  Brushes.DarkGreen $"Fesh was installed and registered with Revit."
            log.printfBrush  Brushes.Black $"\r\n\r\nYou can now close this window."
        | None ->
            ()


    /// Deletes only the .addin files that point to this installation,
    /// so that an older Fesh.Revit installed side by side for Revit 2025 and earlier keeps working.
    let unregister() =
        for year in Array.append yearsOlder yearsNet10 do
            let addinFile = IO.Path.Combine(searchAddinFolder, $"{year}", "Fesh.addin")
            if IO.File.Exists addinFile && isOwnAddinFile addinFile then
                try
                    IO.File.Delete addinFile
                    printfn $" The file {addinFile} was deleted."
                with e ->
                    eprintfn $"Error deleting {addinFile}: {e}"



