# AgeCheck 1.6.0 - New desk layout and barcode text checks

- Redesigned the app with a side navigation rail on wide screens, a new scan dashboard, updated icons and colors, a framed logo, and a clearer result layout. Light and dark themes remain available.
- Buttons now give a brief glow and movement on hover. The scan indicator moves while input is arriving; opening and page transitions remain short and respect the motion setting and Windows animation preference.
- Added **Photo + text**: paste complete PDF417 barcode text into the app and check it with the same local age and expiration rules as a scanner input. Pasted text clears from the editor after checking; the session result remains copyable until cleared.
- The issuer badge now shows a state abbreviation and name for recognized US issuers, with a typography treatment for New Hampshire, California and Florida. It uses the barcode issuer code, not the address state, and is not an authenticity mark.
- Reduced the initial quiet period for keyboard scans from 700 ms to 500 ms. Incomplete scans still wait for more input and require manual review when data is missing.
- Rechecked barcode tests, image decoding, editor markup, motion, privacy behavior and 300 light/dark layout scenarios. Barcode data alone cannot establish that a physical ID is genuine.
