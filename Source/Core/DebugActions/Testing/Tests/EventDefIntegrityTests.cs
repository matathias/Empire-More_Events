using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FactionColonies.Events
{
    /// <summary>
    /// A non-destructive sweep over every event def the submod ships (defName prefix
    /// <c>empireEvents_</c>). It walks the loaded <see cref="DefDatabase{FCEventDef}"/> and asserts
    /// the cross-references and value ranges the XML relies on: option success chances, resolved
    /// stat/incompatible/follow-up references, and instantiated mod extensions. This is the broadest
    /// single coverage win — it validates all shipped events and their handler wiring at once.
    /// </summary>
    public static class EventDefIntegrityTests
    {
        private const string DefNamePrefix = "empireEvents_";

        private static List<FCEventDef> SubmodEvents()
        {
            return DefDatabase<FCEventDef>.AllDefsListForReading
                .Where(d => d.defName != null && d.defName.StartsWith(DefNamePrefix))
                .ToList();
        }

        [EmpireTest("Events.Defs")]
        public static void SubmodEvents_AreLoaded()
        {
            // Guards against the sweep tests passing vacuously (e.g. a prefix/rename that matches nothing).
            TestAssert.IsNotEmpty(SubmodEvents(), "No empireEvents_* defs found in the DefDatabase");
        }

        [EmpireTest("Events.Defs")]
        public static void AllOptions_SuccessChanceInRange()
        {
            foreach (FCEventDef def in SubmodEvents())
            {
                foreach (FCOptionDef opt in def.options)
                {
                    if (opt is null) continue;
                    TestAssert.IsTrue(opt.baseChanceOfSuccess >= 0f && opt.baseChanceOfSuccess <= 100f,
                        $"{def.defName}: option '{opt.defName}' baseChanceOfSuccess {opt.baseChanceOfSuccess} out of [0,100]");
                }
            }
        }

        [EmpireTest("Events.Defs")]
        public static void AllStatModifiers_HaveResolvedStat()
        {
            foreach (FCEventDef def in SubmodEvents())
            {
                for (int i = 0; i < def.statModifiers.Count; i++)
                    TestAssert.IsNotNull(def.statModifiers[i].stat,
                        $"{def.defName}: statModifiers[{i}] has a null stat (unresolved defName?)");
                for (int i = 0; i < def.permanentStatModifiers.Count; i++)
                    TestAssert.IsNotNull(def.permanentStatModifiers[i].stat,
                        $"{def.defName}: permanentStatModifiers[{i}] has a null stat (unresolved defName?)");
            }
        }

        [EmpireTest("Events.Defs")]
        public static void AllIncompatibleEvents_Resolve()
        {
            foreach (FCEventDef def in SubmodEvents())
            {
                for (int i = 0; i < def.incompatibleEvents.Count; i++)
                    TestAssert.IsNotNull(def.incompatibleEvents[i],
                        $"{def.defName}: incompatibleEvents[{i}] is null (unresolved defName?)");
            }
        }

        [EmpireTest("Events.Defs")]
        public static void PromisedFollowUps_HaveTarget()
        {
            foreach (FCEventDef def in SubmodEvents())
            {
                if (def.eventFollows || def.splitEventFollows)
                    TestAssert.IsNotNull(def.followingEvent,
                        $"{def.defName}: declares a follow-up (eventFollows/splitEventFollows) but followingEvent is null");
            }
        }

        [EmpireTest("Events.Defs")]
        public static void AllModExtensions_Instantiated()
        {
            foreach (FCEventDef def in SubmodEvents())
            {
                if (def.modExtensions is null) continue;
                for (int i = 0; i < def.modExtensions.Count; i++)
                    TestAssert.IsNotNull(def.modExtensions[i],
                        $"{def.defName}: modExtensions[{i}] failed to instantiate (bad Class= in XML?)");
            }
        }

        /*-*-*- DefOf references resolve -*-*-*/

        [EmpireTest("Events.Defs")]
        public static void MoreEventsDefOf_AllResolve()
        {
            TestAssert.IsNotNull(MoreEventsDefOf.empireEvents_goldenAge, "empireEvents_goldenAge did not resolve");
            TestAssert.IsNotNull(MoreEventsDefOf.empireEvents_growingPains_0, "empireEvents_growingPains_0 did not resolve");
            TestAssert.IsNotNull(MoreEventsDefOf.empireEvents_tribute_0, "empireEvents_tribute_0 did not resolve");
        }
    }
}
