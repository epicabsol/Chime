using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Chime.Screens
{
    public class TestScreen : Screen
    {
        public TestScreen() : base()
        {
            this.Scene.AddChild(new Scene.Prop(Graphics.StaticModel.FromGLTF(Chime.Properties.Resources.SuzannePBR), new BulletSharp.SphereShape(0.75f), 1.0f, "Test Model (SuzannePBR)", new Vector3(0.0f, 3.0f, -3.0f)));
            this.Scene.AddChild(new Scene.Prop(Graphics.StaticModel.FromGLTF(Chime.Properties.Resources.DamagedHelmet), new BulletSharp.SphereShape(0.75f), 1.0f, "Test Model (DamagedHelmet)", new Vector3(0.0f, 6.5f, -1.5f), null, new Vector3(0.1f, 0.1f, 0.1f)));
            this.Scene.AddChild(new Scene.Prop(Graphics.StaticModel.FromGLTF(Chime.Properties.Resources.Chime1), new BulletSharp.CylinderShape(new Vector3(0.1f, 0.35f, 0.1f)), 1.0f, "Test Model (Chime1)", new Vector3(0.0f, 1.5f, -0.5f)));
            Chime.Scene.PointLight light = new Scene.PointLight(Vector3.One, "Test Light", new Vector3(0.0f, 20.0f, 0.0f));
            light.Color = new Vector3(2000.0f, 2000.0f, 2000.0f);
            this.Scene.AddChild(light);

            this.Scene.AddChild(new Scene.Grid(true, "Test Grid"));
        }

        public override void Update(float timestep)
        {
            base.Update(timestep);
        }

        public override void Render()
        {
            base.Render();
        }
    }
}
