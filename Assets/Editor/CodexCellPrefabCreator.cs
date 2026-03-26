using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// 도서관 목차 셀 프리팹을 생성하는 에디터 도구.
/// Menu: Tools > Create Codex Cell Prefab
/// 무채색(Black, White, Gray) 공식 문서 톤.
/// </summary>
public static class CodexCellPrefabCreator
{
    private const string PrefabPath = "Assets/Prefabs/CodexCellPrefab.prefab";

    [MenuItem("Tools/Create Codex Cell Prefab")]
    public static void CreateCodexCellPrefab()
    {
        GameObject root = new GameObject("CodexCell");
        var rect = root.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280, 48);

        var canvasRenderer = root.AddComponent<CanvasRenderer>();
        var image = root.AddComponent<Image>();
        image.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        image.raycastTarget = true;
        var button = root.AddComponent<Button>();

        root.AddComponent<CodexEntryCell>();

        GameObject labelObj = new GameObject("TitleText");
        labelObj.transform.SetParent(root.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(1, 0.5f);
        labelRect.pivot = new Vector2(0, 0.5f);
        labelRect.anchoredPosition = new Vector2(12, 0);
        labelRect.sizeDelta = new Vector2(-24, 28);
        var labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = "목차 항목";
        labelText.fontSize = 14;
        labelText.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        labelText.raycastTarget = false;

        string dir = System.IO.Path.GetDirectoryName(PrefabPath);
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();
        Debug.Log($"[Codex] 셀 프리팹 생성 완료: {PrefabPath}");
    }
}
