
struct VS_IN {
	float3 Position : POSITION0;
	float4 Color : COLOR0;
};

struct PS_IN {
	float4 Position : SV_Position;
	float4 Color : COLOR0;
};

cbuffer SceneConstants : register(b0) {
	float4x4 ModelMatrix;
	float4x4 ViewMatrix;
	float4x4 ProjectionMatrix;
	float4x4 InverseProjectionMatrix;
};

PS_IN VS_Main(VS_IN input) {
	PS_IN output = (PS_IN)0;

	output.Position = mul(float4(input.Position, 1.0), ModelMatrix);
	output.Position = mul(output.Position, ViewMatrix);
	output.Position = mul(output.Position, ProjectionMatrix);

	output.Color = input.Color;

	return output;
}

float4 PS_Main(PS_IN input) : SV_Target0 {
	return input.Color;
}