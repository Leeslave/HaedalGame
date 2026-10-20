using System;
using System.Linq;
using NUnit.Framework;

public class PlanningSpecificationTests
{
    [TestCase(0,2)] [TestCase(1,3)] [TestCase(2,3)] [TestCase(3,4)]
    public void ApplicantsFollowTierAndPenalty(int tier, int expected)
    {
        Assert.AreEqual(expected, StaffRecruitmentRules.ApplicantCount(tier, 0, false));
        Assert.AreEqual(Math.Max(0,expected-1), StaffRecruitmentRules.ApplicantCount(tier, 2, true));
        Assert.AreEqual(Math.Max(0,expected-2), StaffRecruitmentRules.ApplicantCount(tier, 4, true));
        Assert.AreEqual(Math.Max(0,expected-3), StaffRecruitmentRules.ApplicantCount(tier, 5, true));
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
    public void StatsAreIntegersBoundedAndHaveExactTotal(int grade)
    {
        var random = new Random(718);
        for(int n=0;n<2000;n++)
        {
            var s=StaffRecruitmentRules.RollStats(grade,random);
            float[] values={s.serving,s.cooking,s.handy,s.hp};
            Assert.AreEqual(StaffRecruitmentRules.Total(grade),values.Sum());
            foreach(float value in values) { Assert.AreEqual(Math.Floor(value),value); Assert.That(value,Is.InRange(StaffRecruitmentRules.Min(grade),StaffRecruitmentRules.Max(grade))); }
        }
    }
    [Test] public void ScoutDistributionUsesNormalizedWeights()
    {
        int[] counts=new int[3];var random=new Random(42);
        for(int i=0;i<50000;i++) counts[StaffRecruitmentRules.RollGrade(1,random)-1]++;
        Assert.That(counts[0]/50000f, Is.EqualTo(70f/110).Within(.01));
        Assert.That(counts[1]/50000f, Is.EqualTo(30f/110).Within(.01));
        Assert.That(counts[2]/50000f, Is.EqualTo(10f/110).Within(.01));
    }
    [Test] public void EveryResearchRecipeHasItsOwnValidPlan()
    {
        var recipes=GameDatabase.Recipes.Recipes.Where(r=>!r.DefaultUnlock).ToArray();
        Assert.AreEqual(18,recipes.Length);
        string[] kinds={"Wash","Cut","Mix","Add","Shape","Stir","Plate","Grill","Boil","Simmer","Fry"};
        foreach(var recipe in recipes)
        {
            var plan=RecipeResearchPlan.Find(recipe.RecipeId);
            Assert.NotNull(plan,recipe.RecipeName);
            Assert.That(plan.steps.Length,Is.InRange(2,7));
            foreach(var step in plan.steps) { Assert.Contains(step.kind,kinds); Assert.IsNotEmpty(step.text); }
            foreach(var cost in plan.requirements) { Assert.Greater(cost.amount,0); Assert.IsTrue(GameDatabase.Recipes.TryGetIngredientById(cost.ingredientId,out _)); }
        }
        Assert.AreEqual(2,RecipeResearchPlan.Find(5).steps.Length);
        Assert.AreEqual(7,RecipeResearchPlan.Find(22).steps.Length);
    }
    [Test] public void SpecialtiesDoNotRepeatWithinCycleAndSurviveReload()
    {
        for(int cycle=0;cycle<4;cycle++)
        {
            var values=Enumerable.Range(1+cycle*12,12).Select(day=>CustomerPreferences.FavoriteIngredient(0,day,"test-save")).ToArray();
            Assert.AreEqual(12,values.Distinct().Count());
            CollectionAssert.AreEqual(values,Enumerable.Range(1+cycle*12,12).Select(day=>CustomerPreferences.FavoriteIngredient(0,day,"test-save")));
        }
    }
}
