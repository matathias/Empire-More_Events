using Verse;

namespace FactionColonies.Events
{
    /// <summary>
    /// Destructive coverage of the condition-event firing path without poking
    /// <see cref="EventConditionChecker"/> internals: it drives the same base APIs the checker's
    /// TryFireEvent uses (<see cref="FCEventMaker.MakeRandomEvent"/> +
    /// <see cref="FCEventMaker.BuildEventLetterBody"/>) for each condition-triggered def, and smoke-tests
    /// the checker's periodic tick. The letter-body check guards the unresolved-{FACTION}-token
    /// regression that motivated building the body via BuildEventLetterBody in the first place.
    /// </summary>
    public static class ConditionFiringDestructiveTests
    {
        private static readonly System.Func<FCEventDef>[] ConditionDefs =
        {
            () => MoreEventsDefOf.empireEvents_goldenAge,
            () => MoreEventsDefOf.empireEvents_growingPains_0,
            () => MoreEventsDefOf.empireEvents_tribute_0,
        };

        [EmpireDestructiveTest("Events.Destructive.Conditions")]
        public static void ConditionEvents_MakeAndRenderLetter_NoRawTokens()
        {
            FactionFC f = DestructiveTestUtil.RequireFaction();

            foreach (System.Func<FCEventDef> get in ConditionDefs)
            {
                FCEventDef def = get();
                if (def is null) continue;

                FCEvent evt = null;
                TestAssert.DoesNotThrow(() => evt = FCEventMaker.MakeRandomEvent(def, null),
                    $"{def.defName}: MakeRandomEvent threw");
                // Option/activate-at-start events legitimately return null (they open a window instead).
                if (evt is null) continue;

                string body = null;
                TestAssert.DoesNotThrow(() => body = FCEventMaker.BuildEventLetterBody(evt),
                    $"{def.defName}: BuildEventLetterBody threw");
                TestAssert.IsNotNull(body, $"{def.defName}: letter body was null");
                TestAssert.IsFalse(body.Contains("{FACTION"),
                    $"{def.defName}: letter body has an unresolved token -- {body}");
            }

            DestructiveTestUtil.AssertEmpireInvariants(f, "ConditionEvents_MakeAndRenderLetter");
        }

        [EmpireDestructiveTest("Events.Destructive.Conditions")]
        public static void WorldComponentTick_DoesNotThrow()
        {
            FactionFC f = DestructiveTestUtil.RequireFaction();
            EventConditionChecker checker = Find.World?.GetComponent<EventConditionChecker>();
            if (checker is null) TestAssert.Skip("No EventConditionChecker on the world");

            TestAssert.DoesNotThrow(() => checker.WorldComponentTick(), "WorldComponentTick threw");
            DestructiveTestUtil.AssertEmpireInvariants(f, "WorldComponentTick_DoesNotThrow");
        }
    }
}
