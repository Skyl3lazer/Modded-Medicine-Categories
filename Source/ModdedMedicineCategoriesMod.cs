using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModdedMedicineCategories
{
    public class ModdedMedicineCategoriesMod : Mod
    {
        public static ModdedMedicineCategoriesSettings Settings;

        private Vector2 _scrollPosition;
        private readonly HashSet<string> _expandedKeys = new HashSet<string>();

        private const float RowHeight = 32f;
        private const float RowGap = 4f;
        private const float IconSize = 28f;
        private const float DropdownWidth = 220f;
        private const float ColumnGap = 8f;
        private const float ToggleSize = 24f;
        private const float SurgeryLineHeight = 22f;
        private const float ExpandedPadding = 6f;
        private const float ExpandedIndent = 36f;

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

            float viewHeight = 0f;
            foreach (var cat in categories)
            {
                viewHeight += RowHeight;
                if (_expandedKeys.Contains(cat.Key))
                    viewHeight += ExpandedHeight(cat);
                viewHeight += RowGap;
            }

            var viewRect = new Rect(0f, 0f, inRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(inRect, ref _scrollPosition, viewRect);

            float y = 0f;
            foreach (var cat in categories)
            {
                var rowRect = new Rect(0f, y, viewRect.width, RowHeight);
                DrawRow(rowRect, cat, medicines);
                y += RowHeight;

                if (_expandedKeys.Contains(cat.Key))
                {
                    var expandedRect = new Rect(0f, y, viewRect.width, ExpandedHeight(cat));
                    DrawExpanded(expandedRect, cat);
                    y += ExpandedHeight(cat);
                }

                y += RowGap;
            }

            Widgets.EndScrollView();
        }

        private static float ExpandedHeight(SurgeryCategoryRegistry.Category cat)
        {
            return (cat.Recipes.Count * SurgeryLineHeight) + (ExpandedPadding * 2f);
        }

        private void DrawRow(Rect rect, SurgeryCategoryRegistry.Category cat, IReadOnlyList<MedicineRegistry.Entry> medicines)
        {
            bool expanded = _expandedKeys.Contains(cat.Key);

            var toggleRect = new Rect(rect.x, rect.y + (rect.height - ToggleSize) * 0.5f, ToggleSize, ToggleSize);
            string toggleLabel = expanded ? "\u25BC" : "\u25B6";
            if (Widgets.ButtonText(toggleRect, toggleLabel))
            {
                if (expanded) _expandedKeys.Remove(cat.Key);
                else _expandedKeys.Add(cat.Key);
            }
            TooltipHandler.TipRegion(
                toggleRect,
                expanded
                    ? (TipSignal)"ModdedMedicineCategories.HideSurgeries".Translate()
                    : (TipSignal)"ModdedMedicineCategories.ShowSurgeries".Translate());

            float labelStart = toggleRect.xMax + ColumnGap;
            float labelWidth = rect.width - (labelStart - rect.x) - IconSize - DropdownWidth - (ColumnGap * 2f);
            var labelRect = new Rect(labelStart, rect.y, labelWidth, rect.height);
            var iconRect = new Rect(labelRect.xMax + ColumnGap, rect.y + (rect.height - IconSize) * 0.5f, IconSize, IconSize);
            var ddRect = new Rect(iconRect.xMax + ColumnGap, rect.y + (rect.height - 28f) * 0.5f, DropdownWidth, 28f);

            string labelText = $"{cat.Label}  ({cat.Recipes.Count})";
            var prevAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, labelText);
            Text.Anchor = prevAnchor;

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

        private static void DrawExpanded(Rect rect, SurgeryCategoryRegistry.Category cat)
        {
            Widgets.DrawLightHighlight(rect);

            var prevAnchor = Text.Anchor;
            var prevFont = Text.Font;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Small;

            float y = rect.y + ExpandedPadding;
            float lineWidth = rect.width - ExpandedIndent - ExpandedPadding;
            foreach (var recipe in cat.Recipes)
            {
                var lineRect = new Rect(rect.x + ExpandedIndent, y, lineWidth, SurgeryLineHeight);
                Widgets.Label(lineRect, "\u2022 " + recipe.LabelCap);
                y += SurgeryLineHeight;
            }

            Text.Anchor = prevAnchor;
            Text.Font = prevFont;
        }
    }
}
