# AgeCheck 1.6.1 - Older New York barcode support

- Added checked support for AAMVA version 03 barcodes, including the older `DCT` given-name field and padded names. This covers the two-subfile New York layout reported after version 1.6.0.
- The scanner now accepts that layout when line breaks are preserved but the header record separator is lost, provided the complete directory, subfiles, dates and fields validate. Truncated or conflicting scans still require manual review.
- Added a fictional version 03 New York test for both complete and scanner-labeled input. No customer barcode or personal details were included in the code or tests.
- Age and expiration are calculated from barcode data; staff still need to compare the physical card, photograph and customer.
