using TMPro;
using UnityEngine;

/// <summary>
/// 런타임에 생성하는 TMP에 쓰는 기본 폰트(Noto Sans KR Bold SDF). Resources/Fonts에 둡니다.
/// </summary>
public static class TmpFontCache
{
    private static TMP_FontAsset _defaultUiFont;

    /// <summary>동적 UI 텍스트용 기본 폰트(씬의 TMP와 동일 계열).</summary>
    public static TMP_FontAsset LiberationSansSdf
    {
        get
        {
            if (_defaultUiFont == null)
            {
                _defaultUiFont = Resources.Load<TMP_FontAsset>("Fonts/NotoSansKR-Bold SDF");
                if (_defaultUiFont == null)
                {
                    _defaultUiFont =
                        Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                }
            }

            return _defaultUiFont;
        }
    }
}
