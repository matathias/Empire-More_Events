using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace FactionColonies.Events
{
    /// <summary>
    /// On resolution, picks 1-2 settlements NOT already affected by the plague
    /// and creates a new plague event on them (the "pray and wait" consequence).
    /// </summary>
    public class FCEventHandlerExtension_PlagueSpread : FCEventHandlerExtension
    {
        public override bool ResolveEvent(FCEvent evt, FactionFC faction)
        {
            FCEventDef plagueDef = MoreEventsDefOf.empireEvents_plague_0;
            if (plagueDef is null)
            {
                LogEE.Error("PlagueSpread: Could not find empireEvents_plague_0 def.");
                return true;
            }

            // Gather settlements already affected by plague
            HashSet<WorldSettlementFC> alreadyAffected = new HashSet<WorldSettlementFC>();
            if (evt.settlementTraitLocations != null)
            {
                foreach (WorldSettlementFC s in evt.settlementTraitLocations)
                {
                    alreadyAffected.Add(s);
                }
            }

            // Also check any other active plague events
            foreach (FCEvent active in faction.Events)
            {
                if (active.def is null) continue;
                if (active.def.defName != null && active.def.defName.StartsWith("empireEvents_plague"))
                {
                    if (active.settlementTraitLocations != null)
                    {
                        foreach (WorldSettlementFC s in active.settlementTraitLocations)
                        {
                            alreadyAffected.Add(s);
                        }
                    }
                }
            }

            // Find eligible settlements
            List<WorldSettlementFC> eligible = new List<WorldSettlementFC>();
            foreach (WorldSettlementFC s in faction.settlements)
            {
                if (!alreadyAffected.Contains(s))
                {
                    eligible.Add(s);
                }
            }

            if (!eligible.Any())
            {
                LogEE.Message("PlagueSpread: No unaffected settlements to spread to.");
                return true;
            }

            int count = Math.Min(Rand.RangeInclusive(1, 2), eligible.Count);
            List<WorldSettlementFC> targets = eligible.InRandomOrder().Take(count).ToList();

            FCEvent newEvt = FCEventMaker.MakeRandomEvent(plagueDef, targets);
            if (newEvt is null)
            {
                LogEE.Warning("PlagueSpread: Failed to create plague event.");
                return true;
            }

            faction.AddEvent(newEvt);

            string names = string.Join("\n", targets.Select(s => " " + s.Name));
            Find.LetterStack.ReceiveLetter(
                "EE_PlagueSpreadsTitle".Translate(),
                "EE_PlagueSpreadsDesc".Translate("FCEventAffectingSettlements".Translate(), names),
                LetterDefOf.ThreatSmall);

            LogEE.Message("PlagueSpread: Plague spread to " + count + " new settlement(s).");
            return true;
        }
    }
}
