namespace FactionColonies.Events
{
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
            WorldSettlementFC s = EEDestructiveTestUtil.FirstOrTransient(f);
            if (s is null) TestAssert.Skip("No settlement available");

            WorldObjectComp_ImmigrantBoost comp = EEDestructiveTestUtil.BoostComp(s);
            if (comp is null) TestAssert.Skip("Settlement has no ImmigrantBoost comp");

            ResourceTypeDef res = EEDestructiveTestUtil.FirstBoostableResource();
            if (res is null) TestAssert.Skip("No boostable resource (with productionAdditiveStat) available");
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
            WorldSettlementFC s = EEDestructiveTestUtil.FirstOrTransient(f);
            if (s is null) TestAssert.Skip("No settlement available");

            WorldObjectComp_ImmigrantBoost comp = EEDestructiveTestUtil.BoostComp(s);
            if (comp is null) TestAssert.Skip("Settlement has no ImmigrantBoost comp");

            ResourceTypeDef res = EEDestructiveTestUtil.FirstBoostableResource();
            if (res is null) TestAssert.Skip("No boostable resource (with productionAdditiveStat) available");
            FCStatDef stat = res.productionAdditiveStat;
            string sourceId = FCEventHandlerExtension_ResourcePicker.ResourceSourcePrefix + res.defName;

            comp.ClearBoost();
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

            comp.ClearBoost();
            DestructiveTestUtil.AssertEmpireInvariants(f, "PostSettlementLoadInit_ReappliesBoost");
        }
    }
}
