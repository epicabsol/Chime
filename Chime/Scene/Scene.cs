using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Chime.Scene
{
    public enum SceneRenderPass
    {
        Unknown,
        /// <summary>
        /// PBR geometry
        /// </summary>
        GBuffer,
        /// <summary>
        /// Transparent VFX
        /// </summary>
        Effects,
        /// <summary>
        /// World-space overlays like 3D lines, grids, etc
        /// </summary>
        Overlays,
    }

    public class ObjectDrawContext
    {
        public Graphics.DeferredPipeline Pipeline { get; }
        public Camera Camera { get; }
        public SceneRenderPass RenderPass { get; }

        public ObjectDrawContext(Graphics.DeferredPipeline pipeline, Camera camera, SceneRenderPass renderPass)
        {
            this.Pipeline = pipeline;
            this.Camera = camera;
            this.RenderPass = renderPass;
        }
    }

    public class Scene : SceneObject
    {

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
            pipeline.BeginGBuffer(viewMatrix, camera.ProjectionMatrix);
            this.Draw(new ObjectDrawContext(pipeline, camera, SceneRenderPass.GBuffer));

            // Shade objects from GBuffer
            pipeline.ShadeGBuffer();

            // Draw VFX
            this.Draw(new ObjectDrawContext(pipeline, camera, SceneRenderPass.Effects));

            // Postprocessing
            pipeline.PostProcess();

            // Overlays
            pipeline.BeginOverlays();
            this.Draw(new ObjectDrawContext(pipeline, camera, SceneRenderPass.Overlays));
        }
    }
}
