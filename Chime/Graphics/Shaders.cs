using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace Chime.Graphics
{
    public class Shaders
    {
        private const string VertexEntrypoint = "VS_Main";
        private const string PixelEntrypoint = "PS_Main";

        public byte[] SolidColorShaderVS { get; }
        public byte[] SolidColorShaderPS { get; }

        public byte[] GBufferShaderVS { get; }
        public byte[] GBufferShaderPS { get; }

        public byte[] ScreenQuadVS { get; }

        public byte[] PointLightPS { get; }
        public byte[] TonemapPS { get; }

        public Shaders()
        {
            this.SolidColorShaderVS = Shaders.CompileShader(Chime.Properties.Resources.SolidColorShader, VertexEntrypoint);
            this.SolidColorShaderPS = Shaders.CompileShader(Chime.Properties.Resources.SolidColorShader, PixelEntrypoint);

            this.GBufferShaderVS = Shaders.CompileShader(Chime.Properties.Resources.GBufferShader, VertexEntrypoint);
            this.GBufferShaderPS = Shaders.CompileShader(Chime.Properties.Resources.GBufferShader, PixelEntrypoint);

            this.ScreenQuadVS = Shaders.CompileShader(Chime.Properties.Resources.ScreenQuadVS, VertexEntrypoint);

            this.PointLightPS = Shaders.CompileShader(Chime.Properties.Resources.PointLightShader, PixelEntrypoint);
            this.TonemapPS = Shaders.CompileShader(Chime.Properties.Resources.TonemapPS, PixelEntrypoint);
        }

        private static byte[] CompileShader(byte[] source, string entrypoint)
        {
            using (SharpDX.D3DCompiler.CompilationResult result = SharpDX.D3DCompiler.ShaderBytecode.Compile(source, entrypoint, entrypoint.Substring(0, 2).ToLower() + "_5_0", SharpDX.D3DCompiler.ShaderFlags.Debug))
            {
                if (result.HasErrors || (result.Message?.Length ?? 0) > 0)
                {
                    throw new Exception("Failed to compile shader. Message: " + result.Message);
                }
                return result.Bytecode.Data;
            }
        }
    }
}
