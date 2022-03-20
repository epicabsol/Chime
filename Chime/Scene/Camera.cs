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
        public float NearClip { get; set; }
        public float FarClip { get; set; }

        public Camera(float nearClip, float farClip, string? name = null, Vector3? relativeTranslation = null, Quaternion? relativeRotation = null) : base(name, relativeTranslation, relativeRotation)
        {
            this.NearClip = nearClip;
            this.FarClip = farClip;
        }
    }

    public class PerspectiveCamera : Camera
    {
        public float FOV { get; set; }
        public float AspectRatio { get; set; }

        public override Matrix4x4 ProjectionMatrix => Matrix4x4.CreatePerspectiveFieldOfView(this.FOV, this.AspectRatio, this.NearClip, this.FarClip);

        public PerspectiveCamera(float fov, float aspectRatio, float nearClip, float farClip, string? name = null, Vector3? relativeTranslation = null, Quaternion? relativeRotation = null) : base(nearClip, farClip, name, relativeTranslation, relativeRotation)
        {
            this.FOV = fov;
            this.AspectRatio = aspectRatio;
        }
    }

    public class VREyeCamera : Camera
    {
        public Platform.Headset.Eye Eye { get; }
        private Matrix4x4 _projectionMatrix;
        public override Matrix4x4 ProjectionMatrix => this._projectionMatrix;

        public VREyeCamera(Platform.Headset.Eye eye, float nearClip, float farClip, string? name = null) : base(nearClip, farClip, name)
        {
            if (Program.Headset == null)
                throw new Exception();
            
            this.Eye = eye;

            Program.Headset.GetEyeProjection(this.Eye, this.NearClip, this.FarClip, out this._projectionMatrix);
            Vector3 position;
            Quaternion rotation;
            Program.Headset.GetEyeTransform(this.Eye, out position, out rotation);
            this.RelativeTranslation = position;
            this.RelativeRotation = rotation;
        }
    }
}
