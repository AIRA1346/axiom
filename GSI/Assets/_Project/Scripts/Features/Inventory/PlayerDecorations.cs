using System;
using System.Collections.Generic;
using UnityEngine;
using ArchE.Game;

/// <summary>
/// 아이템 등급 정의 (Common ~ WorldClass)
/// </summary>
public enum DecoRarity
{
    Common,       // 일반 (흰색)
    Uncommon,     // 비범 (노란색)
    Rare,         // 희귀 (주황색)
    Epic,         // 영웅 (초록색)
    Ancient,      // 고대 (하늘색)
    Legacy,       // 유산급 (파란색)
    Relic,        // 성유물급 (보라색)
    Legendary,    // 전설급 (갈색)
    Mythical,     // 신기 (빨강)
    WorldClass    // 월즈 아이템 (검정)
}

/// <summary>
/// 데코 아이템 데이터 사양 및 인벤토리/저장 데이터 헬퍼 클래스
/// </summary>
public static class PlayerDecorations
{
    private const string OwnedPrefix = "GSI_Deco_Owned_";
    private const string PlacedPrefix = "GSI_Deco_Placed_";

    public readonly struct DecoItemDef
    {
        public readonly string Id;
        public readonly string DisplayNameKey;
        public readonly string EnglishName;
        public readonly DecoRarity Rarity;
        public readonly Color DefaultColor;
        public readonly string ProceduralShape; // "star", "crystal", "ring"

        public DecoItemDef(string id, string displayNameKey, string englishName, DecoRarity rarity, Color color, string shape)
        {
            Id = id;
            DisplayNameKey = displayNameKey;
            EnglishName = englishName;
            Rarity = rarity;
            DefaultColor = color;
            ProceduralShape = shape;
        }
    }

    // ─── 데코 아이템 등록부 (모든 아이템은 '일반' 등급으로 배정) ──────────────────
    private static readonly IReadOnlyList<DecoItemDef> ItemRegistry = new[]
    {
        new DecoItemDef("deco_yellow_star", "deco.yellow_star", "Yellow Star", DecoRarity.Common, new Color(1.0f, 0.9f, 0.3f, 1f), "star"),
        new DecoItemDef("deco_purple_crystal", "deco.purple_crystal", "Purple Crystal", DecoRarity.Common, new Color(0.78f, 0.45f, 0.98f, 1f), "crystal"),
        new DecoItemDef("deco_neon_ring", "deco.neon_ring", "Neon Ring", DecoRarity.Common, new Color(0.2f, 0.85f, 1.0f, 1f), "ring")
    };

    public static IReadOnlyList<DecoItemDef> All => ItemRegistry;

    public static bool TryGetItemDef(string id, out DecoItemDef def)
    {
        for (int i = 0; i < ItemRegistry.Count; i++)
        {
            if (ItemRegistry[i].Id == id)
            {
                def = ItemRegistry[i];
                return true;
            }
        }
        def = default;
        return false;
    }

    // ─── 등급 컬러 매핑 (Hex 코드 기반 유니티 Color) ───────────────────────────
    public static Color GetRarityColor(DecoRarity rarity)
    {
        switch (rarity)
        {
            case DecoRarity.Common:       return new Color(0.95f, 0.96f, 0.96f, 1f); // #F3F4F6
            case DecoRarity.Uncommon:     return new Color(0.98f, 0.8f, 0.08f, 1f);  // #FACC15
            case DecoRarity.Rare:         return new Color(0.98f, 0.45f, 0.09f, 1f); // #F97316
            case DecoRarity.Epic:         return new Color(0.13f, 0.77f, 0.37f, 1f); // #22C55E
            case DecoRarity.Ancient:      return new Color(0.22f, 0.74f, 0.97f, 1f); // #38BDF8
            case DecoRarity.Legacy:       return new Color(0.23f, 0.51f, 0.96f, 1f); // #3B82F6
            case DecoRarity.Relic:        return new Color(0.66f, 0.33f, 0.97f, 1f); // #A855F7
            case DecoRarity.Legendary:    return new Color(0.55f, 0.27f, 0.07f, 1f); // #8B4513
            case DecoRarity.Mythical:     return new Color(0.94f, 0.27f, 0.27f, 1f); // #EF4444
            case DecoRarity.WorldClass:   return new Color(0.12f, 0.16f, 0.22f, 1f); // #1F2937
            default:                      return Color.white;
        }
    }

    // ─── 등급 명칭 매핑 (텍스트 배지용) ───────────────────────────────────────────
    public static string GetRarityName(DecoRarity rarity, bool korean)
    {
        switch (rarity)
        {
            case DecoRarity.Common:       return korean ? "일반" : "Common";
            case DecoRarity.Uncommon:     return korean ? "비범" : "Uncommon";
            case DecoRarity.Rare:         return korean ? "희귀" : "Rare";
            case DecoRarity.Epic:         return korean ? "영웅" : "Epic";
            case DecoRarity.Ancient:      return korean ? "고대" : "Ancient";
            case DecoRarity.Legacy:       return korean ? "유산급" : "Legacy";
            case DecoRarity.Relic:        return korean ? "성유물급" : "Relic";
            case DecoRarity.Legendary:    return korean ? "전설급" : "Legendary";
            case DecoRarity.Mythical:     return korean ? "신기" : "Mythical";
            case DecoRarity.WorldClass:   return korean ? "월즈 아이템" : "World-Class";
            default:                      return "";
        }
    }

    // ─── 인벤토리 보유량 관리 (중복 구매 가능) ──────────────────────────────────
    public static int GetTotalOwned(string itemId)
    {
        return GsiSaveSystem.GetInt(OwnedPrefix + itemId, 0);
    }

    public static void SetTotalOwned(string itemId, int count)
    {
        GsiSaveSystem.SetInt(OwnedPrefix + itemId, Math.Max(0, count));
        GsiSaveSystem.Save();
    }

    // ─── 특정 씬 또는 전체 씬의 배치 수량 확인 ─────────────────────────────────
    public static int GetPlacedCountInScene(string sceneName, string itemId)
    {
        string raw = GsiSaveSystem.GetString(PlacedPrefix + sceneName, "");
        if (string.IsNullOrEmpty(raw)) return 0;

        int count = 0;
        string[] items = raw.Split(';');
        for (int i = 0; i < items.Length; i++)
        {
            if (string.IsNullOrEmpty(items[i])) continue;
            string[] tokens = items[i].Split(':');
            if (tokens.Length > 0 && tokens[0] == itemId)
            {
                count++;
            }
        }
        return count;
    }

    public static int GetPlacedCountAllScenes(string itemId)
    {
        return GetPlacedCountInScene("Lobby", itemId) + GetPlacedCountInScene("GSI", itemId);
    }

    public static int GetAvailableCount(string itemId)
    {
        int total = GetTotalOwned(itemId);
        int placed = GetPlacedCountAllScenes(itemId);
        return Math.Max(0, total - placed);
    }

    // ─── 배치 데이터 구조화 및 파싱 ──────────────────────────────────────────
    public readonly struct PlacedDecoData
    {
        public readonly string ItemId;
        public readonly Vector2 NormalizedPos;

        public PlacedDecoData(string itemId, Vector2 pos)
        {
            ItemId = itemId;
            NormalizedPos = pos;
        }
    }

    public static List<PlacedDecoData> LoadPlacedDecos(string sceneName)
    {
        var list = new List<PlacedDecoData>();
        string raw = GsiSaveSystem.GetString(PlacedPrefix + sceneName, "");
        if (string.IsNullOrEmpty(raw)) return list;

        string[] items = raw.Split(';');
        for (int i = 0; i < items.Length; i++)
        {
            if (string.IsNullOrEmpty(items[i])) continue;
            string[] tokens = items[i].Split(':');
            if (tokens.Length >= 3)
            {
                string id = tokens[0];
                if (float.TryParse(tokens[1], out float x) && float.TryParse(tokens[2], out float y))
                {
                    list.Add(new PlacedDecoData(id, new Vector2(x, y)));
                }
            }
        }
        return list;
    }

    public static void SavePlacedDecos(string sceneName, List<PlacedDecoData> decos)
    {
        var parts = new List<string>();
        for (int i = 0; i < decos.Count; i++)
        {
            parts.Add($"{decos[i].ItemId}:{decos[i].NormalizedPos.x:F4}:{decos[i].NormalizedPos.y:F4}");
        }
        string raw = string.Join(";", parts);
        GsiSaveSystem.SetString(PlacedPrefix + sceneName, raw);
        GsiSaveSystem.Save();
    }
}
