using Verse;
using RimWorld;

namespace Infused
{
    public class CompProperties_Enchant : CompProperties
    {
        public QualityCategory quality;

        public IntRange count;

        public CompProperties_Enchant()
        {
            compClass = typeof(CompInfused);
        }
    }
}
