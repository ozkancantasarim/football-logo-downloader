# Security Engineering Review — v2.0.0

This document records the maintainer's security-focused engineering review performed for the v2.0.0 release.

> This is **not** an independent penetration test, third-party audit or certification. It documents controls implemented in the source code and release process.

## Reviewed Attack Surface

The review focused on:

- remote HTML received from FootyLogos;
- SVG downloads and local file creation;
- HTTP redirects and TLS behavior;
- file and folder names derived from remote content;
- user-selected output paths;
- shell launches used for **Open Folder** and the fixed rights link;
- local settings storage;
- build and release supply-chain controls.

## Implemented Controls

### Network

- FootyLogos requests are HTTPS-only.
- Standard Windows/.NET TLS certificate validation is retained; no certificate-bypass callback is configured.
- Certificate revocation checking is enabled.
- Automatic redirects are disabled. Redirects are followed manually only when the destination remains an HTTPS `footylogos.com` host, with a finite redirect limit.
- Cookies are disabled for the downloader HTTP client.
- Response-header size is limited.
- HTML and SVG bodies have explicit maximum sizes.
- Requests use finite timeouts.

### SVG Validation

- Downloaded content must parse successfully as XML with an SVG root.
- DTD processing is prohibited and external entity resolution is disabled.
- Unexpected SVG namespaces are rejected.
- Active elements such as `script`, `foreignObject`, `iframe`, `object`, `embed`, `audio`, `video` and `canvas` are rejected.
- `on*` event attributes are rejected.
- External `href` and `src` references are rejected.
- Potentially active CSS constructs and external `url(...)` references are rejected.
- Only validated SVGs are written to their final destination.
- Downloads are first written to a randomly named temporary file and then moved to the final path.

### Paths and Files

- Remote slugs are restricted to a conservative character set.
- Filenames are sanitized for Windows-invalid/control characters and reserved device names.
- Output paths are normalized before use.
- Windows device-namespace and UNC/SMB download roots are rejected.
- The application runs without elevation.

### Parsing and Resource Limits

- Regular expressions used on untrusted remote content have finite timeouts.
- A global regular-expression timeout is configured as defense in depth.
- Network response sizes and request durations are bounded.

### Local Data

- The application stores only non-sensitive preferences in `%LOCALAPPDATA%\FootballLogoDownloader\settings.json`.
- No account credentials, passwords, API keys or authentication tokens are required or stored.
- No telemetry or analytics service is integrated.

### Build and Release Supply Chain

- The project targets .NET 10 for the v2 release line.
- The application does not require third-party runtime NuGet packages.
- NuGet vulnerability auditing is enabled for builds.
- CodeQL analysis is configured for push, pull request, scheduled and manual runs.
- Dependabot monitors GitHub Actions and NuGet dependencies.
- Release builds generate SHA-256 checksums.
- GitHub Actions build provenance attestation is configured for CI artifacts.
- Common private-key/certificate extensions are excluded by `.gitignore`.

## Remaining Maintainer-Controlled Item

A trusted Authenticode signature requires a code-signing certificate/private key controlled by the maintainer or an approved signing service. This cannot be stored in the public repository.

See [`docs/CODE_SIGNING.md`](docs/CODE_SIGNING.md).

## Conclusion

The v2.0.0 design intentionally keeps the application non-privileged and narrows network, file and SVG handling to the minimum required for its purpose. These measures reduce attack surface but do not constitute a guarantee that the software can never contain a vulnerability.
