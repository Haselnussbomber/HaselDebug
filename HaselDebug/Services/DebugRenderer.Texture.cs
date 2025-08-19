using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Utils;
using KernelTexture = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Texture;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawAtkTexture(nint address, NodeOptions nodeOptions)
    {
        var tex = (AtkTexture*)address;
        if (!tex->IsTextureReady())
        {
            ImGui.Text("Texture not ready"u8);
            return;
        }

        var title = "AtkTexture";
        if (tex->TextureType == TextureType.Resource)
            title = tex->Resource->TexFileResourceHandle->ResourceHandle.FileName.ToString();

        var kernelTexture = tex->GetKernelTexture();
        if (kernelTexture == null)
        {
            ImGui.Text("No KernelTexture"u8);
            return;
        }

        DrawTexture((nint)kernelTexture, nodeOptions.WithAddress(address).WithSeStringTitle(title));
    }

    public void DrawTexture(nint address, NodeOptions nodeOptions)
    {
        if (address == 0)
        {
            ImGui.Text("null"u8);
            return;
        }

        nodeOptions = nodeOptions.WithAddress(address);

        var tex = (KernelTexture*)address;
        var title = $"{tex->ActualWidth}x{tex->ActualHeight}, {(TextureFormat)tex->TextureFormat}";
        if (nodeOptions.SeStringTitle != null)
            title = $"{nodeOptions.SeStringTitle.Value} ({title})";
        using var titleColor = ImRaii.PushColor(ImGuiCol.Text, ColorTreeNode.ToVector());
        using var node = ImRaii.TreeNode($"{title}##TextureNode{nodeOptions.AddressPath}", nodeOptions.GetTreeNodeFlags());
        if (!node) return;
        titleColor?.Dispose();

        var size = new Vector2(tex->ActualWidth, tex->ActualHeight);
        var availSize = ImGui.GetContentRegionAvail();

        var scale = availSize.X / size.X;
        var scaledSize = new Vector2(size.X * scale, size.Y * scale);

        ImGui.Image(new ImTextureID(tex->D3D11ShaderResourceView), availSize.X < size.X ? scaledSize : size);
    }
}
