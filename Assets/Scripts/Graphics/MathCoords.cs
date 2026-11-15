using UnityEngine;

namespace EulersFuneral.Graphics
{
    /// <summary>
    /// Maps mathematical coordinates (x, y, z = f(x,y)) where z is vertical height
    /// to Unity world coordinates where Y is vertical up:
    /// Math (x, y, z) <-> Unity (x, z, y).
    /// </summary>
    public static class MathCoords
    {
        public static Vector3 ToUnity(double mathX, double mathY, double mathZ)
        {
            return new Vector3((float)mathX, (float)mathZ, (float)mathY);
        }

        public static Vector3 ToUnity(Vector3 mathPos)
        {
            return new Vector3(mathPos.x, mathPos.z, mathPos.y);
        }

        public static void FromUnity(Vector3 unityPos, out double mathX, out double mathY, out double mathZ)
        {
            mathX = unityPos.x;
            mathY = unityPos.z;
            mathZ = unityPos.y;
        }
    }
}
