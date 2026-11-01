# AgeCheck 1.6.2 - More barcode layouts

- Added assisted reading for the reported Vermont layout with an empty jurisdiction section whose declared length omits its trailing separator. The name, birth date, expiration and issuing state appear for comparison with the physical card; staff must confirm the printed dates before an age result appears.
- Added complete-frame support for AAMVA PDF417 versions 04–07, alongside the existing 03 and 08–11 layouts, and verified the non-driver ID subfile path.
- Clarified the scan screen when dates are found but printed-card confirmation is needed. The uncommon Vermont recovery uses a quick directory check before any deeper parsing.
- Added fictional regression tests for the reported layout, older versions, malformed dates and truncated or inconsistent frames. No customer barcode or personal details are included in this release.

AgeCheck calculates age and expiration from barcode fields. It does not authenticate the physical ID or replace a comparison of the card, photograph and customer.
