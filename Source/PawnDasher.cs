using RimWorld;
using UnityEngine;
using Verse;


namespace MCHF
{

    
    // a part of me wants to revamp this to add functionality other than graphics just to see if I can(like what if the jumper graphic was treated like a projectile)
    // though that's extremely dumb especially if I want to configure this in the future to do other animations like dodging 



    // changes some of the calcs cause we're just traveling in a straight line really fast
    public class PawnDasher : PawnFlyer
    {

        private int positionLastComputedTick = -1;
        private Vector3 groundPos;
        private Vector3 effectivePos;

 

        public override Vector3 DrawPos
        {
            get
            {
                RecomputePosition();
                return effectivePos;
            }
        }

        public Vector3 DesPos => DestinationPos.Yto0();

        private void RecomputePosition()
        {
            if (positionLastComputedTick != ticksFlying)
            {
                positionLastComputedTick = ticksFlying;
                float t = (float)ticksFlying / ticksFlightTime;
                
                groundPos = Vector3.Lerp(startVec, DesPos, t);
                effectivePos = groundPos;
                Position = groundPos.ToIntVec3();
            }
        }
        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            RecomputePosition();
            if (FlyingPawn != null)
            {
                FlyingPawn.DynamicDrawPhaseAt(phase, effectivePos);
            }
            else
            {
                FlyingThing?.DynamicDrawPhaseAt(phase, effectivePos);
            }
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (CarriedThing != null && FlyingPawn != null)
            {
                PawnRenderUtility.DrawCarriedThing(FlyingPawn, effectivePos, CarriedThing);
            }
        }

    }
    
}
