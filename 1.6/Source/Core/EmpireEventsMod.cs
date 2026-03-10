using UnityEngine;
using Verse;

namespace FactionColonies.Events
{
    public class EmpireEventsSettings : ModSettings
    {
        private static bool printDebug = false;
        public static bool PrintDebug => printDebug;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref printDebug, "printDebug", false);
        }

        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(inRect);
            ls.CheckboxLabeled("Enable debug logging", ref printDebug);
            ls.End();
        }
    }

    public class EmpireEventsMod : Mod
    {
        public EmpireEventsSettings settings;

        public EmpireEventsMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<EmpireEventsSettings>();
        }

        public override string SettingsCategory() => "Empire - Events";

        public override void DoSettingsWindowContents(Rect inRect) => settings.DoWindowContents(inRect);
    }
}
