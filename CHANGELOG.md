# Changelog

## [Unreleased]

- **Updated:** ClientStructs now at [70ac7923](https://github.com/aers/FFXIVClientStructs/tree/70ac7923) ([compare](https://github.com/aers/FFXIVClientStructs/compare/8a31ad5..70ac7923)).

## [1.13.1] (2025-01-22)

Update for Patch 7.16.

- **Changed:** Replaced the plugins glamour dresser cache with the games glamour dresser cache from ItemFinderModule for the Outfits tab.
- **Updated:** ClientStructs now at [8a31ad5](https://github.com/aers/FFXIVClientStructs/tree/8a31ad5) ([compare](https://github.com/aers/FFXIVClientStructs/compare/71e803c..8a31ad5)).

## [1.13.0] (2025-01-18)

- **Updated:** Completely reworked all Unlock tables.
  - Added nice looking tooltips for Items, EventItems, Quests, Triple Triad Cards and other things.
  - Added a useless progress overview on the Unlock category itself.
  - RowIds are now clickable and open the Row in a new window.
  - Removed rewards from Quest table.
- **Added:** A Target tab.
- **Added:** GameObject type overrides based on their ObjectKind.
- **Added:** CS Docs now support simple lists.
- **Changed:** Reworked dependency injection by using Injectio.
- **Fixed:** CS Docs are no longer displayed twice when the field is inherited.
- **Updated:** ClientStructs now at [71e803c](https://github.com/aers/FFXIVClientStructs/tree/71e803c) ([compare](https://github.com/aers/FFXIVClientStructs/compare/fb4dfc09..71e803c)).

There is probably some change that I forgot to list. :)

## [1.12.2] (2025-01-08)

- **Added:** Excel tab now also has support for AddonTransient sheet.
- **Fixed:** Info on Triple Triad Card tooltips didn't render correctly.

## [1.12.1] (2025-01-06)

- **Added:** A checkbox in the Unlock Links tab to filter CharaMakeCustomize rows, displaying only those that are usable by the currently logged-in character.
- **Fixed:** Forgot to update the version number last release and the CS version had the wrong text (it's at fb4dfc09, as linked).

## [1.12.0] (2025-01-06)

- **Added:** Unlock tabs Quests and Triple Triad Cards.
- **Added:** Excel supports Lobby sheet now. (This is all hardcoded btw, so sadly this is still not a super duper Excel browser and won't be for a while.)
- **Added:** For my SeStringEvaluator I've added support for sheet redirects (It resolves ActStr, ObjStr etc. if you've ever seen those). A tab for testing that has been added.
- **Updated:** Continued work on TextDecoder, now called NounProcessor, in preparations of possible contribution to Lumina, once finished.
  - Renamed to NounProcessor, as it implements the games Noun classes and doesn't actually do any decoding.
  - The Person argument/rows was renamed to ArticleType and every language has its own enum now, because the values have different meanings depending on the language.
  - Grammatical cases are only valid for the German language, so the columns were removed when displaying other languages.
- **Updated:** ClientStructs now at [fb4dfc09](https://github.com/aers/FFXIVClientStructs/tree/fb4dfc09) ([compare](https://github.com/aers/FFXIVClientStructs/compare/7ba7ab4..fb4dfc09)).

## [1.11.1] (2025-01-03)

- **Added:** An ImGui Main Menu button for /xldev to toggle the HaselDebug window.
- **Changed:** The Unlocks tabs are now sub-categories in the sidebar.
- **Changed:** In the EventFramework tab, the task history is now logged by hooking the AddTask function, instead of observing the Tasks vector.
- **Changed:** In the Lua Debug tab, the types have been moved to the front and the TreeNodes now have the SpanAvailWidth flag.
- **Fixed:** When searching Agents by name, the results couldn't be clicked on, because it would auto-select the first entry. This was removed, so it's now possible to select the agents.
- **Updated:** ClientStructs now at [f26035a](https://github.com/aers/FFXIVClientStructs/tree/7ba7ab4) ([compare](https://github.com/aers/FFXIVClientStructs/compare/f26035a..7ba7ab4)).

## [1.11.0] (2024-12-29)

- **Added:** New tab Addon Config.
- **Added:** Support for StdSet.
- **Added:** ILayoutInstance type redirect to SharedGroupLayoutInstance, when the type matches.
- **Updated:** Instance Content Director tab now uses the correct struct type based on InstanceContentType or PublicContentDirectorType.
- **Fixed:** A line to 3D positions is now also drawn for structs that inherit ILayoutInstance.
- **Fixed:** Incorrect usage of Std* struct types.
- **Updated:** ClientStructs now at [f26035a](https://github.com/aers/FFXIVClientStructs/tree/f26035a) ([compare](https://github.com/aers/FFXIVClientStructs/compare/cc98a564..f26035a)).

## [1.10.1] (2024-12-22)

Last update broke StdMap for many structs. This time it actually fetches the value offset directly instead of calculating where it could be based on the key type.

## [1.10.0] (2024-12-22)

- **Added:** Unlock tabs Outfits.
- **Updated:** A new context menu on all StdDeque, StdList, StdMap, StdVector nodes that allows to copy the address and to pop them out into a new window.
- **Updated:** GameObjects and its subclasses, and ILayoutInstances in any struct (not only in the Object Table tab) now draw a line from the cursor to the 3D position.
- **Updated:** Clicking on collected items in the unlock tabs Fashion Accessories, Minions and Mounts now summon them.
- **Updated:** Unlock Links now supports entries from the BannerCondition (BannerBg, BannerFrame, BannerDecoration, BannerFacial, BannerTimeline), BuddyAction, CSBonusContentType, CharaMakeCustomize, CraftAction, MJILandmark, Perform, QuestAcceptAdditionCondition and Trait sheets.
- **Fixed:** Inconsistencies in hiding achievement spoilers were hopefully fully fixed.
- **Fixed:** An issue with the StdMap key size was solved by using a minimum size of 8 bytes (seen in LayoutManager.InstancesByType where the key is an enum with the underlying type byte).
- **Fixed:** Removed soft hypens from MainCommands entries.
- **Fixed:** The Orchestrion Rolls category icon was moved to the Category column.

## [1.9.0] (2024-12-20)

- **Added:** Unlock tabs Fish and Spearfish.
- **Updated:** It's now possible to click on the NPC name in the Satisfaction Supply tab to teleport to the closest aetheryte.
- **Updated:** Currency Manager tab got a little facelift.
- **Updated:** Namespaces in xml docs are now truncated based on the parents namespace.
- **Fixed:** A parsing issue with multiple c-tags on a single xml doc line.
- **Fixed:** Hidden achievements displayed the description in the tooltip when the Hide Spoilers checkbox was enabled.
- **Updated:** ClientStructs now at [cc98a564](https://github.com/aers/FFXIVClientStructs/tree/cc98a564) ([compare](https://github.com/aers/FFXIVClientStructs/compare/b3a25a18..cc98a564)).

## [1.8.0] (2024-12-04)

- **Added:** More unlock tabs (Achievements, Glasses, Minions, Mounts, Orchestrion Rolls and Fashion Accessories).
- **Added:** Config tab now shows all config options in a table.
- **Added:** XML docs on ClientStructs fields are now shown in their tooltip. Fields that have docs are shown with an underline.
- **Added:** Shift-clicking on offsets now copies the offset. (Clicking on offsets without shift still copies the entire address.)
- **Fixed:** MainCommands tooltip no longer flickers on first show.
- **Updated:** ClientStructs now at [b3a25a18](https://github.com/aers/FFXIVClientStructs/tree/b3a25a18) ([compare](https://github.com/aers/FFXIVClientStructs/compare/e8b73d8f..b3a25a18)).

## [1.7.0] (2024-11-28)

- **Added:** Housing tab, displaying some useless info.
- **Added:** Unlock tab Aether Currents (Thanks @Scrxtchy; I allowed myself to enhance it a little^^).
- **Added:** Unlock tab BuddyEquip.
- **Added:** Support for excel Collection structs (like ItemStruct in SpecialShop).
- **Changed:** Addon and LogMessage tabs have been consolidated into a new Excel tab.
- **Updated:** SeString inspector will now draw the icon of Icon and Icon2 payloads.
- **Updated:** Small changes to the header in the Addon Inspector. It will now show the agents using this addon, or the host addon.
- **Removed:** Addons in the Addon Inspector can no longer be pinned to the sidebar.
- **Updated:** ClientStructs now at [e8b73d8f](https://github.com/aers/FFXIVClientStructs/tree/e8b73d8f) ([compare](https://github.com/aers/FFXIVClientStructs/compare/cf5ba30..e8b73d8f)).

## [1.6.5] (2024-10-20)

Update for 7.1.

- **Added:** Unknown Agents will show the Addon name in yellow, if they are active.
- **Added:** Addon Inspector will now highlight the nodes on hover.
- **Fixed:** StdList rendering was incorrect.
- **Updated:** ClientStructs now at [cf5ba30](https://github.com/aers/FFXIVClientStructs/tree/cf5ba30) ([compare](https://github.com/aers/FFXIVClientStructs/compare/81fb801..cf5ba30)).

## [1.6.4] (2024-09-28)

- **Added:** EventFramework Task History (not very useful, but still better than nothing).
- **Updated:** ClientStructs now at [81fb801](https://github.com/aers/FFXIVClientStructs/tree/81fb801)

## [1.6.3] (2024-07-22)

- **Fixed:** StdMap table column count was incorrect.

## [1.6.2] (2024-07-22)

- **Fixed:** Sheet payloads using a column of type SeString were not evaluated with the ColumnParam expression as lnum(1).
- **Fixed:** Added missing sheets Aetheryte and BNpcName to the Text Decoder tab.

## [1.6.1] (2024-07-22)

- **Fixed:** Some local parameters were not automatically detected, for example the condition in If payloads.

## [1.6.0] (2024-07-21)

- **Added:** The SeString Inspector window now has inputs for local parameters.  
  These are automatically detected from what's in the string. If you find a string with incorrectly detected parameters please open an issue.
- **Added:** The SeString evaluator now evaluates Digit, Head, HeadAll, JaNoun, EnNoun, DeNoun and FrNoun payloads.
- **Fixed:** Strings from the Addon/LogMessage tabs could not be opened in different languages.
- **Fixed:** Evaluating gstr parameters would produce garbage.
- **Fixed:** Evaluating Kilo payloads with a value of 0 would result in an empty string.
- **Fixed:** Evaluating Sheet payloads with a numeric column would produce a string. It now produces a Num payload as a workaround for not being able to write an integer expression directly.

## [1.5.0] (2024-07-20)

- **Added:** A LogMessage tab to search through texts from the LogMessage sheet (until a full Excel tab is implemented).
- **Added:** A language selector to the right of the search bar in the Addon and LogMessage tabs.
- **Added:** Holding shift while closing a SeString Inspector window will close all SeString Inspector windows.
- **Fixed:** The context menu to popout a tab was visible even when the window was already open.

## [1.4.3] (2024-07-19)

- **Changed:** Merged Addon Inspector and Addon Inspector 2 tabs.
- **Fixed:** The `/haseldebug` command didn't auto-register.

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

[unreleased]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.13.1...main
[1.13.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.13.0...v1.13.1
[1.13.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.12.2...v1.13.0
[1.12.2]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.12.1...v1.12.2
[1.12.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.12.0...v1.12.1
[1.12.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.11.1...v1.12.0
[1.11.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.11.0...v1.11.1
[1.11.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.10.1...v1.11.0
[1.10.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.10.0...v1.10.1
[1.10.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.9.0...v1.10.0
[1.9.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.8.0...v1.9.0
[1.8.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.7.0...v1.8.0
[1.7.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.6.5...v1.7.0
[1.6.5]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.6.4...v1.6.5
[1.6.4]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.6.3...v1.6.4
[1.6.3]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.6.2...v1.6.3
[1.6.2]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.6.1...v1.6.2
[1.6.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.6.0...v1.6.1
[1.6.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.5.0...v1.6.0
[1.5.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.4.3...v1.5.0
[1.4.3]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.4.2...v1.4.3
[1.4.2]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.4.1...v1.4.2
[1.4.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.4.0...v1.4.1
[1.4.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.2.1...v1.3.0
[1.2.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.2.0...v1.2.1
[1.2.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.1.1...v1.2.0
[1.1.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Haselnussbomber/HaselDebug/commit/eb8a00af
