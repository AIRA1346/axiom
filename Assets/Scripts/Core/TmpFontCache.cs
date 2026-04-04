using TMPro;
using UnityEngine;

/// <summary>
/// UI에서 반복 사용하는 TMP 폰트를 한 번만 Resources 로드합니다.
/// </summary>
public static class TmpFontCache
{
    private static TMP_FontAsset _liberationSansSdf;

    public static TMP_FontAsset LiberationSansSdf
    {
        get
        {
            if (_liberationSansSdf == null)
            {
                _liberationSansSdf = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }

            return _liberationSansSdf;
        }
    }
}
