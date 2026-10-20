# Scanner compatibility notes

Reviewed October 19, 2026. Tests and screenshots use synthetic IDs only.

## References

- [AAMVA 2020 card design standard, Annex D](https://www.aamva.org/getmedia/99ac7057-0f4d-4461-b0a2-3a5532e1b35c/AAMVA-2020-DLID-Card-Design-Standard.pdf): the fixed header, ASCII separators, subfile directory, date/country fields and different US/Canadian date orders. The parser follows this structure for strict validation.
- [Honeywell: wrong characters and keyboard country layouts](https://sps-support.honeywell.com/s/article/Scanner-does-not-support-my-keyboard-layout-wrong-characters-are-displayed?tabset-3dec3=1): keyboard layouts and national character replacements can affect keyboard-mode scanner output. The article includes Xenon XP 195x. Honeywell's page may require login or client-side access; its indexed official text was available during review.
- [Honeywell Xenon XP 1950g product documentation](https://automation.honeywell.com/us/en/products/productivity-solutions/barcode-scanners/general-purpose-handheld/xenon-xp-1950g-general-duty-scanner): use the user guide for the exact hardware/firmware. The linked current user-guide download required additional access during development.
- [Microsoft WPF dynamic resources](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/systems/xaml-resources-overview): resource updates used to switch theme brushes without rebuilding the window.
- [Microsoft WPF animation guidance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/animation-tips-and-tricks) and [Windows animation preference](https://learn.microsoft.com/en-us/dotnet/api/system.windows.systemparameters.clientareaanimation): short, finite animations that return to their base values and respect reduced-motion preferences.

## The reported header variation

The reported scanner output begins with `@ÂNSI ` instead of the standard header and has no normal line separators. Version 1.0.2 rejected non-ASCII text before reaching its recovery path. This is the identified software cause of that rejection. A keyboard/dead-key mismatch is a plausible transport cause, not a confirmed diagnosis of the physical scanner.

Version 1.0.3 recognizes only the specific `@ÂNSI ` and decomposed `@A\u0302NSI ` header variants, at the beginning of the input after an optional `]L0` prefix. It maps that fixed token to `@ANSI ` for operator-assisted recovery. It does not strip accents from names or rewrite dates, countries or license fields. Other unrecognized character changes still need review.

Because separators have been lost, original lengths/field boundaries cannot establish complete-frame validity. Recovered dates therefore still require a comparison with the printed card and explicit confirmation. Invalid, duplicate, conflicting or missing required values remain blocked. Strict mode does not apply this repair.

## Synthetic tests

The fixtures use the same structural pattern (fixed header, directory, DAQ/DCS/name flags, birth/issue/expiration, USA/CAN and trailing card fields), with unrelated made-up values. They cover composed/decomposed accents, plain headers, missing headers, control substitutions, suffixes, duplicate fields, malformed and truncated dates, conflicting date orders, concatenated scans and unsupported mutations.

The WPF self-test also sends the synthetic accented-header payload in chunks through the application's routed text-input handler, verifies displayed dates and confirmation, and checks that capture is disabled on other pages. This exercises more than a standalone parser call. It does not substitute for testing the physical Honeywell scanner.
