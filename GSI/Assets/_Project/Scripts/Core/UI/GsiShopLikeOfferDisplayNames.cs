/// <summary>
/// <see cref="ShopCatalog.Offer"/> 의 <see cref="ShopCatalog.Offer.DisplayNameKey"/> 를 로컬라이즈된 표시 문자열로 바꿉니다.
/// 상점(응시권·스킨)·인벤토리(스킨)에서 동일하게 사용합니다.
/// </summary>
public static class GsiShopLikeOfferDisplayNames
{
    public static string GetLocalized(ShopCatalog.Offer offer)
    {
        return offer.DisplayNameKey switch
        {
            UiStringKeys.ShopOfferTickets1 => GameLocalization.GetUiString(UiStringKeys.ShopOfferTickets1, "Exam ticket x1"),
            UiStringKeys.ShopOfferTickets5 => GameLocalization.GetUiString(UiStringKeys.ShopOfferTickets5, "Exam ticket x5"),
            UiStringKeys.ShopOfferSkinDefault => GameLocalization.GetUiString(UiStringKeys.ShopOfferSkinDefault, "UI skin: Default"),
            UiStringKeys.ShopOfferSkinOcean => GameLocalization.GetUiString(UiStringKeys.ShopOfferSkinOcean, "UI skin: Ocean"),
            UiStringKeys.ShopOfferSkinAmber => GameLocalization.GetUiString(UiStringKeys.ShopOfferSkinAmber, "UI skin: Amber"),
            UiStringKeys.ShopOfferSkinViolet => GameLocalization.GetUiString(UiStringKeys.ShopOfferSkinViolet, "UI skin: Violet"),
            _ => offer.Id
        };
    }
}
