# AgeCheck 1.0.1 — Simple, private ID date checks

## Download and start

**Download `AgeCheck-win-x64-Setup.exe` from Assets below.** Run it to install AgeCheck. You do not need to install .NET or developer tools separately.

Want to try it without installing? Use `AgeCheck-win-x64-Portable.zip`. The portable copy does not support in-app updates.

## What's new

- **Clean AgeCheck branding:** the app's publisher details and new installer names no longer include a personal name.
- **A clearer project page:** screenshots, a simple feature list, and easier setup instructions.
- **Better release notes:** clear download instructions and an explanation of what the app does.

## Features included

- **Check for age 21+:** reads the birth date from a supported ID barcode.
- **Check expiration:** shows if the barcode expiration date has passed.
- **Easy-to-read results:** 21+, Under 21, Expired, or Manual review.
- **Compare dates:** shows birth and expiration dates beside the result.
- **Private scanning:** processes data on your PC and does not save scan history or ID details.
- **Automatic clearing:** clears after 30 seconds, on the next scan, or when you leave the window.
- **Offline use:** scan without an internet connection.
- **Built-in updates:** check, download, and restart when you are ready.
- **Smooth interface:** short animations with no constantly running effects.
- **Try it safely:** made-up demo examples are clearly marked DEMO.

## Coming from 1.0.0?

**Close and uninstall 1.0.0, then install the new Setup file.** This one-time reinstall removes the old publisher and installation name. The new version uses a separate update channel, so 1.0.0 will not install it through the old updater. Future updates keep the AgeCheck identity.

## Which file should I choose?

| File | What it is for |
| --- | --- |
| **AgeCheck-win-x64-Setup.exe** | **Recommended.** Installs AgeCheck with built-in updates. |
| AgeCheck-win-x64-Portable.zip | Try the app without installing. No in-app updates. |
| `.nupkg` and `.json` files | Used by the updater. You do not need to open these. |
| Source code | For people who want to build or change the app. |

## Before using it

- Windows x64 is required. Selected US and Canadian AAMVA formats (08–10) are supported.
- The installer is unsigned. Windows may show an unknown-publisher warning.
- Physical Honeywell scanner testing and an installed-to-installed update test are still outstanding.
- **ID authenticity not verified.** Check the printed card, photograph, and customer. A barcode result does not approve a sale.

[Scanner setup](https://github.com/PrithviGhale/ID-Reader/blob/main/docs/SCANNER_SETUP.md) · [Test results](https://github.com/PrithviGhale/ID-Reader/blob/main/docs/VALIDATION.md)
