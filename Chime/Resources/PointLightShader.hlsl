
struct PS_IN {
	float4 Position : SV_Position;
	float3 ViewPosition : TEXCOORD1;
	float2 TexCoord : TEXCOORD0;
};

cbuffer SceneConstants : register(b0) {
	float4x4 ModelMatrix;
	float4x4 ViewMatrix;
	float4x4 ProjectionMatrix;
	float4x4 InverseProjectionMatrix;
};

cbuffer PointLightConstants : register(b1) {
	float3 LightColor;
	float ZNear;
	float3 LightPosition; // View-space
	float ZFar;
};

Texture2D DepthStencilBuffer : register(t0);
Texture2D DiffuseRoughnessBuffer : register(t1);
Texture2D NormalMetallicBuffer : register(t2);

SamplerState GBufferSampler : register(s0);

#define PI (3.14159265359)

float LinearizeDepth(float hardwareDepth) {
	// https://stackoverflow.com/a/51137756
	return ZNear * ZFar / (ZFar + hardwareDepth * (ZNear - ZFar));
}

// PBR shading based on https://learnopengl.com/PBR/Lighting
float3 FresnelSchlick(float cosTheta, float3 F0) {
	return F0 + (float3(1.0, 1.0, 1.0) - F0) * pow(saturate(1.0 - cosTheta), 5.0);
}

float DistributionGGX(float3 N, float3 H, float roughness) {
	float a = roughness * roughness;
	float a2 = a * a;
	float NdotH = max(dot(N, H), 0.0);
	float NdotH2 = NdotH * NdotH;

	float num = a2;
	float denom = (NdotH2 * (a2 - 1.0) + 1.0);
	denom = PI * denom * denom;

	return num / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness) {
	float r = roughness + 1.0;
	float k = (r * r) / 8.0;

	float num = NdotV;
	float denom = NdotV * (1.0 - k) + k;

	return num / denom;
}

float GeometrySmith(float3 N, float3 V, float3 L, float roughness) {
	float NdotV = max(dot(N, V), 0.0);
	float NdotL = max(dot(N, L), 0.0);
	float ggx2 = GeometrySchlickGGX(NdotV, roughness);
	float ggx1 = GeometrySchlickGGX(NdotL, roughness);
	
	return ggx1 * ggx2;
}

float4 PS_Main(PS_IN input) : SV_Target0 {
	float hardwareDepth = DepthStencilBuffer.SampleLevel(GBufferSampler, input.TexCoord, 0).x;
	float depth = LinearizeDepth(hardwareDepth);
	float3 position = input.ViewPosition * (depth / ZFar);
	float4 diffuseRoughness = DiffuseRoughnessBuffer.SampleLevel(GBufferSampler, input.TexCoord, 0);
	float4 normalMetallic = NormalMetallicBuffer.SampleLevel(GBufferSampler, input.TexCoord, 0);

	float3 N = normalize(normalMetallic.xyz); // Surface normal in view space
	float3 V = float3(0.0, 0.0, 1.0);
	float3 L = normalize(LightPosition - position);
	float3 H = normalize(V + L);

	float NdotL = max(dot(N, L), 0.0);
	float NdotV = max(dot(N, V), 0.0);

	float distance = length(LightPosition - position);
	float attenuation = 1.0 / (distance * distance);
	float3 radiance = LightColor * attenuation;

	float3 F0 = float3(0.04, 0.04, 0.04);
	F0 = lerp(F0, diffuseRoughness.xyz, normalMetallic.w);
	float3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);

	float NDF = DistributionGGX(N, H, diffuseRoughness.w);
	float G = GeometrySmith(N, V, L, diffuseRoughness.w);

	float3 numerator = NDF * G * F;
	float denominator = 4.0 * NdotV * NdotL + 0.0001;
	float3 specular = numerator / denominator;

	float3 kS = F;
	float3 kD = float3(1.0, 1.0, 1.0) - kS;

	kD *= 1.0 - normalMetallic.w;

	float3 totalLight = (kD * diffuseRoughness.xyz / PI + specular) * radiance * NdotL;

	return float4(totalLight, 1.0);
}