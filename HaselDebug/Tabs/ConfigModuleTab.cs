using System.Collections.Generic;
using System.Text;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using HaselDebug.Abstracts;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class ConfigModuleTab : DebugTab
{
    public override void Draw()
    {
        if (ImGui.Button("Copy enum for CS"))
        {
            var sb = new StringBuilder();
            var dict = new Dictionary<int, string>();
            ref var commonSystemConfig = ref Framework.Instance()->SystemConfig;

            ProcessConfigBase(sb, dict, ref commonSystemConfig.SystemConfigBase.ConfigBase, "System");
            ProcessConfigBase(sb, dict, ref commonSystemConfig.UiConfig, "Ui");
            ProcessConfigBase(sb, dict, ref commonSystemConfig.UiControlConfig, "UiControl");
            ProcessConfigBase(sb, dict, ref commonSystemConfig.UiControlGamepadConfig, "UiControlGamepad"); // nothing in here

            ImGui.SetClipboardText(sb.ToString());
        }

        if (ImGui.Button("Copy enum for Dalamud"))
        {
            var sb = new StringBuilder();
            ref var commonSystemConfig = ref Framework.Instance()->SystemConfig;

            ProcessConfigBaseDalamud(sb, ref commonSystemConfig.SystemConfigBase.ConfigBase, "System");
            ProcessConfigBaseDalamud(sb, ref commonSystemConfig.UiConfig, "Ui");
            ProcessConfigBaseDalamud(sb, ref commonSystemConfig.UiControlConfig, "UiControl");
            ProcessConfigBaseDalamud(sb, ref commonSystemConfig.UiControlGamepadConfig, "UiControlGamepad");

            ImGui.SetClipboardText(sb.ToString());
        }

        // TODO: draw tables
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
                continue;

            var type = configEntry->Type switch
            {
                2 => "UInt",
                3 => "Float",
                4 => "String",
                _ => $"{configEntry->Type}"
            };

            sb.AppendLine($"\r\n    /// <summary>\r\n    /// System option with the internal name {name}.\r\n    /// This option is a {type}.\r\n    /// </summary>\r\n    [GameConfigOption(\"{name}\", ConfigType.{type})]\r\n    {name},");
            dict.Add(i, name);
        }

        sb.AppendLine("}");
    }
}
