using RimWorld;
using Verse;

namespace MCHF
{
    [StaticConstructorOnStartup]
    [DefOf]
    public static class MCHFDefOf
    {
        public static ThingDef HalfMagRace;
        public static ThingDef PawnDasher;      
        public static EffecterDef MagDashEffect;
        public static EffecterDef MagChargeUpEffect;
        public static DamageDef HalfMagDashDamage;
        public static PawnKindDef HalfMag;
        public static PawnKindDef HalfMagPirate;
        public static ThinkTreeDef MagAgentAI;
        public static HediffDef MagHediff;
        public static BodyPartDef MagSkull;
        public static BodyPartDef MagRibcage;
        static MCHFDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MCHFDefOf));
        }   
    }
    
}
