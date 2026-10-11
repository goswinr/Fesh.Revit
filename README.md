
![Logo](https://raw.githubusercontent.com/goswinr/Fesh.Revit/main/Media/logo128.png)

# Fesh.Revit
[![Build](https://github.com/goswinr/Fesh.Revit/actions/workflows/build.yml/badge.svg?event=push)](https://github.com/goswinr/Fesh.Revit/actions/workflows/build.yml)
<!-- [![Check NuGet](https://github.com/goswinr/Fesh.Revit/actions/workflows/outdatedNuget.yml/badge.svg)](https://github.com/goswinr/Fesh.Revit/actions/workflows/outdatedNuget.yml) -->

![code size](https://img.shields.io/github/languages/code-size/goswinr/Fesh.Revit.svg)
[![license](https://img.shields.io/github/license/goswinr/Fesh.Revit)](LICENSE.md)

Fesh.Revit is an F# scripting editor hosted inside [Revit](https://en.wikipedia.org/wiki/Autodesk_Revit). It is based on [Fesh](https://github.com/goswinr/Fesh).<br>
It has semantic syntax highlighting, auto completion, type info tooltips and more.<br>
The output window supports colored text.

![Screenshot](Media/screen1.png)
The example script in the root folder generates the axes for cladding of the Louvre Abu Dhabi.<br>
See also my talk at <a href="https://www.youtube.com/watch?v=ZY-bvZZZZnE" target="_blank">FSharpConf 2016</a>


## How to install

Download and run the Setup.exe from [Releases](https://github.com/goswinr/Fesh.Revit/releases).<br>
The current version is built for .NET 10, for Revit 2026 (updated to its .NET 10 version) and later.<br>
For Revit 2025 use [Fesh.Revit 0.32.3 for .NET 8](https://github.com/goswinr/Fesh.Revit/releases/tag/0.32.3).<br>
For Revit 2024 or earlier use [Fesh.Revit 0.32.3 for .NET 4.8](https://github.com/goswinr/Fesh.Revit/releases/tag/0.32.3-net48).<br>
They can be installed side by side.

Fesh.Revit will automatically offer to update itself when a new version is available.

The installer is created with [Velopack](https://velopack.io) and digitally signed.

No admin rights are required to install or run the app.<br>
The app will be installed in `\AppData\Local\Fesh.Revit.net10\current`. <br>
Setup will launch the `Fesh.Revit.Bootstrapper.exe` there. It will register the `net10.0\Fesh.Revit.dll` with every installed Revit from 2026 on <br>
by creating a `Fesh.addin` xml file in the Revit Addins folder of the current user at `%AppData%\Autodesk\Revit\Addins\20XX\Fesh.addin`.<br>
To register Fesh.Revit with a Revit that you install later, start `Fesh.Revit.Bootstrapper.exe` again.


### How to use F# with Revit
By default a f# script evaluation starts asynchronous on a new thread. <br>
The Revit API can only be used on Revit's UI thread. The `Fesh.Revit.Scripting` module has functions that run your code there,
with the current document or app instance, in a transaction or without one.
You need to use them when changing the document or when you need to access the app instance. <br>

```fsharp
Fesh.Revit.Scripting.transactWithApp (fun (app:UIApplication)  -> ... ) // to change the document
Fesh.Revit.Scripting.doWithApp       (fun (app:UIApplication)  -> ... ) // to only read from it
```

These functions only queue your code and return before it has run. Revit runs it when it is idle,
for a script that runs on the UI thread only after the script has finished.
So use the results of your Revit code inside the function you pass to them.

## Release Notes
For changes in each release see the  [CHANGELOG.md](https://github.com/goswinr/Fesh.Revit/blob/main/CHANGELOG.md)

## Publisher Privacy Policy
Fesh.Revit does not collect any data.<br>
The app only connects to the internet to check for updates on the [Github Releases](https://github.com/goswinr/Fesh.Revit/releases) page and downloads upon user confirmation.<br>
The check runs in the `Fesh.Revit.Bootstrapper.exe`, not in Revit. The relevant source code is here: [Updates.fs](https://github.com/goswinr/Fesh.Revit/blob/main/Fesh.Revit.Bootstrapper/Updates.fs).

## License
Fesh is licensed under the [MIT License](https://github.com/goswinr/Fesh.Revit/blob/main/LICENSE.md).
