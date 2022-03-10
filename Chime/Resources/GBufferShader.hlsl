
struct VS_IN {
	float3 Position : POSITION0;
	float3 Normal : NORMAL0;
	float2 TexCoord : TEXCOORD0;
};

struct PS_IN {
	float4 Position : SV_Position;
	float3 Normal : TEXCOORD1;
	float2 TexCoord : TEXCOORD0;
};

cbuffer SceneConstants : register(b0) {
	float4x4 ModelMatrix;
	float4x4 ViewMatrix;
	float4x4 ProjectionMatrix;
};

PS_IN VS_Main(VS_IN input) {
	PS_IN output = (PS_IN)0;

	output.Position = mul(float4(input.Position, 1.0), ModelMatrix);
	output.Position = mul(output.Position, ViewMatrix);
	output.Position = mul(output.Position, ProjectionMatrix);

	output.Normal = mul(float4(input.Normal, 0.0), ModelMatrix).xyz;
	output.TexCoord = input.TexCoord;

	return output;
}

float4 PS_Main(PS_IN input) : SV_Target0 {
	float diffuse = saturate(dot(input.Normal, float3(0.0, 1.0, 0.0)));
	return float4(diffuse, diffuse, diffuse, 1.0);
}