using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chime.Scene
{
    public class Prop : PhysicsObject
    {
        public Graphics.StaticModel Model { get; }

        public Prop(Graphics.StaticModel model, BulletSharp.CollisionShape collisionShape, float mass, string? name = null) : base(collisionShape, mass, name)
        {
            this.Model = model;
            this.RigidBody.Restitution = 0.5f;
        }

        public override void Draw(ObjectDrawContext context)
        {
            base.Draw(context);

            if (context.RenderPass == SceneRenderPass.GBuffer)
            {
                context.Pipeline.DrawStaticModel(this.Model, this.AbsoluteTransform);
            }
        }
    }
}
