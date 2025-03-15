
![Logo](https://raw.githubusercontent.com/goswinr/Fesh.Revit/main/Media/logo128.png)

# Fesh.Revit
[![Build](https://github.com/goswinr/Fesh.Revit/actions/workflows/build.yml/badge.svg?event=push)](https://github.com/goswinr/Fesh.Revit/actions/workflows/build.yml)
[![Check NuGet](https://github.com/goswinr/Fesh.Revit/actions/workflows/outdatedNuget.yml/badge.svg)](https://github.com/goswinr/Fesh.Revit/actions/workflows/outdatedNuget.yml)
[![Hits](https://hits.seeyoufarm.com/api/count/incr/badge.svg?url=https%3A%2F%2Fgithub.com%2Fgoswinr%2FFesh.Revit&count_bg=%2379C83D&title_bg=%23555555&icon=github.svg&icon_color=%23E7E7E7&title=hits&edge_flat=false)](https://hits.seeyoufarm.com)
![code size](https://img.shields.io/github/languages/code-size/goswinr/Fesh.Revit.svg)
[![license](https://img.shields.io/github/license/goswinr/Fesh.Revit)](LICENSE)

Fesh.Revit is an F# scripting editor hosted inside [Revit]("https://www.autodesk.com/products/revit/overview"). It is based on [Fesh](https://github.com/goswinr/Fesh).<br>
It has semantic syntax highlighting, auto completion, type info tooltips and more.<br>
The output window supports colored text.

![Screenshot](Media/screen1.png)
The example script in the root folder generates the axes for cladding of the Louvre Abu Dhabi.<br>
See also my talk at <a href="https://www.youtube.com/watch?v=ZY-bvZZZZnE" target="_blank">FSharpConf 2016</a>


## How to install


Download and run the Setup.exe from [Releases](https://github.com/goswinr/Fesh.Revit/releases).<br>
Use the .NET 8 version if you have Revit 2025 or later.<br>
Use the .NET 4.8 version if you have Revit 2024 or earlier.

Fesh.Revit will automatically offer to update itself when a new version is available.

The installer is created with [Velopack](https://velopack.io) and digitally signed.

No admin rights are required to install or run the app.<br>
The app will be installed in `\AppData\Local\Fesh.Revit`. <br>
Setup will launch the `Fesh.Revit.Bootstrapper.exe`. It will register the `Fesh.Revit.dll` with Revit <br>
by creating an `Fesh.Revit.addin` xml file in the Revit Addins folder at `C:/ProgramData/Autodesk/Revit/Addins/20XX/Fesh.Revit.addin`.


### How to use F# with Revit
By default a f# script evaluation starts asynchronous on a new thread. The `Fesh.Revit.dll` also provides utility functions to run <a href="https://knowledge.autodesk.com/support/revit-products/learn-explore/caas/CloudHelp/cloudhelp/2014/ENU/Revit/files/GUID-C946A4BA-2E70-4467-91A0-1B6BA69DBFBE-htm.html" target="_blank">synchronous transaction</a> on the current document or app instance:

```fsharp
Fesh.Revit.ScriptingSyntax.runApp (fun (app:UIApplication)  -> ...)
```

## Release Notes
For changes in each release see the  [CHANGELOG.md](https://github.com/goswinr/Fesh.Revit/blob/main/CHANGELOG.md)

## License
Fesh is licensed under the [MIT License](https://github.com/goswinr/Fesh.Revit/blob/main/LICENSE.md).
