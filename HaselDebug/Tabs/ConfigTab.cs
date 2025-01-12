using System.Collections.Generic;
using System.Text;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using ImGuiNET;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class ConfigTab(TextService TextService, DebugRenderer DebugRenderer) : DebugTab
{
    private string _searchTerm = string.Empty;

    public override void Draw()
    {
        ref var commonSystemConfig = ref Framework.Instance()->SystemConfig;

        if (ImGui.Button("Copy enum for CS"))
        {
            var sb = new StringBuilder();
            var dict = new Dictionary<int, string>();

            ProcessConfigBase(sb, dict, ref commonSystemConfig.SystemConfigBase.ConfigBase, "System");
            ProcessConfigBase(sb, dict, ref commonSystemConfig.UiConfig, "UiConfig");
            ProcessConfigBase(sb, dict, ref commonSystemConfig.UiControlConfig, "UiControl");
            ProcessConfigBase(sb, dict, ref commonSystemConfig.UiControlGamepadConfig, "UiControlGamepad"); // nothing in here

            ImGui.SetClipboardText(sb.ToString());
        }

        ImGui.SameLine();

        if (ImGui.Button("Copy enum for Dalamud"))
        {
            var sb = new StringBuilder();

            ProcessConfigBaseDalamud(sb, ref commonSystemConfig.SystemConfigBase.ConfigBase, "System");
            ProcessConfigBaseDalamud(sb, ref commonSystemConfig.UiConfig, "UiConfig");
            ProcessConfigBaseDalamud(sb, ref commonSystemConfig.UiControlConfig, "UiControl");
            ProcessConfigBaseDalamud(sb, ref commonSystemConfig.UiControlGamepadConfig, "UiControlGamepad");

            ImGui.SetClipboardText(sb.ToString());
        }

        ImGui.Separator();

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##TextSearch", TextService.Translate("SearchBar.Hint"), ref _searchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);

        using var tabBar = ImRaii.TabBar("ConfigTabs");
        if (!tabBar) return;

        DrawConfigTab(ref commonSystemConfig.SystemConfigBase.ConfigBase, "System");
        DrawConfigTab(ref commonSystemConfig.UiConfig, "UiConfig");
        DrawConfigTab(ref commonSystemConfig.UiControlConfig, "UiControl");
        DrawConfigTab(ref commonSystemConfig.UiControlGamepadConfig, "UiControlGamepad");
    }

    private int GetNumSearchResults(ref ConfigBase configBase)
    {
        var count = 0;

        for (var index = 0u; index < configBase.ConfigCount; index++)
        {
            var option = configBase.GetConfigOption(index);
            if (option == null || option->Type == 0)
                continue;

            var optionName = Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(option->Name));
            if (!optionName.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
                continue;

            count++;
        }

        return count;
    }

    private void DrawConfigTab(ref ConfigBase configBase, string configName)
    {
        var tabTitle = configName;
        var hasSearchTerm = !string.IsNullOrWhiteSpace(_searchTerm);
        var numSearchResults = hasSearchTerm ? GetNumSearchResults(ref configBase) : 0;

        if (hasSearchTerm)
            tabTitle += $" ({numSearchResults})";

        using var tab = ImRaii.TabItem(tabTitle);
        if (!tab) return;

        using var table = ImRaii.Table("ConfigOptionTable", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable | ImGuiTableFlags.NoSavedSettings, ImGui.GetContentRegionAvail());
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Default", ImGuiTableColumnFlags.WidthFixed, 160);
        ImGui.TableSetupColumn("Min", ImGuiTableColumnFlags.WidthFixed, 160);
        ImGui.TableSetupColumn("Max", ImGuiTableColumnFlags.WidthFixed, 160);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var index = 0u; index < configBase.ConfigCount; index++)
        {
            var option = configBase.GetConfigOption(index);
            if (option == null || option->Type == 0)
                continue;

            var optionName = Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(option->Name));
            if (!optionName.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
                continue;

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            DebugRenderer.DrawCopyableText(option->Index.ToString());

            ImGui.TableNextColumn(); // Type
            switch (option->Type)
            {
                case 0: // Empty
                    break;

                case 1: // Category
                    ImGui.TextUnformatted("Category");
                    break;

                case 2: // UInt
                    ImGui.TextUnformatted("UInt");
                    break;

                case 3: // Float
                    ImGui.TextUnformatted("Float");
                    break;

                case 4: // String
                    ImGui.TextUnformatted("String");
                    break;

                default:
                    ImGui.TextUnformatted($"Unknown type {option->Type}");
                    break;
            }

            ImGui.TableNextColumn(); // Name
            DebugRenderer.DrawCopyableText(optionName, highligtedText: hasSearchTerm ? _searchTerm : null);

            switch (option->Type)
            {
                case 0: // Empty
                case 1: // Category
                    break;

                case 2: // UInt
                    ImGui.TableNextColumn(); // Value
                    DebugRenderer.DrawNumeric((nint)(&option->Value.UInt), typeof(uint), default);

                    ImGui.TableNextColumn(); // Default
                    DebugRenderer.DrawNumeric((nint)(&option->Properties.UInt.DefaultValue), typeof(uint), default);

                    ImGui.TableNextColumn(); // Min
                    DebugRenderer.DrawNumeric((nint)(&option->Properties.UInt.MinValue), typeof(uint), default);

                    ImGui.TableNextColumn(); // Max
                    DebugRenderer.DrawNumeric((nint)(&option->Properties.UInt.MaxValue), typeof(uint), default);
                    break;

                case 3: // Float
                    ImGui.TableNextColumn(); // Value
                    DebugRenderer.DrawNumeric((nint)(&option->Value.Float), typeof(float), default);

                    ImGui.TableNextColumn(); // Default
                    DebugRenderer.DrawNumeric((nint)(&option->Properties.Float.DefaultValue), typeof(float), default);

                    ImGui.TableNextColumn(); // Min
                    DebugRenderer.DrawNumeric((nint)(&option->Properties.Float.MinValue), typeof(float), default);

                    ImGui.TableNextColumn(); // Max
                    DebugRenderer.DrawNumeric((nint)(&option->Properties.Float.MaxValue), typeof(float), default);
                    break;

                case 4: // String
                    ImGui.TableNextColumn(); // Value
                    DebugRenderer.DrawCopyableText(option->Properties.String.DefaultValue->ToString());

                    ImGui.TableNextColumn(); // Default
                    ImGui.TableNextColumn(); // Min
                    ImGui.TableNextColumn(); // Max
                    break;

                default:
                    ImGui.TableNextColumn(); // Value
                    ImGui.TableNextColumn(); // Default
                    ImGui.TableNextColumn(); // Min
                    ImGui.TableNextColumn(); // Max
                    break;
            }
        }
    }

    private void ProcessConfigBase(StringBuilder sb, Dictionary<int, string> dict, ref ConfigBase configBase, string configName)
    {
        sb.AppendLine("");
        sb.AppendLine($"    #region {configName}");

        var configEntry = configBase.ConfigEntry;
        for (var i = 0; i < configBase.ConfigCount; i++, configEntry++)
        {
            if (configEntry->Type == 0)
                continue;

            if (dict.ContainsKey(i))
                continue;

            var name = configEntry->Name != null
                ? MemoryHelper.ReadStringNullTerminated((nint)configEntry->Name)
                : "";

            if (dict.ContainsValue(name))
                name = $"{name}_{i}";

            if (configEntry->Type == 1)
            {
                sb.AppendLine($"    // {name}");
                continue;
            }

            var type = configEntry->Type switch
            {
                2 => "uint",
                3 => "float",
                4 => "string",
                _ => $"{configEntry->Type}"
            };

            sb.AppendLine($"    {name} = {i},");
            dict.Add(i, name);
        }

        sb.AppendLine($"    #endregion");
    }

    private void ProcessConfigBaseDalamud(StringBuilder sb, ref ConfigBase configBase, string configName)
    {
        var dict = new Dictionary<int, string>();

        sb.AppendLine("");
        sb.AppendLine($"public enum {configName}ConfigOption {{");

        var usedNames = new HashSet<string>();

        var configEntry = configBase.ConfigEntry;
        for (var i = 0; i < configBase.ConfigCount; i++, configEntry++)
        {
            if (configEntry->Type == 0)
                continue;

            if (dict.ContainsKey(i))
                continue;

            var name = configEntry->Name != null
                ? MemoryHelper.ReadStringNullTerminated((nint)configEntry->Name)
                : "";

            // Dalamud doesn't support multiple options with the same name
            if (!usedNames.Add(name))
                continue;

            if (configEntry->Type == 1)
                continue;

            var type = configEntry->Type switch
            {
                2 => "UInt",
                3 => "Float",
                4 => "String",
                _ => $"{configEntry->Type}"
            };

            sb.AppendLine($"\r\n    /// <summary>\r\n    /// {configName} option with the internal name {name}.\r\n    /// This option is a {type}.\r\n    /// </summary>\r\n    [GameConfigOption(\"{name}\", ConfigType.{type})]\r\n    {name},");
            dict.Add(i, name);
        }

        sb.AppendLine("}");
    }
}
