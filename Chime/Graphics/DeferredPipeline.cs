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
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public Texture2D Backbuffer { get; private set; }
        public RenderTargetView BackbufferRTV { get; private set; }
        public Texture2D DepthStencilBuffer { get; private set; }
        public DepthStencilView DepthStencilBufferDSV { get; private set; }
        public DepthStencilState StandardDepthStencilState { get; private set; }
        public RasterizerState StandardRasterizerState { get; private set; }

        private SharpDX.Direct3D11.Buffer SceneConstantBuffer { get; set; }
        private SceneConstants SceneConstantValues;

        private InputLayout SolidColorInputLayout { get; set; }
        private VertexShader SolidColorVertexShader { get; set; }
        private PixelShader SolidColorPixelShader { get; set; }

        private InputLayout GBufferInputLayout { get; set; }
        private VertexShader GBufferVertexShader { get; set; }
        private PixelShader GBufferPixelShader { get; set; }

        public DeferredPipeline(Texture2D backbuffer, int width, int height)
        {
            if (Program.Renderer == null)
                throw new NullReferenceException();

            this.Width = width;
            this.Height = height;
            this.Backbuffer = backbuffer;
            this.BackbufferRTV = new RenderTargetView(Program.Renderer.Device, this.Backbuffer);

            this.DepthStencilBuffer = new Texture2D(Program.Renderer.Device, new Texture2DDescription() { ArraySize = 1, BindFlags = BindFlags.DepthStencil, CpuAccessFlags = CpuAccessFlags.None, Format = SharpDX.DXGI.Format.D24_UNorm_S8_UInt, Width = this.Width, Height = this.Height, MipLevels = 1, OptionFlags = ResourceOptionFlags.None, SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0), Usage = ResourceUsage.Default });
            this.DepthStencilBufferDSV = new DepthStencilView(Program.Renderer.Device, this.DepthStencilBuffer);
            this.StandardRasterizerState = new RasterizerState(Program.Renderer.Device, new RasterizerStateDescription() { CullMode = CullMode.Back, DepthBias = 0, DepthBiasClamp = 0.0f, FillMode = FillMode.Solid, IsAntialiasedLineEnabled = false, IsDepthClipEnabled = false, IsFrontCounterClockwise = true, IsMultisampleEnabled = false, IsScissorEnabled = false, SlopeScaledDepthBias = 0.0f });
            this.StandardDepthStencilState = new DepthStencilState(Program.Renderer.Device, new DepthStencilStateDescription() { DepthComparison = Comparison.Less, DepthWriteMask = DepthWriteMask.All, IsDepthEnabled = true, IsStencilEnabled = false, FrontFace = new DepthStencilOperationDescription() { Comparison = Comparison.Always, DepthFailOperation = StencilOperation.Keep, PassOperation = StencilOperation.Keep, FailOperation = StencilOperation.Keep } });

            this.SceneConstantBuffer = new SharpDX.Direct3D11.Buffer(Program.Renderer.Device, new BufferDescription(System.Runtime.InteropServices.Marshal.SizeOf<SceneConstants>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0));

            this.SolidColorInputLayout = new InputLayout(Program.Renderer.Device, Program.Renderer.Shaders.SolidColorShaderVS, new InputElement[] { new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0), new InputElement("COLOR", 0, SharpDX.DXGI.Format.R32G32B32A32_Float, 0) });
            this.SolidColorVertexShader = new VertexShader(Program.Renderer.Device, Program.Renderer.Shaders.SolidColorShaderVS);
            this.SolidColorPixelShader = new PixelShader(Program.Renderer.Device, Program.Renderer.Shaders.SolidColorShaderPS);

            this.GBufferInputLayout = new InputLayout(Program.Renderer.Device, Program.Renderer.Shaders.GBufferShaderVS, new InputElement[] { new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0), new InputElement("NORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0), new InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float, 0) });
            this.GBufferVertexShader = new VertexShader(Program.Renderer.Device, Program.Renderer.Shaders.GBufferShaderVS);
            this.GBufferPixelShader = new PixelShader(Program.Renderer.Device, Program.Renderer.Shaders.GBufferShaderPS);
        }

        #region Pipeline Stages

        public void BeginGBuffer(Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
        {
            if (Program.Renderer == null)
                return;

            this.SceneConstantValues.ViewMatrix = Matrix4x4.Transpose(viewMatrix);
            this.SceneConstantValues.ProjectionMatrix = Matrix4x4.Transpose(projectionMatrix);
            Program.Renderer.ImmediateContext.Rasterizer.SetViewport(new SharpDX.Mathematics.Interop.RawViewportF() { Width = this.Width, Height = this.Height, X = 0, Y = 0, MinDepth = 0, MaxDepth = 1 });
            Program.Renderer.ImmediateContext.ClearRenderTargetView(this.BackbufferRTV, new SharpDX.Mathematics.Interop.RawColor4(0.2f, 0.2f, 0.2f, 1.0f));
            Program.Renderer.ImmediateContext.ClearDepthStencilView(this.DepthStencilBufferDSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            Program.Renderer.ImmediateContext.InputAssembler.InputLayout = this.GBufferInputLayout;
            Program.Renderer.ImmediateContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            Program.Renderer.ImmediateContext.Rasterizer.State = this.StandardRasterizerState;
            Program.Renderer.ImmediateContext.OutputMerger.DepthStencilState = this.StandardDepthStencilState;
            Program.Renderer.ImmediateContext.VertexShader.Set(this.GBufferVertexShader);
            Program.Renderer.ImmediateContext.VertexShader.SetConstantBuffer(0, this.SceneConstantBuffer);
            Program.Renderer.ImmediateContext.PixelShader.Set(this.GBufferPixelShader);

            Program.Renderer.ImmediateContext.OutputMerger.SetRenderTargets(this.DepthStencilBufferDSV, this.BackbufferRTV);
        }

        public void ShadeGBuffer()
        {

        }

        public void PostProcess()
        {

        }

        public void BeginOverlays()
        {
            if (Program.Renderer == null)
                return;

            Program.Renderer.ImmediateContext.OutputMerger.SetRenderTargets(this.BackbufferRTV);
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

        public void DrawStaticModel(StaticModel model, Matrix4x4 transform)
        {
            this.SceneConstantValues.ModelMatrix = Matrix4x4.Transpose(transform);
            this.UpdateConstantBuffer(this.SceneConstantBuffer, ref this.SceneConstantValues);

            foreach (StaticModelSection section in model.Sections)
            {
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
            this.DepthStencilBufferDSV.Dispose();
            this.DepthStencilBuffer.Dispose();
            this.BackbufferRTV.Dispose();
        }
    }
}
