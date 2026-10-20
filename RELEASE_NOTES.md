# AgeCheck 1.0.2 — Better scanning, a fresh look, and flexible screens

**Download `AgeCheck-win-x64-Setup.exe` below to install.** Already on 1.0.1? Open **Updates** in the app, download the update, and restart when ready.

## Scanner fix

Some USB scanners send the barcode as one long line, leaving out the header or line breaks. Older versions stopped with an unsupported-header message.

AgeCheck can now recover the date fields from supported scanner text:

1. Scan the barcode with **Scan ID** open.
2. If you see **Check the printed dates**, compare both dates with the physical card.
3. Click **Both dates match the card** to see the age and expiration result.

**Recovered text is a date-only check.** The app cannot verify that the full barcode arrived when its header or separators are missing. It will not give an automatic pass from that text. Missing, conflicting, or invalid dates still need manual review.

## A new look

- Compact top navigation instead of the fixed sidebar.
- Shorter result headings and clearer explanations.
- Birth and expiration dates in their own easy-to-read panel.
- Updated colors, spacing, buttons, and brief transitions.
- A new **Scanner recovery** demo with made-up data.

## More screen sizes

- The date panel moves below the result in narrow windows.
- Text and buttons wrap instead of running off the side.
- Small or short windows scroll so controls remain reachable.
- The starting window fits the available desktop space.
- Per-monitor DPI awareness supports Windows display scaling.

## Still private

Scans stay on your PC. No scan history is saved. Dates clear after 30 seconds, when you leave the window, or on the next scan. Recovery confirmation clears too.

## Which download?

| File | Use |
| --- | --- |
| **AgeCheck-win-x64-Setup.exe** | Recommended installer, with in-app updates. |
| AgeCheck-win-x64-Portable.zip | Try without installing. No in-app updates. |
| `.nupkg` and `.json` files | Update files; you do not need to open them. |

Version 1.0.0 users still need the one-time uninstall/reinstall because the package identity changed in 1.0.1.

## Checks and limits

Automated checks use only made-up data. They cover scanner recovery, duplicate and incomplete dates, privacy clearing, and layouts at several window sizes. Physical scanner and mixed-monitor testing are still needed. The installer is unsigned, so Windows may show an unknown-publisher warning.

**ID authenticity not verified.** Compare the printed card, photograph, and customer. A barcode result does not approve a sale.

[Scanner setup](https://github.com/PrithviGhale/ID-Reader/blob/main/docs/SCANNER_SETUP.md) · [Validation details](https://github.com/PrithviGhale/ID-Reader/blob/main/docs/VALIDATION.md)
