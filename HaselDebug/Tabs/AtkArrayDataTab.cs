using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Tabs;

// almost the same as in Dalamud :)
#pragma warning disable SeStringRenderer
public unsafe class AtkArrayDataTab(TextService TextService, DebugRenderer DebugRenderer) : DebugTab
{
    private readonly Type numberType = typeof(NumberArrayType);
    private readonly Type stringType = typeof(StringArrayType);
    private readonly Type extendType = typeof(ExtendArrayType);

    private int selectedNumberArray;
    private int selectedStringArray;
    private int selectedExtendArray;

    private string searchTerm = string.Empty;
    private bool hideUnsetStringArrayEntries = false;
    private bool hideUnsetExtendArrayEntries = false;
    private bool showTextAddress = false;
    private bool showMacroString = false;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var hostchild = ImRaii.Child("AtkArrayDataTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        using var tabs = ImRaii.TabBar("AtkArrayDataTabs");
        if (!tabs) return;

        DrawNumberArrayTab();
        DrawStringArrayTab();
        DrawExtendArrayTab();
    }

    private static void DrawCopyableText(string text, string tooltipText)
    {
        ImGuiHelpers.SafeTextWrapped(text);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(tooltipText);
            ImGui.EndTooltip();
        }

        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(text);
        }
    }

    private void DrawArrayList(Type? arrayType, int arrayCount, short* arrayKeys, AtkArrayData** arrays, ref int selectedIndex)
    {
        using var table = ImRaii.Table("ArkArrayTable", 3, ImGuiTableFlags.ScrollY | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings, new Vector2(300, -1));
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        var hasSearchTerm = !string.IsNullOrEmpty(searchTerm);

        for (var arrayIndex = 0; arrayIndex < arrayCount; arrayIndex++)
        {
            var inUse = arrayKeys[arrayIndex] != -1;

            var rowsFound = 0;

            if (hasSearchTerm && arrayType == typeof(StringArrayType))
            {
                if (!inUse)
                    continue;

                var stringArrayData = (StringArrayData*)arrays[arrayIndex];
                for (var rowIndex = 0; rowIndex < arrays[arrayIndex]->Size; rowIndex++)
                {
                    var isNull = (nint)stringArrayData->StringArray[rowIndex] == 0;
                    if (isNull)
                        continue;

                    if (new ReadOnlySeStringSpan(stringArrayData->StringArray[rowIndex]).ExtractText().Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
                        rowsFound++;
                }

                if (rowsFound == 0)
                    continue;
            }

            using var disabled = ImRaii.Disabled(!inUse);
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            if (ImGui.Selectable($"#{arrayIndex}", selectedIndex == arrayIndex, ImGuiSelectableFlags.SpanAllColumns))
                selectedIndex = arrayIndex;

            ImGui.TableNextColumn(); // Type
            if (arrayType != null && Enum.IsDefined(arrayType, arrayIndex))
            {
                ImGui.TextUnformatted(Enum.GetName(arrayType, arrayIndex));
            }
            else if (inUse && arrays[arrayIndex]->SubscribedAddonsCount > 0)
            {
                var raptureAtkUnitManager = RaptureAtkUnitManager.Instance();

                for (var j = 0; j < arrays[arrayIndex]->SubscribedAddonsCount; j++)
                {
                    if (arrays[arrayIndex]->SubscribedAddons[j] == 0)
                        continue;

                    using (ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF))
                        ImGui.TextUnformatted(raptureAtkUnitManager->GetAddonById(arrays[arrayIndex]->SubscribedAddons[j])->NameString);
                    break;
                }
            }

            ImGui.TableNextColumn(); // Size
            if (inUse)
                ImGui.TextUnformatted((rowsFound > 0 ? rowsFound : arrays[arrayIndex]->Size).ToString());
        }
    }

    private void DrawArrayHeader(Type? arrayType, string type, int index, AtkArrayData* array)
    {
        ImGui.TextUnformatted($"{type} Array #{index}");

        if (arrayType != null && Enum.IsDefined(arrayType, index))
        {
            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted($" ({Enum.GetName(arrayType, index)})");
        }

        ImGui.SameLine();
        ImGui.TextUnformatted("–");
        ImGui.SameLine();
        ImGui.TextUnformatted("Address: ");
        ImGui.SameLine(0, 0);
        DrawCopyableText($"0x{(nint)array:X}", "Copy address");

        if (array->SubscribedAddonsCount > 0)
        {
            ImGui.SameLine();
            ImGui.TextUnformatted("–");
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF))
                ImGui.TextUnformatted($"{array->SubscribedAddonsCount} Subscribed Addon" + (array->SubscribedAddonsCount > 1 ? 's' : string.Empty));

            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                if (tooltip)
                {
                    var raptureAtkUnitManager = RaptureAtkUnitManager.Instance();

                    for (var j = 0; j < array->SubscribedAddonsCount; j++)
                    {
                        if (array->SubscribedAddons[j] == 0)
                            continue;

                        ImGui.TextUnformatted(raptureAtkUnitManager->GetAddonById(array->SubscribedAddons[j])->NameString);
                    }
                }
            }
        }
    }

    private void DrawNumberArrayTab()
    {
        var atkArrayDataHolder = RaptureAtkModule.Instance()->AtkArrayDataHolder;

        using var tab = ImRaii.TabItem("Number Arrays");
        if (!tab) return;

        DrawArrayList(
            numberType,
            atkArrayDataHolder.NumberArrayCount,
            atkArrayDataHolder.NumberArrayKeys,
            (AtkArrayData**)atkArrayDataHolder.NumberArrays,
            ref selectedNumberArray);

        if (selectedNumberArray >= atkArrayDataHolder.NumberArrayCount || atkArrayDataHolder.NumberArrayKeys[selectedNumberArray] == -1)
            selectedNumberArray = 0;

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        using var child = ImRaii.Child("AtkArrayContent", new Vector2(-1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!child) return;

        var array = atkArrayDataHolder.NumberArrays[selectedNumberArray];
        DrawArrayHeader(numberType, "Number", selectedNumberArray, (AtkArrayData*)array);

        using var table = ImRaii.Table("NumberArrayDataTable", 7, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Entry Address", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Integer", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Short", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Byte", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Float", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Hex", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(7, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < array->Size; i++)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted($"#{i}");

            var ptr = &array->IntArray[i];

            ImGui.TableNextColumn(); // Address
            DrawCopyableText($"0x{(nint)ptr:X}", "Copy entry address");

            ImGui.TableNextColumn(); // Integer
            DrawCopyableText((*ptr).ToString(), "Copy value");

            ImGui.TableNextColumn(); // Short
            DrawCopyableText((*(short*)ptr).ToString(), "Copy as short");

            ImGui.TableNextColumn(); // Byte
            DrawCopyableText((*(byte*)ptr).ToString(), "Copy as byte");

            ImGui.TableNextColumn(); // Float
            DrawCopyableText((*(float*)ptr).ToString(), "Copy as float");

            ImGui.TableNextColumn(); // Hex
            DrawCopyableText($"0x{array->IntArray[i]:X2}", "Copy Hex");
        }
    }

    private void DrawStringArrayTab()
    {
        using var tab = ImRaii.TabItem("String Arrays");
        if (!tab) return;

        var atkArrayDataHolder = RaptureAtkModule.Instance()->AtkArrayDataHolder;

        using (var sidebarchild = ImRaii.Child("StringArraySidebar", new Vector2(300, -1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings))
        {
            if (sidebarchild)
            {
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##TextSearch", TextService.Translate("SearchBar.Hint"), ref searchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);

                DrawArrayList(
                    stringType,
                    atkArrayDataHolder.StringArrayCount,
                    atkArrayDataHolder.StringArrayKeys,
                    (AtkArrayData**)atkArrayDataHolder.StringArrays,
                    ref selectedStringArray);
            }
        }

        if (selectedStringArray >= atkArrayDataHolder.StringArrayCount || atkArrayDataHolder.StringArrayKeys[selectedStringArray] == -1)
            selectedStringArray = 0;

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        using var child = ImRaii.Child("AtkArrayContent", new Vector2(-1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!child) return;

        var array = atkArrayDataHolder.StringArrays[selectedStringArray];
        DrawArrayHeader(stringType, "String", selectedStringArray, (AtkArrayData*)array);
        ImGui.Checkbox("Hide unset entries##HideUnsetStringArrayEntriesCheckbox", ref hideUnsetStringArrayEntries);
        ImGui.SameLine();
        ImGui.Checkbox("Show text address##WordWrapCheckbox", ref showTextAddress);
        ImGui.SameLine();
        ImGui.Checkbox("Show macro string##RenderStringsCheckbox", ref showMacroString);

        using var table = ImRaii.Table("StringArrayDataTable", 4, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn(showTextAddress ? "Text Address" : "Entry Address", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Managed", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(4, 1);
        ImGui.TableHeadersRow();

        var hasSearchTerm = !string.IsNullOrEmpty(searchTerm);

        for (var i = 0; i < array->Size; i++)
        {
            var isNull = (nint)array->StringArray[i] == 0;
            if (isNull && hideUnsetStringArrayEntries)
                continue;

            if (hasSearchTerm)
            {
                if (isNull)
                    continue;

                if (!new ReadOnlySeStringSpan(array->StringArray[i]).ExtractText().Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
                    continue;
            }

            using var disabledColor = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), isNull);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted($"#{i}");

            ImGui.TableNextColumn(); // Address
            if (showTextAddress)
            {
                if (!isNull)
                    DrawCopyableText($"0x{(nint)array->StringArray[i]:X}", "Copy text address");
            }
            else
            {
                DrawCopyableText($"0x{(nint)(&array->StringArray[i]):X}", "Copy entry address");
            }

            ImGui.TableNextColumn(); // Managed
            if (!isNull)
            {
                ImGui.TextUnformatted(((nint)array->StringArray[i] != 0 && array->ManagedStringArray[i] == array->StringArray[i]).ToString());
            }

            ImGui.TableNextColumn(); // Text
            if (!isNull)
            {
                if (showMacroString)
                {
                    DrawCopyableText(new ReadOnlySeStringSpan(array->StringArray[i]).ToString(), "Copy text");
                }
                else
                {
                    DebugRenderer.DrawSeString(array->StringArray[i], new NodeOptions() { AddressPath = new AddressPath([(nint)array, (nint)array->StringArray[i]]) });
                }
            }
        }
    }

    private void DrawExtendArrayTab()
    {
        using var tab = ImRaii.TabItem("Extend Arrays");
        if (!tab) return;

        var atkArrayDataHolder = RaptureAtkModule.Instance()->AtkArrayDataHolder;

        DrawArrayList(
            extendType,
            atkArrayDataHolder.ExtendArrayCount,
            atkArrayDataHolder.ExtendArrayKeys,
            (AtkArrayData**)atkArrayDataHolder.ExtendArrays,
            ref selectedExtendArray);

        if (selectedExtendArray >= atkArrayDataHolder.ExtendArrayCount || atkArrayDataHolder.ExtendArrayKeys[selectedExtendArray] == -1)
            selectedExtendArray = 0;

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        using var child = ImRaii.Child("AtkArrayContent", new Vector2(-1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);

        var array = atkArrayDataHolder.ExtendArrays[selectedExtendArray];
        DrawArrayHeader(null, "Extend", selectedExtendArray, (AtkArrayData*)array);
        ImGui.Checkbox("Hide unset entries##HideUnsetExtendArrayEntriesCheckbox", ref hideUnsetExtendArrayEntries);

        using var table = ImRaii.Table("ExtendArrayDataTable", 3, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Entry Address", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Pointer", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < array->Size; i++)
        {
            var isNull = (nint)array->DataArray[i] == 0;
            if (isNull && hideUnsetExtendArrayEntries)
                continue;

            using var disabledColor = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), isNull);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted($"#{i}");

            ImGui.TableNextColumn(); // Address
            DrawCopyableText($"0x{(nint)(&array->DataArray[i]):X}", "Copy entry address");

            ImGui.TableNextColumn(); // Pointer
            if (!isNull)
            {
                var marker = (MapMarkerBase*)array->DataArray[i];
                DebugRenderer.DrawIcon(marker->IconId);
                DebugRenderer.DrawPointerType(array->DataArray[i], typeof(MapMarkerBase), new NodeOptions()
                {
                    SeStringTitle = new ReadOnlySeStringSpan(marker->Subtext).ToReadOnlySeString(),
                    AddressPath = new AddressPath([(nint)array, (nint)array->DataArray[i]])
                });
            }
        }
    }
}
