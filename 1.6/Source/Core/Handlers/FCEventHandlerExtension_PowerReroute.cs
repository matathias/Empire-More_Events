using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace FactionColonies.Events
{
    /// <summary>
    /// Attached to the reroute trigger event (1500 ticks). On resolution, creates:
    /// 1. A "reroute main" event with -1 Power on the original affected settlements (180k ticks)
    /// 2. A "reroute drain" event with -1 Power on 1-2 OTHER settlements (180k ticks)
    /// </summary>
    public class FCEventHandlerExtension_PowerReroute : FCEventHandlerExtension
    {
        public override bool ResolveEvent(FCEvent evt, FactionFC faction)
        {
            // Create the main penalty event on the originally affected settlements
            FCEventDef mainDef = MoreEventsDefOf.empireEvents_powerFailure_reroute_main;
            if (mainDef is null)
            {
                LogEE.Error("PowerReroute: Could not find empireEvents_powerFailure_reroute_main def.");
                return true;
            }

            FCEvent mainEvt = FCEventMaker.MakeRandomEvent(mainDef, evt.settlementTraitLocations);
            if (mainEvt != null)
            {
                faction.AddEvent(mainEvt);
            }

            // Find settlements NOT already affected
            FCEventDef drainDef = MoreEventsDefOf.empireEvents_powerFailure_reroute_drain;
            if (drainDef is null)
            {
                LogEE.Error("PowerReroute: Could not find empireEvents_powerFailure_reroute_drain def.");
                return true;
            }

            HashSet<WorldSettlementFC> excluded = new HashSet<WorldSettlementFC>();
            if (evt.settlementTraitLocations != null)
            {
                foreach (WorldSettlementFC s in evt.settlementTraitLocations)
                {
                    excluded.Add(s);
                }
            }

            List<WorldSettlementFC> eligible = new List<WorldSettlementFC>();
            foreach (WorldSettlementFC s in faction.settlements)
            {
                if (!excluded.Contains(s))
                {
                    eligible.Add(s);
                }
            }

            if (!eligible.Any())
            {
                LogEE.Message("PowerReroute: No other settlements to reroute power from.");
                return true;
            }

            int count = Math.Min(Rand.RangeInclusive(1, 2), eligible.Count);
            List<WorldSettlementFC> targets = eligible.InRandomOrder().Take(count).ToList();

            FCEvent drainEvt = FCEventMaker.MakeRandomEvent(drainDef, targets);
            if (drainEvt is null)
            {
                LogEE.Warning("PowerReroute: Failed to create drain event.");
                return true;
            }

            faction.AddEvent(drainEvt);

            string mainNames = evt.settlementTraitLocations != null
                ? string.Join("\n", evt.settlementTraitLocations.Select(s => " " + s.Name))
                : "";
            string drainNames = string.Join("\n", targets.Select(s => " " + s.Name));

            Find.LetterStack.ReceiveLetter(
                "EE_PowerReroutedTitle".Translate(),
                "EE_PowerReroutedDesc".Translate("EventAffectingSettlements".Translate(), drainNames),
                LetterDefOf.NeutralEvent);

            LogEE.Message("PowerReroute: Created main + drain events. Drain on " + count + " settlement(s).");
            return true;
        }
    }
}
