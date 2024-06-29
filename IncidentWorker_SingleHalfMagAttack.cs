using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MCHF
{
    // event that is supposed to fire once bring under StorytellerCompProperties_SingleOnceFixed in StorytellerDef
    // happens in first 10 days of a new game as a cool tutorial and to let you know the mod is working or not
    public class IncidentWorker_SingleHalfMagAttack : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return MCHFMod.settings.enableEarlyGameHalfMagAttack;
        }


        protected override bool TryExecuteWorker(IncidentParms parms)
        {

            Map map = (Map)parms.target;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out var result, map, CellFinder.EdgeRoadChance_Hostile))
            {
                return false;
            }
            Pawn pawn = PawnGenerator.GeneratePawn(MCHFDefOf.HalfMagPirate, Faction.OfPirates);
            if (pawn == null)
            {
                return false;
            }

            Rot4 rot = Rot4.FromAngleFlat((map.Center - result).AngleFlat);
            GenSpawn.Spawn(pawn, result, map, rot);
        
            SendIncidentLetter(def.letterLabel, def.letterText.Formatted(pawn.Faction.Named("FACTION")), LetterDefOf.ThreatBig, parms, pawn, def);
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            LordMaker.MakeNewLord(parms.faction, new LordJob_AssaultColony(Faction.OfPirates, canKidnap: false, canTimeoutOrFlee: false, sappers: false, useAvoidGridSmart: false, canSteal: false), map, Gen.YieldSingle(pawn));
            return true;


        }

    }
}
