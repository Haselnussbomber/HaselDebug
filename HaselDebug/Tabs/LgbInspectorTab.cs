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

    [StructLayout(LayoutKind.Explicit)]
    private struct LayoutWorldStub
    {
        [FieldOffset(0x28)] public LayoutManager* UnkLayout28;
    }

    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("LgbInspectorTabBar");
        if (!tabBar) return;

        var layoutWorld = LayoutWorld.Instance();

        DrawTab("Active", layoutWorld->ActiveLayout);
        DrawTab("Global", layoutWorld->GlobalLayout);
        DrawTab("Unk28", ((LayoutWorldStub*)layoutWorld)->UnkLayout28);
        DrawTab("Prefetch", layoutWorld->PrefetchLayout);
    }

    private void DrawTab(string label, LayoutManager* layout)
    {
        using var disabledTab = ImRaii.Disabled(layout == null);
        using var tab = ImRaii.TabItem(label);
        if (!tab) return;

        if (layout == null)
        {
            ImGui.Text("null"u8);
            return;
        }

        if (layout->InitState != 7)
        {
            ImGui.Text($"InitState: {layout->InitState}");
            return;
        }

        using (var stringsTreeNode = ImRaii.TreeNode($"ResourcePaths ({layout->ResourcePaths.Strings.Count})", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            if (stringsTreeNode)
            {
                foreach (var str in layout->ResourcePaths.Strings)
                {
                    ImGuiUtils.DrawCopyableText(str.Value->DataString);
                }
            }
        }

        ImGui.Separator();

        foreach (var (type, instances) in layout->InstancesByType)
        {
            using var treeNode = ImRaii.TreeNode($"{type} ({instances.Value->Count})###" + type.ToString(), ImGuiTreeNodeFlags.SpanAvailWidth);
            if (!treeNode)
                continue;

            foreach (var (id, instance) in *instances.Value)
            {
                var title = $"[{instance.Value->Id.Type}] InstanceKey: {instance.Value->Id.InstanceKey} | LayerKey: {instance.Value->Id.LayerKey} | u0: {instance.Value->Id.u0} | SubId: 0x{instance.Value->SubId:X}";

                using var col = Color.From(ImGuiCol.TextDisabled).Push(ImGuiCol.Text);
                using var innerTreeNode = ImRaii.TreeNode(title, ImGuiTreeNodeFlags.SpanAvailWidth);
                col.Dispose();

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
