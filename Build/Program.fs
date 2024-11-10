module Program 

open System
open Fake.IO
open Fake.IO.Globbing.Operators // !! and ++
open Fake.DotNet
open Fake.Core
open Fun.Build
open Fun.Result

let path xs = System.IO.Path.Combine(Array.ofList xs)
let rootDir = Files.findParent __SOURCE_DIRECTORY__ "Fesh.Revit2024.fsproj";
let buildDir = path [ rootDir; ".build" ]
let addinProjDir = rootDir
let installerProjDir = path [ rootDir; "Installer" ]
let installerProj = path [ installerProjDir; "Installer.fsproj" ]
let installerBinReleaseDir = path [ installerProjDir; "bin"; "Release" ]

let supportedRevitYears = [ 2024 ]

pipeline "Fesh.Revit" {

    stage "Clean" {
        run (fun _ ->
            Shell.cleanDir buildDir
            Shell.cleanDir (path [ addinProjDir; "bin" ])
            Shell.cleanDir (path [ installerProjDir; "bin" ])
        )
    }

    stage "Build Addin" {
        // run $"dotnet build {addinProjDir}\\Fesh.Revit2024.fsproj -p:RevitVersion=2024"
        run (fun ctx -> asyncResult {
            for year in supportedRevitYears do
                do! ctx.RunCommand $"dotnet build {addinProjDir}\\Fesh.Revit%i{year}.fsproj -c Release -p:RevitVersion=%i{year}"
        })        
    }
    
    stage "Build Installer" {
        run (fun _ ->
            let setParams p = { p with DoRestore = true; Properties = ["Configuration", "Release"] }
            MSBuild.build setParams installerProj
        )
        //run $"dotnet build {installerProj} -c Release"
    }

    stage "Copy Installer to Build Dir" {
        run (fun _ -> 
            !! $"{installerBinReleaseDir}/*.msi"
            |> Shell.copyFiles buildDir
        )
    }

    runIfOnlySpecified false
}

tryPrintPipelineCommandHelp ()
