using System.Numerics;
using Dalamud.Interface.Utility.Raii;
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
            ImGui.TextUnformatted("Texture not ready");
            return;
        }

        var title = "AtkTexture";
        if (tex->TextureType == TextureType.Resource)
            title = tex->Resource->TexFileResourceHandle->ResourceHandle.FileName.ToString();

        var kernelTexture = tex->GetKernelTexture();
        if (kernelTexture == null)
        {
            ImGui.TextUnformatted("No KernelTexture");
            return;
        }

        DrawTexture((nint)kernelTexture, nodeOptions.WithAddress(address).WithSeStringTitle(title));
    }

    public void DrawTexture(nint address, NodeOptions nodeOptions)
    {
        if (address == 0)
        {
            ImGui.TextUnformatted("null");
            return;
        }

        nodeOptions = nodeOptions.WithAddress(address);

        var tex = (KernelTexture*)address;
        var title = $"{tex->ActualWidth}x{tex->ActualHeight}, {(TextureFormat)tex->TextureFormat}";
        if (nodeOptions.SeStringTitle != null)
            title = $"{nodeOptions.SeStringTitle.Value.ExtractText()} ({title})";
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

    // See https://github.com/aers/FFXIVClientStructs/pull/1065
    private enum TextureFormat : uint
    {
        B8G8R8A8_UNORM_4 = 0x1130,
        A8_UNORM = 0x1131,
        R8_UNORM = 0x1132,
        R8_UINT = 0x1133,
        R16_UINT = 0x1140,
        R32_UINT = 0x1150,
        R8G8_UNORM = 0x1240,
        B8G8R8A8_UNORM_2 = 0x1440,
        B8G8R8A8_UNORM_3 = 0x1441,
        B8G8R8A8_UNORM = 0x1450,
        B8G8R8X8_UNORM = 0x1451,
        R16_FLOAT = 0x2140,
        R32_FLOAT = 0x2150,
        R16G16_FLOAT = 0x2250,
        R32G32_FLOAT = 0x2260,
        R11G11B10_FLOAT = 0x2350,
        R16G16B16A16_FLOAT = 0x2460,
        R32G32B32A32_FLOAT = 0x2470,
        BC1_UNORM = 0x3420,
        BC2_UNORM = 0x3430,
        BC3_UNORM = 0x3431,
        /// <remarks> Can also be R16_TYPELESS or R16_UNORM depending on context. </remarks>
        D16_UNORM = 0x4140,
        /// <remarks> Can also be R24G8_TYPELESS or R24_UNORM_X8_TYPELESS depending on context. </remarks>
        D24_UNORM_S8_UINT = 0x4250, // depth 28 stencil 8, see MS texture formats on google if you really care :)
        /// <remarks> Can also be R16_TYPELESS or R16_UNORM depending on context. </remarks>
        D16_UNORM_2 = 0x5140,
        /// <remarks> Can also be R24G8_TYPELESS or R24_UNORM_X8_TYPELESS depending on context. </remarks>
        D24_UNORM_S8_UINT_2 = 0x5150,
        BC4_UNORM = 0x6120,
        BC5_UNORM = 0x6230,
        BC6H_SF16 = 0x6330,
        BC7_UNORM = 0x6432,
        R16_UNORM = 0x7140,
        R16G16_UNORM = 0x7250,
        R10G10B10A2_UNORM_2 = 0x7350,
        R10G10B10A2_UNORM = 0x7450,
        /// <remarks> Can also be R24G8_TYPELESS or R24_UNORM_X8_TYPELESS depending on context. </remarks>
        D24_UNORM_S8_UINT_3 = 0x8250,
    }
}
