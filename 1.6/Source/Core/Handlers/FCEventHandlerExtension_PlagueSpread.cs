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
            FCEventDef plagueDef = DefDatabase<FCEventDef>.GetNamedSilentFail("empireEvents_plague_medicine");
            if (plagueDef == null)
            {
                LogUtil.Error("PlagueSpread: Could not find empireEvents_plague_medicine def.");
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
            foreach (FCEvent active in faction.events)
            {
                if (active.def == null) continue;
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
                LogUtil.Message("PlagueSpread: No unaffected settlements to spread to.");
                return true;
            }

            int count = Math.Min(Rand.RangeInclusive(1, 2), eligible.Count);
            List<WorldSettlementFC> targets = eligible.InRandomOrder().Take(count).ToList();

            FCEvent newEvt = FCEventMaker.MakeRandomEvent(plagueDef, targets);
            if (newEvt == null)
            {
                LogUtil.Warning("PlagueSpread: Failed to create plague event.");
                return true;
            }

            faction.AddEvent(newEvt);

            string names = string.Join("\n", targets.Select(s => " " + s.Name));
            Find.LetterStack.ReceiveLetter(
                "Plague Spreads",
                "The plague has spread to new settlements!\n" + "EventAffectingSettlements".Translate() + "\n" + names,
                LetterDefOf.ThreatSmall);

            LogUtil.Message("PlagueSpread: Plague spread to " + count + " new settlement(s).");
            return true;
        }
    }
}
