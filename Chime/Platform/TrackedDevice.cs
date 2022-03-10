using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Chime.Input;

namespace Chime.Platform
{
    public class TrackedDevice : Input.InputDevice
    {
        public uint VRDeviceIndex { get; }

        public override string DisplayName => "Tracked Device";

        private bool _isConnected = true;
        public override bool IsConnected => this._isConnected;

        public InputElement<Matrix4x4> TrackedTransform { get; }

        protected TrackedDevice(uint vrDeviceIndex, List<InputElement<bool>> actionElements, List<InputElement<float>> axisElements, List<InputElement<Matrix4x4>> transformElements) : base(actionElements, axisElements, transformElements)
        {
            this.VRDeviceIndex = vrDeviceIndex;

            this.TrackedTransform = new InputElement<Matrix4x4>("Transform");
            transformElements.Add(this.TrackedTransform);

            this._isConnected = OpenVR.OpenVR.System.IsTrackedDeviceConnected(this.VRDeviceIndex);

            //this.DumpProperties();
        }

        private void DumpProperties()
        {
            System.Diagnostics.Debug.WriteLine($"Tracked Device {this.VRDeviceIndex} Properties:");

            OpenVR.ETrackedPropertyError error = OpenVR.ETrackedPropertyError.TrackedProp_Success;
            StringBuilder stringBuilder = new StringBuilder(1000);
            foreach (OpenVR.ETrackedDeviceProperty property in Enum.GetValues<OpenVR.ETrackedDeviceProperty>())
            {
                bool boolValue = OpenVR.OpenVR.System.GetBoolTrackedDeviceProperty(this.VRDeviceIndex, property, ref error);
                if (error == OpenVR.ETrackedPropertyError.TrackedProp_Success)
                {
                    System.Diagnostics.Debug.WriteLine($"   bool {property} = {boolValue}");
                    continue;
                }
                else if (error != OpenVR.ETrackedPropertyError.TrackedProp_WrongDataType && error != OpenVR.ETrackedPropertyError.TrackedProp_BufferTooSmall)
                {
                    continue;
                }

                float floatValue = OpenVR.OpenVR.System.GetFloatTrackedDeviceProperty(this.VRDeviceIndex, property, ref error);
                if (error == OpenVR.ETrackedPropertyError.TrackedProp_Success)
                {
                    System.Diagnostics.Debug.WriteLine($"   float {property} = {floatValue}");
                    continue;
                }

                int intValue = OpenVR.OpenVR.System.GetInt32TrackedDeviceProperty(this.VRDeviceIndex, property, ref error);
                if (error == OpenVR.ETrackedPropertyError.TrackedProp_Success)
                {
                    System.Diagnostics.Debug.WriteLine($"   int {property} = {intValue}");
                    continue;
                }

                ulong longValue = OpenVR.OpenVR.System.GetUint64TrackedDeviceProperty(this.VRDeviceIndex, property, ref error);
                if (error == OpenVR.ETrackedPropertyError.TrackedProp_Success)
                {
                    System.Diagnostics.Debug.WriteLine($"   ulong {property} = {longValue}");
                    continue;
                }

                OpenVR.HmdMatrix34_t matrixValue = OpenVR.OpenVR.System.GetMatrix34TrackedDeviceProperty(this.VRDeviceIndex, property, ref error);
                if (error == OpenVR.ETrackedPropertyError.TrackedProp_Success)
                {
                    System.Numerics.Matrix4x4.Decompose(matrixValue, out Vector3 scale, out Quaternion rotation, out Vector3 translation);
                    System.Diagnostics.Debug.WriteLine($"   matrix {property} = translation: {translation}, scale: {scale}, rotation: {rotation}");
                    continue;
                }

                uint stringLength = OpenVR.OpenVR.System.GetStringTrackedDeviceProperty(this.VRDeviceIndex, property, stringBuilder, (uint)stringBuilder.Capacity, ref error);
                if (error == OpenVR.ETrackedPropertyError.TrackedProp_Success)
                {
                    System.Diagnostics.Debug.WriteLine($"   string {property} = {stringBuilder}");
                    continue;
                }

            }
        }

        public TrackedDevice(uint vrDeviceIndex) : this(vrDeviceIndex, new List<InputElement<bool>>(), new List<InputElement<float>>(), new List<InputElement<Matrix4x4>>())
        {

        }

        public string? GetStringProperty(OpenVR.ETrackedDeviceProperty property)
        {
            StringBuilder builder = new StringBuilder(1000);
            OpenVR.ETrackedPropertyError error = OpenVR.ETrackedPropertyError.TrackedProp_Success;
            uint stringLength = OpenVR.OpenVR.System.GetStringTrackedDeviceProperty(this.VRDeviceIndex, property, builder, (uint)builder.Capacity, ref error);
            if (error == OpenVR.ETrackedPropertyError.TrackedProp_Success)
            {
                return builder.ToString();
            }
            else if (error == OpenVR.ETrackedPropertyError.TrackedProp_ValueNotProvidedByDevice)
            {
                return null;
            }
            else if (error == OpenVR.ETrackedPropertyError.TrackedProp_BufferTooSmall)
            {
                builder.EnsureCapacity((int)stringLength);
                stringLength = OpenVR.OpenVR.System.GetStringTrackedDeviceProperty(this.VRDeviceIndex, property, builder, (uint)builder.Capacity, ref error);
                if (error == OpenVR.ETrackedPropertyError.TrackedProp_Success)
                {
                    return builder.ToString();
                }
                else
                {
                    throw new Exception($"Failed to get property {property} because {error}!");
                }
            }
            else
            {
                throw new Exception($"Failed to get property {property} because {error}!");
            }
        }

        public void UpdateTransform(Matrix4x4 newTransform, double applicationTime)
        {
            this.QueueTransformEvent(this.TrackedTransform, newTransform, applicationTime);
        }

        public virtual void HandleVREvent(OpenVR.VREvent_t vrEvent, double applicationTime)
        {
            // Nothing to do here
            System.Diagnostics.Debug.WriteLine($"[WARNING]: VR event unhandled by tracked device {this.DisplayName}!");
            // TODO: Is there an event that indicates that we should check IsTrackedDeviceConnected again?
        }
    }
}
