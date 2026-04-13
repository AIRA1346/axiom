using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점 판매 항목(응시권 번들, UI 스킨). 가격·수량은 여기서 조정합니다.
/// </summary>
public static class ShopCatalog
{
    public enum OfferKind
    {
        ExamTickets,
        Skin
    }

    public readonly struct Offer
    {
        public readonly string Id;
        public readonly OfferKind Kind;
        public readonly int PriceGold;
        public readonly int TicketCount;
        public readonly string DisplayNameKey;

        public Offer(string id, OfferKind kind, int priceGold, int ticketCount, string displayNameKey)
        {
            Id = id;
            Kind = kind;
            PriceGold = priceGold;
            TicketCount = ticketCount;
            DisplayNameKey = displayNameKey;
        }
    }

    private static readonly IReadOnlyList<Offer> Offers = new[]
    {
        new Offer("tickets_1", OfferKind.ExamTickets, 120, 1, UiStringKeys.ShopOfferTickets1),
        new Offer("tickets_5", OfferKind.ExamTickets, 520, 5, UiStringKeys.ShopOfferTickets5),
        new Offer("skin_default", OfferKind.Skin, 0, 0, UiStringKeys.ShopOfferSkinDefault),
        new Offer("skin_ocean", OfferKind.Skin, 250, 0, UiStringKeys.ShopOfferSkinOcean),
        new Offer("skin_amber", OfferKind.Skin, 250, 0, UiStringKeys.ShopOfferSkinAmber),
        new Offer("skin_violet", OfferKind.Skin, 250, 0, UiStringKeys.ShopOfferSkinViolet),
    };

    public static IReadOnlyList<Offer> All => Offers;

    /// <summary>UI 스킨만(기본·유료 스킨 행). 인벤토리 등에서 사용합니다.</summary>
    public static IEnumerable<Offer> EnumerateSkinOffers()
    {
        foreach (Offer o in Offers)
        {
            if (o.Kind == OfferKind.Skin)
            {
                yield return o;
            }
        }
    }

    public static bool TryGetOffer(string id, out Offer offer)
    {
        foreach (var o in Offers)
        {
            if (o.Id == id)
            {
                offer = o;
                return true;
            }
        }

        offer = default;
        return false;
    }
}
