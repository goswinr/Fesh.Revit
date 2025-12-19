# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.30.2] - 2025-12-19
### Fixed
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


[Unreleased]: https://github.com/goswinr/Fesh.Revit/compare/0.30.1...HEAD
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

