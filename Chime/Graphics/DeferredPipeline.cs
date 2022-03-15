using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using SharpDX.Direct3D11;

namespace Chime.Graphics
{
    public class DeferredPipeline : IDisposable
    {
        private struct SceneConstants
        {
            public Matrix4x4 ModelMatrix;
            public Matrix4x4 ViewMatrix;
            public Matrix4x4 ProjectionMatrix;
            public Matrix4x4 InverseProjectionMatrix;
        }

        private struct PointLightConstants
        {
            public Vector3 LightColor;
            public float ZNear;
            public Vector3 LightPosition;
            public float ZFar;
        }

        public int Width { get; }
        public int Height { get; }
        public Texture2D Backbuffer { get; }
        public RenderTargetView BackbufferRTV { get; }
        public Texture2D DepthStencilBuffer { get; }
        public ShaderResourceView DepthStencilBufferSRV { get; }
        public DepthStencilView DepthStencilBufferDSV { get; }
        public Texture2D DiffuseRoughnessBuffer { get; }
        public ShaderResourceView DiffuseRoughnessBufferSRV { get; }
        public RenderTargetView DiffuseRoughnessBufferRTV { get; }
        public Texture2D NormalMetallicBuffer { get; }
        public ShaderResourceView NormalMetallicBufferSRV { get; }
        public RenderTargetView NormalMetallicBufferRTV { get; }
        public Texture2D LightBuffer { get; }
        public ShaderResourceView LightBufferSRV { get; }
        public RenderTargetView LightBufferRTV { get; }

        public DepthStencilState StandardDepthStencilState { get; }
        public RasterizerState StandardRasterizerState { get; }
        public BlendState AdditiveBlendState { get; }
        public SamplerState DefaultSamplerState { get; }

        private SharpDX.Direct3D11.Buffer SceneConstantBuffer { get; set; }
        private SceneConstants SceneConstantValues;
        private SharpDX.Direct3D11.Buffer PointLightConstantBuffer { get; }
        private PointLightConstants PointLightConstantValues;

        private InputLayout SolidColorInputLayout { get; set; }
        private VertexShader SolidColorVertexShader { get; set; }
        private PixelShader SolidColorPixelShader { get; set; }

        private InputLayout GBufferInputLayout { get; set; }
        private VertexShader GBufferVertexShader { get; set; }
        private PixelShader GBufferPixelShader { get; set; }

        private VertexShader ScreenQuadVertexShader { get; }

        public PixelShader PointLightPixelShader { get; }
        public PixelShader TonemapPixelShader { get; }

        public DeferredPipeline(Texture2D backbuffer, int width, int height)
        {
            if (Program.Renderer == null)
                throw new NullReferenceException();

            this.Width = width;
            this.Height = height;
            this.Backbuffer = backbuffer;
            this.BackbufferRTV = new RenderTargetView(Program.Renderer.Device, this.Backbuffer);

            this.DepthStencilBuffer = new Texture2D(Program.Renderer.Device, new Texture2DDescription() { ArraySize = 1, BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource, CpuAccessFlags = CpuAccessFlags.None, Format = SharpDX.DXGI.Format.R32_Typeless, Width = this.Width, Height = this.Height, MipLevels = 1, OptionFlags = ResourceOptionFlags.None, SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0), Usage = ResourceUsage.Default });
            this.DepthStencilBufferSRV = new ShaderResourceView(Program.Renderer.Device, this.DepthStencilBuffer, new ShaderResourceViewDescription() { Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D, Format = SharpDX.DXGI.Format.R32_Float, Texture2D = new ShaderResourceViewDescription.Texture2DResource() { MipLevels = -1, MostDetailedMip = 0 } });
            this.DepthStencilBufferDSV = new DepthStencilView(Program.Renderer.Device, this.DepthStencilBuffer, new DepthStencilViewDescription() { Dimension = DepthStencilViewDimension.Texture2D, Flags = DepthStencilViewFlags.None, Format = SharpDX.DXGI.Format.D32_Float, Texture2D = new DepthStencilViewDescription.Texture2DResource() { MipSlice = 0 } });
            this.DiffuseRoughnessBuffer = new Texture2D(Program.Renderer.Device, new Texture2DDescription() { ArraySize = 1, BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource, CpuAccessFlags = CpuAccessFlags.None, Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm, Width = this.Width, Height = this.Height, MipLevels = 1, OptionFlags = ResourceOptionFlags.None, SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0), Usage = ResourceUsage.Default });
            this.DiffuseRoughnessBufferSRV = new ShaderResourceView(Program.Renderer.Device, this.DiffuseRoughnessBuffer);
            this.DiffuseRoughnessBufferRTV = new RenderTargetView(Program.Renderer.Device, this.DiffuseRoughnessBuffer);
            this.NormalMetallicBuffer = new Texture2D(Program.Renderer.Device, new Texture2DDescription() { ArraySize = 1, BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource, CpuAccessFlags = CpuAccessFlags.None, Format = SharpDX.DXGI.Format.R16G16B16A16_SNorm, Width = this.Width, Height = this.Height, MipLevels = 1, OptionFlags = ResourceOptionFlags.None, SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0), Usage = ResourceUsage.Default });
            this.NormalMetallicBufferSRV = new ShaderResourceView(Program.Renderer.Device, this.NormalMetallicBuffer);
            this.NormalMetallicBufferRTV = new RenderTargetView(Program.Renderer.Device, this.NormalMetallicBuffer);
            this.LightBuffer = new Texture2D(Program.Renderer.Device, new Texture2DDescription() { ArraySize = 1, BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource, CpuAccessFlags = CpuAccessFlags.None, Format = SharpDX.DXGI.Format.R32G32B32A32_Float, Width = this.Width, Height = this.Height, MipLevels = 1, OptionFlags = ResourceOptionFlags.None, SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0), Usage = ResourceUsage.Default });
            this.LightBufferSRV = new ShaderResourceView(Program.Renderer.Device, this.LightBuffer);
            this.LightBufferRTV = new RenderTargetView(Program.Renderer.Device, this.LightBuffer);

            this.StandardRasterizerState = new RasterizerState(Program.Renderer.Device, new RasterizerStateDescription() { CullMode = CullMode.Back, DepthBias = 0, DepthBiasClamp = 0.0f, FillMode = FillMode.Solid, IsAntialiasedLineEnabled = false, IsDepthClipEnabled = false, IsFrontCounterClockwise = true, IsMultisampleEnabled = false, IsScissorEnabled = false, SlopeScaledDepthBias = 0.0f });
            this.StandardDepthStencilState = new DepthStencilState(Program.Renderer.Device, new DepthStencilStateDescription() { DepthComparison = Comparison.Less, DepthWriteMask = DepthWriteMask.All, IsDepthEnabled = true, IsStencilEnabled = false, FrontFace = new DepthStencilOperationDescription() { Comparison = Comparison.Always, DepthFailOperation = StencilOperation.Keep, PassOperation = StencilOperation.Keep, FailOperation = StencilOperation.Keep } });
            BlendStateDescription additiveBSDesc = new BlendStateDescription();
            additiveBSDesc.RenderTarget[0] = new RenderTargetBlendDescription(true, BlendOption.One, BlendOption.One, BlendOperation.Add, BlendOption.One, BlendOption.One, BlendOperation.Add, ColorWriteMaskFlags.All);
            this.AdditiveBlendState = new BlendState(Program.Renderer.Device, additiveBSDesc);
            this.DefaultSamplerState = new SamplerState(Program.Renderer.Device, new SamplerStateDescription() { AddressU = TextureAddressMode.Wrap, AddressV = TextureAddressMode.Wrap, AddressW = TextureAddressMode.Wrap, BorderColor = new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 0), ComparisonFunction = Comparison.Always, Filter = Filter.MinMagMipLinear });
            Program.Renderer.ImmediateContext.PixelShader.SetSampler(0, this.DefaultSamplerState);

            this.SceneConstantBuffer = new SharpDX.Direct3D11.Buffer(Program.Renderer.Device, new BufferDescription(System.Runtime.InteropServices.Marshal.SizeOf<SceneConstants>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0));
            this.PointLightConstantBuffer = new SharpDX.Direct3D11.Buffer(Program.Renderer.Device, new BufferDescription(System.Runtime.InteropServices.Marshal.SizeOf<PointLightConstants>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0));

            this.SolidColorInputLayout = new InputLayout(Program.Renderer.Device, Program.Renderer.Shaders.SolidColorShaderVS, new InputElement[] { new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0), new InputElement("COLOR", 0, SharpDX.DXGI.Format.R32G32B32A32_Float, 0) });
            this.SolidColorVertexShader = new VertexShader(Program.Renderer.Device, Program.Renderer.Shaders.SolidColorShaderVS);
            this.SolidColorPixelShader = new PixelShader(Program.Renderer.Device, Program.Renderer.Shaders.SolidColorShaderPS);

            this.GBufferInputLayout = new InputLayout(Program.Renderer.Device, Program.Renderer.Shaders.GBufferShaderVS, new InputElement[] { new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0), new InputElement("NORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0), new InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float, 0), new InputElement("TANGENT", 0, SharpDX.DXGI.Format.R32G32B32A32_Float, 0) });
            this.GBufferVertexShader = new VertexShader(Program.Renderer.Device, Program.Renderer.Shaders.GBufferShaderVS);
            this.GBufferPixelShader = new PixelShader(Program.Renderer.Device, Program.Renderer.Shaders.GBufferShaderPS);

            this.ScreenQuadVertexShader = new VertexShader(Program.Renderer.Device, Program.Renderer.Shaders.ScreenQuadVS);

            this.PointLightPixelShader = new PixelShader(Program.Renderer.Device, Program.Renderer.Shaders.PointLightPS);
            this.TonemapPixelShader = new PixelShader(Program.Renderer.Device, Program.Renderer.Shaders.TonemapPS);
        }

        #region Pipeline Stages

        public void BeginGBuffer(Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, float zNear, float zFar)
        {
            if (Program.Renderer == null)
                return;

            this.SceneConstantValues.ViewMatrix = Matrix4x4.Transpose(viewMatrix);
            this.SceneConstantValues.ProjectionMatrix = Matrix4x4.Transpose(projectionMatrix);
            bool inverted = Matrix4x4.Invert(projectionMatrix, out Matrix4x4 inverseProjectionMatrix);
            if (!inverted)
                throw new Exception("Projection matrix was not invertable!");
            this.SceneConstantValues.InverseProjectionMatrix = Matrix4x4.Transpose(inverseProjectionMatrix);
            this.PointLightConstantValues.ZNear = zNear;
            this.PointLightConstantValues.ZFar = zFar;
            Program.Renderer.ImmediateContext.Rasterizer.SetViewport(new SharpDX.Mathematics.Interop.RawViewportF() { Width = this.Width, Height = this.Height, X = 0, Y = 0, MinDepth = 0, MaxDepth = 1 });
            Program.Renderer.ImmediateContext.ClearRenderTargetView(this.DiffuseRoughnessBufferRTV, new SharpDX.Mathematics.Interop.RawColor4(0.0f, 0.0f, 0.0f, 1.0f));
            Program.Renderer.ImmediateContext.ClearRenderTargetView(this.NormalMetallicBufferRTV, new SharpDX.Mathematics.Interop.RawColor4(0.0f, 0.0f, 0.0f, 1.0f));
            Program.Renderer.ImmediateContext.ClearDepthStencilView(this.DepthStencilBufferDSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            Program.Renderer.ImmediateContext.InputAssembler.InputLayout = this.GBufferInputLayout;
            Program.Renderer.ImmediateContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            Program.Renderer.ImmediateContext.Rasterizer.State = this.StandardRasterizerState;
            Program.Renderer.ImmediateContext.OutputMerger.DepthStencilState = this.StandardDepthStencilState;
            Program.Renderer.ImmediateContext.VertexShader.Set(this.GBufferVertexShader);
            Program.Renderer.ImmediateContext.VertexShader.SetConstantBuffer(0, this.SceneConstantBuffer);
            Program.Renderer.ImmediateContext.PixelShader.Set(this.GBufferPixelShader);

            Program.Renderer.ImmediateContext.PixelShader.SetShaderResources(0, null, null, null);
            Program.Renderer.ImmediateContext.OutputMerger.SetRenderTargets(this.DepthStencilBufferDSV, this.DiffuseRoughnessBufferRTV, this.NormalMetallicBufferRTV);
        }

        public void BeginLighting()
        {
            Program.Renderer.ImmediateContext.OutputMerger.DepthStencilState = null; // this.StandardDepthStencilState;

            Program.Renderer.ImmediateContext.ClearRenderTargetView(this.LightBufferRTV, new SharpDX.Mathematics.Interop.RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            Program.Renderer.ImmediateContext.InputAssembler.InputLayout = null;
            Program.Renderer.ImmediateContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            Program.Renderer.ImmediateContext.OutputMerger.DepthStencilState = null;
            Program.Renderer.ImmediateContext.VertexShader.Set(this.ScreenQuadVertexShader);
            Program.Renderer.ImmediateContext.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
            Program.Renderer.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(null, 0, 0));

            Program.Renderer.ImmediateContext.OutputMerger.SetRenderTargets(this.LightBufferRTV);
            Program.Renderer.ImmediateContext.PixelShader.SetShaderResources(0, this.DepthStencilBufferSRV, this.DiffuseRoughnessBufferSRV, this.NormalMetallicBufferSRV);
            Program.Renderer.ImmediateContext.PixelShader.SetConstantBuffer(0, this.SceneConstantBuffer);
            Program.Renderer.ImmediateContext.OutputMerger.BlendState = this.AdditiveBlendState;
        }

        public void BeginEffects()
        {
            Program.Renderer.ImmediateContext.OutputMerger.BlendState = null;
        }

        public void PostProcess()
        {
            Program.Renderer.ImmediateContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            Program.Renderer.ImmediateContext.OutputMerger.DepthStencilState = null;
            Program.Renderer.ImmediateContext.VertexShader.Set(this.ScreenQuadVertexShader);
            Program.Renderer.ImmediateContext.InputAssembler.SetIndexBuffer(null, SharpDX.DXGI.Format.Unknown, 0);
            Program.Renderer.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(null, 0, 0));

            // Tonemapping
            Program.Renderer.ImmediateContext.OutputMerger.SetRenderTargets(this.BackbufferRTV);
            Program.Renderer.ImmediateContext.PixelShader.SetShaderResources(0, this.LightBufferSRV);
            Program.Renderer.ImmediateContext.PixelShader.Set(this.TonemapPixelShader);
            Program.Renderer.ImmediateContext.Draw(3, 0);

        }

        public void BeginOverlays()
        {
            if (Program.Renderer == null)
                return;

            Program.Renderer.ImmediateContext.PixelShader.SetShaderResources(0, null, null, null);
            Program.Renderer.ImmediateContext.OutputMerger.SetRenderTargets(this.DepthStencilBufferDSV, this.BackbufferRTV);
        }

        #endregion

        private unsafe void UpdateConstantBuffer<T>(SharpDX.Direct3D11.Buffer buffer, ref T value) where T : unmanaged
        {
            if (Program.Renderer == null)
                return;

            SharpDX.DataBox box = Program.Renderer.ImmediateContext.MapSubresource(buffer, 0, MapMode.WriteDiscard, MapFlags.None, out SharpDX.DataStream stream);
            T* destination = (T*)box.DataPointer;
            *destination = value;
            Program.Renderer.ImmediateContext.UnmapSubresource(buffer, 0);
        }

        public void DrawLineMesh(Mesh<SolidColorVertex> mesh, Matrix4x4 transform)
        {
            if (Program.Renderer == null)
                return;

            this.SceneConstantValues.ModelMatrix = Matrix4x4.Transpose(transform);
            this.UpdateConstantBuffer(this.SceneConstantBuffer, ref this.SceneConstantValues);

            Program.Renderer.ImmediateContext.InputAssembler.InputLayout = this.SolidColorInputLayout;
            Program.Renderer.ImmediateContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.LineList;
            Program.Renderer.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(mesh.VertexBuffer, System.Runtime.InteropServices.Marshal.SizeOf<SolidColorVertex>(), 0));
            Program.Renderer.ImmediateContext.InputAssembler.SetIndexBuffer(mesh.IndexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
            Program.Renderer.ImmediateContext.VertexShader.Set(this.SolidColorVertexShader);
            Program.Renderer.ImmediateContext.VertexShader.SetConstantBuffer(0, this.SceneConstantBuffer);
            Program.Renderer.ImmediateContext.PixelShader.Set(this.SolidColorPixelShader);
            Program.Renderer.ImmediateContext.DrawIndexed((int)mesh.IndexCount, 0, 0);
        }

        public void DrawPointLight(Vector3 color, Vector3 position)
        {
            this.PointLightConstantValues.LightColor = color;
            this.PointLightConstantValues.LightPosition = Vector3.Transform(position, Matrix4x4.Transpose(this.SceneConstantValues.ViewMatrix));
            this.UpdateConstantBuffer(this.PointLightConstantBuffer, ref this.PointLightConstantValues);
            Program.Renderer.ImmediateContext.PixelShader.SetConstantBuffer(1, this.PointLightConstantBuffer);
            Program.Renderer.ImmediateContext.PixelShader.Set(this.PointLightPixelShader);
            Program.Renderer.ImmediateContext.Draw(3, 0);
        }

        public void DrawStaticModel(StaticModel model, Matrix4x4 transform)
        {
            this.SceneConstantValues.ModelMatrix = Matrix4x4.Transpose(transform);
            this.UpdateConstantBuffer(this.SceneConstantBuffer, ref this.SceneConstantValues);

            foreach (StaticModelSection section in model.Sections)
            {
                Program.Renderer.ImmediateContext.PixelShader.SetShaderResources(0, section.Material.DiffuseTexture ?? Program.Renderer.WhiteTextureSRV, section.Material.NormalTexture ?? Program.Renderer.FlatNormalTextureSRV, section.Material.MetallicRoughnessTexture ?? Program.Renderer.DefaultRoughnessMetallicTextureSRV);
                Program.Renderer.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(section.Mesh.VertexBuffer, System.Runtime.InteropServices.Marshal.SizeOf<StaticModelVertex>(), 0));
                Program.Renderer.ImmediateContext.InputAssembler.SetIndexBuffer(section.Mesh.IndexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
                Program.Renderer.ImmediateContext.DrawIndexed((int)section.Mesh.IndexCount, 0, 0);
            }
        }

        public void Dispose()
        {
            this.GBufferPixelShader.Dispose();
            this.GBufferVertexShader.Dispose();
            this.GBufferInputLayout.Dispose();
            this.SolidColorPixelShader.Dispose();
            this.SolidColorVertexShader.Dispose();
            this.SolidColorInputLayout.Dispose();
            this.SceneConstantBuffer.Dispose();
            this.StandardDepthStencilState.Dispose();
            this.StandardRasterizerState.Dispose();
            this.NormalMetallicBufferRTV.Dispose();
            this.NormalMetallicBufferSRV.Dispose();
            this.NormalMetallicBuffer.Dispose();
            this.DiffuseRoughnessBufferRTV.Dispose();
            this.DiffuseRoughnessBufferSRV.Dispose();
            this.DiffuseRoughnessBuffer.Dispose();
            this.DepthStencilBufferDSV.Dispose();
            this.DepthStencilBuffer.Dispose();
            this.BackbufferRTV.Dispose();
        }
    }
}
