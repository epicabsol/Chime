using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Chime.Scene
{
    public class VRPlayer : SceneObject
    {
        public VREyeCamera LeftEye { get; }
        public VREyeCamera RightEye { get; }

        public VRPlayer(string? name = null) : base(name)
        {
            if (Program.Headset == null)
                throw new Exception("No headset detected!");

            this.LeftEye = new VREyeCamera(Platform.Headset.Eye.Left, 0.1f, 1000.0f, "LeftEye");
            this.AddChild(this.LeftEye);
            this.RightEye = new VREyeCamera(Platform.Headset.Eye.Right, 0.1f, 1000.0f, "RightEye");
            this.AddChild(this.RightEye);
        }
    }
}
