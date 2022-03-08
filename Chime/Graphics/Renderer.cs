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

        public Renderer()
        {
            DeviceCreationFlags flags = DeviceCreationFlags.None;
#if DEBUG
            flags |= DeviceCreationFlags.Debug;
#endif
            this.Device = new Device(SharpDX.Direct3D.DriverType.Hardware, flags);
            this.ImmediateContext = this.Device.ImmediateContext;
        }

        public void Dispose()
        {
            this.ImmediateContext.Dispose();
            this.Device.Dispose();
        }
    }
}
