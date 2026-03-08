using HarmonyLib;
using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Zoonosis
{
    // -------------------------------------------------------
    // 1. Healing patch
    // Activated when a settler heals an animal's wounds or illnesses.
    // -------------------------------------------------------
    [HarmonyPatch(typeof(TendUtility), "DoTend")]
    public static class Patch_Zoonosis_Tend
    {
        [HarmonyPostfix]
        public static void PostFix(Pawn doctor, Pawn patient)
        {
            if (doctor == null || patient == null || !patient.RaceProps.Animal || !doctor.RaceProps.Humanlike)
                return;

            foreach (Hediff hediff in patient.health.hediffSet.hediffs)
            {
                var comp = hediff.TryGetComp<HediffComp_Zoonosis>();
                if (comp != null)
                {
                    // true = use the high probability of 'tendingInfectionChance'
                    comp.CheckAndTryInfect(doctor, true);
                }
            }
        }
    }

    // -----------------------------------------------------------
    // 2. Social interaction and treatment patch (tame, train, nuzzle)
    // -----------------------------------------------------------
    [HarmonyPatch(typeof(Pawn_InteractionsTracker), "TryInteractWith")]
    public static class Patch_Zoonosis_Interact
    {
        // Note: Harmony allows access to private variables using 3 underscores (___pawn)
        [HarmonyPostfix]
        public static void PostFix(bool __result, Pawn recipient, Pawn ___pawn)
        {
            if (!__result || recipient == null || ___pawn == null) return;

            // Scenario A: Human interacts with animal (e.g., taming or training)
            if (___pawn.RaceProps.Humanlike && recipient.RaceProps.Animal)
            {
                foreach (Hediff hediff in recipient.health.hediffSet.hediffs)
                {
                    var comp = hediff.TryGetComp<HediffComp_Zoonosis>();
                    if (comp != null) comp.CheckAndTryInfect(___pawn, false);
                }
            }
            // Scenario B: Animal interacts with human (e.g., nuzzling)
            else if (___pawn.RaceProps.Animal && recipient.RaceProps.Humanlike)
            {
                foreach (Hediff hediff in ___pawn.health.hediffSet.hediffs)
                {
                    var comp = hediff.TryGetComp<HediffComp_Zoonosis>();
                    if (comp != null) comp.CheckAndTryInfect(recipient, false);
                }
            }
        }
    }

    // -----------------------------------------------------------
    // 3. Harvesting patch (milking and shearing)
    // -----------------------------------------------------------
    [HarmonyPatch(typeof(CompHasGatherableBodyResource), "Gathered")]
    public static class Patch_Zoonosis_Gather
    {
        [HarmonyPostfix]
        public static void Postfix(CompHasGatherableBodyResource __instance, Pawn doer)
        {
            if (doer == null || !doer.RaceProps.Humanlike) return;

            // The animal is extracted from the base class
            if (__instance.parent is Pawn animal && animal.RaceProps.Animal)
            {
                foreach (Hediff hediff in animal.health.hediffSet.hediffs)
                {
                    var comp = hediff.TryGetComp<HediffComp_Zoonosis>();
                    if (comp != null) comp.CheckAndTryInfect(doer, false);
                }
            }
        }
    }

    // -----------------------------------------------------------
    // 4. Transport patch (e.g., rescue or take to the corral)
    // -----------------------------------------------------------
    [HarmonyPatch(typeof(Pawn_CarryTracker), "TryStartCarry", new System.Type[] { typeof(Thing), typeof(int), typeof(bool) })]
    public static class Patch_Zoonosis_Carry
    {
        [HarmonyPostfix]
        public static void Postfix(int __result, Thing item, Pawn ___pawn)
        {
            // We verify that something has been collected, that it is an animal, and that the collector is human.
            if (__result <= 0 || ___pawn == null || !___pawn.RaceProps.Humanlike || !(item is Pawn animal) || !animal.RaceProps.Animal)
                return;

            foreach (Hediff hediff in animal.health.hediffSet.hediffs)
            {
                var comp = hediff.TryGetComp<HediffComp_Zoonosis>();
                if (comp != null)
                {
                    // Rescue/Load involves intense physical contact, we use "true" (tending chance)
                    comp.CheckAndTryInfect(___pawn, true);
                }
            }
        }
    }
    // -----------------------------------------------------------
    // 5. Butchery patch
    // Activated when a settler cuts up a corpse on the table.
    // -----------------------------------------------------------
    [HarmonyPatch(typeof(Verse.Corpse), "ButcherProducts")]
    public static class Patch_Zoonosis_Butcher
    {
        [HarmonyPrefix]
        public static void Prefix(Verse.Corpse __instance, Pawn butcher)
        {
            // We verify that there is a human butcher
            if (butcher == null || !butcher.RaceProps.Humanlike) return;

            // We extract the ghost animal from inside the corpse
            Pawn animal = __instance.InnerPawn;

            if (animal != null && animal.RaceProps.Animal)
            {
                // We reviewed the illnesses the animal had just before it died
                foreach (Hediff hediff in animal.health.hediffSet.hediffs)
                {
                    var comp = hediff.TryGetComp<HediffComp_Zoonosis>();
                    if (comp != null)
                    {
                        // We pass true to the "isButchering" parameter
                        comp.CheckAndTryInfect(butcher, false, true);
                    }
                }
            }
        }
    }
}