using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Fomites
{
    /// <summary>
    /// XML properties for the Fomite Contagion component.
    /// Configures how frequently an infected pawn contaminates their current equipment and furniture.
    /// </summary>
    public class HediffCompProperties_FomiteContagion : HediffCompProperties
    {
        /// <summary>Frequency (in ticks) at which the pawn attempts to contaminate their surroundings. Default 2500 is roughly 1 game hour.</summary>
        public int contaminateInterval = 2500;

        /// <summary>Whether to notify the player when an object becomes a fomite (usually kept false for immersion).</summary>
        public bool sendNotification = true;

        /// <summary>If true, uses a Letter; otherwise, a Message.</summary>
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_FomiteContagion()
        {
            this.compClass = typeof(HediffComp_FomiteContagion);
        }
    }

    /// <summary>
    /// Active Hediff component that spreads the disease to inanimate objects.
    /// It scans the pawn's worn apparel and current bed to apply the 'Contaminated' status.
    /// </summary>
    public class HediffComp_FomiteContagion : HediffComp
    {
        public HediffCompProperties_FomiteContagion Props => (HediffCompProperties_FomiteContagion)this.props;

        /// <summary>
        /// Executed every tick. Checks the interval to perform contamination logic.
        /// </summary>
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn == null || !Pawn.Spawned || Pawn.Dead) return;

            // Performance-friendly check using hash intervals.
            if (Pawn.IsHashIntervalTick(Props.contaminateInterval))
            {
                ContaminateSurroundings();
            }
        }

        /// <summary>
        /// Identifies nearby or worn objects with a CompFomite and transfers the pathogen to them.
        /// </summary>
        private void ContaminateSurroundings()
        {
            // Vector 1: Worn Apparel.
            // If the pawn is sweating or bleeding into their clothes, the clothes become infectious.
            if (Pawn.apparel != null)
            {
                foreach (Apparel apparel in Pawn.apparel.WornApparel)
                {
                    CompFomite fomiteComp = apparel.TryGetComp<CompFomite>();
                    if (fomiteComp != null)
                    {
                        fomiteComp.Contaminate(this.parent.def);
                    }
                }
            }

            // Vector 2: Bedding.
            // If the pawn is resting, the bed becomes a primary source of indirect infection.
            if (Pawn.InBed())
            {
                Building_Bed bed = Pawn.CurrentBed();
                if (bed != null)
                {
                    CompFomite fomiteComp = bed.TryGetComp<CompFomite>();
                    if (fomiteComp != null)
                    {
                        fomiteComp.Contaminate(this.parent.def);
                    }
                }
            }
        }
    }
}