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
        public Graphics.StaticModel HandModel { get; }

        public VRPlayerController(Platform.MotionController controller, string? name) : base(name)
        {
            this.Controller = controller;

            this.HandModel = Graphics.StaticModel.FromGLTF(this.Hand == Platform.MotionControllerHand.Left ? Chime.Properties.Resources.MinifigHandLeft : Chime.Properties.Resources.MinifigHandRight);
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

            if (this.Controller.Model != null && context.RenderPass == SceneRenderPass.GBuffer)
            {
                Matrix4x4 absoluteTransform = this.AbsoluteTransform;
                if (this.Controller.Model.Components.Count == 0)
                {
                    context.Pipeline.DrawStaticModel(this.Controller.Model.BaseModel, absoluteTransform);
                }
                foreach (string component in this.Controller.Model.Components.Keys)
                {
                    if (this.Controller.Model.Components[component] is Graphics.StaticModel model)
                    {
                        context.Pipeline.DrawStaticModel(model, this.Controller.ComponentTransforms[component] * absoluteTransform);
                    }
                }

                context.Pipeline.DrawStaticModel(this.HandModel, absoluteTransform);
            }
        }
    }
}
