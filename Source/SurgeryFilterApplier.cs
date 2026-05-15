using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ModdedMedicineCategories
{
    internal static class SurgeryFilterApplier
    {
        private static Dictionary<string, HashSet<string>> _originalAllowed;

        public static void EnsureSnapshots()
        {
            if (_originalAllowed != null) return;
            _originalAllowed = new Dictionary<string, HashSet<string>>();

            foreach (var recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (recipe == null || !recipe.IsSurgery) continue;
                var ing = SurgeryCategoryRegistry.FindMedicineIngredient(recipe);
                if (ing == null) continue;

                var orig = new HashSet<string>();
                foreach (var med in MedicineRegistry.All)
                {
                    if (!ing.filter.Allows(med.Def)) continue;
                    if (recipe.fixedIngredientFilter != null && !recipe.fixedIngredientFilter.Allows(med.Def)) continue;
                    orig.Add(med.Def.defName);
                }
                _originalAllowed[recipe.defName] = orig;
            }
        }

        public static void ApplyAllPersistedOverrides()
        {
            EnsureSnapshots();
            var dict = ModdedMedicineCategoriesMod.Settings?.recipeMinMedicine;
            if (dict == null || dict.Count == 0) return;

            var stale = new List<string>();
            foreach (var kvp in dict)
            {
                var recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(kvp.Key);
                if (recipe == null)
                {
                    stale.Add(kvp.Key);
                    continue;
                }
                var med = DefDatabase<ThingDef>.GetNamedSilentFail(kvp.Value);
                if (med == null || !MedicineRegistry.Contains(med))
                {
                    stale.Add(kvp.Key);
                    continue;
                }
                if (SurgeryCategoryRegistry.FindMedicineIngredient(recipe) == null)
                {
                    stale.Add(kvp.Key);
                    continue;
                }
                ApplyOverride(recipe, med);
            }

            if (stale.Count > 0)
            {
                foreach (var key in stale) dict.Remove(key);
                Log.Message($"[Modded Medicine Categories] Reset {stale.Count} surgery override(s) referencing missing or invalid defs to their defaults.");
                LoadedModManager.GetMod<ModdedMedicineCategoriesMod>()?.WriteSettings();
            }
        }

        public static void ApplyOverride(RecipeDef recipe, ThingDef minMedicine)
        {
            EnsureSnapshots();
            var ing = SurgeryCategoryRegistry.FindMedicineIngredient(recipe);
            if (ing == null) return;

            float threshold = minMedicine.GetStatValueAbstract(StatDefOf.MedicalPotency);
            foreach (var med in MedicineRegistry.All)
            {
                bool allow = med.Potency >= threshold;
                ing.filter.SetAllow(med.Def, allow);
                recipe.fixedIngredientFilter?.SetAllow(med.Def, allow);
            }
        }

        public static void RevertToOriginal(RecipeDef recipe)
        {
            EnsureSnapshots();
            if (!_originalAllowed.TryGetValue(recipe.defName, out var orig)) return;
            var ing = SurgeryCategoryRegistry.FindMedicineIngredient(recipe);
            if (ing == null) return;

            foreach (var med in MedicineRegistry.All)
            {
                bool allow = orig.Contains(med.Def.defName);
                ing.filter.SetAllow(med.Def, allow);
                recipe.fixedIngredientFilter?.SetAllow(med.Def, allow);
            }
        }

        public static bool IsAtDefault(RecipeDef recipe)
        {
            EnsureSnapshots();
            if (!_originalAllowed.TryGetValue(recipe.defName, out var orig)) return true;
            var ing = SurgeryCategoryRegistry.FindMedicineIngredient(recipe);
            if (ing == null) return true;

            foreach (var med in MedicineRegistry.All)
            {
                bool currentAllow = ing.filter.Allows(med.Def)
                    && (recipe.fixedIngredientFilter == null || recipe.fixedIngredientFilter.Allows(med.Def));
                bool origAllow = orig.Contains(med.Def.defName);
                if (currentAllow != origAllow) return false;
            }
            return true;
        }

        public static ThingDef GetDefaultMinimumMedicine(RecipeDef recipe)
        {
            EnsureSnapshots();
            if (!_originalAllowed.TryGetValue(recipe.defName, out var orig)) return null;
            foreach (var med in MedicineRegistry.All)
            {
                if (orig.Contains(med.Def.defName)) return med.Def;
            }
            return null;
        }
    }
}
