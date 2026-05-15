using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ModdedMedicineCategories
{
    internal static class MedicineRegistry
    {
        public class Entry
        {
            public ThingDef Def;
            public float Potency;
        }

        private static List<Entry> _all;

        public static IReadOnlyList<Entry> All
        {
            get
            {
                if (_all == null) Build();
                return _all;
            }
        }

        public static void Invalidate() => _all = null;

        public static float PotencyOf(ThingDef def)
        {
            foreach (var e in All)
            {
                if (e.Def == def) return e.Potency;
            }
            return 0f;
        }

        public static bool Contains(ThingDef def)
        {
            if (def == null) return false;
            foreach (var e in All)
            {
                if (e.Def == def) return true;
            }
            return false;
        }

        private static void Build()
        {
            var list = new List<Entry>();
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def?.statBases == null) continue;
                var stat = def.statBases.FirstOrDefault(s => s?.stat == StatDefOf.MedicalPotency);
                if (stat == null) continue;
                list.Add(new Entry { Def = def, Potency = stat.value });
            }
            list.Sort((a, b) => a.Potency.CompareTo(b.Potency));
            _all = list;
        }
    }
}
