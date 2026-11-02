# AgeCheck 1.6.3 - Names with unknown truncation flags

- Show a scanned first and last name when its AAMVA truncation flag is `U` (unknown), with a clear reminder to compare the name with the printed card. Names explicitly marked as truncated (`T`) remain unavailable for copying.
- Corrected scanner-text recovery for non-driver ID subfiles that begin with the expiration-date field. The `ID` marker next to `DBA` no longer looks like a second `DDB` revision-date field.
- Added fictional regression tests for complete IDs, damaged scanner formatting, unknown name flags, dates and safe handling of genuinely truncated names. No customer's barcode or personal details were added to the repository.

AgeCheck calculates age and expiration from barcode fields. It does not authenticate the physical ID or replace a comparison of the card, photograph and customer.
