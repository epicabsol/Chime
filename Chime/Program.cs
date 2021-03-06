using System.Numerics;

namespace Chime
{
    public class Program
    {
        public static Graphics.Renderer? Renderer { get; private set; }
        public static Platform.Application? Application { get; private set; }
        public static Platform.Window? Window { get; private set; }
        public static Platform.Headset? Headset { get; private set; }
        public static Scene.Scene? Scene { get; private set; }
        public static Scene.Camera? DesktopCamera { get; private set; }
        public static Scene.VRPlayer? VRPlayer { get; private set; }

        public static List<Input.InputDevice> InputDevices { get; } = new List<Input.InputDevice>();

        public static void Main(string[] args)
        {
            using (Program.Renderer = new Graphics.Renderer())
            {
                Program.Application = new Platform.Application();
                using (Program.Window = new Platform.Window(Program.Renderer, "Chime"))
                using (Program.Headset = Platform.Headset.Initialize())
                {
                    // Stop the application when the game window closes
                    Program.Window.Closed += (s, e) => Program.Application.Stop(0);
                    Program.Application.Tick += Application_Tick;

                    Program.Scene = new Scene.Scene();
                    Program.DesktopCamera = new Scene.PerspectiveCamera(1.0f, (float)Program.Window.Pipeline.Width / Program.Window.Pipeline.Height, 0.1f, 1000.0f, "Desktop Camera", new Vector3(1.0f, 2.0f, 2.0f), Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.5f) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, -0.5f));
                    Program.Scene.AddChild(Program.DesktopCamera);
                    Program.Scene.AddChild(new Scene.Prop(Graphics.StaticModel.FromGLTF(Chime.Properties.Resources.SuzannePBR), new BulletSharp.SphereShape(0.75f), 1.0f, "Test Model (SuzannePBR)", new Vector3(0.0f, 3.0f, -3.0f)));
                    Program.Scene.AddChild(new Scene.Prop(Graphics.StaticModel.FromGLTF(Chime.Properties.Resources.DamagedHelmet), new BulletSharp.SphereShape(0.75f), 1.0f, "Test Model (DamagedHelmet)", new Vector3(0.0f, 6.5f, -1.5f), null, new Vector3(0.1f, 0.1f, 0.1f)));
                    Program.Scene.AddChild(new Scene.Prop(Graphics.StaticModel.FromGLTF(Chime.Properties.Resources.Chime1), new BulletSharp.CylinderShape(new Vector3(0.1f, 0.35f, 0.1f)), 1.0f, "Test Model (Chime1)", new Vector3(0.0f, 1.5f, -0.5f)));
                    Chime.Scene.PointLight light = new Scene.PointLight(Vector3.One, "Test Light", new Vector3(0.0f, 20.0f, 0.0f));
                    light.Color = new Vector3(2000.0f, 2000.0f, 2000.0f);
                    Program.Scene.AddChild(light);

                    if (Program.Headset != null)
                    {
                        Program.InputDevices.Add(Program.Headset);

                        Program.VRPlayer = new Scene.VRPlayer(Program.Headset, "VR Player");
                        Program.Scene.AddChild(Program.VRPlayer);
                    }

                    Program.Scene.AddChild(new Scene.Grid(true, "Test Grid"));

                    Program.Application.Run();

                }
                Program.Headset?.Dispose();
                Program.Window = null;
                Program.Application = null;
            }
            Program.Renderer = null;
        }

        private static void Application_Tick(object? sender, Platform.TickEventArgs e)
        {
            if (Program.Renderer == null || Program.Window == null || Program.Scene == null || Program.DesktopCamera == null)
                return;

            Program.Headset?.GetVRInput();
            
            foreach (Input.InputDevice device in Program.InputDevices)
            {
                device.ProcessEvents();
            }

            Program.Scene.Update(e.DeltaTime);

            Program.Scene.DebugDraw.Commit();

            Program.Scene.Render(Program.Window.Pipeline, Program.DesktopCamera);
            Program.Window.SwapChain.Present(0, SharpDX.DXGI.PresentFlags.None);

            if (Program.Headset != null && Program.VRPlayer != null)
            {
                Program.Scene.Render(Program.Headset.LeftEyePipeline, Program.VRPlayer.LeftEye);
                Program.Scene.Render(Program.Headset.RightEyePipeline, Program.VRPlayer.RightEye);
                Program.Headset.PresentEyes();
            }
            Program.Scene.DebugDraw.Flush();
        }
    }
}
