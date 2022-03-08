

namespace Chime
{
    public class Program
    {
        public static Graphics.Renderer? Renderer { get; private set; }
        public static Platform.Application? Application { get; private set; }
        public static Platform.Window? Window { get; private set; }
        public static Platform.Headset? Headset { get; private set; }

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
            if (Program.Renderer == null || Program.Window == null)
                return;

            Program.Renderer.ImmediateContext.ClearRenderTargetView(Program.Window.Pipeline.BackbufferRTV, new SharpDX.Mathematics.Interop.RawColor4(1.0f, 0.0f, 0.0f, 1.0f));
            Program.Window.SwapChain.Present(0, SharpDX.DXGI.PresentFlags.None);

            if (Program.Headset != null)
            {
                Program.Headset.UpdatePose();
                Program.Renderer.ImmediateContext.ClearRenderTargetView(Program.Headset.LeftEyePipeline.BackbufferRTV, new SharpDX.Mathematics.Interop.RawColor4(0.0f, 1.0f, 0.0f, 1.0f));
                Program.Renderer.ImmediateContext.ClearRenderTargetView(Program.Headset.RightEyePipeline.BackbufferRTV, new SharpDX.Mathematics.Interop.RawColor4(0.0f, 0.0f, 1.0f, 1.0f));
                Program.Headset.PresentEye(true);
                Program.Headset.PresentEye(false);
            }
        }
    }
}
