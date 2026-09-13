# Changelog

All notable changes to Football Logo Downloader are documented here.

## [2.0.0] - 2026-09-13

- Fixed country discovery so popular links do not hide England, Spain, Germany or Brazil from the country selector.

### Added

- Complete rebuild as a C# / .NET 10 / WPF Windows desktop application.
- Standalone self-contained `win-x64` executable.
- Modern dark, light and system appearance modes.
- New FLD application branding and icon.
- Turkish and English interfaces.
- Download progress, detailed results and completion status.
- Direct **Open Folder** actions.
- Local persistence for language, theme, output folder and recent selections.
- Release SHA-256 checksum generation.
- GitHub Actions Windows build workflow.
- CodeQL analysis and Dependabot configuration.
- Security and privacy documentation.

### Security

- HTTPS-only source policy for application network requests.
- Controlled redirect handling.
- Response-size limits and request timeouts.
- XML/SVG validation with DTD and external entity resolution disabled.
- Rejection of active SVG content and external resource references.
- Filename/path hardening and temporary-file writes.
- NuGet vulnerability auditing during builds.

### Changed

- Replaced the v1 PowerShell/WinForms runtime architecture with a compiled WPF application.
- Users no longer need to launch a BAT, VBS or PowerShell script.
- Updated release packaging for direct EXE distribution.

## [1.0.2]

- Cleaned the country selector by using canonical country records.
- Removed multilingual duplicate country entries.
- Hid countries without available competition collections.

## [1.0.1]

- Country-list cleanup and minor fixes.

## [1.0.0]

- Initial public PowerShell/Windows Forms release.
- Country and league selection.
- SVG logo downloads.
- Turkish / English interface.
- Unicode filename support.
