using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;



/* RAMBLINGS
 * 
 * I didn't plan to use Harmony initially which is naive to say in hind sight
 * The harmony patches are to mostly make the Half-Mag work as a draftable ally pawn similar to how colony mechs work
 * though I did try to make it as compatible as I knew how by using just postfixes and trying to mess with as few things as possible
 * but of course issues can always happen, especially with IsColonistPlayerControlled and IsFreeNonSlaveColonist patches as they could have intended consequences
 * though things seem stable based on testing and a week long playthrough I did with other mods installed
 * hardest part was probably getting floatmenus to work (I looked at how Draftable Animals did that but there are better ways to do it I think), 
 * 
 * So basically I think this is a mess and I am gonna just use HAR and VFE and learn how to use transpilers when I redo this mod after I do other mods
 */



namespace MCHF
{
    [StaticConstructorOnStartup]
    public static class HalfMagModHarmonyPatches
    {

        static HalfMagModHarmonyPatches()
        {

            var harmony = new Harmony("HalfMag.Mod");
            harmony.PatchAll();
        }
        // copy pasted from the basegame since the og method is private
        private static void ValidateTakeToBedOption(Pawn pawn, Pawn target, FloatMenuOption option, string cannot, GuestStatus? guestStatus = null)
        {
            Building_Bed bedFor = RestUtility.FindBedFor(target, pawn, checkSocialProperness: false, ignoreOtherReservations: false, guestStatus);
            if (bedFor != null)
            {
                return;
            }
            bedFor = RestUtility.FindBedFor(target, pawn, checkSocialProperness: false, ignoreOtherReservations: true, guestStatus);
            if (bedFor != null)
            {
                if (pawn.MapHeld.reservationManager.TryGetReserver(bedFor, pawn.Faction, out var reserver))
                {
                    option.Label = option.Label + " (" + bedFor.def.label + " " + "ReservedBy".Translate(reserver.LabelShort, reserver).Resolve().StripTags() + ")";
                }
            }
            else
            {
                option.Disabled = true;
                option.Label = cannot;
            }
        }
        
        // lets game see custom race as controllable by player so they can have draft orders, gizmos, ect... 
        [HarmonyPatch(typeof(Pawn), "IsColonistPlayerControlled", MethodType.Getter)]
        public static class HalfMagPatch_IsColonistPlayerControlled
        {     
                [HarmonyPriority(10)]
                [HarmonyPostfix]
                public static void HalfMagIsPlayerControlled(Pawn __instance, ref bool __result)
                {
               
                    if (__instance.def.Equals(MCHFDefOf.HalfMagRace))
                    {
                        __result = __instance.Spawned && (__instance.Faction?.IsPlayer ?? false);
                    }
                }               
        }
        
       // oh my god this was retarded
            /*
            // uses reflection so method returns true for Half-Mag race if it's being called by specific methods
            // used so Half-Mags can appear in animaltab(RimWorld.MainTabWindow_Animals.Pawns aka <get_Pawns>b__3_0), can be fed as a patient, and be released
            [HarmonyPatch(typeof(Pawn), "IsNonMutantAnimal", MethodType.Getter)]
            public static class HalfMagPatch_IsNonMutantAnimal
            {
                [HarmonyPriority(10)]
                [HarmonyPostfix]
                public static void HalfMagSometimesNonMutantAnimal(ref bool __result, Pawn __instance)
                {  

                    if (__instance.def.Equals(MCHFDefOf.HalfMagRace) )
                    {
                    // TO-DO: see if there's a better way to do this with other reflection tools like Harmony's
                    // CHANGE THIS SHIT TO AN AUXIARLLY PATCH 
                        StackTrace stack = new StackTrace();
                        MethodBase caller = AccessTools.GetOutsideCaller(); 
                        // var getPawn = AccessTools.Method(typeof(MainTabWindow_Animals),"Pawns");
                        FileLog.Log("Caller Name: "+caller.Name);
                        // FileLog.Log("getPawn Name: " + getPawn.Name);
                        // FileLog.Log("Check if caller equal to getPawn: " + caller.Equals(getPawn));

                        string str = stack.GetFrame(2).GetMethod().Name;
                        string str2 = stack.GetFrame(2).GetMethod().DeclaringType.Name;
                        __result = __instance.Spawned && ( (MCHFMod.settings.enableAnimalTab && str.Equals("<get_Pawns>b__3_0")) || str2.Equals("Designator_ReleaseAnimalToWild")
                            || str2.Equals("WorkGiver_ReleaseAnimalsToWild") || str2.Equals("WorkGiver_FeedPatient"));

                    }
                }
            }
            */

        // so you can release Half-Mags from animal tab
        [HarmonyPatch(typeof(PawnColumnWorker_ReleaseAnimalToWild), "HasCheckbox")]
        public static class HalfMagPatch_PawnColumnWorker_ReleaseAnimalToWild
                {
                    [HarmonyPriority(11)]
                    [HarmonyPostfix]
                    public static void HalfMagReleasePawnTableCheckBox(ref bool __result, ref Pawn pawn)
                    {

                        if (pawn.def.Equals(MCHFDefOf.HalfMagRace))
                        {
                            __result = pawn.Faction == Faction.OfPlayer && pawn.SpawnedOrAnyParentSpawned;
                        }
                    }
                }
        // to allow Half-Mags to join caravans mainly
        [HarmonyPatch(typeof(Pawn), "IsFreeNonSlaveColonist", MethodType.Getter)]
        public static class HalfMagPatch_IsColonistPlayerControlled_IsFreeNonSlaveColonist
        {

            [HarmonyPriority(10)]
            [HarmonyPostfix]
            public static void HalfMagIsFreeNonSlave(ref bool __result, Pawn __instance)
            {

                if (__instance.def.Equals(MCHFDefOf.HalfMagRace))
                {
                    __result = __instance.Spawned && (__instance.Faction?.IsPlayer ?? false) && __instance.HostFaction == null;
                }

            }
        }  
        
        // copy pasted stuff from HumanLikeOrders so player Half-Mags can take forced orders like rescuing, firefighting, hauling ect...
        // if it's good enough for Draftable Animals then I assume it's good enough for my first ever mod but there is probably a better way to do this
        [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
        public static class HalfMagPatch_ChoicesAtFor
        {
            [HarmonyPriority(30)]
            [HarmonyPostfix]
            public static void AddFloatMenu(ref List<FloatMenuOption> __result, Vector3 clickPos, Pawn pawn, bool suppressAutoTakeableGoto = false)
            {
                IntVec3 clickCell = IntVec3.FromVector3(clickPos);
                if (pawn.def.Equals(MCHFDefOf.HalfMagRace) && clickCell.InBounds(pawn.Map) && pawn.Map == Find.CurrentMap)
                {
                        
                    if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && new TargetInfo(clickCell, pawn.Map).IsBurning())
                    {
                        FloatMenuOption floatMenuOption;
                        if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Firefighter))
                        {
                            WorkGiverDef fightFires = WorkGiverDefOf.FightFires;
                            floatMenuOption = new FloatMenuOption(string.Format("{0}: {1}", "CannotGenericWorkCustom".Translate(fightFires.label), "IncapableOf".Translate().CapitalizeFirst() + " " + WorkTypeDefOf.Firefighter.gerundLabel), null);
                        }
                        else
                        {
                            floatMenuOption = new FloatMenuOption("ExtinguishFiresNearby".Translate(), delegate
                            {
                                Job job3 = JobMaker.MakeJob(JobDefOf.ExtinguishFiresNearby);
                                foreach (Fire current5 in clickCell.GetFiresNearCell(pawn.Map))
                                {
                                    job3.AddQueuedTarget(TargetIndex.A, current5);
                                }
                                pawn.jobs.TryTakeOrderedJob(job3, JobTag.Misc);
                            });
                        }
                        __result.Add(floatMenuOption);
                    }
                    

                if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    foreach (LocalTargetInfo dest in GenUI.TargetsAt(clickPos, TargetingParameters.ForArrest(pawn), thingsOnly: true))
                    {
                        bool flag = dest.HasThing && dest.Thing is Pawn && ((Pawn)dest.Thing).IsWildMan();
                        if (!pawn.Drafted && !flag)
                        {
                            continue;
                        }
                        if (dest.Thing is Pawn && (pawn.InSameExtraFaction((Pawn)dest.Thing, ExtraFactionType.HomeFaction) || pawn.InSameExtraFaction((Pawn)dest.Thing, ExtraFactionType.MiniFaction)))
                        {
                            __result.Add(new FloatMenuOption("CannotArrest".Translate() + ": " + "SameFaction".Translate((Pawn)dest.Thing), null));
                            continue;
                        }
                        if (!pawn.CanReach(dest, PathEndMode.OnCell, Danger.Deadly))
                        {
                            __result.Add(new FloatMenuOption("CannotArrest".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                            continue;
                        }
                        Pawn pTarg = (Pawn)dest.Thing;
                        Action action = delegate
                        {
                            Building_Bed building_Bed4 = RestUtility.FindBedFor(pTarg, pawn, checkSocialProperness: false, ignoreOtherReservations: false, GuestStatus.Prisoner);
                            if (building_Bed4 == null)
                            {
                                building_Bed4 = RestUtility.FindBedFor(pTarg, pawn, checkSocialProperness: false, ignoreOtherReservations: true, GuestStatus.Prisoner);
                            }
                            if (building_Bed4 == null)
                            {
                                Messages.Message("CannotArrest".Translate() + ": " + "NoPrisonerBed".Translate(), pTarg, MessageTypeDefOf.RejectInput, historical: false);
                            }
                            else
                            {
                                Job job34 = JobMaker.MakeJob(JobDefOf.Arrest, pTarg, building_Bed4);
                                job34.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job34, JobTag.Misc);
                                if (pTarg.Faction != null && ((pTarg.Faction != Faction.OfPlayer && !pTarg.Faction.Hidden) || pTarg.IsQuestLodger()))
                                {
                                    TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.ArrestingCreatesEnemies, pTarg.GetAcceptArrestChance(pawn).ToStringPercent());
                                }
                            }
                        };
                        __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TryToArrest".Translate(dest.Thing.LabelCap, dest.Thing, pTarg.GetAcceptArrestChance(pawn).ToStringPercent()), action, MenuOptionPriority.High, null, dest.Thing), pawn, pTarg));
                    }
                }

                // order to eat

                foreach (Thing thing in clickCell.GetThingList(pawn.Map))
                {
                    Thing t = thing;
                    if ((!t.def.IsDrug && pawn.needs?.food == null) || t.def.ingestible == null || !t.def.ingestible.showIngestFloatOption || !pawn.RaceProps.CanEverEat(t) || !t.IngestibleNow)
                    {
                        continue;
                    }
                    string str = ((!t.def.ingestible.ingestCommandString.NullOrEmpty()) ? ((string)t.def.ingestible.ingestCommandString.Formatted(t.LabelShort)) : ((string)"ConsumeThing".Translate(t.LabelShort, t)));
                    // slight change here
                    if (!SocialProperness.IsSociallyProper(t, pawn, pawn.IsPrisonerOfColony, true))
                    {
                        str = str + ": " + "ReservedForPrisoners".Translate().CapitalizeFirst();
                    }
                    else if (FoodUtility.MoodFromIngesting(pawn, t, t.def) < 0f)
                    {
                        str = string.Format("{0}: ({1})", str, "WarningFoodDisliked".Translate());
                    }
                    if ((!t.def.IsDrug || !ModsConfig.IdeologyActive || new HistoryEvent(HistoryEventDefOf.IngestedDrug, pawn.Named(HistoryEventArgsNames.Doer)).Notify_PawnAboutToDo(out var opt, str) || PawnUtility.CanTakeDrugForDependency(pawn, t.def)) && (!t.def.IsNonMedicalDrug || !ModsConfig.IdeologyActive || new HistoryEvent(HistoryEventDefOf.IngestedRecreationalDrug, pawn.Named(HistoryEventArgsNames.Doer)).Notify_PawnAboutToDo(out opt, str) || PawnUtility.CanTakeDrugForDependency(pawn, t.def)) && (!t.def.IsDrug || !ModsConfig.IdeologyActive || t.def.ingestible.drugCategory != DrugCategory.Hard || new HistoryEvent(HistoryEventDefOf.IngestedHardDrug, pawn.Named(HistoryEventArgsNames.Doer)).Notify_PawnAboutToDo(out opt, str)))
                    {
                        if (t.def.IsNonMedicalDrug && !pawn.CanTakeDrug(t.def))
                        {
                            opt = new FloatMenuOption(str + ": " + TraitDefOf.DrugDesire.DataAtDegree(-1).GetLabelCapFor(pawn), null);
                        }
                        else if (FoodUtility.InappropriateForTitle(t.def, pawn, allowIfStarving: true))
                        {
                            opt = new FloatMenuOption(str + ": " + "FoodBelowTitleRequirements".Translate(pawn.royalty.MostSeniorTitle.def.GetLabelFor(pawn).CapitalizeFirst()).CapitalizeFirst(), null);
                        }
                        else if (!pawn.CanReach(t, PathEndMode.OnCell, Danger.Deadly))
                        {
                            opt = new FloatMenuOption(str + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                        }
                        else
                        {
                            MenuOptionPriority priority = ((t is Corpse) ? MenuOptionPriority.Low : MenuOptionPriority.Default);
                            int maxAmountToPickup = FoodUtility.GetMaxAmountToPickup(t, pawn, FoodUtility.WillIngestStackCountOf(pawn, t.def, FoodUtility.NutritionForEater(pawn, t)));
                            opt = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(str, delegate
                            {
                                int maxAmountToPickup2 = FoodUtility.GetMaxAmountToPickup(t, pawn, FoodUtility.WillIngestStackCountOf(pawn, t.def, FoodUtility.NutritionForEater(pawn, t)));
                                if (maxAmountToPickup2 != 0)
                                {
                                    t.SetForbidden(value: false);
                                    Job job33 = JobMaker.MakeJob(JobDefOf.Ingest, t);
                                    job33.count = maxAmountToPickup2;
                                    pawn.jobs.TryTakeOrderedJob(job33, JobTag.Misc);
                                }
                            }, priority), pawn, t);
                            if (maxAmountToPickup == 0)
                            {
                                opt.action = null;
                            }
                        }
                    }
                    __result.Add(opt);
                }

                // order to save pawn for quest or incident?
                foreach (LocalTargetInfo dest in GenUI.TargetsAt(clickPos, TargetingParameters.ForQuestPawnsWhoWillJoinColony(pawn), thingsOnly: true))
                {
                    Pawn toHelpPawn = (Pawn)dest.Thing;
                    FloatMenuOption floatMenuOption = (pawn.CanReach(dest, PathEndMode.Touch, Danger.Deadly) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(toHelpPawn.IsPrisoner ? "FreePrisoner".Translate() : "OfferHelp".Translate(), delegate
                    {
                        pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.OfferHelp, toHelpPawn), JobTag.Misc);
                    }, MenuOptionPriority.RescueOrCapture, null, toHelpPawn), pawn, toHelpPawn) : new FloatMenuOption("CannotGoNoPath".Translate(), null));
                    __result.Add(floatMenuOption);
                }

                // general stuff for capturing, hauling to containers, rescueing, ect... includes dlc stuff
                ChildcareUtility.BreastfeedFailReason? reason1;
                if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    List<Thing> thingList = clickCell.GetThingList(pawn.Map);
                    foreach (Thing thing in thingList)
                    {
                        Corpse corpse;
                        if ((corpse = thing as Corpse) == null || !corpse.IsInValidStorage())
                        {
                            continue;
                        }
                        StoragePriority priority = StoreUtility.CurrentHaulDestinationOf(corpse).GetStoreSettings().Priority;
                        Building_Grave grave;
                        if (StoreUtility.TryFindBestBetterNonSlotGroupStorageFor(corpse, pawn, pawn.Map, priority, Faction.OfPlayer, out var haulDestination, acceptSamePriority: true) && haulDestination.GetStoreSettings().Priority == priority && (grave = haulDestination as Building_Grave) != null)
                        {
                            __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PrioritizeGeneric".Translate("Burying".Translate(), corpse.Label).CapitalizeFirst(), delegate
                            {
                                pawn.jobs.TryTakeOrderedJob(HaulAIUtility.HaulToContainerJob(pawn, corpse, grave), JobTag.Misc);
                            }), pawn, new LocalTargetInfo(corpse)));
                        }
                    }
                    foreach (Thing thing in thingList)
                    {
                        Corpse corpse = thing as Corpse;
                        if (corpse == null)
                        {
                            continue;
                        }
                        Building_GibbetCage cage = Building_GibbetCage.FindGibbetCageFor(corpse, pawn);
                        if (cage != null)
                        {
                            __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PlaceIn".Translate(corpse, cage), delegate
                            {
                                pawn.jobs.TryTakeOrderedJob(HaulAIUtility.HaulToContainerJob(pawn, corpse, cage), JobTag.Misc);
                            }), pawn, new LocalTargetInfo(corpse)));
                        }
                        if (ModsConfig.BiotechActive && corpse.InnerPawn.health.hediffSet.HasHediff(HediffDefOf.MechlinkImplant))
                        {
                            __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Extract".Translate() + " " + HediffDefOf.MechlinkImplant.label, delegate
                            {
                                Job job32 = JobMaker.MakeJob(JobDefOf.RemoveMechlink, corpse);
                                pawn.jobs.TryTakeOrderedJob(job32, JobTag.Misc);
                            }), pawn, new LocalTargetInfo(corpse)));
                        }
                    }
                    foreach (LocalTargetInfo localTargetInfo in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), thingsOnly: true))
                    {
                        Pawn victim = (Pawn)localTargetInfo.Thing;
                        if (!HealthAIUtility.CanRescueNow(pawn, victim, forced: true) || victim.mindState.WillJoinColonyIfRescued)
                        {
                            continue;
                        }
                        FloatMenuOption option;
                        string cannot;
                        if (!victim.IsPrisonerOfColony && !victim.IsSlaveOfColony && !victim.IsColonyMech)
                        {
                            bool isBaby = ChildcareUtility.CanSuckle(victim, out reason1);
                            if (victim.Faction == Faction.OfPlayer || victim.Faction == null || !victim.Faction.HostileTo(Faction.OfPlayer) || isBaby)
                            {
                                option = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption((HealthAIUtility.ShouldSeekMedicalRest(victim) || !victim.ageTracker.CurLifeStage.alwaysDowned) ? "Rescue".Translate(victim.LabelCap, victim) : "PutSomewhereSafe".Translate(victim.LabelCap, victim), delegate
                                {
                                    if (isBaby)
                                    {
                                        pawn.jobs.TryTakeOrderedJob(ChildcareUtility.MakeBringBabyToSafetyJob(pawn, victim), JobTag.Misc);
                                    }
                                    else
                                    {
                                        Building_Bed building_Bed3 = RestUtility.FindBedFor(victim, pawn, checkSocialProperness: false);
                                        if (building_Bed3 == null)
                                        {
                                            building_Bed3 = RestUtility.FindBedFor(victim, pawn, checkSocialProperness: false, ignoreOtherReservations: true);
                                        }
                                        if (building_Bed3 == null)
                                        {
                                            string text = ((!victim.RaceProps.Animal) ? ((string)"NoNonPrisonerBed".Translate()) : ((string)"NoAnimalBed".Translate()));
                                            Messages.Message("CannotRescue".Translate() + ": " + text, victim, MessageTypeDefOf.RejectInput, historical: false);
                                        }
                                        else
                                        {
                                            Job job31 = JobMaker.MakeJob(JobDefOf.Rescue, victim, building_Bed3);
                                            job31.count = 1;
                                            pawn.jobs.TryTakeOrderedJob(job31, JobTag.Misc);
                                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                                        }
                                    }
                                }, MenuOptionPriority.RescueOrCapture, null, victim), pawn, victim);
                                if (!isBaby)
                                {
                                    string key = (victim.RaceProps.Animal ? "NoAnimalBed" : "NoNonPrisonerBed");
                                    cannot = string.Format("{0}: {1}", "CannotRescue".Translate(), key.Translate().CapitalizeFirst());
                                    ValidateTakeToBedOption(pawn, victim, option, cannot);
                                }
                                __result.Add(option);
                            }
                        }
                        if (victim.IsSlaveOfColony && !victim.InMentalState)
                        {
                            option = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("ReturnToSlaveBed".Translate(), delegate
                            {
                                Building_Bed building_Bed2 = RestUtility.FindBedFor(victim, pawn, checkSocialProperness: false, ignoreOtherReservations: false, GuestStatus.Slave);
                                if (building_Bed2 == null)
                                {
                                    building_Bed2 = RestUtility.FindBedFor(victim, pawn, checkSocialProperness: false, ignoreOtherReservations: true, GuestStatus.Slave);
                                }
                                if (building_Bed2 == null)
                                {
                                    Messages.Message(string.Format("{0}: {1}", "CannotRescue".Translate(), "NoSlaveBed".Translate()), victim, MessageTypeDefOf.RejectInput, historical: false);
                                }
                                else
                                {
                                    Job job30 = JobMaker.MakeJob(JobDefOf.Rescue, victim, building_Bed2);
                                    job30.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job30, JobTag.Misc);
                                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                                }
                            }, MenuOptionPriority.RescueOrCapture, null, victim), pawn, victim);
                            cannot = string.Format("{0}: {1}", "CannotRescue".Translate(), "NoSlaveBed".Translate());
                            ValidateTakeToBedOption(pawn, victim, option, cannot, GuestStatus.Slave);
                            __result.Add(option);
                        }
                        if (!victim.CanBeCaptured())
                        {
                            continue;
                        }
                        TaggedString label = "Capture".Translate(victim.LabelCap, victim);
                        if (!victim.guest.Recruitable)
                        {
                            label += " (" + "Unrecruitable".Translate() + ")";
                        }
                        if (victim.Faction != null && victim.Faction != Faction.OfPlayer && !victim.Faction.Hidden && !victim.Faction.HostileTo(Faction.OfPlayer) && !victim.IsPrisonerOfColony)
                        {
                            label += ": " + "AngersFaction".Translate().CapitalizeFirst();
                        }
                        option = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, delegate
                        {
                            Building_Bed building_Bed = RestUtility.FindBedFor(victim, pawn, checkSocialProperness: false, ignoreOtherReservations: false, GuestStatus.Prisoner);
                            if (building_Bed == null)
                            {
                                building_Bed = RestUtility.FindBedFor(victim, pawn, checkSocialProperness: false, ignoreOtherReservations: true, GuestStatus.Prisoner);
                            }
                            if (building_Bed == null)
                            {
                                Messages.Message("CannotCapture".Translate() + ": " + "NoPrisonerBed".Translate(), victim, MessageTypeDefOf.RejectInput, historical: false);
                            }
                            else
                            {
                                Job job29 = JobMaker.MakeJob(JobDefOf.Capture, victim, building_Bed);
                                job29.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job29, JobTag.Misc);
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
                                if (victim.Faction != null && victim.Faction != Faction.OfPlayer && !victim.Faction.Hidden && !victim.Faction.HostileTo(Faction.OfPlayer) && !victim.IsPrisonerOfColony)
                                {
                                    Messages.Message("MessageCapturingWillAngerFaction".Translate(victim.Named("PAWN")).AdjustedFor(victim), victim, MessageTypeDefOf.CautionInput, historical: false);
                                }
                            }
                        }, MenuOptionPriority.RescueOrCapture, null, victim), pawn, victim);
                        cannot = string.Format("{0}: {1}", "CannotCapture".Translate(), "NoPrisonerBed".Translate());
                        ValidateTakeToBedOption(pawn, victim, option, cannot, GuestStatus.Prisoner);
                        __result.Add(option);
                    }
                    foreach (LocalTargetInfo item2 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), thingsOnly: true))
                    {
                        LocalTargetInfo localTargetInfo2 = item2;
                        Pawn victim = (Pawn)localTargetInfo2.Thing;
                        if (!victim.Downed || !pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true) || Building_CryptosleepCasket.FindCryptosleepCasketFor(victim, pawn, ignoreOtherReservations: true) == null)
                        {
                            continue;
                        }
                        string label1 = "CarryToCryptosleepCasket".Translate(localTargetInfo2.Thing.LabelCap, localTargetInfo2.Thing);
                        JobDef jDef = JobDefOf.CarryToCryptosleepCasket;
                        Action action = delegate
                        {
                            Building_CryptosleepCasket building_CryptosleepCasket = Building_CryptosleepCasket.FindCryptosleepCasketFor(victim, pawn);
                            if (building_CryptosleepCasket == null)
                            {
                                building_CryptosleepCasket = Building_CryptosleepCasket.FindCryptosleepCasketFor(victim, pawn, ignoreOtherReservations: true);
                            }
                            if (building_CryptosleepCasket == null)
                            {
                                Messages.Message("CannotCarryToCryptosleepCasket".Translate() + ": " + "NoCryptosleepCasket".Translate(), victim, MessageTypeDefOf.RejectInput, historical: false);
                            }
                            else
                            {
                                Job job28 = JobMaker.MakeJob(jDef, victim, building_CryptosleepCasket);
                                job28.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job28, JobTag.Misc);
                            }
                        };
                        if (victim.IsQuestLodger())
                        {
                            label1 += " (" + "CryptosleepCasketGuestsNotAllowed".Translate() + ")";
                            __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label1, null, MenuOptionPriority.Default, null, victim), pawn, victim));
                        }
                        else if (victim.GetExtraHostFaction() != null)
                        {
                            label1 += " (" + "CryptosleepCasketGuestPrisonersNotAllowed".Translate() + ")";
                            __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label1, null, MenuOptionPriority.Default, null, victim), pawn, victim));
                        }
                        else
                        {
                            __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label1, action, MenuOptionPriority.Default, null, victim), pawn, victim));
                        }
                    }
                    if (ModsConfig.AnomalyActive && pawn.ageTracker.AgeBiologicalYears >= 10)
                    {
                        foreach (LocalTargetInfo localTargetInfo in GenUI.TargetsAt(clickPos, TargetingParameters.ForEntityCapture(), thingsOnly: true))
                        {
                            Thing studyTarget = localTargetInfo.Thing;
                            CompHoldingPlatformTarget holdComp = studyTarget.TryGetComp<CompHoldingPlatformTarget>();
                            if (holdComp == null || !holdComp.StudiedAtHoldingPlatform || !holdComp.CanBeCaptured)
                            {
                                continue;
                            }
                            if (!pawn.CanReserveAndReach(studyTarget, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
                            {
                                __result.Add(new FloatMenuOption("CannotGenericWorkCustom".Translate("CaptureLower".Translate(studyTarget)) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                                continue;
                            }
                            IEnumerable<Building_HoldingPlatform> buildingHoldingPlatforms = from x in pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_HoldingPlatform>()
                                                                                                where !x.Occupied && pawn.CanReserveAndReach(x, PathEndMode.Touch, Danger.Deadly)
                                                                                                select x;
                            Thing building = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, buildingHoldingPlatforms, PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Some), 9999f, null, delegate (Thing t)
                            {
                                CompEntityHolder compEntityHolder2 = t.TryGetComp<CompEntityHolder>();
                                return (compEntityHolder2 != null && compEntityHolder2.ContainmentStrength >= studyTarget.GetStatValue(StatDefOf.MinimumContainmentStrength)) ? (compEntityHolder2.ContainmentStrength / Mathf.Max(studyTarget.PositionHeld.DistanceTo(t.Position), 1f)) : 0f;
                            });
                            if (building != null)
                            {
                                __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Capture".Translate(studyTarget.Label, studyTarget), delegate
                                {
                                    if (!ContainmentUtility.SafeContainerExistsFor(studyTarget))
                                    {
                                        Messages.Message("MessageNoRoomWithMinimumContainmentStrength".Translate(studyTarget.Label), MessageTypeDefOf.ThreatSmall);
                                    }
                                    holdComp.targetHolder = building;
                                    Job job27 = JobMaker.MakeJob(JobDefOf.CarryToEntityHolder, building, studyTarget);
                                    job27.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job27, JobTag.Misc);
                                }), pawn, studyTarget));
                                if (buildingHoldingPlatforms.Count() > 1)
                                {
                                    __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Capture".Translate(studyTarget.Label, studyTarget) + " (" + "ChooseEntityHolder".Translate() + "...)", delegate
                                    {
                                        StudyUtility.TargetHoldingPlatformForEntity(pawn, studyTarget);
                                    }), pawn, studyTarget));
                                }
                            }
                            else
                            {
                                __result.Add(new FloatMenuOption("CannotGenericWorkCustom".Translate("CaptureLower".Translate(studyTarget)) + ": " + "NoHoldingPlatformsAvailable".Translate().CapitalizeFirst(), null));
                            }
                        }
                        foreach (LocalTargetInfo localTargetInfo in GenUI.TargetsAt(clickPos, TargetingParameters.ForHeldEntity(), thingsOnly: true))
                        {
                            Building_HoldingPlatform holdingPlatform;
                            if ((holdingPlatform = localTargetInfo.Thing as Building_HoldingPlatform) == null)
                            {
                                continue;
                            }
                            Pawn heldPawn = holdingPlatform.HeldPawn;
                            if (heldPawn != null && pawn.CanReserveAndReach(holdingPlatform, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true) && GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_HoldingPlatform>(), PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Some), 9999f, delegate (Thing b)
                            {
                                if (!(b is Building_HoldingPlatform building_HoldingPlatform))
                                {
                                    return false;
                                }
                                if (building_HoldingPlatform.Occupied)
                                {
                                    return false;
                                }
                                return pawn.CanReserve(building_HoldingPlatform) ? true : false;
                            }, delegate (Thing t)
                            {
                                CompEntityHolder compEntityHolder = t.TryGetComp<CompEntityHolder>();
                                return (compEntityHolder != null && compEntityHolder.ContainmentStrength >= heldPawn.GetStatValue(StatDefOf.MinimumContainmentStrength)) ? (compEntityHolder.ContainmentStrength / Mathf.Max(heldPawn.PositionHeld.DistanceTo(t.Position), 1f)) : 0f;
                            }) != null)
                            {
                                __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TransferEntity".Translate(heldPawn) + " (" + "ChooseEntityHolder".Translate() + "...)", delegate
                                {
                                    StudyUtility.TargetHoldingPlatformForEntity(pawn, heldPawn, transferBetweenPlatforms: true, holdingPlatform);
                                }), pawn, holdingPlatform));
                            }
                        }
                    }
                }

                // stripping people 
                foreach (LocalTargetInfo localTargetInfo in GenUI.TargetsAt(clickPos, TargetingParameters.ForStrip(pawn), thingsOnly: true))
                {
                    LocalTargetInfo stripTarg = localTargetInfo;
                    FloatMenuOption floatMenuOption = (pawn.CanReach(stripTarg, PathEndMode.ClosestTouch, Danger.Deadly) ? ((stripTarg.Pawn != null && stripTarg.Pawn.HasExtraHomeFaction()) ? new FloatMenuOption("CannotStrip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null) : (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Strip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing), delegate
                    {
                        stripTarg.Thing.SetForbidden(value: false, warnOnFail: false);
                        pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Strip, stripTarg), JobTag.Misc);
                        StrippableUtility.CheckSendStrippingImpactsGoodwillMessage(stripTarg.Thing);
                    }), pawn, stripTarg) : new FloatMenuOption("CannotStrip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing) + ": " + "Incapable".Translate().CapitalizeFirst(), null))) : new FloatMenuOption("CannotStrip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                    __result.Add(floatMenuOption);
                }

                // equipping shit

                if (pawn.equipment != null)
                {
                    List<Thing> thingList = clickCell.GetThingList(pawn.Map);
                    for (int index = 0; index < thingList.Count; index++)
                    {
                        if (thingList[index].TryGetComp<CompEquippable>() == null)
                        {
                            continue;
                        }
                        ThingWithComps equipment = (ThingWithComps)thingList[index];
                        string labelShort = equipment.LabelShort;
                        FloatMenuOption floatMenuOption;
                        string cantReason;
                        if (equipment.def.IsWeapon && pawn.WorkTagIsDisabled(WorkTags.Violent))
                        {
                            floatMenuOption = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn), null);
                        }
                        else if (equipment.def.IsRangedWeapon && pawn.WorkTagIsDisabled(WorkTags.Shooting))
                        {
                            floatMenuOption = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "IsIncapableOfShootingLower".Translate(pawn), null);
                        }
                        else if (!pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly))
                        {
                            floatMenuOption = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                        }
                        else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                        {
                            floatMenuOption = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "Incapable".Translate().CapitalizeFirst(), null);
                        }
                        else if (equipment.IsBurning())
                        {
                            floatMenuOption = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "BurningLower".Translate(), null);
                        }
                        else if (pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanEquip(equipment, pawn))
                        {
                            floatMenuOption = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null);
                        }
                        else if (!EquipmentUtility.CanEquip(equipment, pawn, out cantReason, checkBonded: false))
                        {
                            floatMenuOption = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + cantReason.CapitalizeFirst(), null);
                        }
                        else
                        {
                            string label4 = "Equip".Translate(labelShort);
                            if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
                            {
                                label4 += " " + "EquipWarningBrawler".Translate();
                            }
                            if (EquipmentUtility.AlreadyBondedToWeapon(equipment, pawn))
                            {
                                label4 += " " + "BladelinkAlreadyBonded".Translate();
                                TaggedString dialogText = "BladelinkAlreadyBondedDialog".Translate(pawn.Named("PAWN"), equipment.Named("WEAPON"), pawn.equipment.bondedWeapon.Named("BONDEDWEAPON"));
                                floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label4, delegate
                                {
                                    Find.WindowStack.Add(new Dialog_MessageBox(dialogText));
                                }, MenuOptionPriority.High), pawn, equipment);
                            }
                            else
                            {
                                floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label4, delegate
                                {
                                    string personaWeaponConfirmationText = EquipmentUtility.GetPersonaWeaponConfirmationText(equipment, pawn);
                                    if (!personaWeaponConfirmationText.NullOrEmpty())
                                    {
                                        Find.WindowStack.Add(new Dialog_MessageBox(personaWeaponConfirmationText, "Yes".Translate(), delegate
                                        {
                                            Equip();
                                        }, "No".Translate()));
                                    }
                                    else
                                    {
                                        Equip();
                                    }
                                }, MenuOptionPriority.High), pawn, equipment);
                            }
                        }
                        __result.Add(floatMenuOption);
                        void Equip()
                        {
                            equipment.SetForbidden(value: false);
                            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Equip, equipment), JobTag.Misc);
                            FleckMaker.Static(equipment.DrawPos, equipment.MapHeld, FleckDefOf.FeedbackEquip);
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
                        }
                    }
                }
                // reloading stuff like motoars
                foreach (Pair<IReloadableComp, Thing> pair in ReloadableUtility.FindPotentiallyReloadableGear(pawn, clickCell.GetThingList(pawn.Map)))
                {
                    IReloadableComp reloadable = pair.First;
                    Thing second = pair.Second;
                    ThingComp thingComp = reloadable as ThingComp;
                    string label = "Reload".Translate(thingComp.parent.Named("GEAR"), NamedArgumentUtility.Named(reloadable.AmmoDef, "AMMO")) + " (" + reloadable.LabelRemaining + ")";
                    if (!pawn.CanReach(second, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        __result.Add(new FloatMenuOption(label + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                        continue;
                    }
                    if (!reloadable.NeedsReload(allowForceReload: true))
                    {
                        __result.Add(new FloatMenuOption(label + ": " + "ReloadFull".Translate(), null));
                        continue;
                    }
                    List<Thing> chosenAmmo;
                    if ((chosenAmmo = ReloadableUtility.FindEnoughAmmo(pawn, second.Position, reloadable, forceReload: true)) == null)
                    {
                        __result.Add(new FloatMenuOption(label + ": " + "ReloadNotEnough".Translate(), null));
                        continue;
                    }
                    if (pawn.carryTracker.AvailableStackSpace(reloadable.AmmoDef) < reloadable.MinAmmoNeeded(allowForcedReload: true))
                    {
                        __result.Add(new FloatMenuOption(label + ": " + "ReloadCannotCarryEnough".Translate(NamedArgumentUtility.Named(reloadable.AmmoDef, "AMMO")), null));
                        continue;
                    }
                    Action action = delegate
                    {
                        pawn.jobs.TryTakeOrderedJob(JobGiver_Reload.MakeReloadJob(reloadable, chosenAmmo), JobTag.Misc);
                    };
                    __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, action), pawn, second));
                }
                // caravan shit
                if (pawn.IsFormingCaravan())
                {
                    foreach (Thing item in clickCell.GetItems(pawn.Map))
                    {
                        if (!item.def.EverHaulable || !item.def.canLoadIntoCaravan)
                        {
                            continue;
                        }
                        Pawn packTarget = GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace(pawn) ?? pawn;
                        JobDef jobDef = ((packTarget == pawn) ? JobDefOf.TakeInventory : JobDefOf.GiveToPackAnimal);
                        if (!pawn.CanReach(item, PathEndMode.ClosestTouch, Danger.Deadly))
                        {
                            __result.Add(new FloatMenuOption("CannotLoadIntoCaravan".Translate(item.Label, item) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                            continue;
                        }
                        if (MassUtility.WillBeOverEncumberedAfterPickingUp(packTarget, item, 1))
                        {
                            __result.Add(new FloatMenuOption("CannotLoadIntoCaravan".Translate(item.Label, item) + ": " + "TooHeavy".Translate(), null));
                            continue;
                        }
                        LordJob_FormAndSendCaravan lordJob = (LordJob_FormAndSendCaravan)pawn.GetLord().LordJob;
                        float capacityLeft = CaravanFormingUtility.CapacityLeft(lordJob);
                        if (item.stackCount == 1)
                        {
                            float capacityLeft1 = capacityLeft - item.GetStatValue(StatDefOf.Mass);
                            __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(CaravanFormingUtility.AppendOverweightInfo("LoadIntoCaravan".Translate(item.Label, item), capacityLeft1), delegate
                            {
                                item.SetForbidden(value: false, warnOnFail: false);
                                Job job17 = JobMaker.MakeJob(jobDef, item);
                                job17.count = 1;
                                job17.checkEncumbrance = packTarget == pawn;
                                pawn.jobs.TryTakeOrderedJob(job17, JobTag.Misc);
                            }, MenuOptionPriority.High), pawn, item));
                            continue;
                        }
                        if (MassUtility.WillBeOverEncumberedAfterPickingUp(packTarget, item, item.stackCount))
                        {
                            __result.Add(new FloatMenuOption("CannotLoadIntoCaravanAll".Translate(item.Label, item) + ": " + "TooHeavy".Translate(), null));
                        }
                        else
                        {
                            float capacityLeft2 = capacityLeft - (float)item.stackCount * item.GetStatValue(StatDefOf.Mass);
                            __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(CaravanFormingUtility.AppendOverweightInfo("LoadIntoCaravanAll".Translate(item.Label, item), capacityLeft2), delegate
                            {
                                item.SetForbidden(value: false, warnOnFail: false);
                                Job job16 = JobMaker.MakeJob(jobDef, item);
                                job16.count = item.stackCount;
                                job16.checkEncumbrance = packTarget == pawn;
                                pawn.jobs.TryTakeOrderedJob(job16, JobTag.Misc);
                            }, MenuOptionPriority.High), pawn, item));
                        }
                        __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("LoadIntoCaravanSome".Translate(item.LabelNoCount, item), delegate
                        {
                            int to3 = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(packTarget, item), item.stackCount);
                            Dialog_Slider window3 = new Dialog_Slider(delegate (int val)
                            {
                                float capacityLeft3 = capacityLeft - (float)val * item.GetStatValue(StatDefOf.Mass);
                                return CaravanFormingUtility.AppendOverweightInfo("LoadIntoCaravanCount".Translate(item.LabelNoCount, item).Formatted(val), capacityLeft3);
                            }, 1, to3, delegate (int count)
                            {
                                item.SetForbidden(value: false, warnOnFail: false);
                                Job job15 = JobMaker.MakeJob(jobDef, item);
                                job15.count = count;
                                job15.checkEncumbrance = packTarget == pawn;
                                pawn.jobs.TryTakeOrderedJob(job15, JobTag.Misc);
                            });
                            Find.WindowStack.Add(window3);
                        }, MenuOptionPriority.High), pawn, item));
                    }
                }
                if (!pawn.IsFormingCaravan())
                {
                    foreach (Thing item in clickCell.GetItems(pawn.Map))
                    {
                        if (!item.def.EverHaulable || !PawnUtility.CanPickUp(pawn, item.def) || (pawn.Map.IsPlayerHome && !JobGiver_DropUnusedInventory.ShouldKeepDrugInInventory(pawn, item)))
                        {
                            continue;
                        }
                        if (!pawn.CanReach(item, PathEndMode.ClosestTouch, Danger.Deadly))
                        {
                            __result.Add(new FloatMenuOption("CannotPickUp".Translate(item.Label, item) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                            continue;
                        }
                        if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, item, 1))
                        {
                            __result.Add(new FloatMenuOption("CannotPickUp".Translate(item.Label, item) + ": " + "TooHeavy".Translate(), null));
                            continue;
                        }
                        int maxAllowedToPickUp = PawnUtility.GetMaxAllowedToPickUp(pawn, item.def);
                        if (maxAllowedToPickUp == 0)
                        {
                            __result.Add(new FloatMenuOption("CannotPickUp".Translate(item.Label, item) + ": " + "MaxPickUpAllowed".Translate(item.def.orderedTakeGroup.max, item.def.orderedTakeGroup.label), null));
                            continue;
                        }
                        if (item.stackCount == 1 || maxAllowedToPickUp == 1)
                        {
                            __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpOne".Translate(item.LabelNoCount, item), delegate
                            {
                                item.SetForbidden(value: false, warnOnFail: false);
                                Job job14 = JobMaker.MakeJob(JobDefOf.TakeInventory, item);
                                job14.count = 1;
                                job14.checkEncumbrance = true;
                                job14.takeInventoryDelay = 120;
                                pawn.jobs.TryTakeOrderedJob(job14, JobTag.Misc);
                            }, MenuOptionPriority.High), pawn, item));
                            continue;
                        }
                        if (maxAllowedToPickUp < item.stackCount)
                        {
                            __result.Add(new FloatMenuOption("CannotPickUpAll".Translate(item.Label, item) + ": " + "MaxPickUpAllowed".Translate(item.def.orderedTakeGroup.max, item.def.orderedTakeGroup.label), null));
                        }
                        else if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, item, item.stackCount))
                        {
                            __result.Add(new FloatMenuOption("CannotPickUpAll".Translate(item.Label, item) + ": " + "TooHeavy".Translate(), null));
                        }
                        else
                        {
                            __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpAll".Translate(item.Label, item), delegate
                            {
                                item.SetForbidden(value: false, warnOnFail: false);
                                Job job13 = JobMaker.MakeJob(JobDefOf.TakeInventory, item);
                                job13.count = item.stackCount;
                                job13.checkEncumbrance = true;
                                job13.takeInventoryDelay = 120;
                                pawn.jobs.TryTakeOrderedJob(job13, JobTag.Misc);
                            }, MenuOptionPriority.High), pawn, item));
                        }
                        __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpSome".Translate(item.LabelNoCount, item), delegate
                        {
                            int b2 = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(pawn, item), item.stackCount);
                            int to2 = Mathf.Min(maxAllowedToPickUp, b2);
                            Dialog_Slider window2 = new Dialog_Slider("PickUpCount".Translate(item.LabelNoCount, item), 1, to2, delegate (int count)
                            {
                                item.SetForbidden(value: false, warnOnFail: false);
                                Job job12 = JobMaker.MakeJob(JobDefOf.TakeInventory, item);
                                job12.count = count;
                                job12.checkEncumbrance = true;
                                job12.takeInventoryDelay = 120;
                                pawn.jobs.TryTakeOrderedJob(job12, JobTag.Misc);
                            });
                            Find.WindowStack.Add(window2);
                        }, MenuOptionPriority.High), pawn, item));
                    }
                }
                if (!pawn.Map.IsPlayerHome && !pawn.IsFormingCaravan())
                {
                    foreach (Thing item in clickCell.GetItems(pawn.Map))
                    {
                        if (!item.def.EverHaulable)
                        {
                            continue;
                        }
                        Pawn bestPackAnimal = GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace(pawn);
                        if (bestPackAnimal == null)
                        {
                            continue;
                        }
                        if (!pawn.CanReach(item, PathEndMode.ClosestTouch, Danger.Deadly))
                        {
                            __result.Add(new FloatMenuOption("CannotGiveToPackAnimal".Translate(item.Label, item) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                            continue;
                        }
                        if (MassUtility.WillBeOverEncumberedAfterPickingUp(bestPackAnimal, item, 1))
                        {
                            __result.Add(new FloatMenuOption("CannotGiveToPackAnimal".Translate(item.Label, item) + ": " + "TooHeavy".Translate(), null));
                            continue;
                        }
                        if (item.stackCount == 1)
                        {
                            __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("GiveToPackAnimal".Translate(item.Label, item), delegate
                            {
                                item.SetForbidden(value: false, warnOnFail: false);
                                Job job11 = JobMaker.MakeJob(JobDefOf.GiveToPackAnimal, item);
                                job11.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job11, JobTag.Misc);
                            }, MenuOptionPriority.High), pawn, item));
                            continue;
                        }
                        if (MassUtility.WillBeOverEncumberedAfterPickingUp(bestPackAnimal, item, item.stackCount))
                        {
                            __result.Add(new FloatMenuOption("CannotGiveToPackAnimalAll".Translate(item.Label, item) + ": " + "TooHeavy".Translate(), null));
                        }
                        else
                        {
                            __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("GiveToPackAnimalAll".Translate(item.Label, item), delegate
                            {
                                item.SetForbidden(value: false, warnOnFail: false);
                                Job job10 = JobMaker.MakeJob(JobDefOf.GiveToPackAnimal, item);
                                job10.count = item.stackCount;
                                pawn.jobs.TryTakeOrderedJob(job10, JobTag.Misc);
                            }, MenuOptionPriority.High), pawn, item));
                        }
                        __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("GiveToPackAnimalSome".Translate(item.LabelNoCount, item), delegate
                        {
                            int to = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(bestPackAnimal, item), item.stackCount);
                            Dialog_Slider window = new Dialog_Slider("GiveToPackAnimalCount".Translate(item.LabelNoCount, item), 1, to, delegate (int count)
                            {
                                item.SetForbidden(value: false, warnOnFail: false);
                                Job job9 = JobMaker.MakeJob(JobDefOf.GiveToPackAnimal, item);
                                job9.count = count;
                                pawn.jobs.TryTakeOrderedJob(job9, JobTag.Misc);
                            });
                            Find.WindowStack.Add(window);
                        }, MenuOptionPriority.High), pawn, item));
                    }
                }
                if (!pawn.Map.IsPlayerHome && pawn.Map.exitMapGrid.MapUsesExitGrid)
                {
                    foreach (LocalTargetInfo target in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), thingsOnly: true))
                    {
                        Pawn p = (Pawn)target.Thing;
                        if (p.Faction != Faction.OfPlayer && !p.IsPrisonerOfColony && !CaravanUtility.ShouldAutoCapture(p, Faction.OfPlayer))
                        {
                            continue;
                        }
                        IntVec3 exitSpot;
                        if (!pawn.CanReach(p, PathEndMode.ClosestTouch, Danger.Deadly))
                        {
                            __result.Add(new FloatMenuOption("CannotCarryToExit".Translate(p.Label, p) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                        }
                        else if (pawn.Map.IsPocketMap)
                        {
                            if (!RCellFinder.TryFindExitPortal(pawn, out var portal))
                            {
                                __result.Add(new FloatMenuOption("CannotCarryToExit".Translate(p.Label, p) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                                continue;
                            }
                            TaggedString label = ((p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony) ? "CarryToExit".Translate(p.Label, p) : "CarryToExitAndCapture".Translate(p.Label, p));
                            __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, delegate
                            {
                                Job job8 = JobMaker.MakeJob(JobDefOf.CarryDownedPawnToPortal, portal, p);
                                job8.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job8, JobTag.Misc);
                            }, MenuOptionPriority.High), pawn, target));
                        }
                        else if (!RCellFinder.TryFindBestExitSpot(pawn, out exitSpot))
                        {
                            __result.Add(new FloatMenuOption("CannotCarryToExit".Translate(p.Label, p) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                        }
                        else
                        {
                            TaggedString label = ((p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony) ? "CarryToExit".Translate(p.Label, p) : "CarryToExitAndCapture".Translate(p.Label, p));
                            __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, delegate
                            {
                                Job job7 = JobMaker.MakeJob(JobDefOf.CarryDownedPawnToExit, p, exitSpot);
                                job7.count = 1;
                                job7.failIfCantJoinOrCreateCaravan = true;
                                pawn.jobs.TryTakeOrderedJob(job7, JobTag.Misc);
                            }, MenuOptionPriority.High), pawn, target));
                        }
                    }
                }

                // dropping stuff
                if (pawn.equipment != null && pawn.equipment.Primary != null && GenUI.TargetsAt(clickPos, TargetingParameters.ForSelf(pawn), thingsOnly: true).Any())
                {
                    if (pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanUnequip(pawn.equipment.Primary, pawn))
                    {
                        __result.Add(new FloatMenuOption("CannotDrop".Translate(pawn.equipment.Primary.Label, pawn.equipment.Primary) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null));
                    }
                    else
                    {
                        Action action = delegate
                        {
                            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DropEquipment, pawn.equipment.Primary), JobTag.Misc);
                        };
                        __result.Add(new FloatMenuOption("Drop".Translate(pawn.equipment.Primary.Label, pawn.equipment.Primary), action, MenuOptionPriority.Default, null, pawn));
                    }
                }
                // opening caskets
                foreach (LocalTargetInfo casket in GenUI.TargetsAt(clickPos, TargetingParameters.ForOpen(pawn), thingsOnly: true))
                {
                    if (!pawn.CanReach(casket, PathEndMode.OnCell, Danger.Deadly))
                    {
                        __result.Add(new FloatMenuOption("CannotOpen".Translate(casket.Thing) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                    }
                    else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    {
                        __result.Add(new FloatMenuOption("CannotOpen".Translate(casket.Thing) + ": " + "Incapable".Translate().CapitalizeFirst(), null));
                    }
                    else if (casket.Thing.Map.designationManager.DesignationOn(casket.Thing, DesignationDefOf.Open) == null)
                    {
                        __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Open".Translate(casket.Thing), delegate
                        {
                            Job job4 = JobMaker.MakeJob(JobDefOf.Open, casket.Thing);
                            job4.ignoreDesignations = true;
                            pawn.jobs.TryTakeOrderedJob(job4, JobTag.Misc);
                        }, MenuOptionPriority.High), pawn, casket.Thing));
                    }
                }

                // FIREFIGHTING
                if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && new TargetInfo(clickCell, pawn.Map).IsBurning())
                {
                    FloatMenuOption floatMenuOption;
                    if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Firefighter))
                    {
                        WorkGiverDef fightFires = WorkGiverDefOf.FightFires;
                        floatMenuOption = new FloatMenuOption(string.Format("{0}: {1}", "CannotGenericWorkCustom".Translate(fightFires.label), "IncapableOf".Translate().CapitalizeFirst() + " " + WorkTypeDefOf.Firefighter.gerundLabel), null);
                    }
                    else
                    {
                        floatMenuOption = new FloatMenuOption("ExtinguishFiresNearby".Translate(), delegate
                        {
                            Job job3 = JobMaker.MakeJob(JobDefOf.ExtinguishFiresNearby);
                            foreach (Fire current5 in clickCell.GetFiresNearCell(pawn.Map))
                            {
                                job3.AddQueuedTarget(TargetIndex.A, current5);
                            }
                            pawn.jobs.TryTakeOrderedJob(job3, JobTag.Misc);
                        });
                    }
                    __result.Add(floatMenuOption);
                }

                // CLEANING
                if (!pawn.Drafted && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Cleaning))
                {
                    Room room = clickCell.GetRoom(pawn.Map);
                    if (room != null && room.ProperRoom && !room.PsychologicallyOutdoors && !room.TouchesMapEdge)
                    {
                        IEnumerable<Filth> filth = CleanRoomFilthUtility.GetRoomFilthCleanableByPawn(clickCell, pawn);
                        if (!filth.EnumerableNullOrEmpty())
                        {
                            string roomRoleLabel = room.GetRoomRoleLabel();
                            __result.Add(new FloatMenuOption("CleanRoom".Translate(roomRoleLabel), delegate
                            {
                                Job job2 = JobMaker.MakeJob(JobDefOf.Clean);
                                foreach (Filth current4 in filth)
                                {
                                    job2.AddQueuedTarget(TargetIndex.A, current4);
                                }
                                pawn.jobs.TryTakeOrderedJob(job2, JobTag.Misc);
                            }));
                        }
                    }
                }
            }
            }
        }


        // this was to have Half-Mags be included in the colonst bar. After a month of trying to get this to work as I learned harmony and the game's ui code
        // I gave up especially since it wasn't neccessary and the main solution was raw reflection during the patch's execution which ate up preformance.
        // Half-mags are not full blown colonist but more like colony mechs who have their own tab anyway
        /*
        // doesn't include half-mags as a "freecolonist" for all calls execpt ColonistBar.CheckRecacheEntries so they can be in colonist bar w/o causing issues
        [HarmonyPatch(typeof(MapPawns), "FreeHumanlikesOfFaction")]
        public static class HalfMagPatch_FreeHumanlikesOfFaction
        {
            [HarmonyPriority(30)]
            [HarmonyPostfix]
            public static void AddHalfMagFreeHumanlike(List<Pawn> __result, MapPawns __instance, Faction faction)
            {
                if (MCHFMod.settings.enableColonistBar && (new System.Diagnostics.StackTrace()).GetFrame(3).GetMethod().Name.Equals("CheckRecacheEntries"))
                {
                    IReadOnlyList<Pawn> allSpawnedPawns = __instance.AllPawnsSpawned;

                    foreach (Pawn pawn in allSpawnedPawns.Where<Pawn>(pawn => (pawn.def.Equals(MCHFDefOf.HalfMagRace) && pawn.Faction == faction)))
                    {
                        __result.Add(pawn);
                    }
                }
            }
        }
        */

        // gives player Half-Mag comps needed to do work and be draftable
        [HarmonyPatch(typeof(PawnComponentsUtility), "AddAndRemoveDynamicComponents")]
        public static class HalfMagPatch_AddAndRemoveDynamicComponents
        {
            [HarmonyPriority(30)]
            [HarmonyPostfix]

            public static void HalfMagPawnComps(ref Pawn pawn, ref bool actAsIfSpawned)
            {
                bool flag = pawn.Faction != null && pawn.Faction.IsPlayer;
                if (pawn.def.Equals(MCHFDefOf.HalfMagRace) && flag)
                {          
                    if ( actAsIfSpawned && pawn.drafter == null)
                    {
                        pawn.drafter = new Pawn_DraftController(pawn);
                      
                    }
                    if (pawn.workSettings == null)
                    {
                        pawn.workSettings = new Pawn_WorkSettings(pawn);
                    }
                       
                } 


            }
        }  
    }
}
