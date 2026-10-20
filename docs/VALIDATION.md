# Validation record

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
