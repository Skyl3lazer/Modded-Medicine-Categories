using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace ModdedMedicineCategories
{
    internal static class SurgeryCategoryRegistry
    {
        public class Category
        {
            public string Key;
            public ThingDef MinimumMedicine;
            public List<ThingDef> AllowedMedicines;
            public List<RecipeDef> Recipes;

            public string Label
            {
                get
                {
                    if (MinimumMedicine == null)
                        return "ModdedMedicineCategories.NoMedicine".Translate();
                    return "ModdedMedicineCategories.RequiresMedicine".Translate(MinimumMedicine.LabelCap);
                }
            }
        }

        private static List<Category> _all;

        public static IReadOnlyList<Category> All
        {
            get
            {
                if (_all == null) Build();
                return _all;
            }
        }

        public static void Invalidate() => _all = null;

        private static void Build()
        {
            var byKey = new Dictionary<string, Category>();

            foreach (var recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe == null || !recipe.IsSurgery) continue;

                var allowed = AllowedMedicines(recipe);
                var key = BuildKey(allowed);

                if (!byKey.TryGetValue(key, out var cat))
                {
                    cat = new Category
                    {
                        Key = key,
                        AllowedMedicines = allowed,
                        MinimumMedicine = allowed.Count > 0 ? allowed[0] : null,
                        Recipes = new List<RecipeDef>(),
                    };
                    byKey[key] = cat;
                }
                cat.Recipes.Add(recipe);
            }

            _all = byKey.Values
                .OrderBy(c => c.MinimumMedicine == null
                    ? -1f
                    : c.MinimumMedicine.GetStatValueAbstract(StatDefOf.MedicalPotency))
                .ThenBy(c => c.Key)
                .ToList();

            foreach (var cat in _all)
                cat.Recipes.Sort((a, b) => string.Compare(a.label ?? a.defName, b.label ?? b.defName, System.StringComparison.OrdinalIgnoreCase));
        }

        private static List<ThingDef> AllowedMedicines(RecipeDef recipe)
        {
            var meds = new List<ThingDef>();
            if (recipe.ingredients == null || recipe.ingredients.Count == 0)
                return meds;

            ThingFilter ingredientFilter = null;
            foreach (var ing in recipe.ingredients)
            {
                if (ing?.filter == null) continue;
                foreach (var med in MedicineRegistry.All)
                {
                    if (ing.filter.Allows(med.Def))
                    {
                        ingredientFilter = ing.filter;
                        break;
                    }
                }
                if (ingredientFilter != null) break;
            }
            if (ingredientFilter == null) return meds;

            foreach (var med in MedicineRegistry.All)
            {
                if (!ingredientFilter.Allows(med.Def)) continue;
                if (recipe.fixedIngredientFilter != null && !recipe.fixedIngredientFilter.Allows(med.Def)) continue;
                if (recipe.defaultIngredientFilter != null && !recipe.defaultIngredientFilter.Allows(med.Def)) continue;
                meds.Add(med.Def);
            }
            return meds;
        }

        private static string BuildKey(List<ThingDef> meds)
        {
            if (meds.Count == 0) return "_none";
            var sb = new StringBuilder();
            for (int i = 0; i < meds.Count; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(meds[i].defName);
            }
            return sb.ToString();
        }
    }
}
