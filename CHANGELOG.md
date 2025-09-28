# Changelog

## [1.36.0] (2025-09-29)

- **Added:** Golden Agent/Addon navigation links in the Addon Inspector and Agents tabs.
- **Added:** The Addon Inspector now shows the callback handler, including EventKind.
- **Added:** ByteColor structs now also show the color in CSS hex color notation (8 digits), the UIColor RowId (if found for the current theme) and a small visual preview.
- **Added:** Type redirect for MassivePcContentDirector.
- **Updated:** ClientStructs now at [b41eccb6](https://github.com/aers/FFXIVClientStructs/tree/b41eccb6) ([compare](https://github.com/aers/FFXIVClientStructs/compare/775e4363..b41eccb6)).

## [1.35.1] (2025-09-23)

- **Updated:** Text in the "Completion" tab is now copyable.
- **Fixed:** The categories in the "Completion" tab are now correctly force-opened when the search term has changed and exists.

## [1.35.0] (2025-09-23)

- **Added:** A Completion tab to search through auto-translate texts.
- **Updated:** The "Export Timeline" button now copies code that is indented using tabs.
- **Updated:** Updated Link macro expression names and added names for Description, WKSPioneeringTrail and MKDLore links.
- **Fixed:** When using the "Export Timeline" button, `textColor` and `textOutlineColor` parameters had a closing bracket too much after the value.
- **Updated:** ClientStructs now at [775e4363](https://github.com/aers/FFXIVClientStructs/tree/775e4363) ([compare](https://github.com/aers/FFXIVClientStructs/compare/e6a625a0..775e4363)).

## [1.34.2] (2025-09-21)

- **Fixed:** In the Addon Inspector, the "Export Timeline" button now also supports Text Color keyframes. I forgot to add this last update.

## [1.34.1] (2025-09-21)

- **Fixed:** In the Addon Inspector, Text Nodes now correctly show Text Color keyframes instead of Part ID keyframes.
- **Updated:** ClientStructs now at [e6a625a0](https://github.com/aers/FFXIVClientStructs/tree/e6a625a0) ([compare](https://github.com/aers/FFXIVClientStructs/compare/c5652dd3..e6a625a0)).

## [1.34.0] (2025-09-17)

- **Added:** An "Event Object Manager" tab.
- **Added:** New unlock tabs "Chocobo Taxi Stands" and "HowTos".
- **Added:** An "Unlock Span Length Test" tab to quickly validate most bit arrays in UIState and PlayerState.
- **Fixed:** `void*` no longer show their own address. Instead, the address it points to is shown.
- **Updated:** ClientStructs now at [c5652dd3](https://github.com/aers/FFXIVClientStructs/tree/c5652dd3) ([compare](https://github.com/aers/FFXIVClientStructs/compare/59feea87..c5652dd3)).

## [1.33.0] (2025-09-11)

- **Added:** The agent context menu in the Agents tab now has a "Show Agent"/"Hide Agent" option when the agent is activatable. Use at your own risk!
- **Added:** Visible agents in the Agents tab are now listed in green.
- **Added:** The Main Commands tab now displays stats recorded by the McAggreModule.
- **Fixed:** Agents with an Unk entry in the AgentId enum are now once again using the addon name as fallback when it's open.
- **Updated:** ClientStructs now at [59feea87](https://github.com/aers/FFXIVClientStructs/tree/59feea87) ([compare](https://github.com/aers/FFXIVClientStructs/compare/dd9edd91..59feea87)).

## [1.32.2] (2025-09-04)

- **Updated:** ClientStructs now at [dd9edd91](https://github.com/aers/FFXIVClientStructs/tree/dd9edd91) ([compare](https://github.com/aers/FFXIVClientStructs/compare/834d3cc9..dd9edd91)).

## [1.32.1] (2025-09-02)

- **Updated:** ClientStructs now at [834d3cc9](https://github.com/aers/FFXIVClientStructs/tree/834d3cc9) ([compare](https://github.com/aers/FFXIVClientStructs/compare/24edbea0..834d3cc9)).

## [1.32.0] (2025-09-01)

- **Added:** A Permissions tab to check which Permissions match the current Conditions.
- **Updated:** ClientStructs now at [24edbea0](https://github.com/aers/FFXIVClientStructs/tree/24edbea0) ([compare](https://github.com/aers/FFXIVClientStructs/compare/0e5c2ea3..24edbea0)).

## [1.31.0] (2025-08-26)

- **Added:** Keybinds are now displayed in the Input tab.
- **Added:** The Addon Config tab now has a sub-tab that lists all HudLayout configurable addons.
- **Updated:** The GlobalParameters list in the RaptureTextModule tab was updated.
- **Updated:** ClientStructs now at [0e5c2ea3](https://github.com/aers/FFXIVClientStructs/tree/0e5c2ea3) ([compare](https://github.com/aers/FFXIVClientStructs/compare/2d10f753..0e5c2ea3)).

## [1.30.5] (2025-08-15)

- **Fixed:** There was a substantial FPS loss when displaying structs with a lot of field due to inheritance checks for type redirects. The result of inheritance checks is now cached.
- **Fixed:** Hovering over an uninitialized `Character.CompanionObject` to highlight the position would crash the game because VirtualTable was a nullptr.
- **Updated:** ClientStructs now at [2d10f753](https://github.com/aers/FFXIVClientStructs/tree/2d10f753) ([compare](https://github.com/aers/FFXIVClientStructs/compare/48d43753..2d10f753)).

## [1.30.4] (2025-08-13)

- **Fixed:** The signatures for generating addon name hashes in the Addon Config tab were outdated.
- **Updated:** ClientStructs now at [48d43753](https://github.com/aers/FFXIVClientStructs/tree/48d43753) ([compare](https://github.com/aers/FFXIVClientStructs/compare/08480efc..48d43753)).

## [1.30.3] (2025-08-11)

- **Added:** More event handler type redirects: FateDirector and BattleLeveDirector. CompanyLeveDirector, CompanyLeveOfficer and GatheringLeveDirector use LeveDirector for now.
- **Updated:** The Drag Drop Type tab was reworked into separate lists.
- **Updated:** The flag enum input was changed so that it shows the bit and fills in unknown values.
- **Updated:** ClientStructs now at [08480efc](https://github.com/aers/FFXIVClientStructs/tree/08480efc) ([compare](https://github.com/aers/FFXIVClientStructs/compare/a1b91b0b..08480efc)).

## [1.30.2] (2025-08-09)

- **Added:** In the RaptureTextModule Definitions tab, the vfunc index is now displayed.
- **Fixed:** In the RaptureTextModule Definitions tab, the parameters displayed garbage after the TotalParamCount was reached.
- **Fixed:** Drawing AtkTextures no longer replaces the struct view, instead the texture is displayed inside the struct at the end.
- **Updated:** ClientStructs now at [a1b91b0b](https://github.com/aers/FFXIVClientStructs/tree/a1b91b0b) ([compare](https://github.com/aers/FFXIVClientStructs/compare/3d153390..a1b91b0b)).

## [1.30.1] (2025-08-07)

- **Fixed:** The Outfits table was empty.

## [1.30.0] (2025-08-07)

Update for 7.3.

- **Added:** `InventoryItem.CrafterContentId` now shows a name, if available.
- **Added:** The wedding date on InventoryItems is now displayed.
- **Added:** Addon Factories tab was added.
- **Updated:** Tabs now load data when they are opened for the first time, not when the plugin starts.
- **Updated:** The address of ConfigOptions can be copied by holding shift.
- **Temporarily:** Drag Drop Type tab is disabled due to new ImGui bindings not handling that many columns. Needs a rewrite.
- **Updated:** ClientStructs now at [3d153390](https://github.com/aers/FFXIVClientStructs/tree/3d153390) ([compare](https://github.com/aers/FFXIVClientStructs/compare/62ea2008..3d153390)).

## [1.29.1] (2025-07-15)

- **Fixed:** The border around highlighted nodes didn't respect the user-set scaling of the addon.
- **Fixed:** The border around highlighted component nodes wasn't shown when `AtkResNode` was null. It now falls back to `OwnerNode`.
- **Updated:** It's now possible to copy values from the Addon Config table.
- **Updated:** ClientStructs now at [62ea2008](https://github.com/aers/FFXIVClientStructs/tree/62ea2008) ([compare](https://github.com/aers/FFXIVClientStructs/compare/8b3a8f45..62ea2008)).

## [1.29.0] (2025-07-12)

- **Added:** An Item Action Type tab to list all ItemActions grouped by Type and their items.
- **Updated:** The Addon Inspector's Node Picker now opens the TreeNodes up to the node that was clicked on and only resets the selection index when hovered nodes change, not when the cursor moved by a single pixel.
- **Updated:** ClientStructs updated to the branch used in my [Update InventoryItem PR](https://github.com/aers/FFXIVClientStructs/pull/1480), based on [8b3a8f45](https://github.com/aers/FFXIVClientStructs/tree/8b3a8f45) ([compare](https://github.com/aers/FFXIVClientStructs/compare/8a6e0bb6..2ee1714c)).

## [1.28.1] (2025-06-24)

- **Updated:** ClientStructs now at [8a6e0bb6](https://github.com/aers/FFXIVClientStructs/tree/8a6e0bb6) ([compare](https://github.com/aers/FFXIVClientStructs/compare/a93b68f5..8a6e0bb6)).

## [1.28.0] (2025-06-20)

- **Added:** StdLinkedLists are now supported.
- **Added:** StdStrings are now rendered as normal, copyable strings.
- **Added:** ResourceHandle.FileType is now written as string.
- **Added:** The following pointers are now rendered as array:
  - AtkUldManager.Assets
  - AtkUldManager.PartsList
  - AtkUldManager.NodeList
  - AtkUldManager.Objects (exception for ObjectCount 1 which is displayed directly)
  - AtkUldWidgetInfo.NodeList
  - AtkTimelineManager.Timelines
  - AtkTimelineManager.Animations
  - AtkTimelineManager.LabelSets
  - AtkTimelineManager.KeyFrames
- **Updated:** Atk nodes as TreeNode are now always highlighted on hover.
- **Changed:** The AtkValues table is no longer limited in size.

## [1.27.0] (2025-06-19)

- **Added:** Addon Inspector can now display Events. Target and Listener columns are hidden unless an event points to something different than the same Node or UnitBase.
- **Added:** A Drag Drop Type tab displaying the matrix of DragDropTypeMasks.
- **Updated:** Animation Group values in the Addon Inspector are now copyable and colors are shown with ImGui.ColorEdit3 (can't make them read-only with Dalamuds ImGui version).
- **Fixed:** Addon Inspector now primarily uses the addon id, before checking the addon name. This should fix displaying the wrong addon if multiple have the same name.
- **Fixed:** TreeNode ids were no longer unique with the last update.
- **Updated:** ClientStructs updated to the branch used in my [DragDrop PR](https://github.com/aers/FFXIVClientStructs/pull/1457), based on [a93b68f5](https://github.com/aers/FFXIVClientStructs/tree/a93b68f5) ([compare](https://github.com/aers/FFXIVClientStructs/compare/b45b7d42..a93b68f5)).

## [1.26.0] (2025-06-14)

- **Added:** Addon Inspector can now display Animation Groups and has an "Export Timeline" button to copy code for KamiToolKit. (Thanks to @MidoriKami!)
- **Added:** A new Atk Handler Calls tab to log calls of `AtkStage->AtkExternalInterface->CallHandler`. I named as many handlers as I could.
- **Updated:** More AtkValue ValueTypes are now rendered, including nested AtkValues.
- **Updated:** ClientStructs now at [b45b7d42](https://github.com/aers/FFXIVClientStructs/tree/b45b7d42) ([compare](https://github.com/aers/FFXIVClientStructs/compare/7028ecae..b45b7d42)).

## [1.25.0] (2025-06-10)

- **Updated:** Addon Inspector was updated a bit:
  - Addons and Nodes can be popped out into their own window (rightclick the tree nodes)
  - Most properties are now editable
  - Label Sets are now displayed
  - Added a search bar to the top-level (=addon) Node List, which can find nodes by address (hex only), NodeId, NodeType, ComponentType

  It's still work in progress. Animations and Parts will be added later.
- **Updated:** ClientStructs now at [7028ecae](https://github.com/aers/FFXIVClientStructs/tree/7028ecae) ([compare](https://github.com/aers/FFXIVClientStructs/compare/3d53c797..7028ecae)).

## [1.24.2] (2025-06-04)

- **Added:** Type redirect for PublicContentOccultCrescent.
- **Updated:** ClientStructs now at [3d53c797](https://github.com/aers/FFXIVClientStructs/tree/3d53c797) ([compare](https://github.com/aers/FFXIVClientStructs/compare/901b2362..3d53c797)).

## [1.24.1] (2025-05-27)

- **Updated:** ClientStructs now at [901b2362](https://github.com/aers/FFXIVClientStructs/tree/901b2362) ([compare](https://github.com/aers/FFXIVClientStructs/compare/bd82d122..901b2362)).

## [1.24.0] (2025-05-27)

- **Added:** Type redirects for AtkResNode and AtkComponentBase.
- **Added:** Blue Mage Actions tab.
- **Added:** Input tab.
- **Changed:** The AtkEventData in the Atk Events tab now displays the correct struct based on the AtkEventType.
- **Fixed:** BuddyAction and QuestAcceptAdditionCondition unlock link indexes were off by 1.
- **Updated:** ClientStructs now at [bd82d122](https://github.com/aers/FFXIVClientStructs/tree/bd82d122) ([compare](https://github.com/aers/FFXIVClientStructs/compare/ba0a6602..bd82d122)).

## [1.23.1] (2025-04-30)

- **Added:** DrawObject type redirects, based on their ObjectType:
  - CharacterBase, based on their ModelType:
    - Human
    - Demihuman
    - Monster
    - Weapon
- **Changed:** The "Icon" name check to draw icons was reverted. Instead, an additional `uint` check was added.
- **Updated:** ClientStructs now at [ba0a6602](https://github.com/aers/FFXIVClientStructs/tree/ba0a6602) ([compare](https://github.com/aers/FFXIVClientStructs/compare/377ddb3..ba0a6602)).

## [1.23.0] (2025-04-30)

- **Added:** A Conditions tab, using the CS Conditions struct instead of Dalamuds ConditionFlags enum, providing xmldoc comments if available.
- **Added:** A new Object Tables category, featuring:
  - Character Manager,
  - Chara Select Character List,
  - Client Object Manager,
  - Game Object Manager, now with 3 tabs IndexSorted, GameObjectIdSorted and EntityIdSorted,
  - and the newly reversed Stand Object Manager, displaying nameless, "lively" EventNpcs and EventObjects.

  These now reuse the same table code and I've added the EntityId and ObjectId columns. It's now possible to hide columns, though settings are not saved for these tables.
- **Changed:** Previously, fields with "IconId" in their name showed the icon next to the value, now it's for all fields containing just "Icon".
- **Updated:** I updated my TerritoryIntendedUse enum in HaselCommon, so 60 is now called CosmicExploration.
- **Updated:** ClientStructs now at [377ddb3](https://github.com/aers/FFXIVClientStructs/tree/377ddb3) ([compare](https://github.com/aers/FFXIVClientStructs/compare/09d40c6a..377ddb3)).

## [1.22.0] (2025-04-23)

- **Added:** Added a "Copy as hex" button to SeStringMaker that lets you copy the SeString as raw byte data.
- **Fixed:** Incorrect StdSet/SetMap memory alignment.
- **Fixed:** Inventory tab now correctly relies on functions, so it can work in space (Cosmopouch1/2).
- **Fixed:** Unreleased Glasses (rows without icons) are now hidden.
- **Fixed:** Fish not connected with an item are now hidden.
- **Fixed:** Aether Currents table was missing the increment for the numbering.
- **Fixed:** Errors that are thrown while rendering a tab are now properly logged.
- **Updated:** ClientStructs now at [09d40c6a](https://github.com/aers/FFXIVClientStructs/tree/09d40c6a) ([compare](https://github.com/aers/FFXIVClientStructs/compare/4ae9f561..09d40c6a)).

## [1.21.0] (2025-04-13)

- **Added:** Holding shift in Unlock Links -> Titles now shows the english version of the titles.
- **Added:** Added Radius to the Agent Map Event Markers table.
- **Updated:** Excel v2 now has a better preview for column values.
- **Fixed:** Agent Map Event Markers table broke when the icon wasn't found.
- **Fixed:** Added the missing GameObject -> Companion type redirect.
- **Fixed:** Object Table tab now sorts by index.
- **Updated:** ClientStructs now at [4ae9f561](https://github.com/aers/FFXIVClientStructs/tree/4ae9f561) ([compare](https://github.com/aers/FFXIVClientStructs/compare/a625ce4d..4ae9f561)).

## [1.20.1] (2025-03-30)

- **Added:** Node ID is now shown in the Node List in the Addon Inspector.
- **Added:** Right clicking the Columns button in Excel (v2) now clears all columns (except for RowId/SubrowId).
- **Fixed:** Enums with FlagsAttribute displayed incorrect values.
- **Updated:** ClientStructs now at [a625ce4d](https://github.com/aers/FFXIVClientStructs/tree/a625ce4d) ([compare](https://github.com/aers/FFXIVClientStructs/compare/b484eac4..a625ce4d)).

## [1.20.0] (2025-03-26)

First update for Patch 7.2.

- **Added:** A work in progress Excel (v2) tab, which is based on Lumina.Excel properties.
- **Added:** Support for IconId arrays in the struct renderer.
- **Removed:** Noun Processor and Sheet Redirect Test tabs were removed, because the SeStringEvaluator is now part of Dalamud and therefore I removed those services.
- **Fixed:** HouseIds in the Housing tab are now correctly displayed as structs.
- **Updated:** ClientStructs now at [b484eac4](https://github.com/aers/FFXIVClientStructs/tree/b484eac4) ([compare](https://github.com/aers/FFXIVClientStructs/compare/4d473c7..b484eac4)).

## [1.19.0] (2025-03-17)

- **Added:** An AtkEvents tab for global events, excluding MouseMove, MouseOver, MouseOut, FocusStart, FocusStop, WindowRollOver, WindowRollOut, TimerTick, 74 and 79 to avoid spam.
- **Added:** The Excel tab now features the LogKind sheet.
- **Added:** SeString inspector now shows the name of the Item, Quest, Achievement, HowTo, Status, and AkatsukiNote that was linked in the Link payload.
- **Added:** The Shop tab now displays the used AtkComponentList struct.
- **Changed internally:** The Excel tab was rewritten, so that things can be reused.
- **Updated:** SeStringEvaluator is now reflecting the state of my Dalamud PR, correctly handling Sheet redirects.
- **Updated:** ClientStructs now at [4d473c7](https://github.com/aers/FFXIVClientStructs/tree/4d473c7) ([compare](https://github.com/aers/FFXIVClientStructs/compare/977a8fd..4d473c7)), which is my PR 1322 based on the `7.2_prep` branch.

## [1.18.2] (2025-03-07)

- **Updated:** Added expression names for macro codes Num, String, Caps, Split, LevelPos.
- **Fixed:** NounProcessor tab didn't initialize.

## [1.18.1] (2025-03-06)

- **Updated:** ClientStructs now at [977a8fd](https://github.com/aers/FFXIVClientStructs/tree/977a8fd) ([compare](https://github.com/aers/FFXIVClientStructs/compare/4a727b4..977a8fd)).

## [1.18.0] (2025-02-25)

- **Added:** A very simple Special Shops tab to explore the sheet.
- **Updated:** ClientStructs now at [4a727b4](https://github.com/aers/FFXIVClientStructs/tree/4a727b4) ([compare](https://github.com/aers/FFXIVClientStructs/compare/1c32fa48..4a727b4)).

## [1.17.0] (2025-02-23)

- **Added:** Rapture Text Module/Definitions: The address of the decoder function was added, including the address of the resolved vfunc (since they are just small functions that jump to there).
- **Updated:** Rapture Text Module/Definitions: The table is now sorted by macro code id.
- **Updated:** SeStringEvaluator now supports Ordinal and Split macros.
- **Changed:** SeStringEvaluator now passes through payloads that were not evaluated.
- **Changed:** Lots of code was rewritten to use the Injectio and AutoCtor generators.
- **Fixed:** Rapture Text Module/Definitions: The parameter columns were shifted because of a leftover `ImGui.TableNextColumn` call.
- **Fixed:** Unlocks: The unlock progress for Emotes was incorrectly using the "Can use" state.
- **Removed:** The plugin no longer depends on the Yoga layout engine, therefore the Unlocks overview is now a boring table and the Triple Triad Card preview in the tooltip was also replaced with "normal" ImGui code.

## [1.16.0] (2025-02-18)

- **Added:** An Addon Names tab that lists addon names used by the `RaptureAtkModule.OpenAddon` function.
- **Added:** A Territory Intended Use tab to debug the enum I added to my HaselCommon lib.
- **Added:** EventHandler type redirects based on the ContentId for:
  - QuestEventHandler
  - GatheringPointEventHandler
  - ShopEventHandler
  - AetheryteEventHandler
  - CraftEventHandler
  - CustomTalkEventHandler
  - InstanceContentDirector
    - InstanceContentDeepDungeon
    - InstanceContentOceanFishing
  - PublicContentDirector
    - PublicContentBozja
    - PublicContentEureka
  - GoldSaucerDirector
- **Added:** Rapture Text Module: A new Icon2 Mapping tab, which displays the Icon2 macros remapped icons based on the controller button mappings.
- **Updated:** Rapture Text Module: Reversed some more global parameters.
- **Updated:** SeStringEvaluator now supports MacroCodes LowerHead, Lower, PcName, IfPcGender, IfPcName and IfSelf. Icon2 now respects controller button mappings, as written above.
- **Updated:** ClientStructs now at [1c32fa48](https://github.com/aers/FFXIVClientStructs/tree/1c32fa48) ([compare](https://github.com/aers/FFXIVClientStructs/compare/ee4144e0..1c32fa48)).

## [1.15.0] (2025-01-30)

- **Added:** A Zone column was added to the Sightseeing Log table.
- **Added:** A tooltip was added to the name column of the Sightseeing Log table.
- **Added:** The SeString inspector window now also shows the payloads that are the result of evaluation by the SeStringEvaluator.
- **Added:** The new StringPointer type in CS is now handled as SeString.
- **Changed:** In the Utf8String Sanitize tab, the Unicode range has been moved to the front for better readability.
- **Fixed:** Sheet macros have an optional fourth parameter that was treated as a required parameter by the SeStringEvaluator.
- **Fixed:** Images in tooltips of the Quests, Tiple Triad Cards, and Sightseeing Log tables are now pre-loaded, so they don't flicker.
- **Updated:** ClientStructs now at [ee4144e0](https://github.com/aers/FFXIVClientStructs/tree/ee4144e0) ([compare](https://github.com/aers/FFXIVClientStructs/compare/70ac7923..ee4144e0)).

## [1.14.0] (2025-01-27)

- **Added:** An Utf8String Sanitize tab to see which characters are filtered with which flags.
- **Added:** StringMaker now has a Preview window, which uses my SeStringEvaluator and allows you to test local parameters, if there are any.
- **Added:** StringMaker now has a Print Evaluated button, which prints the string that went through my SeStringEvaluator (as seen in the Preview window) instead of the games MacroEncoder/TextModule/PronounModule whatever.
- **Added:** The SeStringEvaluator now supports Fixed macros, except for the auto-translate/completion feature (I will add that another time).
- **Changed:** For now, the SeStringEvaluator will simply pass through NonBreakingSpace, Hyphen and SoftHyphen payloads instead of converting them to their Unicode counterparts. I might revisit this decision in the future.
- **Removed:** StringMaker no longer shows the output of each entry due to the addition of the preview window.
- **Fixed:** In the SeStringEvaluator. the Sheet macro will now correctly generate strings for numeric columns.
- **Fixed:** In the SeStringEvaluator, sheet name redirects passed to the Sheet macro will now be resolved.
- **Fixed:** In the SeStringEvaluator, LevelPos macros now use the language that was defined with the context.
- **Updated:** SeStrings rendered with the DebugRenderer will now display names for expressions of Fixed macros.
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

[unreleased]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.36.0...main
[1.36.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.35.1...v1.36.0
[1.35.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.35.0...v1.35.1
[1.35.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.34.2...v1.35.0
[1.34.2]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.34.1...v1.34.2
[1.34.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.34.0...v1.34.1
[1.34.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.33.0...v1.34.0
[1.33.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.32.2...v1.33.0
[1.32.2]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.32.1...v1.32.2
[1.32.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.32.0...v1.32.1
[1.32.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.31.0...v1.32.0
[1.31.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.30.5...v1.31.0
[1.30.5]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.30.4...v1.30.5
[1.30.4]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.30.3...v1.30.4
[1.30.3]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.30.2...v1.30.3
[1.30.2]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.30.1...v1.30.2
[1.30.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.30.0...v1.30.1
[1.30.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.29.1...v1.30.0
[1.29.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.29.0...v1.29.1
[1.29.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.28.1...v1.29.0
[1.28.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.28.0...v1.28.1
[1.28.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.27.0...v1.28.0
[1.27.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.26.0...v1.27.0
[1.26.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.25.0...v1.26.0
[1.25.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.24.2...v1.25.0
[1.24.2]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.24.1...v1.24.2
[1.24.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.24.0...v1.24.1
[1.24.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.23.1...v1.24.0
[1.23.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.23.0...v1.23.1
[1.23.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.22.0...v1.23.0
[1.22.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.21.0...v1.22.0
[1.21.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.20.1...v1.21.0
[1.20.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.20.0...v1.20.1
[1.20.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.19.0...v1.20.0
[1.19.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.18.2...v1.19.0
[1.18.2]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.18.1...v1.18.2
[1.18.1]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.18.0...v1.18.1
[1.18.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.17.0...v1.18.0
[1.17.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.16.0...v1.17.0
[1.16.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.15.0...v1.16.0
[1.15.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.14.0...v1.15.0
[1.14.0]: https://github.com/Haselnussbomber/HaselDebug/compare/v1.13.1...v1.14.0
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
