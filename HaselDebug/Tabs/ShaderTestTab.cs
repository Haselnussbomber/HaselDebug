using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision.Math;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;
using ID3D11Buffer = TerraFX.Interop.DirectX.ID3D11Buffer;
using ID3D11Device = TerraFX.Interop.DirectX.ID3D11Device;
using KernelDevice = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Device;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ShaderTestTab : DebugTab, IDisposable
{
    private readonly PluginAssemblyProvider _pluginAssembly;

    private IDXGISwapChain* _swapChain;
    private ID3D11Device* _device;
    private ID3D11DeviceContext* _context;

    private ID3D11Texture2D* _pass1RenderTargetTexture;
    private ID3D11RenderTargetView* _pass1RenderTargetView;
    private ID3D11ShaderResourceView* _pass1ShaderResourceView;

    private ID3D11Texture2D* _pass2RenderTargetTexture;
    private ID3D11RenderTargetView* _pass2RenderTargetView;
    private ID3D11ShaderResourceView* _pass2ShaderResourceView;

    private ID3D11VertexShader* _vertexShader;
    private ID3D11PixelShader* _pixelShader;
    private ID3D11InputLayout* _inputLayout;
    private ID3D11Buffer* _constantBuffer;
    private ID3D11Buffer* _vertexBuffer;
    private ID3D11SamplerState* _samplerState;
    private ID3D11Texture2D* _backBufferCopyTexture;
    private ID3D11ShaderResourceView* _backBufferCopySRV;

    private string _errorMessage = string.Empty;
    private bool _isDisposing;
    private float _blurStrength = 2;

    public void Dispose()
    {
        _isDisposing = true;

        Release(ref _vertexBuffer);
        Release(ref _constantBuffer);
        Release(ref _samplerState);
        Release(ref _pass1ShaderResourceView);
        Release(ref _pass1RenderTargetView);
        Release(ref _pass1RenderTargetTexture);
        Release(ref _pass2ShaderResourceView);
        Release(ref _pass2RenderTargetView);
        Release(ref _pass2RenderTargetTexture);
        Release(ref _vertexShader);
        Release(ref _pixelShader);
        Release(ref _inputLayout);
        Release(ref _backBufferCopySRV);
        Release(ref _backBufferCopyTexture);
        Release(ref _context);
        Release(ref _device);
        // Do not release _swapChain as per original logic
    }

    private void Release<T>(ref T* obj) where T : unmanaged
    {
        if (obj != null)
        {
            ((IUnknown*)obj)->Release();
            obj = null;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    private struct ShaderParams
    {
        public Vector2 WindowPos;
        public Vector2 WindowSize;
        public Vector2 Resolution;
        public Vector2 Direction;
        public float BlurStrength;
    }

    private bool InitializeDevice()
    {
        if (_device != null)
            return true;

        try
        {
            var kernelDevice = KernelDevice.Instance();
            if (kernelDevice == null || kernelDevice->SwapChain == null)
            {
                _errorMessage = "Failed to get kernel device or swap chain";
                return false;
            }

            _swapChain = (IDXGISwapChain*)kernelDevice->SwapChain->DXGISwapChain;

            Guid iid = __uuidof<ID3D11Device>();

            fixed (ID3D11Device** pDevice = &_device)
                _swapChain->GetDevice(&iid, (void**)pDevice);

            fixed (ID3D11DeviceContext** pContext = &_context)
                _device->GetImmediateContext(pContext);

            return true;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to initialize device: {ex.Message}";
            return false;
        }
    }

    private bool CompileShaders()
    {
        if (_vertexShader != null && _pixelShader != null)
            return true;

        ID3DBlob* vsBlob = null;
        ID3DBlob* psBlob = null;
        ID3DBlob* errorBlob = null;

        try
        {
            using var stream = _pluginAssembly.Assembly.GetManifestResourceStream("HaselDebug.Tabs.Shader.hlsl")!;
            using var reader = new StreamReader(stream);
            var shaderCode = reader.ReadToEnd();
            var shaderBytes = Encoding.ASCII.GetBytes(shaderCode);

            fixed (byte* pCode = shaderBytes)
            {
                // Vertex Shader

                fixed (byte* pEntrypoint = "VSMain"u8)
                fixed (byte* pTarget = "vs_5_0"u8)
                    DirectX.D3DCompile(pCode, (nuint)shaderBytes.Length, null, null, null, (sbyte*)pEntrypoint, (sbyte*)pTarget, 0, 0, &vsBlob, &errorBlob);

                if (vsBlob == null)
                {
                    if (errorBlob != null)
                    {
                        var pErrorBuffer = errorBlob->GetBufferPointer();
                        var errorMessage = Marshal.PtrToStringAnsi((IntPtr)pErrorBuffer);
                        Release(ref errorBlob);

                        throw new Exception($"VS Compilation failed: {errorMessage}");
                    }

                    throw new Exception("VS Compilation failed");
                }

                // Pixel Shader
                fixed (byte* pEntrypoint = "PSMain"u8)
                fixed (byte* pTarget = "ps_5_0"u8)
                    DirectX.D3DCompile(pCode, (nuint)shaderBytes.Length, null, null, null, (sbyte*)pEntrypoint, (sbyte*)pTarget, 0, 0, &psBlob, &errorBlob);

                if (psBlob == null)
                {
                    if (errorBlob != null)
                    {
                        var pErrorBuffer = errorBlob->GetBufferPointer();
                        var errorMessage = Marshal.PtrToStringAnsi((IntPtr)pErrorBuffer);
                        Release(ref errorBlob);

                        throw new Exception($"PS Compilation failed: {errorMessage}");
                    }

                    throw new Exception("PS Compilation failed");
                }
            }

            fixed (ID3D11VertexShader** pVertexShader = &_vertexShader)
                _device->CreateVertexShader(vsBlob->GetBufferPointer(), vsBlob->GetBufferSize(), null, pVertexShader);

            fixed (ID3D11PixelShader** pPixelShader = &_pixelShader)
                _device->CreatePixelShader(psBlob->GetBufferPointer(), psBlob->GetBufferSize(), null, pPixelShader);

            // Input Layout
            fixed (byte* pSemanticName = "POSITION"u8)
            {
                D3D11_INPUT_ELEMENT_DESC[] inputElements = [
                    new D3D11_INPUT_ELEMENT_DESC {
                        SemanticName = (sbyte*)pSemanticName,
                        SemanticIndex = 0,
                        Format = DXGI_FORMAT.DXGI_FORMAT_R32G32B32_FLOAT,
                        InputSlot = 0,
                        AlignedByteOffset = 0,
                        InputSlotClass = D3D11_INPUT_CLASSIFICATION.D3D11_INPUT_PER_VERTEX_DATA,
                        InstanceDataStepRate = 0
                    }
                ];

                fixed (D3D11_INPUT_ELEMENT_DESC* pElements = inputElements)
                fixed (ID3D11InputLayout** pInputLayout = &_inputLayout)
                    _device->CreateInputLayout(pElements, 1, vsBlob->GetBufferPointer(), vsBlob->GetBufferSize(), pInputLayout);

            }

            return true;
        }
        finally
        {
            Release(ref vsBlob);
            Release(ref psBlob);
            Release(ref errorBlob);
        }
    }

    private bool CreateRenderTarget(int width, int height)
    {
        if (_pass1RenderTargetTexture != null)
        {
            D3D11_TEXTURE2D_DESC currentDesc;
            _pass1RenderTargetTexture->GetDesc(&currentDesc);
            if (currentDesc.Width == (uint)width && currentDesc.Height == (uint)height)
                return true;
        }

        Release(ref _pass1ShaderResourceView);
        Release(ref _pass1RenderTargetView);
        Release(ref _pass1RenderTargetTexture);
        Release(ref _pass2ShaderResourceView);
        Release(ref _pass2RenderTargetView);
        Release(ref _pass2RenderTargetTexture);

        D3D11_TEXTURE2D_DESC desc = new()
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
            Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
            BindFlags = (uint)(D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET | D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE),
        };

        fixed (ID3D11Texture2D** pTex1 = &_pass1RenderTargetTexture)
        fixed (ID3D11Texture2D** pTex2 = &_pass2RenderTargetTexture)
        fixed (ID3D11RenderTargetView** pRTV1 = &_pass1RenderTargetView)
        fixed (ID3D11RenderTargetView** pRTV2 = &_pass2RenderTargetView)
        fixed (ID3D11ShaderResourceView** pSRV1 = &_pass1ShaderResourceView)
        fixed (ID3D11ShaderResourceView** pSRV2 = &_pass2ShaderResourceView)
        {
            _device->CreateTexture2D(&desc, null, pTex1);
            _device->CreateRenderTargetView((ID3D11Resource*)_pass1RenderTargetTexture, null, pRTV1);
            _device->CreateShaderResourceView((ID3D11Resource*)_pass1RenderTargetTexture, null, pSRV1);

            _device->CreateTexture2D(&desc, null, pTex2);
            _device->CreateRenderTargetView((ID3D11Resource*)_pass2RenderTargetTexture, null, pRTV2);
            _device->CreateShaderResourceView((ID3D11Resource*)_pass2RenderTargetTexture, null, pSRV2);
        }

        return true;
    }

    private bool CreateVertexBuffer()
    {
        if (_vertexBuffer != null)
            return true;

        var vertices = new Matrix4x3()
        {
            Row0 = new(0.0f, 0.0f, 0.0f), // Top-Left
            Row1 = new(1.0f, 0.0f, 0.0f), // Top-Right
            Row2 = new(0.0f, 1.0f, 0.0f), // Bottom-Left
            Row3 = new(1.0f, 1.0f, 0.0f), // Bottom-Right
        };

        D3D11_BUFFER_DESC desc = new()
        {
            ByteWidth = (uint)sizeof(Matrix4x3),
            Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
            BindFlags = (uint)D3D11_BIND_FLAG.D3D11_BIND_VERTEX_BUFFER
        };

        D3D11_SUBRESOURCE_DATA data = new()
        {
            pSysMem = &vertices
        };

        fixed (ID3D11Buffer** pVertexBuffer = &_vertexBuffer)
            _device->CreateBuffer(&desc, &data, pVertexBuffer);

        return true;
    }

    private bool CreateConstantBuffer()
    {
        if (_constantBuffer != null)
            return true;

        D3D11_BUFFER_DESC desc = new()
        {
            ByteWidth = (uint)((sizeof(ShaderParams) + 15) & ~15), // Align to 16 bytes
            Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
            BindFlags = (uint)D3D11_BIND_FLAG.D3D11_BIND_CONSTANT_BUFFER
        };

        fixed (ID3D11Buffer** pConstantBuffer = &_constantBuffer)
            _device->CreateBuffer(&desc, null, pConstantBuffer);

        return true;
    }
    private bool CreateSamplerState()
    {
        if (_samplerState != null) return true;

        D3D11_SAMPLER_DESC desc = new()
        {
            Filter = D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_LINEAR,
            AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_CLAMP,
            AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_CLAMP,
            AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_CLAMP,
            ComparisonFunc = D3D11_COMPARISON_FUNC.D3D11_COMPARISON_NEVER,
            MinLOD = 0,
            MaxLOD = float.MaxValue,
        };

        fixed (ID3D11SamplerState** pSampler = &_samplerState)
            _device->CreateSamplerState(&desc, pSampler);

        return _samplerState != null;
    }

    public override void Draw()
    {
        if (_isDisposing)
            return;

        var pos = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        if (size.X <= 0 || size.Y <= 0)
            return;

        if (!InitializeDevice())
            return;

        if (!CompileShaders())
            return;

        if (!CreateRenderTarget((int)size.X, (int)size.Y))
            return;

        if (!CreateVertexBuffer())
            return;

        if (!CreateConstantBuffer())
            return;

        if (!CreateSamplerState())
            return;

        var bbDesc = CopyBackBuffer();

        D3D11_VIEWPORT vp = new()
        {
            TopLeftX = 0,
            TopLeftY = 0,
            Width = size.X,
            Height = size.Y,
            MaxDepth = 1.0f
        };
        _context->RSSetViewports(1, &vp);

        var stride = (uint)sizeof(Vector3);
        var offset = 0u;
        var vbo = _vertexBuffer;
        _context->IASetInputLayout(_inputLayout);
        _context->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP);
        _context->IASetVertexBuffers(0, 1, &vbo, &stride, &offset);
        _context->VSSetShader(_vertexShader, null, 0);
        _context->PSSetShader(_pixelShader, null, 0);

        var cb = _constantBuffer;
        _context->VSSetConstantBuffers(0, 1, &cb);
        _context->PSSetConstantBuffers(0, 1, &cb);

        var sampler = _samplerState;
        _context->PSSetSamplers(0, 1, &sampler);

        Span<float> clearColor = stackalloc float[4];
        clearColor.Clear();
        var clearColorPtr = (float*)Unsafe.AsPointer(ref clearColor[0]);

        ShaderParams parms = new()
        {
            WindowPos = pos,
            WindowSize = size,
            Resolution = new Vector2(bbDesc.Width, bbDesc.Height),
            Direction = Vector2.Zero,
            BlurStrength = _blurStrength,
        };

        // Rendering Pass 1 (horizontal)
        parms.Direction = new Vector2(1, 0);
        _context->UpdateSubresource((ID3D11Resource*)_constantBuffer, 0, null, &parms, 0, 0);

        var rtv = _pass1RenderTargetView;
        _context->OMSetRenderTargets(1, &rtv, null);

        var srv = _backBufferCopySRV;
        _context->PSSetShaderResources(0, 1, &srv);

        _context->ClearRenderTargetView(_pass1RenderTargetView, clearColorPtr);
        _context->Draw(4, 0);

        // Rendering Pass 2 (vertical)
        parms.Direction = new Vector2(0, 1);
        _context->UpdateSubresource((ID3D11Resource*)_constantBuffer, 0, null, &parms, 0, 0);

        var rtv2 = _pass2RenderTargetView;
        _context->OMSetRenderTargets(1, &rtv2, null);

        var srvPass1 = _pass1ShaderResourceView;
        _context->PSSetShaderResources(0, 1, &srvPass1);

        _context->ClearRenderTargetView(_pass2RenderTargetView, clearColorPtr);
        _context->Draw(4, 0);

        var cursorPos = ImGui.GetCursorPos();

        ImGui.Image(
            new ImTextureID(_pass2ShaderResourceView),
            ImGui.GetWindowSize());

        ImGui.SetCursorPos(cursorPos);

        ImGui.Text($"WindowPos: {parms.WindowPos}");
        ImGui.Text($"WindowSize: {parms.WindowSize}");
        ImGui.Text($"Resolution: {parms.Resolution}");
        ImGui.SetNextItemWidth(120);
        ImGui.DragFloat("BlurStrength", ref _blurStrength, 1, 0, 10);
    }

    private D3D11_TEXTURE2D_DESC CopyBackBuffer()
    {
        Release(ref _backBufferCopySRV);
        Release(ref _backBufferCopyTexture);

        ID3D11Texture2D* backBuffer = null;
        Guid iidTex2d = __uuidof<ID3D11Texture2D>();
        _swapChain->GetBuffer(0, &iidTex2d, (void**)&backBuffer);

        D3D11_TEXTURE2D_DESC bbDesc;
        backBuffer->GetDesc(&bbDesc);
        bbDesc.BindFlags = (uint)(D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET);

        fixed (ID3D11Texture2D** pCopy = &_backBufferCopyTexture)
            _device->CreateTexture2D(&bbDesc, null, pCopy);

        _context->CopyResource((ID3D11Resource*)_backBufferCopyTexture, (ID3D11Resource*)backBuffer);

        fixed (ID3D11ShaderResourceView** pBackBufferCopySRV = &_backBufferCopySRV)
            _device->CreateShaderResourceView((ID3D11Resource*)_backBufferCopyTexture, null, pBackBufferCopySRV);

        Release(ref backBuffer);
        return bbDesc;
    }
}
