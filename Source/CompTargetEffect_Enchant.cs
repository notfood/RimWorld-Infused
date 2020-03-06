using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace Infused
{
    public class CompTargetEffect_Enchant : CompTargetEffect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            var infused = target.TryGetComp<CompInfused>();
            if (!infused.IsActive)
            {
                float hp = target.HitPoints / target.MaxHitPoints;
                var toTranfer = parent.GetComp<CompInfused>();
                foreach(Def infusion in toTranfer.Infusions)
                {
                    infused.Attach(infusion);
                }
                target.HitPoints = Mathf.FloorToInt(target.MaxHitPoints * hp);
                infused.Setup();
            }
        }
    }
}
