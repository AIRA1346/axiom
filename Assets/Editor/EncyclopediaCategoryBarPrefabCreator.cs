using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// 대시보드 카테고리 진행률 바 프리팹 생성.
/// Menu: Tools > Create Encyclopedia Category Bar Prefab
/// </summary>
public static class EncyclopediaCategoryBarPrefabCreator
{
    private const string PrefabPath = "Assets/Prefabs/EncyclopediaCategoryBarPrefab.prefab";

    [MenuItem("Tools/ARCHÉ/Encyclopedia/Create Category Bar Prefab")]
    public static void Create()
    {
        GameObject root = new GameObject("CategoryBar");
        var rect = root.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 24);

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(root.transform, false);
        var fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.6f, 0.9f, 0.8f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 0.5f;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(root.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(6, 0);
        labelRect.offsetMax = new Vector2(-6, 0);
        var labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = "Material: 15/100 (15%)";
        labelText.fontSize = 12;
        labelText.color = Color.white;
        labelText.raycastTarget = false;

        string dir = System.IO.Path.GetDirectoryName(PrefabPath);
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();
        Debug.Log($"[Encyclopedia] 카테고리 바 프리팹 생성: {PrefabPath}");
    }
}
