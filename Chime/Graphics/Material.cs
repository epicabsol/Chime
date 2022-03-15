using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using SharpDX.Direct3D11;

namespace Chime.Graphics
{
    public class Material
    {
        public ShaderResourceView? DiffuseTexture { get; }
        public Vector3 DiffuseTint { get; }
        public ShaderResourceView? NormalTexture { get; }
        public ShaderResourceView? MetallicRoughnessTexture { get; }

        public Material(ShaderResourceView? diffuseTexture, Vector3 diffuseTint, ShaderResourceView? normalTexture, ShaderResourceView? metallicRoughnessTexture)
        {
            this.DiffuseTexture = diffuseTexture;
            this.DiffuseTint = diffuseTint;
            this.NormalTexture = normalTexture;
            this.MetallicRoughnessTexture = metallicRoughnessTexture;
        }
    }
}
