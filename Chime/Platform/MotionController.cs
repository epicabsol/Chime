using Chime.Input;
using OpenVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Chime.Platform
{
    public enum MotionControllerHand
    {
        Left,
        Right,
    }

    public class MotionControllerModel
    {
        public string BaseModelName { get; }
        public Graphics.StaticModel BaseModel { get; }
        public Dictionary<string, Graphics.StaticModel?> Components { get; }

        public MotionControllerModel(string baseModelName, Graphics.StaticModel baseModel, Dictionary<string, Graphics.StaticModel?> components)
        {
            this.BaseModelName = baseModelName;
            this.BaseModel = baseModel;
            this.Components = components;
        }
    }

    public class MotionController : TrackedDevice
    {
        public override string DisplayName => $"{this.Hand} Motion Controller";

        public MotionControllerHand Hand { get; }

        public InputElement<bool>[] Buttons { get; } = new InputElement<bool>[(int)EVRButtonId.k_EButton_Max];
        public InputElement<bool>[] ButtonTouches { get; } = new InputElement<bool>[(int)EVRButtonId.k_EButton_Max];
        public InputElement<float>[] Axes { get; } = new InputElement<float>[(int)OpenVR.OpenVR.k_unControllerStateAxisCount * 2];

        public MotionControllerModel? Model { get; private set; }
        public Dictionary<string, Matrix4x4> ComponentTransforms { get; } = new Dictionary<string, Matrix4x4>();

        protected MotionController(Headset headset, uint vrDeviceIndex, MotionControllerHand hand, List<InputElement<bool>> actionElements, List<InputElement<float>> axisElements, List<InputElement<Matrix4x4>> transformElements) : base(vrDeviceIndex, actionElements, axisElements, transformElements)
        {
            this.Hand = hand;

            for (int buttonIndex = 0; buttonIndex < (int)EVRButtonId.k_EButton_Max; buttonIndex++)
            {
                InputElement<bool> element = new InputElement<bool>($"Button {buttonIndex}");
                this.Buttons[buttonIndex] = element;
                actionElements.Add(element);

                //element.ValueChanged += (sender, e) => System.Diagnostics.Debug.WriteLine($"{this.DisplayName} {e.Element.DisplayName} {(e.NewValue ? "Pressed" : "Released")}");

                InputElement<bool> touchedElement = new InputElement<bool>($"Button {buttonIndex} Touch");
                this.ButtonTouches[buttonIndex] = touchedElement;
                actionElements.Add(touchedElement);

                //touchedElement.ValueChanged += (sender, e) => System.Diagnostics.Debug.WriteLine($"{this.DisplayName} {e.Element.DisplayName} {(e.NewValue ? "Touched" : "Untouched")}");
            }

            for (int axisIndex = 0; axisIndex < (int)OpenVR.OpenVR.k_unControllerStateAxisCount; axisIndex++)
            {
                InputElement<float> xElement = new InputElement<float>($"Axis {axisIndex} X");
                this.Axes[axisIndex * 2] = xElement;
                axisElements.Add(xElement);

                /*xElement.ValueChanged += (sender, e) =>
                {
                    if (MathF.Abs(e.NewValue - e.OldValue) > 0.05f)
                    {
                        System.Diagnostics.Debug.WriteLine($"{this.DisplayName} {e.Element.DisplayName} {e.NewValue}");
                    }
                };*/

                InputElement<float> yElement = new InputElement<float>($"Axis {axisIndex} Y");
                this.Axes[axisIndex * 2 + 1] = yElement;
                axisElements.Add(yElement);

                /*yElement.ValueChanged += (sender, e) =>
                {
                    if (MathF.Abs(e.NewValue - e.OldValue) > 0.05f)
                    {
                        System.Diagnostics.Debug.WriteLine($"{this.DisplayName} {e.Element.DisplayName} {e.NewValue}");
                    }
                };*/
            }

            // Load the base model
            string? modelName = this.GetStringProperty(ETrackedDeviceProperty.Prop_RenderModelName_String);
            if (modelName == null)
            {
                this.Model = null;
                return;
            }

            // Load the component models
            Dictionary<string, Graphics.StaticModel?> components = new Dictionary<string, Graphics.StaticModel?>();

            uint componentCount = OpenVR.OpenVR.RenderModels.GetComponentCount(modelName);
            StringBuilder buffer = new StringBuilder();
            for (uint i = 0; i < componentCount; i++)
            {
                uint bufferSize = OpenVR.OpenVR.RenderModels.GetComponentName(modelName, i, null, 0);
                buffer.EnsureCapacity((int)bufferSize);
                OpenVR.OpenVR.RenderModels.GetComponentName(modelName, i, buffer, bufferSize);
                string componentName = buffer.ToString();
                buffer.Clear();

                bufferSize = OpenVR.OpenVR.RenderModels.GetComponentRenderModelName(modelName, componentName, null, 0);
                if (bufferSize > 1)
                {
                    buffer.EnsureCapacity((int)bufferSize);
                    OpenVR.OpenVR.RenderModels.GetComponentRenderModelName(modelName, componentName, buffer, bufferSize);
                    string componentRenderModelName = buffer.ToString();

                    components[componentName] = headset.LoadRenderModel(componentRenderModelName);
                    this.ComponentTransforms[componentName] = default;
                }
                else
                {
                    components[componentName] = null;
                }
            }

            this.Model = new MotionControllerModel(modelName, headset.LoadRenderModel(modelName), components);
        }

        public MotionController(Headset headset, uint vrDeviceIndex, MotionControllerHand hand) : this(headset, vrDeviceIndex, hand, new List<InputElement<bool>>(), new List<InputElement<float>>(), new List<InputElement<Matrix4x4>>())
        {

        }

        public void PollAxes()
        {
            VRControllerState_t state = default;
            double time = Program.Application!.ApplicationTime;
            if (OpenVR.OpenVR.System.GetControllerState(this.VRDeviceIndex, ref state, (uint)System.Runtime.InteropServices.Marshal.SizeOf<VRControllerState_t>()))
            {
                this.QueueAxisEvent(this.Axes[0], state.rAxis0.x, time);
                this.QueueAxisEvent(this.Axes[1], state.rAxis0.y, time);
                this.QueueAxisEvent(this.Axes[2], state.rAxis1.x, time);
                this.QueueAxisEvent(this.Axes[3], state.rAxis1.y, time);
                this.QueueAxisEvent(this.Axes[4], state.rAxis2.x, time);
                this.QueueAxisEvent(this.Axes[5], state.rAxis2.y, time);
                this.QueueAxisEvent(this.Axes[6], state.rAxis3.x, time);
                this.QueueAxisEvent(this.Axes[7], state.rAxis3.y, time);
                this.QueueAxisEvent(this.Axes[8], state.rAxis4.x, time);
                this.QueueAxisEvent(this.Axes[9], state.rAxis4.y, time);

                if (this.Model != null)
                {
                    RenderModel_ControllerMode_State_t controllerMode = default;
                    RenderModel_ComponentState_t componentState = default;
                    foreach (string component in this.Model.Components.Keys)
                    {
                        OpenVR.OpenVR.RenderModels.GetComponentState(this.Model.BaseModelName, component, ref state, ref controllerMode, ref componentState);
                        this.ComponentTransforms[component] = componentState.mTrackingToComponentRenderModel;
                    }
                }
            }
        }

        public override void HandleVREvent(VREvent_t vrEvent, double applicationTime)
        {
            switch (vrEvent.eventType)
            {
                case EVREventType.VREvent_ButtonPress:
                    this.QueueActionEvent(this.Buttons[vrEvent.data.controller.button], true, applicationTime);
                    break;
                case EVREventType.VREvent_ButtonUnpress:
                    this.QueueActionEvent(this.Buttons[vrEvent.data.controller.button], false, applicationTime);
                    break;
                case EVREventType.VREvent_ButtonTouch:
                    this.QueueActionEvent(this.ButtonTouches[vrEvent.data.controller.button], true, applicationTime);
                    break;
                case EVREventType.VREvent_ButtonUntouch:
                    this.QueueActionEvent(this.ButtonTouches[vrEvent.data.controller.button], false, applicationTime);
                    break;
                default:
                    base.HandleVREvent(vrEvent, applicationTime);
                    break;
            }
        }
    }
}
