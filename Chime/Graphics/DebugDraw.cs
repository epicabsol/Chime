using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using SharpDX.Direct3D11;
using BulletSharp;

namespace Chime.Graphics
{
    public struct DebugLine
    {
        public Vector3 Start;
        public Vector3 End;
        public Vector4 Color;
    }

    public class DebugDraw : BulletSharp.DebugDraw, IDisposable
    {
        private class LineBuffer : IDisposable
        {
            public const int DefaultCapacity = 256;
            private const int VerticesPerLine = 2;

            public SharpDX.Direct3D11.Buffer Vertices { get; }
            public int Count { get; private set; } = 0;
            private int Capacity { get; }
            private IntPtr DataAddress { get; set; }

            public LineBuffer(int capacity = LineBuffer.DefaultCapacity)
            {
                this.Capacity = capacity;

                this.Vertices = new SharpDX.Direct3D11.Buffer(Program.Renderer!.Device, System.Runtime.InteropServices.Marshal.SizeOf<SolidColorVertex>() * LineBuffer.VerticesPerLine * this.Capacity, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
                this.DataAddress = Program.Renderer!.ImmediateContext.MapSubresource(this.Vertices, 0, MapMode.WriteDiscard, MapFlags.None).DataPointer;
            }

            public unsafe int AppendLines(Span<DebugLine> lines)
            {
                int count = this.Capacity - this.Count;
                if (count > lines.Length)
                    count = lines.Length;

                Span<SolidColorVertex> vertices = new Span<SolidColorVertex>((void*)this.DataAddress, Capacity * LineBuffer.VerticesPerLine);
                for (int i = 0; i < count; i++)
                {
                    vertices[(this.Count + i) * LineBuffer.VerticesPerLine] = new SolidColorVertex() { Position = lines[i].Start, Color = lines[i].Color };
                    vertices[(this.Count + i) * LineBuffer.VerticesPerLine + 1] = new SolidColorVertex() { Position = lines[i].End, Color = lines[i].Color };
                }

                this.Count += count;
                return count;
            }

            public void Commit()
            {
                Program.Renderer!.ImmediateContext.UnmapSubresource(this.Vertices, 0);
            }

            public void Reset()
            {
                this.Count = 0;
                this.DataAddress = Program.Renderer!.ImmediateContext.MapSubresource(this.Vertices, 0, MapMode.WriteDiscard, MapFlags.None).DataPointer;
            }

            public void Dispose()
            {
                Program.Renderer!.ImmediateContext.UnmapSubresource(this.Vertices, 0);
                this.DataAddress = IntPtr.Zero;
                this.Vertices.Dispose();
            }
        }

        private List<LineBuffer> Buffers { get; } = new List<LineBuffer>();

        private int CurrentBuffer { get; set; } = 0;
        public override DebugDrawModes DebugMode { get => DebugDrawModes.All; set => throw new NotImplementedException(); }
        public bool IsCommitted { get; private set; } = false;

        public DebugDraw()
        {
            this.Buffers.Add(new LineBuffer());
        }

        public unsafe void DrawLine(Vector3 start, Vector3 end, Vector4 color)
        {
            Span<DebugLine> lines = stackalloc DebugLine[] { new DebugLine() { Start = start, End = end, Color = color } };
            this.DrawLines(lines);
        }

        public void DrawLines(Span<DebugLine> lines)
        {
            int startIndex = 0;
            while (startIndex < lines.Length)
            {
                int drawn = this.Buffers[this.CurrentBuffer].AppendLines(lines.Slice(startIndex));
                startIndex += drawn;
                if (startIndex < lines.Length)
                {
                    this.CurrentBuffer += 1;
                    if (this.CurrentBuffer == this.Buffers.Count)
                    {
                        this.Buffers.Add(new LineBuffer());
                    }
                    else
                    {
                        this.Buffers[this.CurrentBuffer].Reset();
                    }
                }
            }
        }

        public void Commit()
        {
            if (!this.IsCommitted)
            {
                for (int i = 0; i <= this.CurrentBuffer; i++)
                {
                    this.Buffers[i].Commit();
                }
                this.IsCommitted = true;
            }
        }

        public void Draw(Scene.DrawContext drawContext)
        {
            if (!this.IsCommitted)
                throw new Exception("DebugDraw must be committed to draw!");

            for (int i = 0; i <= this.CurrentBuffer; i++)
            {
                if (this.Buffers[i].Count > 0)
                {
                    drawContext.Pipeline.DrawDebugLines(this.Buffers[i].Vertices, this.Buffers[i].Count);
                }
            }

        }
        
        public void Flush()
        {
            if (this.IsCommitted)
            {
                this.CurrentBuffer = 0;
                this.Buffers[0].Reset();
                this.IsCommitted = false;
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            foreach (LineBuffer buffer in Buffers)
            {
                buffer.Dispose();
            }
            this.Buffers.Clear();
        }

        public override void DrawLine(ref Vector3 from, ref Vector3 to, ref Vector3 color)
        {
            this.DrawLine(from, to, new Vector4(color, 1.0f));
        }

        public override void Draw3DText(ref Vector3 location, string textString)
        {
            throw new NotImplementedException();
        }

        public override void ReportErrorWarning(string warningString)
        {
            System.Diagnostics.Debug.WriteLine($"Physics warning: {warningString}");
        }
    }
}
