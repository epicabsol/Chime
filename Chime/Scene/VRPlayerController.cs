using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Chime.Scene
{
    public class VRPlayerController : SceneObject
    {
        public Platform.MotionController Controller { get; }
        public Platform.MotionControllerHand Hand => this.Controller.Hand;
        public Graphics.StaticModel? Model { get; }

        public VRPlayerController(Platform.MotionController controller, string? name) : base(name)
        {
            this.Controller = controller;
            this.Model = this.Controller.LoadModel();
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            Matrix4x4.Decompose(this.Controller.TrackedTransform.Value, out _, out Quaternion rotation, out Vector3 translation);
            this.Position = translation;
            this.Rotation = rotation;
        }

        public override void Draw(ObjectDrawContext context)
        {
            base.Draw(context);

            if (this.Model != null && context.RenderPass == SceneRenderPass.GBuffer)
            {
                context.Pipeline.DrawStaticModel(this.Model, this.AbsoluteTransform);
            }
        }
    }
}
