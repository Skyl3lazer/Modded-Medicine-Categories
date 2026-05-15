using Verse;

namespace ModdedMedicineCategories
{
    [StaticConstructorOnStartup]
    internal static class StartupApplier
    {
        static StartupApplier()
        {
            SurgeryFilterApplier.ApplyAllPersistedOverrides();
            SurgeryCategoryRegistry.Invalidate();
        }
    }
}
