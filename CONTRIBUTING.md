# Contributing

Thanks for your interest in Football Logo Downloader.

## Before Opening an Issue

- Use the latest public release.
- Check existing issues for the same problem.
- For security vulnerabilities, follow [`SECURITY.md`](SECURITY.md) instead of posting exploit details publicly.

## Bug Reports

Useful bug reports include:

- application version;
- Windows version;
- selected country / competition when relevant;
- exact steps to reproduce the issue;
- expected and observed behavior;
- screenshots or error messages with private information removed.

## Pull Requests

Keep pull requests focused and explain the user-facing reason for the change.

For code changes:

- target the current .NET/WPF architecture;
- avoid adding new runtime dependencies unless they provide clear value;
- preserve Turkish and English UI behavior;
- preserve non-admin operation;
- do not weaken URI, SVG or path validation;
- update documentation when behavior changes.

Before submitting, build the project and run the relevant security checks described in [`RELEASE_SECURITY_CHECKLIST.md`](RELEASE_SECURITY_CHECKLIST.md).

## Third-Party Marks

Do not add third-party football logos, badges or trademark assets to the repository as bundled application content unless the project has appropriate permission to distribute them.
