using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX.Direct3D11;

namespace Chime.Graphics
{
    public class Renderer : IDisposable
    {
        public Device Device { get; }
        public DeviceContext ImmediateContext { get; }

        public Shaders Shaders { get; }

        public Texture2D WhiteTexture { get; }
        public ShaderResourceView WhiteTextureSRV { get; }
        public Texture2D FlatNormalTexture { get; }
        public ShaderResourceView FlatNormalTextureSRV { get; }
        public Texture2D DefaultRoughnessMetallicTexture { get; }
        public ShaderResourceView DefaultRoughnessMetallicTextureSRV { get; }

        public unsafe Renderer()
        {
            DeviceCreationFlags flags = DeviceCreationFlags.None;
#if DEBUG
            flags |= DeviceCreationFlags.Debug;
#endif
            this.Device = new Device(SharpDX.Direct3D.DriverType.Hardware, flags);
            this.ImmediateContext = this.Device.ImmediateContext;
            this.Shaders = new Shaders();

            Span<byte> pixelData = stackalloc byte[4];
            fixed (byte* pixelDataPtr = pixelData)
            {
                pixelData[0] = 255;
                pixelData[1] = 255;
                pixelData[2] = 255;
                pixelData[3] = 255;
                this.WhiteTexture = new Texture2D(this.Device, new Texture2DDescription() { ArraySize = 1, BindFlags = BindFlags.ShaderResource, CpuAccessFlags = CpuAccessFlags.None, Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm, Height = 1, Width = 1, MipLevels = 1, OptionFlags = ResourceOptionFlags.None, SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0), Usage = ResourceUsage.Immutable }, new SharpDX.DataRectangle((IntPtr)pixelDataPtr, 4));

                pixelData[0] = 127;
                pixelData[1] = 127;
                pixelData[2] = 255;
                pixelData[3] = 255;
                this.FlatNormalTexture = new Texture2D(this.Device, new Texture2DDescription() { ArraySize = 1, BindFlags = BindFlags.ShaderResource, CpuAccessFlags = CpuAccessFlags.None, Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm, Height = 1, Width = 1, MipLevels = 1, OptionFlags = ResourceOptionFlags.None, SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0), Usage = ResourceUsage.Immutable }, new SharpDX.DataRectangle((IntPtr)pixelDataPtr, 4));

                pixelData[0] = 200;
                pixelData[1] = 0;
                pixelData[2] = 0;
                pixelData[3] = 255;
                this.DefaultRoughnessMetallicTexture = new Texture2D(this.Device, new Texture2DDescription() { ArraySize = 1, BindFlags = BindFlags.ShaderResource, CpuAccessFlags = CpuAccessFlags.None, Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm, Height = 1, Width = 1, MipLevels = 1, OptionFlags = ResourceOptionFlags.None, SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0), Usage = ResourceUsage.Immutable }, new SharpDX.DataRectangle((IntPtr)pixelDataPtr, 4));
            }
            this.WhiteTextureSRV = new ShaderResourceView(this.Device, this.WhiteTexture);
            this.FlatNormalTextureSRV = new ShaderResourceView(this.Device, this.FlatNormalTexture);
            this.DefaultRoughnessMetallicTextureSRV = new ShaderResourceView(this.Device, this.DefaultRoughnessMetallicTexture);
        }

        public void Dispose()
        {
            this.DefaultRoughnessMetallicTextureSRV.Dispose();
            this.DefaultRoughnessMetallicTexture.Dispose();
            this.FlatNormalTextureSRV.Dispose();
            this.FlatNormalTexture.Dispose();
            this.WhiteTextureSRV.Dispose();
            this.WhiteTexture.Dispose();
            this.ImmediateContext.Dispose();
            this.Device.Dispose();
        }
    }
}
