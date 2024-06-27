# Fesh.Revit
 This repo contains a <a href="https://www.autodesk.com/products/revit/overview" target="_blank">Autodesk Revit</a> plugin to host Fesh. Fesh is a fsharp scripting editor based on <a href="https://github.com/goswinr/AvalonEditB" target="_blank">AvalonEdit</a>. The editor supports the latest features of F# 8.0 via <a href="https://www.nuget.org/packages/FSharp.Compiler.Service/43.8.300" target="_blank">FCS 430.0.0</a>. It has semantic syntax highlighting, auto completion and typ info tooltips. The output log supports colored text.


![](Docs/screen1.png)
The example script in the root folder generates the axes for cladding of the Louvre Abu Dhabi.
See also my talk at <a href="https://www.youtube.com/watch?v=ZY-bvZZZZnE" target="_blank">FSharpConf 2016</a>



### How to build
Before compiling make sure the path in the file `Fesh.addin` points to the correct location.
The `addin` file will then be copied to `C:/ProgramData/Autodesk/Revit/Addins/20XX/Fesh.addin` as the last step of the build process. See end of the `.fsproj` files

![](Docs/addinPath.png)

### How to use F# with Revit
By default a f# script evaluation starts asynchronous on a new thread. The `Fesh.Revit.dll` also provides utility functions to run <a href="https://knowledge.autodesk.com/support/revit-products/learn-explore/caas/CloudHelp/cloudhelp/2014/ENU/Revit/files/GUID-C946A4BA-2E70-4467-91A0-1B6BA69DBFBE-htm.html" target="_blank">synchronous transaction</a> on the current document or app instance:

    Fesh.Revit.ScriptingSyntax.runApp (fun (app:UIApplication)  -> ...)


### Licence
[MIT](https://github.com/goswinr/Fesh.Revit/blob/main/LICENSE)


