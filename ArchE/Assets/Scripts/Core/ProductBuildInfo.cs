/// <summary>
/// G.S.I 단독 빌드: Player Settings → Scripting Define Symbols에 <c>GSI_PRODUCT</c> 추가
/// (또는 메뉴 Tools → ARCHÉ → Build Profile → GSI 단독 빌드 심볼 켜기).
/// 켜지면 <see cref="ItemDatabase"/>는 메타/매니페스트를 로드하지 않습니다.
/// </summary>
public static class ProductBuildInfo
{
#if GSI_PRODUCT
    public const bool IsGsiOnlyProduct = true;
#else
    public const bool IsGsiOnlyProduct = false;
#endif
}
