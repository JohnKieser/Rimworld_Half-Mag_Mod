using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;
using Verse.AI;

namespace MCHF
{

    [HarmonyPatch]
    // Includes Half-Mag in Animal tab
    public static class HalfMagPatch_MainTabWindow_Animals
    {
        public static MethodBase TargetMethod()
        {
            
            Type[] nestedTypes = typeof(MainTabWindow_Animals).GetNestedTypes(AccessTools.all);
            foreach (Type type in nestedTypes)
            {
                MethodInfo[] methods = type.GetMethods(AccessTools.all);
                foreach (MethodInfo methodInfo in methods)
                {
                    if (methodInfo.Name.Contains("<get_Pawns>b__3_0"))
                    {
                        return methodInfo;

                    }
                }
            }

            return null;
        }


        
        [HarmonyPostfix]
        [HarmonyPriority(10)]
        public static void PostFix(ref Pawn p, ref bool __result)
        {
            if (p.def.Equals(MCHFDefOf.HalfMagRace))
            {
                __result = p.Faction == Faction.OfPlayer;
            }
        }

    }

    [HarmonyPatch(typeof(WorkGiver_ReleaseAnimalsToWild), "HasJobOnThing")]
    // Allows pawns to "release" colony Half-Mags 
    public static class HalfMagPatch_ReleaseAnimalsToWild
    {


        [HarmonyPostfix]
        [HarmonyPriority(10)]
        public static void HalfMag_HasJobOnThing(ref bool __result, ref Pawn pawn, ref Thing t, ref bool forced)
        {
            if ((t is Pawn) && (t as Pawn).def.Equals(MCHFDefOf.HalfMagRace))
            {
                Pawn pawn2 = t as Pawn; 
                if (! ((pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.ReleaseAnimalToWild) == null) || (pawn.Faction != t.Faction)
                        || (pawn2.InAggroMentalState || pawn2.Dead) || (!pawn.CanReserve(t, 1, -1, null, forced))
                        || (!JobDriver_ReleaseAnimalToWild.TryFindClosestOutsideCell_NewTemp(t.Position, t.Map, TraverseParms.For(pawn), pawn, out var _))) )
                {
                    __result = true;
                }
               
            }
        }

    }

    
    [HarmonyPatch(typeof(Designator_ReleaseAnimalToWild), "CanDesignateThing")]
    // Allows Half-Mags to have the release animal designation 
    public static class HalfMagPatch_Designator_ReleaseAnimalToWild
    {

        [HarmonyPostfix]
        [HarmonyPriority(9)]
        public static AcceptanceReport PostFix(AcceptanceReport result, ref Thing t, Designator_ReleaseAnimalToWild __instance )
        {
            if ((t is Pawn) && (t as Pawn).def.Equals(MCHFDefOf.HalfMagRace)) {

                Pawn pawn = t as Pawn;

                if (pawn.Faction == Faction.OfPlayer && __instance.Map.designationManager.DesignationOn(t, DesignationDefOf.ReleaseAnimalToWild) == null && !pawn.Dead && pawn.RaceProps.canReleaseToWild)
                {
                    return AcceptanceReport.WasAccepted;
                }
            }
            return result;
        }
    }
}
