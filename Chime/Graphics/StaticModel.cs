using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

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
            Dictionary<SharpGLTF.Schema2.Material, Material> materials = new Dictionary<SharpGLTF.Schema2.Material, Material>();
            foreach (SharpGLTF.Schema2.Mesh mesh in root.LogicalMeshes)
            {
                foreach (SharpGLTF.Schema2.MeshPrimitive primitive in mesh.Primitives)
                {
                    uint[] indices = primitive.GetIndices().ToArray();
                    IList<Vector3> positions = primitive.GetVertices("POSITION").AsVector3Array();
                    IList<Vector3> normals = primitive.GetVertices("NORMAL").AsVector3Array();
                    IList<Vector2> texcoords = primitive.GetVertexAccessor("TEXCOORD_0").AsVector2Array();
                    IList<Vector4>? tangents = primitive.GetVertexAccessor("TANGENT")?.AsVector4Array();

                    StaticModelVertex[] vertices = new StaticModelVertex[positions.Count];
                    if (primitive.DrawPrimitiveType != SharpGLTF.Schema2.PrimitiveType.TRIANGLES)
                        throw new Exception($"Unsupported primitive type {primitive.DrawPrimitiveType} in GLTF mesh!");

                    for (int i = 0; i < positions.Count; i++)
                    {
                        vertices[i].Position = positions[i];
                        vertices[i].Normal = normals[i];
                        vertices[i].TexCoord = texcoords[i];
                        //vertices[i].Tangent = tangents[i];
                    }
                    if (tangents != null)
                    {
                        for (int i = 0; i < positions.Count; i++)
                        {
                            vertices[i].Tangent = tangents[i];
                        }
                    }
                    else
                    {
                        StaticModel.ComputeTangents(vertices, indices);
                    }

                    Material? material = materials.GetValueOrDefault(primitive.Material);
                    if (material == null)
                    {
                        SharpDX.Direct3D11.ShaderResourceView? diffuseTexture = null;
                        SharpDX.Direct3D11.ShaderResourceView? normalTexture = null;
                        SharpDX.Direct3D11.ShaderResourceView? metallicRoughnessTexture = null;
                        foreach (SharpGLTF.Schema2.MaterialChannel channel in primitive.Material.Channels)
                        {
                            if (channel.Texture == null)
                                continue;

                            //BaseColor MetallicRoughness Normal Occlusion Emissive
                            if (channel.Key == "BaseColor")
                            {
                                diffuseTexture = StaticModel.LoadGLTFTexture(channel.Texture);
                            }
                            else if (channel.Key == "Normal")
                            {
                                normalTexture = StaticModel.LoadGLTFTexture(channel.Texture);
                            }
                            else if (channel.Key == "MetallicRoughness")
                            {
                                metallicRoughnessTexture = StaticModel.LoadGLTFTexture(channel.Texture);
                            }
                        }
                        material = new Material(diffuseTexture, Vector3.One, normalTexture, metallicRoughnessTexture);
                        materials[primitive.Material] = material;
                    }

                    sections.Add(new StaticModelSection(new Mesh<StaticModelVertex>(Program.Renderer!.Device, vertices, indices), material));
                }
            }
            return new StaticModel(sections);
        }

        private static unsafe SharpDX.Direct3D11.ShaderResourceView LoadGLTFTexture(SharpGLTF.Schema2.Texture texture)
        {
            var finish = (int width, int height, SharpDX.DXGI.Format format, IntPtr data) =>
            {
                SharpDX.Direct3D11.Texture2D texture = new SharpDX.Direct3D11.Texture2D(Program.Renderer!.Device, new SharpDX.Direct3D11.Texture2DDescription() { ArraySize = 1, BindFlags = SharpDX.Direct3D11.BindFlags.ShaderResource, CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None, Format = format, Width = width, Height = height, MipLevels = 1, OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None, SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0), Usage = SharpDX.Direct3D11.ResourceUsage.Immutable }, new SharpDX.DataRectangle(data, width * SharpDX.DXGI.FormatHelper.SizeOfInBits(format) / 8));
                return new SharpDX.Direct3D11.ShaderResourceView(Program.Renderer!.Device, texture);
            };

            using (Stream imageStream = texture.PrimaryImage.Content.Open())
            {
                Span<byte> pixels = null;
                SharpDX.DXGI.Format pixelFormat = SharpDX.DXGI.Format.Unknown;

                if (texture.PrimaryImage.Content.IsPng || texture.PrimaryImage.Content.IsJpg)
                {
                    using (SixLabors.ImageSharp.Image image = SixLabors.ImageSharp.Image.Load(imageStream))
                    {
                        if (image is SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> imageBgra)
                        {
                            SixLabors.ImageSharp.PixelFormats.Rgba32[] pixelData = new SixLabors.ImageSharp.PixelFormats.Rgba32[image.Width * image.Height];
                            imageBgra.CopyPixelDataTo(pixelData);
                            fixed (void* pixelDataPointer = pixelData)
                            {
                                return finish(image.Width, image.Height, SharpDX.DXGI.Format.R8G8B8A8_UNorm, (IntPtr)pixelDataPointer);
                            }
                        }
                        else if (image is SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgb24> imageRgb)
                        {
                            SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> resultImage = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(image.Width, image.Height);

                            // Process the 3-byte RGB data to 4-byte RGBA data, because GPUs do not support 24-bit texture formats
                            imageRgb.ProcessPixelRows(resultImage, (accessor1, accessor2) =>
                            {
                                for (int y = 0; y < accessor1.Height; y++)
                                {
                                    Span<Rgb24> inSpan = accessor1.GetRowSpan(y);
                                    Span<Rgba32> outSpan = accessor2.GetRowSpan(y);
                                    for (int x = 0; x < accessor1.Width; x++)
                                    {
                                        outSpan[x].FromRgb24(inSpan[x]);
                                    }
                                }
                            });

                            SixLabors.ImageSharp.PixelFormats.Rgba32[] pixelData = new SixLabors.ImageSharp.PixelFormats.Rgba32[image.Width * image.Height];
                            resultImage.CopyPixelDataTo(pixelData);
                            fixed (void* pixelDataPointer = pixelData)
                            {
                                return finish(image.Width, image.Height, SharpDX.DXGI.Format.R8G8B8A8_UNorm, (IntPtr)pixelDataPointer);
                            }
                        }
                        else
                        {
                            throw new Exception("Unsupported pixel format in PNG image.");
                        }
                    }
                }
                else
                {
                    throw new Exception($"Unsupported image type {texture.PrimaryImage.Content.MimeType}!");
                }
            }
        }
    
        public static void ComputeTangents(IList<StaticModelVertex> vertices, uint[] indices)
        {
            Vector3[] tangents = new Vector3[vertices.Count];
            Vector3[] bitangents = new Vector3[vertices.Count];

            // Algorithm from https://marti.works/posts/post-calculating-tangents-for-your-mesh/post/
            for (int t = 0; t < indices.Length / 3; t++)
            {
                int i0 = (int)indices[t * 3];
                int i1 = (int)indices[t * 3 + 1];
                int i2 = (int)indices[t * 3 + 2];

                Vector3 pos0 = vertices[i0].Position;
                Vector3 pos1 = vertices[i1].Position;
                Vector3 pos2 = vertices[i2].Position;

                Vector2 uv0 = vertices[i0].TexCoord;
                Vector2 uv1 = vertices[i1].TexCoord;
                Vector2 uv2 = vertices[i2].TexCoord;

                Vector3 edge1 = pos1 - pos0;
                Vector3 edge2 = pos2 - pos0;

                Vector2 uvEdge1 = uv1 - uv0;
                Vector2 uvEdge2 = uv2 - uv0;

                float r = 1.0f / (uvEdge1.X * uvEdge2.Y - uvEdge1.Y * uvEdge2.X);

                Vector3 tangent = new Vector3(((edge1.X * uvEdge2.Y) - (edge2.X * uvEdge1.Y)) * r, ((edge1.Y * uvEdge2.Y) - (edge2.Y * uvEdge1.Y)) * r, ((edge1.Z * uvEdge2.Y) - (edge2.Z * uvEdge1.Y)) * r);
                tangents[i0] += tangent;
                tangents[i1] += tangent;
                tangents[i2] += tangent;

                Vector3 bitangent = new Vector3(((edge1.X * uvEdge2.X) - (edge2.X * uvEdge1.X)) * r, ((edge1.Y * uvEdge2.X) - (edge2.Y * uvEdge1.X)) * r, ((edge1.Z * uvEdge2.X) - (edge2.Z * uvEdge1.X)) * r);
                bitangents[i0] += bitangent;
                bitangents[i1] += bitangent;
                bitangents[i2] += bitangent;
            }

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 tangent = Vector3.Normalize(tangents[i] - (vertices[i].Normal * Vector3.Dot(vertices[i].Normal, tangents[i])));

                Vector3 cross = Vector3.Cross(vertices[i].Normal, tangents[i]);

                vertices[i] = new StaticModelVertex() { Position = vertices[i].Position, Normal = vertices[i].Normal, TexCoord = vertices[i].TexCoord, Tangent = new Vector4(tangent, (Vector3.Dot(cross, bitangents[i]) < 0.0f) ? -1.0f : 1.0f) };
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
