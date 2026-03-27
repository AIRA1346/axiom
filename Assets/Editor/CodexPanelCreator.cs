using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// ARCHÉ ADDS 도서관(Codex) 패널을 생성하는 에디터 도구.
/// Menu: Tools > Create Codex Panel
/// Split View: 좌측 목차(Navigation) + 우측 본문(Viewer). 무채색 공식 문서 톤.
/// </summary>
public static class CodexPanelCreator
{
    private const string PrefabPath = "Assets/Prefabs/CodexPanelPrefab.prefab";

    [MenuItem("Tools/Create Codex Panel Prefab")]
    public static void CreateCodexPanelPrefab()
    {
        GameObject root = new GameObject("CodexPanel");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.AddComponent<CanvasRenderer>();
        var rootImage = root.AddComponent<Image>();
        rootImage.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        var controller = root.AddComponent<CodexController>();

        // ---- 상단 바 (나가기 + Breadcrumb) ----
        GameObject topBar = new GameObject("TopBar");
        topBar.transform.SetParent(root.transform, false);
        var topBarRect = topBar.AddComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0, 1);
        topBarRect.anchorMax = new Vector2(1, 1);
        topBarRect.pivot = new Vector2(0.5f, 1f);
        topBarRect.anchoredPosition = Vector2.zero;
        topBarRect.sizeDelta = new Vector2(0, 48);
        var topBarImage = topBar.AddComponent<Image>();
        topBarImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        GameObject exitBtn = new GameObject("ExitButton");
        exitBtn.transform.SetParent(topBar.transform, false);
        var exitRect = exitBtn.AddComponent<RectTransform>();
        exitRect.anchorMin = new Vector2(0, 0.5f);
        exitRect.anchorMax = new Vector2(0, 0.5f);
        exitRect.pivot = new Vector2(0, 0.5f);
        exitRect.anchoredPosition = new Vector2(60, 0);
        exitRect.sizeDelta = new Vector2(100, 36);
        var exitImage = exitBtn.AddComponent<Image>();
        exitImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        var exitButton = exitBtn.AddComponent<Button>();

        GameObject exitTextObj = new GameObject("Text");
        exitTextObj.transform.SetParent(exitBtn.transform, false);
        var exitTextRect = exitTextObj.AddComponent<RectTransform>();
        exitTextRect.anchorMin = Vector2.zero;
        exitTextRect.anchorMax = Vector2.one;
        exitTextRect.offsetMin = Vector2.zero;
        exitTextRect.offsetMax = Vector2.zero;
        var exitText = exitTextObj.AddComponent<TextMeshProUGUI>();
        exitText.text = "나가기";
        exitText.fontSize = 14;
        exitText.color = Color.white;
        exitText.alignment = TextAlignmentOptions.Center;
        exitText.raycastTarget = false;

        GameObject breadcrumbObj = new GameObject("BreadcrumbText");
        breadcrumbObj.transform.SetParent(topBar.transform, false);
        var breadcrumbRect = breadcrumbObj.AddComponent<RectTransform>();
        breadcrumbRect.anchorMin = new Vector2(0, 0.5f);
        breadcrumbRect.anchorMax = new Vector2(1, 0.5f);
        breadcrumbRect.pivot = new Vector2(0.5f, 0.5f);
        breadcrumbRect.anchoredPosition = new Vector2(0, 0);
        breadcrumbRect.sizeDelta = new Vector2(-200, 28);
        var breadcrumbText = breadcrumbObj.AddComponent<TextMeshProUGUI>();
        breadcrumbText.text = "ARCHÉ · 도서관";
        breadcrumbText.fontSize = 14;
        breadcrumbText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        breadcrumbText.alignment = TextAlignmentOptions.Center;
        breadcrumbText.raycastTarget = false;

        // ---- 본문 영역 (Split: 좌측 Navigation + 우측 Viewer) ----
        GameObject bodyArea = new GameObject("BodyArea");
        bodyArea.transform.SetParent(root.transform, false);
        var bodyRect = bodyArea.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0, 0);
        bodyRect.anchorMax = new Vector2(1, 1);
        bodyRect.offsetMin = new Vector2(0, 0);
        bodyRect.offsetMax = new Vector2(0, -48);

        // 좌측 Navigation (Scroll View)
        GameObject leftNav = new GameObject("Navigation");
        leftNav.transform.SetParent(bodyArea.transform, false);
        var leftRect = leftNav.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0.35f, 1);
        leftRect.offsetMin = new Vector2(8, 8);
        leftRect.offsetMax = new Vector2(-4, -8);
        var leftImage = leftNav.AddComponent<Image>();
        leftImage.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        GameObject scrollView = new GameObject("Scroll View");
        scrollView.transform.SetParent(leftNav.transform, false);
        var scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;
        var scroll = scrollView.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        var viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 400);

        scroll.viewport = viewportRect;
        scroll.content = contentRect;

        var virtualizedScroll = scrollView.AddComponent<VirtualizedCodexScrollView>();
        var soScroll = new SerializedObject(virtualizedScroll);
        soScroll.FindProperty("_scrollRect").objectReferenceValue = scroll;
        soScroll.FindProperty("_viewport").objectReferenceValue = viewportRect;
        soScroll.FindProperty("_content").objectReferenceValue = contentRect;
        soScroll.FindProperty("_cellHeight").floatValue = 48f;
        soScroll.FindProperty("_cellSpacing").floatValue = 2f;
        var cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CodexCellPrefab.prefab");
        if (cellPrefab != null)
        {
            soScroll.FindProperty("_cellPrefab").objectReferenceValue = cellPrefab;
        }
        soScroll.ApplyModifiedPropertiesWithoutUndo();

        // 우측 Viewer
        GameObject rightViewer = new GameObject("Viewer");
        rightViewer.transform.SetParent(bodyArea.transform, false);
        var rightRect = rightViewer.AddComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.35f, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.offsetMin = new Vector2(4, 8);
        rightRect.offsetMax = new Vector2(-8, -8);
        var rightImage = rightViewer.AddComponent<Image>();
        rightImage.color = new Color(1f, 1f, 1f, 1f);

        GameObject viewerScroll = new GameObject("ViewerScroll");
        viewerScroll.transform.SetParent(rightViewer.transform, false);
        var viewerScrollRect = viewerScroll.AddComponent<RectTransform>();
        viewerScrollRect.anchorMin = Vector2.zero;
        viewerScrollRect.anchorMax = Vector2.one;
        viewerScrollRect.offsetMin = new Vector2(16, 16);
        viewerScrollRect.offsetMax = new Vector2(-16, -16);
        viewerScroll.AddComponent<RectMask2D>();

        GameObject bodyTextObj = new GameObject("BodyText");
        bodyTextObj.transform.SetParent(viewerScroll.transform, false);
        var bodyTextRect = bodyTextObj.AddComponent<RectTransform>();
        bodyTextRect.anchorMin = new Vector2(0, 1);
        bodyTextRect.anchorMax = new Vector2(1, 1);
        bodyTextRect.pivot = new Vector2(0.5f, 1f);
        bodyTextRect.anchoredPosition = Vector2.zero;
        bodyTextRect.sizeDelta = new Vector2(0, 0);
        var bodyText = bodyTextObj.AddComponent<TextMeshProUGUI>();
        bodyText.text = "";
        bodyText.fontSize = 16;
        bodyText.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        bodyText.richText = true;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.overflowMode = TMPro.TextOverflowModes.Overflow;

        var contentSizeFitter = bodyTextObj.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRectViewer = viewerScroll.AddComponent<ScrollRect>();
        scrollRectViewer.content = bodyTextRect;
        scrollRectViewer.viewport = viewerScrollRect;
        scrollRectViewer.horizontal = false;
        scrollRectViewer.vertical = true;

        GameObject emptyState = new GameObject("EmptyState");
        emptyState.transform.SetParent(rightViewer.transform, false);
        var emptyRect = emptyState.AddComponent<RectTransform>();
        emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
        emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
        emptyRect.anchoredPosition = Vector2.zero;
        emptyRect.sizeDelta = new Vector2(300, 60);
        var emptyText = emptyState.AddComponent<TextMeshProUGUI>();
        emptyText.text = "목차에서 항목을 선택하세요";
        emptyText.fontSize = 18;
        emptyText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        emptyText.alignment = TextAlignmentOptions.Center;
        emptyText.raycastTarget = false;

        GameObject loadingObj = new GameObject("LoadingIndicator");
        loadingObj.transform.SetParent(rightViewer.transform, false);
        var loadingRect = loadingObj.AddComponent<RectTransform>();
        loadingRect.anchorMin = new Vector2(0.5f, 0.5f);
        loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
        loadingRect.anchoredPosition = Vector2.zero;
        loadingRect.sizeDelta = new Vector2(120, 40);
        var loadingText = loadingObj.AddComponent<TextMeshProUGUI>();
        loadingText.text = "로딩 중...";
        loadingText.fontSize = 14;
        loadingText.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        loadingText.alignment = TextAlignmentOptions.Center;
        loadingObj.SetActive(false);

        var so = new SerializedObject(controller);
        so.FindProperty("_exitButton").objectReferenceValue = exitButton;
        so.FindProperty("_breadcrumbText").objectReferenceValue = breadcrumbText;
        so.FindProperty("_indexScrollView").objectReferenceValue = virtualizedScroll;
        so.FindProperty("_bodyViewer").objectReferenceValue = bodyText;
        so.FindProperty("_loadingIndicator").objectReferenceValue = loadingObj;
        so.FindProperty("_emptyStateRoot").objectReferenceValue = emptyState;
        so.ApplyModifiedPropertiesWithoutUndo();

        string dir = System.IO.Path.GetDirectoryName(PrefabPath);
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();
        Debug.Log($"[Codex] 패널 프리팹 생성 완료: {PrefabPath}. Cell Prefab은 별도 생성 후 Index Scroll View에 할당하세요.");
    }
}
