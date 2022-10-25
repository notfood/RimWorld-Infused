using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Infused
{
    public class CompTargetable_WeaponOrApparel : CompTargetable
    {
        InfusionAllowance allowance = new InfusionAllowance();

        bool stealsInfusions;

        protected override bool PlayerChoosesTarget => true;

        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            yield return targetChosenByPlayer;
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            return base.CompFloatMenuOptions(selPawn);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            var comp = parent.GetComp<CompInfused>();
            if (comp == null || comp.InfusionCount == 0)
            {
                stealsInfusions = true;

                return;
            }

            foreach (var infusion in comp.Infusions)
            {
                var filter = infusion.filter;

                if (filter == null)
                {
                    continue;
                }

                allowance.Merge(filter.allowance);
            }
        }

        protected override TargetingParameters GetTargetingParameters()
        {
            return new TargetingParameters
            {
                canTargetPawns = false,
                canTargetAnimals = false,
                canTargetHumans = false,
                canTargetMechs = false,
                mapObjectTargetsMustBeAutoAttackable = false,

                canTargetItems = true,
                canTargetBuildings = true,

                mustBeSelectable = true,

                validator = Validate
            };

            bool Validate(TargetInfo x)
            {
                var thing = x.Thing;
                if (thing == null)
                {
                    return false;
                }

                if (thing.TryGetComp<CompQuality>() == null)
                {
                    return false;
                }

                int infusionCount = thing.TryGetComp<CompInfused>()?.InfusionCount ?? 0;

                if (stealsInfusions && infusionCount > 0)
                {
                    return true;
                }

                if (infusionCount >= Settings.max )
                {
                    return false;
                }

                var def = x.Thing.def;

                if (allowance.Allows(def))
                {
                    return true;
                }

                return false;
            }
        }

        string cachedInspectStringExtra;
        public override string CompInspectStringExtra()
        {
            if (cachedInspectStringExtra == null && !stealsInfusions)
            {
                var sb = new System.Text.StringBuilder();

                bool comma = false;
                void Comma()
                {
                    if (comma)
                    {
                        sb.Append(", ");
                    }
                    else
                    {
                        comma = true;
                    }
                }

                sb.Append(ResourceBank.Strings.Allows);
                sb.Append(": ");
                if (allowance.apparel)
                {
                    Comma();
                    sb.Append(ResourceBank.Strings.AllowsApparel);
                }
                if (allowance.melee)
                {
                    Comma();
                    sb.Append(ResourceBank.Strings.AllowsMelee);
                }
                if (allowance.ranged)
                {
                    Comma();
                    sb.Append(ResourceBank.Strings.AllowsRanged);
                }
                if (allowance.furniture)
                {
                    Comma();
                    sb.Append(ResourceBank.Strings.AllowsFurniture);
                }

                cachedInspectStringExtra = sb.ToString();
            }
            return cachedInspectStringExtra;
        }

        string cachedDescriptionPart;
        public override string GetDescriptionPart()
        {
            if (cachedDescriptionPart == null && !stealsInfusions)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append(ResourceBank.Strings.Allows);
                sb.Append(":  ");
                if (allowance.apparel)
                {
                    sb.Append(ResourceBank.Strings.AllowsApparel.CapitalizeFirst());
                }
                if (allowance.melee)
                {
                    sb.Append(ResourceBank.Strings.AllowsMelee.CapitalizeFirst());
                }
                if (allowance.ranged)
                {
                    sb.Append(ResourceBank.Strings.AllowsRanged.CapitalizeFirst());
                }
                if (allowance.furniture)
                {
                    sb.Append(ResourceBank.Strings.AllowsFurniture.CapitalizeFirst());
                }

                cachedDescriptionPart = sb.ToString();
            }
            return cachedDescriptionPart;

        }
    }
}
