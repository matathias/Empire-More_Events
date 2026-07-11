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
                        LogEE.Warning("Debug - event returned null: " + localDef.defName);
                        return;
                    }

                    // activateAtStart events (with options) open their own FCOptionWindow inside
                    // MakeRandomEvent; only queue the rest so they fire on the next ProcessEvents tick.
                    if (!localDef.activateAtStart)
                    {
                        FindFC.EventManager.AddEvent(evt);
                    }

                    Find.LetterStack.ReceiveLetter(localDef.label, FCEventMaker.BuildEventLetterBody(evt), LetterDefOf.NeutralEvent);
                    LogEE.MessageForce("Debug - triggered non-random event: " + localDef.defName);
                }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }
    }
}
