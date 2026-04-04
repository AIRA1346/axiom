using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    private readonly Dictionary<string, CraftingRecipe> _recipeDict = new Dictionary<string, CraftingRecipe>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadRecipes();
    }

    public IEnumerable<CraftingRecipe> GetAllRecipes()
    {
        return _recipeDict.Values;
    }

    public bool CanCraft(string recipeId)
    {
        if (string.IsNullOrWhiteSpace(recipeId)
            || !_recipeDict.TryGetValue(recipeId, out CraftingRecipe recipe)
            || recipe == null
            || recipe.ResultItem == null
            || EconomyManager.Instance == null
            || InventoryManager.Instance == null)
        {
            return false;
        }

        if (EconomyManager.Instance.Tokens < recipe.CraftingCost)
        {
            return false;
        }

        foreach (CraftingRecipe.CraftingIngredient ingredient in recipe.Ingredients)
        {
            if (ingredient.Item == null || ingredient.Amount <= 0)
            {
                return false;
            }

            if (InventoryManager.Instance.GetItemCount(ingredient.Item.ItemId) < ingredient.Amount)
            {
                return false;
            }
        }

        return true;
    }

    public bool CraftItem(string recipeId)
    {
        if (!CanCraft(recipeId) || !_recipeDict.TryGetValue(recipeId, out CraftingRecipe recipe) || recipe == null)
        {
            return false;
        }

        if (!EconomyManager.Instance.SpendTokens(recipe.CraftingCost))
        {
            return false;
        }

        foreach (CraftingRecipe.CraftingIngredient ingredient in recipe.Ingredients)
        {
            if (!InventoryManager.Instance.RemoveItem(ingredient.Item.ItemId, ingredient.Amount))
            {
                return false;
            }
        }

        InventoryManager.Instance.AddItem(recipe.ResultItem.ItemId, recipe.ResultAmount);
        return true;
    }

    private void LoadRecipes()
    {
        _recipeDict.Clear();

        var manifest = Resources.Load<RecipeManifest>("RecipeManifest");
        if (manifest == null)
        {
            Debug.LogError(
                "[CraftingManager] Resources/RecipeManifest 을 찾을 수 없습니다. 메뉴 Tools → ARCHÉ → Items → Build Recipe Manifest 를 실행하세요.");
            return;
        }

        IReadOnlyList<CraftingRecipe> loadedRecipes = manifest.Recipes;
        if (loadedRecipes == null)
        {
            return;
        }

        foreach (CraftingRecipe recipe in loadedRecipes)
        {
            if (recipe == null || string.IsNullOrWhiteSpace(recipe.RecipeId))
            {
                continue;
            }

            _recipeDict[recipe.RecipeId] = recipe;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
