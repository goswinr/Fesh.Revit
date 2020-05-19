namespace Seff.Revit.AssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices


// IMPORTANT:
// Without this Guid Rhino does not remeber the plugin after restart, setting <ProjectGuid> in the new SDK fsproj file does not to work.
[<assembly: Guid("47c48fce-bb24-441e-bf66-6c1c1825ea1f")>] //Don't change its used in Rhino.Scripting.dll via reflection
//System.Guid.NewGuid() //for fsi


// done in new SDK fsproj file:
//[<assembly: AssemblyTitle("Seff.Rhino")>]
//[<assembly: AssemblyDescription("Seff | FSharp Scriting Editor for Rhino")>]
//[<assembly: AssemblyConfiguration("")>]
//[<assembly: AssemblyCompany("Seff.io")>]
//[<assembly: AssemblyProduct("Seff.Rhino")>]
//[<assembly: AssemblyCopyright("© Copyright Goswin Rothenthal 2020")>]
//[<assembly: AssemblyTrademark("")>]
//[<assembly: AssemblyCulture("en-US")>]
//[<assembly: AssemblyVersion("0.1.*")>] // You can specify all the values or you can default the Build and Revision Numbers  by using the '*' as shown below:
//[<assembly: AssemblyFileVersion("0.1.1.0")>]

do ()