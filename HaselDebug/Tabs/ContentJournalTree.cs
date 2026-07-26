using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ContentJournalTree : DebugTab
{
    private readonly DebugRenderer _debugRenderer;

    public override void Draw()
    {
        if (!TryGetAddon<AddonContentsFinder>("ContentsFinder", out var addon))
        {
            using var disabled = ImRaii.Disabled(!UIModule.Instance()->IsMainCommandUnlocked(33));
            if (ImGui.Button("Open Duty Finder"))
            {
                UIModule.Instance()->ExecuteMainCommand(33);
            }
            return;
        }

        _debugRenderer.DrawPointerType(addon);

        foreach (var item in addon->DutyList->Items)
        {
            _debugRenderer.DrawPointerType(item.Value, new Utils.NodeOptions() { SeStringTitle = item.Value->StringValues[0].AsReadOnlySeString() });

            if (item.Value->Type is TreeListItemType.Group)
                ImGui.Indent();

            if (item.Value->Type is TreeListItemType.LastItemInGroup)
                ImGui.Unindent();
        }
    }
}
