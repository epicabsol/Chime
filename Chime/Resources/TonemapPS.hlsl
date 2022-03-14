
struct PS_IN {
	float4 Position : SV_Position;
	float3 ViewPosition : TEXCOORD1;
	float2 TexCoord : TEXCOORD0;
};

Texture2D LightBuffer : register(t0);

SamplerState GBufferSampler : register(s0);

float4 PS_Main(PS_IN input) : SV_Target0 {
	float3 color = LightBuffer.SampleLevel(GBufferSampler, input.TexCoord, 0).xyz;

	// Tonemapping (reinhard)
	color = color / (color + float3(1.0, 1.0, 1.0));

	// Gamma correction
	float gamma = 2.2;
	color = pow(max(color, float3(0.0, 0.0, 0.0)), 1.0 / gamma);

	return float4(color, 1.0);
}