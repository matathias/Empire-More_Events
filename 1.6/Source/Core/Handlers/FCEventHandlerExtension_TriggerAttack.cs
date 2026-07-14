using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies.Events
{
    /// <summary>
    /// Shared attack handler. When the event resolves, picks an enemy faction and
    /// launches <see cref="attackCount"/> random attacks against distinct settlements
    /// drawn from the event's locations (or the faction-wide pool as a fallback).
    /// All attacks in one event share the same enemy faction and tech baseline; each
    /// attack gets its own MilitaryForce instance so per-battle forceRemaining doesn't bleed.
    ///
    /// Used by: Refugee Influx (25% follow-up), Tribute Demand (refusal / militaristic /
    /// feudal / counter-fail), Border Skirmish (Stage 2).
    /// </summary>
    public class FCEventHandlerExtension_TriggerAttack : FCEventHandlerExtension
    {
        public IntRange attackCount = new IntRange(1, 1);

        public override bool ResolveEvent(FCEvent evt, FactionFC faction)
        {
            if (!faction.settlements.Any())
            {
                LogEE.Warning("TriggerAttack: No settlements to attack.");
                return true;
            }

            Faction enemy = ThreatScalingUtil.PickWeightedEnemyFaction(faction);
            if (enemy == null)
            {
                LogEE.Warning("TriggerAttack: No hostile factions available.");
                return true;
            }

            MilitaryDeploymentUtil.GetTechLevelBaseline(enemy.def.techLevel, out double level, out double efficiency);

            List<WorldSettlementFC> candidates;
            if (evt.settlementTraitLocations != null && evt.settlementTraitLocations.Any())
                candidates = evt.settlementTraitLocations.Where(s => s != null).Distinct().ToList();
            else
                candidates = faction.settlements.ToList();

            if (candidates.Count == 0)
            {
                LogEE.Warning("TriggerAttack: No candidate settlements after filtering.");
                return true;
            }

            int actual = ResolveAttackCount(attackCount.RandomInRange, candidates.Count);
            List<WorldSettlementFC> targets = candidates.InRandomOrder().Take(actual).ToList();

            int launched = 0;
            foreach (WorldSettlementFC target in targets)
            {
                MilitaryForce force = new MilitaryForce(level, efficiency, null, enemy);
                if (MilitaryOperationsUtil.AttackPlayerSettlement(force, target, enemy))
                    launched++;
            }

            LogEE.Message("TriggerAttack: Launched " + launched + "/" + targets.Count + " attack(s) by " + enemy.Name + ".");
            return true;
        }

        /// <summary>
        /// Clamps a rolled attack count to at least one, then down to the number of available
        /// target settlements. Extracted so the clamp is testable without the world-dependent roll.
        /// </summary>
        internal static int ResolveAttackCount(int requestedRandom, int candidateCount) =>
            Math.Min(Math.Max(1, requestedRandom), candidateCount);
    }
}
