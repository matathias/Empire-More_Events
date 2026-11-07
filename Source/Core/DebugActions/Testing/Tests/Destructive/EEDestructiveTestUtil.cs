namespace FactionColonies.Events;

/// <summary>
/// More Events-specific helpers for the destructive test tier. Builds on the base mod's
/// <see cref="DestructiveTestUtil"/> (transient-settlement create/teardown + invariant battery)
/// and adds the fixtures the More Events destructive tests share: settlement/comp/resource
/// lookups and a snapshot/sweep for the military operations a test launches.
/// </summary>
public static class EEDestructiveTestUtil
{
    /// <summary>
    /// A live settlement carrying the ImmigrantBoost comp (which this mod patches onto surface
    /// settlements), creating a transient surface settlement if none has it. Fails the test if the
    /// new settlement lacks the comp.
    /// </summary>
    public static WorldSettlementFC EnsureBoostSettlement(out WorldObjectComp_ImmigrantBoost comp)
    {
        WorldSettlementFC s = DestructiveTestUtil.EnsureSettlement(x => BoostComp(x) is not null);
        comp = BoostComp(s);
        return s;
    }

    /// <summary>The settlement's ImmigrantBoost comp, or null if the def doesn't carry it.</summary>
    public static WorldObjectComp_ImmigrantBoost BoostComp(WorldSettlementFC settlement)
    {
        return settlement?.GetComponent<WorldObjectComp_ImmigrantBoost>();
    }

    /// <summary>First non-pool resource that carries a production-additive stat (what a boost
    /// applies to). The base mod ships several, so none is a failure.</summary>
    public static ResourceTypeDef FirstBoostableResource()
    {
        return TestAssert.NotNull(DefDatabase<ResourceTypeDef>.AllDefsListForReading
                .FirstOrDefault(r => r.productionAdditiveStat is not null && !r.isPoolResource),
            "No boostable resource (with productionAdditiveStat) is defined");
    }

    /// <summary>
    /// Makes sure at least one visible, undefeated faction is hostile to the player (what an
    /// empire attack needs to pick an attacker). If none is, the first eligible non-player,
    /// non-empire faction is declared hostile. Fails the test if no faction can be made hostile.
    /// </summary>
    public static void EnsureHostileFaction()
    {
        bool Eligible(Faction x) => !x.IsPlayer && !x.defeated && !x.Hidden;
        List<Faction> visible = Find.FactionManager.AllFactionsVisible.Where(Eligible).ToList();
        if (visible.Any(x => x.HostileTo(Faction.OfPlayer))) return;

        Faction target = TestAssert.NotNull(visible.FirstOrDefault(x => x != FindFC.EmpireFaction),
            "No visible non-player faction exists to become hostile");
        target.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, canSendHostilityLetter: false);
        TestAssert.IsTrue(target.HostileTo(Faction.OfPlayer), $"Could not make {target.Name} hostile to the player");
    }

    /// <summary>Counts settlement modifiers matching the boost's shape (+1 on the given stat).</summary>
    public static int CountBoostModifiers(WorldSettlementFC settlement, FCStatDef stat)
    {
        return settlement.StatModifiers.Count(m => m.stat == stat && m.value == 1.0);
    }

    /// <summary>Snapshot of the currently-active military operations, for diffing after a test.</summary>
    public static HashSet<MilitaryOperation> SnapshotActiveOps()
    {
        IReadOnlyList<MilitaryOperation> active = FindFC.MilitaryManager?.Active;
        return active is not null
            ? new HashSet<MilitaryOperation>(active)
            : [];
    }

    /// <summary>Operations that became active since <paramref name="before"/> was captured.</summary>
    public static List<MilitaryOperation> NewOpsSince(HashSet<MilitaryOperation> before)
    {
        IReadOnlyList<MilitaryOperation> active = FindFC.MilitaryManager?.Active;
        if (active is null) return [];
        return active.Where(op => op is not null && !before.Contains(op)).ToList();
    }

    /// <summary>Resolves and sweeps a set of operations so a launched attack doesn't linger.</summary>
    public static void SweepOps(IEnumerable<MilitaryOperation> ops)
    {
        foreach (MilitaryOperation op in ops)
            DestructiveTestUtil.ResolveAndSweepOp(op);
    }
}
