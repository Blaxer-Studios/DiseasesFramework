using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Fomites
{
    /// <summary>
    /// XML properties for the Fomite component.
    /// Defines how long an object remains contaminated before the pathogen naturally decays.
    /// </summary>
    public class CompProperties_Fomite : CompProperties
    {
        /// <summary>Number of in-game days until the contamination automatically clears (decays).</summary>
        public float daysToDecay = 2f;

        public CompProperties_Fomite()
        {
            this.compClass = typeof(CompFomite);
        }
    }

    /// <summary>
    /// Component that allows an object (Thing) to hold and preserve a pathogen.
    /// Objects with this component can be contaminated by sick pawns and later infect healthy ones.
    /// </summary>
    public class CompFomite : ThingComp
    {
        public CompProperties_Fomite Props => (CompProperties_Fomite)this.props;

        private HediffDef activeDisease = null;
        private int tickContaminated = -1;

        /// <summary>Gets the current disease contaminating this object.</summary>
        public HediffDef ActiveDisease => activeDisease;

        /// <summary>
        /// Handles the saving and loading of contamination data.
        /// Ensures that contaminated objects remain dangerous after reloading a save file.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref activeDisease, "activeDisease");
            Scribe_Values.Look(ref tickContaminated, "tickContaminated", -1);
        }

        /// <summary>
        /// Sets the object as contaminated with a specific disease and records the current time.
        /// </summary>
        /// <param name="disease">The disease to store on the object.</param>
        public void Contaminate(HediffDef disease)
        {
            activeDisease = disease;
            tickContaminated = Find.TickManager.TicksGame;
        }

        /// <summary>
        /// Manually clears any pathogen from the object.
        /// Useful for cleaning mechanics or disinfection.
        /// </summary>
        public void Cleanse()
        {
            activeDisease = null;
            tickContaminated = -1;
        }

        /// <summary>
        /// Checks if the object is currently contaminated.
        /// Automatically triggers decay logic if the configured 'daysToDecay' has passed.
        /// </summary>
        /// <returns>True if the object is still infectious; false otherwise.</returns>
        public bool IsContaminated()
        {
            if (activeDisease == null || tickContaminated == -1) return false;

            // RimWorld standard: 60,000 ticks = 1 in-game day.
            float daysPassed = (Find.TickManager.TicksGame - tickContaminated) / 60000f;

            if (daysPassed > Props.daysToDecay)
            {
                Cleanse();
                return false;
            }

            return true;
        }
    }
}