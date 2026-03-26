using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// 상점 상품 슬롯 프리팹 생성.
/// Menu: Tools > Create Shop Item Slot Prefab
/// </summary>
public static class ShopItemSlotPrefabCreator
{
    private const string PrefabPath = "Assets/Prefabs/ShopItemSlotPrefab.prefab";

    [MenuItem("Tools/Create Shop Item Slot Prefab")]
    public static void Create()
    {
        GameObject root = new GameObject("ShopItemSlot");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(0, 100);

        var image = root.AddComponent<Image>();
        image.color = new Color(0.22f, 0.22f, 0.26f, 0.95f);

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(root.transform, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(50, 0);
        iconRect.sizeDelta = new Vector2(64, 64);
        var iconImage = iconObj.AddComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.color = Color.clear;

        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(root.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.5f);
        nameRect.anchorMax = new Vector2(0.7f, 0.5f);
        nameRect.pivot = new Vector2(0, 0.5f);
        nameRect.anchoredPosition = new Vector2(130, 0);
        nameRect.sizeDelta = new Vector2(-140, 32);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "[凡] 아이템명";
        nameText.fontSize = 16;
        nameText.raycastTarget = false;

        GameObject priceObj = new GameObject("PriceText");
        priceObj.transform.SetParent(root.transform, false);
        var priceRect = priceObj.AddComponent<RectTransform>();
        priceRect.anchorMin = new Vector2(0.7f, 0.5f);
        priceRect.anchorMax = new Vector2(0.85f, 0.5f);
        priceRect.pivot = new Vector2(0.5f, 0.5f);
        priceRect.anchoredPosition = new Vector2(0, 0);
        priceRect.sizeDelta = new Vector2(100, 24);
        var priceText = priceObj.AddComponent<TextMeshProUGUI>();
        priceText.text = "100 기초 골드";
        priceText.fontSize = 14;
        priceText.raycastTarget = false;

        GameObject buttonObj = new GameObject("BuyButton");
        buttonObj.transform.SetParent(root.transform, false);
        var btnRect = buttonObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.85f, 0.5f);
        btnRect.anchorMax = new Vector2(1f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(-40, 0);
        btnRect.sizeDelta = new Vector2(80, 36);
        var btnImage = buttonObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.4f, 0.2f, 1f);
        var buyButton = buttonObj.AddComponent<Button>();

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(buttonObj.transform, false);
        var btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
        var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "구매";
        btnText.fontSize = 14;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.raycastTarget = false;

        buyButton.targetGraphic = btnImage;

        string dir = System.IO.Path.GetDirectoryName(PrefabPath);
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();
        Debug.Log($"[Shop] 프리팹 생성 완료: {PrefabPath}");
    }
}
