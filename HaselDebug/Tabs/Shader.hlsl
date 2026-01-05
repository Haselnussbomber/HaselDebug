cbuffer Params : register(b0)
{
    float2 WindowPos;
    float2 WindowSize;
    float2 Resolution;
    float2 Direction; // (1,0) for horizontal, (0,1) for vertical
    float BlurStrength;
};

Texture2D backBuffer : register(t0);
SamplerState samplerState : register(s0);

struct VS_OUT
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

VS_OUT VSMain(float3 pos : POSITION)
{
    VS_OUT output;

    // The quad position in Clip Space (-1 to 1) is ALWAYS the same 
    // because your Viewport in C# is set to the Window Size.
    // If your viewport is Window-sized, a 0..1 quad fills it perfectly.
    output.Position.x = pos.x * 2.0 - 1.0;
    output.Position.y = 1.0 - pos.y * 2.0;
    output.Position.z = 0.0;
    output.Position.w = 1.0;

    if (Direction.x == 1)
    {
        // Pass 1: We are sampling the Backbuffer (Screen Sized)
        // We need to offset based on where the window is on screen
        float2 pixelPos = WindowPos + (pos.xy * WindowSize);
        output.TexCoord = pixelPos / Resolution;
    }
    else
    {
        // Pass 2: We are sampling the Pass 1 texture (Window Sized)
        // Just use the local 0..1 coordinates
        output.TexCoord = pos.xy;
    }

    return output;
}

float4 PSMain(VS_OUT input) : SV_Target
{
    float weights[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };
    
    float2 currentSize = (Direction.x == 1) ? Resolution : WindowSize;
    float2 texelSize = 1.0 / currentSize;
    
    float3 color = backBuffer.Sample(samplerState, input.TexCoord).rgb * weights[0];
    
    for (int i = 1; i < 5; ++i)
    {
        // Now the offset is scaled correctly for whichever texture we are holding
        float2 offset = Direction * texelSize * (float) i * BlurStrength;
        color += backBuffer.Sample(samplerState, input.TexCoord + offset).rgb * weights[i];
        color += backBuffer.Sample(samplerState, input.TexCoord - offset).rgb * weights[i];
    }
    
    return float4(color, 1.0);
}
