using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace DiseasesFramework.HarmonyPatches
{
    /// <summary>
    /// Harmony patch for FloatMenuMakerMap to inject custom disinfection orders.
    /// Targeted at RimWorld 1.6's updated menu system which utilizes a caching and revalidation mechanism.
    /// </summary>
    [HarmonyPatch(typeof(FloatMenuMakerMap), "GetOptions")]
    public static class FloatMenu_Fomite_Patch
    {
        /// <summary>
        /// Postfix that appends the "Disinfect" option to the float menu if the target object is contaminated.
        /// </summary>
        /// <param name="__result">The resulting list of menu options to be displayed and cached.</param>
        /// <param name="selectedPawns">The list of pawns currently selected by the player.</param>
        /// <param name="clickPos">The world position where the user performed the right-click.</param>
        [HarmonyPostfix]
        public static void Postfix(ref List<FloatMenuOption> __result, List<Pawn> selectedPawns, Vector3 clickPos)
        {
            // Retrieve the primary pawn from the selection context
            Pawn pawn = selectedPawns.FirstOrDefault();

            // Safety checks: Ensure pawn exists, is alive, functional, and belongs to the player's faction
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.Faction != Faction.OfPlayer) return;

            IntVec3 c = IntVec3.FromVector3(clickPos);

            // Iterate through all entities at the clicked position to find Fomite-enabled objects
            foreach (Thing thing in c.GetThingList(pawn.Map))
            {
                var comp = thing.TryGetComp<InfectionVectors.DF_Fomites.CompFomite>();

                // Verify the presence of the Fomite component and check if it currently carries a pathogen
                if (comp != null && comp.IsContaminated())
                {
                    // Generate localized label using the object's short label (e.g., "Disinfect Wooden Bed")
                    string label = "DF_DisinfectBed_Option".Translate(thing.LabelShort);

                    FloatMenuOption option = new FloatMenuOption(
                        label,
                        delegate
                        {
                            // Instantiate and assign the custom Disinfect Job defined in XML (DF_DisinfectBedJob)
                            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("DF_DisinfectBedJob"), thing);
                            pawn.jobs.TryTakeOrderedJob(job);
                        },
                        MenuOptionPriority.Low
                    );

                    // CRITICAL FOR 1.6: Assigning the revalidateClickTarget prevents the menu's
                    // 4-frame revalidation cycle from incorrectly disabling or removing this option.
                    option.revalidateClickTarget = thing;
                    option.iconThing = thing;

                    // Apply standard UI decorations for prioritized tasks and append to the result list
                    __result.Add(FloatMenuUtility.DecoratePrioritizedTask(option, pawn, thing));
                };
            }
        }
    }
}