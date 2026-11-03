using System.Collections.Generic;

namespace FactionColonies.Events
{
    /// <summary>
    /// Pure, hermetic boundary tests for the decision predicates extracted from
    /// <see cref="EventConditionChecker"/> (Golden Age thresholds, cooldown/sustain timing, the
    /// Growing Pains count gate, tribute proximity, and event eligibility). No game state is
    /// touched — these are arrange/act/assert on literals, mirroring the base mod's math tests.
    /// </summary>
    public static class ConditionPredicateTests
    {
        /*-*-*- Golden Age thresholds: happiness>90 && loyalty>90 && unrest<10 && prosperity>80 -*-*-*/

        [EmpireTest("Events.Conditions")]
        public static void GoldenAge_AllAboveThreshold_True()
        {
            TestAssert.IsTrue(EventConditionChecker.GoldenAgeConditionsMet(91, 91, 9, 81));
            TestAssert.IsTrue(EventConditionChecker.GoldenAgeConditionsMet(90.001, 90.001, 9.999, 80.001));
        }

        [EmpireTest("Events.Conditions")]
        public static void GoldenAge_HappinessAtBoundary_False()
        {
            // 90 is not > 90.
            TestAssert.IsFalse(EventConditionChecker.GoldenAgeConditionsMet(90, 91, 9, 81));
        }

        [EmpireTest("Events.Conditions")]
        public static void GoldenAge_LoyaltyAtBoundary_False()
        {
            TestAssert.IsFalse(EventConditionChecker.GoldenAgeConditionsMet(91, 90, 9, 81));
        }

        [EmpireTest("Events.Conditions")]
        public static void GoldenAge_UnrestAtBoundary_False()
        {
            // 10 is not < 10.
            TestAssert.IsFalse(EventConditionChecker.GoldenAgeConditionsMet(91, 91, 10, 81));
        }

        [EmpireTest("Events.Conditions")]
        public static void GoldenAge_ProsperityAtBoundary_False()
        {
            // 80 is not > 80.
            TestAssert.IsFalse(EventConditionChecker.GoldenAgeConditionsMet(91, 91, 9, 80));
        }

        /*-*-*- Cooldown window: now - lastTick >= cooldownTicks -*-*-*/

        [EmpireTest("Events.Conditions")]
        public static void Cooldown_ExactlyElapsed_True()
        {
            TestAssert.IsTrue(EventConditionChecker.CooldownElapsed(100, 0, 100));
        }

        [EmpireTest("Events.Conditions")]
        public static void Cooldown_OneShort_False()
        {
            TestAssert.IsFalse(EventConditionChecker.CooldownElapsed(99, 0, 100));
        }

        [EmpireTest("Events.Conditions")]
        public static void Cooldown_NoTimePassed_False()
        {
            TestAssert.IsFalse(EventConditionChecker.CooldownElapsed(0, 0, 100));
        }

        /*-*-*- Sustain window: qualifyingSince >= 0 && now - qualifyingSince >= sustainTicks -*-*-*/

        [EmpireTest("Events.Conditions")]
        public static void Sustain_NotYetQualifying_False()
        {
            // qualifyingSince == -1 means the sustain timer has not started.
            TestAssert.IsFalse(EventConditionChecker.SustainSatisfied(100, -1, 50));
        }

        [EmpireTest("Events.Conditions")]
        public static void Sustain_ExactlyMet_True()
        {
            TestAssert.IsTrue(EventConditionChecker.SustainSatisfied(150, 100, 50));
        }

        [EmpireTest("Events.Conditions")]
        public static void Sustain_OneShort_False()
        {
            TestAssert.IsFalse(EventConditionChecker.SustainSatisfied(149, 100, 50));
        }

        /*-*-*- Growing Pains: settlementCount > threshold (6) -*-*-*/

        [EmpireTest("Events.Conditions")]
        public static void GrowingPains_AtThreshold_False()
        {
            TestAssert.IsFalse(EventConditionChecker.GrowingPainsThresholdMet(EventConditionChecker.GrowingPainsSettlementThreshold));
        }

        [EmpireTest("Events.Conditions")]
        public static void GrowingPains_AboveThreshold_True()
        {
            TestAssert.IsTrue(EventConditionChecker.GrowingPainsThresholdMet(EventConditionChecker.GrowingPainsSettlementThreshold + 1));
        }

        /*-*-*- Tribute proximity: distance <= maxTiles -*-*-*/

        [EmpireTest("Events.Conditions")]
        public static void Proximity_AtMax_True()
        {
            TestAssert.IsTrue(EventConditionChecker.WithinTributeProximity(50f, 50f));
        }

        [EmpireTest("Events.Conditions")]
        public static void Proximity_JustBeyondMax_False()
        {
            TestAssert.IsFalse(EventConditionChecker.WithinTributeProximity(50.01f, 50f));
        }

        /*-*-*- Eligibility: no duplicate / incompatible active event -*-*-*/

        [EmpireTest("Events.Conditions")]
        public static void IsEligible_NoActiveEvents_True()
        {
            FCEventDef def = EETestHelper.MakeEventDef("ee_test_a");
            TestAssert.IsTrue(EventConditionChecker.IsEligible(def, new List<FCEvent>()));
        }

        [EmpireTest("Events.Conditions")]
        public static void IsEligible_DuplicateDefActive_False()
        {
            FCEventDef def = EETestHelper.MakeEventDef("ee_test_a");
            var active = new List<FCEvent> { EETestHelper.MakeEvent(def) };
            TestAssert.IsFalse(EventConditionChecker.IsEligible(def, active));
        }

        [EmpireTest("Events.Conditions")]
        public static void IsEligible_ActiveListsCandidateAsIncompatible_False()
        {
            FCEventDef candidate = EETestHelper.MakeEventDef("ee_test_a");
            FCEventDef active = EETestHelper.MakeEventDef("ee_test_b",
                new List<FCEventDef> { candidate });
            TestAssert.IsFalse(EventConditionChecker.IsEligible(candidate,
                new List<FCEvent> { EETestHelper.MakeEvent(active) }));
        }

        [EmpireTest("Events.Conditions")]
        public static void IsEligible_CandidateListsActiveAsIncompatible_False()
        {
            FCEventDef active = EETestHelper.MakeEventDef("ee_test_b");
            FCEventDef candidate = EETestHelper.MakeEventDef("ee_test_a",
                new List<FCEventDef> { active });
            TestAssert.IsFalse(EventConditionChecker.IsEligible(candidate,
                new List<FCEvent> { EETestHelper.MakeEvent(active) }));
        }

        [EmpireTest("Events.Conditions")]
        public static void IsEligible_UnrelatedActiveEvent_True()
        {
            FCEventDef candidate = EETestHelper.MakeEventDef("ee_test_a");
            FCEventDef unrelated = EETestHelper.MakeEventDef("ee_test_b");
            TestAssert.IsTrue(EventConditionChecker.IsEligible(candidate,
                new List<FCEvent> { EETestHelper.MakeEvent(unrelated) }));
        }

        /*-*-*- Options-disabled filter -*-*-*/

        [EmpireTest("Events.Conditions")]
        public static void BlockedByOptions_SettingOff_NeverBlocks()
        {
            EETestHelper.SettingsSnapshot snap = EETestHelper.SnapshotSettings();
            try
            {
                FCSettings.disableEventsWithOptions = false;
                FCEventDef def = EETestHelper.MakeEventDef("ee_test_not_in_chain_set");
                TestAssert.IsFalse(EventConditionChecker.BlockedByOptionsSetting(def));
            }
            finally
            {
                EETestHelper.RestoreSettings(snap);
            }
        }

        [EmpireTest("Events.Conditions")]
        public static void BlockedByOptions_SettingOn_DefNotInChain_NotBlocked()
        {
            EETestHelper.SettingsSnapshot snap = EETestHelper.SnapshotSettings();
            try
            {
                FCSettings.disableEventsWithOptions = true;
                FCEventDef def = EETestHelper.MakeEventDef("ee_test_not_in_chain_set");
                TestAssert.IsFalse(EventConditionChecker.BlockedByOptionsSetting(def));
            }
            finally
            {
                EETestHelper.RestoreSettings(snap);
            }
        }

        [EmpireTest("Events.Conditions")]
        public static void BlockedByOptions_SettingOn_OptionEventInChain_Blocked()
        {
            FCEventDef growingPains = MoreEventsDefOf.empireEvents_growingPains_0;
            if (growingPains is null) TestAssert.Skip("empireEvents_growingPains_0 not loaded");
            if (!FactionCache.EventDefNamesWithOptionsInChain.Contains(growingPains.defName))
                TestAssert.Skip("empireEvents_growingPains_0 is not registered as an options-in-chain event");

            EETestHelper.SettingsSnapshot snap = EETestHelper.SnapshotSettings();
            try
            {
                FCSettings.disableEventsWithOptions = true;
                TestAssert.IsTrue(EventConditionChecker.BlockedByOptionsSetting(growingPains));
            }
            finally
            {
                EETestHelper.RestoreSettings(snap);
            }
        }
    }
}
