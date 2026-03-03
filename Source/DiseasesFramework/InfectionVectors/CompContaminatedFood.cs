using Verse;
using RimWorld;

namespace DiseasesFramework.InfectionVectors
{
    public class CompProperties_ContaminatedFood : CompProperties
    {
        public bool sendNotification = true;
        public bool useLetterInsteadOfMessage = false;

        public CompProperties_ContaminatedFood()
        {
            this.compClass = typeof(CompContaminatedFood);
        }
    }

    public class CompContaminatedFood : ThingComp
    {
        public HediffDef linkedDisease;

        public CompProperties_ContaminatedFood Props => (CompProperties_ContaminatedFood)this.props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref linkedDisease, "linkedDisease");
        }

        public override void PostSplitOff(Thing piece)
        {
            base.PostSplitOff(piece);
            CompContaminatedFood otherComp = piece.TryGetComp<CompContaminatedFood>();

            if (otherComp != null)
            {
                otherComp.linkedDisease = this.linkedDisease;
            }
        }

        public override void PostIngested(Pawn ingester)
        {
            base.PostIngested(ingester);

            if (linkedDisease == null || ingester == null) return;
            if (!ingester.RaceProps.IsFlesh) return;
            if (ingester.health.hediffSet.HasHediff(linkedDisease)) return;

            ingester.health.AddHediff(linkedDisease);

            if (Props.sendNotification && ingester.Faction == Faction.OfPlayer)
            {
                string text = $"{ingester.LabelShort} contracted {linkedDisease.label} from contaminated food.";

                if (Props.useLetterInsteadOfMessage)
                {
                    Find.LetterStack.ReceiveLetter("Food Contamination!", text, LetterDefOf.NegativeEvent, ingester);
                }
                else
                {
                    Messages.Message(text, ingester, MessageTypeDefOf.NegativeEvent, true);
                }
            }
        }

        public override System.Collections.Generic.IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Infect Food (FoodPoisoning)",
                    defaultDesc = "Debug tool for Blaxer Studios: Manually link Food Poisoning to this item.",
                    icon = TexCommand.Attack,
                    action = () =>
                    {
                        this.linkedDisease = HediffDefOf.FoodPoisoning;
                        Messages.Message("Dev: Food manually infected with Food Poisoning", MessageTypeDefOf.NeutralEvent);
                    }
                };
            }
        }
    }
}