#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 절차 지형을 에디터에서 씬에 생성·저장해 씬 뷰에서 편집할 수 있게 합니다.
/// Tools → ARCHÉ → Open World → Bake Terrain Into Current Scene
/// </summary>
public static class OpenWorldTerrainBake
{
    private const string TerrainFolder = "Assets/Terrain";

    private const string TerrainDataPath = "Assets/Terrain/OpenWorldTerrainData.asset";

    private const string GrassTexPath = "Assets/Terrain/OpenWorldTerrain_GrassDiffuse.asset";

    [MenuItem("Tools/ARCHÉ/Open World/Bake Terrain Into Current Scene")]
    public static void BakeFromMenu()
    {
        Bake(null);
    }

    /// <param name="optionalBoot">인스펙터 버튼 등에서 지정. null 이면 씬에서 첫 번째 OpenWorldBootstrap 을 사용합니다.</param>
    public static void Bake(OpenWorldBootstrap optionalBoot)
    {
        OpenWorldBootstrap boot = optionalBoot != null
            ? optionalBoot
            : Object.FindFirstObjectByType<OpenWorldBootstrap>();
        if (boot == null)
        {
            EditorUtility.DisplayDialog(
                "Open World",
                "현재 씬에 OpenWorldBootstrap 이 없습니다.\nOpenWorldScene 을 열거나 씬에 컴포넌트를 추가하세요.",
                "확인");
            return;
        }

        SerializedObject so = new SerializedObject(boot);
        Terrain prevTerrain = so.FindProperty("_sceneTerrain").objectReferenceValue as Terrain;

        Vector3 wSize = so.FindProperty("_terrainWorldSize").vector3Value;
        int res = so.FindProperty("_terrainHeightmapResolution").intValue;
        int seed = so.FindProperty("_terrainSeed").intValue;
        float flatR = so.FindProperty("_terrainSpawnFlatRadius").floatValue;
        float noise = so.FindProperty("_terrainNoiseScale").floatValue;
        int octaves = so.FindProperty("_terrainNoiseOctaves").intValue;

        EnsureTerrainFolder();
        Texture2D grass = EnsureGrassTextureAsset();

        foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null)
            {
                continue;
            }

            if (t == prevTerrain || t.name == "OpenWorld_Terrain")
            {
                Undo.DestroyObjectImmediate(t.gameObject);
            }
        }

        if (AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath) != null)
        {
            AssetDatabase.DeleteAsset(TerrainDataPath);
        }

        TerrainData td = OpenWorldTerrainGenerator.CreateTerrainData(wSize, res, seed, flatR, noise, octaves);
        OpenWorldTerrainGenerator.ApplyGrassLayer(td, grass);
        AssetDatabase.CreateAsset(td, TerrainDataPath);
        AssetDatabase.SaveAssets();

        TerrainData tdRef = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        GameObject terrainGo = Terrain.CreateTerrainGameObject(tdRef);
        terrainGo.name = "OpenWorld_Terrain";
        terrainGo.transform.position = Vector3.zero;
        Terrain newTerrain = terrainGo.GetComponent<Terrain>();
        if (newTerrain != null)
        {
            newTerrain.heightmapPixelError = 5f;
            newTerrain.basemapDistance = 2000f;
        }

        Undo.RegisterCreatedObjectUndo(terrainGo, "Bake Open World Terrain");

        so.FindProperty("_sceneTerrain").objectReferenceValue = newTerrain;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(terrainGo.scene);
        Selection.activeGameObject = terrainGo;

        EditorUtility.DisplayDialog(
            "Open World",
            "지형을 씬에 베이크했습니다.\nOpenWorldBootstrap 의 Scene Terrain 에 연결되었습니다.\n변경을 유지하려면 씬을 저장하세요.",
            "확인");
    }

    private static void EnsureTerrainFolder()
    {
        if (!AssetDatabase.IsValidFolder(TerrainFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Terrain");
        }
    }

    private static Texture2D EnsureGrassTextureAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassTexPath);
        if (existing != null)
        {
            return existing;
        }

        var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false)
        {
            name = "OpenWorld_Terrain_Grass",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
        };

        var c = new Color(0.32f, 0.48f, 0.28f, 1f);
        var pixels = new Color[64];
        for (int i = 0; i < 64; i++)
        {
            pixels[i] = c;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        AssetDatabase.CreateAsset(tex, GrassTexPath);
        AssetDatabase.SaveAssets();
        return tex;
    }
}
#endif
