# AgeCheck 1.2.5 - More reliable two-section scans

- A complete two-subfile keyboard scan can now recover a first-name field ending in D without mistaking its last letter for an extra field. The encoded directory length and strict field/date checks must still agree.
- Scans that preserve labeled field separators but omit a header control character can be checked directly after reconstructing that fixed header, provided the entire barcode passes the strict parser.
- Added synthetic tests for both scanner transports and incomplete input. Manual review remains required for damaged or ambiguous data; the barcode does not prove the physical ID is genuine.
