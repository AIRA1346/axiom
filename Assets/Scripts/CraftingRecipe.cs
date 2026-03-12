using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "gsi/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [System.Serializable]
    public struct CraftingIngredient
    {
        public ItemData Item;
        public int Amount;
    }

    public string RecipeId;
    public string RecipeName;
    public List<CraftingIngredient> Ingredients = new List<CraftingIngredient>();
    public int CraftingCost;
    public ItemData ResultItem;
    public int ResultAmount = 1;
}
