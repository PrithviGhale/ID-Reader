# AgeCheck 1.5.0 - Issuing state and expiration countdown

- Results now show the issuing jurisdiction from the AAMVA issuer code in the barcode header, plus calendar days until expiration or days since expiry. Session History and Image Scanner results show the same details. The address-state field is not treated as the issuing state.
- Added strict support for AAMVA barcode version 11 alongside versions 08-10. Unknown or incomplete formats still require manual review.
- Image Scanner now tries a high-contrast pass on small photo regions after ordinary PDF417 detection fails. It does not guess from an unreadable barcode; photos with insufficient detail still need a sharper capture or physical scan.
- Added tests for issuer-code mapping, expiration boundaries and version 11, and rechecked the offline image decoder. Barcode data does not prove the physical ID is genuine.
