# Windows Code Signing

Football Logo Downloader can be built without a code-signing certificate, but official public releases should be Authenticode-signed when a trusted signing method is available.

Code signing provides two important properties:

1. Windows can identify the verified publisher associated with the certificate.
2. Windows can detect whether the signed executable was modified after signing.

Code signing does **not** prove that software is vulnerability-free.

## Recommended Release Order

Use this order for signed releases:

```text
Build final EXE
    ↓
Security / malware scan
    ↓
Authenticode sign
    ↓
Timestamp
    ↓
Verify signature
    ↓
Generate SHA-256
    ↓
Create release ZIP
    ↓
Upload release assets
```

The checksum must be generated **after** signing because signing changes the executable bytes.

## Signing Options

Suitable options may include:

- a trusted OV/EV code-signing certificate held in compliant hardware or a signing service;
- a managed cloud signing provider;
- an approved open-source signing service such as SignPath Foundation, when the project meets its eligibility requirements.

Choose a provider that is trusted by supported Windows versions and meets current code-signing key-protection requirements.

## Private-Key Rules

Never commit or upload any of the following to the public repository:

- `.pfx` or `.p12` certificate containers;
- private keys;
- hardware-token PINs;
- certificate passwords;
- cloud-signing secrets;
- signing-service API credentials.

The repository `.gitignore` excludes common private-key/certificate file extensions as defense in depth, but secret management must not rely on `.gitignore` alone.

## Verification

After signing, verify the signature before generating the checksum and release package. On a Windows machine with the Windows SDK installed, `signtool verify` can be used for Authenticode verification.

The release process should fail rather than publish an artifact when signature verification fails.

## GitHub Actions

Do not place a private signing key directly in repository files or workflow YAML.

If automated signing is added later, use the signing provider's supported GitHub integration or protected GitHub secrets/environments with the minimum required permissions. Prefer signing systems where the private key is non-exportable.

## Current Project Status

The source repository is prepared for signing, but no trusted private signing key is included. Until official signing is configured, users should download releases only from the official repository and verify the published SHA-256 checksum.
