using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Infused
{
    public class CompTargetable_WeaponOrApparel : CompTargetable
    {
        readonly InfusionAllowance allowance = new InfusionAllowance
        {
            apparel = false,
            melee = false,
            ranged = false
        };

        protected override bool PlayerChoosesTarget => true;

        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            yield return targetChosenByPlayer;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            var comp = parent.GetComp<CompInfused>();
            if (comp == null)
            {
                return;
            }

            foreach (var infusion in comp.Infusions)
            {
                allowance.apparel |= infusion.allowance.apparel;
                allowance.melee   |= infusion.allowance.melee;
                allowance.ranged  |= infusion.allowance.ranged;
            }
        }

        protected override TargetingParameters GetTargetingParameters()
        {
            return new TargetingParameters
            {
                canTargetPawns = false,
                canTargetBuildings = false,
                canTargetAnimals = false,
                canTargetHumans = false,
                canTargetMechs = false,
                mapObjectTargetsMustBeAutoAttackable = false,

                canTargetItems = true,
                mustBeSelectable = true,

                validator = Validate
            };

            bool Validate(TargetInfo x)
            {
                if (x.Thing?.TryGetComp<CompInfused>()?.IsActive ?? true)
                {
                    return false;
                }

                var def = x.Thing.def;

                if (allowance.apparel && def.IsApparel)
                {
                    return true;
                }

                if (!def.IsWeapon || def.IsIngestible)
                {
                    return false;
                }
                if (allowance.ranged && def.IsRangedWeapon)
                {
                    return true;
                }
                if (allowance.melee && def.IsMeleeWeapon)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
