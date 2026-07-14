using System.Linq;
using Verse;

namespace FactionColonies.Events
{
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
            ResourceTypeDef res = DefDatabase<ResourceTypeDef>.AllDefsListForReading.FirstOrDefault();
            if (res is null) TestAssert.Skip("No ResourceTypeDef loaded");

            // With no WorldSettlementFC parent, ApplyModifier's Settlement-null guard makes SetBoost
            // store the choice without applying a modifier; ClearBoost then unwinds it. Neither throws.
            var comp = new WorldObjectComp_ImmigrantBoost();
            TestAssert.DoesNotThrow(() => comp.SetBoost(res), "SetBoost with no parent should not throw");
            TestAssert.DoesNotThrow(() => comp.ClearBoost(), "ClearBoost after a parentless SetBoost should not throw");
        }
    }
}
