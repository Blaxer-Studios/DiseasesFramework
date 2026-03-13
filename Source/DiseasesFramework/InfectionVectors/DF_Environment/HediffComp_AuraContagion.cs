using Verse;
using RimWorld;
using System.Collections.Generic;
using UnityEngine; // Necesario para Mathf.Clamp01

namespace DiseasesFramework.InfectionVectors.DF_Environment
{
    /// <summary>
    /// XML properties for the Aura Contagion component.
    /// Defines how a disease spreads from a living carrier to other nearby pawns through airborne or proximity transmission.
    /// </summary>
    public class HediffCompProperties_AuraContagion : HediffCompProperties
    {
        /// <summary>The maximum distance (in tiles) the infectious aura can reach.</summary>
        public float radius = 3f;

        /// <summary>How often (in ticks) the aura pulses to check for new victims. Default 2500 is roughly 1 game hour.</summary>
        public int tickInterval = 2500;

        /// <summary>Base probability (0.0 to 1.0) of infecting someone within the aura per pulse.</summary>
        public float infectionChance = 0.5f;

        /// <summary>The specific HediffDef (disease) to transmit to the victims.</summary>
        public HediffDef hediffToApply;

        /// <summary>If true, walls and doors will physically block the infectious aura from spreading.</summary>
        public bool requireLineOfSight = true;

        // --- NEW MECHANICS ---

        /// <summary>The minimum severity the carrier's disease must reach before the aura becomes active (Incubation period).</summary>
        public float minSeverityToInfect = 0f;

        /// <summary>If true, the base infection chance is multiplied by the carrier's current disease severity.</summary>
        public bool scaleWithSeverity = false;

        /// <summary>A multiplier applied to the infection chance if both the carrier and victim share the same indoor room.</summary>
        public float indoorMultiplier = 1.0f;

        // ---------------------

        /// <summary>Whether to display a notification to the player when a colonist is infected via this aura.</summary>
        public bool sendNotification = false;

        /// <summary>If true, sends a high-priority Letter (red envelope) instead of a standard top-left Message.</summary>
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_AuraContagion()
        {
            this.compClass = typeof(HediffComp_AuraContagion);
        }
    }

    /// <summary>
    /// Active component for infectious auras.
    /// Periodically scans the area around the carrier and attempts to infect nearby biological pawns.
    /// </summary>
    public class HediffComp_AuraContagion : HediffComp
    {
        /// <summary>Provides easy access to the XML-defined properties for this component.</summary>
        public HediffCompProperties_AuraContagion Props => (HediffCompProperties_AuraContagion)this.props;

        /// <summary>
        /// Called every tick. Uses IsHashIntervalTick to optimize performance by only running the scan based on the tickInterval.
        /// </summary>
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (this.Pawn.IsHashIntervalTick(Props.tickInterval))
            {
                TryInfectOthers();
            }
        }

        /// <summary>
        /// Scans the defined radius for potential victims, applying incubation, environmental, and resistance checks before infection.
        /// </summary>
        private void TryInfectOthers()
        {
            // Standard safety checks for the carrier pawn.
            if (this.Pawn == null || !this.Pawn.Spawned || this.Pawn.Map == null || this.Pawn.Dead)
            {
                return;
            }

            // 1. INCUBATION CHECK: Prevent infection if the disease hasn't reached the required severity.
            if (this.parent.Severity < Props.minSeverityToInfect)
            {
                return;
            }

            // Failsafe check for XML configuration.
            if (Props.hediffToApply == null)
            {
                Log.ErrorOnce("[Disease Framework] AuraContagion is missing <hediffToApply> in XML.", this.Pawn.thingIDNumber);
                return;
            }

            // Retrieve all items within the contagion radius.
            IEnumerable<Thing> thingsInRadius = GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, Props.radius, true);

            foreach (Thing thing in thingsInRadius)
            {
                Pawn targetPawn = thing as Pawn;

                // Ensure the target is a living pawn and not the carrier themselves.
                if (targetPawn != null && !targetPawn.Dead && targetPawn != this.Pawn)
                {
                    // Ensure only biological standard pawns (like Humans) are susceptible.
                    if (targetPawn.RaceProps.IsMechanoid || targetPawn.RaceProps.FleshType != FleshTypeDefOf.Normal)
                    {
                        continue;
                    }

                    // Check for Line of Sight if walls/doors should block the aura.
                    if (Props.requireLineOfSight && !GenSight.LineOfSight(this.Pawn.Position, targetPawn.Position, this.Pawn.Map, true))
                    {
                        continue;
                    }

                    // Avoid infecting pawns that already have the disease.
                    if (targetPawn.health.hediffSet.HasHediff(Props.hediffToApply))
                    {
                        continue;
                    }

                    // CALCULATING MITIGATION:
                    // Uses ToxicEnvironmentResistance (e.g., from Gas Masks, Hazmat suits, or closed Apparel).
                    float protection = targetPawn.GetStatValue(StatDefOf.ToxicEnvironmentResistance);
                    float adjustedChance;

                    if (protection >= 0.8f) // High protection (Full Hazmat)
                    {
                        adjustedChance = 0f;
                    }
                    else if (protection >= 0.5f) // Partial protection (Masks)
                    {
                        adjustedChance = Props.infectionChance * 0.1f;
                    }
                    else // Low/No protection scales linearly
                    {
                        adjustedChance = Props.infectionChance * (1f - protection);
                    }

                    // Skip further calculations if the target is fully immune.
                    if (adjustedChance <= 0f) continue;

                    // 2. SEVERITY SCALING: Increase infectivity based on carrier's disease stage.
                    if (Props.scaleWithSeverity)
                    {
                        adjustedChance *= this.parent.Severity;
                    }

                    // 3. INDOOR MULTIPLIER: Increase infectivity if trapped in the same room.
                    if (Props.indoorMultiplier != 1.0f)
                    {
                        Room carrierRoom = this.Pawn.GetRoom();
                        Room targetRoom = targetPawn.GetRoom();

                        // Check if both are in the same valid room and it is not considered outdoors.
                        if (carrierRoom != null && carrierRoom == targetRoom && !carrierRoom.PsychologicallyOutdoors)
                        {
                            adjustedChance *= Props.indoorMultiplier;
                        }
                    }

                    // Final infection roll. Mathf.Clamp01 ensures chance stays between 0.0 and 1.0.
                    if (Rand.Chance(Mathf.Clamp01(adjustedChance)))
                    {
                        targetPawn.health.AddHediff(Props.hediffToApply);

                        // Handle player notifications.
                        if (Props.sendNotification && targetPawn.Faction == Faction.OfPlayer)
                        {
                            // {0} = Target pawn name, {1} = Disease label
                            string text = "DF_AuraInfection_Message".Translate(targetPawn.LabelShort, Props.hediffToApply.label);
                            string label = "DF_AuraInfection_LetterLabel".Translate();

                            if (Props.useLetterInsteadOfMessage)
                            {
                                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent, targetPawn);
                            }
                            else
                            {
                                Messages.Message(text, targetPawn, MessageTypeDefOf.NegativeEvent, true);
                            }
                        }
                    }
                }
            }
        }
    }
}