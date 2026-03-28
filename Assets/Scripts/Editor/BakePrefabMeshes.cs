#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class BakePrefabMeshes
{
    [MenuItem("Gridlock/Bake Meshes into Prefabs")]
    public static void Run()
    {
        BakeEnemy();
        BakeTower();
        BakeProjectile();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BakePrefabMeshes] Done");
    }

    static void BakeEnemy()
    {
        var mesh = GetOrCreateMesh("Assets/Meshes/Tetrahedron.asset", CreateTetrahedron);
        var defaultMat = GetDefaultLitMaterial();

        string path = "Assets/Prefabs/Enemies/Enemy.prefab";
        var contents = PrefabUtility.LoadPrefabContents(path);

        var mf = contents.GetComponentInChildren<MeshFilter>();
        if (mf != null) mf.sharedMesh = mesh;

        var mr = contents.GetComponentInChildren<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = defaultMat;

        contents.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    static void BakeTower()
    {
        var defaultMat = GetDefaultLitMaterial();

        string path = "Assets/Prefabs/Towers/TowerPrefab.prefab";
        var contents = PrefabUtility.LoadPrefabContents(path);

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var cubeMesh = cube.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(cube);

        var mf = contents.GetComponent<MeshFilter>();
        if (mf != null) mf.sharedMesh = cubeMesh;

        var mr = contents.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = defaultMat;

        contents.transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    static void BakeProjectile()
    {
        var defaultMat = GetDefaultLitMaterial();

        string path = "Assets/Prefabs/Combat/BasicProjectile.prefab";
        var contents = PrefabUtility.LoadPrefabContents(path);

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var sphereMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(sphere);

        var mf = contents.GetComponent<MeshFilter>();
        if (mf != null) mf.sharedMesh = sphereMesh;

        var mr = contents.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = defaultMat;

        contents.transform.localScale = Vector3.one * 0.2f;

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    static Material GetDefaultLitMaterial()
    {
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var mat = tmp.GetComponent<MeshRenderer>().sharedMaterial;
        Object.DestroyImmediate(tmp);
        return mat;
    }

    static Mesh GetOrCreateMesh(string assetPath, System.Func<Mesh> creator)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (existing != null) return existing;

        string dir = System.IO.Path.GetDirectoryName(assetPath).Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(dir))
        {
            var parts = dir.Split('/');
            string parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(parent, parts[i]);
                parent = next;
            }
        }

        var mesh = creator();
        AssetDatabase.CreateAsset(mesh, assetPath);
        return mesh;
    }

    static Mesh CreateTetrahedron()
    {
        var mesh = new Mesh { name = "Tetrahedron" };

        var v0 = new Vector3(0, 1f, 0);
        var v1 = new Vector3(-0.5f, -0.33f, 0.47f);
        var v2 = new Vector3(0.5f, -0.33f, 0.47f);
        var v3 = new Vector3(0, -0.33f, -0.55f);

        mesh.vertices = new[]
        {
            v0, v1, v2,
            v0, v2, v3,
            v0, v3, v1,
            v1, v3, v2
        };
        mesh.triangles = new[] { 0,1,2, 3,4,5, 6,7,8, 9,10,11 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
#endif
