
using System.Linq;
using RimWorld;
using Verse;

namespace MCHF
{
    
    public class CompMagAgent : ThingComp
    {
        
        private Pawn parentPawn;
        public Pawn Parent
        {
            get
            {
                if (parentPawn == null)
                {
                    parentPawn = parent as Pawn;
                }
                return parentPawn;
            }
        }

        public int ticksTillHeal;            
        // should have worse healing than ghouls. bio-engineered freaks are different from unnaturally created occult freaks
        public const int ticksPerHeal = 2500; // in-game hour
        public const float healAmount = 3f;   // heal # per hour

        public override void PostPostMake()
        {
            base.PostPostMake();
            ResetTicksTillHeal();
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (!Parent.health.hediffSet.HasHediff(MCHFDefOf.MagHediff))
            {
                Parent.health.AddHediff(MCHFDefOf.MagHediff);
            }
        }
        public override void CompTick()
        {
            ticksTillHeal--;
            if (ticksTillHeal <= 0)
            {
                HealBodyPart(Parent);
                ResetTicksTillHeal();
            }
        }
        private void ResetTicksTillHeal()
        {
            ticksTillHeal = ticksPerHeal;
        }


        // regrow limbs(hp at 1) first then reduce injury severity   
        public static void HealBodyPart(Pawn pawn)
        {
            Hediff result = pawn.health.hediffSet.hediffs.FirstOrDefault(hd => hd is Hediff_Injury || hd is Hediff_MissingPart);
            if (result == null) return;
            
            if (result is Hediff_Injury)
                result.Heal(healAmount);
            
            else 
            {                
                Hediff temp = HediffMaker.MakeHediff(HediffDefOf.Misc, pawn, result.Part);          
                temp.Severity = result.Part.def.hitPoints*pawn.HealthScale-1f;
                HealthUtility.Cure(result);
                pawn.health.AddHediff(temp, result.Part);
                
            }
        }

        /*
         * redirects hits inside body toward chest/head to instead hit the ribcage/skull instead of organs. 
         * ribcage/skull must be destroyed first by low pen weapons to damage organs (or just use high pen weapons)
         */
        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            
            if (!dinfo.IgnoreArmor && dinfo.ArmorPenetrationInt < 0.20 )
            {
                // pre-roll for hitpart since game doesn't roll for it yet
                BodyPartRecord hitpart = Parent.health.hediffSet.GetRandomNotMissingPart(dinfo.Def);
                
                
                if ((hitpart.depth == BodyPartDepth.Inside && hitpart.height != BodyPartHeight.Bottom) && !(hitpart.def == MCHFDefOf.MagRibcage || hitpart.def == MCHFDefOf.MagSkull) )
                {

                    if (hitpart.height == BodyPartHeight.Middle)
                    {
                        hitpart = Parent.health.hediffSet.GetBodyPartRecord(MCHFDefOf.MagRibcage) ?? hitpart;
                    } else if (hitpart.height == BodyPartHeight.Top) 
                    {
                        hitpart = Parent.health.hediffSet.GetBodyPartRecord(MCHFDefOf.MagSkull) ?? hitpart;
                    }
                }

                // this should not effect how game rolls hits when damage doesn't need to be redirected
                // since game checks if dinfo has a hitpart before deciding to call GetRandomNotMissingPart() 
                // so we are just rolling for hitpart much earlier
                dinfo.SetHitPart(hitpart);
                dinfo.SetBodyRegion(hitpart.height, hitpart.depth);

            }

        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref ticksTillHeal, "ticksToHeal", 0);
        }

        




    }

}
