using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Fomites
{
    /// <summary>
    /// Configuration properties for the Fomite component.
    /// Defines the persistence threshold of a pathogen on a physical object before natural decay occurs.
    /// </summary>
    public class CompProperties_Fomite : CompProperties
    {
        /// <summary>The duration (in in-game days) a pathogen remains active on the object before decaying.</summary>
        public float daysToDecay = 2f;

        public CompProperties_Fomite()
        {
            this.compClass = typeof(CompFomite);
        }
    }

    /// <summary>
    /// Component that enables an object (Thing) to act as a vector for pathogens.
    /// Stores disease data transmitted by infected pawns, allowing for indirect transmission to healthy pawns.
    /// </summary>
    public class CompFomite : ThingComp
    {
        /// <summary>Typed access to the component's XML properties.</summary>
        public CompProperties_Fomite Props => (CompProperties_Fomite)this.props;

        private HediffDef activeDisease = null;
        private int tickContaminated = -1;

        /// <summary>Exposes the current pathogen contaminating this object.</summary>
        public HediffDef ActiveDisease => activeDisease;

        /// <summary>
        /// Manages data persistence during save/load operations.
        /// Ensures contamination status and timestamps are preserved across game sessions.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref activeDisease, "activeDisease");
            Scribe_Values.Look(ref tickContaminated, "tickContaminated", -1);
        }

        /// <summary>
        /// Marks the object as contaminated. Stores the specific disease type 
        /// and stamps the current game tick for decay calculations.
        /// </summary>
        /// <param name="disease">The pathogen definition to be stored.</param>
        public void Contaminate(HediffDef disease)
        {
            activeDisease = disease;
            tickContaminated = Find.TickManager.TicksGame;
        }

        /// <summary>
        /// Resets the contamination status without external feedback. 
        /// Typically used for internal decay logic or silent resets.
        /// </summary>
        public void Cleanse()
        {
            activeDisease = null;
            tickContaminated = -1;
        }

        /// <summary>
        /// Explicitly cleanses the object and triggers a success message to the player.
        /// Primarily called by JobDrivers or player-initiated disinfection actions.
        /// </summary>
        public void Disinfect()
        {
            this.activeDisease = null;
            this.tickContaminated = -1; // Added to ensure full state reset
            Messages.Message("DF_BedDisinfected".Translate(parent.LabelShort), parent, MessageTypeDefOf.PositiveEvent);
        }

        /// <summary>
        /// Evaluates if the object is currently infectious. 
        /// Compares the elapsed game time against 'daysToDecay' to determine if the pathogen is still viable.
        /// </summary>
        /// <returns>True if the pathogen is active; false if clean or if the pathogen has decayed.</returns>
        public bool IsContaminated()
        {
            if (activeDisease == null || tickContaminated == -1)
                return false;

            // Conversion: 60,000 game ticks equals 1 in-game day.
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