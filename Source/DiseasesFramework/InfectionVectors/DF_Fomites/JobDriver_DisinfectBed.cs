using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Fomites
{
    /// <summary>
    /// JobDriver responsible for the disinfection process of a contaminated object.
    /// Handles the pawn's movement to the target, the work duration, and the execution of the cleaning logic.
    /// </summary>
    public class JobDriver_DisinfectBed : JobDriver
    {
        /// <summary>Remaining work amount. 100 ticks is approximately 1.66 seconds at normal speed.</summary>
        private float workLeft = 600f;
        private const float TotalWork = 600f;

        /// <summary>
        /// Reserves the target object to prevent other pawns from interacting with it 
        /// while the disinfection is in progress.
        /// </summary>
        /// <param name="errorOnFailed">Whether to log an error if reservation fails.</param>
        /// <returns>True if the reservation was successful.</returns>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        /// <summary>
        /// Defines the sequence of actions (Toils) required to complete the job.
        /// </summary>
        /// <returns>An enumerable of Toils representing the job steps.</returns>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Step 1: Approach the target.
            // PathEndMode.Touch ensures the pawn stands adjacent to or on the target.
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Step 2: Perform the disinfection work.
            Toil disinfect = new Toil();
            disinfect.tickAction = delegate
            {
                // Progressively reduce the remaining work each tick.
                workLeft -= 1f;

                // Once work is completed, trigger the disinfection logic.
                if (workLeft <= 0f)
                {
                    CompFomite comp = TargetA.Thing.TryGetComp<CompFomite>();
                    if (comp != null)
                    {
                        // Resets the contamination state and triggers player feedback.
                        comp.Disinfect();
                    }

                    // Increment the pawn's cleaning records for statistics/achievements.
                    pawn.records.Increment(RecordDefOf.MessesCleaned);

                    // Signal that the job has been completed successfully.
                    EndJobWith(JobCondition.Succeeded);
                }
            };

            // Set the toil to stay active until manually ended via code logic (EndJobWith).
            disinfect.defaultCompleteMode = ToilCompleteMode.Never;

            // Display a circular progress bar over the pawn's head.
            disinfect.WithProgressBar(TargetIndex.A, () => 1f - (workLeft / TotalWork));

            // Fail-safe: If the target is destroyed or despawned, the job ends immediately.
            disinfect.FailOnDespawnedOrNull(TargetIndex.A);

            // Play the "Clean Filth" sound effect continuously while working.
            disinfect.PlaySustainerOrSound(SoundDefOf.Interact_CleanFilth);

            yield return disinfect;
        }
    }
}