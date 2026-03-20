using System;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies.Events
{
    /// <summary>
    /// Shared attack handler. When the event resolves, picks an enemy faction,
    /// creates a military force, and attacks a random settlement from the event's locations.
    ///
    /// Used by: Refugee Influx (40% follow-up), Tribute Demand (refusal),
    /// Border Skirmish (Stage 2), Overextension Crisis (30% follow-up).
    /// </summary>
    public class FCEventHandlerExtension_TriggerAttack : FCEventHandlerExtension
    {
        public override bool ResolveEvent(FCEvent evt, FactionFC faction)
        {
            if (!faction.settlements.Any())
            {
                LogUtil.Warning("TriggerAttack: No settlements to attack.");
                return true;
            }

            double etl = ThreatScalingUtil.ComputeEmpireThreatLevel(faction);
            Faction enemy = ThreatScalingUtil.PickWeightedEnemyFaction(etl);
            if (enemy == null)
            {
                LogUtil.Warning("TriggerAttack: No hostile factions available.");
                return true;
            }

            militaryForce force = militaryForce.CreateMilitaryForceFromFaction(enemy, true);

            WorldSettlementFC target;
            if (evt.settlementTraitLocations != null && evt.settlementTraitLocations.Any())
            {
                target = evt.settlementTraitLocations.RandomElement();
            }
            else
            {
                target = faction.settlements.RandomElement();
            }

            MilitaryUtilFC.AttackPlayerSettlement(force, target, enemy);
            LogUtil.Message("TriggerAttack: Launched attack on " + target.Name + " by " + enemy.Name);

            return true;
        }
    }
}
