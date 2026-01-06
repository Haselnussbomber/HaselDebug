/*
MIT License

Copyright (c) 2023 Baedrick

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

// Original: https://github.com/Baedrick/Dual-Kawase-Blur-Demo/blob/main/Assets/Plugins/DualKawaseBlur/Shaders/DualKawaseBlur.hlsl

Texture2D _SourceTex;
SamplerState sampler_linear_clamp;

cbuffer Params : register(b0)
{
    float4 _SourceTex_TexelSize;
    float2 _CropPos;
    float2 _CropSize;
    float _BlurOffset;
    float3 _Padding;
};

struct V2F
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

struct V2F_DownSample
{
    float4 positionCS : SV_POSITION;
    float2 uv0 : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    float4 uv2 : TEXCOORD2;
};

struct V2F_UpSample
{
    float4 positionCS : SV_POSITION;
    float2 uv0 : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    float4 uv2 : TEXCOORD2;
    float4 uv3 : TEXCOORD3;
    float4 uv4 : TEXCOORD4;
};

V2F Vert(const uint vertexID : SV_VertexID)
{
    V2F output;
    
    output.positionCS = float4(
        vertexID <= 1 ? -1.0 : 3.0,
        vertexID == 1 ? 3.0 : -1.0,
        0.0, 1.0
    );
    /*
    output.uv = float2(
        vertexID <= 1 ? 0.0 : 2.0,
        vertexID == 1 ? 2.0 : 0.0
    );
    */
    
    // edit: ffxiv back buffer is flipped vertically
    output.uv = float2(
        vertexID <= 1 ? 0.0 : 2.0,
        vertexID == 1 ? -1.0 : 1.0
    );
    
    return output;
}

float4 Frag(const V2F input) : SV_TARGET
{
    // edit: ffxiv back buffer uses alpha to store depth
    return float4(_SourceTex.SampleLevel(sampler_linear_clamp, input.uv, 0).rgb, 1.0);
}

V2F_DownSample Vert_DownSample(const uint vertexID : SV_VertexID)
{
    V2F_DownSample output;
    
    output.positionCS = float4(
        vertexID <= 1 ? -1.0 : 3.0,
        vertexID == 1 ? 3.0 : -1.0,
        0.0, 1.0
    );
    output.uv0 = float2(
        vertexID <= 1 ? 0.0 : 2.0,
        vertexID == 1 ? 2.0 : 0.0
    );

    const float2 uv = output.uv0;
    const float2 halfPixel = _SourceTex_TexelSize.xy * 0.5;
    const float2 offset = float2(1.0 + _BlurOffset, 1.0 + _BlurOffset);
    
    output.uv1.xy = uv - halfPixel * offset;
    output.uv1.zw = uv + halfPixel * offset;
    
    output.uv2.xy = uv - float2(halfPixel.x, -halfPixel.y) * offset;
    output.uv2.zw = uv + float2(halfPixel.x, -halfPixel.y) * offset;
    return output;
}

float4 Frag_DownSample(const V2F_DownSample input) : SV_TARGET
{
    float4 sum = _SourceTex.SampleLevel(sampler_linear_clamp, input.uv0, 0) * 4.0;
    sum += _SourceTex.SampleLevel(sampler_linear_clamp, input.uv1.xy, 0);
    sum += _SourceTex.SampleLevel(sampler_linear_clamp, input.uv1.zw, 0);
    sum += _SourceTex.SampleLevel(sampler_linear_clamp, input.uv2.xy, 0);
    sum += _SourceTex.SampleLevel(sampler_linear_clamp, input.uv2.zw, 0);
    return sum * 0.125;
}

V2F_UpSample Vert_UpSample(const uint vertexID : SV_VertexID)
{
    V2F_UpSample output;
    
    output.positionCS = float4(
        vertexID <= 1 ? -1.0 : 3.0,
        vertexID == 1 ? 3.0 : -1.0,
        0.0, 1.0
    );
    output.uv0 = float2(
        vertexID <= 1 ? 0.0 : 2.0,
        vertexID == 1 ? 2.0 : 0.0
    );

    const float2 uv = output.uv0;
    const float2 halfPixel = _SourceTex_TexelSize.xy * 0.5;
    const float2 offset = float2(1.0 + _BlurOffset, 1.0 + _BlurOffset);
    
    output.uv1.xy = uv + float2(-halfPixel.x * 2.0, 0.0) * offset;
    output.uv1.zw = uv + float2(-halfPixel.x, halfPixel.y) * offset;
    
    output.uv2.xy = uv + float2(0.0, halfPixel.y * 2.0) * offset;
    output.uv2.zw = uv + halfPixel * offset;

    output.uv3.xy = uv + float2(halfPixel.x * 2.0, 0.0) * offset;
    output.uv3.zw = uv + float2(halfPixel.x, -halfPixel.y) * offset;
    
    output.uv4.xy = uv + float2(0.0, -halfPixel.y * 2.0) * offset;
    output.uv4.zw = uv - halfPixel * offset;
    
    return output;
}

float4 Frag_UpSample(const V2F_UpSample input) : SV_TARGET
{
    float4 sum = _SourceTex.SampleLevel(sampler_linear_clamp, input.uv1.xy, 0);
    sum += _SourceTex.SampleLevel(sampler_linear_clamp, input.uv1.zw, 0) * 2.0;
    sum += _SourceTex.SampleLevel(sampler_linear_clamp, input.uv2.xy, 0);
    sum += _SourceTex.SampleLevel(sampler_linear_clamp, input.uv2.zw, 0) * 2.0;
    sum += _SourceTex.SampleLevel(sampler_linear_clamp, input.uv3.xy, 0);
    sum += _SourceTex.SampleLevel(sampler_linear_clamp, input.uv3.zw, 0) * 2.0;
    sum += _SourceTex.SampleLevel(sampler_linear_clamp, input.uv4.xy, 0);
    sum += _SourceTex.SampleLevel(sampler_linear_clamp, input.uv4.zw, 0) * 2.0;
    return sum * 0.0833;
}
