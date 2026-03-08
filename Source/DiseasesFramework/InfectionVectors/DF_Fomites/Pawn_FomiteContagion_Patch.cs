using HarmonyLib;
using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Fomites
{
    /// <summary>
    /// Harmony patch that monitors healthy pawns for exposure to contaminated objects (fomites).
    /// It checks worn apparel and the bed the pawn is currently using.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "TickRare")]
    public static class Pawn_FomiteContagion_Patch
    {
        /// <summary>
        /// Postfix executed every 250 ticks. 
        /// It triggers a deeper exposure check at a longer interval to optimize CPU usage.
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || !__instance.Spawned || __instance.Dead || !__instance.RaceProps.Humanlike)
                return;

            // Executes the exposure check roughly once every 2500 ticks (approx. 1 in-game hour).
            if (__instance.IsHashIntervalTick(2500))
            {
                CheckFomiteExposure(__instance);
            }
        }

        /// <summary>
        /// Scans the pawn's environment and gear for pathogens.
        /// </summary>
        private static void CheckFomiteExposure(Pawn pawn)
        {
            // Vector 1: Worn Apparel.
            // If the pawn is wearing contaminated clothes, they are constantly exposed to the pathogen.
            if (pawn.apparel != null)
            {
                foreach (Apparel apparel in pawn.apparel.WornApparel)
                {
                    CompFomite comp = apparel.TryGetComp<CompFomite>();

                    if (comp != null && comp.IsContaminated() && !pawn.health.hediffSet.HasHediff(comp.ActiveDisease))
                    {
                        TryInfect(pawn, comp.ActiveDisease, apparel.LabelShort);
                    }
                }
            }

            // Vector 2: Furniture (Beds).
            // Sleeping in a contaminated bed is a high-risk activity for disease transmission.
            if (pawn.InBed())
            {
                Building_Bed bed = pawn.CurrentBed();
                if (bed != null)
                {
                    CompFomite comp = bed.TryGetComp<CompFomite>();

                    if (comp != null && comp.IsContaminated() && !pawn.health.hediffSet.HasHediff(comp.ActiveDisease))
                    {
                        TryInfect(pawn, comp.ActiveDisease, bed.LabelShort);
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to infect the pawn based on a fixed probability roll.
        /// Handles notifications and letters according to the disease's configuration.
        /// </summary>
        /// <param name="pawn">The pawn being exposed.</param>
        /// <param name="disease">The pathogen present on the object.</param>
        /// <param name="sourceName">The name of the contaminated object for the notification.</param>
        private static void TryInfect(Pawn pawn, HediffDef disease, string sourceName)
        {
            // Base chance for contact infection.
            float infectionChance = 0.05f;

            if (Rand.Chance(infectionChance))
            {
                pawn.health.AddHediff(disease);

                if (pawn.Faction == Faction.OfPlayer)
                {
                    // Attempt to retrieve custom notification settings from the disease's XML props.
                    var props = disease.CompProps<HediffCompProperties_FomiteContagion>();

                    bool sendNotif = props != null ? props.sendNotification : true;
                    bool useLetter = props != null ? props.useLetterInsteadOfMessage : false;

                    if (sendNotif)
                    {
                        string title = "Contact infection";
                        string text = $"{pawn.LabelShort} has contracted {disease.label} through contact with a contaminated object ({sourceName}).";

                        if (useLetter)
                        {
                            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NegativeEvent, pawn);
                        }
                        else
                        {
                            Messages.Message(text, pawn, MessageTypeDefOf.NegativeEvent, true);
                        }
                    }
                }
            }
        }
    }
}