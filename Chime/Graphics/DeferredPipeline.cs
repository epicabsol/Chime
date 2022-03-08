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

        private SharpDX.Direct3D11.Buffer SceneConstantBuffer { get; set; }
        private SceneConstants SceneConstantValues;

        private InputLayout SolidColorInputLayout { get; set; }
        private VertexShader SolidColorVertexShader { get; set; }
        private PixelShader SolidColorPixelShader { get; set; }

        public DeferredPipeline(Texture2D backbuffer, int width, int height)
        {
            if (Program.Renderer == null)
                throw new NullReferenceException();

            this.Width = width;
            this.Height = height;
            this.Backbuffer = backbuffer;
            this.BackbufferRTV = new RenderTargetView(Program.Renderer.Device, this.Backbuffer);
            this.SceneConstantBuffer = new SharpDX.Direct3D11.Buffer(Program.Renderer.Device, new BufferDescription(System.Runtime.InteropServices.Marshal.SizeOf<SceneConstants>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0));

            this.SolidColorInputLayout = new InputLayout(Program.Renderer.Device, Program.Renderer.Shaders.SolidColorShaderVS, new InputElement[] { new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0), new InputElement("COLOR", 0, SharpDX.DXGI.Format.R32G32B32A32_Float, 0) });
            this.SolidColorVertexShader = new VertexShader(Program.Renderer.Device, Program.Renderer.Shaders.SolidColorShaderVS);
            this.SolidColorPixelShader = new PixelShader(Program.Renderer.Device, Program.Renderer.Shaders.SolidColorShaderPS);
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

        public void Dispose()
        {
            this.SolidColorPixelShader.Dispose();
            this.SolidColorVertexShader.Dispose();
            this.SolidColorInputLayout.Dispose();
            this.SceneConstantBuffer.Dispose();
            this.BackbufferRTV.Dispose();
        }
    }
}
