using System;
using System.Numerics;
using Gridlock.Core;
using Raylib_cs;
using Color = Raylib_cs.Color;
using RlMesh = Raylib_cs.Mesh;
using RlTexture = Raylib_cs.Texture2D;

namespace Gridlock.Grid
{
    public sealed class GridVisual
    {
        private const float VERTICES_PER_UNIT = 1.5f;
        private const float CELL_FILL = 0.03f;

        private static readonly Vector4 PathColor = new(1f, 0f, 0.7f, 0.8f);
        private static readonly Vector4 BlockedColor = new(1f, 0.2f, 0.2f, 0.9f);
        private static readonly Vector4 SpawnColor = new(1f, 0f, 0.7f, 1f);
        private static readonly Vector4 ObjectiveColor = new(0f, 1f, 0.8f, 1f);
        private static readonly Vector4 ClearColor = Vector4.Zero;

        private GridManager _gridManager = null!;
        private GridWarpManager _warpManager = null!;

        private RlMesh _mesh;
        private Material _material;
        private Material _fallbackMaterial;
        private bool _hasFallbackMaterial;
        private RlTexture _cellMap;
        private byte[] _cellMapPixels = Array.Empty<byte>();
        private bool _cellMapDirty;

        private int _vertCount;
        private int _resX, _resZ;
        private float _gridWidth, _gridHeight;

        private float[] _positionFloats = Array.Empty<float>();
        private byte[] _colorBytes = Array.Empty<byte>();

        private bool _initialized;
        private Shader _gridShader;
        private bool _hasShader;
        public bool HasShader => _hasShader;

        private int _locGridSize;
        private int _locGridOrigin;
        private int _locGridExtent;
        private int _locCellFill;
        private int _locCellMap;
        private int _locGridColor;

        public void Init(GridManager gridManager, GridWarpManager warpManager)
        {
            _gridManager = gridManager;
            _warpManager = warpManager;

            var def = _gridManager.Definition;
            _gridWidth = def.Width * def.CellSize;
            _gridHeight = def.Height * def.CellSize;

            _resX = (int)MathF.Ceiling(_gridWidth * VERTICES_PER_UNIT);
            _resZ = (int)MathF.Ceiling(_gridHeight * VERTICES_PER_UNIT);

            var restPositions = CreateSubdividedPlane(_gridWidth, _gridHeight, _resX, _resZ);

            _warpManager.Init(_resX, _resZ, _gridWidth, _gridHeight, restPositions);

            BuildCellMap();

            _gridManager.OnCellChanged += OnCellChanged;

            _initialized = true;
        }

        public void SetShader(Shader shader)
        {
            _gridShader = shader;
            _hasShader = true;

            _locGridSize = Raylib.GetShaderLocation(shader, "gridSize");
            _locGridOrigin = Raylib.GetShaderLocation(shader, "gridOrigin");
            _locGridExtent = Raylib.GetShaderLocation(shader, "gridExtent");
            _locCellFill = Raylib.GetShaderLocation(shader, "cellFill");
            _locCellMap = Raylib.GetShaderLocation(shader, "cellMap");
            _locGridColor = Raylib.GetShaderLocation(shader, "gridColor");

            Console.WriteLine($"[GridVisual] Shader uniform locations: gridSize={_locGridSize} gridOrigin={_locGridOrigin} " +
                $"gridExtent={_locGridExtent} cellFill={_locCellFill} cellMap={_locCellMap} gridColor={_locGridColor}");

            _material = Raylib.LoadMaterialDefault();
            unsafe
            {
                _material.Shader = shader;

                int matModelLoc = Raylib.GetShaderLocation(shader, "matModel");
                if (matModelLoc >= 0)
                    shader.Locs[(int)ShaderLocationIndex.MatrixModel] = matModelLoc;

                int mvpLoc = Raylib.GetShaderLocation(shader, "mvp");
                if (mvpLoc >= 0)
                    shader.Locs[(int)ShaderLocationIndex.MatrixMvp] = mvpLoc;

                Console.WriteLine($"[GridVisual] Matrix locations: matModel={matModelLoc} mvp={mvpLoc}");

                // Bind cellMap to the material's albedo map slot so DrawMesh
                // correctly binds it to texture unit 0 (prevents DrawMesh from
                // overwriting our SetShaderValueTexture binding with the default
                // white texture).
                _material.Maps[(int)MaterialMapIndex.Albedo].Texture = _cellMap;
            }
        }

        public void Shutdown()
        {
            if (_gridManager != null)
                _gridManager.OnCellChanged -= OnCellChanged;

            if (_initialized)
            {
                Raylib.UnloadMesh(_mesh);
                Raylib.UnloadTexture(_cellMap);
            }

            if (_hasShader && _gridShader.Id > 0)
                Raylib.UnloadShader(_gridShader);

            _hasShader = false;
            _initialized = false;
        }

        private Vector3[] CreateSubdividedPlane(float width, float height, int resX, int resZ)
        {
            int vx = resX + 1;
            int vz = resZ + 1;
            _vertCount = vx * vz;
            int triCount = resX * resZ * 2;

            _mesh = new RlMesh(_vertCount, triCount);
            _mesh.AllocVertices();
            _mesh.AllocColors();
            _mesh.AllocIndices();
            _mesh.AllocNormals();
            _mesh.AllocTexCoords();

            var restPositions = new Vector3[_vertCount];
            var verts = _mesh.VerticesAs<Vector3>();
            var norms = _mesh.NormalsAs<Vector3>();
            var uvs = _mesh.TexCoordsAs<Vector2>();
            var colors = _mesh.ColorsAs<Color>();

            for (int z = 0; z <= resZ; z++)
            {
                for (int x = 0; x <= resX; x++)
                {
                    int i = z * vx + x;
                    float px = ((float)x / resX - 0.5f) * width;
                    float pz = ((float)z / resZ - 0.5f) * height;
                    var pos = new Vector3(px, -0.01f, pz);
                    restPositions[i] = pos;
                    verts[i] = pos;
                    norms[i] = Vector3.UnitY;
                    uvs[i] = new Vector2((float)x / resX, (float)z / resZ);
                    colors[i] = new Color(0, 0, 0, 0);
                }
            }

            var indices = _mesh.IndicesAs<ushort>();
            int t = 0;
            for (int z = 0; z < resZ; z++)
            {
                for (int x = 0; x < resX; x++)
                {
                    int bl = z * vx + x;
                    int br = bl + 1;
                    int tl = bl + vx;
                    int tr = tl + 1;
                    indices[t++] = (ushort)bl;
                    indices[t++] = (ushort)tl;
                    indices[t++] = (ushort)br;
                    indices[t++] = (ushort)br;
                    indices[t++] = (ushort)tl;
                    indices[t++] = (ushort)tr;
                }
            }

            Raylib.UploadMesh(ref _mesh, true);

            _positionFloats = new float[_vertCount * 3];
            _colorBytes = new byte[_vertCount * 4];

            return restPositions;
        }

        private void BuildCellMap()
        {
            var def = _gridManager.Definition;
            int w = def.Width;
            int h = def.Height;
            _cellMapPixels = new byte[w * h * 4];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var cell = _gridManager.GetRuntimeCell(x, y);
                    SetCellPixel(x, y, w, GetCellColor(cell));
                }
            }

            var image = Raylib.GenImageColor(w, h, new Color(0, 0, 0, 0));
            _cellMap = Raylib.LoadTextureFromImage(image);
            Raylib.UnloadImage(image);
            Raylib.SetTextureFilter(_cellMap, TextureFilter.Point);
            Raylib.SetTextureWrap(_cellMap, TextureWrap.Clamp);

            Raylib.UpdateTexture(_cellMap, _cellMapPixels);
        }

        private void SetCellPixel(int x, int y, int width, Vector4 color)
        {
            int idx = (y * width + x) * 4;
            _cellMapPixels[idx + 0] = (byte)(color.X * 255f);
            _cellMapPixels[idx + 1] = (byte)(color.Y * 255f);
            _cellMapPixels[idx + 2] = (byte)(color.Z * 255f);
            _cellMapPixels[idx + 3] = (byte)(color.W * 255f);
        }

        private void OnCellChanged(int x, int y, CellType type)
        {
            SetCellPixel(x, y, _gridManager.Definition.Width, GetCellColor(type));
            _cellMapDirty = true;
        }

        private static Vector4 GetCellColor(CellType type) => type switch
        {
            CellType.Path => PathColor,
            CellType.Blocked => BlockedColor,
            CellType.Spawn => SpawnColor,
            CellType.Objective => ObjectiveColor,
            _ => ClearColor
        };

        public void Update(float dt)
        {
            if (!_initialized) return;

            if (_cellMapDirty)
            {
                Raylib.UpdateTexture(_cellMap, _cellMapPixels);
                _cellMapDirty = false;
            }

            if (!_warpManager.IsSleeping)
                UpdateMeshBuffers();
        }

        private unsafe void UpdateMeshBuffers()
        {
            var positions = _warpManager.Positions;
            var colors = _warpManager.Colors;

            for (int i = 0; i < _vertCount; i++)
            {
                int fi = i * 3;
                _positionFloats[fi + 0] = positions[i].X;
                _positionFloats[fi + 1] = positions[i].Y;
                _positionFloats[fi + 2] = positions[i].Z;

                int ci = i * 4;
                _colorBytes[ci + 0] = (byte)Math.Clamp(colors[i].X * 255f, 0f, 255f);
                _colorBytes[ci + 1] = (byte)Math.Clamp(colors[i].Y * 255f, 0f, 255f);
                _colorBytes[ci + 2] = (byte)Math.Clamp(colors[i].Z * 255f, 0f, 255f);
                _colorBytes[ci + 3] = (byte)Math.Clamp(colors[i].W * 255f, 0f, 255f);
            }

            Raylib.UpdateMeshBuffer<float>(_mesh, 0, _positionFloats.AsSpan(), 0);
            Raylib.UpdateMeshBuffer<byte>(_mesh, 3, _colorBytes.AsSpan(), 0);
        }

        public void Render()
        {
            if (!_initialized) return;

            if (_hasShader)
            {
                var def = _gridManager.Definition;
                Raylib.SetShaderValue(_gridShader, _locGridSize, def.CellSize, ShaderUniformDataType.Float);
                Raylib.SetShaderValue(_gridShader, _locCellFill, CELL_FILL, ShaderUniformDataType.Float);

                var origin = new float[] { -_gridWidth * 0.5f, -_gridHeight * 0.5f };
                var extent = new float[] { _gridWidth, _gridHeight };
                Raylib.SetShaderValue(_gridShader, _locGridOrigin, origin, ShaderUniformDataType.Vec2);
                Raylib.SetShaderValue(_gridShader, _locGridExtent, extent, ShaderUniformDataType.Vec2);

                var gridColor = new float[] { 0f, 1f, 1f, 1f };
                Raylib.SetShaderValue(_gridShader, _locGridColor, gridColor, ShaderUniformDataType.Vec4);

                // Bind cellMap sampler to texture unit 0 -- the material's albedo
                // map is already set to _cellMap so DrawMesh will bind the correct
                // texture to unit 0 instead of the default white texture.
                if (_locCellMap >= 0)
                    Raylib.SetShaderValue(_gridShader, _locCellMap, 0, ShaderUniformDataType.Int);

                Rlgl.DisableBackfaceCulling();
                Rlgl.DisableDepthMask();
                Rlgl.DisableDepthTest();
                Raylib.BeginBlendMode(BlendMode.Alpha);

                Raylib.DrawMesh(_mesh, _material, Matrix4x4.Identity);

                Raylib.EndBlendMode();
                Rlgl.EnableDepthTest();
                Rlgl.EnableDepthMask();
                Rlgl.EnableBackfaceCulling();
            }
            else
            {
                if (!_hasFallbackMaterial)
                {
                    _fallbackMaterial = Raylib.LoadMaterialDefault();
                    _hasFallbackMaterial = true;
                }
                Raylib.DrawMesh(_mesh, _fallbackMaterial, Matrix4x4.Identity);
            }
        }
    }
}
