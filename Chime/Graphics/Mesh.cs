using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace Chime.Graphics
{
    public class Mesh<TVertex> : IDisposable where TVertex : unmanaged
    {
        public SharpDX.Direct3D11.Buffer VertexBuffer { get; }
        public uint VertexCount { get; }
        public SharpDX.Direct3D11.Buffer IndexBuffer { get; }
        public uint IndexCount { get; }

        public unsafe Mesh(Device device, Span<TVertex> vertices, Span<uint> indices)
        {
            this.VertexCount = (uint)vertices.Length;
            this.IndexCount = (uint)indices.Length;
            fixed (TVertex* vertexPointer = vertices)
            fixed (uint* indexPointer = indices)
            {
                this.VertexBuffer = new SharpDX.Direct3D11.Buffer(device, (IntPtr)vertexPointer, new BufferDescription(System.Runtime.InteropServices.Marshal.SizeOf<TVertex>() * vertices.Length, BindFlags.VertexBuffer, ResourceUsage.Immutable));
                this.IndexBuffer = new SharpDX.Direct3D11.Buffer(device, (IntPtr)indexPointer, new BufferDescription(sizeof(uint) * indices.Length, BindFlags.IndexBuffer, ResourceUsage.Immutable));
            }
        }

        public void Dispose()
        {
            this.IndexBuffer.Dispose();
            this.VertexBuffer.Dispose();
        }
    }
}
