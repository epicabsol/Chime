using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Chime
{
    public static class MathUtils
    {
        public static Vector3 GetXAxis(ref this Matrix4x4 matrix)
        {
            return new Vector3(matrix.M11, matrix.M12, matrix.M13);
        }

        public static Vector3 GetYAxis(ref this Matrix4x4 matrix)
        {
            return new Vector3(matrix.M21, matrix.M22, matrix.M23);
        }

        public static Vector3 GetZAxis(ref this Matrix4x4 matrix)
        {
            return new Vector3(matrix.M31, matrix.M32, matrix.M33);
        }
    }
}
