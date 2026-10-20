# Validation record

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
