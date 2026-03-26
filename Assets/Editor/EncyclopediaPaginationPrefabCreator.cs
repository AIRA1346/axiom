using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// 도감 페이지네이션 UI 프리팹 생성.
/// Menu: Tools > Create Encyclopedia Pagination Prefab
/// 생성 후 Encyclopedia 패널에 배치하고 EncyclopediaBrowserController의 Pagination 필드에 할당하세요.
/// </summary>
public static class EncyclopediaPaginationPrefabCreator
{
    private const string PrefabPath = "Assets/Prefabs/EncyclopediaPaginationPrefab.prefab";

    [MenuItem("Tools/Create Encyclopedia Pagination Prefab")]
    public static void Create()
    {
        GameObject root = new GameObject("EncyclopediaPagination");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(400, 36);
        rootRect.anchorMin = new Vector2(0.5f, 0);
        rootRect.anchorMax = new Vector2(0.5f, 0);

        var hLayout = root.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 12;
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;
        hLayout.padding = new RectOffset(8, 8, 4, 4);

        // 이전 버튼
        GameObject prevBtn = CreateButton("PrevPage", "◀ 이전", 90);
        prevBtn.transform.SetParent(root.transform, false);

        // 페이지 번호 텍스트
        GameObject pageTextObj = new GameObject("PageText");
        pageTextObj.transform.SetParent(root.transform, false);
        var pageTextRect = pageTextObj.AddComponent<RectTransform>();
        pageTextRect.sizeDelta = new Vector2(80, 28);
        var pageText = pageTextObj.AddComponent<TextMeshProUGUI>();
        pageText.text = "1 / 1";
        pageText.fontSize = 14;
        pageText.color = Color.white;
        pageText.alignment = TextAlignmentOptions.Center;
        pageText.raycastTarget = false;

        // 다음 버튼
        GameObject nextBtn = CreateButton("NextPage", "다음 ▶", 90);
        nextBtn.transform.SetParent(root.transform, false);

        string dir = System.IO.Path.GetDirectoryName(PrefabPath);
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();
        Debug.Log($"[Encyclopedia] 페이지네이션 프리팹 생성: {PrefabPath}");
    }

    private static GameObject CreateButton(string name, string label, float width)
    {
        var go = new GameObject(name);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, 28);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.25f, 0.25f, 0.35f, 1f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.9f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        btn.colors = colors;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(go.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 13;
        tmp.color = new Color(0.2f, 0.2f, 0.2f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return go;
    }
}
