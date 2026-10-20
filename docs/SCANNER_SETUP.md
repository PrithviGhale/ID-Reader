# Honeywell Xenon XP 1950g setup

Target hardware: USB Honeywell Xenon XP 1950g, model **1950GHD-2-INT-N**. Hardware testing is still required; the application cannot detect a connection in keyboard mode.

1. Obtain the **Xenon XP Series User Guide** from the [official Honeywell product page](https://automation.honeywell.com/us/en/products/productivity-solutions/barcode-scanners/general-purpose-handheld/xenon-xp-1950g-general-duty-scanner). The page currently lists `195X-EN-UG-01 Rev V`. Its download may require a Honeywell login. Use the revision applicable to your firmware.
2. Connect the USB cable and select **USB PC Keyboard** using that manual's programming barcode.
3. Match the scanner's keyboard country to the Windows keyboard layout. US English on both is the simplest setup for the supported ASCII barcode profile.
4. Enable **PDF417**. Turn off custom data formatting/truncation, extra prefixes and field reordering.
5. Use **Control Character Output** to emit textual control substitutions (`[LF]`, `[CR]`, `[RS]`). AgeCheck restores them before validating original subfile byte positions. The exact setting name/availability must be checked in your firmware's guide. Do not enable a mode that silently drops separators.
6. A suffix of Enter or Tab is accepted after a complete frame. No custom prefix is needed; `]L0` is accepted if symbology IDs are enabled. Other prefixes are rejected.
7. Return to **Scan an ID**, focus AgeCheck and scan synthetic test data first. Real ID data must not be pasted into a text editor, support ticket or online barcode service.

The application also accepts intact literal control characters, but keyboard interfaces can transform these into shortcuts. Text substitution is the recommended transport profile. The manufacturer download was access-restricted during development; the exact programming barcodes have intentionally not been reproduced or invented here.

## Troubleshooting

| Symptom | Try |
| --- | --- |
| Scanner beeps, nothing happens | Click AgeCheck, select Scan an ID, and check USB keyboard mode. The beep is only a decode indication. |
| Unsupported header | Check control substitutions, prefix settings and the keyboard layout. Only AAMVA ANSI versions 08–10 are supported. |
| Truncated or inconsistent length | Disable editing/stripping of control characters. Ensure the entire barcode is transmitted. A pause over 700 ms can terminate a partial transmission. |
| Scan exceeds 20 seconds | Reduce excessive inter-character delays and retry after Clear. |
| Input limit exceeded | Choose Clear and confirm the scanner is transmitting just one ID, not a batch. |
| Valid card still needs review | Compare printed dates manually. Do not treat a scan problem as proof of a counterfeit. Unsupported formats deliberately fail closed. |
| Data disappears | Expected on focus loss, next scan, page change and 30 seconds after a result. |

Never change scanner configuration using programming barcodes intended for another model. Never assume this app certifies authenticity or legal eligibility.
