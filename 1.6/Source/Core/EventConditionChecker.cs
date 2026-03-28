using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using HarmonyLib;

namespace FactionColonies.Events
{
    /// <summary>
    /// WorldComponent that periodically checks conditions the random event pool can't handle:
    /// settlement count thresholds, sustained stat conditions, and NPC proximity.
    /// Generates events programmatically when conditions are met.
    /// </summary>
    public class EventConditionChecker : WorldComponent
    {
        private const int CheckInterval = GenDate.TicksPerDay;
        private const int CooldownTicks = 30 * GenDate.TicksPerDay; // 30 days
        private const int GoldenAgeSustainTicks = 5 * GenDate.TicksPerDay; // 5 days
        private const float TributeProximityTiles = 50f;

        // Cooldown tracking (last tick each event was generated)
        private int lastGoldenAgeTick = -CooldownTicks;
        private int lastGrowingPainsTick = -CooldownTicks;
        private int lastOverextensionTick = -CooldownTicks;
        private int lastTributeTick = -CooldownTicks;

        // Golden Age sustained-condition tracking
        private int goldenAgeQualifyingSince = -1;

        public EventConditionChecker(World world) : base(world)
        {
        }

        public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame % CheckInterval != 0) return;

            FactionFC faction = FactionCache.FactionComp;
            if (faction == null || !faction.settlements.Any()) return;

            int now = Find.TickManager.TicksGame;

            CheckGoldenAge(faction, now);
            CheckGrowingPains(faction, now);
            CheckOverextensionCrisis(faction, now);

            // CheckTributeDemand does a proximity check, which is a little more expensive than I'd like.
            //   So we'll call it less frequently than the other checks.
            if (Find.TickManager.TicksGame % (12 * CheckInterval) == 0)
                CheckTributeDemand(faction, now);
        }

        private void CheckGoldenAge(FactionFC faction, int now)
        {
            if (now - lastGoldenAgeTick < CooldownTicks) return;

            // Not in first year
            int elapsed = now - faction.FoundingTick;
            if (elapsed < GenDate.TicksPerYear) return;

            bool qualifies = faction.averageHappiness > 90
                && faction.averageLoyalty > 90
                && faction.averageUnrest < 10
                && faction.averageProsperity > 80;

            if (!qualifies)
            {
                goldenAgeQualifyingSince = -1;
                return;
            }

            if (goldenAgeQualifyingSince < 0)
            {
                goldenAgeQualifyingSince = now;
                return;
            }

            if (now - goldenAgeQualifyingSince < GoldenAgeSustainTicks) return;

            FCEventDef def = DefDatabase<FCEventDef>.GetNamedSilentFail("empireEvents_goldenAge");
            if (def == null || !IsEligible(def, faction) || FCSettings.IsEventDisabled(def.defName)) return;

            TryFireEvent(def, faction);
            lastGoldenAgeTick = now;
            goldenAgeQualifyingSince = -1;
        }

        private void CheckGrowingPains(FactionFC faction, int now)
        {
            if (now - lastGrowingPainsTick < CooldownTicks) return;
            if (faction.settlements.Count <= 6) return;

            FCEventDef def = DefDatabase<FCEventDef>.GetNamedSilentFail("empireEvents_growingPains_0");
            if (def == null || !IsEligible(def, faction) || FCSettings.IsEventDisabled(def.defName)) return;

            TryFireEvent(def, faction);
            lastGrowingPainsTick = now;
        }

        private void CheckOverextensionCrisis(FactionFC faction, int now)
        {
            if (now - lastOverextensionTick < CooldownTicks) return;
            if (faction.settlements.Count <= 8) return;
            if (faction.averageLoyalty >= 60) return;

            FCEventDef def = DefDatabase<FCEventDef>.GetNamedSilentFail("empireEvents_overextension_0");
            if (def == null || !IsEligible(def, faction) || FCSettings.IsEventDisabled(def.defName)) return;

            TryFireEvent(def, faction);
            lastOverextensionTick = now;
        }

        private void CheckTributeDemand(FactionFC faction, int now)
        {
            if (now - lastTributeTick < CooldownTicks) return;

            // Check if any hostile faction has a settlement within proximity of any player settlement
            Faction playerFaction = FactionCache.PlayerColonyFaction;
            if (playerFaction == null) return;

            bool hasNearbyHostile = false;
            List<Settlement> hostileSettlements = Find.WorldObjects.Settlements.Where(s => s.Faction != null &&
                                                                                           !s.Faction.IsPlayer &&
                                                                                           !s.Faction.defeated &&
                                                                                           s.Faction.RelationKindWith(playerFaction) == FactionRelationKind.Hostile).ToList();

            foreach (WorldSettlementFC playerSettlement in faction.settlements)
            {
                foreach (Settlement hostile in hostileSettlements)
                {
                    float dist = Find.WorldGrid.ApproxDistanceInTiles(playerSettlement.Tile, hostile.Tile);
                    if (dist <= TributeProximityTiles)
                    {
                        hasNearbyHostile = true;
                        break;
                    }
                }
                if (hasNearbyHostile) break;
            }

            if (!hasNearbyHostile) return;

            FCEventDef def = DefDatabase<FCEventDef>.GetNamedSilentFail("empireEvents_tribute_0");
            if (def == null || !IsEligible(def, faction) || FCSettings.IsEventDisabled(def.defName)) return;

            TryFireEvent(def, faction);
            lastTributeTick = now;
        }

        /// <summary>
        /// Checks that no active event conflicts with this def (incompatible events or duplicate).
        /// </summary>
        private bool IsEligible(FCEventDef def, FactionFC faction)
        {
            foreach (FCEvent evt in faction.events)
            {
                if (evt.def == null) continue;
                if (evt.def == def) return false;

                foreach (FCEventDef incompat in evt.def.incompatibleEvents)
                {
                    if (incompat == def) return false;
                }

                foreach (FCEventDef incompat in def.incompatibleEvents)
                {
                    if (incompat == evt.def) return false;
                }
            }
            return true;
        }

        private void TryFireEvent(FCEventDef def, FactionFC faction)
        {
            FCEvent evt = FCEventMaker.MakeRandomEvent(def, null);
            if (evt == null) return;

            faction.AddEvent(evt);

            string settlementString = string.Join("\n", evt.settlementTraitLocations.Select(s => " " + s.Name));

            string desc = def.desc;
            if (!settlementString.NullOrEmpty())
            {
                desc += "\n" + "EventAffectingSettlements".Translate() + "\n" + settlementString;
            }

            Find.LetterStack.ReceiveLetter(def.label, desc, LetterDefOf.NeutralEvent);
            LogEE.Message("EventConditionChecker fired: " + def.defName);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastGoldenAgeTick, "lastGoldenAgeTick", -CooldownTicks);
            Scribe_Values.Look(ref lastGrowingPainsTick, "lastGrowingPainsTick", -CooldownTicks);
            Scribe_Values.Look(ref lastOverextensionTick, "lastOverextensionTick", -CooldownTicks);
            Scribe_Values.Look(ref lastTributeTick, "lastTributeTick", -CooldownTicks);
            Scribe_Values.Look(ref goldenAgeQualifyingSince, "goldenAgeQualifyingSince", -1);
        }
    }
}
