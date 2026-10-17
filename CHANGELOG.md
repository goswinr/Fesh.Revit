# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- The symbol `FESHREVIT` is defined in FSI and in the type checker, so that scripts can use `#if FESHREVIT` for code that only runs in Revit.
- One of the symbols `REVITNET10`, `REVITNET8` or `REVITNET48` is defined too, for the .NET runtime of Revit: .NET 10, .NET 8 or .NET Framework 4.8.
- A symbol for the version of Revit is defined too, like `REVIT2024`.
### Changed
- The window of the bootstrapper closes after 30 seconds. Its button counts down the seconds, clicking it keeps the window open.
- After installing, the window shows the path of the installer, which can be deleted then.


## [0.35.1] - 2026-10-11
### Changed
- One installer for every Revit from 2018 on, like the one of [Caddis](https://github.com/goswinr/Caddis): `Fesh.Revit-win-Setup.exe`.
- It has a build of Fesh.Revit for each runtime of Revit, in the folders `net48`, `net8.0` and `net10.0`. Revit 2024 and earlier run it on .NET Framework 4.8, Revit 2025 and 2026 on .NET 8, or on .NET 10 from their .5 updates on, Revit 2027 on .NET 10.
- In Revit 2025 and later the new `Fesh.Revit.Loader` loads the build for the runtime of Revit when Revit starts, so Fesh keeps working when Revit is updated. The bootstrapper doesn't check the runtime of Revit anymore.
- The .NET builds reference the RevitAPI of Revit 2025, so that they load in Revit 2025.5 on .NET 10 too.
- The installer has the id `Fesh.Revit` again and installs to `AppData/Local/Fesh.Revit`. It installs over Fesh.Revit 0.32.3 for .NET 8. The installations of 0.32.3 for .NET Framework 4.8 and of 0.34.0 and 0.35.0 for .NET 10 don't update to it, it replaces their registration in Revit, then they can be uninstalled.
- The release fails if one of the `vpk` commands fails, not only the last one.


## [0.35.0] - 2026-10-11
### Changed
- Update to [Fesh 0.35.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0350), with a dark theme and Go to Definition
- The bootstrapper of the installer runs on .NET Framework 4.8, like the one of [Caddis](https://github.com/goswinr/Caddis), so the installer needs no .NET runtime. `Fesh.Revit.dll` is installed in the subfolder `net10.0`.
- Fesh.Revit is registered for the current user in `%AppData%/Autodesk/Revit/Addins`, not for all users in `C:/ProgramData/Autodesk/Revit/Addins`. The registration of 0.34.0 is moved there.
- It is registered with every Revit from 2026 on that runs on .NET 10, on every install, update and start of `Fesh.Revit.Bootstrapper.exe`, e.g. for a Revit installed later. In those Revits it replaces the registration of Fesh.Revit 0.32.3 for .NET 8.
- The bootstrapper shows what it registered, and any problems, in its window.
- Updates are checked for and installed by `Fesh.Revit.Bootstrapper.exe`, not by Velopack in Revit.
- The default code for new files uses `transactWithApp` again.
### Fixed
- A Revit 2026 that still runs on .NET 8 doesn't get a registration that fails to load, and a working registration of 0.32.3 there isn't overwritten.
- The path in `Fesh.addin` is XML escaped, e.g. for a user name with `&`.
- `transactWithDoc` and `transactWithApp` roll back the transaction if the function fails, instead of committing half of its changes.
- Running a script doesn't fail with a NullReferenceException when no document is open.
- The example script works with Revit 2026.
- The README says that the functions of `Fesh.Revit.Scripting` queue the code, they don't wait for it.


## [0.34.0] - 2026-10-06
### Changed
- Update to [Fesh 0.34.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0340)
- Build for .NET 10 only, for Revit 2026 (updated to its .NET 10 version) and later. For Revit 2025 and earlier use Fesh.Revit 0.32.3 (.NET 8 and .NET 4.8).
- The installer has the new id `Fesh.Revit.net10` and installs to `AppData/Local/Fesh.Revit.net10`, so that it can be installed side by side with Fesh.Revit 0.32.3, which is still needed for Revit 2025 and earlier.
- It updates itself on the new `net10` channel. The .NET 8 installations of 0.32.3 and earlier don't update to it.
- Uninstalling only removes the `Fesh.addin` files that point to this installation.
- Update Velopack to 1.2.161 and SourceLink to 10.0.401
### Added
- The folders of `RevitAPI.dll` and `Fesh.Revit.dll` are lib folders for the type checker and FSI, so that `#r "RevitAPI"`, `#r "RevitAPIUI"` and `#r "Fesh.Revit"` resolve without a full path.
- The default code for new files has no `#I` path to the Revit installation folder anymore.


## [0.32.3] - 2026-05-23
### Changed
- Update to [Fesh 0.32.3](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0323)


## [0.32.0] - 2026-01-28
### Fixed
- Include netstandard.xml in output, to show better tooltips in net10

## [0.31.0] - 2025-12-19
### Changed
- Update to [Fesh 0.31.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0310)

## [0.30.2] - 2025-12-19
### Changed
- Default code on startup

## [0.30.1] - 2025-12-19
### Added
- New functions in `Fesh.Revit.Scripting` module:
  - `transactWithDoc` - Runs a function in a Revit API transaction with Document
  - `transactWithApp` - Runs a function in a RevitAPI transaction using UIApplication
  - `doWithDoc` - Runs a function with current Document without transaction
  - `doWithApp` - Runs a function with current UIApplication without transaction

### Changed
- Update to [Fesh 0.30.1](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0301)

### Deprecated
- `ScriptingSyntax` module is now obsolete, use `Fesh.Revit.Scripting` module functions instead
- `ScriptingSyntax.run` is deprecated, use `Fesh.Revit.Scripting.transactWithDoc` instead
- `ScriptingSyntax.runApp` is deprecated, use `Fesh.Revit.Scripting.transactWithApp` instead


## [0.29.3] - 2025-12-14
### Changed
- Update to [Fesh 0.29.3](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0293) to try fix issue with FSharp.Core loading in Revit 2025

## [0.29.0] - 2025-12-14
### Changed
- Update to [Fesh 0.29.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0290)
- build for net8 and net48

## [0.28.1] - 2025-10-05
### Changed
- Update to [Fesh 0.28.1](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0281)

## [0.28.0] - 2025-06-13
### Changed
- Update to [Fesh 0.28.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0280)

## [0.26.4] - 2025-05-16
### Changed
- Enable running transactions from async Thread

## [0.26.3] - 2025-05-14
### Changed
- Update to Fesh 0.26.3
- Update to Velopack

## [0.25.0] - 2025-03-18
### Added
- Fixed missing method in Velopack exception in Revit 2024
(by removing Microsoft.Extensions.Logging reference in AvalonLog dependency)

## [0.24.1] - 2025-03-18
### Added
- Fixed type load exception in Revit 2024
(by removing explicit FSharp.Core reference in AvalonLog dependency)

## [0.24.0] - 2025-03-17
### Added
- Fesh 0.24.0

## [0.23.0] - 2025-02-16
### Fixed
- include FSharp.Core.xml

## [0.22.0] - 2025-02-15
### Changed
- Update to [Fesh 0.22.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md)

## [0.21.0] - 2025-02-05
### Changed
- Update to [Fesh 0.21.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md)

## [0.20.0] - 2025-01-12
### Changed
- Create installer with Velopack
- Enable  automatic Update
- Add Code Signing via Azure Trusted Signing
- Update to [Fesh 0.20.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md)

## [0.14.1] - 2024-11-07
### Changed
- allow to build with different Revit versions

## [0.14.0] - 2024-11-04
### Changed
- First public release
[Unreleased]: https://github.com/goswinr/Fesh.Revit/compare/0.35.1...HEAD
[0.35.1]: https://github.com/goswinr/Fesh.Revit/compare/0.35.0...0.35.1
[0.35.0]: https://github.com/goswinr/Fesh.Revit/compare/0.34.0...0.35.0
[0.34.0]: https://github.com/goswinr/Fesh.Revit/compare/0.32.3...0.34.0
[0.32.3]: https://github.com/goswinr/Fesh.Revit/compare/0.32.0...0.32.3
[0.32.0]: https://github.com/goswinr/Fesh.Revit/compare/0.31.0...0.32.0
[0.31.0]: https://github.com/goswinr/Fesh.Revit/compare/0.30.2...0.31.0
[0.30.2]: https://github.com/goswinr/Fesh.Revit/compare/0.30.1...0.30.2
[0.30.1]: https://github.com/goswinr/Fesh.Revit/compare/0.29.3...0.30.1
[0.29.3]: https://github.com/goswinr/Fesh.Revit/compare/0.29.0...0.29.3
[0.29.0]: https://github.com/goswinr/Fesh.Revit/compare/0.28.1...0.29.0
[0.28.1]: https://github.com/goswinr/Fesh.Revit/compare/0.28.0...0.28.1
[0.28.0]: https://github.com/goswinr/Fesh.Revit/compare/0.26.4...0.28.0
[0.26.4]: https://github.com/goswinr/Fesh.Revit/compare/0.26.3...0.26.4
[0.26.3]: https://github.com/goswinr/Fesh.Revit/compare/0.25.0...0.26.3
[0.25.0]: https://github.com/goswinr/Fesh.Revit/compare/0.24.1...0.25.0
[0.24.1]: https://github.com/goswinr/Fesh.Revit/compare/0.24.0...0.24.1
[0.24.0]: https://github.com/goswinr/Fesh.Revit/compare/0.23.0...0.24.0
[0.23.0]: https://github.com/goswinr/Fesh.Revit/compare/0.22.0...0.23.0
[0.22.0]: https://github.com/goswinr/Fesh.Revit/compare/0.21.0...0.22.0
[0.21.0]: https://github.com/goswinr/Fesh.Revit/compare/0.20.0...0.21.0
[0.20.0]: https://github.com/goswinr/Fesh.Revit/compare/0.14.1...0.20.0
[0.14.1]: https://github.com/goswinr/Fesh.Revit/compare/0.14.0...0.14.1
[0.14.0]: https://github.com/goswinr/Fesh.Revit/releases/tag/0.14.0
