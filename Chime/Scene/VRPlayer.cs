using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Chime.Scene
{
    public class VRPlayer : SceneObject
    {
        public Platform.Headset Headset { get; }
        public SceneObject HeadsetObject { get; }
        public VREyeCamera LeftEye { get; }
        public VREyeCamera RightEye { get; }

        public VRPlayerController? LeftController { get; }
        public VRPlayerController? RightController { get; }
        public IReadOnlyList<VRPlayerController> Controllers { get; }

        public VRPlayer(Platform.Headset headset, string? name = null, Vector3? relativeTranslation = null, Quaternion? relativeRotation = null, Vector3? relativeScale = null) : base(name, relativeTranslation, relativeRotation, relativeScale)
        {
            this.Headset = headset;
            this.HeadsetObject = new SceneObject("VR Headset");
            this.AddChild(this.HeadsetObject);

            // Set up eyes for the headset
            this.LeftEye = new VREyeCamera(Platform.Headset.Eye.Left, 0.1f, 1000.0f, "LeftEye");
            this.HeadsetObject.AddChild(this.LeftEye);
            this.RightEye = new VREyeCamera(Platform.Headset.Eye.Right, 0.1f, 1000.0f, "RightEye");
            this.HeadsetObject.AddChild(this.RightEye);

            // Set up motion controllers
            List<VRPlayerController> controllers = new List<VRPlayerController>();
            foreach (Platform.TrackedDevice device in headset.ConnectedDevices)
            {
                if (device is Platform.MotionController controllerDevice)
                {
                    VRPlayerController controller = new VRPlayerController(controllerDevice, $"{controllerDevice.Hand} Motion Controller");

                    // TEMP: Toss a grid so we can see the controller
                    Grid grid = new Grid(false);
                    grid.RelativeScale = new Vector3(0.1f, 0.1f, 0.1f);
                    controller.AddChild(grid);

                    if (controller.Hand == Platform.MotionControllerHand.Left)
                        this.LeftController = controller;
                    else
                        this.RightController = controller;
                    controllers.Add(controller);
                    this.AddChild(controller);
                }
            }
            this.Controllers = controllers.AsReadOnly();
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            Matrix4x4.Decompose(Headset.TrackedTransform.Value, out _, out Quaternion rotation, out Vector3 translation);
            this.HeadsetObject.RelativeTranslation = translation;
            this.HeadsetObject.RelativeRotation = rotation;
        }
    }
}
