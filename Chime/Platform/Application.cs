﻿

using System.Runtime.InteropServices;

namespace Chime.Platform
{
    public class TickEventArgs : EventArgs
    {
        public float DeltaTime { get; }

        public TickEventArgs(float deltaTime)
        {
            this.DeltaTime = deltaTime;
        }
    }

    public class Application
    {

        public event EventHandler<TickEventArgs>? Tick;

        public Application()
        {
            
        }

        public unsafe int Run()
        {
            PInvoke.User32.MSG message = new PInvoke.User32.MSG();

            // Simple message loop for UI applications, that blocks while there are no new messages
            /*while (PInvoke.User32.GetMessage(&message, IntPtr.Zero, 0, 0) != 0)
            {
                PInvoke.User32.TranslateMessage(&message);
                PInvoke.User32.DispatchMessage(&message);
            }*/

            // Real-time game message loop that handles all pending messages without blocking and then updates the game
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            int exitCode = 0;
            stopwatch.Start();
            bool wantsQuit = false;
            while (!wantsQuit)
            {
                while (PInvoke.User32.PeekMessage(&message, IntPtr.Zero, 0, 0, PInvoke.User32.PeekMessageRemoveFlags.PM_REMOVE))
                {
                    PInvoke.User32.TranslateMessage(&message);
                    IntPtr result = PInvoke.User32.DispatchMessage(&message);
                    if (message.message == PInvoke.User32.WindowMessage.WM_QUIT)
                    {
                        wantsQuit = true;
                        exitCode = (int)result;
                        return exitCode;
                    }
                }

                float deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();
                this.Tick?.Invoke(this, new TickEventArgs(deltaTime));
                System.Threading.Thread.Sleep(20);
            }

            return exitCode;
        }

        public void Stop(int exitCode)
        {
            PInvoke.User32.PostQuitMessage(exitCode);
        }
    }
}
