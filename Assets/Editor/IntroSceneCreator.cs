using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// IntroScene을 자동 생성합니다. Tools > ARCHÉ > Setup
/// 무채색 UI, ItemDatabase, IntroController 포함.
/// </summary>
public static class IntroSceneCreator
{
    private const string ScenePath = "Assets/Scenes/IntroScene.unity";

    [MenuItem("Tools/ARCHÉ/Setup/Create Intro Scene")]
    [MenuItem("Assets/ARCHÉ/Create Intro Scene")]
    public static void Create()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            if (!AssetDatabase.IsValidFolder("Assets"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            else
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var canvasObj = CreateCanvas();
        CreateIntroUI(canvasObj.transform);
        CreateItemDatabase();
        CreateIntroController(canvasObj);

        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (saved)
        {
            AddToBuildSettings();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("IntroScene 생성 완료", $"저장 경로: {ScenePath}\n\nBuild Settings에 자동 추가되었습니다.", "확인");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ScenePath);
        }
        else
        {
            EditorUtility.DisplayDialog("저장 실패", $"IntroScene을 저장할 수 없습니다.\n경로: {ScenePath}", "확인");
        }
    }

    private static GameObject CreateCanvas()
    {
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        if (!Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>())
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        return canvasObj;
    }

    private static void CreateIntroUI(Transform parent)
    {
        var bg = new GameObject("Background");
        bg.transform.SetParent(parent, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.05f, 1f);

        var logoObj = new GameObject("LogoText");
        logoObj.transform.SetParent(parent, false);
        var logoRect = logoObj.GetComponent<RectTransform>() ?? logoObj.AddComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.5f, 0.65f);
        logoRect.anchorMax = new Vector2(0.5f, 0.65f);
        logoRect.sizeDelta = new Vector2(400, 80);
        logoRect.anchoredPosition = Vector2.zero;
        var logoTmp = logoObj.AddComponent<TextMeshProUGUI>();
        logoTmp.text = "G.S.I";
        logoTmp.fontSize = 48;
        logoTmp.color = Color.white;
        logoTmp.alignment = TextAlignmentOptions.Center;

        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(parent, false);
        var statusRect = statusObj.GetComponent<RectTransform>() ?? statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.4f);
        statusRect.anchorMax = new Vector2(0.5f, 0.4f);
        statusRect.sizeDelta = new Vector2(600, 40);
        statusRect.anchoredPosition = Vector2.zero;
        var statusTmp = statusObj.AddComponent<TextMeshProUGUI>();
        statusTmp.text = "";
        statusTmp.fontSize = 22;
        statusTmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        statusTmp.alignment = TextAlignmentOptions.Center;

        var fadeObj = new GameObject("FadeOverlay");
        fadeObj.transform.SetParent(parent, false);
        var fadeRect = fadeObj.GetComponent<RectTransform>() ?? fadeObj.AddComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;
        var fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.raycastTarget = false;

        logoObj.AddComponent<CanvasRenderer>();
        statusObj.AddComponent<CanvasRenderer>();
        fadeObj.AddComponent<CanvasRenderer>();
    }

    private static void CreateItemDatabase()
    {
        var go = new GameObject("ItemDatabase");
        go.AddComponent<ItemDatabase>();
    }

    private static void CreateIntroController(GameObject canvasObj)
    {
        var controllerObj = new GameObject("IntroController");
        controllerObj.transform.SetParent(canvasObj.transform);

        var controller = controllerObj.AddComponent<IntroController>();

        var logo = canvasObj.transform.Find("LogoText")?.GetComponent<TextMeshProUGUI>();
        var status = canvasObj.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
        var fade = canvasObj.transform.Find("FadeOverlay")?.GetComponent<Image>();

        var so = new SerializedObject(controller);
        so.FindProperty("_statusText").objectReferenceValue = status;
        so.FindProperty("_logoText").objectReferenceValue = logo;
        so.FindProperty("_fadeOverlay").objectReferenceValue = fade;
        so.FindProperty("_mainSceneName").stringValue = "SampleScene";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
        {
            if (s.path.Contains("IntroScene"))
                return;
        }

        var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        newScenes[0] = new EditorBuildSettingsScene(ScenePath, true);
        for (int i = 0; i < scenes.Length; i++)
        {
            newScenes[i + 1] = scenes[i];
        }
        EditorBuildSettings.scenes = newScenes;
    }
}
