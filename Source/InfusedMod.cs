using System;
using System.Collections.Generic;
using System.Linq;

using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace Infused
{
    public class InfusedMod : Mod
    {
        public InfusedMod(ModContentPack content) : base(content)
        {
            HarmonyInstance.Create("rimworld.infused").PatchAll(System.Reflection.Assembly.GetExecutingAssembly());

            LongEventHandler.ExecuteWhenFinished(Inject);

            GetSettings<Settings>();
        }

        public override string SettingsCategory() { return ResourceBank.Strings.Infused; }
        public override void DoSettingsWindowContents(Rect inRect) => Settings.DoSettingsWindowContents(inRect);

        static void Inject() {
            var defs = (
                from def in DefDatabase<ThingDef>.AllDefs
                where (def.IsApparel || def.IsWeapon)
                && def.HasComp(typeof(CompQuality)) && !def.HasComp(typeof(CompInfused))
                select def
            ).ToList();

            var tabType = typeof(ITab_Infused);
            var tab = InspectTabManager.GetSharedInstance(tabType);
            var compProperties = new CompProperties { compClass = typeof(CompInfused) };

            foreach (var def in defs)
            {
                def.comps.Add(compProperties);
                #if DEBUG
                Log.Message("Infused :: Component added to " + def.label);
                #endif

                if (def.inspectorTabs == null || def.inspectorTabs.Count == 0)
                {
                    def.inspectorTabs = new List<Type>();
                    def.inspectorTabsResolved = new List<InspectTabBase>();
                }

                def.inspectorTabs.Add(tabType);
                def.inspectorTabsResolved.Add(tab);
            }
            #if DEBUG
            Log.Message("Infused :: Injected " + defs.Count + "/" + DefDatabase<ThingDef>.AllDefs.Count());
            #endif
        }
    }

    [HarmonyPatch(typeof(CompQuality))]
    [HarmonyPatch(nameof(CompQuality.SetQuality))]
    static class CompQuality_SetQuality_Patch
    {
        static void Postfix(CompQuality __instance, QualityCategory q, ArtGenerationContext source)
        {
            // Can we be infused?
            var compInfused = __instance.parent.TryGetComp<CompInfused>();
            if (compInfused != null)
            {
                var thing = __instance.parent;

                // Get those Infused lucky rolls
                var pools = (
                    from pool in DefDatabase<PoolDef>.AllDefs
                    where pool.Allows(thing) && Rand.Value < pool.Chance(q) * Settings.mult
                    select pool
                ).ToList();

                if (pools.Count == 0) {
                    return;
                }

                #if DEBUG
                Log.Message("Infused :: " + q + " " + thing.def.label + " got " + pools.Count + " lucky rolls");
                #endif

                var tier = RollTier(q);

                foreach (var pool in pools)
                {
                    var infusions = AvailableInfusions(pool, tier, thing);
                    if (infusions.Count == 0) {
                        #if DEBUG
                        Log.Warning(" > Couldn't find any infusion to give to " + q + " " + thing.def.label);
                        #endif
                        return;
                    }
                    var infusion = infusions.RandomElementByWeight(i => i.weight);

                    #if DEBUG
                    Log.Message(" > Added " + infusion + " to " + q + " " + thing.def.label + " from " + pool.defName + "-" + infusion.tier);
                    #endif

                    compInfused.Attach(infusion);
                }

                if (compInfused.IsActive)
                {
                    __instance.parent.HitPoints = __instance.parent.MaxHitPoints;
                }
            }
        }

        static List<Def> AvailableInfusions(PoolDef pool, InfusionTier tier, Thing thing) {
            List<Def> infusions = new List<Def>(0);
            while (tier >= 0 && infusions.Count == 0)
            {
                infusions = (
                    from def in DefDatabase<Def>.AllDefs
                    where def.pool == pool && def.tier == tier && def.Allows(thing)
                    select def
                ).ToList();

                if (infusions.Count == 0)
                {
                    #if DEBUG
                    Log.Warning(" > No " + tier + " infusions for " + pool.defName);
                    #endif
                    tier--;
                }
            }
            return infusions;
        }

        static InfusionTier RollTier(QualityCategory q)
        {
            float roll = Mathf.Pow(Rand.Value, 1 + (float) q * Settings.bias);
            #if DEBUG
            Log.Message(" > rolled: " + roll + " for " + q);
            #endif
            if (roll < 0.03175f)
            {
                return InfusionTier.Artifact;
            }
            if (roll < 0.0625f)
            {
                return InfusionTier.Legendary;
            }
            if (roll < 0.125f)
            {
                return InfusionTier.Epic;
            }
            if (roll < 0.25f)
            {
                return InfusionTier.Rare;
            }
            if (roll < 0.50f)
            {
                return InfusionTier.Uncommon;
            }
            return InfusionTier.Common;
        }
    }

    [HarmonyPatch(typeof(GenMapUI))]
    [HarmonyPatch(nameof(GenMapUI.DrawThingLabel))]
    [HarmonyPatch(new Type[] { typeof(Thing), typeof(string), typeof(Color) })]
    static class GenMapUI_DrawThingLabel_Patch
    {
        static void Postfix(Thing thing) {
            if (!CompInfused.TryGetInfusedComp(thing, out CompInfused comp)) {
                return;
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            GUI.color = comp.InfusedLabelColor;

            string text = comp.InfusedLabelShort;
            float x = Text.CalcSize(text).x;
            var screenPos = GenMapUI.LabelDrawPosFor(thing, -0.66f);
            var rect = new Rect(screenPos.x - x / 2f, screenPos.y - 3f, x, 999f);
            Widgets.Label(rect, text);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }
    }
}
