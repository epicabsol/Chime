using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chime.Scene
{
    public class Prop : SceneObject
    {
        public Graphics.StaticModel Model { get; }

        public Prop(Graphics.StaticModel model, string? name = null) : base(name)
        {
            this.Model = model;
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
