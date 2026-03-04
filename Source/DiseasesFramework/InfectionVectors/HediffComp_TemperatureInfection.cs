using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors
{
    public class HediffCompProperties_TemperatureInfection : HediffCompProperties
    {
        public float minSeverity = 0.1f;
        public HediffDef diseaseToApply;
        public float chancePerCheck = 0.05f;
        public int checkInterval = 2500;
        public bool sendNotification = true;
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_TemperatureInfection()
        {
            this.compClass = typeof(HediffComp_TemperatureInfection);
        }
    }

    public class HediffComp_TemperatureInfection : HediffComp
    {
        public HediffCompProperties_TemperatureInfection Props => (HediffCompProperties_TemperatureInfection)this.props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn == null || Pawn.Dead) return;

            if (Pawn.IsHashIntervalTick(Props.checkInterval))
            {
                CheckForInfection();
            }
        }

        private void CheckForInfection()
        {
            if (parent == null) return;

            if (parent.Severity < Props.minSeverity) return;

            if (Pawn.health.hediffSet.HasHediff(Props.diseaseToApply)) return;

            if (Rand.Chance(Props.chancePerCheck))
            {
                Pawn.health.AddHediff(Props.diseaseToApply);

                if (Props.sendNotification && Pawn.Faction == Faction.OfPlayer)
                {
                    string text = $"{Pawn.LabelShort} has contracted {Props.diseaseToApply.label} due to severe exposure.";

                    if (Props.useLetterInsteadOfMessage)
                    {
                        Find.LetterStack.ReceiveLetter("Disease Risk", text, LetterDefOf.NegativeEvent, Pawn);
                    }
                    else
                    {
                        Messages.Message(text, Pawn, MessageTypeDefOf.NegativeEvent, true);
                    }
                }
            }
        }
    }
}