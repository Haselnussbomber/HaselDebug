namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawAddress(void* obj)
        => DrawAddress((nint)obj);

    public void DrawAddress(nint address)
    {
        if (address == 0)
        {
            ImGui.Text("null");
            return;
        }

        var displayText = ImGui.IsKeyDown(ImGuiKey.LeftShift)
            ? $"0x{address:X}"
            : _processInfoService.GetAddressName(address);

        ImGuiUtils.DrawCopyableText(displayText);
    }
}
