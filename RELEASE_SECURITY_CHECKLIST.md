# Public Release Checklist

Maintainer checklist for every public Football Logo Downloader release.

## Source & Versioning

- [ ] Release is built from a clean commit on `main`.
- [ ] Version number is updated in the project and release notes.
- [ ] `CHANGELOG.md` is updated.
- [ ] `README.md`, `SECURITY.md`, `PRIVACY.md` and third-party notices still match the application behavior.
- [ ] A Git tag matching the release version is created (for example `v2.0.0`).

## Build & Security

- [ ] Build uses a supported .NET 10 SDK patch.
- [ ] `dotnet list package --vulnerable --include-transitive` reports no known vulnerable packages requiring remediation.
- [ ] GitHub CodeQL completes without unresolved high/critical findings.
- [ ] The self-contained `win-x64` release builds successfully from a clean source tree.
- [ ] Final EXE is scanned with current Microsoft Defender definitions.
- [ ] Application is tested without administrator privileges.
- [ ] Country/competition loading and SVG downloading are tested against the live source.

## Signing & Integrity

- [ ] If Authenticode signing is configured, sign and timestamp the **final EXE**.
- [ ] Verify the Authenticode signature after signing.
- [ ] Generate SHA-256 **after signing**.
- [ ] Confirm `SHA256SUMS.txt` matches the exact EXE being uploaded.
- [ ] Never upload signing private keys, `.pfx`/`.p12` files, passwords or signing-service credentials.
- [ ] Prefer a GitHub Actions build with provenance attestation when available.

## Functional QA

- [ ] Test on a supported Windows 10/11 x64 machine.
- [ ] Test dark, light and system appearance modes.
- [ ] Test Turkish and English UI.
- [ ] Test first launch with no existing settings file.
- [ ] Test an existing valid SVG is skipped as expected.
- [ ] Test download completion and **Open Folder**.
- [ ] Confirm FLD branding renders correctly in both light and dark themes.

## Release Assets

Upload only public release artifacts, for example:

- [ ] `Football Logo Downloader.exe`
- [ ] `Football-Logo-Downloader-vX.Y.Z-win-x64.zip`
- [ ] `SHA256SUMS.txt`
- [ ] `THIRD_PARTY_NOTICE.txt`

The GitHub repository contains the source license, privacy policy and security documentation.

## GitHub Account / Repository

- [ ] Maintainer GitHub account has 2FA enabled.
- [ ] Repository collaborators and release permissions are reviewed.
- [ ] Branch protection / review settings are appropriate for the project's maintenance model.
- [ ] Release is marked **Latest** only after the uploaded artifacts are re-downloaded and smoke-tested.
