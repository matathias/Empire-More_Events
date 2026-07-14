namespace FactionColonies.Events
{
    /// <summary>
    /// Non-destructive tests for <see cref="FCEventHandlerExtension_TriggerAttack"/> config and its
    /// attack-count clamp. The world-dependent <c>ResolveEvent</c> path (which launches real attacks)
    /// is covered in the destructive tier.
    /// </summary>
    public static class TriggerAttackTests
    {
        [EmpireTest("Events.Attack")]
        public static void DefaultAttackCount_IsExactlyOne()
        {
            var handler = new FCEventHandlerExtension_TriggerAttack();
            TestAssert.AreEqual(1, handler.attackCount.min, "default attackCount.min should be 1");
            TestAssert.AreEqual(1, handler.attackCount.max, "default attackCount.max should be 1");
        }

        [EmpireTest("Events.Attack")]
        public static void ResolveAttackCount_ClampsRollToAtLeastOne()
        {
            // A zero/negative roll is floored to 1 (then capped by candidate count).
            TestAssert.AreEqual(1, FCEventHandlerExtension_TriggerAttack.ResolveAttackCount(0, 5));
        }

        [EmpireTest("Events.Attack")]
        public static void ResolveAttackCount_CappedByCandidateCount()
        {
            // A roll larger than the available settlements is capped at the settlement count.
            TestAssert.AreEqual(2, FCEventHandlerExtension_TriggerAttack.ResolveAttackCount(5, 2));
        }

        [EmpireTest("Events.Attack")]
        public static void ResolveAttackCount_WithinRange_Unchanged()
        {
            TestAssert.AreEqual(3, FCEventHandlerExtension_TriggerAttack.ResolveAttackCount(3, 5));
        }
    }
}
