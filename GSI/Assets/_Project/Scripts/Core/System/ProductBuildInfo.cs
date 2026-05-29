/// <summary>G.S.I 전용 프로젝트입니다. <c>GSI/csc.rsp</c>에 <c>GSI_PRODUCT</c>가 정의되어 있습니다.</summary>
public static class ProductBuildInfo
{
#if GSI_PRODUCT
    public const bool IsGsiOnlyProduct = true;
#else
    public const bool IsGsiOnlyProduct = false;
#endif
}
