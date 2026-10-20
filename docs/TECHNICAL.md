# Technical details

## Privacy and performance

- ID processing is local and works offline. Raw barcodes, addresses, license numbers and images are not saved. A bounded session history holds only first/last names, scan times, demo/confirmation flags and result categories in memory; it is never written to disk.
- Current input and results clear after 30 seconds, on the next scan, when changing pages, and when the app loses focus. Session history intentionally remains until explicitly cleared, disabled or the app closes.
- Only non-sensitive preferences (startup update checks, light/dark appearance, motion and the history-enabled switch) are stored in `%LOCALAPPDATA%\AgeCheck\settings.json`.
- Copy name is an explicit clipboard action. Copied names can persist outside the app and follow Windows clipboard settings; they are not cleared on focus loss so they can be pasted into a rewards form.
- No analytics or application crash reporting. Update requests contain no scanned ID data. GitHub receives ordinary network metadata when checking/downloading updates.
- No browser engine, global keyboard hook, background service or continuous animation. Input and one-shot timers drive the interface; all scan timers stop when idle.
- Page/result transitions last 340 ms; the opening reveal lasts 520 ms. They respect the motion switch and Windows preference, use opacity/translation/scale, and stop at their base values. The drawer and edge-color transitions are finite too.

Clearing removes application references and visible data; .NET garbage collection and Windows memory management do not provide a guarantee of immediate secure memory erasure. Avoid collecting process dumps while real IDs are in use.

## Supported barcode subset

### Names, checks and history in 1.1.0

Strict fields use DAC/DCS for first/last names, with DDE/DDF truncation flags respected when present. Flattened recovery requires unambiguous standard field boundaries and rejects field-code fragments masquerading as a name. Names are normalized to uppercase and exclude DAD middle names. Missing, truncated or ambiguous values are not copied as complete names.

Optional card revision dates are checked for validity and consistency with issue dates; optional sex/compliance/truncation code values are checked against their supported enumerations. These are inconsistency checks, not authentication. A compliance value of N does not imply a fake.

`SessionHistory` caps entries at 100, updates confirmation by an app-generated scan ID without creating duplicate rows, and immediately clears when disabled. It has no persistence code. History UI uses a virtualized list. Developer clipboard tests replace the clipboard writer and do not overwrite the user's real clipboard.

### Window shell

WPF WindowChrome provides an integrated caption and resize border without a transparent layered window. Custom minimize/maximize/restore/close controls remain in the app header. DWM rounded corners are requested where supported; maximized frames are square. The left drawer overlays the page and pauses scanner capture while open. Status edges use finite brush-color transitions and a thin translucent border instead of a whole-window blur effect.

### Header recovery in 1.0.3

The assisted path recognizes the specific composed/decomposed accented `@ÂNSI` token at the start after an optional supported symbology prefix. This narrow repair happens before the ASCII check; other non-ASCII input is still rejected. Strict mode does not repair the header or auto-pass flattened data. See [research and references](SCANNER_RESEARCH.md) for the diagnosis, official references and limits.

### Themes and motion

Frozen brushes with WPF dynamic resource references cover the entire visual tree, including results and scrollbars. Changing appearance updates those resources and saves the selected theme; older preferences with only startup-check settings still load. Developer modes use a separate preferences file under `artifacts/` and never overwrite real user preferences.

Opening and page transitions use finite fade/translation animations with `FillBehavior.Stop`. Starting a new transition cancels the prior opacity/translation animation for that element. Reduced motion cancels active effects and leaves controls at full opacity with zero offset. `--motion-test` checks opening, page changes, rapid tab changes and cancellation. Theme contrast checks require a ratio of at least 4.5 for primary and muted text on the main surfaces.

### Scanner compatibility in 1.0.2

Strict frame validation remains unchanged. The UI additionally enables an operator-assisted recovery path for recognized flattened name-field/ANSI prefixes. It extracts each required date/country marker only when unique and followed by a complete value and a plausible next field boundary. The same calendar and consistency checks run on recovered values. It never marks recovery as a passed scan: an explicit printed-date comparison is required, and the result remains labeled as date-only. With separators missing, completeness and field boundaries cannot be proved; duplicate or malformed candidates are rejected conservatively. Calling `Barcode.Check` without its recovery option retains strict behavior.

### Display layout

The app declares PerMonitorV2 DPI awareness. Its width-aware layout stacks the date panel below the result below 800 logical pixels, reduces padding below 600, and scrolls vertically on short displays. Startup dimensions are capped to the Windows working area. Rendering tests exercise logical viewports from 480 to 2560 pixels wide, including the effective viewport sizes of 1080p at 125%, 150%, and 200% scaling. This is layout simulation, not physical mixed-monitor testing.

Run the synthetic rendering checks with `AgeCheck.exe --layout-test`; previews and the report are written under `artifacts/layout/` and `artifacts/layout-results.txt`. No real ID data is used.

AgeCheck validates AAMVA `ANSI ` headers, version **08, 09 or 10**, subfile offsets/lengths and exactly one `DL` or `ID` subfile. `DCG`, `DBB`, `DBA` and `DBD` are required for country, birth, expiration and issue dates. Duplicate fields, overlapping subfiles, missing data, invalid dates and unexpected trailing content require review. It is an age/date checker, not a validator of every mandatory AAMVA identity field.

Dates are explicitly `MMddyyyy` for `USA` and `yyyyMMdd` for `CAN`; the app never guesses a country. Dates use the PC's local calendar date, so keep the system clock correct. The expiration date is treated as valid through that day. A February 29 birthday reaches 21 on March 1 in a non-leap year; February 28 in that boundary year always requires manual review because jurisdiction-specific policy can differ. Expiration takes priority in the displayed outcome.

The capture limit is 16,384 characters and 20 seconds. After 700 ms without input, the app validates the **whole frame**; an embedded newline does not submit a scan. A scan beep is not proof that every character arrived. Unsupported versions (including newer formats), modified scanner output and non-AAMVA documents require manual review.

## Updates

Open **App updates** to check for stable releases, read release notes, download an update, then choose **Update and restart**. You can return to scanning and postpone restart. Startup checks can be disabled. Failed checks/downloads leave offline checking available.

Updates use Velopack's `GithubSource` and package verification, over HTTPS, from this public repository. No tokens are embedded. Development and portable copies explain that the Setup installer is required for in-app updates.

**Pushing ordinary code does not update installed apps. Publishing a new versioned GitHub release does.** See the [release guide](RELEASING.md). The repository URL and package identity live in one place: [`release-config.json`](../release-config.json).

## Build and test

On Windows with the .NET 10 SDK:

```powershell
dotnet restore
dotnet test -c Release
dotnet run --project src/AgeCheck
./scripts/package.ps1 -Version 1.0.3
```

The packaging script produces the self-contained application, installer and update assets under `artifacts/`. It uses the pinned Velopack CLI version in `dotnet-tools.json`.

For synthetic UI checks after a Release build:

```powershell
Start-Process ./src/AgeCheck/bin/Release/net10.0-windows/AgeCheck.exe -ArgumentList '--self-test' -Wait
Get-Content artifacts/ui-test-results.txt
```

`--screenshot` renders synthetic screen previews to `artifacts/screenshots/`. These explicit developer modes are never run during ordinary scanning.

## Verification and limits

Automated tests cover exact age boundaries, leap days, expiration day, both supported date conventions, invalid/conflicting/missing/duplicate fields, every truncated prefix of a sample, framing, multiline input, suffixes, control substitutions and buffer limits. The UI self-test exercises scan states, next-scan clearing, the real privacy timer, deactivation handling and the uninstalled-update path.

Physical testing with a Honeywell **1950GHD-2-INT-N**, varied jurisdiction barcodes, and an installed-version-to-newer-version update remain unverified. The test inputs are synthetic. Check [validation notes](VALIDATION.md) before deploying at a counter.

## References and license

- [Honeywell Xenon XP 1950g documentation](https://automation.honeywell.com/us/en/products/productivity-solutions/barcode-scanners/general-purpose-handheld/xenon-xp-1950g-general-duty-scanner)
- [AAMVA 2020 card design standard, Annex D](https://www.aamva.org/getmedia/99ac7057-0f4d-4461-b0a2-3a5532e1b35c/AAMVA-2020-DLID-Card-Design-Standard.pdf)
- [Current AAMVA standards](https://www.aamva.org/topics/driver-license-and-identification-standards)
- [Velopack integration](https://docs.velopack.io/getting-started/csharp), [GitHub sources](https://docs.velopack.io/integrating/update-sources), [release workflow](https://docs.velopack.io/distributing/github-actions), [code signing](https://docs.velopack.io/packaging/signing)

MIT licensed. See [LICENSE](../LICENSE).
