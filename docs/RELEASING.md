# Build and publish AgeCheck

## Local build

Install the .NET 10 SDK on Windows x64, then run:

```powershell
./scripts/package.ps1 -Version 1.0.1
```

The script restores the pinned Velopack tool, runs tests, publishes a self-contained Windows app and packages all assets under `artifacts/releases/`. No SDK or separate runtime is required on the customer's machine. Full update packages are used; delta packages are an optional future optimization.

## Publish the next update

1. Make your changes and run the tests.
2. Update `RELEASE_NOTES.md` to describe the new version.
3. Commit and push the changes, then push a new version tag:

```powershell
git add .
git commit -m "Prepare AgeCheck 1.0.2"
git push origin main
git tag v1.0.2
git push origin v1.0.2
```

The Windows GitHub Actions job tests, builds, packages and publishes the tag as a stable release. It uses the workflow's temporary `GITHUB_TOKEN`; never put a token in the app. Wait for the workflow to finish and inspect the release assets. Every release needs the installer, full `.nupkg`, and generated `releases.win-x64.json` / feed files. Keep the generated filenames and upload all output files. An ordinary push does **not** update installed applications.

Only numeric `vMAJOR.MINOR.PATCH` tags are accepted. Increase the version; do not overwrite tags or publish a lower version. Keep `packId`, `channel` and `runtime` in `release-config.json` stable. The same file supplies the trusted repository URL to both the app and publishing script. A fork must deliberately change it before distributing a build; existing installations will continue trusting their configured repository until migrated.

## Signing

The default workflow produces an **unsigned** release. This is disclosed in release notes. It is not represented as a signed production build. Velopack verifies downloaded package integrity; that is distinct from verifying a publisher with a Windows code-signing certificate.

For signed builds, provision an Azure Artifact Signing account and certificate profile, grant the build identity permission to sign, and follow the [Velopack signing guide](https://docs.velopack.io/packaging/signing). Create a metadata JSON file with `Endpoint`, `CodeSigningAccountName` and `CertificateProfileName`, then run:

```powershell
./scripts/package.ps1 -Version 1.0.2 -SigningMetadata ./signing-metadata.json
```

The script passes that file to Velopack's supported `--azureTrustedSignFile` option so its setup/update binaries and application files are signed during packaging. CLI/package versions are pinned together at 1.2.161; use the requirements for that version (newer 1.3 documentation describes a different signing implementation). Configure the required Windows signing tools and Azure authentication in your build environment. In CI, authenticate using a protected identity or secrets and supply the metadata path to the packaging step. No signing credentials belong in source control or the app. Signing has not been exercised in this repository because no signing identity was provided.

## Installed update acceptance test

Use an isolated Windows account or VM:

1. Install the older Setup release and launch it.
2. Publish a strictly newer package with identical identity, architecture and channel to the configured repository.
3. Check for updates; verify the version and notes. Download, postpone, scan synthetic samples, then choose Update and restart.
4. Confirm the new version, preserved update preference and absence of retained scan data.
5. Repeat offline and with interrupted downloads; verify scanning remains usable. Confirm restart cannot happen during receiving/processing.

Do not publish throwaway stable tags to the real repository just for testing: installed users may receive them. An isolated test fork and matching `release-config.json` are preferable. This end-to-end installed update test remains outstanding for the initial release.

## One-time branding change in 1.0.1

Version 1.0.1 uses package ID `AgeCheck` and channel `win-x64`. Version 1.0.0 used the original personal-name package identity and `win` channel. Users of 1.0.0 must uninstall that version and install the new Setup file once. Do not publish new-identity packages to the old channel. Keep the new identity stable for later updates.
