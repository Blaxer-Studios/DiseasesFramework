using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors.DF_Zoonosis
{
    public class HediffCompProperties_Zoonosis : HediffCompProperties
    {
        public HediffDef hediffToApply;

        public float handlingInfectionChance = 0.05f;
        public float tendingInfectionChance = 0.15f;
        public float butcheringInfectionChance = 0.25f;

        public bool sendNotification = true;
        public bool useLetterInsteadOfMessage = false;

        public HediffCompProperties_Zoonosis()
        {
            this.compClass = typeof(HediffComp_Zoonosis);
        }
    }

    public class HediffComp_Zoonosis : HediffComp
    {
        public HediffCompProperties_Zoonosis Props => (HediffCompProperties_Zoonosis)this.props;

        public void CheckAndTryInfect(Pawn human, bool isTending = false, bool isButchering = false)
        {
            if (human == null || !human.Spawned || human.Dead || !human.RaceProps.Humanlike)
                return;

            float chance = Props.handlingInfectionChance;
            if (!isTending) chance = Props.tendingInfectionChance;
            if (!isButchering) chance = Props.butcheringInfectionChance;

            if (Rand.Chance(chance))
            {
                HediffDef diseaseToGive = Props.hediffToApply ?? this.parent.def;

                if (!human.health.hediffSet.HasHediff(diseaseToGive))
                {
                    human.health.AddHediff(diseaseToGive);

                    if (Props.sendNotification && human.Faction == Faction.OfPlayer)
                    {
                        string animalName = this.Pawn.LabelShort;
                        string text = $"{human.LabelShort} has contracted {diseaseToGive.label} from interacting with an infected animal ({animalName}).";

                        if (Props.useLetterInsteadOfMessage)
                        {
                            Find.LetterStack.ReceiveLetter("Zoonotic Infection", text, LetterDefOf.NegativeEvent, human);
                        }
                        else
                        {
                            Messages.Message(text, human, MessageTypeDefOf.NegativeEvent, true);
                        }
                    }
                }
            }
        }
    }
}