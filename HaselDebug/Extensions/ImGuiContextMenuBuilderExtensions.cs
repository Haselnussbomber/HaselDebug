using HaselCommon.Services;
using ImGuiNET;

namespace HaselDebug.Extensions;

public static class ImGuiContextMenuBuilderExtensions
{
    public static void AddCopyName(this ImGuiContextMenuBuilder builder, TextService textService, string name)
    {
        builder.Add(new ImGuiContextMenuEntry()
        {
            Visible = !string.IsNullOrEmpty(name),
            Label = textService.Translate("ContextMenu.CopyName"),
            ClickCallback = () => ImGui.SetClipboardText(name)
        });
    }

    public static void AddCopyAddress(this ImGuiContextMenuBuilder builder, TextService textService, nint address)
    {
        builder.Add(new ImGuiContextMenuEntry()
        {
            Visible = address != 0,
            Label = textService.Translate("ContextMenu.CopyAddress"),
            ClickCallback = () => ImGui.SetClipboardText(address.ToString("X"))
        });
    }
}
