module Program
#nowarn "3391"
open System
open WixSharp // Ensure System.Xml.Linq and System.Windows.Forms are referenced in project since they are referenced by the WixSharp extension methods
open WixSharp.CommonTasks
open FileSystem

let run () =
    let productName = "Fesh.Revit"
    let supportedRevitYears = [| 2024 |]
    
    // Content paths
    let getReleasePath year = 
        let platform = if year <= 2024 then "net48" else "net8-windows"
        $"%s{src.FullName}/bin/%i{year}/%s{platform}"

    let dllVer = System.Reflection.Assembly.LoadFile(getReleasePath (supportedRevitYears |> Seq.head) + "/Fesh.Revit.dll").GetName().Version
    let version = Version(dllVer.ToString())
    
    // Create a project file structure
    let project =
        Project($"{productName} v{version}",
            Dir("%CommonAppData%/Autodesk/Revit/Addins", // ProgramData folder  
                // Year subfolders
                supportedRevitYears
                |> Array.map (fun year ->
                    let feature = Feature($"Revit %i{year}", $"%s{productName} %i{year}", false) // set enabled=false to evaluate condition
                    feature.Condition <- FeatureCondition($"REVIT_{year}_PRODUCT_CODE <> \"empty\"", 1)
                    
                    let releasePath = getReleasePath year
                    let feshRevitDir = getDir releasePath feature |> renameDir "Fesh.Revit"

                    Dir(feature, $"%i{year}", 
                        File(feature, "Resources/Fesh.Revit.addin"),
                        feshRevitDir
                    ) :> WixEntity 
                )
            )
        )

    project.Properties <- 
        // Add properties with Revit product codes from registry (to be used to enable/disable features)
        supportedRevitYears
        |> Array.map (fun year ->
            let regPath = $@"Software\Autodesk\Revit\Autodesk Revit {year}\Components"
            RegValueProperty($"REVIT_{year}_PRODUCT_CODE", RegistryHive.LocalMachine, regPath, "ProductCode", "empty", Win64 = true)
        )

    // Configure project
    project.Version <- version
    project.UI <- WUI.WixUI_FeatureTree // Shows the feature selection wizard page
    project.UpgradeCode <- Guid("6BE255E5-BC0B-45CD-B276-778C78B4829A")
    project.OutDir <- "bin/Release"
    project.MajorUpgrade <- MajorUpgrade(
        AllowDowngrades = false,
        AllowSameVersionUpgrades = true,
        Schedule = UpgradeSchedule.afterInstallInitialize,
        DowngradeErrorMessage = $"A newer version of {productName} for Revit is already installed.",
        IgnoreRemoveFailure = false)

    project.Scope <- InstallScope.perMachine
    project.LicenceFile <- "Resources/End User License Agreement.rtf"
    project.ControlPanelInfo.Manufacturer <- "Goswin Rothenthal"
    project.ControlPanelInfo.UrlInfoAbout <- "https://github.com/goswinr/Fesh.Revit"
    
    // Create the installer
    Compiler.BuildMsi(project) |> ignore

    0

[<EntryPoint>]
let main argv =
    try
        run ()
    with ex ->
        printfn $"ERROR: {ex.Message}"; 1

