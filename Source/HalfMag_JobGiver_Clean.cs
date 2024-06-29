/*
using System.Collections.Generic;
using Verse.AI;
using Verse;
using RimWorld;


// reasons for removal: job kept ending and starting up again causing the "pawn started 10 jobs in 10 ticks" error
//                      I wanted to release the mod already and not spend time trying to learn the job system when I plan to rebuild the mod using stuff like HAR and VEF
//                      was dumb even if it was error free, since you couldn't control half-mags like a normal pawn via the work tab so it's best if they only did one job for now
//


namespace MCHF
{
    
    public class HalfMag_JobGiver_Clean : ThinkNode_JobGiver
    {
        public bool HasJobOnThing(Pawn pawn, Thing t)
        {
            if (!(t is Filth filth))
            {
                return false;
            }
            if (!filth.Map.areaManager.Home[filth.Position])
            {
                return false;
            }
            if (!pawn.CanReserve(t, 1, -1, null, false))
            {
                return false;
            }
            if (filth.TicksSinceThickened < 600)
            {
                return false;
            }
            return true;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Thing thing = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, pawn.Map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.Filth)), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 100f);
            if (thing == null || !pawn.CanReserve(thing)) {  return null; }

            Job job = new Job(JobDefOf.Clean);
            job.AddQueuedTarget(TargetIndex.A, thing);
            int num = 15;
            Map map = thing.Map;
            
            for (int index1 = 0; index1 < 100; index1++)
            {
                IntVec3 c = thing.Position + GenRadial.RadialPattern[index1];

                List<Thing> thingList = c.GetThingList(map);
                for (int index2 = 0; index2 < thingList.Count; index2++)
                {
                    Thing things = thingList[index2];
                    if (HasJobOnThing(pawn, things) && things != thing)
                    {
                        job.AddQueuedTarget(TargetIndex.A, things);
                    }
                }
                if (job.GetTargetQueue(TargetIndex.A).Count >= num)
                {
                    break;
                }
            }
            if (job.targetQueueA != null && job.targetQueueA.Count >= 5)
            {
                job.targetQueueA.SortBy((LocalTargetInfo targ) => targ.Cell.DistanceToSquared(pawn.Position));
            }
            return job;

        }
    }
}
*/