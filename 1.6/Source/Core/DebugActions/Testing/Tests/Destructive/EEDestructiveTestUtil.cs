using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FactionColonies.Events
{
    /// <summary>
    /// More Events-specific helpers for the destructive test tier. Builds on the base mod's
    /// <see cref="DestructiveTestUtil"/> (transient-settlement create/teardown + invariant battery)
    /// and adds the fixtures the More Events destructive tests share: settlement/comp/resource
    /// lookups and a snapshot/sweep for the military operations a test launches.
    /// </summary>
    public static class EEDestructiveTestUtil
    {
        /// <summary>First live settlement, or a freshly created transient one (null if neither possible).</summary>
        public static WorldSettlementFC FirstOrTransient(FactionFC f)
        {
            if (f?.settlements != null && f.settlements.Count > 0) return f.settlements[0];
            return DestructiveTestUtil.CreateTransientSettlement();
        }

        /// <summary>The settlement's ImmigrantBoost comp, or null if the def doesn't carry it.</summary>
        public static WorldObjectComp_ImmigrantBoost BoostComp(WorldSettlementFC settlement)
        {
            return settlement?.GetComponent<WorldObjectComp_ImmigrantBoost>();
        }

        /// <summary>First non-pool resource that carries a production-additive stat (what a boost applies to).</summary>
        public static ResourceTypeDef FirstBoostableResource()
        {
            return DefDatabase<ResourceTypeDef>.AllDefsListForReading
                .FirstOrDefault(r => r.productionAdditiveStat is object && !r.isPoolResource);
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
            return active is object
                ? new HashSet<MilitaryOperation>(active)
                : new HashSet<MilitaryOperation>();
        }

        /// <summary>Operations that became active since <paramref name="before"/> was captured.</summary>
        public static List<MilitaryOperation> NewOpsSince(HashSet<MilitaryOperation> before)
        {
            IReadOnlyList<MilitaryOperation> active = FindFC.MilitaryManager?.Active;
            if (active is null) return new List<MilitaryOperation>();
            return active.Where(op => op is object && !before.Contains(op)).ToList();
        }

        /// <summary>Resolves and sweeps a set of operations so a launched attack doesn't linger.</summary>
        public static void SweepOps(IEnumerable<MilitaryOperation> ops)
        {
            foreach (MilitaryOperation op in ops)
                DestructiveTestUtil.ResolveAndSweepOp(op);
        }
    }
}
