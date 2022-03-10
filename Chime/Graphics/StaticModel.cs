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

        public static StaticModel FromGLTF(byte[] gltfData)
        {
            SharpGLTF.Schema2.ModelRoot root = SharpGLTF.Schema2.ModelRoot.ParseGLB(gltfData, null);

            List<StaticModelSection> sections = new List<StaticModelSection>();
            foreach (SharpGLTF.Schema2.Mesh mesh in root.LogicalMeshes)
            {
                foreach (SharpGLTF.Schema2.MeshPrimitive primitive in mesh.Primitives)
                {
                    IList<Vector3> positions = primitive.GetVertices("POSITION").AsVector3Array();
                    IList<Vector3> normals = primitive.GetVertices("NORMAL").AsVector3Array();
                    IList<Vector2> texcoords = primitive.GetVertexAccessor("TEXCOORD_0").AsVector2Array();

                    StaticModelVertex[] vertices = new StaticModelVertex[positions.Count];
                    if (primitive.DrawPrimitiveType != SharpGLTF.Schema2.PrimitiveType.TRIANGLES)
                        throw new Exception($"Unsupported primitive type {primitive.DrawPrimitiveType} in GLTF mesh!");

                    for (int i = 0; i < positions.Count; i++)
                    {
                        vertices[i].Position = positions[i];
                        vertices[i].Normal = normals[i];
                        vertices[i].TexCoord = texcoords[i];
                    }

                    sections.Add(new StaticModelSection(new Mesh<StaticModelVertex>(Program.Renderer!.Device, vertices, primitive.GetIndices().ToArray()), new Material(null, Vector3.One)));
                }
            }
            return new StaticModel(sections);
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
