using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// 도감 아이템 셀 프리팹을 생성하는 에디터 도구.
/// Menu: Tools > Create Encyclopedia Cell Prefab
/// </summary>
public static class EncyclopediaCellPrefabCreator
{
    private const string PrefabPath = "Assets/Prefabs/EncyclopediaCellPrefab.prefab";

    [MenuItem("Tools/ARCHÉ/Encyclopedia/Create Cell Prefab")]
    public static void CreateEncyclopediaCellPrefab()
    {
        GameObject root = new GameObject("EncyclopediaCell");
        var rect = root.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 80);

        var canvasRenderer = root.AddComponent<CanvasRenderer>();
        var image = root.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.25f, 0.95f);
        var button = root.AddComponent<Button>();

        var cell = root.AddComponent<EncyclopediaItemCell>();

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(root.transform, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(45, 0);
        iconRect.sizeDelta = new Vector2(64, 64);
        var iconImage = iconObj.AddComponent<Image>();
        iconImage.raycastTarget = false;

        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(root.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.5f);
        nameRect.anchorMax = new Vector2(1, 0.5f);
        nameRect.pivot = new Vector2(0, 0.5f);
        nameRect.anchoredPosition = new Vector2(120, 0);
        nameRect.sizeDelta = new Vector2(-130, 36);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "Item Name";
        nameText.fontSize = 18;
        nameText.raycastTarget = false;

        GameObject lockedObj = new GameObject("LockedOverlay");
        lockedObj.transform.SetParent(root.transform, false);
        var lockedRect = lockedObj.AddComponent<RectTransform>();
        lockedRect.anchorMin = Vector2.zero;
        lockedRect.anchorMax = Vector2.one;
        lockedRect.offsetMin = Vector2.zero;
        lockedRect.offsetMax = Vector2.zero;
        var lockedImage = lockedObj.AddComponent<Image>();
        lockedImage.color = new Color(0, 0, 0, 0.3f);
        lockedImage.raycastTarget = false;

        var so = new SerializedObject(cell);
        so.FindProperty("_iconImage").objectReferenceValue = iconImage;
        so.FindProperty("_nameText").objectReferenceValue = nameText;
        so.FindProperty("_lockedOverlay").objectReferenceValue = lockedObj;
        so.FindProperty("_backgroundImage").objectReferenceValue = image;
        so.ApplyModifiedPropertiesWithoutUndo();

        lockedObj.SetActive(false);

        string dir = System.IO.Path.GetDirectoryName(PrefabPath);
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();
        Debug.Log($"[Encyclopedia] 프리팹 생성 완료: {PrefabPath}");
    }
}
