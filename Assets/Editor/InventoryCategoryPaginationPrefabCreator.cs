using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// 인벤토리 카테고리 선택(대/중/소분류) + 페이지네이션 UI 프리팹 생성.
/// SimpleCategoryDropdown 기반으로 Button 클릭이 확실히 동작하는 드롭다운을 만듭니다.
/// Menu: Tools > Create Inventory Category Pagination Prefab
/// </summary>
public static class InventoryCategoryPaginationPrefabCreator
{
    private const string PrefabPath = "Assets/Prefabs/InventoryCategoryPaginationPrefab.prefab";
    private const int UILayer = 5;

    [MenuItem("Tools/ARCHÉ/Inventory/Create Category Pagination Prefab")]
    public static void Create()
    {
        var root = new GameObject("InventoryCategoryPagination");
        root.layer = UILayer;
        var rootRect = root.AddComponent<RectTransform>();
        // 상단 가로 스트레치: 패널 상단에 배치되도록
        rootRect.anchorMin = new Vector2(0, 1);
        rootRect.anchorMax = new Vector2(1, 1);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(0, 120);
        var rootLayout = root.AddComponent<LayoutElement>();
        rootLayout.preferredHeight = 120;
        rootLayout.flexibleHeight = 0;
        var vLayout = root.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 8;
        vLayout.padding = new RectOffset(8, 8, 8, 8);
        vLayout.childForceExpandWidth = true;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandHeight = false;

        var categoryRow = new GameObject("CategoryRow");
        categoryRow.layer = UILayer;
        categoryRow.transform.SetParent(root.transform, false);
        var catRect = categoryRow.AddComponent<RectTransform>();
        catRect.sizeDelta = new Vector2(0, 32);
        var catLayout = categoryRow.AddComponent<HorizontalLayoutGroup>();
        catLayout.spacing = 12;
        catLayout.childForceExpandWidth = true;
        catLayout.childControlHeight = false;

        var mainDropdown = CreateSimpleDropdown("MainCategoryDropdown", "대분류");
        mainDropdown.transform.SetParent(categoryRow.transform, false);

        var middleDropdown = CreateSimpleDropdown("MiddleCategoryDropdown", "중분류");
        middleDropdown.transform.SetParent(categoryRow.transform, false);

        var subDropdown = CreateSimpleDropdown("SubCategoryDropdown", "소분류");
        subDropdown.transform.SetParent(categoryRow.transform, false);

        var pageRow = CreatePaginationRow();
        pageRow.transform.SetParent(root.transform, false);

        var dir = System.IO.Path.GetDirectoryName(PrefabPath);
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();

        Debug.Log($"[Inventory] SimpleCategoryDropdown 기반 프리팹 생성 완료: {PrefabPath}\n\n" +
            "기존 인벤토리 패널에 이 프리팹이 있다면:\n" +
            "1. 기존 InventoryCategoryPagination 인스턴스 삭제\n" +
            "2. 새 프리팹을 InventoryPanel에 배치\n" +
            "3. Tools > Fix Inventory Panel Layout 실행 (카테고리가 상단에 보이도록)\n" +
            "4. InventoryController에서 드롭다운·페이지네이션 참조 재연결");
        EditorUtility.DisplayDialog("완료", "프리팹 생성 완료.\n\nTools > Fix Inventory Panel Layout 을 실행하여 카테고리를 상단에 배치하세요.", "확인");
    }

    [MenuItem("Tools/ARCHÉ/Inventory/Fix Panel Layout")]
    public static void FixInventoryPanelLayout()
    {
        var panel = GameObject.Find("InventoryPanel");
        if (panel == null)
        {
            EditorUtility.DisplayDialog("오류", "InventoryPanel을 찾을 수 없습니다.\nSampleScene을 열고 다시 시도하세요.", "확인");
            return;
        }

        var rect = panel.GetComponent<RectTransform>();
        if (rect == null) return;

        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
        }

        // 카테고리 프리팹을 상단에 배치 (Index 0 또는 1 - InventoryController가 0이면 1)
        var category = FindChildByName(panel.transform, "InventoryCategoryPaginationPrefab");
        var controller = panel.transform.Find("InventoryController");
        if (category != null)
        {
            int targetIndex = (controller != null && controller.GetSiblingIndex() == 0) ? 1 : 0;
            if (category.GetSiblingIndex() > targetIndex)
                category.SetSiblingIndex(targetIndex);
        }

        // Scroll View에 flexible height
        var scrollView = FindChildByName(panel.transform, "Scroll View");
        if (scrollView != null)
        {
            var le = scrollView.GetComponent<LayoutElement>();
            if (le == null) le = scrollView.gameObject.AddComponent<LayoutElement>();
            le.flexibleHeight = 1;
        }

        EditorUtility.DisplayDialog("완료", "InventoryPanel 레이아웃을 수정했습니다.\n카테고리가 상단에 표시됩니다.", "확인");
    }

    private static Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform c in parent)
        {
            if (c.name == name || c.name.StartsWith(name + "(Clone)")) return c;
        }
        return null;
    }

    /// <summary>
    /// Button 기반 SimpleCategoryDropdown 구조를 생성합니다. 클릭이 확실히 동작합니다.
    /// </summary>
    private static GameObject CreateSimpleDropdown(string name, string captionText)
    {
        var root = new GameObject(name);
        root.layer = UILayer;
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(0, 32);
        var layoutElem = root.AddComponent<LayoutElement>();
        layoutElem.flexibleWidth = 1;

        // 트리거 버튼 (캡션 표시 영역)
        var trigger = new GameObject("Trigger");
        trigger.layer = UILayer;
        trigger.transform.SetParent(root.transform, false);
        var triggerRect = trigger.AddComponent<RectTransform>();
        triggerRect.anchorMin = Vector2.zero;
        triggerRect.anchorMax = Vector2.one;
        triggerRect.offsetMin = Vector2.zero;
        triggerRect.offsetMax = Vector2.zero;
        var triggerImage = trigger.AddComponent<Image>();
        triggerImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd")
            ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        triggerImage.color = new Color(0.22f, 0.22f, 0.28f, 1f);
        trigger.AddComponent<Button>();

        var caption = new GameObject("Caption");
        caption.layer = UILayer;
        caption.transform.SetParent(trigger.transform, false);
        var captionRect = caption.AddComponent<RectTransform>();
        captionRect.anchorMin = new Vector2(0, 0);
        captionRect.anchorMax = new Vector2(1, 1);
        captionRect.offsetMin = new Vector2(10, 2);
        captionRect.offsetMax = new Vector2(-24, -2);
        var tmp = caption.AddComponent<TextMeshProUGUI>();
        tmp.text = captionText;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;

        // 리스트 패널 (드롭다운 열릴 때 표시)
        var listPanel = new GameObject("ListPanel");
        listPanel.layer = UILayer;
        listPanel.transform.SetParent(root.transform, false);
        listPanel.SetActive(false);
        var listPanelRect = listPanel.AddComponent<RectTransform>();
        listPanelRect.anchorMin = new Vector2(0, 0);
        listPanelRect.anchorMax = new Vector2(1, 0);
        listPanelRect.pivot = new Vector2(0.5f, 0);
        listPanelRect.anchoredPosition = new Vector2(0, -2);
        listPanelRect.sizeDelta = new Vector2(0, 180);
        listPanelRect.SetSiblingIndex(0); // 트리거 아래에
        var listBg = listPanel.AddComponent<Image>();
        listBg.sprite = triggerImage.sprite;
        listBg.color = new Color(0.18f, 0.18f, 0.22f, 1f);
        listPanel.AddComponent<ScrollRect>();

        var viewport = new GameObject("Viewport");
        viewport.layer = UILayer;
        viewport.transform.SetParent(listPanel.transform, false);
        var viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(4, 4);
        viewportRect.offsetMax = new Vector2(-4, -4);
        var viewportMask = viewport.AddComponent<UnityEngine.UI.Mask>();
        viewportMask.showMaskGraphic = false;
        var viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = new Color(1, 1, 1, 0.01f);
        viewportImg.raycastTarget = false;

        var listContent = new GameObject("Content");
        listContent.layer = UILayer;
        listContent.transform.SetParent(viewport.transform, false);
        var contentRect = listContent.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);
        var contentVlg = listContent.AddComponent<VerticalLayoutGroup>();
        contentVlg.spacing = 0;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;
        contentVlg.childControlHeight = true;
        listContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = listPanel.GetComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        // 아이템 템플릿
        var itemTemplate = new GameObject("ItemTemplate");
        itemTemplate.layer = UILayer;
        itemTemplate.transform.SetParent(listContent.transform, false);
        itemTemplate.SetActive(false);
        var itemRect = itemTemplate.AddComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0, 28);
        var itemImg = itemTemplate.AddComponent<Image>();
        itemImg.color = new Color(0.28f, 0.28f, 0.35f, 1f);
        itemImg.raycastTarget = true;
        itemTemplate.AddComponent<Button>();

        var itemLabel = new GameObject("Label");
        itemLabel.layer = UILayer;
        itemLabel.transform.SetParent(itemTemplate.transform, false);
        var itemLabelRect = itemLabel.AddComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(10, 2);
        itemLabelRect.offsetMax = new Vector2(-10, -2);
        var itemTmp = itemLabel.AddComponent<TextMeshProUGUI>();
        itemTmp.text = "Option";
        itemTmp.fontSize = 13;
        itemTmp.alignment = TextAlignmentOptions.Left;
        itemTmp.raycastTarget = false;

        var itemLayoutElem = itemTemplate.AddComponent<LayoutElement>();
        itemLayoutElem.minHeight = 28;
        itemLayoutElem.preferredHeight = 28;

        var simpleDropdown = root.AddComponent<SimpleCategoryDropdown>();
        var so = new SerializedObject(simpleDropdown);
        so.FindProperty("_triggerButton").objectReferenceValue = triggerRect;
        so.FindProperty("_captionText").objectReferenceValue = tmp;
        so.FindProperty("_listPanel").objectReferenceValue = listPanelRect;
        so.FindProperty("_listContent").objectReferenceValue = contentRect;
        so.FindProperty("_itemTemplate").objectReferenceValue = itemTemplate;
        so.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject CreatePaginationRow()
    {
        var pageRow = new GameObject("PaginationRow");
        pageRow.layer = UILayer;
        var pageRect = pageRow.AddComponent<RectTransform>();
        pageRect.sizeDelta = new Vector2(0, 36);
        var pageLayout = pageRow.AddComponent<HorizontalLayoutGroup>();
        pageLayout.spacing = 12;
        pageLayout.childAlignment = TextAnchor.MiddleCenter;
        pageLayout.childForceExpandWidth = false;

        var prevBtn = CreateButton("PrevPage", "◀ 이전", 90);
        prevBtn.layer = UILayer;
        prevBtn.transform.SetParent(pageRow.transform, false);

        var pageTextObj = new GameObject("PageText");
        pageTextObj.layer = UILayer;
        pageTextObj.transform.SetParent(pageRow.transform, false);
        var pageTextRect = pageTextObj.AddComponent<RectTransform>();
        pageTextRect.sizeDelta = new Vector2(80, 28);
        var pageText = pageTextObj.AddComponent<TextMeshProUGUI>();
        pageText.text = "1 / 1";
        pageText.fontSize = 14;
        pageText.alignment = TextAlignmentOptions.Center;
        pageText.raycastTarget = false;

        var nextBtn = CreateButton("NextPage", "다음 ▶", 90);
        nextBtn.layer = UILayer;
        nextBtn.transform.SetParent(pageRow.transform, false);

        return pageRow;
    }

    private static GameObject CreateButton(string name, string label, float width)
    {
        var go = new GameObject(name);
        go.layer = UILayer;
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, 28);
        var image = go.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd")
            ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        image.color = new Color(0.25f, 0.25f, 0.35f, 1f);
        go.AddComponent<Button>();

        var textObj = new GameObject("Text");
        textObj.layer = UILayer;
        textObj.transform.SetParent(go.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 13;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return go;
    }
}
