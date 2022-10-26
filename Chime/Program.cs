using System.Numerics;
using System.Runtime.CompilerServices;

namespace Chime
{
    public class Program
    {
        public static Graphics.Renderer? Renderer { get; private set; }
        public static Platform.Application? Application { get; private set; }
        public static Platform.Window? Window { get; private set; }
        public static Platform.Headset? Headset { get; private set; }
        

        public static List<Input.InputDevice> InputDevices { get; } = new List<Input.InputDevice>();

        public static Screen? CurrentScreen { get; set; }

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

                    if (Program.Headset != null)
                    {
                        Program.InputDevices.Add(Program.Headset);
                    }

                    // Create the initial screen
                    Program.CurrentScreen = new Screens.TestScreen();

                    Program.Application.Run();

                }
                Program.CurrentScreen?.Dispose();
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

            //
            // Input processing
            //
            Program.Headset?.GetVRInput();
            
            foreach (Input.InputDevice device in Program.InputDevices)
            {
                device.ProcessEvents();
            }

            //
            // Update game state
            //
            Program.CurrentScreen?.Update(e.DeltaTime);

            //
            // Render graphics
            //
            Program.CurrentScreen?.Render();

            Program.Window.SwapChain.Present(0, SharpDX.DXGI.PresentFlags.None);
        }
    }
}
