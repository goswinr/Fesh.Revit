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

    let yearsNet48 = [| 2018 .. 2024 |]
    let yearsNet8  = [| 2025 .. 2030 |]

    let isNetFramework =
        Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase)

    let applicableYears, notApplicableYears =
        if isNetFramework then yearsNet48, yearsNet8
        else                   yearsNet8 , yearsNet48

    let searchAddinFolder =
         // https://help.autodesk.com/view/RVT/2024/ENU/?guid=Revit_API_Revit_API_Developers_Guide_Introduction_Add_In_Integration_Add_in_Registration_html
         @"C:\ProgramData\Autodesk\Revit\Addins"

    let findAddinFolders() =
        if not (IO.Directory.Exists searchAddinFolder) then
            eprintfn $"Revit Addin root directory not found:\r\n{searchAddinFolder}\r\nis Revit installed?"
            None
        else
            let ok    = applicableYears    |> Array.map (fun year -> IO.Path.Combine(searchAddinFolder, $"{year}")) |> Array.filter IO.Directory.Exists
            let notOk = notApplicableYears |> Array.map (fun year -> IO.Path.Combine(searchAddinFolder, $"{year}")) |> Array.filter IO.Directory.Exists

            if ok.Length = 0 then
                if notOk.Length = 0 then
                    eprintfn $"No Revit version subfolder found in {searchAddinFolder}. Is Revit installed?"
                elif isNetFramework then
                    printfn $"These Revit addin folders for Revit 2025 or later exist:"
                    for no in notOk do printfn $"    {no}"
                    eprintfn $"Please use the installer based on .NET 8 for Revit 2025 and later versions:"
                    eprintfn $"https://github.com/goswinr/Fesh.Revit/releases"
                else
                    printfn $"These Revit addin folders for Revit 2024 and earlier exist:"
                    for no in notOk do eprintfn $"    {no}"
                    eprintfn $"Please use the installer based on .NET FRamework 4.8 for Revit 2024 and earlier versions:"
                    eprintfn $"https://github.com/goswinr/Fesh.Revit/releases"
                None
            else
                for dir in notOk do
                    let dllPath = IO.Path.Combine(dir, "Fesh.addin")
                    if IO.File.Exists dllPath then
                        () // this folder has Fesh installed, all good
                    else
                        let year = IO.Path.GetFileName(dir)
                        eprintfn $"There is a folder called {dir}."
                        eprintfn $"It looks like you also have Revit {year} installed."
                        if isNetFramework then
                            eprintfn $"To also use Fesh there, please install the .NET 8 based version of Fesh for Revit 2025 and later."
                        else
                            eprintfn $"To also use Fesh there, please install the .NET 4.8 based version of Fesh for Revit {yearsNet48[0]} to 2024."
                Some ok


    let register(log:AvalonLog.AvalonLog) =
        match findAddinFolders() with
        | Some dirs ->
            for dir in dirs do
                let exeDir = Reflection.Assembly.GetExecutingAssembly().Location |> IO.Path.GetDirectoryName
                let dllPath = IO.Path.Combine(exeDir, "Fesh.Revit.dll")
                let addinFile = IO.Path.Combine(dir, "Fesh.addin")
                let year = IO.Path.GetFileName(dir)
                let xml = getXml year dllPath
                IO.File.WriteAllText(addinFile, xml, Text.Encoding.UTF8)
                log.printfBrush  Brushes.DarkGreen $"The file {addinFile} was created."
            log.printfBrush  Brushes.DarkGreen $"Fesh was installed and registered with Revit."
            log.printfBrush  Brushes.Black $"\r\n\r\nYou can now close this window."
        | None ->
            ()


    let unregister() =
        for year in 2018 .. 2030 do
            let addinFile = IO.Path.Combine(@"C:\ProgramData\Autodesk\Revit\Addins", $"{year}", "Fesh.addin")
            if IO.File.Exists addinFile then
                try
                    IO.File.Delete addinFile
                    printfn $" The file {addinFile} was deleted."
                with e ->
                    eprintfn $"Error deleting {addinFile}: {e}"



