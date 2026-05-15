using System.Collections.Generic;
using Verse;

namespace ModdedMedicineCategories
{
    public class ModdedMedicineCategoriesSettings : ModSettings
    {
        public Dictionary<string, string> recipeMinMedicine = new Dictionary<string, string>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref recipeMinMedicine, "recipeMinMedicine", LookMode.Value, LookMode.Value);
            if (recipeMinMedicine == null)
                recipeMinMedicine = new Dictionary<string, string>();
        }
    }
}
