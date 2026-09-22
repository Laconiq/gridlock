using System.Numerics;
using Raylib_cs;

namespace Gridlock.Visual
{
    public static class WireframeMeshes
    {
        public static void DrawSphere(Vector3 pos, float radius, Color color)
        {
            Raylib.DrawSphereEx(pos, radius, 4, 6, color);
        }
    }
}
