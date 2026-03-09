using Dalamud.Interface.Textures;

namespace HaselDebug.Services;

public partial class DebugRenderer
{
    public void DrawIcon(object value, Type? type = null, bool isHq = false, bool sameLine = true, DrawInfo drawInfo = default, bool canCopy = true, bool noTooltip = false)
    {
        if (value == null)
        {
            DrawIcon(0, isHq, sameLine, drawInfo, canCopy, noTooltip);
            return;
        }

        var iconId = (type ?? value.GetType()) switch
        {
            Type t when t == typeof(short) => (short)value > 0 ? (uint)(short)value : 0u,
            Type t when t == typeof(ushort) => (ushort)value,
            Type t when t == typeof(int) => (int)value > 0 ? (uint)(int)value : 0u,
            Type t when t == typeof(uint) => (uint)value,
            _ => 0u
        };

        DrawIcon(iconId, isHq, sameLine, drawInfo, canCopy, noTooltip);
    }

    public void DrawIcon(uint iconId, bool isHq = false, bool sameLine = true, DrawInfo drawInfo = default, bool canCopy = true, bool noTooltip = false)
    {
        drawInfo.DrawSize ??= new Vector2(ImGui.GetTextLineHeight());

        if (iconId == 0)
        {
            ImGui.Dummy(drawInfo.DrawSize.Value);
            if (sameLine)
                ImGui.SameLine();
            return;
        }

        if (!ImGui.IsRectVisible(drawInfo.DrawSize.Value))
        {
            ImGui.Dummy(drawInfo.DrawSize.Value);
            if (sameLine)
                ImGui.SameLine();
            return;
        }

        if (_textureProvider.TryGetFromGameIcon(new GameIconLookup(iconId, isHq), out var tex) && tex.TryGetWrap(out var texture, out _))
        {
            ImGui.Image(texture.Handle, drawInfo.DrawSize.Value);

            if (ImGui.IsItemHovered())
            {
                if (canCopy)
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (!noTooltip)
                {
                    ImGui.BeginTooltip();
                    if (canCopy)
                        ImGui.Text("Click to copy IconId"u8);
                    ImGui.Text($"ID: {iconId} – Size: {texture.Width}x{texture.Height}");
                    ImGui.Image(texture.Handle, new(texture.Width, texture.Height));
                    ImGui.EndTooltip();
                }
            }

            if (canCopy && ImGui.IsItemClicked())
                ImGui.SetClipboardText(iconId.ToString());
        }
        else
        {
            ImGui.Dummy(drawInfo.DrawSize.Value);
        }

        if (sameLine)
            ImGui.SameLine();
    }

    public void DrawTexture(string path, bool sameLine = true, DrawInfo drawInfo = default, bool canCopy = true, bool noTooltip = false)
    {
        drawInfo.DrawSize ??= new Vector2(ImGui.GetTextLineHeight());

        if (string.IsNullOrEmpty(path))
        {
            ImGui.Dummy(drawInfo.DrawSize.Value);
            if (sameLine)
                ImGui.SameLine();
            return;
        }

        if (!ImGui.IsRectVisible(drawInfo.DrawSize.Value))
        {
            ImGui.Dummy(drawInfo.DrawSize.Value);
            if (sameLine)
                ImGui.SameLine();
            return;
        }

        if (_textureProvider.GetFromGame(path).TryGetWrap(out var texture, out _))
        {
            ImGui.Image(texture.Handle, drawInfo.DrawSize.Value);

            if (ImGui.IsItemHovered())
            {
                if (canCopy)
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (!noTooltip)
                {
                    ImGui.BeginTooltip();
                    if (canCopy)
                        ImGui.Text("Click to copy IconId"u8);
                    ImGui.Text($"Path: {path} – Size: {texture.Width}x{texture.Height}");
                    ImGui.Image(texture.Handle, new(texture.Width, texture.Height));
                    ImGui.EndTooltip();
                }
            }

            if (canCopy && ImGui.IsItemClicked())
                ImGui.SetClipboardText(path.ToString());
        }
        else
        {
            ImGui.Dummy(drawInfo.DrawSize.Value);
        }

        if (sameLine)
            ImGui.SameLine();
    }
}
