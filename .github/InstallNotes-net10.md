## for Revit 2026 and later

To install just download and run `Fesh.Revit.net10-net10-Setup.exe` from the Assets.

Revit 2026 needs to be updated to its .NET 10 version.<br>
For Revit 2025 use [Fesh.Revit 0.32.3 for .NET 8](https://github.com/goswinr/Fesh.Revit/releases/tag/0.32.3),<br>
for Revit 2024 and earlier use [Fesh.Revit 0.32.3 for .NET 4.8](https://github.com/goswinr/Fesh.Revit/releases/tag/0.32.3-net48).<br>
They can be installed side by side with this version, best before it:
Fesh.Revit 0.32.3 for .NET 8 registers itself with Revit 2026 and later too.
If you install it after this version, start `Fesh.Revit.Bootstrapper.exe` of this version again, see below.

<details>

<summary>More Info on Installation</summary>

No admin rights are required to install or run the app.<br>
The app will be installed in `C:\Users\{username}\AppData\Local\Fesh.Revit.net10\current`<br>
and registered with a `Fesh.addin` in `%AppData%\Autodesk\Revit\Addins\{year}` with every installed Revit 2026 or later that runs on .NET 10.<br>
For a Revit that you install or update later, start `Fesh.Revit.Bootstrapper.exe` in that folder again.

When Fesh opens, it checks for a newer version and offers to download it.
It is installed when the last Revit has closed.

The installer is digitally code-signed via Azure Trusted Signing Service.<br>
It should not raise any security warnings.

See the [Changelog](https://github.com/goswinr/Fesh.Revit/blob/main/CHANGELOG.md) for changes in this release.

This release was created with [Velopack](https://velopack.io/).

</details>
