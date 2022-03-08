

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
                    Program.DesktopCamera = new Scene.PerspectiveCamera(1.0f, (float)Program.Window.Pipeline.Width / Program.Window.Pipeline.Height, 0.1f, 1000.0f, "Desktop Camera");
                    Program.DesktopCamera.Position = new System.Numerics.Vector3(0.0f, 5.0f, 8.0f);
                    Program.DesktopCamera.Rotation = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitY, 0.5f) * System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX, -0.5f);
                    Program.Scene.AddChild(Program.DesktopCamera);

                    if (Program.Headset != null)
                    {
                        Program.VRPlayer = new Scene.VRPlayer("VR Player");
                        Program.Scene.AddChild(Program.VRPlayer);
                        Program.VRPlayer.LeftEye.AddChild(new Scene.Grid(false) { Scale = System.Numerics.Vector3.One * 0.1f });
                        Program.VRPlayer.RightEye.AddChild(new Scene.Grid(false) { Scale = System.Numerics.Vector3.One * 0.1f });
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

            Program.Scene.Update(e.DeltaTime);

            Program.Scene.Render(Program.Window.Pipeline, Program.DesktopCamera);
            Program.Window.SwapChain.Present(0, SharpDX.DXGI.PresentFlags.None);

            if (Program.Headset != null && Program.VRPlayer != null)
            {
                Program.Headset.UpdatePose(out System.Numerics.Vector3 headsetPosition, out System.Numerics.Quaternion headsetRotation);
                Program.VRPlayer.Position = headsetPosition;
                Program.VRPlayer.Rotation = headsetRotation;
                Program.Scene.Render(Program.Headset.LeftEyePipeline, Program.VRPlayer.LeftEye);
                Program.Scene.Render(Program.Headset.RightEyePipeline, Program.VRPlayer.RightEye);
                Program.Headset.PresentEyes();
            }
        }
    }
}
