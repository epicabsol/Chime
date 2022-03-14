using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Chime.Scene
{
    public class PointLight : SceneObject
    {
        public Vector3 Color { get; set; }

        public PointLight(Vector3 color, string? name = null) : base(name)
        {
            this.Color = color;

            this.AddChild(new Grid(false));
        }

        public override void Draw(ObjectDrawContext context)
        {
            base.Draw(context);

            if (context.RenderPass == SceneRenderPass.Lighting)
            {
                context.Pipeline.DrawPointLight(this.Color, this.AbsoluteTransform.Translation);
            }
        }
    }
}
