using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Infused
{
    public class InfusedMod : Mod
    {
        public InfusedMod(ModContentPack content) : base(content)
        {
            new Harmony("rimworld.infused").PatchAll(System.Reflection.Assembly.GetExecutingAssembly());

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
            var compProperties = new Verse.CompProperties { compClass = typeof(CompInfused) };

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

        public static IEnumerable<Def> Infuse(Thing thing, QualityCategory q, int min = 0, bool skipThingFilter = false)
        {
            IEnumerable<PoolDef> query = DefDatabase<PoolDef>.AllDefs;
            if (!skipThingFilter)
            {
                query = query.Where(p => p.Allows(thing));
            }
            if (min <= 0) {
                // Get those Infused lucky rolls
                query = query.Where(p => Rand.Value < p.Chance(q) * Settings.mult);
            } else {
                query = query.OrderBy(p => Guid.NewGuid()).Take(min);
            }

            var pools = query.ToList();

            if (pools.Count == 0) {
                yield break;
            }

            #if DEBUG
            Log.Message("Infused :: " + q + " " + thing.def.label + " got " + pools.Count + " lucky rolls");
            #endif

            var tier = RollTier(q);

            foreach (var pool in pools)
            {
                var infusions = AvailableInfusions(pool, tier, thing, skipThingFilter);
                if (infusions.Count == 0) {
                    #if DEBUG
                    Log.Warning(" > Couldn't find any infusion to give to " + q + " " + thing.def.label);
                    #endif
                    continue;
                }
                var infusion = infusions.RandomElementByWeight(i => i.weight);

                #if DEBUG
                Log.Message(" > Added " + infusion + " to " + q + " " + thing.def.label + " from " + pool.defName + "-" + infusion.tier);
                #endif

                yield return infusion;
            }
        }

        static List<Def> AvailableInfusions(PoolDef pool, InfusionTier tier, Thing thing, bool skipThingFilter = false) {
            List<Def> infusions = new List<Def>(0);
            while (tier >= 0 && infusions.Count == 0)
            {
                infusions = (
                    from def in DefDatabase<Def>.AllDefs
                    where def.pool == pool && def.tier == tier && (skipThingFilter || def.Allows(thing))
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

    [HarmonyPatch(typeof(CompQuality), nameof(CompQuality.SetQuality))]
    static class CompQuality_SetQuality_Patch
    {
        static void Postfix(CompQuality __instance, QualityCategory q, ArtGenerationContext source)
        {
            // Can we be infused?
            var compInfused = __instance.parent.TryGetComp<CompInfused>();
            if (!(compInfused?.IsActive ?? true))
            {
                var thing = __instance.parent;

                foreach(var infusion in InfusedMod.Infuse(thing, q))
                {
                    compInfused.Attach(infusion);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.DrawGUIOverlay))]
    static class Thing_DrawGUIOverlay_Patch
    {
        static void Postfix(Thing __instance) {
            if (Find.CameraDriver.CurrentZoom != CameraZoomRange.Closest)
            {
                return;
            }

            if (!CompInfused.TryGetInfusedComp(__instance, out CompInfused comp)) {
                return;
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            GUI.color = comp.InfusedLabelColor;

            string text = comp.InfusedLabelShort;
            float x = Text.CalcSize(text).x;
            var screenPos = GenMapUI.LabelDrawPosFor(__instance, -0.66f);
            var rect = new Rect(screenPos.x - x / 2f, screenPos.y - 3f, x, 999f);
            Widgets.Label(rect, text);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }
    }
}
