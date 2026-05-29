using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 濡쒕퉬 ??硫붿씤 硫붾돱: G.S.I ?쒖꽕 ?낆옣 諛?寃쎌젣 ?띿뒪???쒖떆. ?ㅽ겕/?쇱씠???멸? 諛섏쁺.
/// </summary>
[ExecuteAlways]
public sealed class MainMenuController : MonoBehaviour
{
    private const string LobbyActionRowName = "LobbyActionRow";
    private const string LobbyActionPrimaryRowLegacyName = "LobbyActionPrimaryRow";
    private const string LobbyActionSecondaryRowLegacyName = "LobbyActionSecondaryRow";

    /// <summary>Bottom action row: inset from screen bottom, height, horizontal inset (each side).</summary>
    private const float LobbyActionRowBottomInset = 12f;
    private const float LobbyActionRowHeight = 108f;
    private const float LobbyActionRowSideInset = 28f;

    /// <summary>Economy strip (Stardust / ticket) inset from the top edge of the lobby panel.</summary>
    private const float LobbyEconomyStripTopInset = 20f;
    private const string LobbyLabelRulesRootName = "LobbyLabelRules";
    private const string LobbyLabelRuleTopName = "LobbyLabelRuleTop";
    private const string LobbyLabelRuleBottomName = "LobbyLabelRuleBottom";
    private const string LobbyBackdropObjectName = "LobbyBackdrop";
    private const string LobbyHeaderBarLegacyName = "LobbyHeaderBar";
    private const string LobbyEconomyStripName = "LobbyEconomyStrip";
    private const string LobbyEconomyStripSpacerName = "LobbyEconomyStripSpacer";
    /// <summary>Strip ?덉뿉??怨⑤뱶/?곗폆?????⑹뼱由щ줈 臾띠뼱 ?곗긽?⑥뿉 怨좎젙 ??쑝濡?諛곗튂?⑸땲??(CSF+HLG 瑗ъ엫 諛⑹?).</summary>
    private const string LobbyEconomyRightClusterName = "LobbyEconomyRightCluster";
    private const float LobbyEconomyClusterMinWidth = 520f;
    private const float LobbyEconomyClusterPreferredWidth = 600f;
    private const float LobbyEconomyLabelMinWidth = 200f;
    private const float LobbyEconomyLabelPreferredWidth = 280f;
    private const string LobbyTopLeftBarName = "LobbyTopLeftBar";
    /// <summary>怨⑤뱶/?묒떆沅?以꾧낵 ?숈씪( <see cref="UpdateEconomyTexts"/> ??24f ).</summary>
    private const float LobbyTopLeftFontSize = 24f;
    private const float LobbyTopLeftTitleClockGap = 12f;
    /// <summary>濡쒕퉬 媛濡쒖쓽 ?쇰?留??ъ슜 ???곷떒 怨⑤뱶 ?곸뿭怨?寃뱀튂吏 ?딄쾶 ?〓땲??</summary>
    private const float LobbyTopLeftWidthFraction = 0.5f;
    /// <summary>?쒓퀎(?걔룹썡쨌?셋룹떆媛? ?댁씠 ?덈Т ?뉗븘吏??寃껋쓣 留됰뒗 理쒖넖媛??ㅼ젣???띿뒪?몄뿉 留욎땄).</summary>
    private const float LobbyTopLeftTimeMinWidth = 200f;
    private const float LobbyTopLeftBarMaxHeight = 300f;
    private const string LobbyCenterStageName = "LobbyCenterStage";
    private const string LobbyLayerBaseName = "Layer_Base";
    private const string LobbyLayerFarName = "Layer_Far";
    private const string LobbyLayerMidName = "Layer_Mid";
    private const string LobbyLayerNearName = "Layer_Near";
    private const string LobbyLayerGlowName = "Layer_Glow";
    private const string LobbyLayerVignetteName = "Layer_Vignette";
    /// <summary>Parallax art ?? 湲濡쒖슦쨌?쒕ぉ蹂대떎 ?꾨옒 ??硫붿씤 ?꾪듃瑜??댁쭩 ?꾨Ⅴ??UI??諛섑닾紐?硫?</summary>
    private const string LobbyLayerUiScrimName = "Layer_UiScrim";
    private const string LobbyDecorRootName = "Decor_Root";
    private const string LobbyDecorBrandingName = "LobbyBrandingTitle";
    /// <summary>Resources.Load path (no extension) for <c>Layer_Far</c> when override is not set.</summary>
    private const string LobbyFarDistantResourcePath = "Art/Lobby/lobby_far_distant";
    private const string LobbyMidgroundResourcePath = "Art/Lobby/lobby_midground";
    private const string LobbyNeargroundResourcePath = "Art/Lobby/lobby_nearground";
    private const float LobbyEconomyStripHeight = 48f;
    /// <summary>Extra gap between economy strip and center background artboard.</summary>
    private const float LobbyCenterStageExtraTopGap = 14f;
    private static readonly Color LobbyActionButtonBackgroundClear = new Color(1f, 1f, 1f, 0f);

    [Header("G.S.I")]
    [Tooltip("?좊떦 ??G.S.I ?쒖꽕 ?ъ쑝濡??대룞?⑸땲??")]
    [SerializeField] private Button _enterGsiFacilityButton;

    [Tooltip("?좊떦 ???곸젏 ???묒떆沅뙿룹뒪???쇰줈 ?대룞?⑸땲??")]
    [SerializeField] private Button _openShopButton;

    [Tooltip("?좊떦 ???몃깽?좊━ ??怨⑤뱶쨌?묒떆沅뙿룹뒪???쇰줈 ?대룞?⑸땲??")]
    [SerializeField] private Button _openInventoryButton;

    [Tooltip("?좊떦 ???듯빀 ?쒗뿕 湲곕줉 ??Altar of Verity)?쇰줈 ?대룞?⑸땲??")]
    [SerializeField] private Button _openAltarOfVerityButton;

    [SerializeField] private Button _practiceButton;
    [SerializeField] private Button _aimPracticeButton;
    [SerializeField] private Button _reactionExamButton;
    [SerializeField] private Button _aimExamButton;

    [SerializeField] private TextMeshProUGUI _tokenText;
    [SerializeField] private TextMeshProUGUI _ticketText;

    [Header("Audio")]
    [Tooltip("Lobby BGM; uses GsiAudio music channel (GsiUserSettings music volume).")]
    [SerializeField] private AudioClip _lobbyBackgroundMusic;

    [Header("Lobby center art")]
    [Tooltip("If set, used as Layer_Far; otherwise Resources path Art/Lobby/lobby_far_distant is loaded.")]
    [SerializeField] private Sprite _lobbyFarDistantSpriteOverride;

    [Tooltip("If set, used as Layer_Mid; otherwise Resources path Art/Lobby/lobby_midground is loaded.")]
    [SerializeField] private Sprite _lobbyMidgroundSpriteOverride;

    [Tooltip("If set, used as Layer_Near; otherwise Resources path Art/Lobby/lobby_nearground is loaded.")]
    [SerializeField] private Sprite _lobbyNeargroundSpriteOverride;

    [Header("Lobby center entrance")]
    [SerializeField] private bool _lobbyCenterEntranceFade = true;
    [SerializeField] private float _lobbyCenterEntranceDuration = 0.45f;

    [Header("Lobby action row entrance")]
    [Tooltip("Bottom button row (Start, Shop, ?? fades in after the center, so Stardust/ticket stay readable.")]
    [SerializeField] private bool _lobbyActionRowEntranceFade = true;
    [SerializeField] private float _lobbyActionRowEntranceDelay = 0.1f;
    [SerializeField] private float _lobbyActionRowEntranceDuration = 0.3f;

    private RectTransform _lobbyRoot;
    private Image _panelBackground;
    private Image _heroImage;
    private bool _lobbyShellBuilt;
    private static Sprite _proceduralSpaceSprite;
    private TextMeshProUGUI _lobbyTopLeftTitleTmp;
    private TextMeshProUGUI _lobbyTopLeftTimeTmp;
    private long _lobbyClockSecondStamp = -1L;
    private float _lobbyTopLeftBarLastLayoutWidth = -1f;
    private bool _appearanceSubscribed;
    private bool _localeSubscribed;
    private LobbyCenterStageScaffold _lobbyCenterStage;
    private Coroutine _lobbyEntranceSequenceRoutine;

    private void Awake()
    {
        _lobbyRoot = transform.parent as RectTransform;
        WireOptionalHierarchyButtons();
        CacheLobbyVisualRefs();

        if (_practiceButton != null)
        {
            _practiceButton.onClick.AddListener(OnPracticeClicked);
        }

        if (_aimPracticeButton != null)
        {
            _aimPracticeButton.onClick.AddListener(OnAimPracticeClicked);
        }

        if (_reactionExamButton != null)
        {
            _reactionExamButton.onClick.AddListener(OnReactionExamClicked);
        }

        if (_aimExamButton != null)
        {
            _aimExamButton.onClick.AddListener(OnAimExamClicked);
        }

        if (_enterGsiFacilityButton != null)
        {
            _enterGsiFacilityButton.onClick.AddListener(OnEnterGsiFacilityClicked);
        }

        if (_openShopButton != null)
        {
            _openShopButton.onClick.AddListener(OnOpenShopClicked);
        }

        GlobalSettingsOverlay.EnsureCreated();
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (_lobbyRoot == null)
        {
            _lobbyRoot = transform.parent as RectTransform;
            CacheLobbyVisualRefs();
        }

        GameLocalization.TryInitializeSynchronouslyForEditorSceneView();
        EnsureLobbyShell();
        WireOptionalHierarchyButtons();
        ApplyLobbyChrome();
        UpdateEconomyTexts();
        MarkLobbySceneDirtyIfNeeded();
    }
#endif

    private void WireOptionalHierarchyButtons()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        if (_enterGsiFacilityButton == null)
        {
            Transform t = _lobbyRoot.Find("GSIButton");
            if (t != null)
            {
                _enterGsiFacilityButton = t.GetComponent<Button>();
            }
        }

        if (_openShopButton == null)
        {
            Transform t = _lobbyRoot.Find("ShopButton");
            if (t != null)
            {
                _openShopButton = t.GetComponent<Button>();
            }
        }

        if (_openInventoryButton == null)
        {
            Transform t = _lobbyRoot.Find("InventoryButton");
            if (t != null)
            {
                _openInventoryButton = t.GetComponent<Button>();
            }
        }

        if (_openAltarOfVerityButton == null)
        {
            Transform t = _lobbyRoot.Find($"{LobbyActionRowName}/AltarOfVerityButton");
            if (t == null)
            {
                t = _lobbyRoot.Find("AltarOfVerityButton");
            }

            if (t != null)
            {
                _openAltarOfVerityButton = t.GetComponent<Button>();
            }
        }
    }

    private void CacheLobbyVisualRefs()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        _panelBackground = _lobbyRoot.GetComponent<Image>();
        Transform hero = _lobbyRoot.Find("Image");
        if (hero != null)
        {
            _heroImage = hero.GetComponent<Image>();
        }
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            Cursor.visible = true;
        }

        EnsureLobbyShell();
        if (Application.isPlaying && _lobbyRoot != null)
        {
            Transform stageTf = _lobbyRoot.Find(LobbyCenterStageName);
            if (stageTf != null)
            {
                PrepareLobbyCenterStageEntrance(stageTf);
            }

            Transform actionRow = _lobbyRoot.Find(LobbyActionRowName);
            if (actionRow != null)
            {
                PrepareLobbyActionRowEntrance(actionRow);
            }
        }

        WireOptionalHierarchyButtons();
        ApplyLobbyChrome();
        TrySubscribeAppearance();

        if (_enterGsiFacilityButton != null)
        {
            HideLegacyGsiEntryButtons();
        }

        if (_openInventoryButton != null)
        {
            _openInventoryButton.onClick.AddListener(OnOpenInventoryClicked);
        }

        if (_openAltarOfVerityButton != null)
        {
            _openAltarOfVerityButton.onClick.AddListener(OnOpenAltarOfVerityClicked);
        }

        UpdateEconomyTexts();
        TrySubscribeLocaleChanged();

        if (Application.isPlaying && (_lobbyCenterEntranceFade || _lobbyActionRowEntranceFade))
        {
            _lobbyEntranceSequenceRoutine = StartCoroutine(CoLobbyEntranceSequence());
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnEconomyChanged += HandleEconomyChanged;
        }

        TryPlayLobbyBgm();
    }

    private void OnDestroy()
    {
        if (_lobbyEntranceSequenceRoutine != null)
        {
            StopCoroutine(_lobbyEntranceSequenceRoutine);
            _lobbyEntranceSequenceRoutine = null;
        }

        TryUnsubscribeLocaleChanged();
        TryUnsubscribeAppearance();

        if (_practiceButton != null)
        {
            _practiceButton.onClick.RemoveListener(OnPracticeClicked);
        }

        if (_aimPracticeButton != null)
        {
            _aimPracticeButton.onClick.RemoveListener(OnAimPracticeClicked);
        }

        if (_reactionExamButton != null)
        {
            _reactionExamButton.onClick.RemoveListener(OnReactionExamClicked);
        }

        if (_aimExamButton != null)
        {
            _aimExamButton.onClick.RemoveListener(OnAimExamClicked);
        }

        if (_enterGsiFacilityButton != null)
        {
            _enterGsiFacilityButton.onClick.RemoveListener(OnEnterGsiFacilityClicked);
        }

        if (_openShopButton != null)
        {
            _openShopButton.onClick.RemoveListener(OnOpenShopClicked);
        }

        if (_openInventoryButton != null)
        {
            _openInventoryButton.onClick.RemoveListener(OnOpenInventoryClicked);
        }

        if (_openAltarOfVerityButton != null)
        {
            _openAltarOfVerityButton.onClick.RemoveListener(OnOpenAltarOfVerityClicked);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnEconomyChanged -= HandleEconomyChanged;
        }
    }

    private void TryPlayLobbyBgm()
    {
        if (!Application.isPlaying || _lobbyBackgroundMusic == null)
        {
            return;
        }

        GsiAudio.PlayMusic(_lobbyBackgroundMusic, loop: true);
    }

    private void TrySubscribeAppearance()
    {
        if (_appearanceSubscribed)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }
#endif
        GsiUiAppearance.Changed += OnLobbyAppearanceChanged;
        _appearanceSubscribed = true;
    }

    private void TryUnsubscribeAppearance()
    {
        if (!_appearanceSubscribed)
        {
            return;
        }

        GsiUiAppearance.Changed -= OnLobbyAppearanceChanged;
        _appearanceSubscribed = false;
    }

    private void OnLobbyAppearanceChanged()
    {
        ApplyLobbyChrome();
    }

    private void TrySubscribeLocaleChanged()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }
#endif
        if (_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged += OnLobbyLocaleChanged;
        _localeSubscribed = true;
    }

    private void TryUnsubscribeLocaleChanged()
    {
        if (!_localeSubscribed)
        {
            return;
        }

        GameLocalization.UiLocaleChanged -= OnLobbyLocaleChanged;
        _localeSubscribed = false;
    }

    private void OnLobbyLocaleChanged()
    {
        ApplyLobbyChrome();
    }

    private void EnsureLobbyShell()
    {
        if (_lobbyShellBuilt || _lobbyRoot == null)
        {
            return;
        }

        RemoveLegacyLobbyHeaderAndSettings();
        BuildLobbyActionRow();
        EnsureLobbyActionRowLayout();
        EnsureLobbyEconomyStrip();
        EnsureLobbyTopLeftTitleAndClock();
        EnsureLobbyCenterStage();
        BringLobbyInteractiveUiInFront();

        _lobbyShellBuilt = true;
    }

    private void RemoveLegacyLobbyHeaderAndSettings()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        Transform legacyHeader = _lobbyRoot.Find(LobbyHeaderBarLegacyName);
        if (legacyHeader != null)
        {
            if (_tokenText != null && _tokenText.transform.IsChildOf(legacyHeader))
            {
                _tokenText.transform.SetParent(_lobbyRoot, false);
            }

            if (_ticketText != null && _ticketText.transform.IsChildOf(legacyHeader))
            {
                _ticketText.transform.SetParent(_lobbyRoot, false);
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(legacyHeader.gameObject);
            }
            else
#endif
            {
                Destroy(legacyHeader.gameObject);
            }
        }

        Transform looseSettings = _lobbyRoot.Find("SettingsButton");
        if (looseSettings != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(looseSettings.gameObject);
            }
            else
#endif
            {
                Destroy(looseSettings.gameObject);
            }
        }
    }

    private void EnsureLobbyEconomyStrip()
    {
        if (_lobbyRoot == null || (_tokenText == null && _ticketText == null))
        {
            return;
        }

        Transform stripTf = _lobbyRoot.Find(LobbyEconomyStripName);
        RectTransform stripRt;
        if (stripTf == null)
        {
            var stripGo = new GameObject(LobbyEconomyStripName);
            stripRt = stripGo.AddComponent<RectTransform>();
            stripRt.SetParent(_lobbyRoot, false);
            ApplyLobbyEconomyStripRect(stripRt);

            var hor = stripGo.AddComponent<HorizontalLayoutGroup>();
            hor.spacing = 36f;
            hor.padding = new RectOffset(20, (int)LobbyActionRowSideInset, 6, 6);
            hor.childAlignment = TextAnchor.MiddleLeft;
            hor.childControlWidth = true;
            hor.childControlHeight = true;
            hor.childForceExpandWidth = false;
            hor.childForceExpandHeight = true;
        }
        else
        {
            stripRt = stripTf.GetComponent<RectTransform>();
            ApplyLobbyEconomyStripRect(stripRt);
            if (!stripTf.TryGetComponent(out HorizontalLayoutGroup existingHor))
            {
                existingHor = stripTf.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            existingHor.spacing = 36f;
            existingHor.padding = new RectOffset(20, (int)LobbyActionRowSideInset, 6, 6);
            existingHor.childAlignment = TextAnchor.MiddleLeft;
            existingHor.childControlWidth = true;
            existingHor.childControlHeight = true;
            existingHor.childForceExpandWidth = false;
            existingHor.childForceExpandHeight = true;
        }

        // Background: no layout slot; only spacer (flex) + right cluster (fixed) participate in the strip HLG.
        EnsureEconomyStripBackground(stripRt);
        EnsureLobbyEconomyRightCluster(stripRt);
        EnsureLobbyEconomyStripSpacer(stripRt);
        EnforceLobbyEconomyStripChildOrder(stripRt);
        stripRt.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(stripRt);
    }

    /// <summary>Pushes Stardust/ticket to the right edge of the full-width economy strip (flexible spacer on the left).</summary>
    private static void EnsureLobbyEconomyStripSpacer(RectTransform stripRt)
    {
        if (stripRt == null)
        {
            return;
        }

        Transform spacerTf = stripRt.Find(LobbyEconomyStripSpacerName);
        RectTransform spacerRt;
        if (spacerTf == null)
        {
            var go = new GameObject(LobbyEconomyStripSpacerName);
            spacerRt = go.AddComponent<RectTransform>();
            spacerRt.SetParent(stripRt, false);
        }
        else
        {
            spacerRt = spacerTf.GetComponent<RectTransform>();
        }

        LayoutElement le = spacerRt.GetComponent<LayoutElement>();
        if (le == null)
        {
            le = spacerRt.gameObject.AddComponent<LayoutElement>();
        }

        le.minWidth = 0f;
        le.preferredWidth = 0f;
        le.flexibleWidth = 1f;
        le.minHeight = 0f;
        le.flexibleHeight = 0f;

        // Keep spacer as the <b>first layout child</b> after the background, which is ignored for layout.
        Transform stripBg = stripRt.Find("StripBg");
        if (stripBg != null)
        {
            spacerRt.SetSiblingIndex(stripBg.GetSiblingIndex() + 1);
        }
        else
        {
            spacerRt.SetAsFirstSibling();
        }
    }

    private void EnsureLobbyEconomyRightCluster(RectTransform stripRt)
    {
        if (stripRt == null)
        {
            return;
        }

        Transform clusterTf = stripRt.Find(LobbyEconomyRightClusterName);
        RectTransform clusterRt;
        if (clusterTf == null)
        {
            var go = new GameObject(LobbyEconomyRightClusterName);
            clusterRt = go.AddComponent<RectTransform>();
            clusterRt.SetParent(stripRt, false);
            clusterRt.localScale = Vector3.one;
            var hor = go.AddComponent<HorizontalLayoutGroup>();
            hor.spacing = 28f;
            hor.childAlignment = TextAnchor.MiddleLeft;
            hor.childControlWidth = true;
            hor.childControlHeight = true;
            hor.childForceExpandWidth = false;
            hor.childForceExpandHeight = true;
            var clusterLe = go.AddComponent<LayoutElement>();
            clusterLe.minWidth = LobbyEconomyClusterMinWidth;
            clusterLe.preferredWidth = LobbyEconomyClusterPreferredWidth;
            clusterLe.flexibleWidth = 0f;
            clusterLe.minHeight = 0f;
        }
        else
        {
            clusterRt = (RectTransform)clusterTf;
            if (!clusterRt.TryGetComponent(out HorizontalLayoutGroup h))
            {
                h = clusterRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            h.spacing = 28f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            if (!clusterRt.TryGetComponent(out LayoutElement clusterLe))
            {
                clusterLe = clusterRt.gameObject.AddComponent<LayoutElement>();
            }

            clusterLe.minWidth = LobbyEconomyClusterMinWidth;
            clusterLe.preferredWidth = LobbyEconomyClusterPreferredWidth;
            clusterLe.flexibleWidth = 0f;
        }

        if (_tokenText != null)
        {
            _tokenText.transform.SetParent(clusterRt, false);
            RemoveContentSizeFitterIfAny(_tokenText.gameObject);
            _tokenText.alignment = TextAlignmentOptions.MidlineLeft;
            _tokenText.enableWordWrapping = false;
            _tokenText.overflowMode = TextOverflowModes.Overflow;
            if (!_tokenText.gameObject.TryGetComponent(out LayoutElement tLe))
            {
                tLe = _tokenText.gameObject.AddComponent<LayoutElement>();
            }

            tLe.minWidth = LobbyEconomyLabelMinWidth;
            tLe.preferredWidth = LobbyEconomyLabelPreferredWidth;
            tLe.flexibleWidth = 0f;
        }

        if (_ticketText != null)
        {
            _ticketText.transform.SetParent(clusterRt, false);
            RemoveContentSizeFitterIfAny(_ticketText.gameObject);
            _ticketText.alignment = TextAlignmentOptions.MidlineLeft;
            _ticketText.enableWordWrapping = false;
            _ticketText.overflowMode = TextOverflowModes.Overflow;
            if (!_ticketText.gameObject.TryGetComponent(out LayoutElement kLe))
            {
                kLe = _ticketText.gameObject.AddComponent<LayoutElement>();
            }

            kLe.minWidth = LobbyEconomyLabelMinWidth;
            kLe.preferredWidth = LobbyEconomyLabelPreferredWidth;
            kLe.flexibleWidth = 0f;
        }
    }

    private static void EnforceLobbyEconomyStripChildOrder(Transform stripRt)
    {
        if (stripRt == null)
        {
            return;
        }

        Transform bgTf = stripRt.Find("StripBg");
        Transform spacerTf = stripRt.Find(LobbyEconomyStripSpacerName);
        Transform clusterTf = stripRt.Find(LobbyEconomyRightClusterName);
        if (bgTf != null)
        {
            bgTf.SetSiblingIndex(0);
        }

        if (spacerTf != null)
        {
            if (bgTf != null)
            {
                spacerTf.SetSiblingIndex(bgTf.GetSiblingIndex() + 1);
            }
            else
            {
                spacerTf.SetAsFirstSibling();
            }
        }

        if (clusterTf != null)
        {
            if (spacerTf != null)
            {
                clusterTf.SetSiblingIndex(spacerTf.GetSiblingIndex() + 1);
            }
            else if (bgTf != null)
            {
                clusterTf.SetSiblingIndex(bgTf.GetSiblingIndex() + 1);
            }
        }
    }

    private static void RemoveContentSizeFitterIfAny(GameObject go)
    {
        if (go == null)
        {
            return;
        }

        if (!go.TryGetComponent(out ContentSizeFitter csf))
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEngine.Object.DestroyImmediate(csf, true);
        }
        else
#endif
        {
            UnityEngine.Object.Destroy(csf);
        }
    }

    private static void EnsureEconomyStripBackground(RectTransform stripRt)
    {
        if (stripRt == null)
        {
            return;
        }

        Transform stripRoot = stripRt;
        Transform bgTf = stripRoot.Find("StripBg");
        if (bgTf == null)
        {
            var bgGo = new GameObject("StripBg");
            RectTransform bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.SetParent(stripRoot, false);
            GsiUiRuntimeWidgets.StretchFull(bgRt);
            var img = bgGo.AddComponent<Image>();
            img.color = GsiUiAppearance.LobbyEconomyStripGlass;
            img.raycastTarget = false;
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
            LayoutElement bgLe = bgGo.GetComponent<LayoutElement>();
            if (bgLe == null)
            {
                bgLe = bgGo.AddComponent<LayoutElement>();
            }

            bgLe.ignoreLayout = true;
            bgRt.SetAsFirstSibling();
        }
        else
        {
            if (bgTf.TryGetComponent(out Image existing))
            {
                existing.color = GsiUiAppearance.LobbyEconomyStripGlass;
                GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(existing);
            }

            if (bgTf.TryGetComponent(out LayoutElement bgLe))
            {
                bgLe.ignoreLayout = true;
            }
            else
            {
                bgLe = bgTf.gameObject.AddComponent<LayoutElement>();
                bgLe.ignoreLayout = true;
            }
        }
    }

    private void EnsureLobbyTopLeftTitleAndClock()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        Transform barTf = _lobbyRoot.Find(LobbyTopLeftBarName);
        if (barTf == null)
        {
            var barGo = new GameObject(LobbyTopLeftBarName);
            RectTransform barRt = barGo.AddComponent<RectTransform>();
            barRt.SetParent(_lobbyRoot, false);
            ApplyLobbyTopLeftBarRect(barRt);

            var h = barGo.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.UpperLeft;
            h.spacing = LobbyTopLeftTitleClockGap;
            h.padding = new RectOffset(0, 0, 0, 0);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            const string titleChildName = "LobbyTopLeftTitle";
            const string clockChildName = "LobbyTopLeftClock";
            _lobbyTopLeftTitleTmp = CreateLobbyTopLeftTitleText(titleChildName, barRt.transform);
            LayoutElement titleLe = _lobbyTopLeftTitleTmp.gameObject.GetComponent<LayoutElement>();
            if (titleLe == null)
            {
                titleLe = _lobbyTopLeftTitleTmp.gameObject.AddComponent<LayoutElement>();
            }

            titleLe.minWidth = 48f;
            titleLe.preferredWidth = 48f;
            titleLe.flexibleWidth = 0f;
            titleLe.minHeight = LobbyTopLeftFontSize;

            _lobbyTopLeftTimeTmp = CreateLobbyTopLeftClockText(clockChildName, barRt.transform);
            LayoutElement timeLe = _lobbyTopLeftTimeTmp.gameObject.GetComponent<LayoutElement>();
            if (timeLe == null)
            {
                timeLe = _lobbyTopLeftTimeTmp.gameObject.AddComponent<LayoutElement>();
            }

            timeLe.minWidth = LobbyTopLeftTimeMinWidth;
            timeLe.preferredWidth = LobbyTopLeftTimeMinWidth;
            timeLe.flexibleWidth = 0f;
            timeLe.minHeight = LobbyTopLeftFontSize;

            EnforceLobbyTopLeftTitleBeforeClock(barRt.transform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(barRt);
        }
        else
        {
            if (_lobbyTopLeftTitleTmp == null)
            {
                Transform t = barTf.Find("LobbyTopLeftTitle");
                if (t != null)
                {
                    _lobbyTopLeftTitleTmp = t.GetComponent<TextMeshProUGUI>();
                }
            }

            if (_lobbyTopLeftTimeTmp == null)
            {
                Transform t = barTf.Find("LobbyTopLeftClock");
                if (t != null)
                {
                    _lobbyTopLeftTimeTmp = t.GetComponent<TextMeshProUGUI>();
                }
            }

            UpgradeLegacyLobbyTopLeftLayout(barTf);
            EnforceLobbyTopLeftTitleBeforeClock(barTf);
            if (barTf.TryGetComponent(out RectTransform existingBarRt))
            {
                ApplyLobbyTopLeftBarRect(existingBarRt);
            }
        }

        _lobbyTopLeftBarLastLayoutWidth = -1f;
        RefreshLobbyTopLeftTitleAndTime();
    }

    private void ApplyLobbyTopLeftBarRect(RectTransform barRt)
    {
        if (_lobbyRoot == null || barRt == null)
        {
            return;
        }

        float panelW = _lobbyRoot.rect.width;
        float inner = LobbyActionRowSideInset;
        float barW = Mathf.Max(120f, panelW * LobbyTopLeftWidthFraction - 2f * inner);
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(0f, 1f);
        barRt.pivot = new Vector2(0f, 1f);
        barRt.anchoredPosition = new Vector2(inner, -LobbyEconomyStripTopInset);
        barRt.sizeDelta = new Vector2(barW, 80f);
    }

    private static void UpgradeLegacyLobbyTopLeftLayout(Transform barTf)
    {
        if (barTf == null)
        {
            return;
        }

        if (!barTf.TryGetComponent(out HorizontalLayoutGroup h))
        {
            h = barTf.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        h.childAlignment = TextAnchor.UpperLeft;
        h.spacing = LobbyTopLeftTitleClockGap;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        Transform titleTf = barTf.Find("LobbyTopLeftTitle");
        if (titleTf != null && titleTf.TryGetComponent(out TextMeshProUGUI titleTmp))
        {
            titleTmp.fontSize = LobbyTopLeftFontSize;
            titleTmp.enableWordWrapping = true;
            titleTmp.overflowMode = TextOverflowModes.Overflow;
            titleTmp.alignment = TextAlignmentOptions.TopLeft;
            LayoutElement le = titleTmp.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = titleTmp.gameObject.AddComponent<LayoutElement>();
            }

            le.minWidth = 48f;
            le.preferredWidth = 48f;
            le.flexibleWidth = 0f;
            le.minHeight = LobbyTopLeftFontSize;
        }

        Transform clockTf = barTf.Find("LobbyTopLeftClock");
        if (clockTf != null && clockTf.TryGetComponent(out TextMeshProUGUI clockTmp))
        {
            clockTmp.fontSize = LobbyTopLeftFontSize;
            clockTmp.enableWordWrapping = false;
            clockTmp.overflowMode = TextOverflowModes.Overflow;
            clockTmp.alignment = TextAlignmentOptions.TopLeft;
            LayoutElement le = clockTmp.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = clockTmp.gameObject.AddComponent<LayoutElement>();
            }

            le.minWidth = LobbyTopLeftTimeMinWidth;
            le.preferredWidth = Mathf.Max(
                LobbyTopLeftTimeMinWidth,
                le.preferredWidth);
            le.flexibleWidth = 0f;
            le.minHeight = LobbyTopLeftFontSize;
        }
    }

    private static void EnforceLobbyTopLeftTitleBeforeClock(Transform barTf)
    {
        if (barTf == null)
        {
            return;
        }

        Transform titleTf = barTf.Find("LobbyTopLeftTitle");
        Transform clockTf = barTf.Find("LobbyTopLeftClock");
        if (titleTf != null)
        {
            titleTf.SetAsFirstSibling();
        }

        if (clockTf != null)
        {
            if (titleTf != null)
            {
                clockTf.SetSiblingIndex(titleTf.GetSiblingIndex() + 1);
            }
            else
            {
                clockTf.SetAsLastSibling();
            }
        }
    }

    private static string FormatLobbyDateTime(DateTime now)
    {
        return now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
    }

    private static float MeasureLobbyTimeColumnWidth(TextMeshProUGUI timeTmp)
    {
        if (timeTmp == null)
        {
            return LobbyTopLeftTimeMinWidth;
        }

        if (string.IsNullOrEmpty(timeTmp.text))
        {
            return LobbyTopLeftTimeMinWidth;
        }

        timeTmp.ForceMeshUpdate(true);
        float w = timeTmp.GetPreferredValues(timeTmp.text, 0f, 0f).x;
        return Mathf.Max(LobbyTopLeftTimeMinWidth, w + 4f);
    }

    private void ApplyLobbyTopLeftColumnWidths(RectTransform bar)
    {
        if (bar == null || _lobbyTopLeftTitleTmp == null || _lobbyTopLeftTimeTmp == null)
        {
            return;
        }

        LayoutElement titleLe = _lobbyTopLeftTitleTmp.GetComponent<LayoutElement>();
        if (titleLe == null)
        {
            titleLe = _lobbyTopLeftTitleTmp.gameObject.AddComponent<LayoutElement>();
        }

        LayoutElement timeLe = _lobbyTopLeftTimeTmp.GetComponent<LayoutElement>();
        if (timeLe == null)
        {
            timeLe = _lobbyTopLeftTimeTmp.gameObject.AddComponent<LayoutElement>();
        }

        if (string.IsNullOrEmpty(_lobbyTopLeftTimeTmp.text))
        {
            _lobbyTopLeftTimeTmp.text = FormatLobbyDateTime(DateTime.Now);
        }

        timeLe.flexibleWidth = 0f;
        timeLe.minWidth = LobbyTopLeftTimeMinWidth;
        timeLe.preferredWidth = MeasureLobbyTimeColumnWidth(_lobbyTopLeftTimeTmp);

        float barW = bar.sizeDelta.x;
        float timeW = timeLe.preferredWidth;
        float maxTitleW = Mathf.Max(48f, barW - timeW - LobbyTopLeftTitleClockGap);

        _lobbyTopLeftTitleTmp.ForceMeshUpdate(true);
        float naturalW = _lobbyTopLeftTitleTmp.GetPreferredValues(
            _lobbyTopLeftTitleTmp.text, 1e4f, 0f).x;
        titleLe.minWidth = 48f;
        titleLe.flexibleWidth = 0f;
        titleLe.preferredWidth = naturalW <= maxTitleW
            ? Mathf.Max(48f, naturalW)
            : maxTitleW;
    }

    private static TextMeshProUGUI CreateLobbyTopLeftTitleText(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        ApplyLobbyTopLeftSharedTypography(tmp);
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    private static TextMeshProUGUI CreateLobbyTopLeftClockText(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        ApplyLobbyTopLeftSharedTypography(tmp);
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    private static void ApplyLobbyTopLeftSharedTypography(TextMeshProUGUI tmp)
    {
        if (TmpFontCache.LiberationSansSdf != null)
        {
            tmp.font = TmpFontCache.LiberationSansSdf;
        }

        tmp.fontSize = LobbyTopLeftFontSize;
        tmp.raycastTarget = false;
        GsiUiRuntimeWidgets.ApplyEconomyLineTypography(tmp);
    }

    private void RebuildLobbyTopLeftBarHeights()
    {
        if (_lobbyRoot == null || _lobbyTopLeftTitleTmp == null || _lobbyTopLeftTimeTmp == null)
        {
            return;
        }

        RectTransform bar = _lobbyTopLeftTitleTmp.transform.parent as RectTransform;
        if (bar == null)
        {
            return;
        }

        ApplyLobbyTopLeftColumnWidths(bar);
        LayoutRebuilder.ForceRebuildLayoutImmediate(bar);
        _lobbyTopLeftTitleTmp.ForceMeshUpdate(true);
        _lobbyTopLeftTimeTmp.ForceMeshUpdate(true);

        float titleW = _lobbyTopLeftTitleTmp.rectTransform.rect.width;
        if (titleW < 2f)
        {
            return;
        }

        float titleH = _lobbyTopLeftTitleTmp.GetPreferredValues(
            _lobbyTopLeftTitleTmp.text, titleW, 0f).y;
        float timeH = _lobbyTopLeftTimeTmp.GetPreferredValues(
            _lobbyTopLeftTimeTmp.text, _lobbyTopLeftTimeTmp.rectTransform.rect.width, 0f).y;
        float rowH = Mathf.Max(LobbyTopLeftFontSize, Mathf.Max(titleH, timeH) + 4f);
        float barH = Mathf.Min(LobbyTopLeftBarMaxHeight, rowH);
        if (!Mathf.Approximately(bar.sizeDelta.y, barH))
        {
            var sd = bar.sizeDelta;
            bar.sizeDelta = new Vector2(sd.x, barH);
        }
    }

    private void RefreshLobbyTopLeftTitleAndTime()
    {
        if (_lobbyTopLeftTitleTmp != null)
        {
            _lobbyTopLeftTitleTmp.text = GameLocalization.GetUiString(UiStringKeys.LobbyTopLeftTitle, "The Axiom");
            _lobbyTopLeftTitleTmp.color = GsiUiAppearance.TextPrimary;
        }

        if (_lobbyTopLeftTimeTmp != null)
        {
            _lobbyTopLeftTimeTmp.text = FormatLobbyDateTime(DateTime.Now);
            _lobbyTopLeftTimeTmp.color = GsiUiAppearance.TextSecondary;
        }

        _lobbyClockSecondStamp = -1L;
        RebuildLobbyTopLeftBarHeights();
    }

    private void Update()
    {
        if (!Application.isPlaying || _lobbyRoot == null)
        {
            return;
        }

        if (_lobbyTopLeftTitleTmp == null)
        {
            return;
        }

        Transform topLeftTf = _lobbyRoot.Find(LobbyTopLeftBarName);
        if (topLeftTf is RectTransform barRt)
        {
            float w = _lobbyRoot.rect.width;
            if (!Mathf.Approximately(w, _lobbyTopLeftBarLastLayoutWidth))
            {
                _lobbyTopLeftBarLastLayoutWidth = w;
                ApplyLobbyTopLeftBarRect(barRt);
                RebuildLobbyTopLeftBarHeights();
            }
        }

        if (_lobbyTopLeftTimeTmp == null)
        {
            return;
        }

        DateTime now = DateTime.Now;
        long tickSecond = now.Ticks / TimeSpan.TicksPerSecond;
        if (tickSecond == _lobbyClockSecondStamp)
        {
            return;
        }

        _lobbyClockSecondStamp = tickSecond;
        _lobbyTopLeftTimeTmp.text = FormatLobbyDateTime(now);
        RebuildLobbyTopLeftBarHeights();
    }

    /// <summary>Undoes the two-tier lobby dock experiment: flattens buttons back under <see cref="LobbyActionRowName"/>.</summary>
    private static void TryFlattenLegacyTwoTierLobbyActionRow(Transform rowTf, RectTransform rowRt)
    {
        if (rowTf == null || rowRt == null)
        {
            return;
        }

        Transform p = rowTf.Find(LobbyActionPrimaryRowLegacyName);
        Transform sec = rowTf.Find(LobbyActionSecondaryRowLegacyName);
        if (p == null && sec == null)
        {
            return;
        }

        Transform gsi = p != null ? p.Find("GSIButton") : rowTf.Find("GSIButton");
        Transform shop = sec != null ? sec.Find("ShopButton") : rowTf.Find("ShopButton");
        Transform inv = sec != null ? sec.Find("InventoryButton") : rowTf.Find("InventoryButton");
        Transform altar = sec != null ? sec.Find("AltarOfVerityButton") : rowTf.Find("AltarOfVerityButton");

        void Pull(Transform t, int order)
        {
            if (t == null)
            {
                return;
            }

            t.SetParent(rowRt, false);
            t.SetSiblingIndex(order);
        }

        Pull(gsi, 0);
        Pull(shop, 1);
        Pull(inv, 2);
        Pull(altar, 3);

        if (p != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(p.gameObject);
            }
            else
#endif
            {
                Destroy(p.gameObject);
            }
        }

        if (sec != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(sec.gameObject);
            }
            else
#endif
            {
                Destroy(sec.gameObject);
            }
        }

        if (rowRt.GetComponent<VerticalLayoutGroup>() is VerticalLayoutGroup v)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(v);
            }
            else
#endif
            {
                Destroy(v);
            }
        }
    }

    private void EnsureLobbyActionRowLayout()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        Transform rowTf = _lobbyRoot.Find(LobbyActionRowName);
        if (rowTf == null || !rowTf.TryGetComponent(out RectTransform rowRt))
        {
            return;
        }

        TryFlattenLegacyTwoTierLobbyActionRow(rowTf, rowRt);

        // Stretch rowRt to fill the entire parent screen so stars can float and bounce everywhere!
        rowRt.anchorMin = Vector2.zero;
        rowRt.anchorMax = Vector2.one;
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.anchoredPosition = Vector2.zero;
        rowRt.sizeDelta = Vector2.zero;

        // Disable layout group so child buttons can float freely
        HorizontalLayoutGroup hlg = rowTf.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.enabled = false;
        }

        // Configure GSI Facility Star (Brilliant gold/yellow)
        if (_enterGsiFacilityButton != null)
        {
            SetupLobbyStar(
                _enterGsiFacilityButton,
                new Color(1.0f, 0.88f, 0.35f, 1f),
                GameLocalization.GetUiString(UiStringKeys.UiLobbyStart, "Start"),
                new Vector2(-360f, 80f)
            );
        }

        // Configure Shop Star (Premium amethyst/purple)
        if (_openShopButton != null)
        {
            SetupLobbyStar(
                _openShopButton,
                new Color(0.85f, 0.45f, 1f, 1f),
                GameLocalization.GetUiString(UiStringKeys.UiLobbyShop, "Shop"),
                new Vector2(-120f, -140f)
            );
        }

        // Configure Inventory Star (Sleek emerald/cyan)
        if (_openInventoryButton != null)
        {
            SetupLobbyStar(
                _openInventoryButton,
                new Color(0.35f, 0.95f, 0.85f, 1f),
                GameLocalization.GetUiString(UiStringKeys.UiLobbyInventory, "Inventory"),
                new Vector2(120f, 140f)
            );
        }

        // Configure Altar of Verity Star (Radiant ruby/amber)
        if (_openAltarOfVerityButton != null)
        {
            SetupLobbyStar(
                _openAltarOfVerityButton,
                new Color(1f, 0.48f, 0.45f, 1f),
                GameLocalization.GetUiString(UiStringKeys.UiLobbyAltarOfVerity, "Altar of Verity"),
                new Vector2(360f, -80f)
            );
        }

        rowRt.SetAsLastSibling();
    }

    private void SetupLobbyStar(Button button, Color color, string label, Vector2 initPos)
    {
        // Remove layout element to prevent horizontal auto-positioning
        if (button.TryGetComponent(out LayoutElement le))
        {
            Destroy(le);
        }

        // Remove legacy hover boost to avoid visual interference
        if (button.TryGetComponent(out LobbyButtonLabelHoverBoost hb))
        {
            Destroy(hb);
        }

        // Set anchors to center to make positioning relative to parent's center
        var rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Add the star physics & visualization component
        var star = button.gameObject.GetComponent<GsiLobbyStarNodeController>();
        if (star == null)
        {
            star = button.gameObject.AddComponent<GsiLobbyStarNodeController>();
        }

        star.StarColor = color;
        star.ButtonLabelText = label;
        star.InitialPosition = initPos;
    }

    private void ApplyLobbyEconomyStripRect(RectTransform stripRt)
    {
        if (stripRt == null)
        {
            return;
        }

        stripRt.anchorMin = new Vector2(0f, 1f);
        stripRt.anchorMax = new Vector2(1f, 1f);
        stripRt.pivot = new Vector2(0.5f, 1f);
        stripRt.anchoredPosition = new Vector2(0f, -LobbyEconomyStripTopInset);
        stripRt.sizeDelta = new Vector2(-LobbyActionRowSideInset * 2f, LobbyEconomyStripHeight);
    }

    private void BuildLobbyActionRow()
    {
        if (_lobbyRoot == null || _lobbyRoot.Find(LobbyActionRowName) != null)
        {
            return;
        }

        Transform gsi = _lobbyRoot.Find("GSIButton");
        Transform shop = _lobbyRoot.Find("ShopButton");
        if (gsi == null || shop == null)
        {
            return;
        }

        var rowGo = new GameObject(LobbyActionRowName);
        RectTransform rowRt = rowGo.AddComponent<RectTransform>();
        rowRt.SetParent(_lobbyRoot, false);
        rowRt.anchorMin = new Vector2(0f, 0f);
        rowRt.anchorMax = new Vector2(1f, 0f);
        rowRt.pivot = new Vector2(0.5f, 0f);
        rowRt.sizeDelta = new Vector2(-LobbyActionRowSideInset * 2f, LobbyActionRowHeight);
        rowRt.anchoredPosition = new Vector2(0f, LobbyActionRowBottomInset);

        var hor = rowGo.AddComponent<HorizontalLayoutGroup>();
        hor.spacing = 22f;
        hor.padding = new RectOffset(16, 16, 0, 0);
        hor.childAlignment = TextAnchor.MiddleCenter;
        hor.childControlWidth = true;
        hor.childControlHeight = true;
        hor.childForceExpandWidth = true;
        hor.childForceExpandHeight = true;

        gsi.SetParent(rowRt, false);
        shop.SetParent(rowRt, false);

        Transform inv = _lobbyRoot.Find("InventoryButton");
        if (inv == null)
        {
            Button invBtn = CreateLobbyActionButton(rowRt, "InventoryButton",
                GameLocalization.GetUiString(UiStringKeys.UiLobbyInventory, "Inventory"));
            _openInventoryButton = invBtn;
        }
        else
        {
            inv.SetParent(rowRt, false);
            _openInventoryButton = inv.GetComponent<Button>();
        }

        Transform altar = _lobbyRoot.Find("AltarOfVerityButton");
        if (altar == null)
        {
            Button altarBtn = CreateLobbyActionButton(rowRt, "AltarOfVerityButton",
                GameLocalization.GetUiString(UiStringKeys.UiLobbyAltarOfVerity, "Altar of Verity"));
            _openAltarOfVerityButton = altarBtn;
        }
        else
        {
            altar.SetParent(rowRt, false);
            _openAltarOfVerityButton = altar.GetComponent<Button>();
        }

        LayoutElement gsiLe = gsi.gameObject.GetComponent<LayoutElement>();
        if (gsiLe == null)
        {
            gsiLe = gsi.gameObject.AddComponent<LayoutElement>();
        }

        gsiLe.minHeight = 96f;
        gsiLe.preferredHeight = 102f;

        LayoutElement shopLe = shop.gameObject.GetComponent<LayoutElement>();
        if (shopLe == null)
        {
            shopLe = shop.gameObject.AddComponent<LayoutElement>();
        }

        shopLe.minHeight = 96f;
        shopLe.preferredHeight = 102f;

        Transform invTf = rowRt.Find("InventoryButton");
        if (invTf != null)
        {
            LayoutElement invLe = invTf.gameObject.GetComponent<LayoutElement>();
            if (invLe == null)
            {
                invLe = invTf.gameObject.AddComponent<LayoutElement>();
            }

            invLe.minHeight = 96f;
            invLe.preferredHeight = 102f;
        }

        Transform altarTf = rowRt.Find("AltarOfVerityButton");
        if (altarTf != null)
        {
            LayoutElement altarLe = altarTf.gameObject.GetComponent<LayoutElement>();
            if (altarLe == null)
            {
                altarLe = altarTf.gameObject.AddComponent<LayoutElement>();
            }

            altarLe.minHeight = 96f;
            altarLe.preferredHeight = 102f;
        }

        rowRt.SetAsLastSibling();
    }

    private void ApplyLobbyChrome()
    {
        EnsureLobbyActionRowLayout();

        ApplyLobbyLocalizedTexts();

        EnsureLobbyBackdrop();
        ApplyLobbyCenterStageChrome();
        EnsureLobbyCenterParallaxWired();
        ApplyLobbyDecorBranding();

        if (_panelBackground != null)
        {
            _panelBackground.sprite = GetOrCreateSpaceSprite();
            _panelBackground.type = Image.Type.Simple;
            _panelBackground.preserveAspect = false;
            _panelBackground.color = Color.white;
            _panelBackground.raycastTarget = false;
        }

        if (_heroImage != null)
        {
            _heroImage.enabled = false;
            _heroImage.raycastTarget = false;
        }

        ApplyLobbyPrimaryButton(_enterGsiFacilityButton);
        ApplyLobbySecondaryButton(_openShopButton);
        ApplyLobbySecondaryButton(_openInventoryButton);
        ApplyLobbySecondaryButton(_openAltarOfVerityButton);
        LobbyButtonLabelHoverBoost.EnsureOn(_enterGsiFacilityButton);
        LobbyButtonLabelHoverBoost.EnsureOn(_openShopButton);
        LobbyButtonLabelHoverBoost.EnsureOn(_openInventoryButton);
        LobbyButtonLabelHoverBoost.EnsureOn(_openAltarOfVerityButton);
        UpdateEconomyTexts();
        RefreshLobbyTopLeftTitleAndTime();
        BringLobbyInteractiveUiInFront();
    }

    /// <summary>Removes legacy LobbyBackdrop RawImage child; root panel Image follows <see cref="GsiUiAppearance"/>.</summary>
    private void EnsureLobbyBackdrop()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        Transform backdropTf = _lobbyRoot.Find(LobbyBackdropObjectName);
        if (backdropTf == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(backdropTf.gameObject);
            return;
        }
#endif
        Destroy(backdropTf.gameObject);
    }

    private void EnsureLobbyCenterStage()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        Transform stageTf = _lobbyRoot.Find(LobbyCenterStageName);
        if (stageTf != null)
        {
            if (!stageTf.TryGetComponent(out RectTransform stageRt))
            {
                return;
            }

            ApplyLobbyCenterStageLayout(stageRt);
            TryBindLobbyCenterStageScaffold(stageTf);
            EnsureLobbyCenterArtLayers(stageTf, stageRt);
            TryBindLobbyCenterStageScaffold(stageTf);
            stageRt.SetAsFirstSibling();
            PrepareLobbyCenterStageEntrance(stageTf);
            return;
        }

        var rootGo = new GameObject(LobbyCenterStageName, typeof(RectTransform), typeof(LobbyCenterStageScaffold));
        RectTransform rt = rootGo.GetComponent<RectTransform>();
        rt.SetParent(_lobbyRoot, false);
        ApplyLobbyCenterStageLayout(rt);
        _lobbyCenterStage = rootGo.GetComponent<LobbyCenterStageScaffold>();

        RectTransform baseRt = CreateLobbyCenterLayerImage(rt, LobbyLayerBaseName, out Image baseImg);
        RectTransform farRt = CreateLobbyCenterLayerImage(
            rt, LobbyLayerFarName, out Image farImg, LobbyCenterStageParallax.ParallaxLayerEdgeOverflow);
        RectTransform midRt = CreateLobbyCenterLayerImage(
            rt, LobbyLayerMidName, out Image midImg, LobbyCenterStageParallax.ParallaxLayerEdgeOverflow);
        RectTransform nearRt = CreateLobbyCenterLayerImage(
            rt, LobbyLayerNearName, out Image nearImg, LobbyCenterStageParallax.ParallaxLayerEdgeOverflow);
        RectTransform uiScrimRt = CreateLobbyCenterLayerImage(rt, LobbyLayerUiScrimName, out Image uiScrimImg, 0f);
        RectTransform glowRt = CreateLobbyCenterGlow(rt, out Image glowImg);
        RectTransform vignetteRt = CreateLobbyCenterLayerImage(rt, LobbyLayerVignetteName, out Image vignetteImg);
        var decorGo = new GameObject(LobbyDecorRootName, typeof(RectTransform));
        RectTransform decorRt = decorGo.GetComponent<RectTransform>();
        decorRt.SetParent(rt, false);
        GsiUiRuntimeWidgets.StretchFull(decorRt);

        _lobbyCenterStage.BaseLayer = baseRt;
        _lobbyCenterStage.FarLayer = farRt;
        _lobbyCenterStage.MidLayer = midRt;
        _lobbyCenterStage.NearLayer = nearRt;
        _lobbyCenterStage.UiScrimLayer = uiScrimRt;
        _lobbyCenterStage.GlowLayer = glowRt;
        _lobbyCenterStage.VignetteLayer = vignetteRt;
        _lobbyCenterStage.DecorRoot = decorRt;
        _lobbyCenterStage.BaseImage = baseImg;
        _lobbyCenterStage.FarImage = farImg;
        _lobbyCenterStage.MidImage = midImg;
        _lobbyCenterStage.NearImage = nearImg;
        _lobbyCenterStage.UiScrimImage = uiScrimImg;
        _lobbyCenterStage.GlowImage = glowImg;
        _lobbyCenterStage.VignetteImage = vignetteImg;

        ReorderLobbyCenterStageLayers(stageTf: rt);
        rt.SetAsFirstSibling();
        PrepareLobbyCenterStageEntrance(rt);
    }

    private void PrepareLobbyCenterStageEntrance(Transform stageRoot)
    {
        if (!Application.isPlaying || stageRoot == null)
        {
            return;
        }

        CanvasGroup cg = stageRoot.gameObject.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = stageRoot.gameObject.AddComponent<CanvasGroup>();
        }

        cg.interactable = true;
        cg.blocksRaycasts = false;
        cg.alpha = _lobbyCenterEntranceFade ? 0f : 1f;
    }

    private void PrepareLobbyActionRowEntrance(Transform actionRowRoot)
    {
        if (!Application.isPlaying || actionRowRoot == null)
        {
            return;
        }

        CanvasGroup cg = actionRowRoot.gameObject.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = actionRowRoot.gameObject.AddComponent<CanvasGroup>();
        }

        if (_lobbyActionRowEntranceFade)
        {
            cg.alpha = 0f;
            cg.interactable = false;
        }
        else
        {
            cg.alpha = 1f;
            cg.interactable = true;
        }

        cg.blocksRaycasts = true;
    }

    private IEnumerator CoLobbyEntranceSequence()
    {
        if (_lobbyRoot == null)
        {
            _lobbyEntranceSequenceRoutine = null;
            yield break;
        }

        Transform stage = _lobbyRoot.Find(LobbyCenterStageName);
        if (stage != null && stage.TryGetComponent(out CanvasGroup centerCg))
        {
            if (_lobbyCenterEntranceFade)
            {
                float centerDur = Mathf.Max(0.04f, _lobbyCenterEntranceDuration);
                float t = 0f;
                while (t < centerDur)
                {
                    t += Time.unscaledDeltaTime;
                    centerCg.alpha = Mathf.Clamp01(t / centerDur);
                    yield return null;
                }
            }

            centerCg.alpha = 1f;
        }

        Transform actionRowT = _lobbyRoot.Find(LobbyActionRowName);
        if (actionRowT == null)
        {
            _lobbyEntranceSequenceRoutine = null;
            yield break;
        }

        if (!actionRowT.TryGetComponent(out CanvasGroup rowCg))
        {
            _lobbyEntranceSequenceRoutine = null;
            yield break;
        }

        if (!_lobbyActionRowEntranceFade)
        {
            rowCg.alpha = 1f;
            rowCg.interactable = true;
            _lobbyEntranceSequenceRoutine = null;
            yield break;
        }

        float wait = Mathf.Max(0f, _lobbyActionRowEntranceDelay);
        if (wait > 0f)
        {
            float w = 0f;
            while (w < wait)
            {
                w += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        float rowDur = Mathf.Max(0.04f, _lobbyActionRowEntranceDuration);
        float rt = 0f;
        rowCg.interactable = false;
        while (rt < rowDur)
        {
            rt += Time.unscaledDeltaTime;
            rowCg.alpha = Mathf.Clamp01(rt / rowDur);
            yield return null;
        }

        rowCg.alpha = 1f;
        rowCg.interactable = true;
        _lobbyEntranceSequenceRoutine = null;
    }

    /// <summary>Adds Far/Mid/Near if missing (older scenes) and fixes draw order.</summary>
    private static void EnsureLobbyCenterArtLayers(Transform stageTf, RectTransform stageRt)
    {
        if (stageTf.Find(LobbyLayerFarName) == null)
        {
            CreateLobbyCenterLayerImage(
                stageRt, LobbyLayerFarName, out _, LobbyCenterStageParallax.ParallaxLayerEdgeOverflow);
        }

        if (stageTf.Find(LobbyLayerMidName) == null)
        {
            CreateLobbyCenterLayerImage(
                stageRt, LobbyLayerMidName, out _, LobbyCenterStageParallax.ParallaxLayerEdgeOverflow);
        }

        if (stageTf.Find(LobbyLayerNearName) == null)
        {
            CreateLobbyCenterLayerImage(
                stageRt, LobbyLayerNearName, out _, LobbyCenterStageParallax.ParallaxLayerEdgeOverflow);
        }

        if (stageTf.Find(LobbyLayerUiScrimName) == null)
        {
            CreateLobbyCenterLayerImage(stageRt, LobbyLayerUiScrimName, out _, 0f);
        }

        ReorderLobbyCenterStageLayers(stageTf);
    }

    /// <summary>Back-to-front: Base, Far, Mid, Near, UiScrim, Glow, Vignette, Decor.</summary>
    private static void ReorderLobbyCenterStageLayers(Transform stageTf)
    {
        if (stageTf == null)
        {
            return;
        }

        string[] order =
        {
            LobbyLayerBaseName,
            LobbyLayerFarName,
            LobbyLayerMidName,
            LobbyLayerNearName,
            LobbyLayerUiScrimName,
            LobbyLayerGlowName,
            LobbyLayerVignetteName,
            LobbyDecorRootName,
        };

        int idx = 0;
        for (int i = 0; i < order.Length; i++)
        {
            Transform t = stageTf.Find(order[i]);
            if (t != null)
            {
                t.SetSiblingIndex(idx++);
            }
        }
    }

    private void ApplyLobbyCenterStageLayout(RectTransform stageRt)
    {
        if (stageRt == null)
        {
            return;
        }

        float topInset = LobbyEconomyStripTopInset + LobbyEconomyStripHeight + LobbyCenterStageExtraTopGap;
        float bottomInset = LobbyActionRowBottomInset + LobbyActionRowHeight;
        stageRt.anchorMin = Vector2.zero;
        stageRt.anchorMax = Vector2.one;
        // Edge-to-edge horizontally; top/bottom still clear economy strip and action row.
        stageRt.offsetMin = new Vector2(0f, bottomInset);
        stageRt.offsetMax = new Vector2(0f, -topInset);
        EnsureLobbyCenterStageClipsChildren(stageRt);
    }

    /// <summary>
    /// Parallax layer rects extend past the artboard; clip them to the stage rect so the corridor
    /// never draws into the top economy bar or the bottom action row.
    /// </summary>
    private static void EnsureLobbyCenterStageClipsChildren(RectTransform stageRt)
    {
        if (stageRt == null)
        {
            return;
        }

        if (stageRt.GetComponent<RectMask2D>() == null)
        {
            stageRt.gameObject.AddComponent<RectMask2D>();
        }
    }

    private void TryBindLobbyCenterStageScaffold(Transform stageTf)
    {
        if (!stageTf.TryGetComponent(out LobbyCenterStageScaffold scaffold))
        {
            scaffold = stageTf.gameObject.AddComponent<LobbyCenterStageScaffold>();
        }

        _lobbyCenterStage = scaffold;
        if (scaffold.BaseLayer == null && stageTf.Find(LobbyLayerBaseName) is RectTransform br)
        {
            scaffold.BaseLayer = br;
            scaffold.BaseImage = br.GetComponent<Image>();
        }

        if (scaffold.FarLayer == null && stageTf.Find(LobbyLayerFarName) is RectTransform fr)
        {
            scaffold.FarLayer = fr;
            scaffold.FarImage = fr.GetComponent<Image>();
        }

        if (scaffold.MidLayer == null && stageTf.Find(LobbyLayerMidName) is RectTransform mr)
        {
            scaffold.MidLayer = mr;
            scaffold.MidImage = mr.GetComponent<Image>();
        }

        if (scaffold.NearLayer == null && stageTf.Find(LobbyLayerNearName) is RectTransform nr)
        {
            scaffold.NearLayer = nr;
            scaffold.NearImage = nr.GetComponent<Image>();
        }

        if (scaffold.UiScrimLayer == null && stageTf.Find(LobbyLayerUiScrimName) is RectTransform ur)
        {
            scaffold.UiScrimLayer = ur;
            scaffold.UiScrimImage = ur.GetComponent<Image>();
        }

        if (scaffold.GlowLayer == null && stageTf.Find(LobbyLayerGlowName) is RectTransform gr)
        {
            scaffold.GlowLayer = gr;
            scaffold.GlowImage = gr.GetComponent<Image>();
        }

        if (scaffold.VignetteLayer == null && stageTf.Find(LobbyLayerVignetteName) is RectTransform vr)
        {
            scaffold.VignetteLayer = vr;
            scaffold.VignetteImage = vr.GetComponent<Image>();
        }

        if (scaffold.DecorRoot == null && stageTf.Find(LobbyDecorRootName) is RectTransform dr)
        {
            scaffold.DecorRoot = dr;
        }
    }

    private static RectTransform CreateLobbyCenterLayerImage(
        RectTransform parent, string objectName, out Image img, float edgeOverflow = 0f)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        RectTransform layerRt = go.GetComponent<RectTransform>();
        layerRt.SetParent(parent, false);
        if (edgeOverflow > 0f)
        {
            GsiUiRuntimeWidgets.StretchFullWithEdgeOverflow(layerRt, edgeOverflow);
        }
        else
        {
            GsiUiRuntimeWidgets.StretchFull(layerRt);
        }

        img = go.AddComponent<Image>();
        GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
        img.raycastTarget = false;
        return layerRt;
    }

    private static RectTransform CreateLobbyCenterGlow(RectTransform parent, out Image img)
    {
        var go = new GameObject(LobbyLayerGlowName, typeof(RectTransform));
        RectTransform glowRt = go.GetComponent<RectTransform>();
        glowRt.SetParent(parent, false);
        glowRt.anchorMin = glowRt.anchorMax = new Vector2(0.5f, 0.5f);
        glowRt.pivot = new Vector2(0.5f, 0.5f);
        glowRt.sizeDelta = new Vector2(980f, 540f);
        glowRt.anchoredPosition = Vector2.zero;
        img = go.AddComponent<Image>();
        GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
        img.raycastTarget = false;
        return glowRt;
    }

    private void ApplyLobbyCenterStageChrome()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        Transform stageTf = _lobbyRoot.Find(LobbyCenterStageName);
        if (stageTf != null && _lobbyCenterStage == null)
        {
            TryBindLobbyCenterStageScaffold(stageTf);
        }

        if (_lobbyCenterStage == null)
        {
            return;
        }

        if (_lobbyCenterStage.BaseImage != null)
        {
            _lobbyCenterStage.BaseImage.gameObject.SetActive(false);
        }

        TryApplyFarDistantLayerArt();
        TryApplyMidgroundLayerArt();
        TryApplyNeargroundLayerArt();

        if (_lobbyCenterStage.GlowImage != null)
        {
            // Lighter than before so the mid art reads clearly (less "grey card").
            Color g = GsiUiAppearance.TextPrimary;
            g.a = GsiUiAppearance.Mode == GsiUiAppearanceMode.Dark ? 0.032f : 0.026f;
            _lobbyCenterStage.GlowImage.color = g;
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(_lobbyCenterStage.GlowImage);
        }

        if (_lobbyCenterStage.VignetteImage != null)
        {
            Color v = GsiUiAppearance.OverlayScrim;
            v.a = GsiUiAppearance.Mode == GsiUiAppearanceMode.Dark ? 0.15f : 0.08f;
            _lobbyCenterStage.VignetteImage.color = v;
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(_lobbyCenterStage.VignetteImage);
        }

        if (_lobbyCenterStage.UiScrimImage != null)
        {
            Color u = GsiUiAppearance.OverlayScrim;
            u.a = GsiUiAppearance.Mode == GsiUiAppearanceMode.Dark ? 0.26f : 0.18f;
            _lobbyCenterStage.UiScrimImage.color = u;
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(_lobbyCenterStage.UiScrimImage);
        }

        SetLobbyStageArtNonBlockingRaycasts(_lobbyCenterStage);
    }

    /// <summary>
    /// Center-stage art must not sit in front of buttons/tickets in the raycast or draw stack. Scene-saved
    /// layers may still have m_RaycastTarget on; this forces them off each refresh.
    /// </summary>
    private static void SetLobbyStageArtNonBlockingRaycasts(LobbyCenterStageScaffold scaffold)
    {
        if (scaffold == null)
        {
            return;
        }

        void One(Image img)
        {
            if (img == null)
            {
                return;
            }

            img.raycastTarget = false;
        }

        One(scaffold.BaseImage);
        One(scaffold.FarImage);
        One(scaffold.MidImage);
        One(scaffold.NearImage);
        One(scaffold.UiScrimImage);
        One(scaffold.GlowImage);
        One(scaffold.VignetteImage);
    }

    /// <summary>
    /// Keeps <see cref="LobbyCenterStageName"/> as the rearmost child of the lobby panel, and
    /// economy + action row after it so all interactive UI / chrome draws and receives input on top.
    /// </summary>
    private void BringLobbyInteractiveUiInFront()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        Transform stage = _lobbyRoot.Find(LobbyCenterStageName);
        if (stage != null)
        {
            stage.SetAsFirstSibling();
        }

        Transform topLeftBar = _lobbyRoot.Find(LobbyTopLeftBarName);
        if (topLeftBar != null)
        {
            topLeftBar.SetAsLastSibling();
        }

        Transform strip = _lobbyRoot.Find(LobbyEconomyStripName);
        Transform row = _lobbyRoot.Find(LobbyActionRowName);
        if (strip != null)
        {
            strip.SetAsLastSibling();
        }

        if (row != null)
        {
            row.SetAsLastSibling();
        }
    }

    private Sprite GetOrCreateSpaceSprite()
    {
        if (_proceduralSpaceSprite == null)
        {
            _proceduralSpaceSprite = CreateProceduralSpaceSprite(1024, 576);
        }
        return _proceduralSpaceSprite;
    }

    private static Sprite CreateProceduralSpaceSprite(int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.name = "GSI_CosmicSpaceBackground";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color spaceDark = new Color(0.012f, 0.006f, 0.022f, 1f); // Deep void black/violet
        Color indigoBlack = new Color(0.004f, 0.012f, 0.032f, 1f); // Deep void indigo

        // Define brilliant glowing star coordinates and intensities
        var brightStars = new (float x, float y, float r, float intensity)[]
        {
            (0.15f, 0.72f, 8f, 0.9f),
            (0.32f, 0.24f, 6f, 0.8f),
            (0.55f, 0.85f, 12f, 0.95f), // A bright star in upper middle
            (0.78f, 0.42f, 10f, 0.85f),
            (0.88f, 0.78f, 7f, 0.75f),
            (0.22f, 0.48f, 5f, 0.7f),
            (0.48f, 0.18f, 9f, 0.85f),
            (0.68f, 0.62f, 6f, 0.75f)
        };

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / (height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / (width - 1);

                // Base gradient
                Color pixelColor = Color.Lerp(spaceDark, indigoBlack, v + u * 0.15f);

                // Nebula 1: Large soft violet clouds
                float n1 = Mathf.PerlinNoise(u * 2.2f + 4.5f, v * 1.8f + 1.2f);
                float n2 = Mathf.PerlinNoise(u * 4.8f - 2.5f, v * 3.6f + 3.8f);
                float neb1 = Mathf.Max(0f, (n1 * 0.65f + n2 * 0.35f) - 0.35f) * 1.8f;
                Color nebColor1 = new Color(0.16f, 0.05f, 0.28f, 1f) * neb1;

                // Nebula 2: Glowing cosmic cyan/teal clouds
                float n3 = Mathf.PerlinNoise(u * 3.5f - 8.2f, v * 2.8f + 5.5f);
                float n4 = Mathf.PerlinNoise(u * 6.5f + 1.1f, v * 5.2f - 4.2f);
                float neb2 = Mathf.Max(0f, (n3 * 0.58f + n4 * 0.42f) - 0.42f) * 1.9f;
                Color nebColor2 = new Color(0.03f, 0.18f, 0.24f, 1f) * neb2;

                pixelColor += nebColor1 + nebColor2;

                // Sparkling background stars (pseudo-random fast hash)
                float starSeed = Mathf.Repeat(Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f, 1.0f);
                if (starSeed > 0.9975f)
                {
                    float starBrightness = (starSeed - 0.9975f) / 0.0025f;
                    pixelColor += new Color(starBrightness, starBrightness, starBrightness * 1.08f, 0f) * 0.85f;
                }

                // Draw soft glow for the brilliant stars
                foreach (var star in brightStars)
                {
                    float sx = star.x * width;
                    float sy = star.y * height;
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(sx, sy));
                    if (dist < star.r)
                    {
                        float glow = Mathf.Pow(1.0f - dist / star.r, 2.2f);
                        pixelColor += new Color(star.intensity, star.intensity, star.intensity * 1.05f, 0f) * glow * 0.9f;
                    }
                }

                tex.SetPixel(x, y, pixelColor);
            }
        }

        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void TryApplyFarDistantLayerArt()
    {
        if (_lobbyCenterStage?.FarImage == null)
        {
            return;
        }

        Image far = _lobbyCenterStage.FarImage;
        far.sprite = GetOrCreateSpaceSprite();
        far.type = Image.Type.Simple;
        far.preserveAspect = false;
        far.color = Color.white;
    }

    private void TryApplyMidgroundLayerArt()
    {
        if (_lobbyCenterStage?.MidImage != null)
        {
            _lobbyCenterStage.MidImage.gameObject.SetActive(false);
        }
    }

    private void TryApplyNeargroundLayerArt()
    {
        if (_lobbyCenterStage?.NearImage != null)
        {
            _lobbyCenterStage.NearImage.gameObject.SetActive(false);
        }
    }

    private void EnsureLobbyCenterParallaxWired()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        Transform stageTf = _lobbyRoot.Find(LobbyCenterStageName);
        if (stageTf == null)
        {
            return;
        }

        LobbyCenterStageParallax parallax = stageTf.GetComponent<LobbyCenterStageParallax>();
        if (parallax == null)
        {
            parallax = stageTf.gameObject.AddComponent<LobbyCenterStageParallax>();
        }

        if (!stageTf.TryGetComponent(out LobbyCenterStageScaffold scaffold))
        {
            return;
        }

        LobbyCenterStageParallax.EnsureLayersSizedForParallax(scaffold.FarLayer, scaffold.MidLayer, scaffold.NearLayer);
        parallax.Configure(scaffold.FarLayer, scaffold.MidLayer, scaffold.NearLayer);
    }

    private void ApplyLobbyDecorBranding()
    {
        if (_lobbyRoot == null)
        {
            return;
        }

        Transform stageTf = _lobbyRoot.Find(LobbyCenterStageName);
        if (stageTf != null && stageTf.TryGetComponent(out LobbyCenterStageScaffold scaffold) && scaffold.DecorRoot != null)
        {
            scaffold.DecorRoot.gameObject.SetActive(false);
        }
    }


    private void ApplyLobbyLocalizedTexts()
    {
        ApplyLobbyButtonLabel(_enterGsiFacilityButton, UiStringKeys.UiLobbyStart, "Start");
        ApplyLobbyButtonLabel(_openShopButton, UiStringKeys.UiLobbyShop, "Shop");
        ApplyLobbyButtonLabel(_openInventoryButton, UiStringKeys.UiLobbyInventory, "Inventory");
        ApplyLobbyButtonLabel(_openAltarOfVerityButton, UiStringKeys.UiLobbyAltarOfVerity, "Altar of Verity");
    }

    private static void ApplyLobbyButtonLabel(Button btn, string localizationKey, string englishFallback)
    {
        if (btn == null)
        {
            return;
        }

        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null)
        {
            return;
        }

        tmp.text = GameLocalization.GetUiString(localizationKey, englishFallback);
    }

    private static void ApplyLobbyPrimaryButton(Button btn)
    {
        if (btn == null)
        {
            return;
        }

        if (btn.targetGraphic is Image img)
        {
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
            img.color = LobbyActionButtonBackgroundClear;
            img.raycastTarget = true;
        }

        btn.transition = Selectable.Transition.None;

        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.color = LobbyPrimaryActionLabelColor();
            tmp.fontSize = 24f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = 0.4f;
            tmp.alignment = TextAlignmentOptions.Midline;
        }

        EnsureLobbyActionLabelRuleLines(btn, LobbyPrimaryActionRuleLineColor());
        ApplyLobbyButtonColorTint(btn);
    }

    private static Color LobbyPrimaryActionLabelColor()
    {
        if (CosmeticSkinPalettes.TryGetActive(out _))
        {
            return GsiUiAppearance.TextPrimary;
        }

        return GsiUiAppearance.Mode == GsiUiAppearanceMode.Dark
            ? Color.white
            : GsiUiAppearance.TextPrimary;
    }

    private static Color LobbyPrimaryActionRuleLineColor()
    {
        if (CosmeticSkinPalettes.TryGetActive(out _))
        {
            Color c = GsiUiAppearance.TextPrimary;
            c.a = Mathf.Clamp01(c.a * 0.45f);
            return c;
        }

        if (GsiUiAppearance.Mode == GsiUiAppearanceMode.Dark)
        {
            return new Color(1f, 1f, 1f, 0.42f);
        }

        Color c2 = GsiUiAppearance.TextPrimary;
        c2.a = Mathf.Clamp01(c2.a * 0.45f);
        return c2;
    }

    private static void ApplyLobbySecondaryButton(Button btn)
    {
        if (btn == null)
        {
            return;
        }

        if (btn.targetGraphic is Image img)
        {
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
            img.color = LobbyActionButtonBackgroundClear;
            img.raycastTarget = true;
        }

        btn.transition = Selectable.Transition.None;

        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.color = GsiUiAppearance.TextPrimary;
            tmp.fontSize = 23f;
            tmp.fontStyle = FontStyles.Normal;
            tmp.characterSpacing = 0.25f;
            tmp.alignment = TextAlignmentOptions.Midline;
        }

        Color line = GsiUiAppearance.TextPrimary;
        line.a = Mathf.Clamp01(line.a * 0.48f);
        EnsureLobbyActionLabelRuleLines(btn, line);
        ApplyLobbyButtonColorTint(btn);
    }

    /// <summary>濡쒕퉬 ?≪뀡 ?쇰꺼 ?꽷룹븘???뉗? ???ㅽ겕/?쇱씠?몄뿉 留욎떠 ?됱? ?몄텧遺?먯꽌 吏??.</summary>
    private static void EnsureLobbyActionLabelRuleLines(Button btn, Color lineColor)
    {
        if (btn == null)
        {
            return;
        }

        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null)
        {
            return;
        }

        Transform rulesTf = btn.transform.Find(LobbyLabelRulesRootName);
        RectTransform rulesRt;
        if (rulesTf == null)
        {
            var rulesGo = new GameObject(LobbyLabelRulesRootName, typeof(RectTransform));
            rulesRt = rulesGo.GetComponent<RectTransform>();
            rulesRt.SetParent(btn.transform, false);
            GsiUiRuntimeWidgets.StretchFull(rulesRt);
            rulesRt.SetAsLastSibling();

            var passThrough = rulesGo.AddComponent<CanvasGroup>();
            passThrough.interactable = false;
            passThrough.blocksRaycasts = false;
            passThrough.alpha = 1f;

            var vlg = rulesGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(28, 28, 16, 6);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            CreateLobbyLabelRuleImage(rulesRt, LobbyLabelRuleTopName);
            tmp.transform.SetParent(rulesRt, false);
            CreateLobbyLabelRuleImage(rulesRt, LobbyLabelRuleBottomName);

            LayoutElement textLe = tmp.gameObject.GetComponent<LayoutElement>();
            if (textLe == null)
            {
                textLe = tmp.gameObject.AddComponent<LayoutElement>();
            }

            textLe.flexibleHeight = 1f;
            textLe.minHeight = 26f;
            textLe.flexibleWidth = 1f;
        }
        else
        {
            rulesRt = (RectTransform)rulesTf;
            if (rulesRt.TryGetComponent(out VerticalLayoutGroup existingVlg))
            {
                existingVlg.padding = new RectOffset(28, 28, 16, 6);
            }
        }

        Transform topTf = rulesRt.Find(LobbyLabelRuleTopName);
        if (topTf != null && topTf.TryGetComponent(out Image topImg))
        {
            topImg.color = lineColor;
            topImg.raycastTarget = false;
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(topImg);
        }

        Transform bottomTf = rulesRt.Find(LobbyLabelRuleBottomName);
        if (bottomTf != null && bottomTf.TryGetComponent(out Image bottomImg))
        {
            bottomImg.color = lineColor;
            bottomImg.raycastTarget = false;
            GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(bottomImg);
        }
    }

    private static void CreateLobbyLabelRuleImage(RectTransform parent, string objectName)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, 2f);

        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
        img.color = Color.white;

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 2f;
        le.minHeight = 2f;
        le.flexibleWidth = 1f;
    }

    private static void ApplyLobbyButtonColorTint(Button btn)
    {
        if (btn == null)
        {
            return;
        }

        ColorBlock colors = btn.colors;
        colors.fadeDuration = 0.1f;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 1f);
        btn.colors = colors;
    }

    private void OnEnterGsiFacilityClicked()
    {
        GsiSceneNavigation.LoadGsiFacility();
    }

    private void OnOpenShopClicked()
    {
        GsiSceneNavigation.LoadShop();
    }

    private void OnOpenInventoryClicked()
    {
        GsiSceneNavigation.LoadInventory();
    }

    private void OnOpenAltarOfVerityClicked()
    {
        GsiSceneNavigation.LoadAltarOfVerity();
    }

    private void HideLegacyGsiEntryButtons()
    {
        void Hide(Button b)
        {
            if (b != null)
            {
                b.gameObject.SetActive(false);
            }
        }

        Hide(_practiceButton);
        Hide(_aimPracticeButton);
        Hide(_reactionExamButton);
        Hide(_aimExamButton);
    }

    private void OnPracticeClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetTestType(TestType.Practice);
        GameManager.Instance.SetTestMode(TestMode.Reaction);
        GameManager.Instance.SetGameState(GameState.TestBriefing);
    }

    private void OnAimPracticeClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetTestType(TestType.Practice);
        GameManager.Instance.SetTestMode(TestMode.AimPrecision);
        GameManager.Instance.SetGameState(GameState.TestBriefing);
    }

    private void OnReactionExamClicked()
    {
        GsiSceneNavigation.LoadGsiFacility();
    }

    private void OnAimExamClicked()
    {
        GsiSceneNavigation.LoadGsiFacility();
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.MainMenu)
        {
            UpdateEconomyTexts();
        }
    }

    private void HandleEconomyChanged()
    {
        UpdateEconomyTexts();
    }

    private void UpdateEconomyTexts()
    {
        if (_tokenText != null)
        {
            int gold = EconomyManager.Instance != null ? EconomyManager.Instance.Tokens : 0;
            _tokenText.text = GameLocalization.FormatUiString(UiStringKeys.LobbyCurrencyGoldFmt, "Gold: {0}", gold);
            _tokenText.color = GsiUiAppearance.ShopGoldText;
            _tokenText.fontSize = 24f;
            _tokenText.enableWordWrapping = false;
            GsiUiRuntimeWidgets.ApplyEconomyLineTypography(_tokenText);
        }

        if (_ticketText != null)
        {
            int tickets = EconomyManager.Instance != null ? EconomyManager.Instance.ExamTickets : 0;
            _ticketText.text =
                GameLocalization.FormatUiString(UiStringKeys.LobbyCurrencyTicketFmt, "Exam tickets: {0}", tickets);
            _ticketText.color = GsiUiAppearance.ShopTicketText;
            _ticketText.fontSize = 24f;
            _ticketText.enableWordWrapping = false;
            GsiUiRuntimeWidgets.ApplyEconomyLineTypography(_ticketText);
        }
    }

    private Button CreateLobbyActionButton(RectTransform rowParent, string objectName, string label)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(rowParent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 96f;
        le.preferredHeight = 102f;
        le.flexibleWidth = 1f;

        var img = go.AddComponent<Image>();
        GsiUiRuntimeWidgets.EnsureUiSlicedBackgroundSprite(img);
        img.color = LobbyActionButtonBackgroundClear;
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.None;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tRt = textGo.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TmpFontCache.LiberationSansSdf != null)
        {
            tmp.font = TmpFontCache.LiberationSansSdf;
        }

        tmp.text = label;
        tmp.fontSize = 23f;
        tmp.fontStyle = FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = GsiUiAppearance.TextPrimary;
        tmp.characterSpacing = 0.25f;
        tmp.raycastTarget = false;

        ApplyLobbyButtonColorTint(btn);
        return btn;
    }

#if UNITY_EDITOR
    private void MarkLobbySceneDirtyIfNeeded()
    {
        if (Application.isPlaying)
        {
            return;
        }

        EditorUtility.SetDirty(gameObject);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
}


