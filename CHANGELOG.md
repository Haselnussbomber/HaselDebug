# Changelog

## [1.4.2] (2024-07-19)

- **Added:** It's now possible to pop-out tabs (including pinned ones) with a right click -> Open in new window.
- **Updated:** ClientStructs now at [36a3e4c](https://github.com/aers/FFXIVClientStructs/tree/36a3e4c)

## [1.4.1] (2024-07-16)

- **Changed:** Field names are now copyable.
- **Changed:** TreeNodes are now disabled when the struct doesn't have any fields.
- **Changed:** TreeNodes, tables and SeString Inspector windows no longer save ImGui settings (they kinda spammed the config).
- **Other:** TextDecoder was updated to use Luminas SeStringBuilder instead of Utf8Strings. I noticed some names include payloads, which I want to preserve. The TextDecoder tab though extracts the text now, so no payloads are visible there.

## [1.4.0] (2024-07-14)

- **Added:** Instances and Agents are now pinnable.  
  Right-clicking the TreeNode of an Instance or Agent gives an option to pin it to the sidebar. Agents must implement their own struct (AgentInterface is not pinnable).
- **Added:** TreeNodes for Vector2, Vector3 and Vector4 fields now show the coordinates instead of type name.
- **Added:** An Addon tab to search through texts from the Addon sheet (currently just playing around with it).
- **Added:** A new SeString Inspector window (used in the Addon and Chat tab for now).
- **Added:** SeString expressions now have a name, if known.
- **Added:** Agents are now searchable (just the agents name).
- **Added:** Instances are now searchable (just the type name).

## [1.3.0] (2024-07-11)

- **Added:** A new configuration window was added.  
  Accessible via the plugin installer, `/haseldebug config` or the cogwheel in the main window.  
  Currently it only has a single setting to toggle the automatic opening of the main window when the plugin loads.
- **Added:** GameObjects in the Object Table how have a context menu to toggle drawing.
- **Removed:** The Lobby tab was removed as the current CharaSelect character is also listed in the Objects Table.

## [1.2.1] (2024-07-11)

- **Added:** Support for StdList

## [1.2.0] (2024-07-10)

- **Added:** Early version of the Addon Inspector 2 tab.
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
- **Changed:** AtkTextures are now in their own TreeNode.

## [1.1.1] (2024-07-08)

Updated CS for 7.05hf1

## [1.1.0] (2024-07-08)

- **Added:** ExdSheets dependency and renderer. See it in action in the Territory Type tab.
- **Added:** Text Decoder tab to display output of my C# version of the games TextDecoder.
- **Changed:** StringMaker is now a table.
- **Fixed:** Restored fixed string generation in the StringMaker.

## [1.0.0] (2024-07-07)

First release! 🥳

[unreleased]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.4.1...main
[1.4.2]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.4.1...v1.4.2
[1.4.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.4.0...v1.4.1
[1.4.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.2.1...v1.3.0
[1.2.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.2.0...v1.2.1
[1.2.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.1.1...v1.2.0
[1.1.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Haselnussbomber/HaselDebug/commit/eb8a00af
