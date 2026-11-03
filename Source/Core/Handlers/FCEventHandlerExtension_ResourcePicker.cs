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
    /// When the boost event expires, clears the ImmigrantBoost comp on each targeted settlement
    /// (which removes the picker's resource modifier and the scribed choice) so nothing reapplies
    /// on the next load. Runs in OnEventExpired (the reliable expiry hook), alongside the base
    /// removal of the def's workerBaseMax modifier.
    /// </summary>
    public class FCEventHandlerExtension_ResourceBoostCleanup : FCEventHandlerExtension
    {
        public override void OnEventExpired(FCEvent evt, FactionFC faction)
        {
            base.OnEventExpired(evt, faction); // removes the def's workerBaseMax modifier

            if (evt.settlementTraitLocations is null) return;

            foreach (WorldSettlementFC settlement in evt.settlementTraitLocations)
            {
                settlement?.GetComponent<WorldObjectComp_ImmigrantBoost>()?.ClearBoost();
            }

            LogEE.Message("ResourceBoostCleanup: Cleared immigrant resource boost.");
        }
    }
}
