using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;
using Vector4 = System.Numerics.Vector4;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ShaderTestTab : DebugTab, IDisposable
{
    private readonly PluginAssemblyProvider _pluginAssembly;

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct ShaderParams
    {
        public Vector4 TexelSize; // x: 1/w, y: 1/h, z: w, w: h
        public float BlurOffset;
        private float _pad1;
        private float _pad2;
        private float _pad3;
    }

    private ID3D11Device* _device;
    private ID3D11DeviceContext* _context;
    private IDXGISwapChain* _swapChain;

    private ID3D11VertexShader* _vs;
    private ID3D11PixelShader* _ps;

    private ID3D11VertexShader* _vsDown;
    private ID3D11PixelShader* _psDown;

    private ID3D11VertexShader* _vsUp;
    private ID3D11PixelShader* _psUp;

    private ID3D11Texture2D* _backBufferCopy;
    private ID3D11ShaderResourceView* _backBufferSRV;

    private ID3D11Texture2D* _rt1;
    private ID3D11RenderTargetView* _rtv1;
    private ID3D11ShaderResourceView* _srv1;

    private ID3D11Texture2D* _rt2;
    private ID3D11RenderTargetView* _rtv2;
    private ID3D11ShaderResourceView* _srv2;

    private ID3D11Buffer* _constantBuffer;
    private ID3D11SamplerState* _sampler;

    private float _blurStrength = 2.0f;
    private D3D11_TEXTURE2D_DESC _bbDesc;

    public override void Draw()
    {
        var pos = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        if (size.X <= 0 || size.Y <= 0)
            return;

        if (!InitDX())
            return;

        UpdateRenderTargets();
        CaptureBackBuffer();

        // 1. Setup Global State
        float[] clearColor = { 0, 0, 0, 0 };
        _context->ClearRenderTargetView(_rtv1, (float*)Unsafe.AsPointer(ref clearColor[0]));

        var parms = new ShaderParams
        {
            TexelSize = new Vector4(1f / size.X, 1f / size.Y, size.X, size.Y),
            BlurOffset = _blurStrength
        };
        _context->UpdateSubresource((ID3D11Resource*)_constantBuffer, 0, null, &parms, 0, 0);

        // 2. Configure Pipeline for Full-Screen Triangle
        D3D11_VIEWPORT vp = new()
        {
            Width = size.X,
            Height = size.Y,
            MaxDepth = 1.0f
        };
        _context->RSSetViewports(1, &vp);
        _context->IASetInputLayout(null); // No buffer needed due to SV_VertexID 
        _context->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

        var cb = _constantBuffer;
        _context->VSSetConstantBuffers(0, 1, &cb);
        _context->PSSetConstantBuffers(0, 1, &cb);

        var backBufferSRV = _backBufferSRV;
        _context->PSSetShaderResources(0, 1, &backBufferSRV);

        var samp = _sampler;
        _context->PSSetSamplers(0, 1, &samp);

        // --- PRE-PASS: Crop Background (Backbuffer -> RT1) ---
        _context->VSSetShader(_vs, null, 0);
        _context->PSSetShader(_ps, null, 0);

        var rtv1 = _rtv1;
        _context->OMSetRenderTargets(1, &rtv1, null);

        var bbSRV = _backBufferSRV;
        _context->PSSetShaderResources(0, 1, &bbSRV);

        _context->Draw(3, 0);

        // UNBIND to avoid conflicts
        ID3D11ShaderResourceView* nullSRV = null;
        _context->PSSetShaderResources(0, 1, &nullSRV);

        // --- PASS 1: Downsample (RT1 -> RT2) ---
        _context->VSSetShader(_vsDown, null, 0);
        _context->PSSetShader(_psDown, null, 0);

        var rtv2 = _rtv2;
        _context->OMSetRenderTargets(1, &rtv2, null);

        var srv1 = _srv1;
        _context->PSSetShaderResources(0, 1, &srv1);

        _context->Draw(3, 0);

        _context->PSSetShaderResources(0, 1, &nullSRV); // Unbind RT1

        // --- PASS 2: Upsample (RT2 -> RT1) ---
        _context->VSSetShader(_vsUp, null, 0);
        _context->PSSetShader(_psUp, null, 0);

        _context->OMSetRenderTargets(1, &rtv1, null); // Render to RT1

        var srv2 = _srv2;
        _context->PSSetShaderResources(0, 1, &srv2); // Sample from RT2

        _context->Draw(3, 0);

        _context->PSSetShaderResources(0, 1, &nullSRV); // Unbind RT1

        // 5. Output to ImGui
        var uv0 = new Vector2(pos.X / _bbDesc.Width, pos.Y / _bbDesc.Height);
        var uv1 = new Vector2((pos.X + size.X) / _bbDesc.Width, (pos.Y + size.Y) / _bbDesc.Height);
        ImGui.Image(new ImTextureID(_srv1), size, uv0, uv1);

        ImGui.DragFloat("Strength", ref _blurStrength, 0.1f, 0f, 10f);
    }

    private bool InitDX()
    {
        if (_device != null) return true;

        var kernel = Device.Instance();
        if (kernel == null) return false;

        _swapChain = (IDXGISwapChain*)kernel->SwapChain->DXGISwapChain;
        Guid iid = __uuidof<ID3D11Device>();
        fixed (ID3D11Device** pDev = &_device)
            _swapChain->GetDevice(&iid, (void**)pDev);

        fixed (ID3D11DeviceContext** pCtx = &_context)
            _device->GetImmediateContext(pCtx);

        // Create Sampler
        D3D11_SAMPLER_DESC sDesc = new()
        {
            Filter = D3D11_FILTER.D3D11_FILTER_MIN_MAG_MIP_LINEAR,
            AddressU = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_CLAMP,
            AddressV = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_CLAMP,
            AddressW = D3D11_TEXTURE_ADDRESS_MODE.D3D11_TEXTURE_ADDRESS_CLAMP
        };
        fixed (ID3D11SamplerState** pS = &_sampler)
            _device->CreateSamplerState(&sDesc, pS);

        // Create Constant Buffer 
        D3D11_BUFFER_DESC bDesc = new()
        {
            ByteWidth = (uint)sizeof(ShaderParams),
            Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
            BindFlags = (uint)D3D11_BIND_FLAG.D3D11_BIND_CONSTANT_BUFFER
        };
        fixed (ID3D11Buffer** pB = &_constantBuffer)
            _device->CreateBuffer(&bDesc, null, pB);

        return CompileShaders();
    }

    private void CaptureBackBuffer()
    {
        // 1. Get the actual backbuffer from the SwapChain
        ID3D11Texture2D* backBuffer = null;
        Guid iid = __uuidof<ID3D11Texture2D>();
        _swapChain->GetBuffer(0, &iid, (void**)&backBuffer);
        if (backBuffer == null)
            return;

        D3D11_TEXTURE2D_DESC bbDesc;
        backBuffer->GetDesc(&bbDesc);
        _bbDesc = bbDesc;

        // 2. Ensure our copy texture exists and matches the screen size
        if (_backBufferCopy != null)
        {
            D3D11_TEXTURE2D_DESC copyDesc;
            _backBufferCopy->GetDesc(&copyDesc);
            if (copyDesc.Width != bbDesc.Width || copyDesc.Height != bbDesc.Height)
            {
                Release(ref _backBufferCopy);
                Release(ref _backBufferSRV);
            }
        }

        if (_backBufferCopy == null)
        {
            // We need BindFlags for ShaderResource so the shader can read it
            bbDesc.BindFlags = (uint)D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE;
            bbDesc.Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT;
            bbDesc.CPUAccessFlags = 0;

            fixed (ID3D11Texture2D** p = &_backBufferCopy)
                _device->CreateTexture2D(&bbDesc, null, p);

            fixed (ID3D11ShaderResourceView** p = &_backBufferSRV)
                _device->CreateShaderResourceView((ID3D11Resource*)_backBufferCopy, null, p);
        }

        // 3. Copy the current screen frame into our texture
        _context->CopyResource((ID3D11Resource*)_backBufferCopy, (ID3D11Resource*)backBuffer);

        // Clean up the temporary pointer to the real backbuffer
        ((IUnknown*)backBuffer)->Release();
    }

    private bool CompileShaders()
    {
        // Load the HLSL source from your embedded resources
        using var stream = _pluginAssembly.Assembly.GetManifestResourceStream("HaselDebug.Tabs.Shader.hlsl");
        if (stream == null) return false;

        using var reader = new StreamReader(stream);
        var shaderCode = reader.ReadToEnd();
        var shaderBytes = Encoding.ASCII.GetBytes(shaderCode);

        // 1. Compile Vertex Shader (Used for both passes)
        ID3DBlob* vsBlob = null;
        if (CompileShader(shaderBytes, "Vert"u8, "vs_5_0"u8, &vsBlob))
        {
            fixed (ID3D11VertexShader** pVS = &_vs)
                _device->CreateVertexShader(vsBlob->GetBufferPointer(), vsBlob->GetBufferSize(), null, pVS);
            vsBlob->Release();
        }

        ID3DBlob* psBlob = null;
        if (CompileShader(shaderBytes, "Frag"u8, "ps_5_0"u8, &psBlob))
        {
            fixed (ID3D11PixelShader** pPS = &_ps)
                _device->CreatePixelShader(psBlob->GetBufferPointer(), psBlob->GetBufferSize(), null, pPS);
            psBlob->Release();
        }

        // 2. Compile Downsample Vertex and Pixel Shader
        ID3DBlob* vsDownBlob = null;
        if (CompileShader(shaderBytes, "Vert_DownSample"u8, "vs_5_0"u8, &vsDownBlob))
        {
            fixed (ID3D11VertexShader** pVS = &_vsDown)
                _device->CreateVertexShader(vsDownBlob->GetBufferPointer(), vsDownBlob->GetBufferSize(), null, pVS);
            vsDownBlob->Release();
        }

        ID3DBlob* psDownBlob = null;
        if (CompileShader(shaderBytes, "Frag_DownSample"u8, "ps_5_0"u8, &psDownBlob))
        {
            fixed (ID3D11PixelShader** pPS = &_psDown)
                _device->CreatePixelShader(psDownBlob->GetBufferPointer(), psDownBlob->GetBufferSize(), null, pPS);
            psDownBlob->Release();
        }

        // 3. Compile Upsample Vertex and Pixel Shader
        ID3DBlob* vsUpBlob = null;
        if (CompileShader(shaderBytes, "Vert_UpSample"u8, "vs_5_0"u8, &vsUpBlob))
        {
            fixed (ID3D11VertexShader** pVS = &_vsUp) // And this
                _device->CreateVertexShader(vsUpBlob->GetBufferPointer(), vsUpBlob->GetBufferSize(), null, pVS);
            vsUpBlob->Release();
        }

        ID3DBlob* psUpBlob = null;
        if (CompileShader(shaderBytes, "Frag_UpSample"u8, "ps_5_0"u8, &psUpBlob))
        {
            fixed (ID3D11PixelShader** pPS = &_psUp)
                _device->CreatePixelShader(psUpBlob->GetBufferPointer(), psUpBlob->GetBufferSize(), null, pPS);
            psUpBlob->Release();
        }

        return _vs != null && _psDown != null && _psUp != null;
    }

    private static bool CompileShader(byte[] shaderBytes, ReadOnlySpan<byte> entrypoint, ReadOnlySpan<byte> target, ID3DBlob** ppCode)
    {
        ID3DBlob* pErrorMsgs = null;
#if DEBUG
        uint flags = D3DCOMPILE.D3DCOMPILE_DEBUG;
#else
        uint flags = 0;
#endif

        fixed (byte* pSrcData = shaderBytes)
        fixed (byte* pEntrypoint = entrypoint)
        fixed (byte* pTarget = target)
        {
            var hr = DirectX.D3DCompile(
                pSrcData, (nuint)shaderBytes.Length,
                null, null, null,
                (sbyte*)pEntrypoint, (sbyte*)pTarget,
                flags, 0, ppCode, &pErrorMsgs
            );

            if (hr.FAILED)
            {
                if (pErrorMsgs != null)
                {
                    var msg = Marshal.PtrToStringAnsi((nint)pErrorMsgs->GetBufferPointer());
                    pErrorMsgs->Release();
                    throw new Exception($"Shader Error: {msg}");
                }
                return false;
            }
        }
        return true;
    }

    private void UpdateRenderTargets()
    {
        // 1. Get backbuffer description to get the TRUE game resolution
        ID3D11Texture2D* backBuffer = null;
        Guid iid = __uuidof<ID3D11Texture2D>();
        _swapChain->GetBuffer(0, &iid, (void**)&backBuffer);

        D3D11_TEXTURE2D_DESC bbDesc;
        backBuffer->GetDesc(&bbDesc);
        ((IUnknown*)backBuffer)->Release();

        // 2. Only recreate if the game resolution changed
        if (_rt1 != null)
        {
            D3D11_TEXTURE2D_DESC desc;
            _rt1->GetDesc(&desc);
            if (desc.Width == bbDesc.Width && desc.Height == bbDesc.Height)
                return;
        }

        ReleaseResources();

        D3D11_TEXTURE2D_DESC td = new()
        {
            Width = bbDesc.Width,
            Height = bbDesc.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1 },
            Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
            BindFlags = (uint)(D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET | D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE)
        };

        fixed (ID3D11Texture2D** pT = &_rt1)
            _device->CreateTexture2D(&td, null, pT);
        fixed (ID3D11RenderTargetView** pR = &_rtv1)
            _device->CreateRenderTargetView((ID3D11Resource*)_rt1, null, pR);
        fixed (ID3D11ShaderResourceView** pS = &_srv1)
            _device->CreateShaderResourceView((ID3D11Resource*)_rt1, null, pS);

        fixed (ID3D11Texture2D** pT = &_rt2)
            _device->CreateTexture2D(&td, null, pT);
        fixed (ID3D11RenderTargetView** pR = &_rtv2)
            _device->CreateRenderTargetView((ID3D11Resource*)_rt2, null, pR);
        fixed (ID3D11ShaderResourceView** pS = &_srv2)
            _device->CreateShaderResourceView((ID3D11Resource*)_rt2, null, pS);
    }

    private void ReleaseResources()
    {
        Release(ref _rt1);
        Release(ref _rtv1);
        Release(ref _srv1);

        Release(ref _rt2);
        Release(ref _rtv2);
        Release(ref _srv2);
    }

    private static void Release<T>(ref T* obj) where T : unmanaged
    {
        if (obj != null)
        {
            ((IUnknown*)obj)->Release();
            obj = null;
        }
    }

    public void Dispose()
    {
        ReleaseResources();

        Release(ref _psDown);
        Release(ref _vsDown);

        Release(ref _psUp);
        Release(ref _vsUp);

        Release(ref _ps);
        Release(ref _vs);

        Release(ref _backBufferCopy);
        Release(ref _backBufferSRV);

        Release(ref _constantBuffer);
        Release(ref _sampler);

        Release(ref _context);
        Release(ref _device);
    }
}
