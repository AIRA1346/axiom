using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 제작 UI 패널을 제어합니다. 레시피 목록과 재료 표시, 제작 버튼을 관리합니다.
/// </summary>
public sealed class CraftingUIController : MonoBehaviour
{
    [SerializeField] private Transform _recipeContainer;
    [SerializeField] private GameObject _recipeSlotPrefab;
    [SerializeField] private GameObject _ingredientSlotPrefab;
    [SerializeField] private Button _exitButton;

    private void Awake()
    {
        if (_exitButton != null)
        {
            _exitButton.onClick.AddListener(OnExitClicked);
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        RefreshUI();
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (_exitButton != null)
        {
            _exitButton.onClick.RemoveListener(OnExitClicked);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Crafting)
        {
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        if (_recipeContainer == null
            || _recipeSlotPrefab == null
            || _ingredientSlotPrefab == null
            || CraftingManager.Instance == null)
        {
            return;
        }

        for (int i = _recipeContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(_recipeContainer.GetChild(i).gameObject);
        }

        foreach (CraftingRecipe recipe in CraftingManager.Instance.GetAllRecipes())
        {
            if (recipe == null || recipe.ResultItem == null)
            {
                continue;
            }

            GameObject recipeSlotObject = Instantiate(_recipeSlotPrefab, _recipeContainer);
            TextMeshProUGUI[] texts = recipeSlotObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            Image[] images = recipeSlotObject.GetComponentsInChildren<Image>(true);
            Button craftButton = recipeSlotObject.GetComponentInChildren<Button>(true);
            Transform ingredientContainer = FindChildRecursive(recipeSlotObject.transform, "IngredientContainer");

            TextMeshProUGUI nameText = texts.Length > 0 ? texts[0] : null;
            Image resultIcon = null;

            if (nameText != null)
            {
                nameText.text = $"[{GetTierName(recipe.ResultItem.Tier)}] {recipe.ResultItem.ItemName} 제작 (비용: {recipe.CraftingCost} 기초 골드)";
            }

            foreach (Image image in images)
            {
                if (image != null && image.gameObject.name == "ResultIcon")
                {
                    resultIcon = image;
                    break;
                }
            }

            if (resultIcon == null && images.Length > 0)
            {
                resultIcon = images[0];
            }

            if (resultIcon != null && recipe.ResultItem.ItemIcon != null)
            {
                resultIcon.sprite = recipe.ResultItem.ItemIcon;
            }

            if (ingredientContainer != null)
            {
                for (int i = ingredientContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(ingredientContainer.GetChild(i).gameObject);
                }

                foreach (CraftingRecipe.CraftingIngredient ingredient in recipe.Ingredients)
                {
                    if (ingredient.Item == null)
                    {
                        continue;
                    }

                    int ownedAmount = InventoryManager.Instance != null
                        ? InventoryManager.Instance.GetItemCount(ingredient.Item.ItemId)
                        : 0;

                    GameObject ingredientSlotObject = Instantiate(_ingredientSlotPrefab, ingredientContainer);
                    Image ingredientIcon = ingredientSlotObject.GetComponentInChildren<Image>(true);
                    TextMeshProUGUI ingredientText = ingredientSlotObject.GetComponentInChildren<TextMeshProUGUI>(true);

                    if (ingredientIcon != null && ingredient.Item.ItemIcon != null)
                    {
                        ingredientIcon.sprite = ingredient.Item.ItemIcon;
                    }

                    if (ingredientText != null)
                    {
                        ingredientText.text = $"{ingredient.Item.ItemName} ({ownedAmount}/{ingredient.Amount})";
                        ingredientText.color = ownedAmount < ingredient.Amount ? Color.red : Color.white;
                    }
                }
            }

            if (craftButton != null)
            {
                craftButton.interactable = CraftingManager.Instance.CanCraft(recipe.RecipeId);
                craftButton.onClick.RemoveAllListeners();
                craftButton.onClick.AddListener(() =>
                {
                    CraftingManager.Instance.CraftItem(recipe.RecipeId);
                    RefreshUI();
                });
            }
        }
    }

    private string GetTierName(ItemTier tier)
    {
        switch (tier)
        {
            case ItemTier.Tier1:
                return "凡";

            case ItemTier.Tier2:
                return "奇";

            case ItemTier.Tier3:
                return "珍";

            case ItemTier.Tier4:
                return "傑";

            case ItemTier.Tier5:
                return "古";

            case ItemTier.Tier6:
                return "遺";

            case ItemTier.Tier7:
                return "聖";

            case ItemTier.Tier8:
                return "傳";

            case ItemTier.Tier9:
                return "神";

            case ItemTier.Tier10:
                return "極";

            default:
                return "凡";
        }
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform foundChild = FindChildRecursive(child, childName);

            if (foundChild != null)
            {
                return foundChild;
            }
        }

        return null;
    }

    private void OnExitClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetGameState(GameState.MainMenu);
    }
}
