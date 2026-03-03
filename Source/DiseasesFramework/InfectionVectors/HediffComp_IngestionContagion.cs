using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors
{
    public class HediffCompProperties_IngestionContagion : HediffCompProperties
    {
        public float contaminationChance = 1.0f;
        public HediffDef hediffToApply = null;

        public HediffCompProperties_IngestionContagion()
        {
            this.compClass = typeof(HediffComp_IngestionContagion);
        }
    }

    public class HediffComp_IngestionContagion : HediffComp
    {
        public HediffCompProperties_IngestionContagion Props => (HediffCompProperties_IngestionContagion)this.props;
    }
}