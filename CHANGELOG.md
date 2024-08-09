# Changelog

## [Unreleased]

- **Added:** Unlocks -> Store Items tab.
- **Added:** uint fields named IconId will show their icon in front of their id.
- **Added:** Known byte* strings will now be displayed as string, instead of just a single byte.  
  This has to be maintained by hand. Currently handled fields are:
    - MapMarkerBase.Subtext
    - ExcelSheet.SheetName
- **Changed:** Extend Arrays are now displayed as MapMarkerBase*.
- **Changed:** String Arrays strings are now rendered as SeString, so payloads can be inspected.
- **Changed:** FixedSizeArray strings are now rendered as SeString, so payloads can be inspected.
- **Changed:** Interactable Selectables in the Unlocks tab now show a hand cursor.

## [1.1.1] (2024-07-08)

Updated CS for 7.05hf1

## [1.1.0] (2024-07-08)

- **Added:** ExdSheets dependency and renderer. See it in action in the Territory Type tab.
- **Added:** Text Decoder tab to display output of my C# version of the games TextDecoder.
- **Changed:** StringMaker is now a table.
- **Fixed:** Restored fixed string generation in the StringMaker.

## [1.0.0] (2024-07-07)

First release! 🥳

[unreleased]: https://github.com/Haselnussbomber/HaselDebug/compare/main...v1.1.1
[1.1.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Haselnussbomber/HaselDebug/commit/eb8a00af
