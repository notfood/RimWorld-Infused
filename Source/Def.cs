using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Infused
{
    public enum InfusionTier
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Artifact
    }

    public class TechLevelRange
    {
        public TechLevel min = TechLevel.Undefined;
        public TechLevel max = TechLevel.Ultra;
    }

    public class InfusionAllowance
    {
        public bool melee = true;
        public bool ranged = true;
        public bool apparel = true;
    }

    public class Def : Verse.Def
    {

        public string labelShort = "#NN";
        public bool labelReversed;

        public InfusionTier tier;

        public Dictionary<StatDef, StatMod> stats = new Dictionary<StatDef, StatMod>();

        public PoolDef pool;
        public int weight = 1;

        public InfusionAllowance allowance = new InfusionAllowance();

        public TechLevelRange techLevel = new TechLevelRange();

        public ThingFilter match;

        /// Get matching StatMod for given StatDef. Returns false when none.
        public bool TryGetStatValue(StatDef stat, out StatMod mod)
        {
            return stats.TryGetValue(stat, out mod);
        }

        public override void ResolveReferences()
        {
            // search if we already added the StatPart
            Predicate<StatPart> predicate = (StatPart part) => part.GetType() == typeof(StatPart_InfusedModifier);

            foreach (StatDef statDef in stats.Keys)
            {
                if (statDef.parts == null)
                {
                    statDef.parts = new List<StatPart>(1);
                }
                else if (statDef.parts.Any(predicate))
                {
                    continue;
                }

                statDef.parts.Add(new StatPart_InfusedModifier(statDef));
            }

            match?.ResolveReferences();
        }

        public bool Allows(Thing thing)
        {
            return MatchThingTech(thing.def.techLevel)
                && MatchThingType(thing.def)
                && (match?.Allows(thing) ?? true);
        }

        bool MatchThingTech(TechLevel tech)
        {
            return tech >= techLevel.min
                && tech <= techLevel.max;
        }

        bool MatchThingType(ThingDef def)
        {
            return def.IsMeleeWeapon && allowance.melee
                || def.IsRangedWeapon && allowance.ranged
                || def.IsApparel && allowance.apparel;
        }

        public class StatMod
        {
            public float offset;
            public float multiplier = 1;

            public override string ToString()
            {
                return string.Format("[StatMod offset={0}, multiplier={1}]", offset, multiplier);
            }
        }
    }

    public class PoolDef : Verse.Def
    {

        public QualityChances chances;

        [Unsaved]
        InfusionAllowance allowance = new InfusionAllowance() {
            apparel = false,
            melee = false,
            ranged = false
        };

        [Unsaved]
        TechLevelRange techLevel = new TechLevelRange()
        {
            min = TechLevel.Ultra,
            max = TechLevel.Undefined
        };

        public float Chance(QualityCategory qc)
        {
            switch (qc)
            {
                case QualityCategory.Awful:
                    return chances.awful;
                case QualityCategory.Poor:
                    return chances.poor;
                case QualityCategory.Normal:
                    return chances.normal;
                case QualityCategory.Good:
                    return chances.good;
                case QualityCategory.Excellent:
                    return chances.excellent;
                case QualityCategory.Masterwork:
                    return chances.masterwork;
                case QualityCategory.Legendary:
                    return chances.legendary;
                default:
                    return 0f;
            }
        }

        public struct QualityChances
        {
            public float awful;
            public float poor;
            public float normal;
            public float good;
            public float excellent;
            public float masterwork;
            public float legendary;
        }

        public bool Allows(Thing thing)
        {
            return MatchThingTech(thing.def.techLevel)
                && MatchThingType(thing.def);
        }

        bool MatchThingTech(TechLevel tech)
        {
            return tech >= techLevel.min
                && tech <= techLevel.max;
        }

        bool MatchThingType(ThingDef def)
        {
            return def.IsMeleeWeapon && allowance.melee
                || def.IsRangedWeapon && allowance.ranged
                || def.IsApparel && allowance.apparel;
        }

        public override void ResolveReferences()
        {
            var defs = (
                from def in DefDatabase<Def>.AllDefs
                where def.pool == this
                select def
            );

            foreach (var def in defs)
            {
                allowance.apparel |= def.allowance.apparel;
                allowance.melee |= def.allowance.melee;
                allowance.ranged |= def.allowance.ranged;

                if (techLevel.min > def.techLevel.min)
                    techLevel.min = def.techLevel.min;
                if (techLevel.max < def.techLevel.max)
                    techLevel.max = def.techLevel.max;
            }

            #if DEBUG
            Log.Message("Infused :: Pool " + defName + " allows: from " + techLevel.min + " to " + techLevel.max + "\nApparel=" + allowance.apparel + "\nMelee=" + allowance.melee + "\nRanged=" + allowance.ranged);
            #endif
        }
     }
}
