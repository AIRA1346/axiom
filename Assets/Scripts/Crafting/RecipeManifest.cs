using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 제작 레시피 목록을 한 번에 참조하는 매니페스트.
/// Resources.LoadAll 대신 단일 에셋 로드로 .cursorrules를 준수합니다.
/// </summary>
[CreateAssetMenu(fileName = "RecipeManifest", menuName = "gsi/Recipe Manifest")]
public sealed class RecipeManifest : ScriptableObject
{
    [SerializeField] private List<CraftingRecipe> _recipes = new List<CraftingRecipe>();

    public IReadOnlyList<CraftingRecipe> Recipes => _recipes;
}
