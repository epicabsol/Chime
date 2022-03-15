
struct VS_IN {
	float3 Position : POSITION0;
	float3 Normal : NORMAL0;
	float2 TexCoord : TEXCOORD0;
	float4 Tangent : TANGENT0;
};

struct PS_IN {
	float4 Position : SV_Position;
	float3 Normal : TEXCOORD1;
	float2 TexCoord : TEXCOORD0;
	float3 Tangent : TANGENT0;
	float3 Bitangent : TANGENT1;
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

Texture2D DiffuseTexture : register(t0);
Texture2D NormalTexture : register(t1);
Texture2D MetallicRoughnessTexture : register(t2);

SamplerState TextureSampler : register(s0);

PS_IN VS_Main(VS_IN input) {
	PS_IN output = (PS_IN)0;

	output.Position = mul(float4(input.Position, 1.0), ModelMatrix);
	output.Position = mul(output.Position, ViewMatrix);
	output.Position = mul(output.Position, ProjectionMatrix);

	output.Normal = mul(float4(input.Normal, 0.0), ModelMatrix).xyz; // Transform normal from model space to world space
	output.Normal = mul(float4(output.Normal, 0.0), ViewMatrix).xyz; // Transform normal from world space to view space
	output.TexCoord = input.TexCoord;

	output.Tangent = mul(float4(input.Tangent.xyz * input.Tangent.w, 0.0), ModelMatrix).xyz; // Transform tangent from model space to world space
	output.Tangent = mul(float4(output.Tangent, 0.0), ViewMatrix).xyz; // Transform tangent from world space to view space
	output.Bitangent = cross(output.Normal, output.Tangent);

	return output;
}

GBufferPixel PS_Main(PS_IN input) {
	GBufferPixel output = (GBufferPixel)0;

	/*const float checkerScale = 64.0;
	bool checker = fmod(floor(input.TexCoord.x * checkerScale) + floor(input.TexCoord.y * checkerScale), 2.0) < 1.0;
	float lambertian = checker ? 0.9 : 0.2;
	float3 diffuse = float3(lambertian, lambertian, lambertian);*/
	float3 diffuse = DiffuseTexture.Sample(TextureSampler, input.TexCoord).xyz;
	// Gamma correction
	float gamma = 2.2;
	diffuse = pow(max(diffuse, float3(0.0, 0.0, 0.0)), gamma);
	float3 tangentSpaceNormal = NormalTexture.Sample(TextureSampler, input.TexCoord).xyz * 2.0 - 1.0;
	float3x3 TBN = float3x3(normalize(input.Tangent), normalize(input.Bitangent), normalize(input.Normal));
	float3 normal = mul(tangentSpaceNormal, TBN);
	float4 metallicRoughness = MetallicRoughnessTexture.Sample(TextureSampler, input.TexCoord);
	float metallic = metallicRoughness.x;
	float roughness = metallicRoughness.y;

	output.DiffuseRoughness = float4(diffuse, roughness);
	output.NormalMetallic = float4(normal, metallic);
	return output;
}