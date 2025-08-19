using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AtkArrayDataTab : DebugTab
{
    private readonly TextService _textService;
    private readonly DebugRenderer _debugRenderer;

    private readonly Type _numberType = typeof(NumberArrayType);
    private readonly Type _stringType = typeof(StringArrayType);
    private readonly Type _extendType = typeof(ExtendArrayType);

    private int _selectedNumberArray;
    private int _selectedStringArray;
    private int _selectedExtendArray;

    private string _searchTerm = string.Empty;
    private bool _hideUnsetStringArrayEntries = false;
    private bool _hideUnsetExtendArrayEntries = false;
    private bool _showTextAddress = false;
    private bool _showMacroString = false;

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
        ImGui.TextWrapped(text);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.BeginTooltip();
            ImGui.Text(tooltipText);
            ImGui.EndTooltip();
        }

        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(text);
        }
    }

    private void DrawArrayList(Type? arrayType, int arrayCount, short* arrayKeys, AtkArrayData** arrays, ref int selectedIndex)
    {
        using var table = ImRaii.Table("ArkArrayTable"u8, 3, ImGuiTableFlags.ScrollY | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings, new Vector2(300, -1));
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Size"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        var hasSearchTerm = !string.IsNullOrEmpty(_searchTerm);

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
                    var isNull = (nint)stringArrayData->StringArray[rowIndex].Value == 0;
                    if (isNull)
                        continue;

                    if (stringArrayData->StringArray[rowIndex].ExtractText().Contains(_searchTerm, StringComparison.InvariantCultureIgnoreCase))
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
                ImGui.Text(Enum.GetName(arrayType, arrayIndex));
            }
            else if (inUse && arrays[arrayIndex]->SubscribedAddonsCount > 0)
            {
                var raptureAtkUnitManager = RaptureAtkUnitManager.Instance();

                for (var j = 0; j < arrays[arrayIndex]->SubscribedAddonsCount; j++)
                {
                    if (arrays[arrayIndex]->SubscribedAddons[j] == 0)
                        continue;

                    using (ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF))
                        ImGui.Text(raptureAtkUnitManager->GetAddonById(arrays[arrayIndex]->SubscribedAddons[j])->NameString);
                    break;
                }
            }

            ImGui.TableNextColumn(); // Size
            if (inUse)
                ImGui.Text((rowsFound > 0 ? rowsFound : arrays[arrayIndex]->Size).ToString());
        }
    }

    private void DrawArrayHeader(Type? arrayType, string type, int index, AtkArrayData* array)
    {
        ImGui.Text($"{type} Array #{index}");

        if (arrayType != null && Enum.IsDefined(arrayType, index))
        {
            ImGui.SameLine(0, 0);
            ImGui.Text($" ({Enum.GetName(arrayType, index)})");
        }

        ImGui.SameLine();
        ImGui.Text("–"u8);
        ImGui.SameLine();
        ImGui.Text("Address: "u8);
        ImGui.SameLine(0, 0);
        DrawCopyableText($"0x{(nint)array:X}", "Copy address");

        if (array->SubscribedAddonsCount > 0)
        {
            ImGui.SameLine();
            ImGui.Text("–"u8);
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF))
                ImGui.Text($"{array->SubscribedAddonsCount} Subscribed Addon" + (array->SubscribedAddonsCount > 1 ? 's' : string.Empty));

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

                        ImGui.Text(raptureAtkUnitManager->GetAddonById(array->SubscribedAddons[j])->NameString);
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
            _numberType,
            atkArrayDataHolder.NumberArrayCount,
            atkArrayDataHolder.NumberArrayKeys,
            (AtkArrayData**)atkArrayDataHolder.NumberArrays,
            ref _selectedNumberArray);

        if (_selectedNumberArray >= atkArrayDataHolder.NumberArrayCount || atkArrayDataHolder.NumberArrayKeys[_selectedNumberArray] == -1)
            _selectedNumberArray = 0;

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        using var child = ImRaii.Child("AtkArrayContent", new Vector2(-1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!child) return;

        var array = atkArrayDataHolder.NumberArrays[_selectedNumberArray];
        DrawArrayHeader(_numberType, "Number", _selectedNumberArray, (AtkArrayData*)array);

        using var table = ImRaii.Table("NumberArrayDataTable"u8, 7, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Entry Address"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Integer"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Short"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Byte"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Float"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Hex"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(7, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < array->Size; i++)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.Text($"#{i}");

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
                ImGui.InputTextWithHint("##TextSearch", _textService.Translate("SearchBar.Hint"), ref _searchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);

                DrawArrayList(
                    _stringType,
                    atkArrayDataHolder.StringArrayCount,
                    atkArrayDataHolder.StringArrayKeys,
                    (AtkArrayData**)atkArrayDataHolder.StringArrays,
                    ref _selectedStringArray);
            }
        }

        if (_selectedStringArray >= atkArrayDataHolder.StringArrayCount || atkArrayDataHolder.StringArrayKeys[_selectedStringArray] == -1)
            _selectedStringArray = 0;

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        using var child = ImRaii.Child("AtkArrayContent", new Vector2(-1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!child) return;

        var array = atkArrayDataHolder.StringArrays[_selectedStringArray];
        DrawArrayHeader(_stringType, "String", _selectedStringArray, (AtkArrayData*)array);
        ImGui.Checkbox("Hide unset entries##HideUnsetStringArrayEntriesCheckbox", ref _hideUnsetStringArrayEntries);
        ImGui.SameLine();
        ImGui.Checkbox("Show text address##WordWrapCheckbox", ref _showTextAddress);
        ImGui.SameLine();
        ImGui.Checkbox("Show macro string##RenderStringsCheckbox", ref _showMacroString);

        using var table = ImRaii.Table("StringArrayDataTable"u8, 4, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn(_showTextAddress ? "Text Address" : "Entry Address", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Managed"u8, ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Text"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(4, 1);
        ImGui.TableHeadersRow();

        var hasSearchTerm = !string.IsNullOrEmpty(_searchTerm);

        for (var i = 0; i < array->Size; i++)
        {
            var isNull = (nint)array->StringArray[i].Value == 0;
            if (isNull && _hideUnsetStringArrayEntries)
                continue;

            if (hasSearchTerm)
            {
                if (isNull)
                    continue;

                if (!array->StringArray[i].ExtractText().Contains(_searchTerm, StringComparison.InvariantCultureIgnoreCase))
                    continue;
            }

            using var disabledColor = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), isNull);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.Text($"#{i}");

            ImGui.TableNextColumn(); // Address
            if (_showTextAddress)
            {
                if (!isNull)
                    DrawCopyableText($"0x{(nint)array->StringArray[i].Value:X}", "Copy text address");
            }
            else
            {
                DrawCopyableText($"0x{(nint)(&array->StringArray[i]):X}", "Copy entry address");
            }

            ImGui.TableNextColumn(); // Managed
            if (!isNull)
            {
                ImGui.Text(((nint)array->StringArray[i].Value != 0 && array->ManagedStringArray[i] == array->StringArray[i]).ToString());
            }

            ImGui.TableNextColumn(); // Text
            if (!isNull)
            {
                if (_showMacroString)
                {
                    DrawCopyableText(array->StringArray[i].ToString(), "Copy text");
                }
                else
                {
                    _debugRenderer.DrawSeString(array->StringArray[i], new NodeOptions() { AddressPath = new AddressPath([(nint)array, (nint)array->StringArray[i].Value]) });
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
            _extendType,
            atkArrayDataHolder.ExtendArrayCount,
            atkArrayDataHolder.ExtendArrayKeys,
            (AtkArrayData**)atkArrayDataHolder.ExtendArrays,
            ref _selectedExtendArray);

        if (_selectedExtendArray >= atkArrayDataHolder.ExtendArrayCount || atkArrayDataHolder.ExtendArrayKeys[_selectedExtendArray] == -1)
            _selectedExtendArray = 0;

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        using var child = ImRaii.Child("AtkArrayContent", new Vector2(-1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);

        var array = atkArrayDataHolder.ExtendArrays[_selectedExtendArray];
        DrawArrayHeader(null, "Extend", _selectedExtendArray, (AtkArrayData*)array);
        ImGui.Checkbox("Hide unset entries##HideUnsetExtendArrayEntriesCheckbox", ref _hideUnsetExtendArrayEntries);

        using var table = ImRaii.Table("ExtendArrayDataTable"u8, 3, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Entry Address"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Pointer"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < array->Size; i++)
        {
            var isNull = (nint)array->DataArray[i] == 0;
            if (isNull && _hideUnsetExtendArrayEntries)
                continue;

            using var disabledColor = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), isNull);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.Text($"#{i}");

            ImGui.TableNextColumn(); // Address
            DrawCopyableText($"0x{(nint)(&array->DataArray[i]):X}", "Copy entry address");

            ImGui.TableNextColumn(); // Pointer
            if (!isNull)
            {
                var marker = (MapMarkerBase*)array->DataArray[i];
                _debugRenderer.DrawIcon(marker->IconId);
                _debugRenderer.DrawPointerType(array->DataArray[i], typeof(MapMarkerBase), new NodeOptions()
                {
                    SeStringTitle = new ReadOnlySeString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(marker->Subtext)),
                    AddressPath = new AddressPath([(nint)array, (nint)array->DataArray[i]])
                });
            }
        }
    }
}
