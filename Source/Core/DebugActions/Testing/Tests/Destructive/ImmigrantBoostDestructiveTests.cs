namespace FactionColonies.Events;

/// <summary>
/// Destructive tests for <see cref="WorldObjectComp_ImmigrantBoost"/> against a real settlement:
/// the apply/clear lifecycle and the save-reload reapply that the comp exists to guarantee. Each
/// clears its own boost and ends on the base invariant battery.
/// </summary>
public static class ImmigrantBoostDestructiveTests
{
    [EmpireDestructiveTest("Events.Destructive.Boost")]
    public static void SetBoost_AppliesSingleModifier_ClearRemovesIt()
    {
        FactionFC f = DestructiveTestUtil.RequireFaction();
        WorldSettlementFC s = EEDestructiveTestUtil.EnsureBoostSettlement(out WorldObjectComp_ImmigrantBoost comp);
        ResourceTypeDef res = EEDestructiveTestUtil.FirstBoostableResource();
        FCStatDef stat = res.productionAdditiveStat;

        comp.ClearBoost(); // start from a known-clean state (a prior run may have left a boost)
        int baseline = EEDestructiveTestUtil.CountBoostModifiers(s, stat);

        comp.SetBoost(res);
        TestAssert.AreEqual(baseline + 1, EEDestructiveTestUtil.CountBoostModifiers(s, stat),
            "SetBoost should add exactly one +1 modifier on the resource's production stat");

        comp.ClearBoost();
        TestAssert.AreEqual(baseline, EEDestructiveTestUtil.CountBoostModifiers(s, stat),
            "ClearBoost should remove the modifier it added");

        DestructiveTestUtil.AssertEmpireInvariants(f, "SetBoost_AppliesSingleModifier");
    }

    [EmpireDestructiveTest("Events.Destructive.Boost")]
    public static void PostSettlementLoadInit_ReappliesBoostAfterRebuild()
    {
        FactionFC f = DestructiveTestUtil.RequireFaction();
        WorldSettlementFC s = EEDestructiveTestUtil.EnsureBoostSettlement(out WorldObjectComp_ImmigrantBoost comp);
        ResourceTypeDef res = EEDestructiveTestUtil.FirstBoostableResource();
        FCStatDef stat = res.productionAdditiveStat;
        string sourceId = FCEventHandlerExtension_ResourcePicker.ResourceSourcePrefix + res.defName;

        comp.ClearBoost();
        // The boost is only reapplied while its event is still queued for this settlement.
        FCEvent boostEvent = QueueBoostEvent(f, s);
        try
        {
            comp.SetBoost(res);
            int applied = EEDestructiveTestUtil.CountBoostModifiers(s, stat);

            // Reproduce the load-time rebuild that drops the transient boost modifier (the exact
            // regression this comp guards: PostLoadInit rebuilds statModifiers from buildings/type/
            // active-event defs only, which don't include the picker's per-resource choice).
            s.RemoveStatModifiersBySource(sourceId);
            TestAssert.AreEqual(applied - 1, EEDestructiveTestUtil.CountBoostModifiers(s, stat),
                "the simulated rebuild should drop the boost modifier");

            comp.PostSettlementLoadInit(s);
            TestAssert.AreEqual(applied, EEDestructiveTestUtil.CountBoostModifiers(s, stat),
                "PostSettlementLoadInit should reapply the boost from the scribed choice");
        }
        finally
        {
            f.eventManager.Remove(boostEvent); // its expiry clears the boost
            comp.ClearBoost();
        }

        DestructiveTestUtil.AssertEmpireInvariants(f, "PostSettlementLoadInit_ReappliesBoost");
    }

    /// <summary>
    /// A scribed boost whose event is gone (removed without its expiry cleanup) is dropped on load
    /// instead of being reapplied forever.
    /// </summary>
    [EmpireDestructiveTest("Events.Destructive.Boost")]
    public static void PostSettlementLoadInit_NoBoostEvent_DropsStaleBoost()
    {
        FactionFC f = DestructiveTestUtil.RequireFaction();
        WorldSettlementFC s = EEDestructiveTestUtil.EnsureBoostSettlement(out WorldObjectComp_ImmigrantBoost comp);
        ResourceTypeDef res = EEDestructiveTestUtil.FirstBoostableResource();
        FCStatDef stat = res.productionAdditiveStat;
        string sourceId = FCEventHandlerExtension_ResourcePicker.ResourceSourcePrefix + res.defName;

        comp.ClearBoost();
        RemoveBoostEventsFor(f, s);
        try
        {
            int baseline = EEDestructiveTestUtil.CountBoostModifiers(s, stat);
            comp.SetBoost(res);
            s.RemoveStatModifiersBySource(sourceId);   // simulated load rebuild

            comp.PostSettlementLoadInit(s);
            TestAssert.AreEqual(baseline, EEDestructiveTestUtil.CountBoostModifiers(s, stat),
                "with no boost event for the settlement, the stale boost should not be reapplied");

            // The choice itself is dropped too: a second load pass still applies nothing.
            comp.PostSettlementLoadInit(s);
            TestAssert.AreEqual(baseline, EEDestructiveTestUtil.CountBoostModifiers(s, stat),
                "the stale choice should be cleared, so a later load does not reapply it either");
        }
        finally
        {
            comp.ClearBoost();
        }

        DestructiveTestUtil.AssertEmpireInvariants(f, "PostSettlementLoadInit_DropsStaleBoost");
    }

    /// <summary>
    /// Only a boost event that targets this settlement keeps its boost alive; one for another settlement
    /// does not.
    /// </summary>
    [EmpireDestructiveTest("Events.Destructive.Boost")]
    public static void PostSettlementLoadInit_BoostEventForOtherSettlement_DropsStaleBoost()
    {
        FactionFC f = DestructiveTestUtil.RequireFaction();
        List<WorldSettlementFC> pair = DestructiveTestUtil.EnsureSettlements(2);
        WorldSettlementFC s = pair[0];
        WorldSettlementFC other = pair[1];
        WorldObjectComp_ImmigrantBoost comp = TestAssert.NotNull(EEDestructiveTestUtil.BoostComp(s),
            $"Settlement '{s.Name}' carries no ImmigrantBoost comp");
        ResourceTypeDef res = EEDestructiveTestUtil.FirstBoostableResource();
        FCStatDef stat = res.productionAdditiveStat;
        string sourceId = FCEventHandlerExtension_ResourcePicker.ResourceSourcePrefix + res.defName;

        comp.ClearBoost();
        RemoveBoostEventsFor(f, s);
        FCEvent otherEvent = QueueBoostEvent(f, other);
        try
        {
            int baseline = EEDestructiveTestUtil.CountBoostModifiers(s, stat);
            comp.SetBoost(res);
            s.RemoveStatModifiersBySource(sourceId);   // simulated load rebuild

            comp.PostSettlementLoadInit(s);
            TestAssert.AreEqual(baseline, EEDestructiveTestUtil.CountBoostModifiers(s, stat),
                "a boost event for a different settlement should not keep this settlement's boost alive");
        }
        finally
        {
            f.eventManager.Remove(otherEvent);
            comp.ClearBoost();
        }

        DestructiveTestUtil.AssertEmpireInvariants(f, "PostSettlementLoadInit_OtherSettlementEvent");
    }

    // Queues a real production-boost event targeting the settlement, the same way the resource picker does.
    private static FCEvent QueueBoostEvent(FactionFC f, WorldSettlementFC settlement)
    {
        FCEventDef def = TestAssert.NotNull(MoreEventsDefOf.empireEvents_immigrants_production_boost,
            "The production-boost event def is not loaded");
        FCEvent evt = TestAssert.NotNull(FCEventMaker.MakeRandomEvent(def, [settlement]),
            "Could not make a production-boost event");
        TestAssert.IsTrue(evt.settlementTraitLocations.Contains(settlement),
            "the boost event should target the requested settlement");
        f.eventManager.AddEvent(evt);
        return evt;
    }

    // Removes (with expiry cleanup) any boost event already targeting the settlement, so the stale case starts clean.
    private static void RemoveBoostEventsFor(FactionFC f, WorldSettlementFC settlement)
    {
        FCEventDef def = MoreEventsDefOf.empireEvents_immigrants_production_boost;
        f.eventManager.RemoveWhere(e => e?.def == def && e.settlementTraitLocations is not null
            && e.settlementTraitLocations.Contains(settlement));
    }
}
