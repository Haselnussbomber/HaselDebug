using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Utils;
using KernelTexture = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Texture;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawAtkTexture(nint address, NodeOptions nodeOptions)
    {
        if (address == 0)
        {
            ImGui.Text("null"u8);
            return;
        }

        if (!_processInfoService.IsPointerValid(address))
        {
            ImGui.Text("invalid"u8);
            return;
        }

        var tex = (AtkTexture*)address;
        if (!tex->IsTextureReady())
        {
            ImGui.Text("Texture not ready"u8);
            return;
        }

        var path = string.Empty;
        var title = "AtkTexture";

        if (tex->TextureType == TextureType.Resource)
        {
            path = tex->Resource->TexFileResourceHandle->ResourceHandle.FileName.ToString();
            title = path;
        }

        var kernelTexture = tex->GetKernelTexture();
        if (kernelTexture == null)
        {
            ImGui.Text("No KernelTexture"u8);
            return;
        }

        DrawTexture((nint)kernelTexture, nodeOptions.WithAddress(address).WithTitle(title), path);
    }

    public void DrawTexture(nint address, NodeOptions nodeOptions, string? path = null)
    {
        if (address == 0)
        {
            ImGui.Text("null"u8);
            return;
        }

        if (!_processInfoService.IsPointerValid(address))
        {
            ImGui.Text("invalid"u8);
            return;
        }

        nodeOptions = nodeOptions.WithAddress(address);

        var tex = (KernelTexture*)address;

        var title = $"{tex->ActualWidth}x{tex->ActualHeight}, {tex->TextureFormat}";
        if (!string.IsNullOrEmpty(nodeOptions.Title))
            title = $"{nodeOptions.Title} ({title})";
        nodeOptions = nodeOptions.WithTitle(title);

        nodeOptions.DrawContextMenu = (nodeOptions, builder) =>
        {
            builder.Add(new ImGuiContextMenuEntry()
            {
                Visible = !string.IsNullOrEmpty(path),
                Label = "Copy Path",
                ClickCallback = () => ImGui.SetClipboardText(path)
            });

            builder.Add(new ImGuiContextMenuEntry()
            {
                Label = "Copy Size",
                ClickCallback = () => ImGui.SetClipboardText($"{tex->ActualWidth}x{tex->ActualHeight}")
            });

            builder.Add(new ImGuiContextMenuEntry()
            {
                Label = "Copy Format",
                ClickCallback = () => ImGui.SetClipboardText($"{tex->TextureFormat}")
            });
        };

        using var node = DrawTreeNode(nodeOptions with { Title = title });
        if (!node) return;

        var size = new Vector2(tex->ActualWidth, tex->ActualHeight);
        var availSize = ImGui.GetContentRegionAvail();

        var scale = availSize.X / size.X;
        var scaledSize = new Vector2(size.X * scale, size.Y * scale);

        ImGui.Image(new ImTextureID(tex->D3D11ShaderResourceView), availSize.X < size.X ? scaledSize : size);
    }
}
