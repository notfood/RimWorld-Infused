using System;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Infused
{
    public class CompProperties_Enchant : CompProperties
    {
        public QualityCategory quality;
        public List<PoolDef> pools;

        public CompProperties_Enchant()
        {
            compClass = typeof(CompInfused);
        }
    }
}
