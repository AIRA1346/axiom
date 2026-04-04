using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 낚시(숲속 호수) 프로토 씬: 지면·물면·1인칭 카메라·안내 UI를 런타임에 구성합니다.
/// ExecuteAlways: 에디터(플레이 전)에서도 월드가 생성되어 씬 뷰에서 호수를 볼 수 있습니다.
/// </summary>
[ExecuteAlways]
public sealed class FishingLakeBootstrap : MonoBehaviour
{
    [SerializeField] private bool _rebuildEachAwake;

    private void Awake()
    {
        bool hasWorld = GameObject.Find("FishingWorldRoot") != null;

        if (!_rebuildEachAwake && hasWorld && !Application.isPlaying)
        {
            return;
        }

        if (!_rebuildEachAwake && hasWorld && Application.isPlaying)
        {
            EnsureEventSystem();
            SetupHud();
            EnablePlayModeFishingComponents();
            return;
        }

        SetupWorld();

        if (!Application.isPlaying)
        {
            return;
        }

        EnsureEventSystem();
        SetupHud();
    }

    private static void EnablePlayModeFishingComponents()
    {
        FishingSessionController session = Object.FindFirstObjectByType<FishingSessionController>();
        if (session != null)
        {
            session.enabled = true;
        }

        FishingFirstPersonLook look = Object.FindFirstObjectByType<FishingFirstPersonLook>();
        if (look != null)
        {
            look.enabled = true;
        }

        FishingPlayerMotor motor = Object.FindFirstObjectByType<FishingPlayerMotor>();
        if (motor != null)
        {
            motor.enabled = true;
        }
    }

    private static void SetupWorld()
    {
        if (GameObject.Find("FishingWorldRoot") != null)
        {
            return;
        }

        var root = new GameObject("FishingWorldRoot");

        var defaultDir = GameObject.Find("Directional Light");
        if (defaultDir != null)
        {
            defaultDir.SetActive(false);
        }

        FishingLakeEnvironmentBuilder.ApplyAtmosphere();
        FishingLakeEnvironmentBuilder.BuildTerrainAndWater(root.transform);
        FishingLakeEnvironmentBuilder.BuildForestRing(root.transform);
        FishingLakeEnvironmentBuilder.BuildShoreDetails(root.transform);
        FishingLakeEnvironmentBuilder.BuildFairyParticles(root.transform);

        var player = new GameObject("FishingPlayer");
        player.transform.SetParent(root.transform);
        player.transform.SetPositionAndRotation(new Vector3(0f, 0f, -2.5f), Quaternion.identity);

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.75f;
        cc.radius = 0.38f;
        cc.center = new Vector3(0f, 0.875f, 0f);
        cc.slopeLimit = 50f;
        cc.stepOffset = 0.38f;
        cc.minMoveDistance = 0f;

        var motor = player.AddComponent<FishingPlayerMotor>();

        var pivot = new GameObject("CameraPivot");
        pivot.transform.SetParent(player.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        pivot.AddComponent<FishingFirstPersonLook>();

        var camGo = new GameObject("FishingCamera");
        camGo.transform.SetParent(pivot.transform, false);
        camGo.transform.localPosition = Vector3.zero;
        camGo.transform.localRotation = Quaternion.identity;
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 72f;
        cam.nearClipPlane = 0.05f;
        cam.backgroundColor = RenderSettings.fogColor;
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<FishingSessionController>();

        var mainCamGo = GameObject.Find("Main Camera");
        if (mainCamGo != null && mainCamGo.GetComponent<Camera>() != cam)
        {
            mainCamGo.SetActive(false);
        }

        var lightGo = new GameObject("HealingSun");
        lightGo.transform.SetParent(root.transform);
        lightGo.transform.rotation = Quaternion.Euler(52f, -38f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.88f, 1f);
        light.intensity = 1.12f;
        light.shadows = LightShadows.Soft;

        if (!Application.isPlaying)
        {
            motor.enabled = false;
            FishingFirstPersonLook lookOnPivot = pivot.GetComponent<FishingFirstPersonLook>();
            if (lookOnPivot != null)
            {
                lookOnPivot.enabled = false;
            }

            FishingSessionController session = camGo.GetComponent<FishingSessionController>();
            if (session != null)
            {
                session.enabled = false;
            }
        }
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

    private static void SetupHud()
    {
        if (GameObject.Find("FishingHUD") != null)
        {
            return;
        }

        var canvasGo = new GameObject("FishingHUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("HintPanel");
        panel.transform.SetParent(canvasGo.transform, false);
        var prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 1f);
        prt.anchorMax = new Vector2(0.5f, 1f);
        prt.pivot = new Vector2(0.5f, 1f);
        prt.anchoredPosition = new Vector2(0f, -24f);
        prt.sizeDelta = new Vector2(920f, 120f);
        var hint = panel.AddComponent<TextMeshProUGUI>();
        hint.text = "WASD 이동 · Shift 달리기 · 마우스 시야 · 우클릭 커서 · 호수를 향해 좌클릭 낚시";
        hint.fontSize = 20;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(0.92f, 0.9f, 0.85f, 1f);
        if (TmpFontCache.LiberationSansSdf != null)
        {
            hint.font = TmpFontCache.LiberationSansSdf;
        }

        hint.raycastTarget = false;

        var btnGo = new GameObject("BackButton");
        btnGo.transform.SetParent(canvasGo.transform, false);
        var brt = btnGo.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 1f);
        brt.anchorMax = new Vector2(0f, 1f);
        brt.pivot = new Vector2(0f, 1f);
        brt.anchoredPosition = new Vector2(28f, -28f);
        brt.sizeDelta = new Vector2(220f, 48f);
        var bImg = btnGo.AddComponent<Image>();
        bImg.color = new Color(0.2f, 0.22f, 0.28f, 0.92f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = bImg;
        btn.onClick.AddListener(HealingSceneNavigation.LoadHealingHub);

        var bt = new GameObject("Text");
        bt.transform.SetParent(btnGo.transform, false);
        var ttr = bt.AddComponent<RectTransform>();
        ttr.anchorMin = Vector2.zero;
        ttr.anchorMax = Vector2.one;
        ttr.offsetMin = Vector2.zero;
        ttr.offsetMax = Vector2.zero;
        var btmp = bt.AddComponent<TextMeshProUGUI>();
        btmp.text = "힐링 메뉴";
        btmp.fontSize = 20;
        btmp.alignment = TextAlignmentOptions.Center;
        btmp.color = Color.white;
        if (TmpFontCache.LiberationSansSdf != null)
        {
            btmp.font = TmpFontCache.LiberationSansSdf;
        }
    }
}
