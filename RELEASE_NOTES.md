# AgeCheck 1.2.0 - A clearer counter and familiar faces

- A redesigned violet/mint interface, a large current-age readout, clearer scan results and a home icon to return to Scan.
- Birthday-today messages, days until the next birthday, and days until 21 for younger customers. Dates are calculated from the current checked scan.
- Updates move into Settings. A red ! badge appears on the gear and update card when a newer release is found.
- Returning-customer recognition survives restarts. It defaults on, stores only an encrypted name, keyed card fingerprint, visit counts and times, and can be turned off or deleted in Settings. No full barcode, raw card number, birth date, address or photo is stored. Up to 5,000 records; oldest scans are pruned at capacity. Demos are never saved.
- History now includes age and seen-before status, with separate session and remembered-customer sections. Current details still clear on the next scan, focus loss and the privacy timer.
- Existing scanning and direct 21+/Under 21/Expired behavior is preserved. A saved record never approves a new scan. ID authenticity remains unverified.

## Previous release

# AgeCheck 1.1.2 - Direct keyboard age results and steadier capture

- Complete supported flattened keyboard scans now show 21+, Under 21 or Expired directly, without a confirmation click. The app checks the encoded directory length, inferred field boundaries, unique required dates, country and date consistency before using this path.
- The automatic compatibility profile requires one DL/ID subfile at offset 31, an initial DAQ and a terminal eight-digit DDB revision date. Ambiguous or unsupported layouts retain assisted recovery or review. A recovered partial card never automatically passes.
- A pause in an incomplete transmission now gets up to 2 seconds for more input instead of immediately submitting after 700 ms. Characters arriving during background parsing stay buffered; stale parse results are discarded.
- History records the direct age outcome. No personal scanner samples are included in public source or tests.
- Includes the WPF editor markup repair and remaining code cleanup. ID authenticity remains unverified; physical scanner and installed-to-installed update checks remain necessary.

## Previous release
# AgeCheck 1.1.1 — Keyboard scanner compatibility

- Recover supported keyboard scanner headers when only some line breaks, tabs or record separators survive, including the known composed/decomposed accented header. Recovered scans still require printed-date confirmation.
- Preserve name/date values during header repair; make the recovered-name comparison reminder explicit.
- Expanded tests cover transformed transport, incomplete/joined scans and a published AAMVA specimen. Two private supplied barcodes were checked through Windows routed input without being added to this repository.
- Bumping the version in Directory.Build.props on main now builds and publishes the matching installer and update feed automatically. Existing Setup installations can use App updates.
- Unsigned Windows x64 release. Physical scanner testing on the deployment PC is still required. ID authenticity is not verified.

## Previous release: AgeCheck 1.1.0 — Names, history, glowing edges, and a new home

**Update inside AgeCheck:** open **☰ → App updates**, check for updates, download, then choose Update and restart. For a fresh install, download **`AgeCheck-win-x64-Setup.exe`** below.

## A new home and window

- A redesigned scan screen with a clear result, printed-card dates and a name-copy area.
- The title area is built into the app, with working minimize, maximize, restore and close controls.
- Rounded window edges where Windows supports them.
- A **☰ menu on the left** opens Scan cards, History, Scanner setup and App updates. Click it again to hide the menu.
- A **settings gear at the top right** opens appearance, motion and history preferences.
- Smoother fade, slide and zoom transitions, plus an animated menu reveal. Effects finish and stop; reduced-motion settings are respected.

## Clear color signals

- **Green edges:** the birth date indicates age 21 or older and the date/field checks pass.
- **Red edges:** under 21 or expired.
- **Orange edges:** incomplete, conflicting or unusual data needs manual review.

Text explains the result too. Orange does not mean “fake,” and green does not verify authenticity. Recovered scanner text still requires printed-date confirmation before a passing date result.

## Names and rewards sign-ups

Readable first and last names appear in **ALL CAPS**. Click **Copy name** to paste into your rewards form. Middle names are excluded. Missing, shortened or ambiguous names are shown as unavailable instead of guessed. Compare the name with the physical card first.

**No Next customer click is needed.** Scan another card on the Scan cards page and it replaces the previous result automatically. Clear result remains available if you want to clear manually.

## New: session history

History keeps up to **100 first/last names, scan times and results in memory** so you can see who you scanned during this app session. Confirming recovered dates updates the same row. Demo entries are clearly marked.

- History is on by default in this version.
- **Clear history** removes it immediately.
- Turning history off in Settings also clears it.
- Closing AgeCheck clears the session; history is not saved to disk or restored after restart.
- History does not keep birth dates, expiration dates, addresses, ID numbers, images or raw barcodes.
- Copying a name exports it to your Windows clipboard, where it may remain after the app closes and follows your clipboard settings.

Current results still clear after 30 seconds, on the next scan and when the window loses focus. The session history is separate and remains until cleared or the app closes.

## Extra barcode checks and fixes

- Checks optional revision dates against issue dates and validates supported field-code values and name flags.
- Does not treat a non-REAL-ID compliance flag as evidence of a fake.
- Fixes a malformed-name case where following field codes could otherwise be mistaken for a name.
- Reduces repeated field-counting work and history allocations; the history list is bounded and virtualized.

These are checks that work with your current barcode scanner. No camera, UV/infrared device, government lookup or document-verification service has been added.

## Testing and limits

**109 core tests and 220 layout cases passed.** The self-contained app also passed scanner/UI and animation checks. Tests use synthetic IDs only and cover names, extra field checks, history limits/clearing, automatic next-scan replacement, clipboard contents through a test writer, color states, window controls and both themes.

The installer is unsigned. Physical Honeywell scanner testing, physical mixed-monitor testing and a full installed-version update test remain outstanding.

**ID authenticity not verified.** Compare the printed card, photograph and customer. A barcode result does not approve a sale.

[Setup](https://github.com/PrithviGhale/ID-Reader/blob/main/docs/SCANNER_SETUP.md) · [Validation](https://github.com/PrithviGhale/ID-Reader/blob/main/docs/VALIDATION.md) · [Privacy and technical details](https://github.com/PrithviGhale/ID-Reader/blob/main/docs/TECHNICAL.md)
