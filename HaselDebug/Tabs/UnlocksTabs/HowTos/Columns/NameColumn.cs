using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.HowTos.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<HowTo>
{
    private readonly UnlocksTabUtils _unlocksTabUtils;

    [AutoPostConstruct]
    private void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(HowTo row)
        => row.Name.ToString();

    public override unsafe void DrawColumn(HowTo row)
    {
        var isUnlocked = UIState.Instance()->IsHowToUnlocked(row.RowId);

        if (ImGui.Selectable(ToName(row), false, isUnlocked ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled))
        {
            var agentHowTo = AgentHowToExt.Instance();
            agentHowTo->RowId = row.RowId;
            agentHowTo->Unk44 = 0;
            agentHowTo->AgentInterface.Show();
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            if (isUnlocked)
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            _unlocksTabUtils.DrawHowToTooltip(row);
        }
    }
}

// [GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x98)]
public unsafe partial struct AgentHowToExt
{
    public static AgentHowToExt* Instance() => (AgentHowToExt*)AgentModule.Instance()->GetAgentByInternalId(AgentId.Howto);

    [FieldOffset(0x00)] public AgentInterface AgentInterface;
    [FieldOffset(0x40)] public uint RowId;
    [FieldOffset(0x44)] public uint Unk44;

    [FieldOffset(0x49)] public bool IsPadMode;

    [FieldOffset(0x7C)] public uint PageCount;

    [FieldOffset(0x88)] public nint HowToRowPtr;

    // [MemberFunction("E8 ?? ?? ?? ?? BA ?? ?? ?? ?? 48 8D 4D ?? 8B D8 E8 ?? ?? ?? ?? 89 5D ?? 48 8D 4D ?? 8B 9F")]
    // public partial uint GetAdjustedHowToIconId(nint rowPtr, byte startTown, byte grandCompany);

    // [MemberFunction("E8 ?? ?? ?? ?? EB ?? BA ?? ?? ?? ?? C7 87")]
    // public static partial CStringPointer GetAdjustedHowToText(UIModule* thisPtr, nint rowPtr, byte startTown, byte grandCompany);
}
