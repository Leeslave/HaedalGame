using System;
using System.Linq;
using UnityEngine;

/// <summary>종족별 특산물 순환. 세이브의 runId/일차로 재현되며 한 순환 안에서는 중복되지 않는다.</summary>
public static class CustomerPreferences
{
    private static readonly int[][] Specialties = {
        new[] {1,2,3,4,5,6,7,8,9,10,23,25}, // 기본 해달: 어패류·채소
        new[] {22},                          // 요정: 현재 DB의 과일(레몬)
        new[] {5,6,8,10},                    // 돌고래: 고급 해산물
        new[] {5,8,10},                      // 해달 왕국: 고급 어패류
        new[] {17},                         // 곡물: 현재 DB의 밀가루
        Array.Empty<int>()                  // 유제품 데이터가 추가되기 전에는 출현하지 않는다.
    };
    public static int[] AvailableSpecies(int chapter) => Enumerable.Range(0, Mathf.Clamp(chapter + 1, 1, Specialties.Length))
        .Where(s => Specialties[s].Any(id => GameDatabase.Recipes.Ingredients.Any(i => i.IngredientId == id))).ToArray();
    public static int FavoriteIngredient(int species, int day, string runId)
    {
        int[] pool = (int[])Specialties[Mathf.Clamp(species, 0, Specialties.Length - 1)].Clone();
        if (pool.Length == 0) return -1;
        int cycle = Mathf.Max(0, day - 1) / pool.Length;
        int hash = 17;
        unchecked { foreach (char c in runId ?? "") hash = hash * 31 + c; hash = hash * 31 + species; hash = hash * 31 + cycle; }
        var random = new System.Random(hash);
        for (int i = pool.Length - 1; i > 0; i--) { int j = random.Next(i + 1); (pool[i], pool[j]) = (pool[j], pool[i]); }
        return pool[Mathf.Max(0, day - 1) % pool.Length];
    }
    public static bool Likes(RecipeData recipe, int ingredientId) => recipe != null && ingredientId > 0 && recipe.Requirements.Any(r => r.IngredientId == ingredientId);
}
