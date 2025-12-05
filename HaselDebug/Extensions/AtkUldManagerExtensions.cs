using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselDebug.Extensions;

public static unsafe class AtkUldManagerExtensions
{
    public static bool ContainsNode(ref this AtkUldManager uldManager, AtkResNode* needle)
    {
        foreach (var node in uldManager.Nodes)
        {
            if (node.Value == null)
                continue;

            if (node == needle)
                return true;

            if (node.Value->GetNodeType() != NodeType.Component)
                continue;

            var componentNode = (AtkComponentNode*)node.Value;
            if (componentNode->Component == null)
                continue;

            if (componentNode->Component->UldManager.ContainsNode(needle))
                return true;
        }

        return false;
    }
}
