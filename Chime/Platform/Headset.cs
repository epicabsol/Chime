using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using OpenVR;
using SharpDX.Direct3D11;

namespace Chime.Platform
{
    public class Headset : TrackedDevice, IDisposable
    {
        public enum Eye
        {
            Left,
            Right,
        }

        public Graphics.DeferredPipeline LeftEyePipeline { get; }
        public Graphics.DeferredPipeline RightEyePipeline { get; }

        public override string DisplayName => "VR Headset";

        private TrackedDevice?[] TrackedDevices { get; } = new TrackedDevice?[OpenVR.OpenVR.k_unMaxTrackedDeviceCount];

        public IEnumerable<TrackedDevice> ConnectedDevices => this.TrackedDevices.Where(device => device != null).Select(device => device!);

        private Headset() : base(OpenVR.OpenVR.k_unTrackedDeviceIndex_Hmd)
        {
            if (Program.Renderer == null)
                throw new NullReferenceException();

            uint width = 0;
            uint height = 0;
            OpenVR.OpenVR.System.GetRecommendedRenderTargetSize(ref width, ref height);
            System.Diagnostics.Debug.WriteLine($"[INFO]: Headset recommends using a render target with size {width}x{height}.");

            // TODO: Implement instanced stereo rendering (see https://docs.microsoft.com/en-us/windows/mixed-reality/develop/native/rendering-in-directx)

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

            // Loop through each tracked device slot checking for active devices
            for (uint i = 0; i < OpenVR.OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                this.RefreshDeviceSlot(i);
            }
            this.TrackedDevices[this.VRDeviceIndex] = this;
        }

        /*public void UpdatePose(out Vector3 deviceTranslation, out Quaternion deviceRotation)
        {
            Span<TrackedDevicePose_t> renderPoses = stackalloc TrackedDevicePose_t[1];
            Span<TrackedDevicePose_t> gamePoses = stackalloc TrackedDevicePose_t[1];
            OpenVR.OpenVR.Compositor.WaitGetPoses(renderPoses, gamePoses);
            Matrix4x4.Decompose(((Matrix4x4)renderPoses[0].mDeviceToAbsoluteTracking), out _, out deviceRotation, out deviceTranslation);
        }*/

        public void PresentEyes()
        {
            Texture_t eyeTexture = new Texture_t()
            {
                handle = this.LeftEyePipeline.Backbuffer.NativePointer,
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
            EVRCompositorError result = OpenVR.OpenVR.Compositor.Submit(EVREye.Eye_Left, ref eyeTexture, ref eyeTextureBounds, EVRSubmitFlags.Submit_Default);
            eyeTexture.handle = this.RightEyePipeline.Backbuffer.NativePointer;
            result = OpenVR.OpenVR.Compositor.Submit(EVREye.Eye_Right, ref eyeTexture, ref eyeTextureBounds, EVRSubmitFlags.Submit_Default);
        }

        public void GetEyeProjection(Eye eye, float nearClip, float farClip, out Matrix4x4 projection)
        {
            projection = Matrix4x4.Transpose(OpenVR.OpenVR.System.GetProjectionMatrix(eye == Eye.Left ? EVREye.Eye_Left : EVREye.Eye_Right, nearClip, farClip));
        }

        public void GetEyeTransform(Eye eye, out Vector3 translation, out Quaternion rotation)
        {
            Matrix4x4.Decompose(OpenVR.OpenVR.System.GetEyeToHeadTransform(eye == Eye.Left ? EVREye.Eye_Left : EVREye.Eye_Right), out _, out rotation, out translation);
        }

        // These large-ish arrays are only used within this function, but we don't want to be allocating them every single time we go through here
        private static TrackedDevicePose_t[] _deviceRenderPoses = new TrackedDevicePose_t[(int)OpenVR.OpenVR.k_unMaxTrackedDeviceCount];
        private static TrackedDevicePose_t[] _deviceGamePoses = new TrackedDevicePose_t[(int)OpenVR.OpenVR.k_unMaxTrackedDeviceCount];
        public void GetVRInput()
        {
            // Handle VR events
            VREvent_t eventInfo = new VREvent_t();
            while (OpenVR.OpenVR.System.PollNextEvent(ref eventInfo, (uint)System.Runtime.InteropServices.Marshal.SizeOf<VREvent_t>()))
            {
                double eventTime = Program.Application!.ApplicationTime;
                System.Diagnostics.Debug.WriteLine($"VR Event: {eventInfo.eventType} for device {eventInfo.trackedDeviceIndex}");

                if (eventInfo.trackedDeviceIndex == OpenVR.OpenVR.k_unTrackedDeviceIndexInvalid)
                {
                    // System event - we don't care about these at this point
                    System.Diagnostics.Debug.WriteLine($"[WARNING]: OpenVR event to system was ignored.");
                }
                else
                {
                    switch (eventInfo.eventType)
                    {
                        case EVREventType.VREvent_TrackedDeviceActivated:
                            this.RefreshDeviceSlot(eventInfo.trackedDeviceIndex);
                            continue;
                        case EVREventType.VREvent_TrackedDeviceDeactivated:
                        {
                            if (this.TrackedDevices[eventInfo.trackedDeviceIndex] is TrackedDevice device)
                            {
                                device.Remove();
                                Program.InputDevices.Remove(device);
                                this.TrackedDevices[eventInfo.trackedDeviceIndex] = null;
                            }
                            continue;
                        }
                        default:
                        {
                            if (this.TrackedDevices[eventInfo.trackedDeviceIndex] is TrackedDevice device)
                            {
                                device.HandleVREvent(eventInfo, eventTime - eventInfo.eventAgeSeconds);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[WARNING]: OpenVR sent event {eventInfo.eventType} to unknown device {eventInfo.trackedDeviceIndex}! Ignoring.");
                            }
                            break;
                        }
                    }
                }
            }

            // Update the pose of tracked devices
            /*Span<TrackedDevicePose_t> devicePoses = stackalloc TrackedDevicePose_t[(int)OpenVR.OpenVR.k_unMaxTrackedDeviceCount];
            
            OpenVR.OpenVR.System.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseSeated, 0.0f, devicePoses);
            double time = Program.Application!.ApplicationTime;
            for (int i = 0; i < this.TrackedDevices.Length; i++)
            {
                if (this.TrackedDevices[i] is TrackedDevice device)
                {
                    device.TrackedTransform.ChangeValue(devicePoses[i].mDeviceToAbsoluteTracking, time, true);
                }
            }*/
            OpenVR.OpenVR.Compositor.WaitGetPoses(Headset._deviceRenderPoses, Headset._deviceGamePoses);
            double time = Program.Application!.ApplicationTime;
            for (int i = 0; i < this.TrackedDevices.Length; i++)
            {
                if (this.TrackedDevices[i] is TrackedDevice device)
                {
                    device.UpdateTransform(Headset._deviceRenderPoses[i].mDeviceToAbsoluteTracking, time);
                }
                // Also update the axes of the controllers
                if (this.TrackedDevices[i] is MotionController controller)
                {
                    controller.PollAxes();
                }
            }
        }

        private void RefreshDeviceSlot(uint deviceIndex)
        {
            if (this.TrackedDevices[deviceIndex] is TrackedDevice device)
            {
                device.Remove();
                Program.InputDevices.Remove(device);
                this.TrackedDevices[deviceIndex] = null;
            }

            switch (OpenVR.OpenVR.System.GetTrackedDeviceClass(deviceIndex))
            {
                case ETrackedDeviceClass.HMD:
                    if (deviceIndex == OpenVR.OpenVR.k_unTrackedDeviceIndex_Hmd)
                    {
                        this.TrackedDevices[deviceIndex] = this;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[WARNING]: OpenVR reported a VR heaset with non-zero device index {deviceIndex}! Ignoring.");
                    }
                    break;
                case ETrackedDeviceClass.Controller:
                    ETrackedControllerRole controllerRole = OpenVR.OpenVR.System.GetControllerRoleForTrackedDeviceIndex(deviceIndex);
                    if (controllerRole == ETrackedControllerRole.LeftHand || controllerRole == ETrackedControllerRole.RightHand)
                    {
                        MotionController controller = new MotionController(deviceIndex, controllerRole == ETrackedControllerRole.LeftHand ? MotionControllerHand.Left : MotionControllerHand.Right);
                        this.TrackedDevices[deviceIndex] = controller;
                        Program.InputDevices.Add(controller);
                    }
                    break;
                case ETrackedDeviceClass.GenericTracker:
                    TrackedDevice trackedDevice = new TrackedDevice(deviceIndex);
                    this.TrackedDevices[deviceIndex] = trackedDevice;
                    Program.InputDevices.Add(trackedDevice);
                    break;
            }
        }

        public unsafe Graphics.StaticModel LoadRenderModel(string modelName)
        {
            EVRRenderModelError error = EVRRenderModelError.Loading;

            IntPtr modelPointer = IntPtr.Zero;
            while (error == EVRRenderModelError.Loading)
            {
                error = OpenVR.OpenVR.RenderModels.LoadRenderModel_Async(modelName, ref modelPointer);
            }

            if (error == EVRRenderModelError.None)
            {
                RenderModel_t model = *(RenderModel_t*)modelPointer;
                Span<RenderModel_Vertex_t> vertices = new Span<RenderModel_Vertex_t>((void*)model.rVertexData, (int)model.unVertexCount);
                Span<ushort> indices = new Span<ushort>((void*)model.rIndexData, (int)model.unTriangleCount * 3);

                Graphics.StaticModelVertex[] modelVertices = new Graphics.StaticModelVertex[model.unVertexCount];
                uint[] modelIndices = new uint[model.unTriangleCount * 3];
                for (int i = 0; i < model.unVertexCount; i++)
                {
                    modelVertices[i].Position = vertices[i].vPosition;
                    modelVertices[i].Normal = vertices[i].vNormal;
                    modelVertices[i].TexCoord = new Vector2(vertices[i].rfTextureCoord0, vertices[i].rfTextureCoord1);
                }
                for (int i = 0; i < model.unTriangleCount * 3; i++)
                {
                    modelIndices[i] = indices[i];
                }

                Graphics.StaticModelSection section = new Graphics.StaticModelSection(new Graphics.Mesh<Graphics.StaticModelVertex>(Program.Renderer!.Device, modelVertices, modelIndices), new Graphics.Material(null, Vector3.One));

                OpenVR.OpenVR.RenderModels.FreeRenderModel(modelPointer);

                return new Graphics.StaticModel(new Graphics.StaticModelSection[] { section });
            }
            else
            {
                throw new Exception($"Model load error is {error}!");
            }
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

            return new Headset();
        }
    }
}
