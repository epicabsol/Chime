using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace Chime.Graphics
{
    public class DeferredPipeline : IDisposable
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Texture2D Backbuffer { get; private set; }
        public RenderTargetView BackbufferRTV { get; private set; }

        public DeferredPipeline(Texture2D backbuffer, int width, int height)
        {
            if (Program.Renderer == null)
                throw new NullReferenceException();

            this.Width = width;
            this.Height = height;
            this.Backbuffer = backbuffer;
            this.BackbufferRTV = new RenderTargetView(Program.Renderer.Device, this.Backbuffer);
        }

        public void Dispose()
        {
            this.BackbufferRTV.Dispose();
        }
    }
}
