using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModdedMedicineCategories
{
    internal static class SurgeryCategoryRegistry
    {
        public class Category
        {
            public string Key;
            public ThingDef MinimumMedicine;
            public List<ThingDef> MinimumMedicines = new List<ThingDef>();
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
                    var ties = ComputeTiedMin(allowed);
                    cat = new Category
                    {
                        Key = key,
                        AllowedMedicines = allowed,
                        MinimumMedicines = ties,
                        MinimumMedicine = ties.Count > 0 ? ties[0] : null,
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

        internal static IngredientCount FindMedicineIngredient(RecipeDef recipe)
        {
            if (recipe?.ingredients == null) return null;
            foreach (var ing in recipe.ingredients)
            {
                if (ing?.filter == null) continue;
                foreach (var med in MedicineRegistry.All)
                {
                    if (ing.filter.Allows(med.Def))
                        return ing;
                }
            }
            return null;
        }

        private static List<ThingDef> AllowedMedicines(RecipeDef recipe)
        {
            var meds = new List<ThingDef>();
            var ing = FindMedicineIngredient(recipe);
            if (ing == null) return meds;

            foreach (var med in MedicineRegistry.All)
            {
                if (!ing.filter.Allows(med.Def)) continue;
                if (recipe.fixedIngredientFilter != null && !recipe.fixedIngredientFilter.Allows(med.Def)) continue;
                if (recipe.defaultIngredientFilter != null && !recipe.defaultIngredientFilter.Allows(med.Def)) continue;
                meds.Add(med.Def);
            }
            return meds;
        }

        private static List<ThingDef> ComputeTiedMin(List<ThingDef> sortedAscending)
        {
            var ties = new List<ThingDef>();
            if (sortedAscending.Count == 0) return ties;
            float min = MedicineRegistry.PotencyOf(sortedAscending[0]);
            foreach (var def in sortedAscending)
            {
                float p = MedicineRegistry.PotencyOf(def);
                if (Mathf.Approximately(p, min)) ties.Add(def);
                else break;
            }
            return ties;
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
