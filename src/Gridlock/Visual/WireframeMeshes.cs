using System;
using System.Numerics;
using Raylib_cs;
using RlMesh = Raylib_cs.Mesh;

namespace Gridlock.Visual
{
    public static class WireframeMeshes
    {
        private static RlMesh _cubeMesh;
        private static RlMesh _octahedronMesh;
        private static bool _initialized;

        public static RlMesh Cube => _cubeMesh;
        public static RlMesh Octahedron => _octahedronMesh;

        public static void Init()
        {
            if (_initialized) return;

            _cubeMesh = BuildCubeMesh();
            _octahedronMesh = BuildOctahedronMesh();
            _initialized = true;
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            Raylib.UnloadMesh(_cubeMesh);
            Raylib.UnloadMesh(_octahedronMesh);
            _initialized = false;
        }

        private static RlMesh BuildCubeMesh()
        {
            // Unit cube centered at origin: 6 faces, 2 triangles each = 12 triangles, 36 vertices
            // Each triangle gets unique vertices with barycentric coordinates

            Vector3[] faceNormals =
            {
                new( 0,  0,  1), // front
                new( 0,  0, -1), // back
                new( 1,  0,  0), // right
                new(-1,  0,  0), // left
                new( 0,  1,  0), // top
                new( 0, -1,  0), // bottom
            };

            // Each face defined by 4 corners (CCW winding when viewed from outside)
            Vector3[][] faceCorners =
            {
                // front (z = +0.5)
                new[] { new Vector3(-0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f,  0.5f), new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f) },
                // back (z = -0.5)
                new[] { new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f), new Vector3( 0.5f,  0.5f, -0.5f) },
                // right (x = +0.5)
                new[] { new Vector3( 0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f, -0.5f), new Vector3( 0.5f,  0.5f, -0.5f), new Vector3( 0.5f,  0.5f,  0.5f) },
                // left (x = -0.5)
                new[] { new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f, -0.5f) },
                // top (y = +0.5)
                new[] { new Vector3(-0.5f,  0.5f,  0.5f), new Vector3( 0.5f,  0.5f,  0.5f), new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f) },
                // bottom (y = -0.5)
                new[] { new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f,  0.5f), new Vector3(-0.5f, -0.5f,  0.5f) },
            };

            int vertCount = 36;
            int triCount = 12;

            var mesh = new RlMesh(vertCount, triCount);
            mesh.AllocVertices();
            mesh.AllocNormals();
            mesh.AllocTexCoords();
            mesh.AllocTexCoords2();
            mesh.AllocIndices();

            var verts = mesh.VerticesAs<Vector3>();
            var norms = mesh.NormalsAs<Vector3>();
            var uvs = mesh.TexCoordsAs<Vector2>();
            var bary = mesh.TexCoords2As<Vector2>();
            var indices = mesh.IndicesAs<ushort>();

            // Barycentric coords for each vertex within a triangle
            Vector2[] triBarys = { new(1, 0), new(0, 1), new(0, 0) };

            int vi = 0;
            for (int face = 0; face < 6; face++)
            {
                var c = faceCorners[face];
                var n = faceNormals[face];

                // Triangle 0: corners 0, 1, 2
                verts[vi + 0] = c[0]; verts[vi + 1] = c[1]; verts[vi + 2] = c[2];
                norms[vi + 0] = n; norms[vi + 1] = n; norms[vi + 2] = n;
                uvs[vi + 0] = new Vector2(0, 0); uvs[vi + 1] = new Vector2(1, 0); uvs[vi + 2] = new Vector2(1, 1);
                bary[vi + 0] = triBarys[0]; bary[vi + 1] = triBarys[1]; bary[vi + 2] = triBarys[2];
                indices[vi + 0] = (ushort)(vi + 0);
                indices[vi + 1] = (ushort)(vi + 1);
                indices[vi + 2] = (ushort)(vi + 2);
                vi += 3;

                // Triangle 1: corners 0, 2, 3
                verts[vi + 0] = c[0]; verts[vi + 1] = c[2]; verts[vi + 2] = c[3];
                norms[vi + 0] = n; norms[vi + 1] = n; norms[vi + 2] = n;
                uvs[vi + 0] = new Vector2(0, 0); uvs[vi + 1] = new Vector2(1, 1); uvs[vi + 2] = new Vector2(0, 1);
                bary[vi + 0] = triBarys[0]; bary[vi + 1] = triBarys[1]; bary[vi + 2] = triBarys[2];
                indices[vi + 0] = (ushort)(vi + 0);
                indices[vi + 1] = (ushort)(vi + 1);
                indices[vi + 2] = (ushort)(vi + 2);
                vi += 3;
            }

            Raylib.UploadMesh(ref mesh, false);
            return mesh;
        }

        private static RlMesh BuildOctahedronMesh()
        {
            // Elongated octahedron: 8 triangular faces, 24 vertices
            // radiusH = 0.5, radiusV = 1.0 (unit scale, actual size controlled by transform)
            float rH = 0.5f;
            float rV = 1.0f;

            Vector3 top    = new(0,  rV, 0);
            Vector3 bottom = new(0, -rV, 0);
            Vector3 right  = new( rH, 0, 0);
            Vector3 left   = new(-rH, 0, 0);
            Vector3 front  = new(0, 0,  rH);
            Vector3 back   = new(0, 0, -rH);

            // 8 faces (CCW winding from outside)
            Vector3[][] faces =
            {
                // Top 4 faces
                new[] { top, front, right },
                new[] { top, right, back },
                new[] { top, back, left },
                new[] { top, left, front },
                // Bottom 4 faces
                new[] { bottom, right, front },
                new[] { bottom, back, right },
                new[] { bottom, left, back },
                new[] { bottom, front, left },
            };

            int vertCount = 24;
            int triCount = 8;

            var mesh = new RlMesh(vertCount, triCount);
            mesh.AllocVertices();
            mesh.AllocNormals();
            mesh.AllocTexCoords();
            mesh.AllocTexCoords2();
            mesh.AllocIndices();

            var verts = mesh.VerticesAs<Vector3>();
            var norms = mesh.NormalsAs<Vector3>();
            var uvs = mesh.TexCoordsAs<Vector2>();
            var bary = mesh.TexCoords2As<Vector2>();
            var indices = mesh.IndicesAs<ushort>();

            Vector2[] triBarys = { new(1, 0), new(0, 1), new(0, 0) };

            int vi = 0;
            for (int f = 0; f < 8; f++)
            {
                var a = faces[f][0];
                var b = faces[f][1];
                var c = faces[f][2];

                var edge1 = b - a;
                var edge2 = c - a;
                var normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                verts[vi + 0] = a; verts[vi + 1] = b; verts[vi + 2] = c;
                norms[vi + 0] = normal; norms[vi + 1] = normal; norms[vi + 2] = normal;
                uvs[vi + 0] = Vector2.Zero; uvs[vi + 1] = Vector2.UnitX; uvs[vi + 2] = Vector2.UnitY;
                bary[vi + 0] = triBarys[0]; bary[vi + 1] = triBarys[1]; bary[vi + 2] = triBarys[2];
                indices[vi + 0] = (ushort)(vi + 0);
                indices[vi + 1] = (ushort)(vi + 1);
                indices[vi + 2] = (ushort)(vi + 2);
                vi += 3;
            }

            Raylib.UploadMesh(ref mesh, false);
            return mesh;
        }
    }
}
