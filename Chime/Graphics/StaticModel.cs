using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Chime.Graphics
{
    public class StaticModel : IDisposable
    {
        public IReadOnlyList<StaticModelSection> Sections { get; }
        public Vector3 BoundsMin { get; }
        public Vector3 BoundsMax { get; }

        public StaticModel(IEnumerable<StaticModelSection> sections)
        {
            this.Sections = new List<StaticModelSection>(sections).AsReadOnly();

            if (this.Sections.Count > 0)
            {
                this.BoundsMin = this.Sections[0].Mesh.Vertices[0].Position;
                this.BoundsMax = this.BoundsMin;

                foreach (StaticModelSection section in this.Sections)
                {
                    for (int i = 0; i < section.Mesh.VertexCount; i++)
                    {
                        this.BoundsMin = Vector3.Min(this.BoundsMin, section.Mesh.Vertices[i].Position);
                        this.BoundsMax = Vector3.Max(this.BoundsMax, section.Mesh.Vertices[i].Position);
                    }
                }
            }
        }

        public void Dispose()
        {
            foreach (StaticModelSection section in this.Sections)
            {
                section.Dispose();
            }
        }
    }

    public class StaticModelSection : IDisposable
    {
        public Mesh<StaticModelVertex> Mesh { get; }
        public Material Material { get; }

        public StaticModelSection(Mesh<StaticModelVertex> mesh, Material material)
        {
            this.Mesh = mesh;
            this.Material = material;
        }

        public void Dispose()
        {
            this.Mesh.Dispose();
        }
    }
}
