using TMPro;

/// <summary>
/// 상점·인벤토리 상단 서브바의 골드·응시권 TMP를 <see cref="EconomyManager"/> 값으로 채웁니다.
/// </summary>
public static class GsiShopLikeEconomyBarTexts
{
    public static void Apply(TextMeshProUGUI goldText, TextMeshProUGUI ticketText)
    {
        int gold = EconomyManager.Instance != null ? EconomyManager.Instance.Tokens : 0;
        int tickets = EconomyManager.Instance != null ? EconomyManager.Instance.ExamTickets : 0;

        if (goldText != null)
        {
            goldText.text = GameLocalization.FormatUiStringPreferKeys(
                new[] { UiStringKeys.ShopGoldFmt, UiStringKeys.LobbyCurrencyGoldFmt, UiStringKeys.HubCurrencyGoldFmt },
                "Gold: {0}",
                gold);
            GsiUiRuntimeWidgets.ApplyEconomyLineTypography(goldText);
        }

        if (ticketText != null)
        {
            ticketText.text = GameLocalization.FormatUiStringPreferKeys(
                new[] { UiStringKeys.ShopTicketsFmt, UiStringKeys.LobbyCurrencyTicketFmt, UiStringKeys.HubCurrencyTicketFmt },
                "Exam tickets: {0}",
                tickets);
            GsiUiRuntimeWidgets.ApplyEconomyLineTypography(ticketText);
        }
    }
}
