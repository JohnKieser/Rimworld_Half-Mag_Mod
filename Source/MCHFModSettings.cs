
using UnityEngine;
using Verse;

namespace MCHF
{
    public class MCHFModSettings : ModSettings
    {
        
        public bool enableEarlyGameHalfMagAttack;


        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableEarlyGameHalfMagAttack, "HalfMagColonistBar", true);
            base.ExposeData();
        }
        public void UpdateSettings()
        {
        }


    }

    public class MCHFMod : Mod
    {
   
        public static MCHFModSettings settings;

 
        public MCHFMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<MCHFModSettings>();
        }

  
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("MCHF_HalfMagIntroSettingEnable".Translate(), ref settings.enableEarlyGameHalfMagAttack, "MCHF_HalfMagIntroSettingToolTip".Translate());
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "MCHF_SettingsCategory".Translate();
        }
        public override void WriteSettings()
        {
            base.WriteSettings();
            settings.UpdateSettings();
        }
    }
}
