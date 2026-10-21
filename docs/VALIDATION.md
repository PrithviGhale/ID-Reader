# Validation record

## VS Code markup-generation repair (October 21, 2026)

- The active editor logs reported `Cannot find the type 'Outcome'` during WPF's editor markup pass. A probe that withholds the referenced project assembly reproduced `MC3050` and generated no `MainWindow` source, explaining the cascading missing-control errors.
- Replaced external `x:Static` enum references in history DataTriggers with literal values. The same probe now generates all **63 named controls**, without warnings. Normal builds retain their usual generated-source handling; no hand-written control declarations, diagnostic exclusions or suppression are used.
- `scripts/check-editor.ps1` checks every named control in a fresh intermediate directory. Packaging runs this regression check. The WPF self-test additionally checks that adult/underage/expired history rows still receive their expected colors at runtime.
- Debug build: zero warnings/errors. All 137 core tests passed. This is a source/editor repair; it does not require users of the installed app to reinstall version 1.1.1.

## Version 1.1.1 keyboard transport regression (October 21, 2026)

- Reproduced the two supplied private scanner strings outside the repository. Both recover the expected birth/expiration dates and names, require printed-date confirmation, and then calculate 21+. The original exact strings already recovered in 1.1.0; its reported header error could not be reproduced from the pasted strings alone.
- Found and fixed rejection of partially preserved keyboard header separators and accented-header tokens after line breaks. The header is normalized only for assisted recovery; dates, names and countries are not rewritten. Recovery never automatically passes a card.
- Added synthetic regression tests for mixed separators, unsupported mutations, incomplete inputs, joined cards, duplicate fields and value-encoding changes.
- Added checks against the public fictional example in Annex D.13 of the AAMVA 2020 standard. The transcribed directory mismatch remains blocked. With a consistent directory, the same fields produce 21+ before expiry and Expired after it. Flattened text with ambiguous marker collisions remains manual review.
- Private real-ID text stays outside the repository, release assets and public test fixtures. Public tests and screenshots contain synthetic or public specimen data only.
- **137 core test cases passed.** Debug and Release solution builds completed with **zero compiler warnings and errors**; the reported 167 editor problems were not reproducible in this checkout.
- The self-contained **1.1.1 packaged application passed its WPF self-test**, including character-by-character mixed key/text input, correct adult/underage outcomes after confirmation, next-customer replacement, focus clearing, the real 30-second privacy timer, history/clipboard behavior and safe uninstalled-update handling. A separate private harness passed both supplied exact streams through WPF routed text events and confirmed their 21+ results.
- **220 layout cases passed** across two themes and ten viewports; the narrow recovery/confirmation view was visually inspected. Installer, portable archive, full update package and win-x64 feed were built. A short packaged idle sample used **0.0 ms CPU over five seconds** and **244.8 MiB working set**; this is a measurement, not a performance guarantee.
- Physical Honeywell input and a complete installed-to-installed upgrade remain deployment checks. A build-machine TLS/download restriction was resolved locally by restoring the official runtime packages into the writable workspace; certificate checks were not disabled. GitHub CI uses the standard package sources.

## Version 1.1.0 names, history and integrated window

- **109 core tests passed.** Added first/last-name extraction, all-caps normalization, truncation/malformed-name protection, optional revision/field-code checks and history replacement/capacity/clearing. A malformed flattened-name test exposed a field-code-as-name bug; the recovery boundary logic was tightened.
- The self-contained WPF app passed its self-test for automatic second-scan replacement without a Next button, first/last-only clipboard output through a test writer, history updates without duplicate confirmation rows, demo labeling, history disabling/clearing and the saved preference. Scanner tests wait for completion with a bounded timeout instead of assuming background work always finishes within one second; the app's 700 ms quiet-period timer is unchanged.
- Exactly-21 green, under-21 red, expired red and review-orange window-edge targets are checked.
- Integrated WindowChrome, maximize and restore controls are exercised; the restored frame returns to rounded corners.
- Opening/page zoom/slide transitions, rapid tab changes, drawer reveal/close and reduced-motion cancellation passed the motion checks.
- **220 layout cases passed** across both themes and ten logical viewport sizes, including names, history, open navigation, settings and all result states. Small-screen and desktop renders were visually inspected.
- History contains only names, timestamps, result categories and demo/confirmation flags, is capped at 100, and has no file persistence. Developer tests never write real ID data or overwrite the user's clipboard.
- The final packaged app's short post-test idle sample used **15.6 ms CPU over five seconds** and **214.7 MiB working set** on this machine. This is a short measurement, not a performance guarantee. There are no continuous app animations or history polling loops.
- Physical scanner, physical mixed-monitor and full installed-version update testing remain unverified. Native drag/resize and OS-dependent corner appearance should also be checked on deployment machines.

## Version 1.0.3 accented header, themes and motion

- **89 automated parser tests passed.** Added the reported accented-header structure using unrelated synthetic values, decomposed accents, supported prefixes, country-specific dates/suffixes, malformed and repeated fields, unsupported header mutations and prevention of broad transliteration.
- The WPF self-test sends the synthetic accented-header payload in chunks through the routed text-input handler. Dates display and confirmation succeeds. Capture on other pages stays disabled.
- Light/dark selection, saved preferences, legacy settings migration, animation preferences and primary/muted text contrast (at least 4.5) are checked.
- Opening, page transitions, rapid tab switching and reduced-motion cancellation are checked in the live WPF window. Animations return to full opacity and zero translation.
- **180 layout cases passed**: both themes, ready/passed/under-21/expired/review/recovery states and setup/updates/settings across ten logical viewport sizes. Screenshots use synthetic data only. The images were visually reviewed, including narrow settings and scanner recovery.
- The final self-contained packaged application passed the UI self-test, including real visual-tree theme changes and the accented-header routed input. A five-second idle sample measured **0.0 ms CPU** and **209.8 MiB working set** on this machine; this is a short measurement, not a performance guarantee.
- Actual physical scanner and mixed-monitor testing and a complete installed-version update remain unverified. The specific header repair is based on the supplied transport pattern; it is not a claim that all scanner mutations are supported.

## Version 1.0.2 scanner recovery and layout

- **72 automated core tests passed.** Added flattened scanner output, USA/CAN recovery, supported missing-prefix variants, unique field requirements, malformed/duplicate/missing/conflicting dates, partial inputs, and prevention of automatic passes from recovery.
- WPF self-test passed: recovery dates display, explicit operator confirmation, preserved DEMO labeling, focus-loss clearing of pending confirmation, the real 30-second privacy timer, and the existing scan states.
- **60 layout cases passed** across 10 logical viewports: 480×640, 640×480, 800×600, 1024×768, 1366×768, 1920×1080, 2560×1440, 960×540, 1280×720 and 1536×864. Each tests ready, passed, review, recovery, setup and update screens. Checks reject horizontal overflow and text outside the content area, and verify the recovery button remains reachable by scrolling.
- Rendering uses an offscreen WPF presentation source so large previews are not clipped by the test machine's desktop. Small-screen and desktop renders were visually inspected. The viewport sizes include effective 1080p sizes at 125%, 150% and 200% scaling. Actual monitor changes and physical DPI behavior remain unverified.
- A short idle sample after the UI self-test used **0.0 ms CPU over 5 seconds** and **192.5 MiB working set**. This is one measurement, not a guarantee for every PC.
- Only synthetic barcode content was used in tests and screenshots. No customer ID data was added to the repository.
- The physical Honeywell scanner and an installed-to-installed update are still unverified. This release adds assisted recovery, not a claim of complete barcode validation when separators are missing.

## Version 1.0.1 branding update

All 46 core tests passed again. The self-contained application and installer were rebuilt with `AgeCheck` as the product, company, package ID and package author. The new update channel is `win-x64`; users of 1.0.0 need a one-time reinstall. No scan-checking logic changed in this release.

## Initial release

Initial validation: October 19, 2026, Windows x64 with .NET SDK 10.0.302.

- Release build: zero warnings and errors.
- Automated core tests: 46 passing cases, including a loop over every truncated prefix and 500 deterministic malformed inputs. All inputs synthetic.
- WPF self-test: receiving state, multiline buffering, complete/partial scans, next-scan privacy clearing, demo marker, deactivation handler, overflow latch, 30-second timer and development update handling passed.
- Idle: all scan timers stopped; one 5-second sample measured **0.0 ms CPU time**, with **192.8 MiB working set** after the self-test. This is a short sample on one machine, not a hardware-independent performance guarantee. The self-test includes runtime/JIT and UI-test overhead.
- Ready, passed, under-21, expired, manual-review, setup and update screens rendered using WPF. Button contrast corrected after inspection.
- Self-contained win-x64 executable and Velopack Setup, portable archive, full update package and release feeds built locally.

## Not yet verified

- A physical Honeywell 1950GHD-2-INT-N scanner and its firmware's exact control-character transport. Honeywell's current user-guide download required access permissions.
- Real jurisdiction barcode variations, Windows keyboard layouts other than the recommended matched US layout, and mixed display scaling on multiple monitors.
- A complete installed-version-to-newer-version update through public GitHub Releases, interrupted downloads or rate-limiting in an installed copy.
- Windows code signing: the initial installer is unsigned, with supported signing configuration documented separately.

The self-test calls the deactivation handler directly; it is not a substitute for a physical Alt-Tab / scanner test. Core framing tests plus the UI buffer test do not prove hardware compatibility. See [release acceptance steps](RELEASING.md) and [scanner setup](SCANNER_SETUP.md).
