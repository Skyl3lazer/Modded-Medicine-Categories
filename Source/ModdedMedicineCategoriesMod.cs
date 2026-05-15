using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModdedMedicineCategories
{
    public class ModdedMedicineCategoriesMod : Mod
    {
        public static ModdedMedicineCategoriesSettings Settings;

        private Vector2 _scrollPosition;

        private const float RowHeight = 32f;
        private const float RowGap = 4f;
        private const float IconSize = 28f;
        private const float DropdownWidth = 220f;
        private const float ColumnGap = 8f;

        public ModdedMedicineCategoriesMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ModdedMedicineCategoriesSettings>();
        }

        public override string SettingsCategory()
        {
            return "ModdedMedicineCategories.SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var categories = SurgeryCategoryRegistry.All;
            var medicines = MedicineRegistry.All;

            float viewHeight = categories.Count * (RowHeight + RowGap);
            var viewRect = new Rect(0f, 0f, inRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(inRect, ref _scrollPosition, viewRect);

            float y = 0f;
            foreach (var cat in categories)
            {
                var rowRect = new Rect(0f, y, viewRect.width, RowHeight);
                DrawRow(rowRect, cat, medicines);
                y += RowHeight + RowGap;
            }

            Widgets.EndScrollView();
        }

        private static void DrawRow(Rect rect, SurgeryCategoryRegistry.Category cat, IReadOnlyList<MedicineRegistry.Entry> medicines)
        {
            float labelWidth = rect.width - IconSize - DropdownWidth - (ColumnGap * 2f);
            var labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            var iconRect = new Rect(labelRect.xMax + ColumnGap, rect.y + (rect.height - IconSize) * 0.5f, IconSize, IconSize);
            var ddRect = new Rect(iconRect.xMax + ColumnGap, rect.y + (rect.height - 28f) * 0.5f, DropdownWidth, 28f);

            string labelText = $"{cat.Label}  ({cat.Recipes.Count})";
            var prevAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, labelText);
            Text.Anchor = prevAnchor;

            TooltipHandler.TipRegion(rect, () => RecipeListTooltip(cat), cat.Key.GetHashCode());

            if (cat.MinimumMedicine != null)
                Widgets.ThingIcon(iconRect, cat.MinimumMedicine);

            string current = cat.MinimumMedicine != null
                ? cat.MinimumMedicine.LabelCap.ToString()
                : "ModdedMedicineCategories.None".Translate().ToString();

            if (Widgets.ButtonText(ddRect, current))
            {
                var options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("ModdedMedicineCategories.None".Translate(), () => { }),
                };
                foreach (var med in medicines)
                {
                    var capture = med;
                    options.Add(new FloatMenuOption(capture.Def.LabelCap, () => { }, capture.Def));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static string RecipeListTooltip(SurgeryCategoryRegistry.Category cat)
        {
            const int maxLines = 25;
            var names = cat.Recipes.Select(r => r.LabelCap.ToString()).ToList();
            if (names.Count <= maxLines)
                return string.Join("\n", names);
            return string.Join("\n", names.Take(maxLines))
                + "\n"
                + "ModdedMedicineCategories.AndMore".Translate(names.Count - maxLines);
        }
    }
}
