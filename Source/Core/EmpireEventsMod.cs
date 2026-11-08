using UnityEngine;

namespace FactionColonies.Events;

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
        Listing_Standard ls = new();
        ls.Begin(inRect);

        ls.Label("EE_ModVersion".Translate(EmpireEventsMod.GetModVersion()));
        ls.Gap(10f);

        ls.CheckboxLabeled("EE_SettingsDebugLogging".Translate(), ref printDebug);

        ls.Gap(12f);
        if (ls.ButtonText("EV_OpenPatchNotes".Translate()))
            Find.WindowStack.Add(new PatchNotesDisplayWindow("matathias.empire.events", "EV_PatchTitle".Translate()));

        ls.End();
    }
}

public class EmpireEventsMod : Mod
{
    public EmpireEventsSettings settings;

    public EmpireEventsMod(ModContentPack content) : base(content)
    {
        settings = GetSettings<EmpireEventsSettings>();
        
        ModVersionUtil.LogLoaded(content, LogEE.MessageForce);
    }

    public static string GetModVersion() => ModVersionUtil.Get<EmpireEventsMod>();

    public override string SettingsCategory() => "EE_SettingsCategory".Translate();

    public override void DoSettingsWindowContents(Rect inRect) => settings.DoWindowContents(inRect);
}
