using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;


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
        // internal so the pure-predicate tests can assert against the real thresholds.
        internal const int CooldownTicks = 30 * GenDate.TicksPerDay; // 30 days
        internal const int GoldenAgeSustainTicks = 5 * GenDate.TicksPerDay; // 5 days
        internal const float TributeProximityTiles = 50f;
        internal const int GrowingPainsSettlementThreshold = 6;

        // Cooldown tracking (last tick each event was generated)
        private int lastGoldenAgeTick = -CooldownTicks;
        private int lastGrowingPainsTick = -CooldownTicks;
        private int lastTributeTick = -CooldownTicks;

        // Golden Age sustained-condition tracking
        private int goldenAgeQualifyingSince = -1;

        public EventConditionChecker(World world) : base(world)
        {
        }

        public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame % CheckInterval != 0) return;

            FactionFC faction = FindFC.FactionComp;
            if (faction is null || !faction.settlements.Any()) return;

            int now = Find.TickManager.TicksGame;

            CheckGoldenAge(faction, now);
            CheckGrowingPains(faction, now);

            // CheckTributeDemand does a proximity check, which is a little more expensive than I'd like.
            //   So we'll call it less frequently than the other checks.
            if (Find.TickManager.TicksGame % (12 * CheckInterval) == 0)
                CheckTributeDemand(faction, now);
        }

        private void CheckGoldenAge(FactionFC faction, int now)
        {
            if (!CooldownElapsed(now, lastGoldenAgeTick, CooldownTicks)) return;

            // Not in first year
            int elapsed = now - faction.FoundingTick;
            if (elapsed < GenDate.TicksPerYear) return;

            bool qualifies = GoldenAgeConditionsMet(faction.averageHappiness, faction.averageLoyalty,
                faction.averageUnrest, faction.averageProsperity);

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

            if (!SustainSatisfied(now, goldenAgeQualifyingSince, GoldenAgeSustainTicks)) return;

            FCEventDef def = MoreEventsDefOf.empireEvents_goldenAge;
            if (def is null || !IsEligible(def, faction.Events) || FCSettings.IsEventDisabled(def.defName)) return;

            TryFireEvent(def, faction);
            lastGoldenAgeTick = now;
            goldenAgeQualifyingSince = -1;
        }

        private void CheckGrowingPains(FactionFC faction, int now)
        {
            if (!CooldownElapsed(now, lastGrowingPainsTick, CooldownTicks)) return;
            if (!GrowingPainsThresholdMet(faction.settlements.Count)) return;

            FCEventDef def = MoreEventsDefOf.empireEvents_growingPains_0;
            if (def is null || !IsEligible(def, faction.Events) || FCSettings.IsEventDisabled(def.defName) || BlockedByOptionsSetting(def)) return;

            TryFireEvent(def, faction);
            lastGrowingPainsTick = now;
        }

        private void CheckTributeDemand(FactionFC faction, int now)
        {
            if (!CooldownElapsed(now, lastTributeTick, CooldownTicks)) return;

            // Check if any hostile faction has a settlement within proximity of any player settlement
            Faction empireFaction = FindFC.EmpireFaction;
            if (empireFaction is null) return;

            bool hasNearbyHostile = false;
            // Exclude the Empire faction's own settlements: they share empireFaction, so asking for
            // its relation with itself throws "Tried to get relation between faction and itself".
            List<Settlement> hostileSettlements = Find.WorldObjects.Settlements.Where(s => s.Faction is object &&
                                                                                           s.Faction != empireFaction &&
                                                                                           !s.Faction.IsPlayer &&
                                                                                           !s.Faction.defeated &&
                                                                                           s.Faction.RelationKindWith(empireFaction) == FactionRelationKind.Hostile).ToList();

            foreach (WorldSettlementFC playerSettlement in faction.settlements)
            {
                foreach (Settlement hostile in hostileSettlements)
                {
                    float dist = Find.WorldGrid.ApproxDistanceInTiles(playerSettlement.Tile, hostile.Tile);
                    if (WithinTributeProximity(dist, TributeProximityTiles))
                    {
                        hasNearbyHostile = true;
                        break;
                    }
                }
                if (hasNearbyHostile) break;
            }

            if (!hasNearbyHostile) return;

            FCEventDef def = MoreEventsDefOf.empireEvents_tribute_0;
            if (def is null || !IsEligible(def, faction.Events) || FCSettings.IsEventDisabled(def.defName) || BlockedByOptionsSetting(def)) return;

            TryFireEvent(def, faction);
            lastTributeTick = now;
        }

        /* Pure decision predicates, extracted so the eligibility/threshold/timing logic can be
         * asserted directly with literals (the Check* methods above are stateful and side-effecting). */

        /// <summary>All four Golden Age stat thresholds are met (high happiness/loyalty/prosperity, low unrest).</summary>
        internal static bool GoldenAgeConditionsMet(double happiness, double loyalty, double unrest, double prosperity) =>
            happiness > 90 && loyalty > 90 && unrest < 10 && prosperity > 80;

        /// <summary>The cooldown window since <paramref name="lastTick"/> has fully elapsed.</summary>
        internal static bool CooldownElapsed(int now, int lastTick, int cooldownTicks) =>
            now - lastTick >= cooldownTicks;

        /// <summary>Conditions have been continuously satisfied since <paramref name="qualifyingSince"/> for the sustain window.</summary>
        internal static bool SustainSatisfied(int now, int qualifyingSince, int sustainTicks) =>
            qualifyingSince >= 0 && now - qualifyingSince >= sustainTicks;

        /// <summary>Settlement count has crossed the Growing Pains threshold.</summary>
        internal static bool GrowingPainsThresholdMet(int settlementCount) =>
            settlementCount > GrowingPainsSettlementThreshold;

        /// <summary>A hostile settlement at <paramref name="distanceTiles"/> is close enough to demand tribute.</summary>
        internal static bool WithinTributeProximity(float distanceTiles, float maxTiles) =>
            distanceTiles <= maxTiles;

        /// <summary>
        /// Checks that no active event conflicts with this def (incompatible events or duplicate).
        /// </summary>
        internal static bool IsEligible(FCEventDef def, IEnumerable<FCEvent> activeEvents)
        {
            foreach (FCEvent evt in activeEvents)
            {
                if (evt.def is null) continue;
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

        /// <summary>
        /// Mirrors the base random pipeline's "disable events with options" filter for our
        /// condition-triggered events, which would otherwise open their option windows regardless.
        /// </summary>
        internal static bool BlockedByOptionsSetting(FCEventDef def) =>
            FCSettings.disableEventsWithOptions
            && FactionCache.EventDefNamesWithOptionsInChain.Contains(def.defName);

        private void TryFireEvent(FCEventDef def, FactionFC faction)
        {
            FCEvent evt = FCEventMaker.MakeRandomEvent(def, null);
            if (evt is null) return;

            faction.eventManager.AddEvent(evt);

            // BuildEventLetterBody resolves {FACTION} tokens (.Format()) and appends the stat-modifier
            // summary and affected-settlements list — building the body by hand left raw tokens showing.
            Find.LetterStack.ReceiveLetter(def.label, FCEventMaker.BuildEventLetterBody(evt), LetterDefOf.NeutralEvent);
            LogEE.Message("EventConditionChecker fired: " + def.defName);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastGoldenAgeTick, "lastGoldenAgeTick", -CooldownTicks);
            Scribe_Values.Look(ref lastGrowingPainsTick, "lastGrowingPainsTick", -CooldownTicks);
            Scribe_Values.Look(ref lastTributeTick, "lastTributeTick", -CooldownTicks);
            Scribe_Values.Look(ref goldenAgeQualifyingSince, "goldenAgeQualifyingSince", -1);
        }
    }
}
