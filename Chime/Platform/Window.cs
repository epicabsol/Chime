

using System.Runtime.InteropServices;

namespace Chime.Platform
{
    public unsafe class Window : IDisposable
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(IntPtr lpModuleName);

        private const string WindowClass = "ChimeWindow";
        private static int HasRegisteredWindowClass = 0;

        public IntPtr Handle { get; }
        public SharpDX.DXGI.SwapChain SwapChain { get; }
        public Graphics.DeferredPipeline Pipeline { get; private set; }

        public event EventHandler<EventArgs>? Closed;

        public Window(Graphics.Renderer renderer, string caption)
        {
            IntPtr instanceHandle = GetModuleHandle(IntPtr.Zero);

            // Register the window class if we haven't already
            if (Interlocked.CompareExchange(ref HasRegisteredWindowClass, 1, 0) == 0)
            {
                fixed (char* windowClassBytes = Window.WindowClass)
                {
                    PInvoke.User32.WNDCLASS windowClass = new PInvoke.User32.WNDCLASS
                    {
                        lpfnWndProc = this.WindowProc,
                        hInstance = instanceHandle,
                        lpszClassName = windowClassBytes,
                        hCursor = PInvoke.User32.LoadCursor(IntPtr.Zero, (IntPtr)PInvoke.User32.Cursors.IDC_ARROW).DangerousGetHandle()
                    };
                    PInvoke.User32.RegisterClass(ref windowClass);
                }
            }

            // Create the window
            this.Handle = PInvoke.User32.CreateWindowEx(0, Window.WindowClass, caption, PInvoke.User32.WindowStyles.WS_OVERLAPPEDWINDOW, PInvoke.User32.CW_USEDEFAULT, PInvoke.User32.CW_USEDEFAULT, PInvoke.User32.CW_USEDEFAULT, PInvoke.User32.CW_USEDEFAULT, IntPtr.Zero, IntPtr.Zero, instanceHandle, null);
            if (this.Handle == IntPtr.Zero)
            {
                throw new Exception("Failed to create window.");
            }

            PInvoke.User32.GetClientRect(this.Handle, out PInvoke.RECT clientBounds);

            // Initialize the swapchain
            using (SharpDX.DXGI.Factory2 factory = new SharpDX.DXGI.Factory2())
            {
                SharpDX.DXGI.SwapChainDescription1 swapchainDesc = new SharpDX.DXGI.SwapChainDescription1();
                swapchainDesc.AlphaMode = SharpDX.DXGI.AlphaMode.Ignore;
                swapchainDesc.SwapEffect = SharpDX.DXGI.SwapEffect.FlipDiscard;
                swapchainDesc.BufferCount = 2;
                swapchainDesc.Flags = SharpDX.DXGI.SwapChainFlags.None;
                swapchainDesc.Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm;
                swapchainDesc.Width = clientBounds.right - clientBounds.left;
                swapchainDesc.Height = clientBounds.bottom - clientBounds.top;
                swapchainDesc.SampleDescription.Count = 1;
                swapchainDesc.Scaling = SharpDX.DXGI.Scaling.Stretch;
                swapchainDesc.Stereo = false;
                swapchainDesc.Usage = SharpDX.DXGI.Usage.RenderTargetOutput;

                this.SwapChain = new SharpDX.DXGI.SwapChain1(factory, renderer.Device, this.Handle, ref swapchainDesc);
            }

            // Create the deferred pipeline
            this.Pipeline = new Graphics.DeferredPipeline(this.SwapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0), clientBounds.right - clientBounds.left, clientBounds.bottom - clientBounds.top);

            PInvoke.User32.ShowWindow(this.Handle, PInvoke.User32.WindowShowStyle.SW_SHOWMAXIMIZED);
        }

        private IntPtr WindowProc(IntPtr windowHandle, PInvoke.User32.WindowMessage message, void* wParam, void* lParam)
        {
            switch (message)
            {
                case PInvoke.User32.WindowMessage.WM_DESTROY:
                    this.Closed?.Invoke(this, new EventArgs());
                    return IntPtr.Zero;
                case PInvoke.User32.WindowMessage.WM_SIZE:
                    int width = ((int)lParam) & 0x0000FFFF;
                    int height = ((int)lParam) >> 16;
                    this.OnResize(width, height);
                    return IntPtr.Zero;
                default:
                    return PInvoke.User32.DefWindowProc(windowHandle, message, (IntPtr)wParam, (IntPtr)lParam);
            }
        }

        private void OnResize(int width, int height)
        {
            this.Pipeline.Dispose();
            this.Pipeline.Backbuffer.Dispose();

            this.SwapChain.ResizeBuffers(0, width, height, SharpDX.DXGI.Format.Unknown, SharpDX.DXGI.SwapChainFlags.None);

            this.Pipeline = new Graphics.DeferredPipeline(this.SwapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0), width, height);

            System.Diagnostics.Debug.WriteLine($"Resized to {width}x{height}.");
        }

        public void Show()
        {
            PInvoke.User32.ShowWindow(this.Handle, PInvoke.User32.WindowShowStyle.SW_SHOW);
        }

        public void Hide()
        {
            PInvoke.User32.ShowWindow(this.Handle, PInvoke.User32.WindowShowStyle.SW_HIDE);
        }

        public void Dispose()
        {
            this.Pipeline.Dispose();
            this.Pipeline.Backbuffer.Dispose();
            this.SwapChain.Dispose();
            PInvoke.User32.DestroyWindow(this.Handle);
        }
    }
}
