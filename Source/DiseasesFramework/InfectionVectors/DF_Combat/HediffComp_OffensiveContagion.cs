using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace DiseasesFramework.InfectionVectors.DF_Combat
{
    /// <summary>
    /// XML properties for the Offensive Contagion component.
    /// Defines parameters for a disease that spreads from the carrier to the target when the carrier attacks.
    /// </summary>
    public class HediffCompProperties_OffensiveContagion : HediffCompProperties
    {
        /// <summary>Base probability (0.0 to 1.0) of infecting the target per successful hit.</summary>
        public float infectionChance = 0.5f;

        /// <summary>The HediffDef to apply to the victim.</summary>
        public HediffDef hediffToApply;

        /// <summary>If true, contagion only occurs via melee attacks (bites, scratches, etc.).</summary>
        public bool onlyMelee = true;

        /// <summary>If true, the attack must deal actual damage to trigger the infection check.</summary>
        public bool requiresDamageToPenetrate = true;

        /// <summary>If true, the victim's Toxic Resistance stats will reduce the infection chance.</summary>
        public bool respectsToxicResistance = false;

        /// <summary>Whether to notify the player when a colonist is infected this way.</summary>
        public bool sendNotification = false;

        /// <summary>If true, sends a high-priority Letter; otherwise, a standard Message.</summary>
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_OffensiveContagion()
        {
            this.compClass = typeof(HediffComp_OffensiveContagion);
        }
    }

    /// <summary>
    /// Component that marks a disease as "infectious through attacks."
    /// It doesn't contain logic on its own, but it's used by the Harmony Patch to identify infectious attackers.
    /// </summary>
    public class HediffComp_OffensiveContagion : HediffComp
    {
        public HediffCompProperties_OffensiveContagion Props => (HediffCompProperties_OffensiveContagion)this.props;
    }

    /// <summary>
    /// Harmony patch that intercepts any damage applied to a Pawn.
    /// If the attacker has a disease with OffensiveContagion, it attempts to infect the target.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
    public static class Patch_Pawn_PostApplyDamage_OffensiveContagion
    {
        /// <summary>
        /// Postfix that runs after damage is dealt.
        /// </summary>
        /// <param name="__instance">The pawn being attacked (the potential victim).</param>
        /// <param name="dinfo">Information about the attack.</param>
        /// <param name="totalDamageDealt">The actual amount of damage that got through armor.</param>
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            // Safety check: Ensure the victim exists and there is an instigator (attacker).
            if (__instance == null || __instance.Dead || dinfo.Instigator == null) return;

            // We abort the infection if the target is a machine or has non-biological/special flesh.
            if (__instance.RaceProps.IsMechanoid || __instance.RaceProps.FleshType != FleshTypeDefOf.Normal)
            {
                return;
            }

            Pawn attacker = dinfo.Instigator as Pawn;
            if (attacker == null || attacker == __instance) return;

            // Loop through all diseases the attacker has to find OffensiveContagion components.
            List<Hediff> attackerHediffs = attacker.health.hediffSet.hediffs;
            for (int i = 0; i < attackerHediffs.Count; i++)
            {
                HediffComp_OffensiveContagion comp = attackerHediffs[i].TryGetComp<HediffComp_OffensiveContagion>();

                if (comp != null)
                {
                    var props = comp.Props;
                    if (props.hediffToApply == null) continue;

                    // Condition: Check if damage was required.
                    if (props.requiresDamageToPenetrate && totalDamageDealt <= 0f) continue;

                    // Condition: Check if it's a melee attack.
                    if (props.onlyMelee)
                    {
                        bool isMeleeAttack = dinfo.Weapon == null || !dinfo.Weapon.IsRangedWeapon;
                        if (!isMeleeAttack) continue;
                    }

                    // Condition: Don't infect if the target already has that specific disease.
                    if (__instance.health.hediffSet.HasHediff(props.hediffToApply)) continue;

                    float finalChance = props.infectionChance;

                    // Mitigation check.
                    if (props.respectsToxicResistance)
                    {
                        float bioRes = __instance.GetStatValue(StatDefOf.ToxicResistance);
                        float envRes = __instance.GetStatValue(StatDefOf.ToxicEnvironmentResistance);
                        float bestProtection = Mathf.Max(bioRes, envRes);

                        finalChance *= Mathf.Clamp01(1f - bestProtection);
                    }

                    // Final roll.
                    if (Rand.Chance(finalChance))
                    {
                        __instance.health.AddHediff(props.hediffToApply);

                        if (props.sendNotification && __instance.Faction == Faction.OfPlayer)
                        {
                            string text = __instance.LabelShort + " was infected by an attack from " + attacker.LabelShort + ".";

                            if (props.useLetterInsteadOfMessage)
                            {
                                Find.LetterStack.ReceiveLetter("Offensive Infection!", text, LetterDefOf.NegativeEvent, __instance);
                            }
                            else
                            {
                                Messages.Message(text, __instance, MessageTypeDefOf.NegativeEvent, true);
                            }
                        }
                    }
                }
            }
        }
    }
}