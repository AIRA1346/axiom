using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점·인벤토리 등에서 공통으로 쓰는 세로 ScrollView + List 영역 생성 및 리스트 레이아웃 갱신.
/// </summary>
public static class GsiShopLikeListScrollUi
{
    /// <summary>
    /// List 자식으로 두는 비활성 풀 노드 이름 접두사. <see cref="IsRowPoolHolderName"/> 과 맞춥니다.
    /// </summary>
    public const string RowPoolChildNamePrefix = "_GsiRowPool";

    /// <summary>
    /// 상점 등 단일 풀용 기본 이름.
    /// </summary>
    public const string DefaultRowPoolChildName = RowPoolChildNamePrefix;

    public const string InventorySectionRowPoolChildName = RowPoolChildNamePrefix + "_InvSection";
    public const string InventorySkinRowPoolChildName = RowPoolChildNamePrefix + "_InvSkin";

    public static bool IsRowPoolHolderName(string childName)
    {
        return !string.IsNullOrEmpty(childName) && childName.StartsWith(RowPoolChildNamePrefix, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// ShopCanvas / InventoryCanvas 루트 아래에 ScrollView → Viewport → List 를 붙입니다.
    /// </summary>
    public static void BuildScrollAreaUnderCanvas(Transform canvasTransform)
    {
        var scrollGo = new GameObject("ScrollView", typeof(RectTransform));
        scrollGo.transform.SetParent(canvasTransform, false);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        float top = GsiUiScreenLayout.ScrollContentTopMargin;
        scrollRt.offsetMin = new Vector2(GsiUiScreenLayout.ScrollHorizontalInset, GsiUiScreenLayout.ScrollBottomInset);
        scrollRt.offsetMax = new Vector2(-GsiUiScreenLayout.ScrollHorizontalInset, -top);

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 52f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.125f;
        scroll.elasticity = 0.12f;

        var viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRt = viewport.GetComponent<RectTransform>();
        StretchFull(vpRt);
        viewport.AddComponent<RectMask2D>();
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0f);
        vpImg.raycastTarget = true;

        var listGo = new GameObject("List", typeof(RectTransform));
        listGo.transform.SetParent(viewport.transform, false);
        var listRt = listGo.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0f, 1f);
        listRt.anchorMax = new Vector2(1f, 1f);
        listRt.pivot = new Vector2(0.5f, 1f);
        listRt.anchoredPosition = Vector2.zero;
        listRt.sizeDelta = new Vector2(0f, 0f);

        var listV = listGo.AddComponent<VerticalLayoutGroup>();
        listV.spacing = GsiUiScreenLayout.ListVerticalSpacing;
        listV.childAlignment = TextAnchor.UpperCenter;
        listV.childControlWidth = true;
        listV.childControlHeight = true;
        listV.childForceExpandWidth = true;
        listV.padding = GsiUiScreenLayout.ListPadding;

        var fitter = listGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vpRt;
        scroll.content = listRt;
    }

    /// <summary>
    /// 행 GameObject 풀 부모(List의 자식, 비활성). 타입별로 이름을 나누면 <see cref="GsiRuntimeUiRowPool"/> 스택이 섞이지 않습니다.
    /// </summary>
    public static Transform EnsureRowPoolRoot(Transform listTransform, string childName = DefaultRowPoolChildName)
    {
        Transform existing = listTransform.Find(childName);
        if (existing != null)
        {
            return existing;
        }

        var poolGo = new GameObject(childName, typeof(RectTransform));
        poolGo.transform.SetParent(listTransform, false);
        poolGo.SetActive(false);
        var le = poolGo.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
        var rt = poolGo.GetComponent<RectTransform>();
        rt.sizeDelta = Vector2.zero;
        poolGo.transform.SetAsFirstSibling();
        return poolGo.transform;
    }

    /// <summary>
    /// 리스트 내용 변경 후 레이아웃을 갱신합니다.
    /// <paramref name="scrollToTop"/> 가 false이면 갱신 직전의 세로 정규화 스크롤 위치를 유지합니다(데이터만 바뀐 갱신에 적합).
    /// </summary>
    /// <param name="scrollViewPathFromUiRoot">예: "ShopCanvas/ScrollView", "InventoryCanvas/ScrollView"</param>
    public static void RebuildListLayout(Transform uiRoot, string scrollViewPathFromUiRoot, RectTransform listRect,
        bool scrollToTop)
    {
        Transform scrollT = uiRoot != null ? uiRoot.Find(scrollViewPathFromUiRoot) : null;
        ScrollRect scrollRect = scrollT != null ? scrollT.GetComponent<ScrollRect>() : null;
        float prevNorm = 1f;
        if (!scrollToTop && scrollRect != null)
        {
            prevNorm = scrollRect.verticalNormalizedPosition;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = scrollToTop ? 1f : Mathf.Clamp01(prevNorm);
        }
    }

    /// <summary>
    /// 리스트 내용 변경 후 레이아웃을 갱신하고 스크롤을 맨 위로 둡니다.
    /// </summary>
    public static void RebuildListLayoutAndScrollToTop(Transform uiRoot, string scrollViewPathFromUiRoot, RectTransform listRect)
    {
        RebuildListLayout(uiRoot, scrollViewPathFromUiRoot, listRect, scrollToTop: true);
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
