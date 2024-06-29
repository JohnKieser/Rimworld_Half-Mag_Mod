using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace MCHF
{

    public class HalfMag_JobGiver_FightFire : ThinkNode_JobGiver
    {

        protected override Job TryGiveJob(Pawn pawn)
        {
            Predicate<Thing> fireValidator = delegate (Thing t)
            {
                if (t.IsForbidden(pawn))
                {
                    return false;
                }

               
                if (!pawn.CanReserve(t))
                {
                    return false;
                }
                return !pawn.WorkTagIsDisabled(WorkTags.Firefighting);  
            };



            Thing targetA = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ThingDefOf.Fire), PathEndMode.Touch, TraverseParms.For(pawn), 300f, fireValidator);
            if (targetA != null)
            {
                return JobMaker.MakeJob(JobDefOf.BeatFire, targetA);
            }
            return null;
        }



    }
}
