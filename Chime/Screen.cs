using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Chime
{
    public class Screen : IDisposable
    {
        public Scene.Scene Scene { get; private set; }
        public Scene.Camera DesktopCamera { get; private set; }
        public Scene.VRPlayer? VRPlayer { get; private set; }

        public Screen()
        {
            this.Scene = new Scene.Scene();

            // Create desktop camera
            this.DesktopCamera = new Scene.PerspectiveCamera(1.0f, (float)Program.Window.Pipeline.Width / Program.Window.Pipeline.Height, 0.1f, 1000.0f, "Desktop Camera", new Vector3(1.0f, 2.0f, 2.0f), Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.5f) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, -0.5f));
            this.Scene.AddChild(this.DesktopCamera);

            // Create VR objects if playing in VR
            if (Program.Headset != null)
            {
                this.VRPlayer = new Scene.VRPlayer(Program.Headset, "VR Player");
                this.Scene.AddChild(this.VRPlayer);
            }
        }

        public virtual void Update(float deltaTime)
        {
            this.Scene.Update(deltaTime);
        }

        public virtual void Render()
        {
            this.Scene.DebugDraw.Commit();

            this.Scene.Render(Program.Window.Pipeline, this.DesktopCamera);

            if (Program.Headset != null && this.VRPlayer != null)
            {
                this.Scene.Render(Program.Headset.LeftEyePipeline, this.VRPlayer.LeftEye);
                this.Scene.Render(Program.Headset.RightEyePipeline, this.VRPlayer.RightEye);
                Program.Headset.PresentEyes();
            }
            Scene.DebugDraw.Flush();
        }

        public virtual void Dispose()
        {
            this.Scene.Dispose();
        }
    }
}
