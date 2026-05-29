using ArchE.AsyncVersus;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Versus 비동기 점수 업로드(HTTP)용 PlayerPrefs 설정을 에디터에서 편집합니다.
/// 메뉴: Tools/GSI/Versus Async/Backend Upload Settings
/// </summary>
public sealed class VersusAsyncBackendEditorWindow : EditorWindow
{
    private const string MenuPath = "Tools/GSI/Versus Async/Backend Upload Settings";

    private string _baseUrl = string.Empty;
    private string _relativePath = string.Empty;
    private string _apiKey = string.Empty;
    private int _timeoutSec = 25;
    private bool _useBearer = true;
    private Vector2 _scroll;

    [MenuItem(MenuPath)]
    private static void Open()
    {
        VersusAsyncBackendEditorWindow win = GetWindow<VersusAsyncBackendEditorWindow>(true, "Versus upload (PlayerPrefs)");
        win.minSize = new Vector2(480, 320);
        win.LoadFromPrefs();
    }

    [MenuItem("Tools/GSI/Versus Async/Disable HTTP upload (clear base URL)")]
    private static void MenuDisableHttp()
    {
        if (!EditorUtility.DisplayDialog(
                "Versus upload",
                "Clear base URL only? API key and path prefs stay as-is. HTTP upload will be off until you set a base URL again.",
                "OK",
                "Cancel"))
        {
            return;
        }

        VersusAsyncBackendSettings.DisableHttpUpload();
        Debug.Log("[VersusAsync] HTTP upload disabled (base URL cleared).");
    }

    [MenuItem("Tools/GSI/Versus Async/Delete all Versus upload PlayerPrefs")]
    private static void MenuDeleteAll()
    {
        if (!EditorUtility.DisplayDialog(
                "Versus upload",
                "Remove all stored Versus upload keys (URL, path, API key, timeout, auth mode)?",
                "Delete",
                "Cancel"))
        {
            return;
        }

        VersusAsyncBackendSettings.DeleteAllStoredKeys();
        Debug.Log("[VersusAsync] All Versus upload PlayerPrefs keys deleted.");
    }

    private void OnEnable()
    {
        LoadFromPrefs();
    }

    private void LoadFromPrefs()
    {
        _baseUrl = VersusAsyncBackendSettings.BaseUrl;
        string storedPath = PlayerPrefs.GetString(VersusAsyncBackendSettings.PrefsRelativePath, string.Empty);
        _relativePath = storedPath;
        _apiKey = VersusAsyncBackendSettings.ApiKey;
        _timeoutSec = VersusAsyncBackendSettings.TimeoutSeconds;
        _useBearer = VersusAsyncBackendSettings.UseBearerAuth;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Values are stored in PlayerPrefs (same as runtime). Enter Play Mode after Apply for GameManager to pick up changes, or call VersusAsyncBridge.ApplyTransportFromSettings() yourself.",
            MessageType.Info);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _baseUrl = EditorGUILayout.TextField("Base URL (no trailing slash)", _baseUrl);
        _relativePath = EditorGUILayout.TextField("Relative path (empty = default)", _relativePath);
        EditorGUILayout.LabelField("Default path if empty", VersusAsyncBackendSettings.DefaultRelativePath);
        _apiKey = EditorGUILayout.PasswordField("API key (optional)", _apiKey);
        _timeoutSec = EditorGUILayout.IntSlider("Timeout (seconds)", _timeoutSec, 5, 120);
        _useBearer = EditorGUILayout.Toggle("Use Bearer Authorization", _useBearer);
        if (!_useBearer)
        {
            EditorGUILayout.HelpBox("When off, the key is sent as header X-Api-Key.", MessageType.None);
        }

        string preview = VersusAsyncBackendSettings.BuildEndpointUrlFromParts(_baseUrl, _relativePath);
        EditorGUILayout.Space(4);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Resolved POST URL", string.IsNullOrEmpty(preview) ? "(disabled)" : preview);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reload from PlayerPrefs"))
            {
                LoadFromPrefs();
            }

            if (GUILayout.Button("Apply / Save"))
            {
                VersusAsyncBackendSettings.SaveAll(_baseUrl, _relativePath, _apiKey, _timeoutSec, _useBearer);
                Debug.Log("[VersusAsync] PlayerPrefs saved. Resolved URL: " +
                          (string.IsNullOrEmpty(preview) ? "(disabled)" : preview));
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Disable HTTP (clear base URL)"))
            {
                VersusAsyncBackendSettings.DisableHttpUpload();
                LoadFromPrefs();
            }

            if (GUILayout.Button("Delete all keys"))
            {
                if (EditorUtility.DisplayDialog(
                        "Versus upload",
                        "Delete all Versus upload PlayerPrefs keys?",
                        "Delete",
                        "Cancel"))
                {
                    VersusAsyncBackendSettings.DeleteAllStoredKeys();
                    LoadFromPrefs();
                }
            }
        }

        EditorGUILayout.Space(6);
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Flush outbox now (Play Mode)"))
            {
                VersusAsyncBridge.ApplyTransportFromSettings();
                VersusAsyncBridge.RequestFlushOutboxOnBoot();
                Debug.Log("[VersusAsync] Flush requested.");
            }
        }
    }
}
