using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenVR;
using SharpDX.Direct3D11;

namespace Chime.Platform
{
    public class Headset : IDisposable
    {
        public CVRSystem VRSystem { get; }
        public Graphics.DeferredPipeline LeftEyePipeline { get; }
        public Graphics.DeferredPipeline RightEyePipeline { get; }

        private Headset(CVRSystem vrSystem)
        {
            if (Program.Renderer == null)
                throw new NullReferenceException();

            this.VRSystem = vrSystem;

            uint width = 0;
            uint height = 0;
            this.VRSystem.GetRecommendedRenderTargetSize(ref width, ref height);
            System.Diagnostics.Debug.WriteLine($"[INFO]: Headset recommends using a render target with size {width}x{height}.");

            // Create the eye pipelines
            Texture2DDescription eyeBackbufferDescription = new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                Width = (int)width,
                Height = (int)height,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };
            Texture2D leftBackbuffer = new Texture2D(Program.Renderer.Device, eyeBackbufferDescription);
            this.LeftEyePipeline = new Graphics.DeferredPipeline(leftBackbuffer, (int)width, (int)height);
            Texture2D rightBackbuffer = new Texture2D(Program.Renderer.Device, eyeBackbufferDescription);
            this.RightEyePipeline = new Graphics.DeferredPipeline(rightBackbuffer, (int)width, (int)height);
        }

        public void UpdatePose()
        {
            Span<TrackedDevicePose_t> renderPoses = stackalloc TrackedDevicePose_t[1];
            Span<TrackedDevicePose_t> gamePoses = stackalloc TrackedDevicePose_t[1];
            OpenVR.OpenVR.Compositor.WaitGetPoses(renderPoses, gamePoses);
        }

        public void PresentEye(bool leftEye)
        {
            Texture_t eyeTexture = new Texture_t()
            {
                handle = (leftEye ? this.LeftEyePipeline.Backbuffer : this.RightEyePipeline.Backbuffer).NativePointer,
                eType = ETextureType.DirectX,
                eColorSpace = EColorSpace.Auto
            };
            VRTextureBounds_t eyeTextureBounds = new VRTextureBounds_t()
            {
                uMin = 0.0f,
                uMax = 1.0f,
                vMin = 0.0f,
                vMax = 1.0f,
            };
            EVRCompositorError result = OpenVR.OpenVR.Compositor.Submit(leftEye ? EVREye.Eye_Left : EVREye.Eye_Right, ref eyeTexture, ref eyeTextureBounds, EVRSubmitFlags.Submit_Default);
        }

        public void Dispose()
        {
            this.RightEyePipeline.Dispose();
            this.RightEyePipeline.Backbuffer.Dispose();
            this.LeftEyePipeline.Dispose();
            this.LeftEyePipeline.Backbuffer.Dispose();
        }

        public static Headset? Initialize()
        {
            // Check for the runtime installation
            if (!OpenVR.OpenVR.IsRuntimeInstalled())
            {
                System.Diagnostics.Debug.WriteLine("[WARNING]: OpenVR runtime not installed. Have you not installed SteamVR?");
                return null;
            }

            // Check for an attached headset
            if (!OpenVR.OpenVR.IsHmdPresent())
            {
                System.Diagnostics.Debug.WriteLine("[WARNING]: VR headset not connected.");
                return null;
            }

            // Initialize OpenVR
            EVRInitError initResult = EVRInitError.None;
            CVRSystem vrSystem = OpenVR.OpenVR.Init(ref initResult, EVRApplicationType.VRApplication_Scene);
            if (initResult != EVRInitError.None || vrSystem == null)
            {
                System.Diagnostics.Debug.WriteLine($"[WARNING]: Couldn't initialize OpenVR: ${initResult}");
                return null;
            }

            return new Headset(vrSystem);
        }
    }
}
