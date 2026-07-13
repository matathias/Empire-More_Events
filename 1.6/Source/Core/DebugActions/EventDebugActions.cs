using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace FactionColonies.Events
{
    public static class EventDebugActions
    {
        /* Lists every non-random More Events event (the condition-triggered roots like Golden Age,
         * Growing Pains, and Tribute Demand, plus chain follow-ups) and spawns the chosen one on
         * demand. The base mod's "Make Random Event" only lists isRandomEvent defs, and the "Force
         * Trigger" actions only bump events already queued, so there is otherwise no way to spawn
         * these from scratch for testing. */
        [DebugAction("Empire - More Events", "Trigger Non-Random Event", allowedGameStates = AllowedGameStates.Playing)]
        private static void TriggerNonRandomEvent()
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (FCEventDef def in DefDatabase<FCEventDef>.AllDefsListForReading
                         .Where(d => d.defName.StartsWith("empireEvents_") && !d.isRandomEvent)
                         .OrderBy(d => d.label))
            {
                FCEventDef localDef = def;
                list.Add(new DebugMenuOption($"{localDef.label} ({localDef.defName})", DebugMenuOptionMode.Action, delegate
                {
                    FCEvent evt = FCEventMaker.MakeRandomEvent(localDef, null);
                    if (evt is null)
                    {
                        // activateAtStart+options defs open their own FCOptionWindow inside
                        // MakeRandomEvent and return null by design — that's success, not failure.
                        if (localDef.activateAtStart && localDef.options.Count > 0)
                            LogEE.MessageForce("Debug - opened option window for: " + localDef.defName);
                        else
                            LogEE.Warning("Debug - event returned null: " + localDef.defName);
                        return;
                    }

                    // A non-null event is always a queued (non-activateAtStart) one; fire it on the
                    // next ProcessEvents tick and surface the letter.
                    FindFC.EventManager.AddEvent(evt);
                    Find.LetterStack.ReceiveLetter(localDef.label, FCEventMaker.BuildEventLetterBody(evt), LetterDefOf.NeutralEvent);
                    LogEE.MessageForce("Debug - triggered non-random event: " + localDef.defName);
                }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }
    }
}
