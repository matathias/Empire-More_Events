namespace FactionColonies.Events;

/// <summary>
/// Non-destructive tests for <see cref="WorldObjectComp_ImmigrantBoost"/> that don't require a
/// live settlement: the source-id constant and the null-guards on a parentless comp. The
/// settlement-attached round-trip (apply / save-reload reapply / clear) is covered destructively.
/// </summary>
public static class ImmigrantBoostTests
{
    [EmpireTest("Events.Boost")]
    public static void ResourceSourcePrefix_IsStable()
    {
        // The comp's SourceId and the cleanup handler both key off this exact prefix; a silent
        // change would orphan modifiers instead of clearing them.
        TestAssert.AreEqual("event_immigrants_resource_",
            FCEventHandlerExtension_ResourcePicker.ResourceSourcePrefix);
    }

    [EmpireTest("Events.Boost")]
    public static void ClearBoost_NoBoostSet_NoOp()
    {
        var comp = new WorldObjectComp_ImmigrantBoost();
        TestAssert.DoesNotThrow(() => comp.ClearBoost(), "ClearBoost on an unset comp should be a safe no-op");
    }

    [EmpireTest("Events.Boost")]
    public static void SetBoost_NoParent_DoesNotThrow()
    {
        ResourceTypeDef res = TestAssert.NotNull(DefDatabase<ResourceTypeDef>.AllDefsListForReading.FirstOrDefault(),
            "No ResourceTypeDef loaded");

        // With no WorldSettlementFC parent, ApplyModifier's Settlement-null guard makes SetBoost
        // store the choice without applying a modifier; ClearBoost then unwinds it. Neither throws.
        var comp = new WorldObjectComp_ImmigrantBoost();
        TestAssert.DoesNotThrow(() => comp.SetBoost(res), "SetBoost with no parent should not throw");
        TestAssert.DoesNotThrow(() => comp.ClearBoost(), "ClearBoost after a parentless SetBoost should not throw");
    }

    [EmpireTest("Events.Boost", GameState = TestGameState.Any)]
    public static void BoostEventDefOf_Resolves()
    {
        // The stale-boost check on load and the resource picker both look the boost event up through
        // this DefOf; a renamed def would leave it null and drop every saved boost.
        TestAssert.IsNotNull(MoreEventsDefOf.empireEvents_immigrants_production_boost,
            "MoreEventsDefOf.empireEvents_immigrants_production_boost should resolve to a loaded def");
    }
}
