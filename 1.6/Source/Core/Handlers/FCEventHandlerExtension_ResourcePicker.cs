using System.Linq;
using Verse;

namespace FactionColonies.Events
{
    /// <summary>
    /// On event resolution (OnEventTriggered), opens a ResourcePickerWindow letting the
    /// player choose which resource the skilled immigrants will boost.
    ///
    /// Attached to empireEvents_immigrants_production (the 1500-tick trigger event).
    /// </summary>
    public class FCEventHandlerExtension_ResourcePicker : FCEventHandlerExtension
    {
        /// <summary>
        /// SourceId prefix used for manually-added resource modifiers.
        /// The full sourceId is this prefix + the ResourceTypeDef.defName.
        /// </summary>
        public const string ResourceSourcePrefix = "event_immigrants_resource_";

        public override void OnEventTriggered(FCEvent evt)
        {
            // Get the affected settlement
            WorldSettlementFC settlement = null;
            if (evt.settlementTraitLocations != null && evt.settlementTraitLocations.Any())
            {
                settlement = evt.settlementTraitLocations.First();
            }

            if (settlement == null)
            {
                LogEE.Warning("ResourcePicker: No settlement on event.");
                return;
            }

            Find.WindowStack.Add(new ResourcePickerWindow(settlement, evt));
        }
    }

    /// <summary>
    /// Cleanup handler attached to empireEvents_immigrants_production_boost.
    /// When the boost event resolves (600k ticks), removes the manually-added
    /// resource production modifier that was added by the ResourcePickerWindow.
    /// </summary>
    public class FCEventHandlerExtension_ResourceBoostCleanup : FCEventHandlerExtension
    {
        public override bool ResolveEvent(FCEvent evt, FactionFC faction)
        {
            if (evt.settlementTraitLocations == null) return false;

            foreach (WorldSettlementFC settlement in evt.settlementTraitLocations)
            {
                if (settlement == null) continue;

                // Remove any resource modifiers added by the ResourcePicker
                foreach (ResourceTypeDef resDef in DefDatabase<ResourceTypeDef>.AllDefsListForReading)
                {
                    string sourceId = FCEventHandlerExtension_ResourcePicker.ResourceSourcePrefix + resDef.defName;
                    settlement.RemoveStatModifiersBySource(sourceId);
                }
            }

            LogEE.Message("ResourceBoostCleanup: Cleaned up resource modifiers.");
            return false; // Let standard cleanup also run (removes workerBaseMax from def.statModifiers)
        }
    }
}
