<div align="center">

# AgeCheck

### Scan an ID. Check the age. Keep it private.

A simple Windows app for checking the birth date and expiration date in an ID barcode.

![Windows x64](https://img.shields.io/badge/Windows-x64-176B53?style=flat-square)
![Works offline](https://img.shields.io/badge/Scanning-works_offline-176B53?style=flat-square)
![MIT license](https://img.shields.io/badge/License-MIT-176B53?style=flat-square)

**[Download AgeCheck](https://github.com/PrithviGhale/ID-Reader/releases/latest)** · **[Scanner setup](docs/SCANNER_SETUP.md)** · **[What's new](https://github.com/PrithviGhale/ID-Reader/releases)**

</div>

![AgeCheck app showing the Ready to scan screen](docs/images/ready.png)

## What it does

| Feature | How it helps |
| --- | --- |
| **21+ age check** | Checks whether the barcode birth date says the person is at least 21. |
| **Expiration check** | Shows when the ID's barcode expiration date has passed. |
| **Clear results** | Large text shows 21+, Under 21, Expired, or Manual review. |
| **Date comparison** | Shows the birth date and expiration date so you can compare them with the card. |
| **Private by default** | Processes scans on your PC. Does not save scan history or ID details. |
| **Automatic clearing** | Clears results after 30 seconds, on the next scan, or when you leave the window. |
| **Works offline** | No internet is needed to check a scan. |
| **Built-in updates** | Check for updates, read what's new, download, and restart when you choose. |
| **Simple, smooth design** | A clean Windows interface with short animations and no constantly running effects. |
| **Demo mode** | Try made-up examples without scanning an ID. Each demo is clearly marked. |

## Get started

1. **[Open the latest release](https://github.com/PrithviGhale/ID-Reader/releases/latest)** and download **`AgeCheck-win-x64-Setup.exe`**.
2. Run the installer. Everything the app needs is included.
3. Follow the **[scanner setup guide](docs/SCANNER_SETUP.md)**.
4. Open **Scan an ID**, keep the window active, and scan the barcode on the back of the card.
5. Compare the result with the printed card and the customer. Use **Clear / Next customer** when finished.

**Already using version 1.0.0?** Close and uninstall that version, then install the latest AgeCheck Setup. This one-time reinstall removes the old publisher and installation name. Future releases use the new AgeCheck identity. Your scan history does not need moving because the app does not save any.

## What you need

- A Windows x64 PC.
- A scanner that can read PDF417 barcodes and type them into Windows.
- The setup guide is written for the **Honeywell Xenon XP 1950g**. Physical testing with that scanner is still needed.

AgeCheck supports selected US and Canadian AAMVA barcode formats, versions 08–10. Unsupported, incomplete, or conflicting data asks for manual review. It does not support every ID format.

## Know what the result means

**ID authenticity not verified.** The app checks dates in the barcode. It cannot prove the card is real or approve a sale. Always compare the printed details, photograph, and customer.

The current installer is **unsigned**, so Windows may show an unknown-publisher warning. Physical scanner testing and a full installed-app update test are still outstanding. See the **[test results and limits](docs/VALIDATION.md)**.

## Updates, without rushing you

Open **App updates** to check for a new version. Read the release notes, download it, and choose **Update and restart** when ready. You can turn off startup checks or postpone the update. Scanning still works if the internet is unavailable.

## For developers

Built with C#, .NET 10 LTS, WPF, and Velopack.

```powershell
dotnet restore
dotnet test -c Release
dotnet run --project src/AgeCheck
```

**[Build and publish updates](docs/RELEASING.md)** · **[Technical details](docs/TECHNICAL.md)** · **[Validation](docs/VALIDATION.md)**

Pushing code does not update installed apps. Publishing a new versioned release makes an update available.

---

Free and open source under the **[MIT license](LICENSE)**.
