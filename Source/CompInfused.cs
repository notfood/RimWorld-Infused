using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Infused
{
    public class CompInfused : ThingComp
    {
        bool isNew;
        List<Def> infusions;

        string infusedLabelShort;
        Color infusedLabelColor;

        public IEnumerable<Def> Infusions => infusions ?? Enumerable.Empty<Def>();

        public string InfusedLabelShort => infusedLabelShort;

        public Color InfusedLabelColor => infusedLabelColor;

        public bool IsActive => !infusions.NullOrEmpty();

        public void Attach(Def def) {
            if (infusions == null) {
                infusions = new List<Def>();
                isNew = true;
            }
            infusions.Add(def);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (IsActive)
            {
                var maxtier = InfusionTier.Common;
                var infusedLabelBuilder = new StringBuilder();
                foreach(var infusion in infusions) {
                    if (infusion.tier > maxtier) {
                        maxtier = infusion.tier;
                    }
                    if (infusedLabelBuilder.Length > 0)
                    {
                        infusedLabelBuilder.Append(" ");
                    }
                    infusedLabelBuilder.Append(infusion.labelShort);
                }
                infusedLabelShort = infusedLabelBuilder.ToString();
                infusedLabelColor = ResourceBank.Colors.InfusionColor(maxtier);

                // We only throw notifications for newly spawned items.
                if (isNew)
                    ThrowMote();
            }
        }

        void ThrowMote()
        {
            CompQuality compQuality = parent.TryGetComp<CompQuality>();
            if (compQuality == null)
            {
                return;
            }
            string qualityLabel = compQuality.Quality.GetLabel();

            var msg = new StringBuilder();
            msg.Append(qualityLabel);
            msg.Append(" ");
            if (parent.Stuff != null)
            {
                msg.Append(parent.Stuff.LabelAsStuff);
                msg.Append(" ");
            }
            msg.Append(parent.def.label);
            Messages.Message(ResourceBank.Strings.Notice(msg.ToString()), new RimWorld.Planet.GlobalTargetInfo(parent), MessageTypeDefOf.SilentInput);

            ResourceBank.Sounds.Infused.PlayOneShotOnCamera();

            MoteMaker.ThrowText(parent.Position.ToVector3Shifted(), this.parent.Map, ResourceBank.Strings.Mote, ResourceBank.Colors.Legendary);

            isNew = false;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            if (Scribe.mode == LoadSaveMode.Saving) {
                // let's not bloat saves...
                if (infusions != null) {
                    if (infusions.Count > 1) {
                        infusions = infusions.OrderBy(i => i.labelReversed).ToList();
                    }
                    Scribe_Collections.Look(ref infusions, "infusions", LookMode.Def);
                }
                    
            } else if (Scribe.mode == LoadSaveMode.LoadingVars) {

                // Easy loading
                Scribe_Collections.Look(ref infusions, "infusions", LookMode.Def);

                // Loading legacy save...
                string prefixDefName = string.Empty;
                string suffixDefName = string.Empty;

                Scribe_Values.Look(ref prefixDefName, "prefix");
                Scribe_Values.Look(ref suffixDefName, "suffix");

                if (!prefixDefName.NullOrEmpty())
                {
                    var prefix = DefDatabase<Def>.GetNamedSilentFail(prefixDefName);
                    if (prefix != null)
                        Attach(prefix);
                }
                if (!suffixDefName.NullOrEmpty())
                {
                    
                    var suffix = DefDatabase<Def>.GetNamedSilentFail(suffixDefName);
                    if (suffix != null)
                        Attach(suffix);
                }

                isNew = false;
            }
        }

        public override bool AllowStackWith(Thing other)
        {
            return false;
        }

        public override string TransformLabel(string label)
        {
            // When this function is called, our infusion is no longer new.
            isNew = false;

            if (IsActive) {
                return GetInfusionLabel();
            }

            return base.TransformLabel(label);
        }

        public string GetInfusionLabel(bool shorten = true)
        {
            var result = new StringBuilder();

            string label = parent.def.label;
            foreach (var infusion in infusions)
            {
                result.Length = 0;
                if (infusion.labelReversed)
                {
                    result.Append(label);
                    result.Append(" ");
                    result.Append(infusion.label);
                    label = result.ToString();
                }
                else
                {
                    result.Append(infusion.label);
                    result.Append(" ");
                    result.Append(label);
                    label = result.ToString();
                }
            }

            result.Length = 0;
            if (parent.Stuff != null)
            {
                result.Append(parent.Stuff.LabelAsStuff);
                result.Append(" ");
            }
            result.Append(label);
            result.Append(" (");

            if (parent.TryGetQuality(out QualityCategory qc))
            {
                if (shorten && result.Length > 20)
                {
                    result.Append(qc.GetLabelShort());
                }
                else
                {
                    result.Append(qc.GetLabel());
                }
            }

            if ((!shorten || result.Length <= 30) && parent.HitPoints < parent.MaxHitPoints)
            {
                result.Append(" ");
                result.Append(((float)parent.HitPoints / parent.MaxHitPoints).ToStringPercent());
            }

            result.Append(")");

            return result.ToString();
        }

        public override string GetDescriptionPart()
        {
            if (IsActive) {
                return base.GetDescriptionPart() + "\n" + GetDescriptionInfused();
            }

            return base.GetDescriptionPart();
        }

        //Make a new infusion stat information.
        public string GetDescriptionInfused()
        {
            var result = new StringBuilder(null);
            foreach (var infusion in infusions) {
                result.Append(infusion.label)
                      .Append(" (")
                      .Append(ResourceBank.Strings.Tier(infusion.tier))
                      .AppendLine(")");
                foreach (var current in infusion.stats)
                {
                    if (Math.Abs(current.Value.offset) > 0.0001f)
                    {
                        result.Append("     " + (current.Value.offset > 0 ? "+" : "-"));
                        if (current.Key == StatDefOf.ComfyTemperatureMax || current.Key == StatDefOf.ComfyTemperatureMin)
                        {
                            result.Append(Mathf.Abs(current.Value.offset).ToStringTemperatureOffset());
                        }
                        else if (current.Key.parts.Find(s => s is StatPart_InfusedModifier) is var modifier)
                        {
                            result.Append(modifier.parentStat.ValueToString(Mathf.Abs(current.Value.offset)));
                        }
                        result.AppendLine(" " + current.Key.LabelCap);
                    }
                    if (Math.Abs(current.Value.multiplier - 1) > 0.0001f)
                    {
                        result.Append("     " + Mathf.Abs(current.Value.multiplier).ToStringPercent());
                        result.AppendLine(" " + current.Key.LabelCap);
                    }
                }
                result.AppendLine();

            }
            return result.ToString();
        }

        public static bool TryGetInfusedComp(ThingWithComps thing, out CompInfused comp)
        {
            comp = thing.GetComp<CompInfused>();
            return comp != null && comp.IsActive;
        }

        public static bool TryGetInfusedComp(Thing thing, out CompInfused comp)
        {
            comp = thing.TryGetComp<CompInfused>();
            return comp != null && comp.IsActive;
        }
    }
}
