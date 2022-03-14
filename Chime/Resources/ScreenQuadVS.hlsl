
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

PS_IN VS_Main(uint vertexID : SV_VertexID) {
	PS_IN output = (PS_IN)0;

	output.TexCoord = float2(uint2(vertexID << 1, vertexID) & 2);
	output.Position = float4(output.TexCoord * 2.0 - 1.0, 1.0, 1.0);
	output.TexCoord.y = 1.0 - output.TexCoord.y;
	float4 viewSpace = mul(output.Position, InverseProjectionMatrix);
	output.ViewPosition = viewSpace.xyz / viewSpace.w;

	return output;
}
