using Verse;
using RimWorld;
using HarmonyLib;
using System.Collections.Generic;
using DiseasesFramework.InfectionVectors;

namespace DiseasesFramework.HarmonyPatches
{
    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
    public static class Patch_GenRecipe_MakeRecipeProducts
    {
        public static IEnumerable<Thing> Postfix(IEnumerable<Thing> values, RecipeDef recipeDef, Pawn worker)
        {
            if (worker == null || !worker.RaceProps.IsFlesh)
            {
                foreach (Thing t in values) yield return t;
                yield break;
            }

            HediffComp_IngestionContagion contagionComp = null;
            var hediffs = worker.health.hediffSet.hediffs;

            for (int i = 0; i < hediffs.Count; i++)
            {
                var c = hediffs[i].TryGetComp<HediffComp_IngestionContagion>();
                if (c != null)
                {
                    contagionComp = c;
                    break;
                }
            }

            foreach (Thing product in values)
            {
                if (contagionComp != null)
                {
                    CompContaminatedFood foodComp = product.TryGetComp<CompContaminatedFood>();

                    if (foodComp != null && Rand.Chance(contagionComp.Props.contaminationChance))
                    {
                        foodComp.linkedDisease = contagionComp.Props.hediffToApply;
                    }
                    else if (foodComp == null)
                    {
                        Log.Warning($"[Blaxer Studios] {product.Label} does not have CompContaminatedFood component!");
                    }
                }
                yield return product;
            }
        }
    }
}