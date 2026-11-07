namespace FactionColonies.Events;

/// <summary>
/// Destructive test for the world-dependent side of <see cref="FCEventHandlerExtension_TriggerAttack"/>:
/// it picks a hostile faction and launches real attacks. The launched operations are swept so the
/// raid doesn't linger, and the invariant battery confirms the faction stays consistent.
/// </summary>
public static class TriggerAttackDestructiveTests
{
    [EmpireDestructiveTest("Events.Destructive.Attack")]
    public static void ResolveEvent_LaunchesAttack_ThenSweeps()
    {
        FactionFC f = DestructiveTestUtil.RequireFaction();
        WorldSettlementFC target = DestructiveTestUtil.EnsureSettlement();

        EEDestructiveTestUtil.EnsureHostileFaction();
        TestAssert.NotNull(ThreatScalingUtil.PickWeightedEnemyFaction(f),
            "PickWeightedEnemyFaction found no attacker despite a hostile faction");

        var evt = new FCEvent
        {
            settlementTraitLocations = [target]
        };
        var handler = new FCEventHandlerExtension_TriggerAttack();

        HashSet<MilitaryOperation> before = EEDestructiveTestUtil.SnapshotActiveOps();
        bool result = false;
        TestAssert.DoesNotThrow(() => result = handler.ResolveEvent(evt, f), "ResolveEvent threw");
        TestAssert.IsTrue(result, "ResolveEvent should report success");

        List<MilitaryOperation> newOps = EEDestructiveTestUtil.NewOpsSince(before);
        // A settlement with no MilitaryComp legitimately rejects the attack (launched=0); only
        // assert an op was created when the target could actually be attacked.
        if (target.MilitaryComp is not null)
            TestAssert.IsTrue(newOps.Count >= 1, "expected at least one defensive op to be registered");

        EEDestructiveTestUtil.SweepOps(newOps);
        DestructiveTestUtil.AssertEmpireInvariants(f, "ResolveEvent_LaunchesAttack");
    }
}
