using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 오픈월드 프로토타입 씬: 지형 또는 평면 바닥·Unity-Chan 스폰·<see cref="OpenWorldPlayerMotor"/>·1인칭 카메라·ARCHÉ 복귀 UI.
/// 이동은 Input System(키보드). Unity-Chan 레거시 스크립트는 모터에서 끕니다. Player Settings → Active Input Handling = Both 권장(에셋 호환).
/// </summary>
public sealed class OpenWorldBootstrap : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject _playerPrefab;

    [Tooltip("씬에 미리 둔 플레이어 인스턴스. 비우면 이름이 Player_UnityChan 인 오브젝트를 씬에서 찾고, 없으면 프리팹을 Instantiate 합니다.")]
    [SerializeField] private GameObject _playerPlacedInScene;

    [Tooltip("평면 바닥 모드일 때만 사용. 지형 모드에서는 무시되고 지형 중심 위 표면에 스폰합니다.")]
    [SerializeField] private Vector3 _spawnPosition = new Vector3(0f, 0.1f, 0f);

    [Tooltip("눈 높이(미터). 모델 스케일에 맞게 조정.")]
    [SerializeField] private float _firstPersonEyeHeight = 1.55f;

    [Header("World")]
    [Tooltip("체크 시 Perlin 지형 생성. 끄면 아래 평면 Plane만 사용.")]
    [SerializeField] private bool _useProceduralTerrain = true;

    [SerializeField] private bool _createGroundPlaneWhenNoTerrain = true;

    [SerializeField] private Vector2 _groundSize = new Vector2(200f, 200f);

    [Header("Terrain (지형)")]
    [Tooltip("비우면 플레이 시 씬에 있는 Terrain 을 자동으로 찾습니다. 여러 개일 때 이름이 OpenWorld_Terrain 인 것을 우선합니다. 여기에 넣으면 그 지형만 강제로 사용합니다.")]
    [SerializeField] private Terrain _sceneTerrain;

    [Tooltip("월드 가로·세로(미터), Y는 최대 높이 변화(미터).")]
    [SerializeField] private Vector3 _terrainWorldSize = new Vector3(512f, 42f, 512f);

    [SerializeField] private int _terrainHeightmapResolution = 257;

    [SerializeField] private int _terrainSeed = 202604;

    [Tooltip("스폰 중심 주변 이 반경(미터)은 완만한 평지로 블렌드.")]
    [SerializeField] private float _terrainSpawnFlatRadius = 32f;

    [SerializeField] private float _terrainNoiseScale = 0.022f;

    [SerializeField] private int _terrainNoiseOctaves = 4;

    [Tooltip("지형 표면 위 추가 오프셋(미터). 메시 발이 땅에 묻히지 않게.")]
    [SerializeField] private float _spawnYOffsetOnTerrain = 0.08f;

    [Tooltip("켜면 이전 플레이 종료 시 저장된 위치·방향에서 시작합니다. (persistentDataPath JSON)")]
    [SerializeField] private bool _useSpawnPersistence = true;

    [Header("Animator")]
    [Tooltip("Unity-Chan 기본 프리팹은 ARPose(정지) 컨트롤러입니다. 비우면 Resources/ArchE/OpenWorld/UnityChanLocomotions 를 사용합니다.")]
    [SerializeField] private RuntimeAnimatorController _locomotionAnimatorController;

    private Terrain _terrain;

    private static RuntimeAnimatorController _cachedLocomotionFromResources;

    private void Awake()
    {
        Vector3 defaultSpawn = _spawnPosition;

        if (_useProceduralTerrain)
        {
            _terrain = ResolveTerrainForPlay();
            defaultSpawn = ComputeDefaultSpawnWorld();
        }
        else if (_createGroundPlaneWhenNoTerrain)
        {
            CreateGroundPlane();
        }

        Vector3 spawnWorld = defaultSpawn;
        Quaternion spawnRot = Quaternion.identity;
        if (_useSpawnPersistence && OpenWorldSpawnPersistence.TryLoad(out Vector3 savedPos, out Quaternion savedRot))
        {
            spawnWorld = savedPos;
            spawnRot = savedRot;
        }

        Transform playerRoot = SpawnPlayer(spawnWorld, spawnRot);

        Transform fpPivot = SetupMainCamera(playerRoot);
        OpenWorldPlayerStance stance = playerRoot.GetComponent<OpenWorldPlayerStance>();
        if (stance != null && fpPivot != null)
        {
            stance.ConfigureEyeHeights(_firstPersonEyeHeight);
            stance.BindFirstPersonPivot(fpPivot);
        }

        BuildUi();
    }

    private Vector3 ComputeDefaultSpawnWorld()
    {
        if (!_useProceduralTerrain)
        {
            return _spawnPosition;
        }

        Vector3 terrainSize =
            _terrain != null && _terrain.terrainData != null ? _terrain.terrainData.size : _terrainWorldSize;
        float cx = terrainSize.x * 0.5f;
        float cz = terrainSize.z * 0.5f;
        float groundY = OpenWorldTerrainGenerator.GetSurfaceHeight(_terrain, cx, cz) + _spawnYOffsetOnTerrain;
        return new Vector3(cx, groundY, cz);
    }

    /// <summary>
    /// 1) 인스펙터에 Terrain 을 넣었으면 그대로 사용.
    /// 2) 아니면 씬에 배치된 Terrain 을 찾음 (이름 OpenWorld_Terrain 우선).
    /// 3) 없으면 런타임 절차 지형 생성.
    /// </summary>
    private Terrain ResolveTerrainForPlay()
    {
        if (_sceneTerrain != null)
        {
            return _sceneTerrain;
        }

        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (terrains != null && terrains.Length > 0)
        {
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain t = terrains[i];
                if (t != null && t.name == "OpenWorld_Terrain")
                {
                    return t;
                }
            }

            return terrains[0];
        }

        return OpenWorldTerrainGenerator.CreateTerrain(
            _terrainWorldSize,
            _terrainHeightmapResolution,
            _terrainSeed,
            _terrainSpawnFlatRadius,
            _terrainNoiseScale,
            _terrainNoiseOctaves);
    }

    private void OnDrawGizmos()
    {
        Vector3 p = GetSpawnPositionForGizmo();
        Gizmos.color = new Color(0.2f, 0.92f, 0.45f, 0.9f);
        Gizmos.DrawWireSphere(p, 0.4f);
        Gizmos.DrawLine(p, p + Vector3.up * 1.7f);

        GameObject scenePlayer = _playerPlacedInScene;
        if (scenePlayer == null)
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.scene == gameObject.scene && go.name == "Player_UnityChan")
                {
                    scenePlayer = go;
                    break;
                }
            }
        }

        if (scenePlayer != null)
        {
            Vector3 pp = scenePlayer.transform.position;
            Gizmos.color = new Color(0.95f, 0.82f, 0.15f, 0.95f);
            Gizmos.DrawWireSphere(pp, 0.45f);
        }
    }

    /// <summary>에디터에서 스폰 위치에 플레이어를 올릴 때 사용합니다. (씬에 Terrain 이 없으면 대략값)</summary>
    public Vector3 GetSpawnWorldPositionPreview()
    {
        return GetSpawnPositionForGizmo();
    }

    /// <summary>에디터 Scene 뷰에서 스폰 예상 위치를 그릴 때 사용 (Awake 와 동일한 규칙, 절차 지형은 씬에 없을 수 있음).</summary>
    private Vector3 GetSpawnPositionForGizmo()
    {
        if (!_useProceduralTerrain)
        {
            return _spawnPosition;
        }

        Terrain t = _sceneTerrain;
        if (t == null)
        {
            Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (terrains != null && terrains.Length > 0)
            {
                for (int i = 0; i < terrains.Length; i++)
                {
                    Terrain tr = terrains[i];
                    if (tr != null && tr.name == "OpenWorld_Terrain")
                    {
                        t = tr;
                        break;
                    }
                }

                if (t == null)
                {
                    t = terrains[0];
                }
            }
        }

        if (t != null && t.terrainData != null)
        {
            Vector3 size = t.terrainData.size;
            float cx = size.x * 0.5f;
            float cz = size.z * 0.5f;
            float groundY = OpenWorldTerrainGenerator.GetSurfaceHeight(t, cx, cz) + _spawnYOffsetOnTerrain;
            return new Vector3(cx, groundY, cz);
        }

        float fx = _terrainWorldSize.x * 0.5f;
        float fz = _terrainWorldSize.z * 0.5f;
        return new Vector3(fx, _spawnPosition.y, fz);
    }

    private void CreateGroundPlane()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "OpenWorld_Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(_groundSize.x / 10f, 1f, _groundSize.y / 10f);
        var renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.22f, 0.26f, 0.2f, 1f);
        }
    }

    private Transform SpawnPlayer(Vector3 position, Quaternion rotation)
    {
        GameObject instance = ResolveScenePlayerInstance();
        if (instance != null)
        {
            instance.transform.SetPositionAndRotation(position, rotation);
            ApplyLocomotionAnimator(instance);
            EnsureOpenWorldPlayerComponents(instance);
            return instance.transform;
        }

        if (_playerPrefab == null)
        {
            Debug.LogError(
                "[OpenWorld] Player Prefab 이 없고 씬에 Player_UnityChan 도 없습니다. OpenWorldBootstrap 에 프리팹을 넣거나 Tools → ARCHÉ → Open World → Place Player In Scene 을 실행하세요.");
            return transform;
        }

        GameObject spawned = Instantiate(_playerPrefab, position, rotation);
        spawned.name = "Player_UnityChan";
        ApplyLocomotionAnimator(spawned);
        EnsureOpenWorldPlayerComponents(spawned);
        return spawned.transform;
    }

    private GameObject ResolveScenePlayerInstance()
    {
        if (_playerPlacedInScene != null)
        {
            return _playerPlacedInScene;
        }

        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go == null || !go.scene.IsValid() || go.scene != gameObject.scene)
            {
                continue;
            }

            if (go.name == "Player_UnityChan")
            {
                return go;
            }
        }

        return null;
    }

    private static void EnsureOpenWorldPlayerComponents(GameObject instance)
    {
        if (instance.GetComponent<OpenWorldPlayerMotor>() == null)
        {
            instance.AddComponent<OpenWorldPlayerMotor>();
        }

        if (instance.GetComponent<OpenWorldPlayerStance>() == null)
        {
            instance.AddComponent<OpenWorldPlayerStance>();
        }

        if (instance.GetComponent<OpenWorldWeaponEquip>() == null)
        {
            instance.AddComponent<OpenWorldWeaponEquip>();
        }

        if (instance.GetComponent<OpenWorldPlayerStamina>() == null)
        {
            instance.AddComponent<OpenWorldPlayerStamina>();
        }

        if (instance.GetComponent<OpenWorldParkourController>() == null)
        {
            instance.AddComponent<OpenWorldParkourController>();
        }

        if (instance.GetComponent<OpenWorldPlayerSpawnRecorder>() == null)
        {
            instance.AddComponent<OpenWorldPlayerSpawnRecorder>();
        }

        if (instance.GetComponent<OpenWorldSectorDebug>() == null)
        {
            instance.AddComponent<OpenWorldSectorDebug>();
        }

        if (instance.GetComponent<OpenWorldSectorStreamingStub>() == null)
        {
            instance.AddComponent<OpenWorldSectorStreamingStub>();
        }

        if (instance.GetComponent<OpenWorldSectorAddressablesSample>() == null)
        {
            instance.AddComponent<OpenWorldSectorAddressablesSample>();
        }
    }

    private void ApplyLocomotionAnimator(GameObject playerInstance)
    {
        RuntimeAnimatorController controller = _locomotionAnimatorController;
        if (controller == null)
        {
            if (_cachedLocomotionFromResources == null)
            {
                _cachedLocomotionFromResources =
                    Resources.Load<RuntimeAnimatorController>("ArchE/OpenWorld/UnityChanLocomotions");
            }

            controller = _cachedLocomotionFromResources;
        }

        Animator anim = playerInstance.GetComponent<Animator>();
        if (anim == null)
        {
            return;
        }

        if (controller == null)
        {
            Debug.LogWarning(
                "[OpenWorld] Locomotion Animator Controller 를 찾지 못했습니다. OpenWorldBootstrap 에 UnityChanLocomotions 를 지정하거나 Resources/ArchE/OpenWorld/UnityChanLocomotions 가 있는지 확인하세요.");
            return;
        }

        anim.runtimeAnimatorController = controller;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        anim.updateMode = AnimatorUpdateMode.Fixed;
    }

    /// <summary>1인칭 피벗(시선 높이). 스탠스 연동을 위해 반환합니다.</summary>
    private Transform SetupMainCamera(Transform playerRoot)
    {
        Camera main = Camera.main;
        if (main == null)
        {
            var camGo = new GameObject("Main Camera");
            main = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
        }

        Transform pivot = new GameObject("FirstPersonPivot").transform;

        Animator animator = playerRoot.GetComponent<Animator>();
        Transform headBone = null;
        if (animator != null && animator.isHuman && animator.avatar != null && animator.avatar.isHuman)
        {
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        }

        if (headBone != null)
        {
            pivot.SetParent(headBone, false);
            pivot.localPosition = new Vector3(0f, 0.08f, 0.12f);
            pivot.localRotation = Quaternion.identity;
        }
        else
        {
            const float eyeForward = 0.16f;
            pivot.SetParent(playerRoot, false);
            pivot.localPosition = new Vector3(0f, _firstPersonEyeHeight, eyeForward);
            pivot.localRotation = Quaternion.identity;
        }

        main.transform.SetParent(pivot, false);
        main.transform.localPosition = Vector3.zero;
        main.transform.localRotation = Quaternion.identity;
        main.nearClipPlane = 0.01f;

        OpenWorldFirstPersonCamera fp = pivot.gameObject.GetComponent<OpenWorldFirstPersonCamera>();
        if (fp == null)
        {
            fp = pivot.gameObject.AddComponent<OpenWorldFirstPersonCamera>();
        }

        fp.SetPlayerRoot(playerRoot);

        OpenWorldFollowCamera follow = main.gameObject.GetComponent<OpenWorldFollowCamera>();
        if (follow == null)
        {
            follow = main.gameObject.AddComponent<OpenWorldFollowCamera>();
        }

        follow.enabled = false;

        OpenWorldCameraModeController mode = playerRoot.GetComponent<OpenWorldCameraModeController>();
        if (mode == null)
        {
            mode = playerRoot.gameObject.AddComponent<OpenWorldCameraModeController>();
        }

        mode.Initialize(playerRoot, main, pivot, fp, follow);
        return pivot;
    }

    private void BuildUi()
    {
        if (FindFirstObjectByType<Canvas>() != null)
        {
            return;
        }

        EnsureEventSystem();

        var canvasGo = new GameObject("OpenWorldCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var root = new GameObject("Root");
        root.transform.SetParent(canvasGo.transform, false);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(root.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.92f);
        titleRt.anchorMax = new Vector2(0.5f, 0.92f);
        titleRt.sizeDelta = new Vector2(900f, 56f);
        var title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "ARCHÉ — Open World (Prototype)";
        title.fontSize = 28;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.92f, 0.92f, 0.9f, 1f);
        if (TmpFontCache.LiberationSansSdf != null)
        {
            title.font = TmpFontCache.LiberationSansSdf;
        }

        CreateButton(root.transform, "ARCHÉ 메인으로", new Vector2(0.5f, 0.08f), OpenWorldSceneNavigation.LoadArchEMainHub);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();
    }

    private static void CreateButton(Transform parent, string label, Vector2 anchorCenter, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label.GetHashCode());
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorCenter;
        rt.anchorMax = anchorCenter;
        rt.sizeDelta = new Vector2(380f, 56f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.14f, 0.92f);

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            tmp.font = TmpFontCache.LiberationSansSdf;
        }
    }
}
