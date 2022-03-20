using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Chime.Scene
{
    public class PhysicsObject : SceneObject
    {
        /// <summary>
        /// A helper object that links the transform of a physics object between Chime and Bullet.
        /// </summary>
        private class PhysicsObjectMotionState : BulletSharp.MotionState
        {
            public SceneObject SceneObject { get; set; }

            public PhysicsObjectMotionState(SceneObject sceneObject)
            {
                this.SceneObject = sceneObject;
            }

            public override void GetWorldTransform(out Matrix4x4 worldTrans)
            {
                worldTrans = this.SceneObject.AbsoluteTransform;
            }

            public override void SetWorldTransform(ref Matrix4x4 worldTrans)
            {
                this.SceneObject.AbsoluteTransform = worldTrans;
            }
        }

        public override Vector3 RelativeTranslation 
        {
            get => base.RelativeTranslation;
            set
            {
                base.RelativeTranslation = value;
                //this.RigidBody.WorldTransform = this.RigidBody.InterpolationWorldTransform = this.AbsoluteTransform;
            } 
        }
        public override Quaternion RelativeRotation
        {
            get => base.RelativeRotation;
            set
            {
                base.RelativeRotation = value;
                //this.RigidBody.WorldTransform = this.RigidBody.InterpolationWorldTransform = this.AbsoluteTransform;
            }
        }
        public override Vector3 RelativeScale
        {
            get => base.RelativeScale;
            set
            {
                base.RelativeScale = value;
                //this.RigidBody.WorldTransform = this.RigidBody.InterpolationWorldTransform = this.AbsoluteTransform;
            }
        }

        public BulletSharp.RigidBody RigidBody { get; }
        private bool IsInScene { get; set; }

        public Vector3 Velocity
        {
            get => this.RigidBody.LinearVelocity;
            set => this.RigidBody.LinearVelocity = value;
        }

        public bool IsKinematic => this.RigidBody.InvMass == 0.0f;

        public PhysicsObject(BulletSharp.CollisionShape collisionShape, float mass = 0.0f, string? name = null, Vector3? relativeTranslation = null, Quaternion? relativeRotation = null, Vector3? relativeScale = null) : base(name, relativeTranslation, relativeRotation, relativeScale)
        {
            using (BulletSharp.RigidBodyConstructionInfo info = (mass > 0.0f ? new BulletSharp.RigidBodyConstructionInfo(mass, new PhysicsObjectMotionState(this), collisionShape, collisionShape.CalculateLocalInertia(mass)) : new BulletSharp.RigidBodyConstructionInfo(mass, new PhysicsObjectMotionState(this), collisionShape)))
            {
                info.StartWorldTransform = this.AbsoluteTransform;
                this.RigidBody = new BulletSharp.RigidBody(info);
                this.RigidBody.UserObject = this;
            }
        }

        protected override void OnAncestorChanged(SceneObject sceneObject, SceneObject? oldParent, SceneObject? newParent)
        {
            base.OnAncestorChanged(sceneObject, oldParent, newParent);

            // If we have been removed from a Scene, unregister the rigidbody
            if (oldParent is Scene oldScene)
            {
                oldScene.PhysicsWorld.AddRigidBody(this.RigidBody);
                this.IsInScene = false;
            }

            // If we have been added to a Scene, register the rigidbody
            if (newParent is Scene newScene)
            {
                // Sanity check
                if (this.IsInScene)
                    throw new InvalidOperationException("Cannot be in two scenes at the same time!");

                newScene.PhysicsWorld.AddRigidBody(this.RigidBody);
                this.IsInScene = true;
            }
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            Matrix4x4 world = this.AbsoluteTransform;
            Vector3 color = Vector3.One;
            //this.Scene.PhysicsWorld.DebugDrawObjectRef(ref world, this.RigidBody.CollisionShape, ref color);
        }

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            this.RigidBody.Dispose();
        }
    }
}
