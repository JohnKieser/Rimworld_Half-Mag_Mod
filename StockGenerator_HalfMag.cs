using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MCHF
{
    // traders can sell Half-Mags
    public class StockGenerator_HalfMag : StockGenerator
    {

        public override IEnumerable<Thing> GenerateThings(int forTile, Faction faction = null)
        {
            int count = countRange.RandomInRange;

            for (int i = 0; i < count; i++)
            {
                PawnGenerationRequest request = new PawnGenerationRequest(MCHFDefOf.HalfMag);
                yield return PawnGenerator.GeneratePawn(request);
            }
        
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            return thingDef.Equals(MCHFDefOf.HalfMagRace);
        }
    }
}
