using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies.Events
{
    public class WorldObjectCompProperties_ImmigrantBoost : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_ImmigrantBoost()
        {
            compClass = typeof(WorldObjectComp_ImmigrantBoost);
        }
    }

    /// <summary>
    /// Tracks the resource chosen for a Skilled Immigrants production boost so the chosen
    /// +1 productionAdditive modifier survives save/reload.
    ///
    /// The settlement's transient statModifiers list is not serialized: WorldSettlementFC.PostLoadInit
    /// rebuilds it from buildings, settlement type, and active events' def.statModifiers only. The
    /// picker's per-resource choice lives in none of those, so it was silently dropped on load. This
    /// comp scribes the chosen ResourceTypeDef and reapplies the modifier via ISettlementPostLoadInit,
    /// which runs after PostLoadInit finishes rebuilding the modifiers.
    ///
    /// At most one boost is active per settlement at a time (the base prevents two concurrent
    /// immigrant boosts on one settlement), so a single scribed reference suffices.
    /// </summary>
    public class WorldObjectComp_ImmigrantBoost : WorldObjectComp, ISettlementPostLoadInit
    {
        private ResourceTypeDef boostedResource;

        private WorldSettlementFC Settlement => parent as WorldSettlementFC;

        private string SourceId =>
            FCEventHandlerExtension_ResourcePicker.ResourceSourcePrefix + boostedResource.defName;

        /// <summary>
        /// Records the chosen resource and applies its +1 productionAdditive modifier. Called by the
        /// ResourcePickerWindow when the player picks; the single source of truth for applying the boost.
        /// </summary>
        public void SetBoost(ResourceTypeDef res)
        {
            boostedResource = res;
            ApplyModifier();
        }

        /// <summary>
        /// Removes the applied modifier and clears the scribed choice. Called by the boost event's
        /// cleanup handler on expiry so no stale boost reapplies on the next load.
        /// </summary>
        public void ClearBoost()
        {
            if (boostedResource is null) return;

            Settlement?.RemoveStatModifiersBySource(SourceId);
            boostedResource = null;
        }

        private void ApplyModifier()
        {
            if (boostedResource is null || Settlement is null) return;

            FCStatDef additiveStat = boostedResource.productionAdditiveStat;
            if (additiveStat is null) return;

            List<FCStatModifier> mods = new List<FCStatModifier>
            {
                new FCStatModifier { stat = additiveStat, value = 1 }
            };
            Settlement.AddStatModifiers(mods, SourceId, "EE_ImmigrantsSourceLabel".Translate(boostedResource.LabelCap));
        }

        /* Serialization */

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref boostedResource, "boostedResource");
        }

        /* ISettlementPostLoadInit: reapply after PostLoadInit rebuilds the transient modifiers */

        public void PostSettlementLoadInit(WorldSettlementFC settlement)
        {
            if (boostedResource is object)
                ApplyModifier();
        }
    }
}
