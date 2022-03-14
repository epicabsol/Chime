
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

struct GBufferPixel {
	float4 DiffuseRoughness : SV_Target0;
	float4 NormalMetallic : SV_Target1;
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

	output.Normal = mul(float4(input.Normal, 0.0), ModelMatrix).xyz; // Transform normal from model space to world space
	output.Normal = mul(float4(output.Normal, 0.0), ViewMatrix).xyz; // Transform normal from world space to view space
	output.TexCoord = input.TexCoord;

	return output;
}

GBufferPixel PS_Main(PS_IN input) {
	GBufferPixel output = (GBufferPixel)0;

	const float checkerScale = 64.0;
	bool checker = fmod(floor(input.TexCoord.x * checkerScale) + floor(input.TexCoord.y * checkerScale), 2.0) < 1.0;
	float lambertian = checker ? 0.9 : 0.2;
	float3 diffuse = float3(0.9f, 0.1f, 0.1f); // float3(lambertian, lambertian, lambertian);
	float roughness = 0.3;
	float3 normal = normalize(input.Normal);
	float metallic = 0.0;

	output.DiffuseRoughness = float4(diffuse, roughness);
	output.NormalMetallic = float4(normal, metallic);
	return output;
}