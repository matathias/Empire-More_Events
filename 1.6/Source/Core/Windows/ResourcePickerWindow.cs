using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies.Events
{
    /// <summary>
    /// A modal window that lets the player choose which resource the skilled immigrants
    /// will boost. Styled after FCOptionWindow. Displays context (settlement, description)
    /// and a "Select Resource" button that opens a FloatMenu. Closes only after a resource
    /// is selected.
    /// </summary>
    public class ResourcePickerWindow : Window
    {
        private const float WindowWidth = 420f;
        private const float Padding = 10f;
        private const float AccentBarHeight = 4f;
        private const float ButtonHeight = 36f;

        private readonly WorldSettlementFC settlement;
        private readonly FCEvent sourceEvt;
        private readonly Color categoryColor;
        private readonly List<ResourceFC> producedResources;

        private float cachedTitleHeight;
        private float cachedDescHeight;
        private float cachedSettlementHeight;

        public override Vector2 InitialSize => new Vector2(WindowWidth, 0f);

        public ResourcePickerWindow(WorldSettlementFC settlement, FCEvent sourceEvt)
        {
            this.settlement = settlement;
            this.sourceEvt = sourceEvt;

            this.forcePause = true;
            this.draggable = true;
            this.doCloseX = false;
            this.doCloseButton = false;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.closeOnClickedOutside = false;
            this.preventCameraMotion = false;

            this.categoryColor = AccentUtil.GetEventCategoryColor(sourceEvt);

            // Build filtered resource list
            this.producedResources = new List<ResourceFC>();
            foreach (ResourceFC res in settlement.Resources)
            {
                if (res.assignedWorkers > 0 && !res.def.isPoolResource)
                {
                    producedResources.Add(res);
                }
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            MeasureLayout();
            windowRect = new Rect(
                (UI.screenWidth - WindowWidth) / 2f,
                (UI.screenHeight - windowRect.height) / 2f,
                WindowWidth,
                windowRect.height
            );
        }

        private void MeasureLayout()
        {
            float contentWidth = WindowWidth - (Margin * 2);
            float textWidth = contentWidth - (Padding * 2);

            Text.Font = GameFont.Medium;
            cachedTitleHeight = Text.CalcHeight(sourceEvt.def.label, textWidth);

            Text.Font = GameFont.Small;
            string desc = "EE_ResourcePickerDesc".Translate();
            cachedDescHeight = Text.CalcHeight(desc, textWidth);

            Text.Font = GameFont.Small;
            cachedSettlementHeight = Text.CalcHeight(settlement.Name, textWidth);

            float totalHeight = AccentBarHeight + Padding
                + cachedTitleHeight + Padding
                + cachedSettlementHeight + Padding
                + cachedDescHeight + Padding
                + ButtonHeight + Padding;

            windowRect.height = totalHeight + (Margin * 2);
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;
            Color colorBefore = GUI.color;

            float contentWidth = inRect.width;
            float textWidth = contentWidth - (Padding * 2);
            float curY = inRect.y;

            // Accent bar
            Widgets.DrawBoxSolid(new Rect(inRect.x, curY, contentWidth, AccentBarHeight), categoryColor);
            curY += AccentBarHeight + Padding;

            // Title
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(inRect.x + Padding, curY, textWidth, cachedTitleHeight), sourceEvt.def.label);
            curY += cachedTitleHeight + Padding;

            // Settlement name
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = categoryColor;
            Widgets.Label(new Rect(inRect.x + Padding, curY, textWidth, cachedSettlementHeight), settlement.Name);
            GUI.color = colorBefore;
            curY += cachedSettlementHeight + Padding;

            // Description
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            string desc = "EE_ResourcePickerDesc".Translate();
            Widgets.Label(new Rect(inRect.x + Padding, curY, textWidth, cachedDescHeight), desc);
            GUI.color = colorBefore;
            curY += cachedDescHeight + Padding;

            // Select Resource button
            Rect buttonRect = new Rect(inRect.x + Padding, curY, textWidth, ButtonHeight);
            if (producedResources.Any())
            {
                if (Widgets.ButtonText(buttonRect, "EE_SelectResource".Translate()))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (ResourceFC res in producedResources)
                    {
                        ResourceTypeDef resDef = res.def;
                        options.Add(new FloatMenuOption(resDef.LabelCap, delegate
                        {
                            ApplyResourceBoost(resDef);
                            Close();
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
            else
            {
                if (Widgets.ButtonText(buttonRect, "EE_NoActiveResources".Translate()))
                {
                    Close();
                }
            }

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
            GUI.color = colorBefore;
        }

        private void ApplyResourceBoost(ResourceTypeDef resDef)
        {
            FactionFC faction = FactionCache.FactionComp;
            if (faction == null) return;

            FCEventDef boostDef = DefDatabase<FCEventDef>.GetNamedSilentFail("empireEvents_immigrants_production_boost");
            if (boostDef == null)
            {
                LogEE.Error("ResourcePickerWindow: Could not find empireEvents_immigrants_production_boost def.");
                return;
            }

            List<WorldSettlementFC> targets = new List<WorldSettlementFC> { settlement };
            FCEvent boostEvt = FCEventMaker.MakeRandomEvent(boostDef, targets);
            if (boostEvt == null)
            {
                LogEE.Warning("ResourcePickerWindow: Failed to create boost event.");
                return;
            }

            faction.AddEvent(boostEvt);

            // Add resource-specific production bonus
            FCStatDef additiveStat = resDef.productionAdditiveStat;
            if (additiveStat != null)
            {
                string sourceId = FCEventHandlerExtension_ResourcePicker.ResourceSourcePrefix + resDef.defName;
                List<FCStatModifier> mods = new List<FCStatModifier>
                {
                    new FCStatModifier { stat = additiveStat, value = 1 }
                };
                settlement.AddStatModifiers(mods, sourceId, "EE_ImmigrantsSourceLabel".Translate(resDef.LabelCap));
            }

            Find.LetterStack.ReceiveLetter(
                "EE_ImmigrantsAssignedTitle".Translate(),
                "EE_ImmigrantsAssignedDesc".Translate(resDef.LabelCap, settlement.Name),
                LetterDefOf.PositiveEvent);

            LogEE.Message("ResourcePickerWindow: Boosted " + resDef.defName + " at " + settlement.Name);
        }
    }
}
