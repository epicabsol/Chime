using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Chime.Scene
{
    public abstract class Camera : SceneObject
    {
        public abstract Matrix4x4 ProjectionMatrix { get; }

        public Camera(string? name = null) : base(name)
        {

        }
    }

    public class PerspectiveCamera : Camera
    {
        public float FOV { get; set; }
        public float AspectRatio { get; set; }
        public float NearClip { get; set; }
        public float FarClip { get; set; }

        public override Matrix4x4 ProjectionMatrix => Matrix4x4.CreatePerspectiveFieldOfView(this.FOV, this.AspectRatio, this.NearClip, this.FarClip);

        public PerspectiveCamera(float fov, float aspectRatio, float nearClip, float farClip, string? name = null) : base(name)
        {
            this.FOV = fov;
            this.AspectRatio = aspectRatio;
            this.NearClip = nearClip;
            this.FarClip = farClip;
        }
    }

    public class VREyeCamera : Camera
    {
        public Platform.Headset.Eye Eye { get; }
        public float NearClip { get; }
        public float FarClip { get; }
        private Matrix4x4 _projectionMatrix;
        public override Matrix4x4 ProjectionMatrix => this._projectionMatrix;

        public VREyeCamera(Platform.Headset.Eye eye, float nearClip, float farClip, string? name = null) : base(name)
        {
            if (Program.Headset == null)
                throw new Exception();
            
            this.Eye = eye;
            this.NearClip = nearClip;
            this.FarClip = farClip;

            Program.Headset.GetEyeProjection(this.Eye, this.NearClip, this.FarClip, out this._projectionMatrix);
            Vector3 position;
            Quaternion rotation;
            Program.Headset.GetEyeTransform(this.Eye, out position, out rotation);
            this.Position = position;
            this.Rotation = rotation;
        }
    }
}
