using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 구글 시트 URL 기반 Codex 데이터 임포트 설정.
/// ItemImporter와 동일한 패턴: 한 페이지에서 여러 대분류 시트를 관리하고,
/// 구글 시트 주소만 넣으면 CSV로 자동 변환하여 최신 데이터를 가져옵니다.
/// </summary>
[Serializable]
public sealed class CodexSheetEntry
{
    public bool IsEnabled = true;
    public string SheetUrl = "";
    public string LargeCat = "Norm";
}

public sealed class CodexImporter : EditorWindow
{
    private const string SheetListKey = "GSI.CodexImporter.SheetList";
    private const int RequestTimeoutSeconds = 30;

    private List<CodexSheetEntry> _sheetEntries = new List<CodexSheetEntry>();
    private bool _isImporting;
    private string _lastStatusMessage = "대기 중";
    private Vector2 _sheetListScroll;

    [MenuItem("Tools/Codex Importer Settings")]
    public static void OpenWindow()
    {
        CodexImporter window = GetWindow<CodexImporter>("Codex Importer Settings");
        window.minSize = new Vector2(580f, 320f);
        window.Show();
    }

    private void OnEnable()
    {
        LoadSheetList();
    }

    private void LoadSheetList()
    {
        string json = EditorPrefs.GetString(SheetListKey, "");
        if (string.IsNullOrEmpty(json))
        {
            _sheetEntries = new List<CodexSheetEntry> { new CodexSheetEntry { SheetUrl = "", LargeCat = "Norm" } };
            return;
        }
        try
        {
            var wrapper = JsonUtility.FromJson<CodexSheetListWrapper>(json);
            _sheetEntries = wrapper?.Entries ?? new List<CodexSheetEntry> { new CodexSheetEntry() };
            if (_sheetEntries.Count == 0)
            {
                _sheetEntries.Add(new CodexSheetEntry());
            }
        }
        catch
        {
            _sheetEntries = new List<CodexSheetEntry> { new CodexSheetEntry() };
        }
    }

    private void SaveSheetList()
    {
        var wrapper = new CodexSheetListWrapper { Entries = _sheetEntries };
        EditorPrefs.SetString(SheetListKey, JsonUtility.ToJson(wrapper));
    }

    [Serializable]
    private class CodexSheetListWrapper
    {
        public List<CodexSheetEntry> Entries;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Codex Importer Settings (대분류별 구글 시트)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(_isImporting);

        EditorGUILayout.LabelField("시트 목록 (☑=임포트 대상, 구글 시트 URL + 대분류)", EditorStyles.boldLabel);
        _sheetListScroll = EditorGUILayout.BeginScrollView(_sheetListScroll, GUILayout.MaxHeight(180f));
        for (int i = 0; i < _sheetEntries.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            bool enabled = EditorGUILayout.Toggle(_sheetEntries[i].IsEnabled, GUILayout.Width(18f));
            if (enabled != _sheetEntries[i].IsEnabled)
            {
                _sheetEntries[i].IsEnabled = enabled;
                SaveSheetList();
            }
            EditorGUILayout.LabelField($"{i + 1}", GUILayout.Width(18f));
            string url = EditorGUILayout.TextField(_sheetEntries[i].SheetUrl ?? "", GUILayout.ExpandWidth(true));
            string largeCat = EditorGUILayout.TextField(_sheetEntries[i].LargeCat ?? "Norm", GUILayout.Width(95f));
            if (url != _sheetEntries[i].SheetUrl || largeCat != _sheetEntries[i].LargeCat)
            {
                _sheetEntries[i].SheetUrl = url;
                _sheetEntries[i].LargeCat = string.IsNullOrWhiteSpace(largeCat) ? "Norm" : largeCat.Trim();
                SaveSheetList();
            }
            if (GUILayout.Button("−", GUILayout.Width(22f)) && _sheetEntries.Count > 1)
            {
                _sheetEntries.RemoveAt(i);
                SaveSheetList();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        if (GUILayout.Button("+ 시트 추가", GUILayout.Width(100f)))
        {
            _sheetEntries.Add(new CodexSheetEntry { LargeCat = "Norm" });
            SaveSheetList();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "구글 시트 공유 URL을 붙여넣으세요. 첫 시트 또는 #gid=로 지정한 시트가 CSV로 내보내집니다.\n" +
            "필수 컬럼: Id, Title, LargeCat, MidCat, SmallCat, Summary, Content",
            MessageType.Info);
        EditorGUILayout.Space();

        if (GUILayout.Button("Import from Google Sheets", GUILayout.Height(36f)))
        {
            ImportFromSheets();
        }

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(_lastStatusMessage, MessageType.Info);
    }

    private void ImportFromSheets()
    {
        if (_isImporting)
        {
            return;
        }

        _isImporting = true;
        _lastStatusMessage = "다운로드 준비 중...";
        Repaint();

        ImportFromSheetsAsync(() =>
        {
            _isImporting = false;
            EditorUtility.ClearProgressBar();
            Repaint();
        });
    }

    private void ImportFromSheetsAsync(Action onAllComplete)
    {
        var validSheets = new List<CodexSheetEntry>();
        foreach (var entry in _sheetEntries)
        {
            if (entry != null && entry.IsEnabled && !string.IsNullOrWhiteSpace(entry.SheetUrl))
            {
                validSheets.Add(entry);
            }
        }

        if (validSheets.Count == 0)
        {
            _lastStatusMessage = "임포트할 시트를 체크하고 구글 시트 URL을 입력해 주세요.";
            Debug.LogError("[CodexImporter] 체크된 시트가 없거나 URL이 비어 있습니다.");
            onAllComplete?.Invoke();
            return;
        }

        EditorApplication.delayCall += () =>
        {
            EditorApplication.CallbackFunction callback = null;
            callback = () =>
            {
                EditorApplication.update -= callback;
                RunImportCoroutine(validSheets, () =>
                {
                    _lastStatusMessage = "완료";
                    onAllComplete?.Invoke();
                });
            };
            EditorApplication.update += callback;
        };
    }

    private void RunImportCoroutine(List<CodexSheetEntry> validSheets, Action onComplete)
    {
        IEnumerator routine = ImportRoutine(validSheets, onComplete);
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            try
            {
                if (!routine.MoveNext())
                {
                    EditorApplication.update -= tick;
                    onComplete?.Invoke();
                }
            }
            catch (Exception ex)
            {
                EditorApplication.update -= tick;
                _lastStatusMessage = $"오류: {ex.Message}";
                Debug.LogError($"[CodexImporter] {ex}");
                onComplete?.Invoke();
            }
        };
        EditorApplication.update += tick;
    }

    private IEnumerator ImportRoutine(List<CodexSheetEntry> validSheets, Action onComplete)
    {
        var downloadTasks = new List<(string csvText, string largeCat, string sourceName)>();
        string lastError = null;

        for (int i = 0; i < validSheets.Count; i++)
        {
            var entry = validSheets[i];
            string largeCat = string.IsNullOrWhiteSpace(entry.LargeCat) ? "Norm" : entry.LargeCat.Trim();
            _lastStatusMessage = $"시트 {i + 1}/{validSheets.Count} ({largeCat}) 다운로드 중...";
            Repaint();

            string csvUrl = ConvertGoogleSheetUrlToCsv(entry.SheetUrl);
            using (var request = UnityWebRequest.Get(csvUrl))
            {
                request.timeout = RequestTimeoutSeconds;
                request.SendWebRequest();

                while (!request.isDone)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Codex Importer", $"Downloading {largeCat}...", (float)i / validSheets.Count + 0.1f))
                    {
                        lastError = "사용자 취소";
                        yield break;
                    }
                    yield return null;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    lastError = request.error;
                    Debug.LogError($"[CodexImporter] 다운로드 실패 ({largeCat}): {request.error}");
                    continue;
                }

                string csvText = request.downloadHandler?.text ?? "";
                if (csvText.Length >= 1 && csvText[0] == '\uFEFF')
                {
                    csvText = csvText.Substring(1);
                }

                downloadTasks.Add((csvText, largeCat, largeCat));
            }
        }

        EditorUtility.ClearProgressBar();
        if (downloadTasks.Count == 0 && !string.IsNullOrEmpty(lastError))
        {
            _lastStatusMessage = $"다운로드 실패: {lastError}";
            onComplete?.Invoke();
            yield break;
        }

        CodexDataBuilder.BuildFromDownloadedSheets(downloadTasks);
        _lastStatusMessage = $"{downloadTasks.Count}개 시트 처리 완료";
    }

    private static string ConvertGoogleSheetUrlToCsv(string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return "";
        }

        string trimmed = rawUrl.Trim();
        if (!trimmed.Contains("docs.google.com/spreadsheets/d/", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var match = System.Text.RegularExpressions.Regex.Match(
            trimmed,
            @"https?://docs\.google\.com/spreadsheets/d/([^/?#]+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            return trimmed;
        }

        string sheetId = match.Groups[1].Value;
        string gid = "";
        var gidQuery = System.Text.RegularExpressions.Regex.Match(trimmed, @"[?&]gid=(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var gidFrag = System.Text.RegularExpressions.Regex.Match(trimmed, @"#gid=(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (gidQuery.Success)
        {
            gid = gidQuery.Groups[1].Value;
        }
        else if (gidFrag.Success)
        {
            gid = gidFrag.Groups[1].Value;
        }

        string result = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv";
        if (!string.IsNullOrEmpty(gid))
        {
            result += $"&gid={gid}";
        }
        return result;
    }
}
