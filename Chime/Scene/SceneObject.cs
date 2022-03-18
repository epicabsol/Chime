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
        public virtual Vector3 RelativeTranslation { get; set; }
        public virtual Quaternion RelativeRotation { get; set; } = Quaternion.Identity;
        public virtual Vector3 RelativeScale { get; set; } = Vector3.One;

        // TODO: Verify this order!
        public Matrix4x4 RelativeTransform
        {
            get
            {
                return Matrix4x4.CreateScale(this.RelativeScale) * Matrix4x4.CreateFromQuaternion(this.RelativeRotation) * Matrix4x4.CreateTranslation(this.RelativeTranslation);
            }
            set
            {
                if (Matrix4x4.Decompose(value, out Vector3 scale, out Quaternion rotation, out Vector3 translation))
                {
                    this.RelativeScale = scale;
                    this.RelativeRotation = rotation;
                    this.RelativeTranslation = translation;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The given transform was not decomposable into translation, rotation, and scaling!");
                }
            }
        }

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
            set
            {
                Matrix4x4 parentInverse = Matrix4x4.Identity;
                if (this.Parent != null)
                {
                    if (!Matrix4x4.Invert(this.Parent.AbsoluteTransform, out parentInverse))
                    {
                        throw new Exception("Could not invert parent transform!");
                    }
                }
                this.RelativeTransform = value * parentInverse;
            }
        }

        public Scene? Scene
        {
            get
            {
                SceneObject sceneObject = this;
                while (sceneObject.Parent != null)
                {
                    if (sceneObject.Parent is Scene scene)
                    {
                        return scene;
                    }
                    sceneObject = sceneObject.Parent;
                }
                return sceneObject as Scene;
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
            child.OnAncestorChanged(child, null, this);
        }

        public void RemoveChild(SceneObject child)
        {
            this.ChildObjects.Remove(child);
            child.Parent = null;
            this.OnChildRemoved(child);
            child.OnAncestorChanged(child, this, null);
        }

        protected virtual void OnChildAdded(SceneObject child)
        {

        }

        protected virtual void OnChildRemoved(SceneObject child)
        {

        }

        protected virtual void OnAncestorChanged(SceneObject sceneObject, SceneObject? oldParent, SceneObject? newParent)
        {
            foreach (SceneObject child in this.ChildObjects)
            {
                child.OnAncestorChanged(sceneObject, oldParent, newParent);
            }
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
