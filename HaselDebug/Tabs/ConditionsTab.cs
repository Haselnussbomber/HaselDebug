using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ConditionsTab : DebugTab
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly DebugRenderer _debugRenderer;

    public override void Draw()
    {
        var conditions = Conditions.Instance();
        if (conditions == null) return;

        _debugRenderer.DrawPointerType(conditions, typeof(Conditions), new Utils.NodeOptions());

        foreach (var fieldInfo in typeof(Conditions)
            .GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(fieldInfo => fieldInfo.FieldType == typeof(bool) && fieldInfo.GetCustomAttribute<ObsoleteAttribute>() == null))
        {
            var offset = fieldInfo.GetFieldOffset();
            var value = *(bool*)((nint)conditions + offset);
            if (!value) continue;

            ImGui.TextUnformatted($"#{offset}:");
            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
            var startPos = ImGui.GetWindowPos() + ImGui.GetCursorPos() - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());
            ImGui.TextUnformatted(fieldInfo.Name);

            var fullName = (fieldInfo.DeclaringType != null ? fieldInfo.DeclaringType.FullName + "." : string.Empty) + fieldInfo.Name;

            if (!_debugRenderer.HasDocumentation(fullName))
                continue;

            var textSize = ImGui.CalcTextSize(fieldInfo.Name);
            ImGui.GetWindowDrawList().AddLine(startPos + new Vector2(0, textSize.Y), startPos + textSize, ImGui.GetColorU32(ImGuiCol.Text));

            if (!ImGui.IsItemHovered())
                continue;

            using var tooltip = ImRaii.Tooltip();

            var doc = _debugRenderer.GetDocumentation(fullName);
            if (doc == null)
                continue;

            using var font = _pluginInterface.UiBuilder.MonoFontHandle.Push();
            ImGui.TextUnformatted(fieldInfo.Name);
            ImGui.Separator();

            if (!string.IsNullOrEmpty(doc.Sumamry))
                ImGui.TextUnformatted(doc.Sumamry);

            if (!string.IsNullOrEmpty(doc.Remarks))
                ImGui.TextUnformatted(doc.Remarks);

            if (doc.Parameters.Length > 0)
            {
                foreach (var param in doc.Parameters)
                {
                    ImGui.TextUnformatted($"{param.Key}: {param.Value}");
                }
            }

            if (!string.IsNullOrEmpty(doc.Returns))
                ImGui.TextUnformatted(doc.Returns);
        }
    }
}
