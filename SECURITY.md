# Security Policy

## Supported Versions

Security fixes are provided for the latest public v2.x release of Football Logo Downloader.

| Version | Supported |
| --- | --- |
| Latest v2.x | Yes |
| Older v2.x | Upgrade recommended |
| v1.x PowerShell releases | No |

## Security Model

Football Logo Downloader is a non-privileged Windows desktop application.

The current release is designed to:

- run as the current Windows user and never request administrator elevation;
- avoid listening ports, local servers and background services;
- avoid executing downloaded SVG content;
- avoid launching PowerShell, CMD, BAT or VBS during normal application use;
- avoid collecting or storing passwords, API keys or account credentials;
- restrict application network requests to HTTPS FootyLogos endpoints;
- use normal Windows/.NET TLS certificate validation with certificate revocation checking;
- limit the size of remote HTML and SVG responses;
- parse SVG as XML with DTD and external entity resolution disabled;
- reject active SVG content such as scripts, `foreignObject`, event handlers and external resource references;
- sanitize remotely derived filenames and restrict download paths;
- write downloads through temporary files before replacing final SVG files;
- apply finite regular-expression timeouts to untrusted web content.

See [`SECURITY_REVIEW.md`](SECURITY_REVIEW.md) for the v2.0.0 engineering review.

## Release Integrity

Official release builds should publish:

- `Football Logo Downloader.exe`;
- `SHA256SUMS.txt`;
- `THIRD_PARTY_NOTICE.txt`;
- the packaged release ZIP.

Users should verify the EXE hash against the checksum from the same GitHub Release.

Authenticode signing is recommended for public releases. Signing keys and credentials must never be committed to the repository. See [`docs/CODE_SIGNING.md`](docs/CODE_SIGNING.md).

## Reporting a Vulnerability

Please do **not** publish working exploit details in a public issue before a fix is available.

Preferred reporting method:

1. Use GitHub's private vulnerability reporting / Security Advisory feature when available for this repository.
2. If private reporting is unavailable, contact the repository owner through GitHub and request a private channel before sharing sensitive exploit details.

Please include, when possible:

- affected application version;
- Windows version;
- reproducible steps;
- expected and observed behavior;
- relevant logs or screenshots;
- a minimal sample file or URL required to reproduce the issue.

## Disclosure

After a fix is available, a security advisory or release note may be published with appropriate technical detail and credit, subject to the reporter's preference.

## Scope Note

No security review can prove that software is impossible to exploit. The controls above are intended to reduce realistic risks for this application's limited scope.
