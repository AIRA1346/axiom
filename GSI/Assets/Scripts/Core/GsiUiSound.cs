using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 공통 클릭·호버·취소 SFX. <see cref="GsiUiInteractionSound"/> 가 설정을 주입합니다.
/// </summary>
public static class GsiUiSound
{
    private static GsiUiSoundSettings _settings;
    private static readonly HashSet<int> WiredButtonIds = new HashSet<int>();

    public static void ApplySettings(GsiUiSoundSettings settings)
    {
        _settings = settings;
    }

    public static void PlayClick()
    {
        if (_settings == null || _settings.PrimaryClick == null)
        {
            return;
        }

        GsiAudio.PlaySfx(_settings.PrimaryClick, _settings.ClickVolume);
    }

    public static void PlayHover()
    {
        if (_settings == null || _settings.Hover == null)
        {
            return;
        }

        GsiAudio.PlaySfx(_settings.Hover, _settings.HoverVolume);
    }

    public static void PlayCancel()
    {
        if (_settings == null || _settings.CancelOrClose == null)
        {
            return;
        }

        GsiAudio.PlaySfx(_settings.CancelOrClose, _settings.CancelVolume);
    }

    /// <summary>동적으로 생성된 버튼 등에 호출해 클릭(및 선택적 호버)을 한 번만 연결합니다.</summary>
    public static void WireButtonIfNeeded(Button button)
    {
        if (button == null)
        {
            return;
        }

        // Header wires sounds in WireHeaderListeners (it calls RemoveAllListeners on these).
        string n = button.name;
        if (n == GsiShopLikeCanvasHeaderUi.HeaderBackName || n == GsiShopLikeCanvasHeaderUi.HeaderSettingsName)
        {
            return;
        }

        int id = button.GetInstanceID();
        if (WiredButtonIds.Contains(id))
        {
            return;
        }

        WiredButtonIds.Add(id);
        button.onClick.AddListener(PlayClick);
        TryAddHover(button);
    }

    /// <summary>지정 루트 이하의 모든 <see cref="Button"/> 에 연결합니다.</summary>
    public static void WireButtonsUnder(Transform root)
    {
        if (root == null)
        {
            return;
        }

        foreach (Button b in root.GetComponentsInChildren<Button>(true))
        {
            WireButtonIfNeeded(b);
        }
    }

    private static void TryAddHover(Button button)
    {
        if (_settings == null || _settings.Hover == null)
        {
            return;
        }

        if (button.gameObject.GetComponent<GsiUiSoundHoverWiredMarker>() != null)
        {
            return;
        }

        var trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        if (trigger.triggers == null)
        {
            trigger.triggers = new List<EventTrigger.Entry>();
        }

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(_ => PlayHover());
        trigger.triggers.Add(entry);
        button.gameObject.AddComponent<GsiUiSoundHoverWiredMarker>();
    }
}
