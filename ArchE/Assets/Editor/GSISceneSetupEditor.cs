#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GSIScene에 G.S.I 전용 Canvas·패널·테스트 컨트롤러를 생성합니다.
/// 메뉴: Tools → ARCHÉ → GSI → Generate GSIScene UI
/// </summary>
public static class GSISceneSetupEditor
{
    private const string ScenePath = "Assets/Scenes/GSIScene.unity";
    private const string TmpFontResourcePath = "Fonts & Materials/LiberationSans SDF";

    [MenuItem("Tools/ARCHÉ/GSI/Generate GSIScene UI (replace auto setup)")]
    public static void Generate()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var existing = GameObject.Find("GSI_AutoSetup");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(es.gameObject);
        }

        var eventGo = new GameObject("EventSystem");
        eventGo.AddComponent<EventSystem>();
        eventGo.AddComponent<InputSystemUIInputModule>();

        var font = Resources.Load<TMP_FontAsset>(TmpFontResourcePath);

        var root = new GameObject("GSI_AutoSetup");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        root.AddComponent<GSISceneBootstrap>();
        var gsiUi = root.AddComponent<GSIUIManager>();

        var scoreHost = new GameObject("GSI_RuntimeSystems");
        scoreHost.transform.SetParent(root.transform, false);
        scoreHost.AddComponent<ScoreManager>();

        var rootRt = root.GetComponent<RectTransform>();
        StretchFull(rootRt);

        var panels = new GameObject("Panels");
        panels.transform.SetParent(root.transform, false);
        StretchFull(panels.AddComponent<RectTransform>());

        var lobby = CreatePanel("LobbyPanel", panels.transform, true);
        var standby = CreatePanel("TestStandbyPanel", panels.transform, false);
        var progress = CreatePanel("TestInProgressPanel", panels.transform, false);
        var result = CreatePanel("ResultScreenPanel", panels.transform, false);

        BuildLobby(lobby.transform, font, out var backBtn, out var pracR, out var pracA, out var exR, out var exA, out var tokenTmp, out var ticketTmp, out var bestTmp);
        BuildStandby(standby.transform, font);
        var reactionGo = BuildTestProgress(progress.transform, font, out var aimRect);
        BuildResult(result.transform, font, out var retryBtn, out var menuBtn, out var titleTmp, out var bodyTmp, out var rewardTmp, out var newRecGo);

        var hub = lobby.AddComponent<GSIHubMenuController>();
        using (var so = new SerializedObject(hub))
        {
            so.FindProperty("_backToArchEButton").objectReferenceValue = backBtn;
            so.FindProperty("_practiceButton").objectReferenceValue = pracR;
            so.FindProperty("_aimPracticeButton").objectReferenceValue = pracA;
            so.FindProperty("_reactionExamButton").objectReferenceValue = exR;
            so.FindProperty("_aimExamButton").objectReferenceValue = exA;
            so.FindProperty("_tokenText").objectReferenceValue = tokenTmp;
            so.FindProperty("_ticketText").objectReferenceValue = ticketTmp;
            so.ApplyModifiedProperties();
        }

        using (var so = new SerializedObject(gsiUi))
        {
            so.FindProperty("_lobbyPanel").objectReferenceValue = lobby;
            so.FindProperty("_testStandbyPanel").objectReferenceValue = standby;
            so.FindProperty("_testInProgressPanel").objectReferenceValue = progress;
            so.FindProperty("_resultScreenPanel").objectReferenceValue = result;
            so.FindProperty("_bestRecordText").objectReferenceValue = bestTmp;
            so.ApplyModifiedProperties();
        }

        var reactCtrl = progress.AddComponent<ReactionTestController>();
        using (var so = new SerializedObject(reactCtrl))
        {
            so.FindProperty("_targetObject").objectReferenceValue = reactionGo;
            so.ApplyModifiedProperties();
        }

        var aimCtrl = progress.AddComponent<AimTestController>();
        using (var so = new SerializedObject(aimCtrl))
        {
            so.FindProperty("_aimTargetRect").objectReferenceValue = aimRect;
            so.ApplyModifiedProperties();
        }

        if (progress.GetComponent<MemoryTestController>() == null)
        {
            progress.AddComponent<MemoryTestController>();
        }

        var resCtrl = result.AddComponent<ResultScreenController>();
        using (var so = new SerializedObject(resCtrl))
        {
            so.FindProperty("_retryButton").objectReferenceValue = retryBtn;
            so.FindProperty("_mainMenuButton").objectReferenceValue = menuBtn;
            so.FindProperty("_resultTitleText").objectReferenceValue = titleTmp;
            so.FindProperty("_resultText").objectReferenceValue = bodyTmp;
            so.FindProperty("_rewardText").objectReferenceValue = rewardTmp;
            so.FindProperty("_newRecordIndicator").objectReferenceValue = newRecGo;
            so.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GSISceneSetupEditor] GSIScene 저장 완료. SampleScene의 MainMenuController에 G.S.I 입장 버튼을 연결하세요.");
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static GameObject CreatePanel(string name, Transform parent, bool active)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFull(rt);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.08f, 0.09f, 0.12f, 0.97f);
        img.raycastTarget = false;
        go.SetActive(active);
        return go;
    }

    private static void BuildLobby(Transform parent, TMP_FontAsset font, out Button back, out Button pr, out Button pa, out Button er, out Button ea, out TextMeshProUGUI token, out TextMeshProUGUI ticket, out TextMeshProUGUI best)
    {
        var v = new GameObject("Layout");
        v.transform.SetParent(parent, false);
        var vRt = v.AddComponent<RectTransform>();
        StretchFull(vRt);
        var vl = v.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(48, 48, 48, 48);
        vl.spacing = 16f;
        vl.childAlignment = TextAnchor.UpperCenter;
        vl.childControlWidth = true;
        vl.childControlHeight = false;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        AddTmp("Title", v.transform, font, "G.S.I FACILITY", 36, TextAlignmentOptions.Center, 80f);

        best = AddTmp("BestRecord", v.transform, font, "BEST —", 22, TextAlignmentOptions.Center, 60f);

        var eco = new GameObject("EconomyRow");
        eco.transform.SetParent(v.transform, false);
        var h = eco.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 32f;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = false;
        h.childControlHeight = true;
        var ecoLe = eco.AddComponent<LayoutElement>();
        ecoLe.preferredHeight = 40f;
        token = AddTmp("Token", eco.transform, font, "기초 골드: —", 20, TextAlignmentOptions.Left, 400f);
        ticket = AddTmp("Ticket", eco.transform, font, "응시권: —", 20, TextAlignmentOptions.Left, 400f);

        back = CreateButton("BackToArchE", v.transform, font, "← ARCHÉ 허브로");
        pr = CreateButton("PracticeReaction", v.transform, font, "연습 · 반응");
        pa = CreateButton("PracticeAim", v.transform, font, "연습 · 에임");
        er = CreateButton("ExamReaction", v.transform, font, "공식 시험 · 반응");
        ea = CreateButton("ExamAim", v.transform, font, "공식 시험 · 에임");
    }

    private static void BuildStandby(Transform parent, TMP_FontAsset font)
    {
        AddTmp("Hint", parent, font, "화면을 눌러 시험을 시작하세요.", 28, TextAlignmentOptions.Center, 120f);
    }

    private static GameObject BuildTestProgress(Transform parent, TMP_FontAsset font, out RectTransform aimRect)
    {
        var reaction = new GameObject("ReactionTarget");
        reaction.transform.SetParent(parent, false);
        var rRt = reaction.AddComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0.5f, 0.5f);
        rRt.anchorMax = new Vector2(0.5f, 0.5f);
        rRt.sizeDelta = new Vector2(140f, 140f);
        rRt.anchoredPosition = Vector2.zero;
        var rImg = reaction.AddComponent<Image>();
        rImg.color = new Color(0.2f, 0.85f, 0.35f, 1f);
        reaction.SetActive(false);

        var aimArea = new GameObject("AimTargetArea");
        aimArea.transform.SetParent(parent, false);
        StretchFull(aimArea.AddComponent<RectTransform>());

        var aim = new GameObject("AimTarget");
        aim.transform.SetParent(aimArea.transform, false);
        aimRect = aim.AddComponent<RectTransform>();
        aimRect.anchorMin = new Vector2(0.5f, 0.5f);
        aimRect.anchorMax = new Vector2(0.5f, 0.5f);
        aimRect.sizeDelta = new Vector2(88f, 88f);
        aimRect.anchoredPosition = Vector2.zero;
        var aImg = aim.AddComponent<Image>();
        aImg.color = Color.white;
        aim.SetActive(false);

        return reaction;
    }

    private static void BuildResult(Transform parent, TMP_FontAsset font, out Button retry, out Button menu, out TextMeshProUGUI title, out TextMeshProUGUI body, out TextMeshProUGUI reward, out GameObject newRec)
    {
        var v = new GameObject("Layout");
        v.transform.SetParent(parent, false);
        var vRt = v.AddComponent<RectTransform>();
        StretchFull(vRt);
        var vl = v.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(64, 64, 64, 64);
        vl.spacing = 20f;
        vl.childAlignment = TextAnchor.MiddleCenter;
        vl.childControlWidth = true;
        vl.childForceExpandWidth = true;

        newRec = new GameObject("NewRecordBadge");
        newRec.transform.SetParent(v.transform, false);
        var nrLe = newRec.AddComponent<LayoutElement>();
        nrLe.preferredHeight = 36f;
        var nrTmp = newRec.AddComponent<TextMeshProUGUI>();
        ApplyFont(nrTmp, font);
        nrTmp.text = "NEW RECORD";
        nrTmp.fontSize = 22;
        nrTmp.alignment = TextAlignmentOptions.Center;
        nrTmp.color = new Color(1f, 0.85f, 0.2f);
        newRec.SetActive(false);

        title = AddTmp("ResultTitle", v.transform, font, "RESULT", 30, TextAlignmentOptions.Center, 48f);
        body = AddTmp("ResultBody", v.transform, font, "—", 24, TextAlignmentOptions.Center, 120f);
        reward = AddTmp("Reward", v.transform, font, "—", 20, TextAlignmentOptions.Center, 40f);

        var row = new GameObject("Buttons");
        row.transform.SetParent(v.transform, false);
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 0.5f);
        rowRt.anchorMax = new Vector2(1f, 0.5f);
        rowRt.sizeDelta = new Vector2(0f, 56f);
        rowRt.anchoredPosition = Vector2.zero;
        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 24f;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 56f;

        retry = CreateButton("Retry", row.transform, font, "재도전");
        menu = CreateButton("MainMenu", row.transform, font, "메인으로");
    }

    private static TextMeshProUGUI AddTmp(string name, Transform parent, TMP_FontAsset font, string text, float size, TextAlignmentOptions align, float preferredHeight)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = preferredHeight;
        le.flexibleWidth = 1f;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        ApplyFont(tmp, font);
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    private static void ApplyFont(TMP_Text tmp, TMP_FontAsset font)
    {
        if (font != null)
        {
            tmp.font = font;
        }
    }

    private static Button CreateButton(string name, Transform parent, TMP_FontAsset font, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 52f;
        le.minHeight = 48f;
        le.flexibleWidth = 1f;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 52f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.24f, 0.32f, 1f);
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tRt = textGo.AddComponent<RectTransform>();
        StretchFull(tRt);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        ApplyFont(tmp, font);
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return btn;
    }
}
#endif
