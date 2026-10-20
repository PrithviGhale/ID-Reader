# AgeCheck 1.0.3 — Scanner fix, light & dark, and a new feel

**Already using 1.0.1 or 1.0.2? Open Updates → Check for updates → Download update → Update and restart.**

For a fresh install, download **`AgeCheck-win-x64-Setup.exe`** from Assets below.

## The scanner fix

Some keyboard scanners send a flattened barcode beginning with `@ÂNSI`. Version 1.0.2 rejected the accented header before it could recover the dates. This release handles that specific header variation, including its decomposed form and the supported scanner prefix.

Scan on the **Scan** page. If the scanner removed the normal formatting, compare the displayed birth and expiration dates with the printed card, then click **Both dates match the card**. Missing, invalid or conflicting dates still require manual review. Recovered text is a date-only check; it does not prove that the whole barcode arrived.

## A fresh interface

- New graphite-and-mint dark mode and a clean light mode.
- A **Settings** page that saves your appearance and motion choices.
- Clear scan, setup, update and settings pages with readable text and flexible layouts.
- Theme-aware results, buttons and scrollbars.
- A brief opening reveal and gentle fade-and-slide page changes.
- An animation switch, plus respect for the Windows animation preference.
- Animations finish and settle; there are no constantly running effects.

## Tested with made-up IDs

- **89 automated parser tests**, including the reported header pattern, US/Canadian dates, missing separators, control substitutions, duplicates and truncated scans.
- A WPF input test that sends the synthetic accented-header scan through the app's text-input handler, displays the dates and confirms the result.
- Theme switching, preference saving, legacy settings, text contrast and privacy clearing.
- Opening, tab transitions, rapid tab changes and reduced motion.
- **180 layout cases** across ten small/large viewport sizes in both themes.

Physical Honeywell scanner testing, physical mixed-monitor testing and a full installed-version update test remain outstanding. The installer is unsigned, so Windows may show an unknown-publisher warning.

## Download guide

| File | Use |
| --- | --- |
| **AgeCheck-win-x64-Setup.exe** | Recommended installer, with in-app updates. |
| AgeCheck-win-x64-Portable.zip | Try without installing; no in-app updates. |
| `.nupkg` and `.json` files | Used by the updater; you do not need to open these. |

Windows x64 is required. Version 1.0.0 users still need the one-time uninstall/reinstall because the app identity changed in 1.0.1.

**ID authenticity not verified.** Compare the printed card, photograph and customer. A barcode result does not approve a sale.

[Scanner setup](https://github.com/PrithviGhale/ID-Reader/blob/main/docs/SCANNER_SETUP.md) · [Research and references](https://github.com/PrithviGhale/ID-Reader/blob/main/docs/SCANNER_RESEARCH.md) · [Test details](https://github.com/PrithviGhale/ID-Reader/blob/main/docs/VALIDATION.md)
