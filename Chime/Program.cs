

namespace Chime
{
    public class Program
    {
        public static Graphics.Renderer? Renderer { get; private set; }
        public static Platform.Application? Application { get; private set; }
        public static Platform.Window? Window { get; private set; }

        public static void Main(string[] args)
        {
            using (Program.Renderer = new Graphics.Renderer())
            {
                Program.Application = new Platform.Application();
                using (Program.Window = new Platform.Window(Program.Renderer, "Chime"))
                {
                    // Stop the application when the game window closes
                    Program.Window.Closed += (s, e) => Program.Application.Stop(0);
                    Program.Application.Tick += Application_Tick;

                    Program.Application.Run();

                }
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
        }
    }
}
