using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 숲속 호수 씬 연출(동화·힐링 톤). 런타임 배치 — 에셋 없이 프리미티브·재질·안개·파티클로 구성합니다.
/// </summary>
public static class FishingLakeEnvironmentBuilder
{
    /// <summary>
    /// Unity Plane 메쉬는 10×10 유닛. scale × 10 = 가로·세로 길이(미터에 가깝게 사용).
    /// 기존 (2.35, 1.95) ≈ 23.5m×19.5m → 넓은 호수 느낌으로 확대.
    /// </summary>
    private const float WaterPlaneScaleX = 4.1f;
    private const float WaterPlaneScaleZ = 3.4f;
    private const float ShorePlaneScaleX = 4.6f;
    private const float ShorePlaneScaleZ = 3.75f;

    private static Material _matTrunk;
    private static Material _matFoliageA;
    private static Material _matFoliageB;
    private static Material _matRock;
    private static Material _matReed;
    private static Material _matLily;
    private static Material _matGrassMain;
    private static Material _matGrassPatch;
    private static Material _matWater;
    private static Material _matShore;
    private static Material _skyboxMat;

    public static void ApplyAtmosphere()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.58f, 0.72f, 0.9f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.52f, 0.62f, 0.48f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.28f, 0.32f, 0.26f, 1f);
        RenderSettings.ambientIntensity = 1.05f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.62f, 0.76f, 0.88f, 1f);
        RenderSettings.fogStartDistance = 18f;
        RenderSettings.fogEndDistance = 95f;

        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            if (_skyboxMat == null)
            {
                _skyboxMat = new Material(skyShader);
            }

            _skyboxMat.SetColor("_SkyTint", new Color(0.65f, 0.78f, 0.95f, 1f));
            _skyboxMat.SetColor("_GroundColor", new Color(0.32f, 0.38f, 0.3f, 1f));
            _skyboxMat.SetFloat("_AtmosphereThickness", 1.15f);
            _skyboxMat.SetFloat("_Exposure", 1.05f);
            _skyboxMat.SetFloat("_SunSize", 0.04f);
            _skyboxMat.SetFloat("_SunSizeConvergence", 3.2f);
            RenderSettings.skybox = _skyboxMat;
        }

        DynamicGI.UpdateEnvironment();
    }

    public static void BuildTerrainAndWater(Transform root)
    {
        EnsureMaterials();

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground_Grass";
        ground.transform.SetParent(root);
        ground.transform.localPosition = Vector3.zero;
        ground.transform.localScale = new Vector3(8.5f, 1f, 8.5f);
        ApplyMaterial(ground, _matGrassMain);

        Random.InitState(20260328);
        for (int i = 0; i < 14; i++)
        {
            var patch = GameObject.CreatePrimitive(PrimitiveType.Plane);
            patch.name = $"GrassPatch_{i}";
            patch.transform.SetParent(root);
            float ang = Random.Range(0f, 360f);
            float rad = Random.Range(4f, 22f);
            patch.transform.localPosition = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * rad, 0.01f, Mathf.Sin(ang * Mathf.Deg2Rad) * rad);
            patch.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
            patch.transform.localScale = Vector3.one * Random.Range(0.35f, 0.9f);
            ApplyMaterial(patch, _matGrassPatch);
            Object.Destroy(patch.GetComponent<Collider>());
        }

        var shore = GameObject.CreatePrimitive(PrimitiveType.Plane);
        shore.name = "Shore_Ring";
        shore.transform.SetParent(root);
        shore.transform.localPosition = new Vector3(0f, 0.012f, 8f);
        shore.transform.localScale = new Vector3(ShorePlaneScaleX, 1f, ShorePlaneScaleZ);
        ApplyMaterial(shore, _matShore);
        Object.Destroy(shore.GetComponent<Collider>());

        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Water_Lake";
        water.transform.SetParent(root);
        water.transform.localPosition = new Vector3(0f, 0.02f, 8f);
        water.transform.localScale = new Vector3(WaterPlaneScaleX, 1f, WaterPlaneScaleZ);
        ApplyMaterial(water, _matWater);
        Object.Destroy(water.GetComponent<Collider>());
    }

    public static void BuildForestRing(Transform root)
    {
        EnsureMaterials();
        Random.InitState(42);

        for (int i = 0; i < 22; i++)
        {
            float t = i / 22f * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
            float radius = Random.Range(20f, 34f);
            float x = Mathf.Cos(t) * radius + Random.Range(-1.2f, 1.2f);
            float z = Mathf.Sin(t) * radius + Random.Range(-1.2f, 1.2f) + 5f;
            if (z < 2f && Mathf.Abs(x) < 8f)
            {
                z += 6f;
            }

            BuildStylizedTree(root, new Vector3(x, 0f, z), Random.Range(0.85f, 1.35f));
        }

        for (int i = 0; i < 11; i++)
        {
            float x = Random.Range(-16f, 16f);
            float z = Random.Range(-12f, 4f);
            if (z > -3f && Mathf.Abs(x) < 5f)
            {
                z -= 5f;
            }

            BuildRock(root, new Vector3(x, 0f, z), Random.Range(0.4f, 1.1f));
        }
    }

    public static void BuildShoreDetails(Transform root)
    {
        EnsureMaterials();
        Random.InitState(77);

        float lakeZ = 8f;
        float lakeRx = WaterPlaneScaleX * 5f * 0.92f;
        float lakeRz = WaterPlaneScaleZ * 5f * 0.92f;
        for (int i = 0; i < 28; i++)
        {
            float edgeT = Random.Range(0f, Mathf.PI * 2f);
            Vector3 pos = new Vector3(
                Mathf.Cos(edgeT) * (lakeRx + Random.Range(-0.4f, 0.8f)),
                0.015f,
                lakeZ + Mathf.Sin(edgeT) * (lakeRz + Random.Range(-0.4f, 0.8f)));
            BuildReed(root, pos, Random.Range(0.7f, 1.25f));
        }

        float lilyHx = WaterPlaneScaleX * 5f * 0.78f;
        float lilyHz = WaterPlaneScaleZ * 5f * 0.78f;
        for (int i = 0; i < 14; i++)
        {
            float lx = Random.Range(-lilyHx, lilyHx);
            float lz = lakeZ + Random.Range(-lilyHz, lilyHz);
            if (lx * lx / (lilyHx * lilyHx) + (lz - lakeZ) * (lz - lakeZ) / (lilyHz * lilyHz) > 1f)
            {
                continue;
            }

            BuildLilyPad(root, new Vector3(lx, 0.025f, lz), Random.Range(0.35f, 0.55f));
        }
    }

    public static void BuildFairyParticles(Transform root)
    {
        var go = new GameObject("FairyMotes");
        go.transform.SetParent(root);
        go.transform.localPosition = new Vector3(0f, 1.2f, 6f);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 6f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.95f, 0.92f, 0.75f, 0.55f),
            new Color(0.75f, 0.9f, 1f, 0.45f));

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(38f, 4f, 26f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.y = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.2f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var r = ps.GetComponent<ParticleSystemRenderer>();
        Shader psShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (psShader == null)
        {
            psShader = Shader.Find("Particles/Standard Unlit");
        }

        if (psShader != null)
        {
            r.material = new Material(psShader);
        }
    }

    private static void EnsureMaterials()
    {
        if (_matTrunk != null)
        {
            return;
        }

        _matTrunk = CreateSolid(new Color(0.38f, 0.26f, 0.18f, 1f));
        _matFoliageA = CreateSolid(new Color(0.32f, 0.58f, 0.35f, 1f));
        _matFoliageB = CreateSolid(new Color(0.42f, 0.62f, 0.4f, 1f));
        _matRock = CreateSolid(new Color(0.52f, 0.5f, 0.46f, 1f));
        _matReed = CreateSolid(new Color(0.22f, 0.42f, 0.24f, 1f));
        _matLily = CreateSolid(new Color(0.28f, 0.52f, 0.32f, 1f));
        _matGrassMain = CreateSolid(new Color(0.36f, 0.52f, 0.34f, 1f));
        _matGrassPatch = CreateSolid(new Color(0.4f, 0.58f, 0.38f, 1f));

        Shader sprite = Shader.Find("Sprites/Default");
        if (sprite != null)
        {
            _matWater = new Material(sprite);
            _matWater.color = new Color(0.22f, 0.48f, 0.62f, 0.78f);
        }
        else
        {
            _matWater = CreateSolid(new Color(0.25f, 0.5f, 0.62f, 0.8f));
        }

        _matShore = CreateSolid(new Color(0.45f, 0.42f, 0.32f, 1f));
    }

    private static Material CreateSolid(Color c)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null)
        {
            s = Shader.Find("Standard");
        }

        var m = new Material(s);
        if (m.HasProperty("_BaseColor"))
        {
            m.SetColor("_BaseColor", c);
        }
        else
        {
            m.color = c;
        }

        if (m.HasProperty("_Smoothness"))
        {
            m.SetFloat("_Smoothness", 0.15f);
        }

        if (m.HasProperty("_BaseMap"))
        {
            m.SetTexture("_BaseMap", Texture2D.whiteTexture);
        }

        return m;
    }

    private static void ApplyMaterial(GameObject go, Material mat)
    {
        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = mat;
        }
    }

    private static void BuildStylizedTree(Transform root, Vector3 pos, float scale)
    {
        var tree = new GameObject("Tree");
        tree.transform.SetParent(root);
        tree.transform.position = pos;
        tree.transform.localScale = Vector3.one * scale;

        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        trunk.transform.localScale = new Vector3(0.22f, 0.65f, 0.22f);
        ApplyMaterial(trunk, _matTrunk);
        Object.Destroy(trunk.GetComponent<Collider>());

        for (int l = 0; l < 3; l++)
        {
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.name = $"Foliage_{l}";
            leaf.transform.SetParent(tree.transform, false);
            leaf.transform.localPosition = new Vector3(0f, 1.15f + l * 0.35f, 0f);
            leaf.transform.localScale = new Vector3(1.15f - l * 0.12f, 0.75f - l * 0.08f, 1.15f - l * 0.12f);
            ApplyMaterial(leaf, l % 2 == 0 ? _matFoliageA : _matFoliageB);
            Object.Destroy(leaf.GetComponent<Collider>());
        }
    }

    private static void BuildRock(Transform root, Vector3 pos, float s)
    {
        var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rock.name = "Rock";
        rock.transform.SetParent(root);
        rock.transform.position = pos + Vector3.up * (s * 0.25f);
        rock.transform.localRotation = Quaternion.Euler(Random.Range(0f, 25f), Random.Range(0f, 180f), Random.Range(0f, 18f));
        rock.transform.localScale = new Vector3(s * 1.1f, s * 0.55f, s * 0.95f);
        ApplyMaterial(rock, _matRock);
        Object.Destroy(rock.GetComponent<Collider>());
    }

    private static void BuildReed(Transform root, Vector3 pos, float h)
    {
        var reed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        reed.name = "Reed";
        reed.transform.SetParent(root);
        reed.transform.position = pos + Vector3.up * (h * 0.25f);
        reed.transform.localScale = new Vector3(0.06f, h, 0.06f);
        ApplyMaterial(reed, _matReed);
        Object.Destroy(reed.GetComponent<Collider>());
    }

    private static void BuildLilyPad(Transform root, Vector3 pos, float r)
    {
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "LilyPad";
        pad.transform.SetParent(root);
        pad.transform.position = pos;
        pad.transform.localScale = new Vector3(r, 0.03f, r);
        ApplyMaterial(pad, _matLily);
        Object.Destroy(pad.GetComponent<Collider>());
    }
}
