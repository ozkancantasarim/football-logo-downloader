# Football Logo Downloader v2.0.0

Football Logo Downloader v2.0.0 is a complete rebuild of the project as a compiled Windows desktop application.

## Highlights

- Rebuilt with C# / .NET 10 / WPF.
- Standalone self-contained Windows x64 executable.
- Modern dark, light and system themes.
- New FLD branding and application icon.
- Turkish and English interfaces.
- Country and competition selection.
- SVG logo downloads with Unicode filename support.
- Download progress, result details and direct **Open Folder** actions.
- Local preference saving for language, theme, output folder and recent selections.
- No API key, account or administrator privileges required.

## Security Improvements

- HTTPS-only application requests to FootyLogos endpoints.
- Controlled redirects and standard Windows/.NET TLS validation.
- Size limits and finite timeouts for untrusted network data.
- XML/SVG validation before files are finalized.
- Rejection of active SVG content and external resource references.
- Filename/path hardening and temporary-file writes.
- NuGet vulnerability auditing, CodeQL and Dependabot configuration.
- SHA-256 release checksum generation.

## Download

Recommended public assets:

- `Football Logo Downloader.exe`
- `Football-Logo-Downloader-v2.0.0-win-x64.zip`
- `SHA256SUMS.txt`
- `THIRD_PARTY_NOTICE.txt`

The packaged ZIP also contains the MIT license and a short usage README.

## Requirements

- Windows 10 or Windows 11 (64-bit)
- Internet connection

The official build is self-contained; a separate .NET installation is not required.

## Source & Rights

Logo/data source: FootyLogos.com.

Football Logo Downloader is not affiliated with, endorsed by, sponsored by, or officially connected with FootyLogos. Third-party football names, logos, badges, crests and trademarks remain the property of their respective rights holders.
