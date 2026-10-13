## Fesh for Revit 2018 to 2027

To install, download and run `Fesh.Revit-win-Setup.exe` from the Assets.<br>
The installer is digitally code-signed via Azure Trusted Signing Service.<br>
It should not raise any security warnings.

If you have earlier versions than 0.35 installed, uninstall them first in the Windows Settings, under Apps.

It registers Fesh with every installed Revit from 2018 on.
Revit 2024 and earlier run it on .NET Framework 4.8, Revit 2025 and 2026 on .NET 8, or on .NET 10 from their .5 updates on, Revit 2027 on .NET 10.
Fesh loads its build for the runtime of Revit when Revit starts, so it keeps working when Revit is updated.


<details>

<summary>More Info on Installation</summary>

No admin rights should be required to install or run the app.<br>
The app will be installed in `C:\Users\{username}\AppData\Local\Fesh.Revit\current`,<br>
and registered with a `Fesh.addin` in `%AppData%\Autodesk\Revit\Addins\{year}` for each Revit.<br>
For a Revit that you install later, start `Fesh.Revit.Bootstrapper.exe` in that folder again.

When Fesh opens, it checks for a newer version and offers to download it.
It is installed when the last Revit has closed.


See the [Changelog](https://github.com/goswinr/Fesh.Revit/blob/main/CHANGELOG.md) for changes in this release.

This release was created with [Velopack](https://velopack.io/).

</details>
