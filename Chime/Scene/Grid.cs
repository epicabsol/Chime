using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Chime.Scene
{
    public class Grid : SceneObject
    {
        private Graphics.Mesh<Graphics.SolidColorVertex> Mesh { get; }

        public Grid(bool grid, string? name = null) : base(name)
        {
            if (Program.Renderer == null)
                throw new Exception();

            Vector4 gridColor = new Vector4(0.3f, 0.3f, 0.3f, 0.8f);
            List<Graphics.SolidColorVertex> vertices = new List<Graphics.SolidColorVertex>
            {
                new Graphics.SolidColorVertex() { Position = Vector3.Zero, Color = new Vector4(1.0f, 0.0f, 0.0f, 1.0f) },
                new Graphics.SolidColorVertex() { Position = Vector3.UnitX, Color = new Vector4(1.0f, 0.0f, 0.0f, 1.0f) },
                new Graphics.SolidColorVertex() { Position = Vector3.Zero, Color = new Vector4(0.0f, 1.0f, 0.0f, 1.0f) },
                new Graphics.SolidColorVertex() { Position = Vector3.UnitY, Color = new Vector4(0.0f, 1.0f, 0.0f, 1.0f) },
                new Graphics.SolidColorVertex() { Position = Vector3.Zero, Color = new Vector4(0.0f, 0.0f, 1.0f, 1.0f) },
                new Graphics.SolidColorVertex() { Position = Vector3.UnitZ, Color = new Vector4(0.0f, 0.0f, 1.0f, 1.0f) },
            };
            List<uint> indices = new List<uint>
            {
                0, 1,
                2, 3,
                4, 5,
            };
            if (grid)
            {
                for (int x = -10; x <= 10; x++)
                {
                    indices.Add((uint)vertices.Count);
                    vertices.Add(new Graphics.SolidColorVertex() { Position = new Vector3(x, 0.0f, 10.0f), Color = gridColor });
                    indices.Add((uint)vertices.Count);
                    vertices.Add(new Graphics.SolidColorVertex() { Position = new Vector3(x, 0.0f, -10.0f), Color = gridColor });
                }
                for (int z = -10; z <= 10; z++)
                {
                    indices.Add((uint)vertices.Count);
                    vertices.Add(new Graphics.SolidColorVertex() { Position = new Vector3(10.0f, 0.0f, z), Color = gridColor });
                    indices.Add((uint)vertices.Count);
                    vertices.Add(new Graphics.SolidColorVertex() { Position = new Vector3(-10.0f, 0.0f, z), Color = gridColor });
                }

            }
            this.Mesh = new Graphics.Mesh<Graphics.SolidColorVertex>(Program.Renderer.Device, vertices.ToArray(), indices.ToArray());
        }

        public override void Draw(ObjectDrawContext context)
        {
            base.Draw(context);

            if (context.RenderPass == SceneRenderPass.Overlays)
            {
                context.Pipeline.DrawLineMesh(this.Mesh, this.AbsoluteTransform);
            }
        }

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (disposing)
            {
                this.Mesh.Dispose();
            }
        }
    }
}
