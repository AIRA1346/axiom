using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 숲속 호수 낚시 세션. 힐링 지향: 실패 페널티 없음, 짧은 문구로 재시도 유도.
/// 흐름: 준비 → 캐스팅 → 대기 → 입질(타이밍) → 릴링(홀드) → 결과.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class FishingSessionController : MonoBehaviour
{
    private const float WaterSurfaceY = 0.02f;
    private const float CastDuration = 0.45f;
    private const float BiteWindowSeconds = 2.4f;
    private const float FightTimeoutSeconds = 14f;

    private enum Phase
    {
        ReadyToCast,
        Casting,
        Waiting,
        BiteWindow,
        Fighting,
        Landed,
        SoftMiss,
    }

    [SerializeField] private Vector3 _rodTipLocal = new Vector3(0.22f, -0.12f, 0.55f);
    [SerializeField] private float _waitMin = 2.2f;
    [SerializeField] private float _waitMax = 5.8f;

    private Camera _camera;
    private FishingFirstPersonLook _look;
    private FishingPlayerMotor _motor;
    private LineRenderer _line;
    private Transform _bobber;
    private Renderer _bobberRenderer;

    private Phase _phase = Phase.ReadyToCast;
    private float _phaseTimer;
    private float _waitDuration;
    private Vector3 _bobberRest;
    private Vector3 _castFrom;
    private Vector3 _castTo;
    private FishingFishEntry _activeFish;
    private float _reelProgress;
    private float _biteTimer;

    private Canvas _hostCanvas;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _subText;
    private GameObject _reelRoot;
    private Image _reelFill;
    private GameObject _resultRoot;
    private TextMeshProUGUI _resultName;
    private TextMeshProUGUI _resultFlavor;
    private Button _resultDismiss;

    private static Material _lineMat;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _look = GetComponentInParent<FishingFirstPersonLook>();
        _motor = GetComponentInParent<FishingPlayerMotor>();
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        HealingFishingProgress.RegisterLakeVisit();
        BuildRodVisuals();
        EnsureGameplayUi();
        SetPhase(Phase.ReadyToCast, true);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        UpdatePhase();
        UpdateBobberMotion();
        if (_line != null && _bobber != null)
        {
            Vector3 tip = transform.TransformPoint(_rodTipLocal);
            _line.SetPosition(0, tip);
            _line.SetPosition(1, _bobber.position);
        }
    }

    private void BuildRodVisuals()
    {
        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.numCornerVertices = 2;
        _line.numCapVertices = 2;
        if (_lineMat == null)
        {
            var sh = Shader.Find("Sprites/Default");
            _lineMat = sh != null ? new Material(sh) : new Material(Shader.Find("Unlit/Color"));
        }

        _line.material = _lineMat;
        _line.startWidth = 0.012f;
        _line.endWidth = 0.008f;
        _line.startColor = new Color(0.35f, 0.28f, 0.2f, 0.85f);
        _line.endColor = new Color(0.45f, 0.38f, 0.28f, 0.75f);
        _line.useWorldSpace = true;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;

        var bob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bob.name = "FishingBobber";
        bob.transform.localScale = Vector3.one * 0.14f;
        Object.Destroy(bob.GetComponent<Collider>());
        _bobber = bob.transform;
        _bobberRenderer = bob.GetComponent<Renderer>();
        if (_bobberRenderer != null)
        {
            _bobberRenderer.material.color = new Color(0.95f, 0.35f, 0.32f, 1f);
        }

        PositionBobberAtRodTip();
    }

    private void EnsureGameplayUi()
    {
        var canvasGo = GameObject.Find("FishingHUD");
        if (canvasGo == null)
        {
            return;
        }

        _hostCanvas = canvasGo.GetComponent<Canvas>();
        var root = new GameObject("FishingGameplayUI");
        root.transform.SetParent(canvasGo.transform, false);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        _titleText = CreateTmp(root.transform, "Title", new Vector2(0.5f, 0.58f), 26f, TextAlignmentOptions.Center);
        _subText = CreateTmp(root.transform, "Sub", new Vector2(0.5f, 0.52f), 20f, TextAlignmentOptions.Center);

        _reelRoot = new GameObject("ReelBar");
        _reelRoot.transform.SetParent(root.transform, false);
        var reelRt = _reelRoot.AddComponent<RectTransform>();
        reelRt.anchorMin = new Vector2(0.5f, 0.12f);
        reelRt.anchorMax = new Vector2(0.5f, 0.12f);
        reelRt.sizeDelta = new Vector2(520f, 28f);
        var reelBg = _reelRoot.AddComponent<Image>();
        reelBg.color = new Color(0.12f, 0.14f, 0.18f, 0.88f);
        reelBg.raycastTarget = false;
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(_reelRoot.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(4f, 4f);
        fillRt.offsetMax = new Vector2(-4f, -4f);
        _reelFill = fillGo.AddComponent<Image>();
        _reelFill.color = new Color(0.45f, 0.72f, 0.55f, 0.95f);
        _reelFill.type = Image.Type.Filled;
        _reelFill.fillMethod = Image.FillMethod.Horizontal;
        _reelFill.fillAmount = 0f;
        _reelFill.raycastTarget = false;
        _reelRoot.SetActive(false);

        _resultRoot = new GameObject("ResultPanel");
        _resultRoot.transform.SetParent(root.transform, false);
        var resRt = _resultRoot.AddComponent<RectTransform>();
        resRt.anchorMin = Vector2.zero;
        resRt.anchorMax = Vector2.one;
        resRt.offsetMin = Vector2.zero;
        resRt.offsetMax = Vector2.zero;
        var dim = _resultRoot.AddComponent<Image>();
        dim.color = new Color(0.05f, 0.07f, 0.1f, 0.72f);
        dim.raycastTarget = true;

        _resultName = CreateTmp(_resultRoot.transform, "FishName", new Vector2(0.5f, 0.58f), 36f, TextAlignmentOptions.Center);
        _resultFlavor = CreateTmp(_resultRoot.transform, "Flavor", new Vector2(0.5f, 0.46f), 22f, TextAlignmentOptions.Center);
        _resultFlavor.rectTransform.sizeDelta = new Vector2(800f, 160f);

        var btnGo = new GameObject("AgainButton");
        btnGo.transform.SetParent(_resultRoot.transform, false);
        var brt = btnGo.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.28f);
        brt.anchorMax = new Vector2(0.5f, 0.28f);
        brt.sizeDelta = new Vector2(280f, 52f);
        var bImg = btnGo.AddComponent<Image>();
        bImg.color = new Color(0.28f, 0.42f, 0.36f, 0.95f);
        _resultDismiss = btnGo.AddComponent<Button>();
        _resultDismiss.targetGraphic = bImg;
        _resultDismiss.onClick.AddListener(OnDismissResult);

        var bt = new GameObject("Text");
        bt.transform.SetParent(btnGo.transform, false);
        var ttr = bt.AddComponent<RectTransform>();
        ttr.anchorMin = Vector2.zero;
        ttr.anchorMax = Vector2.one;
        ttr.offsetMin = Vector2.zero;
        ttr.offsetMax = Vector2.zero;
        var btmp = bt.AddComponent<TextMeshProUGUI>();
        btmp.text = "다시 낚시하기";
        btmp.fontSize = 22;
        btmp.alignment = TextAlignmentOptions.Center;
        btmp.color = Color.white;
        ApplyFont(btmp);

        _resultRoot.SetActive(false);
    }

    private static TextMeshProUGUI CreateTmp(Transform parent, string name, Vector2 anchorCenter, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorCenter;
        rt.anchorMax = anchorCenter;
        rt.sizeDelta = new Vector2(900f, 80f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = new Color(0.94f, 0.92f, 0.86f, 1f);
        tmp.raycastTarget = false;
        ApplyFont(tmp);
        return tmp;
    }

    private static void ApplyFont(TextMeshProUGUI tmp)
    {
        if (TmpFontCache.LiberationSansSdf != null)
        {
            tmp.font = TmpFontCache.LiberationSansSdf;
        }
    }

    private void OnDismissResult()
    {
        if (_resultRoot != null)
        {
            _resultRoot.SetActive(false);
        }
        SetLookEnabled(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetPhase(Phase.ReadyToCast, true);
    }

    private void SetLookEnabled(bool on)
    {
        if (_look != null)
        {
            _look.enabled = on;
        }

        if (_motor != null)
        {
            _motor.enabled = on;
        }
    }

    private void SetPhase(Phase p, bool resetTimer)
    {
        _phase = p;
        if (resetTimer)
        {
            _phaseTimer = 0f;
        }

        if (_reelRoot != null)
        {
            _reelRoot.SetActive(p == Phase.Fighting);
        }
        if (_reelFill != null)
        {
            _reelFill.fillAmount = 0f;
        }

        if (_titleText == null)
        {
            return;
        }

        switch (p)
        {
            case Phase.ReadyToCast:
                _titleText.text = "숲속의 호수";
                _subText.text = "마우스로 주위를 둘러보고, 좌클릭으로 낚시를 시작해요.";
                PositionBobberAtRodTip();
                break;
            case Phase.Casting:
                _titleText.text = "던지는 중…";
                _subText.text = string.Empty;
                break;
            case Phase.Waiting:
                _waitDuration = Random.Range(_waitMin, _waitMax);
                _titleText.text = "기다리기";
                _subText.text = "잔잔한 물결을 느껴보세요.";
                break;
            case Phase.BiteWindow:
                _biteTimer = BiteWindowSeconds;
                _titleText.text = "입질!";
                _subText.text = "지금! 좌클릭으로 챔!";
                break;
            case Phase.Fighting:
                _reelProgress = 0f;
                _titleText.text = "살살 당기기";
                _subText.text = "좌클릭을 누른 채로 릴을 감아요. 놓으면 조금씩 빠져나가요.";
                break;
            case Phase.Landed:
                break;
            case Phase.SoftMiss:
                break;
        }
    }

    private void UpdatePhase()
    {
        if (_titleText == null)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        bool click = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool hold = mouse != null && mouse.leftButton.isPressed;
        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        _phaseTimer += Time.deltaTime;

        switch (_phase)
        {
            case Phase.ReadyToCast:
                if (click && !overUi && TryGetWaterCastPoint(out Vector3 hit))
                {
                    _castFrom = _bobber.position;
                    _castTo = hit + Vector3.up * 0.06f;
                    SetPhase(Phase.Casting, true);
                }

                break;

            case Phase.Casting:
            {
                float t = Mathf.Clamp01(_phaseTimer / CastDuration);
                _bobber.position = Vector3.Lerp(_castFrom, _castTo, EaseOutQuad(t));
                if (t >= 1f)
                {
                    _bobberRest = _castTo;
                    SetPhase(Phase.Waiting, true);
                }

                break;
            }

            case Phase.Waiting:
                if (_phaseTimer >= _waitDuration)
                {
                    _activeFish = FishingForestLakeCatalog.RollCatch();
                    SetPhase(Phase.BiteWindow, true);
                }

                break;

            case Phase.BiteWindow:
                _biteTimer -= Time.deltaTime;
                if (click && !overUi)
                {
                    SetPhase(Phase.Fighting, true);
                    return;
                }

                if (_biteTimer <= 0f)
                {
                    ShowSoftMiss("산뜻하게 놓쳤어요. 다시 던져 볼까요?");
                }

                break;

            case Phase.Fighting:
            {
                float mul = Mathf.Max(0.5f, _activeFish.FightMultiplier);
                if (hold && !overUi)
                {
                    _reelProgress += Time.deltaTime * (0.34f / mul);
                }
                else
                {
                    _reelProgress -= Time.deltaTime * (0.13f * mul);
                }

                _reelProgress = Mathf.Clamp01(_reelProgress);
                if (_reelFill != null)
                {
                    _reelFill.fillAmount = _reelProgress;
                }

                if (_reelProgress >= 1f)
                {
                    ShowLanded();
                    return;
                }

                if (_phaseTimer >= FightTimeoutSeconds)
                {
                    ShowSoftMiss("줄이 천천히 풀렸어요. 천천히 다시 해볼까요?");
                }

                break;
            }

            case Phase.SoftMiss:
                if (_phaseTimer >= 1.8f)
                {
                    SetPhase(Phase.ReadyToCast, true);
                }

                break;

            case Phase.Landed:
                break;
        }
    }

    private void UpdateBobberMotion()
    {
        if (_bobber == null)
        {
            return;
        }

        switch (_phase)
        {
            case Phase.ReadyToCast:
            case Phase.Casting:
                return;
            case Phase.Waiting:
            {
                float wobble = Mathf.Sin(Time.time * 2.1f) * 0.04f + Mathf.Sin(Time.time * 3.7f) * 0.02f;
                _bobber.position = _bobberRest + new Vector3(0f, wobble, 0f);
                break;
            }

            case Phase.BiteWindow:
            {
                float dip = Mathf.Sin(Time.time * 18f) * 0.05f - 0.08f;
                _bobber.position = _bobberRest + new Vector3(0f, dip, 0f);
                if (_bobberRenderer != null)
                {
                    float pulse = 1f + Mathf.Sin(Time.time * 22f) * 0.08f;
                    _bobber.transform.localScale = Vector3.one * (0.14f * pulse);
                }

                break;
            }

            case Phase.Fighting:
            {
                float struggle = Mathf.Sin(Time.time * 14f) * 0.035f;
                _bobber.position = _bobberRest + new Vector3(struggle, Mathf.Sin(Time.time * 9f) * 0.03f, struggle * 0.5f);
                break;
            }

            default:
                _bobber.position = _bobberRest;
                if (_bobberRenderer != null)
                {
                    _bobber.transform.localScale = Vector3.one * 0.14f;
                }

                break;
        }
    }

    private void PositionBobberAtRodTip()
    {
        if (_bobber == null)
        {
            return;
        }

        Vector3 tip = transform.TransformPoint(_rodTipLocal);
        _bobber.position = tip;
        _bobberRest = tip;
        _bobber.localScale = Vector3.one * 0.14f;
    }

    private bool TryGetWaterCastPoint(out Vector3 worldHit)
    {
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        var plane = new Plane(Vector3.up, new Vector3(0f, WaterSurfaceY, 0f));
        if (plane.Raycast(ray, out float dist))
        {
            worldHit = ray.GetPoint(dist);
            worldHit.y = WaterSurfaceY + 0.06f;
            if (worldHit.z > 0.5f)
            {
                return true;
            }
        }

        worldHit = default;
        return false;
    }

    private void ShowSoftMiss(string msg)
    {
        if (_titleText != null)
        {
            _titleText.text = "잠깐의 쉼";
        }

        if (_subText != null)
        {
            _subText.text = msg;
        }

        SetPhase(Phase.SoftMiss, true);
    }

    private void ShowLanded()
    {
        HealingFishingProgress.RegisterCatch();
        if (_resultName != null)
        {
            _resultName.text = _activeFish.DisplayName;
        }

        if (_resultFlavor != null)
        {
            _resultFlavor.text = _activeFish.Flavor;
        }

        if (_resultRoot != null)
        {
            _resultRoot.SetActive(true);
        }
        SetLookEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPhase(Phase.Landed, true);
        _titleText.text = "낚였어요!";
        _subText.text = "도감은 차차 채워 나가요.";
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
}
