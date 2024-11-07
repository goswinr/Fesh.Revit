open System

//  The script takes two arguments: the year of the Revit version and the path to the DLL file.
//  It creates the  Fesh.addin  file in the Revit addin folder for the given year.
//  The script is used in the .fsproj  file to create the addin file after the build:
//   <Target Name="CreateRevitAddinFileInRevitFolder" BeforeTargets="AfterBuild">
//     <Exec Command="dotnet fsi createRevitAddinFileInRevitFolder.fsx $(RevitVersion) $(OutputPath)" />
//   </Target>


let xml (year:string) (dllPath:string) = $"""
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>Fesh | F# Editor and Scripting Host for Revit {year}</Name>
    <Assembly>{dllPath}</Assembly><!--adjust to your actual path-->
    <FullClassName>Fesh.Revit.FeshAddin</FullClassName>
    <AddInId>5B83A504-FF2D-4BAE-97CB-0DEB1046A5C2</AddInId>
    <VendorId>Goswin Rothenthal</VendorId>
    <VendorDescription>Fesh | F# Editor and Scripting Host for Revit {year}</VendorDescription>
  </AddIn>
</RevitAddIns>
"""




let printArgs() =
    eprintfn "fsi.CommandLineArgs:"
    for arg in fsi.CommandLineArgs do
        eprintfn $"  '{arg}'"


let createRevitAddinFileInRevitFolder() =
    // fsi.CommandLineArgs:
    //    'createRevitAddinFileInRevitFolder.fsx'
    //    '2024'
    //    'D:\Git\_Fesh_\Fesh.Revit\bin\2024\net48\'
    if fsi.CommandLineArgs.Length < 3 then
        eprintfn $"{ fsi.CommandLineArgs.[0] }: needs two arguments: <year> <dllPath> but got:"
        printArgs()
        1
    else
        let scriptName = fsi.CommandLineArgs.[0].Trim()
        let year = fsi.CommandLineArgs.[1].Trim()
        let targetDir = fsi.CommandLineArgs.[2].Trim() //.Replace("\\", "/")

        if not (IO.Directory.Exists targetDir) then
            eprintfn $"{scriptName}: targetDir found at '{targetDir}'"
            printArgs()
            1
        else
            //C:/ProgramData/Autodesk/Revit/Addins/year/Fesh.addin
            let root = @"C:\ProgramData\Autodesk\Revit\Addins"
            if not (IO.Directory.Exists root) then
                eprintfn $"{scriptName}: Revit Addin root directory not found at '{root}'"
                1
            else
                let revitAddinFolder = IO.Path.Combine(root, year)
                let addinFile = IO.Path.Combine(revitAddinFolder, "Fesh.addin")
                try
                    if not (IO.Directory.Exists revitAddinFolder) then
                        eprintfn $"{scriptName}: Creating missing directory: {revitAddinFolder}"
                        IO.DirectoryInfo(revitAddinFolder).Create()

                    if not (IO.Directory.Exists targetDir) then
                        eprintfn $"{scriptName}: targetDir not found at '{targetDir}'"
                        printArgs()
                        1
                    else
                        let dllPath = IO.Path.Combine(targetDir, "Fesh.Revit.dll")
                        IO.File.WriteAllText(addinFile, (xml year dllPath).Trim(), Text.Encoding.UTF8)
                        printfn $"{scriptName}: The file {addinFile} was created."
                        0
                with e ->
                    eprintfn $"{scriptName}: Failed to write {addinFile} {e.Message}"
                    1

createRevitAddinFileInRevitFolder()


