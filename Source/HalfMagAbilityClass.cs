using RimWorld;
using System.Collections;
using System.Linq;
using Verse;

namespace MCHF
{
    /*
     * RAMBLINGS DO NOT READ:
     * 
     * Could have used an ability comp (and prolly should have), but this uses slightly less overhead and shold be good if I just plan on adding one ability   
     * dont think I like how this is done as in extreme cases ticks could be extremely slow and just now match up with the graphics  
     * so itll look like your pawn got hit out of nowhere if the ability takes a while to tick and your pawn is already out of the way
     * though it can have the side effect of looking cool and weird like how an anime sword slice 
     * 
     * I also saw the way other mods do something similar is by just launching an invisible projectile that pierces and travels the same path/speed as the pawnjumper graphic
     * (specifically that's what Lee's Star Wars : The Force-Psycast does)
     * that way is probably much better. I might try that with the tools VFE has OR customise my subclassed pawnjumper to act like its own projectile by that is prolly unneeded 
     */






    // handles applying damage, warmup effect, and stuns for Verb_DashDamage. things affected by dash consecutively damaged every abilityTick()
    public class HalfMagAbilityClass : Ability
    {

        public Effecter effecterCast;

        public HalfMagAbilityClass() : base()
        { }
        public HalfMagAbilityClass(Pawn pawn) : base(pawn)
        { }
        public HalfMagAbilityClass(Pawn pawn, AbilityDef def) : base(pawn, def)
        { }
        public float BonusDmg
        {

            get
            {
                return pawn.verbTracker.PrimaryVerb.tool.power * 1.2f;
            }
        }

        public float ArmorPen
        {

            get
            {
                return pawn.verbTracker.PrimaryVerb.tool.armorPenetration;
            }
        }

        // passes by value thingsToDamage to DamagedThings 
        public Thing[] DamagedThings
        {
            get
            {
                if (VerbTracker.PrimaryVerb is Verb_DashDamage && (VerbTracker.PrimaryVerb as Verb_DashDamage).thingsToDamage.Any() )
                {
                    return (VerbTracker.PrimaryVerb as Verb_DashDamage).thingsToDamage.ToArray();
                }
                else
                {
                    return null;
                }

            }
        }
        public IEnumerator damagedThings = null;

        public int damageTicks = 0;

        // called by Verb_DashDamage which causes the ability class to damage things affected by the dash every tick
        public void CheckForThingsToDamage()
        {
            if (DamagedThings != null && damagedThings == null)
            {
                damagedThings = DamagedThings.GetEnumerator();
            }
        }

        public void Reset()
        {
            damagedThings = null;
        }

        // when damagedThings isn't null we iterate/damage a thing each tick, setting the enumerator back to null once we're done
        public void DamageTick()
        {
            if (damagedThings.MoveNext())
            {
                ApplyDamage((Thing)damagedThings.Current);
            } else
            {
                Reset();
            }

            
        }
        private void ApplyDamage(Thing thing)
        {
            DamageInfo dinfo = new DamageInfo(MCHFDefOf.HalfMagDashDamage, BonusDmg, ArmorPen, instigator: pawn);

            if (thing != null && thing != pawn)
            {
                thing.TakeDamage(dinfo);
                if (thing is Pawn && !(thing as Pawn).Dead)
                {
                    (thing as Pawn).stances.stagger.StaggerFor(95);
                }             
            }
        }

        // warmpup effecter
        public void EffecterTick()
        {
            if (pawn.Spawned && Casting)
            {
                if (effecterCast == null)
                {
                    IntVec3 cell = pawn.Position;
                    TargetInfo targetInfo = new TargetInfo(cell, pawn.Map);
                    TargetInfo b = targetInfo;
                    effecterCast = MCHFDefOf.MagChargeUpEffect.SpawnMaintained(targetInfo, b, 1);
                }
            }
            else if (effecterCast != null)
            {
                effecterCast.ForceEnd();
                effecterCast = null;
            }
        }
        
        public override void AbilityTick()
        {
            // stops AI from aimbotting so attack can be avoidable
            // At the last 4th of the ability's warmup time, the location ai dashes to stops updating with target's location           
            if (VerbTracker.PrimaryVerb is Verb_DashDamage && VerbTracker.PrimaryVerb.CurrentTarget.IsValid &&
                (VerbTracker.PrimaryVerb.WarmupTicksLeft >= VerbTracker.PrimaryVerb.verbProps.warmupTime.SecondsToTicks() / 4))
            {
                (VerbTracker.PrimaryVerb as Verb_DashDamage).delayedTarget = (VerbTracker.PrimaryVerb as Verb_DashDamage).CurrentTarget.Cell;
            }

            // user gets self-inflicted damaged/stunned if they dash into a wall(or other big thing) like a dumbass 
            if (pawn.Spawned && VerbTracker.PrimaryVerb is Verb_DashDamage && (VerbTracker.PrimaryVerb as Verb_DashDamage).dashBlocked)
            {
                (VerbTracker.PrimaryVerb as Verb_DashDamage).dashBlocked = false;
                DamageInfo dinfo = new DamageInfo(MCHFDefOf.HalfMagDashDamage, BonusDmg, ArmorPen);
                pawn.TakeDamage(dinfo);
                pawn.stances.stunner.StunFor(100, pawn, false, true, false);
            }
            base.AbilityTick();
            EffecterTick();

            if (damagedThings != null) DamageTick();
        }
    }
}
