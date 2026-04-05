using UnityEngine;

/// <summary>
/// 오픈월드용 런타임 TerrainData 생성. Perlin 기반 언덕·골 + 스폰 주변 완만한 평지.
/// </summary>
public static class OpenWorldTerrainGenerator
{
    /// <param name="terrainSize">월드 크기 (x=가로, y=최대 높이, z=세로)</param>
    /// <param name="heightmapResolution">2^n+1 권장 (33, 65, 129, 257, 513 …)</param>
    public static TerrainData CreateTerrainData(
        Vector3 terrainSize,
        int heightmapResolution,
        int seed,
        float centerFlatRadius,
        float noiseScale,
        int noiseOctaves)
    {
        if (heightmapResolution < 33 || heightmapResolution > 4097)
        {
            Debug.LogWarning($"[OpenWorldTerrain] heightmapResolution {heightmapResolution} 비정상, 257로 고정합니다.");
            heightmapResolution = 257;
        }

        var terrainData = new TerrainData();
        terrainData.heightmapResolution = heightmapResolution;
        terrainData.size = terrainSize;

        float[,] heights = BuildHeightmap(
            terrainSize,
            heightmapResolution,
            seed,
            centerFlatRadius,
            noiseScale,
            noiseOctaves);

        terrainData.SetHeights(0, 0, heights);
        return terrainData;
    }

    /// <summary>에디터에서 에셋으로 저장한 텍스처를 쓸 때 호출합니다.</summary>
    public static void ApplyGrassLayer(TerrainData terrainData, Texture2D diffuseTexture)
    {
        if (terrainData == null || diffuseTexture == null)
        {
            return;
        }

        var layer = new TerrainLayer
        {
            diffuseTexture = diffuseTexture,
            tileSize = new Vector2(16f, 16f),
        };

        terrainData.terrainLayers = new[] { layer };
    }

    /// <param name="terrainSize">월드 크기 (x=가로, y=최대 높이, z=세로)</param>
    /// <param name="heightmapResolution">2^n+1 권장 (33, 65, 129, 257, 513 …)</param>
    public static Terrain CreateTerrain(
        Vector3 terrainSize,
        int heightmapResolution,
        int seed,
        float centerFlatRadius,
        float noiseScale,
        int noiseOctaves)
    {
        TerrainData terrainData = CreateTerrainData(
            terrainSize,
            heightmapResolution,
            seed,
            centerFlatRadius,
            noiseScale,
            noiseOctaves);

        ApplyGrassLayer(terrainData, CreateSolidTerrainTexture(new Color(0.32f, 0.48f, 0.28f, 1f)));

        GameObject go = Terrain.CreateTerrainGameObject(terrainData);
        go.name = "OpenWorld_Terrain";
        go.transform.position = Vector3.zero;

        Terrain terrain = go.GetComponent<Terrain>();
        if (terrain != null)
        {
            terrain.heightmapPixelError = 5f;
            terrain.basemapDistance = 2000f;
        }

        return terrain;
    }

    /// <summary>지형 위 월드 좌표의 표면 높이(월드 Y). 플레이어 스폰에 사용.</summary>
    public static float GetSurfaceHeight(Terrain terrain, float worldX, float worldZ)
    {
        if (terrain == null || terrain.terrainData == null)
        {
            return 0f;
        }

        return terrain.SampleHeight(new Vector3(worldX, 10000f, worldZ));
    }

    private static float[,] BuildHeightmap(
        Vector3 terrainSize,
        int res,
        int seed,
        float centerFlatRadius,
        float noiseScale,
        int octaves)
    {
        float[,] heights = new float[res, res];
        float cx = terrainSize.x * 0.5f;
        float cz = terrainSize.z * 0.5f;
        float seedX = seed * 17.31f;
        float seedZ = seed * 91.17f;
        octaves = Mathf.Clamp(octaves, 1, 6);

        float falloff = Mathf.Max(15f, centerFlatRadius * 0.45f);

        for (int hy = 0; hy < res; hy++)
        {
            for (int hx = 0; hx < res; hx++)
            {
                float wx = (hx / (float)(res - 1)) * terrainSize.x;
                float wz = (hy / (float)(res - 1)) * terrainSize.z;

                float n = FbmPerlin(wx * noiseScale + seedX, wz * noiseScale + seedZ, octaves);
                n = Mathf.Lerp(0.18f, 0.92f, n);

                float dist = Vector2.Distance(new Vector2(wx, wz), new Vector2(cx, cz));
                float plateau = 0.38f;
                float edgeBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((dist - centerFlatRadius * 0.35f) / falloff));
                float h = Mathf.Lerp(plateau, n, edgeBlend);

                heights[hy, hx] = Mathf.Clamp01(h);
            }
        }

        return heights;
    }

    private static float FbmPerlin(float x, float z, int octaves)
    {
        float sum = 0f;
        float amp = 1f;
        float freq = 1f;
        float norm = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float nx = x * freq;
            float nz = z * freq;
            sum += Mathf.PerlinNoise(nx, nz) * amp;
            norm += amp;
            freq *= 2.05f;
            amp *= 0.5f;
        }

        return norm > 0.0001f ? sum / norm : 0.5f;
    }

    private static Texture2D CreateSolidTerrainTexture(Color c)
    {
        var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false)
        {
            name = "OpenWorld_Terrain_Grass",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
        };
        var pixels = new Color[64];
        for (int i = 0; i < 64; i++)
        {
            pixels[i] = c;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
