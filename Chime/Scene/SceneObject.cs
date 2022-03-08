using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Chime.Scene
{
    public class SceneObject : IDisposable
    {
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; } = Quaternion.Identity;
        public Vector3 Scale { get; set; } = Vector3.One;

        // TODO: Verify this order!
        public Matrix4x4 RelativeTransform => Matrix4x4.CreateScale(this.Scale) * Matrix4x4.CreateFromQuaternion(this.Rotation) * Matrix4x4.CreateTranslation(this.Position);
        public Matrix4x4 AbsoluteTransform
        {
            get
            {
                Matrix4x4 result = this.RelativeTransform;
                SceneObject? parent = this.Parent;
                while (parent != null)
                {
                    result = result * parent.RelativeTransform;
                    parent = parent.Parent;
                }
                return result;
            }
        }

        public SceneObject(string? name = null)
        {
            this.Name = name ?? this.GetType().Name;
        }

        #region Hierarchy Management
        private List<SceneObject> ChildObjects = new List<SceneObject>();
        private bool disposedValue;

        public IReadOnlyList<SceneObject> Children => this.ChildObjects.AsReadOnly();
        public SceneObject? Parent { get; private set; }

        public void AddChild(SceneObject child)
        {
            if (child.Parent != null)
                child.Parent.RemoveChild(child);
            this.ChildObjects.Add(child);
            child.Parent = this;
            this.OnChildAdded(child);
        }

        public void RemoveChild(SceneObject child)
        {
            this.ChildObjects.Remove(child);
            child.Parent = null;
            this.OnChildRemoved(child);
        }

        protected virtual void OnChildAdded(SceneObject child)
        {

        }

        protected virtual void OnChildRemoved(SceneObject child)
        {

        }
        #endregion

        public virtual void Update(float deltaTime)
        {
            foreach (SceneObject child in this.Children)
            {
                child.Update(deltaTime);
            }
        }

        public virtual void Draw(ObjectDrawContext context)
        {
            foreach (SceneObject child in this.Children)
            {
                child.Draw(context);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                OnDispose(disposing);
                disposedValue = true;
            }
        }

        protected virtual void OnDispose(bool disposing)
        {
            foreach (SceneObject child in this.Children)
            {
                child.Dispose(disposing);
                child.Parent = null;
            }
            this.ChildObjects.Clear();

            if (disposing)
            {
                // Dispose managed state (managed objects)
            }

            // Free unmanaged resources (unmanaged objects) and override finalizer
        }

        ~SceneObject()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
