using System;
using UnityEngine;

/// <summary>
/// 전송 실패한 <see cref="VersusAsyncScorePayload"/>를 JSON 문자열로 로컬에 쌓았다가 이후에 재전송합니다.
/// </summary>
internal static class VersusAsyncOutbox
{
    private const string PrefsKey = "GSI_VersusAsync_Outbox_v1";
    private const int MaxItems = 24;

    [Serializable]
    private sealed class Dto
    {
        public string[] Items = Array.Empty<string>();
    }

    public static void Enqueue(VersusAsyncScorePayload payload)
    {
        if (payload == null)
        {
            return;
        }

        string jsonLine;
        try
        {
            jsonLine = JsonUtility.ToJson(payload);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[VersusAsync] Outbox enqueue serialize failed: " + e.Message);
            return;
        }

        Dto dto = LoadDto();
        var list = new System.Collections.Generic.List<string>(dto.Items ?? Array.Empty<string>());
        list.Add(jsonLine);
        while (list.Count > MaxItems)
        {
            list.RemoveAt(0);
        }

        dto.Items = list.ToArray();
        SaveDto(dto);
    }

    public static int Count => LoadDto().Items?.Length ?? 0;

    public static bool TryPeekFirst(out VersusAsyncScorePayload payload)
    {
        payload = null;
        Dto dto = LoadDto();
        if (dto.Items == null || dto.Items.Length == 0)
        {
            return false;
        }

        try
        {
            payload = JsonUtility.FromJson<VersusAsyncScorePayload>(dto.Items[0]);
            return payload != null;
        }
        catch
        {
            return false;
        }
    }

    public static void RemoveFirst()
    {
        Dto dto = LoadDto();
        if (dto.Items == null || dto.Items.Length == 0)
        {
            return;
        }

        var list = new System.Collections.Generic.List<string>(dto.Items);
        list.RemoveAt(0);
        dto.Items = list.ToArray();
        SaveDto(dto);
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.Save();
    }

    private static Dto LoadDto()
    {
        string raw = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (string.IsNullOrEmpty(raw))
        {
            return new Dto();
        }

        try
        {
            Dto dto = JsonUtility.FromJson<Dto>(raw);
            return dto?.Items != null ? dto : new Dto();
        }
        catch
        {
            return new Dto();
        }
    }

    private static void SaveDto(Dto dto)
    {
        try
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(dto));
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[VersusAsync] Outbox save failed: " + e.Message);
        }
    }
}
