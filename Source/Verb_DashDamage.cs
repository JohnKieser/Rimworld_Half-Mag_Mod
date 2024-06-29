
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;


namespace MCHF
{
  
    // just as the verb is casted, it makes a path between caster and destination and sends a list of allowed things to be damaged to its ability class instance
    // the ability class instance then damages each thing consecutively every tick
    public class Verb_DashDamage : Verb, IAbilityVerb
    {
        public IntVec3 delayedTarget;
        public List<Thing> thingsToDamage = new List<Thing>(30);
        public bool dashBlocked  = false;

        // I think ai uses this to decide who to target, so they should use the verb as if they were targetting with a ranged weapon
        public override bool IsMeleeAttack => false;

        private Ability ability;

        public Ability Ability
        {
            get
            {
                return ability;
            }
            set
            {
                ability = value;
            }
        }

        private float cachedEffectiveRange = -1f;

        public override float EffectiveRange
        {
            get
            {
                if (cachedEffectiveRange < 0f)
                {
                    if (EquipmentSource != null)
                    {
                        cachedEffectiveRange = EquipmentSource.GetStatValue(StatDefOf.JumpRange);
                    }
                    else
                    {
                        cachedEffectiveRange = verbProps.range;
                    }
                }
                return cachedEffectiveRange;
            }
        }

        public override bool MultiSelect => true;

        /*
         * Makes a straight path between pawn and target and checks things in each cell
         * the path is 'cut short' if a thing is impassable or has a bodysize above 2.06
         * returns the cell that pawn dashes to and fills up thingsToDamage
         */
        private IntVec3 DashPathing(IntVec3 sourceCell, IntVec3 targetCell, Map map)
        {
            IEnumerable<IntVec3> cellsToAffect = GenSight.BresenhamCellsBetween(sourceCell, targetCell);           
            IntVec3? prevCell = null;

            foreach (IntVec3 cell in cellsToAffect)
            {
                List<Thing> thingsAtCell = map.thingGrid.ThingsListAtFast(cell);
                for (int i = 0;  i < thingsAtCell.Count; i++) 
                {
                    Thing thing = thingsAtCell[i];

                    if ((thing.def.IsDoor && !(thing as Building_Door).Open) || thing.def.passability == Traversability.Impassable || (thing is Pawn && (thing as Pawn).BodySize > CasterPawn.BodySize*1.6))
                    {
                        thingsToDamage.Add(thing);
                        dashBlocked = true;                     
                        return prevCell ?? cell;                           
                    }

                    if (!thing.def.IsDoor && thing.def.category != ThingCategory.Filth)
                    thingsToDamage.Add(thing);
                }
                prevCell = cell;              
            }
            return cellsToAffect.Last(); 
        }

        // not much different from what verb_jump does execpt destination is decided by DashPathing and delayedtarget
        protected override bool TryCastShot()
        {           
            IntVec3 prevPos = CasterPawn.Position;
            Map map = Caster.Map;
            Pawn pawn = CasterPawn;
         
            IntVec3 finalDes = DashPathing(prevPos, delayedTarget, map);
            bool flag = Find.Selector.IsSelected(pawn);
            PawnDasher pawnDasher = (PawnDasher)PawnFlyer.MakeFlyer(MCHFDefOf.PawnDasher, pawn, finalDes, MCHFDefOf.MagDashEffect,
                                                                    SoundDefOf.MeleeHit_Unarmed, target: currentTarget, overrideStartVec: prevPos.ToVector3Shifted(), flyWithCarriedThing: verbProps.flyWithCarriedThing);

            
            if (pawnDasher != null)
            {

                lastShotTick = Find.TickManager.TicksGame;
                GenSpawn.Spawn(pawnDasher, prevPos, map);

                if (flag)
                {
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }
                if (ability != null)
                {
                    // method has ability class copy thingsToDamage which has to damage/iterate each thing in list every tick
                    (ability as HalfMagAbilityClass).CheckForThingsToDamage();
                  
                    ability.StartCooldown(ability.def.cooldownTicksRange.RandomInRange);
                    thingsToDamage.Clear();
                } else
                {
                    thingsToDamage.Clear();
                }
                return true;
            }
            return false;
        }
        

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            Map map = Caster.Map;
            Job job = JobMaker.MakeJob(JobDefOf.CastJump, target.Cell);
            job.verbToUse = this;
            if (CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
            {
                FleckMaker.Static(target.Cell, map, FleckDefOf.FeedbackGoto);
            }
        }

        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            // so delayedTarget isn't null if verb doesn't have its ability class for whatever reason
            delayedTarget = castTarg.Cell;
            return base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
        }

        // draw radius ring that ignores walls to incentivise players to hit walls
        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
            }
            GenDraw.DrawRadiusRing(caster.Position, EffectiveRange, Color.white);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref ability, "ability");
        }
    }
}
