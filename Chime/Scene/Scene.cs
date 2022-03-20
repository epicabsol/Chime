using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using BulletSharp;

namespace Chime.Scene
{
    public enum SceneRenderPass
    {
        /// <summary>
        /// PBR geometry is drawn into the GBuffer
        /// </summary>
        GBuffer,
        /// <summary>
        /// Light is accumulated using the geometry information in the GBuffer
        /// </summary>
        Lighting,
        /// <summary>
        /// Transparent VFX
        /// </summary>
        Effects,
        /// <summary>
        /// World-space overlays like 3D lines, grids, etc
        /// </summary>
        Overlays,
    }

    public class DrawContext
    {
        public Graphics.DeferredPipeline Pipeline { get; }
        public Camera Camera { get; }
        public SceneRenderPass RenderPass { get; }

        public DrawContext(Graphics.DeferredPipeline pipeline, Camera camera, SceneRenderPass renderPass)
        {
            this.Pipeline = pipeline;
            this.Camera = camera;
            this.RenderPass = renderPass;
        }
    }

    public class Scene : SceneObject
    {
        public DynamicsWorld PhysicsWorld { get; }
        public CollisionConfiguration PhysicsCollisionConfiguration { get; }
        private Dispatcher PhysicsDispatcher { get; }
        private DbvtBroadphase PhysicsBroadphase { get; }

        public Graphics.DebugDraw DebugDraw { get; }

        public Scene(string? name = null) : base(name)
        {
            this.PhysicsCollisionConfiguration = new DefaultCollisionConfiguration();
            this.PhysicsDispatcher = new CollisionDispatcher(this.PhysicsCollisionConfiguration);
            this.PhysicsBroadphase = new DbvtBroadphase();
            this.PhysicsWorld = new DiscreteDynamicsWorld(this.PhysicsDispatcher, this.PhysicsBroadphase, null, this.PhysicsCollisionConfiguration);

            RigidBody groundPlane = new RigidBody(new RigidBodyConstructionInfo(0.0f, new DefaultMotionState(), new StaticPlaneShape(Vector3.UnitY, 0.0f)));
            this.PhysicsWorld.AddRigidBody(groundPlane);

            this.DebugDraw = new Graphics.DebugDraw();
            this.PhysicsWorld.DebugDrawer = this.DebugDraw;
        }

        /// <summary>
        /// Renders this scene to the given pipeline from the given camera's point of view.
        /// </summary>
        /// <param name="pipeline"></param>
        /// <param name="camera"></param>
        public void Render(Graphics.DeferredPipeline pipeline, Camera camera)
        {
            // Render into GBuffer
            if (camera is PerspectiveCamera perspective)
            {
                perspective.AspectRatio = (float)pipeline.Width / pipeline.Height;
            }
            Matrix4x4.Invert(camera.AbsoluteTransform, out Matrix4x4 viewMatrix);
            pipeline.BeginGBuffer(viewMatrix, camera.ProjectionMatrix, camera.NearClip, camera.FarClip);
            this.Draw(new DrawContext(pipeline, camera, SceneRenderPass.GBuffer));

            // Shade objects from GBuffer
            pipeline.BeginLighting();
            this.Draw(new DrawContext(pipeline, camera, SceneRenderPass.Lighting));

            // Draw effects
            pipeline.BeginEffects();
            this.Draw(new DrawContext(pipeline, camera, SceneRenderPass.Effects));

            // Postprocessing
            pipeline.PostProcess();

            // Overlays
            pipeline.BeginOverlays();
            this.Draw(new DrawContext(pipeline, camera, SceneRenderPass.Overlays));
            this.DebugDraw.Draw(new DrawContext(pipeline, camera, SceneRenderPass.Overlays));
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            this.PhysicsWorld.StepSimulation(deltaTime, 4);
            this.PhysicsWorld.DebugDrawWorld();
        }

        public bool Raycast(Vector3 startPosition, Vector3 endPosition, out SceneObject? hitObject, out float t, out Vector3 hitPosition, out Vector3 hitNormal)
        {
            using (ClosestRayResultCallback callback = new ClosestRayResultCallback(ref startPosition, ref endPosition))
            {
                this.PhysicsWorld.RayTestRef(ref startPosition, ref endPosition, callback);

                if (callback.HasHit)
                {
                    hitObject = callback.CollisionObject.UserObject as SceneObject;
                    t = callback.ClosestHitFraction;
                    hitPosition = callback.HitPointWorld;
                    hitNormal = callback.HitNormalWorld;
                    return true;
                }
                else
                {
                    hitObject = null;
                    t = 1.0f;
                    hitPosition = default;
                    hitNormal = default;
                    return false;
                }
            }
        }

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            this.DebugDraw.Dispose();

            this.PhysicsWorld.Dispose();
            this.PhysicsBroadphase.Dispose();
            this.PhysicsDispatcher.Dispose();
            this.PhysicsCollisionConfiguration.Dispose();
        }
    }
}
