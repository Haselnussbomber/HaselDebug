using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class LgbInspectorTab : DebugTab
{
    private readonly IGameGui _gameGui;
    private readonly DebugRenderer _debugRenderer;

    public override void Draw()
    {
        var activeLayout = LayoutWorld.Instance()->ActiveLayout;
        if (activeLayout == null)
        {
            ImGui.Text("No ActiveLayout"u8);
            return;
        }

        if (activeLayout->InitState != 7)
        {
            ImGui.Text($"InitState: {activeLayout->InitState}");
            return;
        }

        using (var stringsTreeNode = ImRaii.TreeNode("ResourcePaths", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            if (stringsTreeNode)
            {
                foreach (var str in activeLayout->ResourcePaths.Strings)
                {
                    ImGuiUtils.DrawCopyableText(str.Value->DataString);
                }
            }
        }

        ImGui.Separator();

        foreach (var (type, instances) in activeLayout->InstancesByType)
        {
            using var treeNode = ImRaii.TreeNode(type.ToString(), ImGuiTreeNodeFlags.SpanAvailWidth);
            if (!treeNode)
                continue;

            foreach (var (id, instance) in *instances.Value)
            {
                using var disabled = ImRaii.Disabled(!instance.Value->IsActive);

                var title = $"[{instance.Value->Id.Type}] InstanceKey: {instance.Value->Id.InstanceKey} | LayerKey: {instance.Value->Id.LayerKey}";

                using var innerTreeNode = ImRaii.TreeNode(title, ImGuiTreeNodeFlags.SpanAvailWidth);

                if (ImGui.IsItemHovered())
                {
                    var transform = *instance.Value->GetTransformImpl();
                    if (_gameGui.WorldToScreen(transform.Translation, out var screenPos))
                    {
                        var drawList = ImGui.GetForegroundDrawList();
                        drawList.AddLine(ImGui.GetMousePos(), screenPos, Color.Orange.ToUInt());
                        drawList.AddCircleFilled(screenPos, 3f, Color.Orange.ToUInt());
                    }
                }

                if (!innerTreeNode)
                    continue;

                var nodeOptions = new NodeOptions()
                {
                    DefaultOpen = true
                };

                _debugRenderer.DrawPointerType(instance, nodeOptions);

                var graphics = instance.Value->GetGraphics2();
                if (graphics != null)
                {
                    _debugRenderer.DrawPointerType(graphics, nodeOptions);
                }
            }
        }
    }
}
