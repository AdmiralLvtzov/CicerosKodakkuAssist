using System;
using System.Collections.Concurrent;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Script;
using KodakkuAssist.Module.Draw;
using System.Collections.Generic;
using System.Diagnostics;
using ECommons;
using System.Numerics;
using Newtonsoft.Json;
using Dalamud.Utility.Numerics;
using ECommons.MathHelpers;
using KodakkuAssist.Module.GameOperate;
using Lumina.Data.Parsing;
using Newtonsoft.Json.Linq;

namespace CicerosKodakkuAssist.Arcadion.Savage
{
    
    [ScriptType(name:"AAC Cruiserweight M4 (Savage)",
        territorys:[1263],
        guid:"aeb4391c-e8a6-4daa-ab71-18e44c94fab8",
        version:"0.0.0.32",
        note:scriptNotes,
        author:"Cicero 灵视")]

    public class AAC_Cruiserweight_M4_Savage
    {
        
        public const string scriptNotes=
            """
            This is the English version of the script for AAC Cruiserweight M4 (Savage), which is also known as M8S in short.
            
            The script has been completed, at least for all the popular strats among EU Party Finder.
            If you encounter any issue, please report to @_publius_cornelius_scipio_ in Kodakku Assist Discord.
            
            Link to RaidPlan 84d for Phase 1: https://raidplan.io/plan/B5Q3Mk62YKuTy84d
            Link to Toxic Friends RaidPlan DOG for Phase 2: https://raidplan.io/plan/9M-1G-mmOaaroDOG
            
            The "Half Rinon" strat during Terrestrial Rage is the one combines the first half of Rinon with the second half of the clock strat.
            You could check Hector's M8S video guide for more details.
            
            The guidance of the "Northwest and South" strat during Ultraviolent Ray 4 is based on real-time positions of players.
            Please make sure to get to the correct platform and form conga as quickly as possible.
            """;

        #region User_Settings

        [UserSetting("----- Global Settings ----- (This setting has no practical meaning.)")]
        public bool _____Global_Settings_____ { get; set; } = false;
        
        [UserSetting("Enbale Text Prompts")]
        public bool enablePrompts { get; set; } = true;
        [UserSetting("Enable Vanilla TTS")]
        public bool enableVanillaTts { get; set; } = true;
        [UserSetting("Enable Daily Routines TTS (It requires the plugin \"Daily Routines\" to be installed and enabled!)")]
        public bool enableDailyRoutinesTts { get; set; } = false;
        [UserSetting("Colour Of Direction Indicators")]
        public ScriptColor colourOfDirectionIndicators { get; set; } = new() { V4 = new Vector4(1,1,0, 1) }; // Yellow by default.
        [UserSetting("Colour Of Highly Dangerous Attacks")]
        public ScriptColor colourOfHighlyDangerousAttacks { get; set; } = new() { V4 = new Vector4(1,0,0,1) }; // Red by default.
        [UserSetting("Enable Shenanigans")]
        public bool enableShenanigans { get; set; } = false;
        
        [UserSetting("----- Phase 1 Settings ----- (This setting has no practical meaning.)")]
        public bool _____Phase_1_Settings_____ { get; set; } = false;
        
        [UserSetting("Strats Of Millennial Decay")]
        public StratsOfMillennialDecay stratOfMillennialDecay { get; set; }
        [UserSetting("Tanks Or Melee Go Further For The Second Set While Doing RaidPlan 84d During Millennial Decay")]
        public bool meleeGoFurther { get; set; } = true;
        [UserSetting("Strats Of Terrestrial Rage")]
        public StratsOfTerrestrialRage stratOfTerrestrialRage { get; set; }
        [UserSetting("Strats Of Beckon Moonlight")]
        public StratsOfBeckonMoonlight stratOfBeckonMoonlight { get; set; }
        
        [UserSetting("----- Phase 2 Settings ----- (This setting has no practical meaning.)")]
        public bool _____Phase_2_Settings_____ { get; set; } = false;
        
        [UserSetting("Strats Of Phase 2")]
        public StratsOfPhase2 stratOfPhase2 { get; set; }
        [UserSetting("Colour Of The North-south Axis And Two Combined Arrows")]
        public ScriptColor colourOfTheNorthSouthAxis { get; set; } = new() { V4 = new Vector4(0,1,1, 1) }; // Blue by default.
        [UserSetting("Arrows Of The North-south Axis Point South Instead Of North")]
        public bool arrowsPointSouth { get; set; } = false;
        [UserSetting("Strats Of Ultraviolent Ray 4")]
        public StratsOfUltraviolentRay4 stratOfUltraviolentRay4 { get; set; }

        #endregion
        
        #region Variables
        
        private volatile int currentPhase=1;
        private volatile int currentSubPhase=1;
        
        /*
         
         Phase 1:
         
         Sub-phase 1: The first half of Millennial Decay
         Sub-phase 2: The second half of Millennial Decay
         Sub-phase 3: Terrestrial Titans
         Sub-phase 4: intermission regins
         Sub-phase 5: Tactical Pack
         Sub-phase 6: Terrestrial Rage
         Sub-phase 7: intermission regins
         Sub-phase 8: Beckon Moonlight
         Sub-phase 9: intermission fangs
         
         Phase 2:
         
         Sub-phase 1: Elemental Purge
         Sub-phase 2: Twofold Tempest
         Sub-phase 3: Champion's Circuit
         Sub-phase 4: Lone Wolf's Lament
         Sub-phase 5: Enrage
         
        */
        
        private volatile string reignId=string.Empty;

        private volatile int numberOfWindWolfLines=0;
        private Vector3 positionOfTheFirstWindWolf=ARENA_CENTER_OF_PHASE_1;
        private volatile bool windWolvesStartFromTheNorth=false;
        private volatile bool windWolvesRotateClockwise=false;
        private System.Threading.AutoResetEvent windWolfRotationSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile int roundOfGust=0;
        private volatile bool gustMarksSupporters=false;
        private System.Threading.AutoResetEvent gustFirstSetSemaphore=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent gustSecondSetSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile int roundOfGustApplied=0;

        private volatile List<ulong> windWolfTethersHaveBeenDrawn=[];
        private volatile int numberOfWindWolfTethers=0;
        private volatile List<int> getWindWolfTethers=[-1,-1,-1,-1,-1,-1,-1,-1];
        private volatile bool windWolvesAreOnTheCardinals=false;
        private System.Threading.AutoResetEvent windWolfTetherSemaphore=new System.Threading.AutoResetEvent(false);

        private volatile List<int> riskIndexOfIntercardinals=[0,0,0,0]; // Northeast, southeast, southwest, northwest accordingly.
        private System.Threading.AutoResetEvent terrestrialTitansSemaphore=new System.Threading.AutoResetEvent(false);

        private Vector3 positionOfTheWindWolfAdd=ARENA_CENTER_OF_PHASE_1;
        private volatile bool addsRotateClockwise=false;
        private System.Threading.AutoResetEvent addRotationSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile bool addRotationHasBeenDrawn=false;
        private ulong? idOfTheWindWolfAdd=null,idOfTheStoneWolfAdd=null,idOfTheWindFont=null,idOfTheEarthFont=null;
        private volatile List<bool> windpackWasApplied=[false,false,false,false,false,false,false,false];
        private volatile List<bool> windborneEndWasApplied=[false,false,false,false,false,false,false,false];
        private volatile List<int> roundForCleanse=[-1,-1,-1,-1,-1,-1,-1,-1];
        private volatile int currentAddRound=0;
        private volatile bool windGuidanceHasBeenDrawn=false,earthGuidanceHasBeenDrawn=false;

        private Vector3 positionOfTheOuterFang=ARENA_CENTER_OF_PHASE_1;
        private double rotationOfTheOuterFang=0;
        private volatile bool outerFangHasBeenCaptured=false;
        private System.Threading.AutoResetEvent newNorthArrowSemaphore=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent newNorthGuidanceSemaphore=new System.Threading.AutoResetEvent(false);
        private bool? dpsStackFirst=null;
        private System.Threading.AutoResetEvent roleStackSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile bool firstSetGuidanceHasBeenDrawn=false;
        private volatile bool shadowsAreOnTheCardinals=false; // Its read-write lock is lockOfShadowNumber.
        private volatile int numberOfShadows=0; // Its read-write lock is lockOfShadowNumber.
        private double refinedRotationForFullRinon=double.PositiveInfinity; // Its read-write lock is lockOfShadowNumber.
        private System.Threading.AutoResetEvent shadowSemaphore=new System.Threading.AutoResetEvent(false);

        private volatile List<List<int>> moonbeamsBite=[]; // 0=Northeast, 1=southeast, 2=southwest, 3=northwest.
        private System.Threading.AutoResetEvent secondMoonbeamsBiteSemaphore=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent fourthMoonbeamsBiteSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile bool secondSetGuidanceHasBeenDrawn=false;
        private volatile bool stoneWolvesAreOnTheCardinals=false;
        
        private volatile bool axisAndArrowsHaveBeenDrawn=false;
        
        private volatile int roundOfQuakeIii=0;

        private volatile int numberOfUltraviolentRay=0;
        private volatile List<bool> playerWasMarkedByAUltraviolentRay=[false,false,false,false,false,false,false,false];
        private System.Threading.AutoResetEvent ultraviolentRaySemaphore=new System.Threading.AutoResetEvent(false);
        private volatile int roundOfUltraviolentRay=0;

        private volatile int roundOfTwinbite=0;
        
        private volatile int roundOfHerosBlow=0;

        private volatile bool mtWasMarkerByPatienceOfWind=false;
        private System.Threading.AutoResetEvent elementalPurgeSemaphore=new System.Threading.AutoResetEvent(false);

        private ulong? currentTempestStackTarget=null;  // Its read-write lock is lockOfTempestStackTarget.
        private volatile PlatformsOfPhase2 beginningPlatform=PlatformsOfPhase2.SOUTH;
        private volatile bool tetherBeginsFromTheWest=false;
        private System.Threading.AutoResetEvent tempestLineSemaphore=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent tempestGuidanceSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile int roundOfTwofoldTempest=0;
        private System.Threading.AutoResetEvent tetherLeavingSemaphore=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent tetherCapturingSemaphore=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent round2Semaphore=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent round3Semaphore=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent round4Semaphore=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent tempestEndSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile int numberOfSteps=65300;

        private volatile int numberOfLamentTethers=0; // Its read-write lock is lockOfLamentData.
        private volatile int dpsWithTheFarTank=-1,dpsWithTheCloseTank=-1,dpsWithTheFarHealer=-1,dpsWithTheCloseHealer=-1; // Its read-write lock is lockOfLamentData.
        private volatile int tankWithTheFarDps=-1,tankWithTheCloseDps=-1,healerWithTheFarDps=-1,healerWithTheCloseDps=-1; // Its read-write lock is lockOfLamentData.
        private System.Threading.AutoResetEvent lamentSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile bool twoPlayerTowerIsOnTheWest=false;
        private System.Threading.AutoResetEvent northTowerSemaphore=new System.Threading.AutoResetEvent(false);

        #endregion

        #region Constants

        private static readonly Vector3 ARENA_CENTER_OF_PHASE_1=new Vector3(100,0,100);
        
        private readonly Object lockOfShadowNumber=new Object();
        
        private static readonly Vector3 ARENA_CENTER_OF_PHASE_2=new Vector3(100,-150,100);
        private static readonly Vector3 RAW_PLATFORM_CENTER=rotatePosition(new Vector3(100,-150,82.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5);
        // The center of the south platform is 100,-150,117.5.
        // The radius of a platform is 8.
        
        private readonly Object lockOfTempestStackTarget=new Object();
        
        private readonly Object lockOfLamentData=new Object();

        #endregion
        
        #region Enumerations_And_Classes

        public enum StratsOfMillennialDecay {

            Rinon_Or_RaidPlan_84d,
            // Ferring,
            // Murderless_Ferring
            Other_Strats_Are_Work_In_Progress

        }
        
        public enum StratsOfTerrestrialRage {

            Full_Rinon_Or_RaidPlan_84d,
            Half_Rinon,
            // Clock
            Other_Strats_Are_Work_In_Progress

        }
        
        public enum StratsOfBeckonMoonlight {

            Quad_Or_RaidPlan_84d,
            // Rinon,
            // Toxic_Friends_RaidPlan_XOs
            Other_Strats_Are_Work_In_Progress

        }
        
        public enum StratsOfPhase2 {

            Toxic_Friends_RaidPlan_DOG,
            // Rinon
            Other_Strats_Are_Work_In_Progress

        }

        public enum StratsOfUltraviolentRay4 {
            
            Same_As_Usual,
            Northwest_And_South
            
        }

        public enum PlatformsOfPhase2 {
            
            NORTHEAST=0,
            SOUTHEAST=1,
            SOUTH=2,
            SOUTHWEST=3,
            NORTHWEST=4
            
        }

        #endregion
        
        #region Initialization

        public void Init(ScriptAccessory accessory) {
            
            accessory.Method.RemoveDraw(".*");
            
            currentPhase=1;
            currentSubPhase=1; // Remember to dial it back after tests!
            
            reignId=string.Empty;
            
            numberOfWindWolfLines=0;
            positionOfTheFirstWindWolf=ARENA_CENTER_OF_PHASE_1;
            windWolvesStartFromTheNorth=false;
            windWolvesRotateClockwise=false;
            windWolfRotationSemaphore.Reset();
            roundOfGust=0;
            gustMarksSupporters=false;
            gustFirstSetSemaphore.Reset();
            gustSecondSetSemaphore.Reset();
            roundOfGustApplied=0;
            
            windWolfTethersHaveBeenDrawn=[];
            numberOfWindWolfTethers=0;
            getWindWolfTethers=[-1,-1,-1,-1,-1,-1,-1,-1];
            windWolvesAreOnTheCardinals=false;
            windWolfTetherSemaphore.Reset();
            
            riskIndexOfIntercardinals=[0,0,0,0];
            terrestrialTitansSemaphore.Reset();
            
            positionOfTheWindWolfAdd=ARENA_CENTER_OF_PHASE_1;
            addsRotateClockwise=false;
            addRotationSemaphore.Reset();
            addRotationHasBeenDrawn=false;
            idOfTheWindWolfAdd=null; idOfTheStoneWolfAdd=null; idOfTheWindFont=null; idOfTheEarthFont=null;
            windpackWasApplied=[false,false,false,false,false,false,false,false];
            windborneEndWasApplied=[false,false,false,false,false,false,false,false]; 
            roundForCleanse=[-1,-1,-1,-1,-1,-1,-1,-1];
            currentAddRound=0;
            windGuidanceHasBeenDrawn=false; earthGuidanceHasBeenDrawn=false;

            positionOfTheOuterFang=ARENA_CENTER_OF_PHASE_1;
            rotationOfTheOuterFang=0;
            outerFangHasBeenCaptured=false;
            newNorthArrowSemaphore.Reset();
            newNorthGuidanceSemaphore.Reset();
            dpsStackFirst=null;
            roleStackSemaphore.Reset();
            firstSetGuidanceHasBeenDrawn=false;
            shadowsAreOnTheCardinals=false;
            numberOfShadows=0;
            refinedRotationForFullRinon=double.PositiveInfinity;
            shadowSemaphore.Reset();
            
            moonbeamsBite=[];
            secondMoonbeamsBiteSemaphore.Reset();
            fourthMoonbeamsBiteSemaphore.Reset();
            secondSetGuidanceHasBeenDrawn=false;
            stoneWolvesAreOnTheCardinals=false;
            
            axisAndArrowsHaveBeenDrawn=false;
            
            roundOfQuakeIii=0;
            
            numberOfUltraviolentRay=0;
            playerWasMarkedByAUltraviolentRay=[false,false,false,false,false,false,false,false];
            ultraviolentRaySemaphore.Reset();
            roundOfUltraviolentRay=0;
            
            roundOfTwinbite=0;

            roundOfHerosBlow=0;

            mtWasMarkerByPatienceOfWind=false;
            elementalPurgeSemaphore.Reset();
            
            currentTempestStackTarget=null;
            beginningPlatform=PlatformsOfPhase2.SOUTH;
            tetherBeginsFromTheWest=false;
            tempestLineSemaphore.Reset();
            tempestGuidanceSemaphore.Reset();
            roundOfTwofoldTempest=0; 
            tetherLeavingSemaphore.Reset(); 
            tetherCapturingSemaphore.Reset();
            round2Semaphore.Reset();
            round3Semaphore.Reset();
            round4Semaphore.Reset();
            tempestEndSemaphore.Reset();
            numberOfSteps=65300;
            
            numberOfLamentTethers=0;
            dpsWithTheFarTank=-1;dpsWithTheCloseTank=-1;dpsWithTheFarHealer=-1;dpsWithTheCloseHealer=-1;
            tankWithTheFarDps=-1;tankWithTheCloseDps=-1;healerWithTheFarDps=-1;healerWithTheCloseDps=-1;
            lamentSemaphore.Reset();
            twoPlayerTowerIsOnTheWest=false;
            northTowerSemaphore.Reset();
            
            shenaniganSemaphore.Set();
            
            baseIdOfTargetIcon=null;

        }

        #endregion
        
        #region Shenanigans
        
        private System.Threading.AutoResetEvent shenaniganSemaphore=new System.Threading.AutoResetEvent(false);
        private static IReadOnlyList<string> quotes=[
            "Del Giordano le rive saluta, di Sionne le torri atterrate...",
            "Through the graves the wind is blowing.",
            "The enslaved were not bricks in your road, and their lives were not chapters in your redemptive history.",
            "Thou hast made us for thyself, O Lord, and our heart is restless until it finds its rest in thee.",
            "No! I'm alive! I will live forever! I have in my heart what does not die!",
            "The living denied a table; the dead get a whole coffin.",
            "What was born by the sword shall die by the sword.",
            "Injustice anywhere is a threat to justice everywhere.",
            "I die without seeing the dawn brighten over my native land.",
            "I entered a kind world and loved it wholeheartedly. I leave in an evil one and have nothing to say by way of farewells.",
            "You cannot nurture a man with pain, nor can you feed him with anger.",
            "\"Hemos pasado!\"",
            "The Banteng has been led to slaughter - and the villagers feast on its remnants.",
            "Those who wear the shirt of fire will realize it burns as much as it warms.",
            "What is built on sand sooner or later would tumble down.",
            "A faithful man shall abound with blessings.",
            "She smiled sadly, as she flew into the night.",
            "Only in death does duty end.",
            "The end may justify the means as long as there is something that justifies the end.",
            "Sing your death song and die like a hero going home.",
            "The mutineers ride into the night.",
            "The specter of homicidal violence has appeared in history whenever it was believed that the hypocritical respect for formalities could replace the obedience of moral obligations.",
            "Nothing more cruel and inhuman than a war. Nothing more desirable than peace. But peace has its causes, it is an effect. The effect of respect for mutual rights.",
            "One by one the righteous fell, and the ills of ignorance permeated.",
            "They defended the grains of sand in the desert to the last drop of their blood.",
            "All history is man's efforts to realise ideals.\n- Éamon de Valera, 1929",
            "Let us dedicate ourselves to what the Greeks wrote so many years ago: to tame the savageness of man and make gentle the life of this world.\n- Robert F. Kennedy, 1968",
            "Yesterday is not ours to recover, but tomorrow is ours to win or lose.\n- Lyndon B. Johnson, 1964",
            "The end of hope is the beginning of death.\n- Charles de Gaulle, 1945",
            "The day I leave the power, inside my pockets will only be dust.\n- Antonio de Oliveira Salazar, 1968",
            "When smashing monuments, save the pedestals. They always come in handy.\n- Stanisław Jerzy Lec, 1957",
            "Fear not the path of truth for the lack of people walking on it.\n- Robert F. Kennedy, 1968",
            "The rocket worked perfectly, except for landing on the wrong planet.\n- Wernher von Braun upon the first V-2 hitting London, 1944",
            "A man is not finished when he's defeated. He's finished when he quits.\n- Richard Nixon, 1962",
            "Do not pray for easy lives, pray to be stronger men.\n- John F. Kennedy, 1963",
            "Nature does not know extinction, only transformation.\n- Wernher von Braun, 1962",
            "The optimist thinks this is the best of all possible worlds. The pessimist fears it is true.\n- James Branch Cabell, The Silver Stallion, 1926",
            "One seldom recognizes the devil when he is putting his hand on your shoulder.\n- Albert Speer, 1972",
            "Laws are silent in times of war.\n- Marcus Tullius Cicero, 52 BC",
            "They don't ask much of you. They only want you to hate the things you love and to love the things you despise.\n- Boris Pasternak, 1960",
            "Most economic fallacies derive from the tendency to assume that there is a fixed pie, that one party can gain only at the expense of another.\n- Milton Friedman, 1980",
            "There are three kinds of lies: lies, damned lies, and statistics.\n- Mark Twain, 1907",
            "Bite us once, shame on the dog; bite us repeatedly, shame on us for allowing it.\n- Phyllis Schlafly, 1995",
            "I know not with what weapons World War III will be fought, but World War IV will be fought with sticks and stones.\n- Albert Einstein, 1949",
            "You can believe in Feng Shui if you want, but ultimately people control their own fate.\n- Li Ka-shing, 1969",
            "I believe it is a big mistake to think that money is the only way to compensate a person for his work. People need money, but they also want to be happy in their work and proud of it.\n- Morita Akio, 1966",
            "A good reputation for yourself and your company is an invaluable asset not reflected in the balance sheets.\n- Li Ka-shing, 1967",
            "Knowledge is your real companion, your life long companion, not fortune. Fortune can disappear.\n- Stanley Ho, 1966",
            "People sometimes say: \"we are in a society that is all rotten, all dishonest.\" That is not true. There are still so many good people, so many honest people.\n- John Paul I, 1978",
            "Half the confusion in the world comes from not knowing how little we need.\n- Admiral Richard E. Byrd on his time in Antarctica, 1935"
        ];

        [ScriptMethod(name:"Shenanigans",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:18215"],
            suppress:10000,
            userControl:false)]

        public void Shenanigans(Event @event,ScriptAccessory accessory) {

            shenaniganSemaphore.WaitOne();

            System.Threading.Thread.MemoryBarrier();
            
            if(!enableShenanigans) {

                return;

            }

            System.Threading.Thread.Sleep(3000);
            
            System.Threading.Thread.MemoryBarrier();
            
            string prompt=quotes[new System.Random().Next(0,quotes.Count)];

            if(!string.IsNullOrWhiteSpace(prompt)) {

                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,10000);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }

        }

        #endregion
        
        #region Phase_1

        [ScriptMethod(name:"Phase 1 Windfang And Stonefang",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(41885|41886|41889|41890)$"])]
    
        public void Phase_1_Windfang_And_Stonefang(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
            
            IReadOnlyList<int> getPartner=[6,5,4,7,2,1,0,3];
        
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            string prompt=string.Empty;
            
            // 41885: Donut & Stack + Cardinal Lines
            // 41886: Donut & Stack + Intercardinal Lines
            // 41889: Circle & Spread + Cardinal Lines
            // 41890: Circle & Spread + Intercardinal Lines

            if(string.Equals(@event["ActionId"],"41885")||string.Equals(@event["ActionId"],"41886")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(15);
                currentProperties.InnerScale=new(8);
                currentProperties.Radian=float.Pi*2;
                currentProperties.Owner=sourceId;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
                
                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

                if(!isLegalIndex(myIndex)) {

                    return;

                }
                
                for(int i=0;i<8;++i) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(15);
                    currentProperties.Owner=sourceId;
                    currentProperties.TargetObject=accessory.Data.PartyList[i];
                    currentProperties.Radian=24f.DegToRad();
                    currentProperties.DestoryAt=6000;

                    if(i==myIndex||i==getPartner[myIndex]) {
                        
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        
                    }

                    else {
                        
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        
                    }
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                    
                }

                prompt+="Get in and stack at the ";

            }
            
            if(string.Equals(@event["ActionId"],"41889")||string.Equals(@event["ActionId"],"41890")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(9);
                currentProperties.Owner=sourceId;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                for(int i=0;i<8;++i) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(15);
                    currentProperties.Owner=sourceId;
                    currentProperties.TargetObject=accessory.Data.PartyList[i];
                    currentProperties.Radian=24f.DegToRad();
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=6000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                    
                }

                prompt+="Get out and spread at the ";

            }
            
            if(string.Equals(@event["ActionId"],"41885")||string.Equals(@event["ActionId"],"41889")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(6,30);
                currentProperties.Position=ARENA_CENTER_OF_PHASE_1;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(6,30);
                currentProperties.Rotation=float.Pi/2;
                currentProperties.Position=ARENA_CENTER_OF_PHASE_1;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);

                prompt+="intercardinal.";

            }
            
            if(string.Equals(@event["ActionId"],"41886")||string.Equals(@event["ActionId"],"41890")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(6,30);
                currentProperties.Rotation=float.Pi/4;
                currentProperties.Position=ARENA_CENTER_OF_PHASE_1;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(6,30);
                currentProperties.Rotation=float.Pi/2+float.Pi/4;
                currentProperties.Position=ARENA_CENTER_OF_PHASE_1;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);

                prompt+="cardinal.";

            }

            if(!string.IsNullOrWhiteSpace(prompt)) {

                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,6000);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 1 Windfang And Stonefang (Guidance)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(41885|41886|41889|41890)$"])]
    
        public void Phase_1_Windfang_And_Stonefang_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            Vector3 innerPosition=new Vector3(100,0,93.5f);
            Vector3 outerPosition=new Vector3(100,0,89.5f);
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(myIndex)) {

                return;

            }
        
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            // 41885: Donut & Stack + Cardinal Lines
            // 41886: Donut & Stack + Intercardinal Lines
            // 41889: Circle & Spread + Cardinal Lines
            // 41890: Circle & Spread + Intercardinal Lines
            
            if(string.Equals(@event["ActionId"],"41885")) {
                
                IReadOnlyList<float> getDegree=[315,135,225,45,225,135,315,45];
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=rotatePosition(innerPosition,ARENA_CENTER_OF_PHASE_1,getDegree[myIndex].DegToRad());
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            }
            
            if(string.Equals(@event["ActionId"],"41886")) {
                
                IReadOnlyList<float> getDegree=[0,180,270,90,270,180,0,90];
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=rotatePosition(innerPosition,ARENA_CENTER_OF_PHASE_1,getDegree[myIndex].DegToRad());
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            }
            
            if(string.Equals(@event["ActionId"],"41889")) {
                
                IReadOnlyList<float> getDegree=[335,155,245,65,205,115,295,25];
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=rotatePosition(outerPosition,ARENA_CENTER_OF_PHASE_1,getDegree[myIndex].DegToRad());
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            }
            
            if(string.Equals(@event["ActionId"],"41890")) {
                
                IReadOnlyList<float> getDegree=[20,200,290,110,250,160,340,70];
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=rotatePosition(outerPosition,ARENA_CENTER_OF_PHASE_1,getDegree[myIndex].DegToRad());
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            }
        
        }
        
        [ScriptMethod(name:"Phase 1 Eminent Reign And Revolutionary Reign (Circle)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(43308|43309|43310|43312|43313)$"])]
    
        public void Phase_1_Eminent_Reign_And_Revolutionary_Reign_Circle(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            Vector3 targetPosition=ARENA_CENTER_OF_PHASE_1;

            try {

                targetPosition=JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("TargetPosition deserialization failed.");

                return;

            }
        
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6);
            currentProperties.Position=targetPosition;
            currentProperties.DestoryAt=7000;

            if(string.Equals(@event["ActionId"],"43312")||string.Equals(@event["ActionId"],"43313")) {

                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(0.8f);

            }

            else {
                
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
        
        }
        
        [ScriptMethod(name:"Phase 1 Eminent Reign And Revolutionary Reign (Direction)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(43312|43313)$"])]
    
        public void Phase_1_Eminent_Reign_And_Revolutionary_Reign_Direction(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            Vector3 targetPosition=ARENA_CENTER_OF_PHASE_1;

            try {

                targetPosition=JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("TargetPosition deserialization failed.");

                return;

            }
        
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            string prompt=string.Empty;
            
            // 43312: Fan
            // 43313: Circle

            currentProperties.Scale=new(10,50);
            currentProperties.Position=targetPosition;
            currentProperties.TargetPosition=ARENA_CENTER_OF_PHASE_1;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(0.8f);
            currentProperties.DestoryAt=9100;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2,9);
            currentProperties.Position=targetPosition;
            currentProperties.TargetPosition=ARENA_CENTER_OF_PHASE_1;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.DestoryAt=7000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2,9);
            currentProperties.Position=targetPosition;
            currentProperties.TargetPosition=ARENA_CENTER_OF_PHASE_1;
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=7000;
            currentProperties.DestoryAt=2100;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);

            if(string.Equals(@event["ActionId"],"43312")) {

                prompt="Fan later, get closer.";

            }
            
            if(string.Equals(@event["ActionId"],"43313")) {

                prompt="Circle later, stay away.";

            }
            
            if(!string.IsNullOrWhiteSpace(prompt)) {

                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,9100);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 1 Eminent Reign And Revolutionary Reign (ID Acquisition)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(43312|43313)$"],
            userControl:false)]
    
        public void Phase_1_Eminent_Reign_And_Revolutionary_ID_Acquisition(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            reignId=@event["ActionId"];

        }
        
        [ScriptMethod(name:"Phase 1 Eminent Reign And Revolutionary Reign (Combo)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:regex:^(42927|41880)$"])]
    
        public void Phase_1_Eminent_Reign_And_Revolutionary_Reign_Combo(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(string.IsNullOrWhiteSpace(reignId)) {

                return;

            }

            Vector3 effectPosition=ARENA_CENTER_OF_PHASE_1;

            try {

                effectPosition=JsonConvert.DeserializeObject<Vector3>(@event["EffectPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("EffectPosition deserialization failed.");

                return;

            }
        
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(myIndex)) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            // 43312: Fan
            // 43313: Circle

            if(string.Equals(reignId,"43312")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(20);
                currentProperties.Position=effectPosition;
                currentProperties.TargetPosition=ARENA_CENTER_OF_PHASE_1;
                currentProperties.Radian=float.Pi/3*2;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=3600;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

            }
            
            if(string.Equals(reignId,"43313")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(14);
                currentProperties.Position=effectPosition;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=3600;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

            }
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(25);
            currentProperties.Position=effectPosition;
            currentProperties.TargetObject=accessory.Data.PartyList[0];
            currentProperties.Radian=float.Pi/3;
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=3600;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(25);
            currentProperties.Position=effectPosition;
            currentProperties.TargetObject=accessory.Data.PartyList[1];
            currentProperties.Radian=float.Pi/3;
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=3600;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(25);
            currentProperties.Position=effectPosition;
            currentProperties.TargetObject=accessory.Data.PartyList[2];
            currentProperties.Radian=float.Pi/6;
            currentProperties.DestoryAt=3600;

            if(!isTank(myIndex)&&isInGroup1(myIndex)) {

                currentProperties.Color=accessory.Data.DefaultSafeColor;

            }

            else {
                
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(25);
            currentProperties.Position=effectPosition;
            currentProperties.TargetObject=accessory.Data.PartyList[3];
            currentProperties.Radian=float.Pi/6;
            currentProperties.DestoryAt=3600;

            if(!isTank(myIndex)&&isInGroup2(myIndex)) {

                currentProperties.Color=accessory.Data.DefaultSafeColor;

            }

            else {
                
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
            System.Threading.Thread.MemoryBarrier();

            reignId=string.Empty;

        }
        
        [ScriptMethod(name:"Phase 1 Eminent Reign And Revolutionary Reign (Guidance)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(43312|43313)$"])]
    
        public void Phase_1_Eminent_Reign_And_Revolutionary_Reign_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            Vector3 targetPosition=ARENA_CENTER_OF_PHASE_1;

            try {

                targetPosition=JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("TargetPosition deserialization failed.");

                return;

            }

            // Vector3 bossPosition=new Vector3(100,0,92.487f);
            Vector3 mtPositionOfEminentReign=new Vector3(97.702f,0,90.559f);
            Vector3 otPositionOfEminentReign=new Vector3(102.298f,0,90.559f);
            Vector3 group1PositionOfEminentReign=new Vector3(94.204f,0,94.040f);
            Vector3 group2PositionOfEminentReign=new Vector3(105.796f,0,94.040f);
            // Eminent Reign: https://www.geogebra.org/calculator/eju6szvv
            Vector3 mtPositionOfRevolutionaryReign=new Vector3(89.465f,0,103.165f);
            Vector3 otPositionOfRevolutionaryReign=new Vector3(110.535f,0,103.165f);
            Vector3 group1PositionOfRevolutionaryReign=new Vector3(97.507f,0,109.303f);
            Vector3 group2PositionOfRevolutionaryReign=new Vector3(102.493f,0,109.303f);
            // Revolutionary Reign: https://www.geogebra.org/calculator/xc8zxmvz
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            Vector3 approximateDestination=rotatePosition(targetPosition,ARENA_CENTER_OF_PHASE_1,Math.PI);
            double rotation=getRotation(approximateDestination,ARENA_CENTER_OF_PHASE_1);
            
            if(!isLegalIndex(myIndex)) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            Vector3 myPosition=ARENA_CENTER_OF_PHASE_1;
            int guidanceDelay=0;

            // 43312: Fan
            // 43313: Circle
            
            if(string.Equals(@event["ActionId"],"43312")) {

                myPosition=myIndex switch {
                    
                    0 => mtPositionOfEminentReign,
                    1 => otPositionOfEminentReign,
                    2 => group1PositionOfEminentReign,
                    3 => group2PositionOfEminentReign,
                    4 => group1PositionOfEminentReign,
                    5 => group2PositionOfEminentReign,
                    6 => group1PositionOfEminentReign,
                    7 => group2PositionOfEminentReign,
                    _ => ARENA_CENTER_OF_PHASE_1
                    
                };
                
                guidanceDelay=myIndex switch {
                    
                    0 => 9100,
                    1 => 9100,
                    2 => 7000,
                    3 => 7000,
                    4 => 7000,
                    5 => 7000,
                    6 => 7000,
                    7 => 7000,
                    _ => 0
                    
                };

            }
            
            if(string.Equals(@event["ActionId"],"43313")) {
                
                myPosition=myIndex switch {
                    
                    0 => mtPositionOfRevolutionaryReign,
                    1 => otPositionOfRevolutionaryReign,
                    2 => group1PositionOfRevolutionaryReign,
                    3 => group2PositionOfRevolutionaryReign,
                    4 => group1PositionOfRevolutionaryReign,
                    5 => group2PositionOfRevolutionaryReign,
                    6 => group1PositionOfRevolutionaryReign,
                    7 => group2PositionOfRevolutionaryReign,
                    _ => ARENA_CENTER_OF_PHASE_1
                    
                };
                
                guidanceDelay=myIndex switch {
                    
                    0 => 0,
                    1 => 0,
                    2 => 9100,
                    3 => 9100,
                    4 => 9100,
                    5 => 9100,
                    6 => 9100,
                    7 => 9100,
                    _ => 0
                    
                };

            }

            if(myPosition.Equals(ARENA_CENTER_OF_PHASE_1)) {

                return;

            }
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=rotatePosition(myPosition,ARENA_CENTER_OF_PHASE_1,rotation);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=guidanceDelay;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=rotatePosition(myPosition,ARENA_CENTER_OF_PHASE_1,rotation);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=guidanceDelay;
            currentProperties.DestoryAt=12700-guidanceDelay;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"Phase 1 Eminent Reign And Revolutionary Reign (Add)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42893"])]
    
        public void Phase_1_Eminent_Reign_And_Revolutionary_Reign_Add(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
        
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6,25);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=2500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
        
        }
        
        [ScriptMethod(name:"Phase 1 The First Half Of Millennial Decay (Line)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41908"])]
    
        public void Phase_1_The_First_Half_Of_Millennial_Decay_Line(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }

            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
        
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(8,24);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=2000;
            currentProperties.DestoryAt=3700;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(8,24);
            currentProperties.Owner=sourceId;
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=5200;
            currentProperties.DestoryAt=2500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
        
        }
        
        [ScriptMethod(name:"Phase 1 The First Half Of Millennial Decay (Direction Acquisition)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:18218"],
            userControl:false)]
    
        public void Phase_1_The_First_Half_Of_Millennial_Decay_Direction_Acquisition(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }

            if(numberOfWindWolfLines>=5) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
        
            ++numberOfWindWolfLines;
            
            System.Threading.Thread.MemoryBarrier();

            if(numberOfWindWolfLines>=3) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            Vector3 sourcePosition=ARENA_CENTER_OF_PHASE_1;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            if(numberOfWindWolfLines==1) {
                
                positionOfTheFirstWindWolf=sourcePosition;

                if(sourcePosition.Z<100) {
                    
                    windWolvesStartFromTheNorth=true;
                    
                }

                else {

                    windWolvesStartFromTheNorth=false;

                }

            }

            else {

                if(numberOfWindWolfLines==2) {
                    
                    if((sourcePosition.X>positionOfTheFirstWindWolf.X&&sourcePosition.Z>positionOfTheFirstWindWolf.Z)
                       ||
                       (sourcePosition.X<positionOfTheFirstWindWolf.X&&sourcePosition.Z<positionOfTheFirstWindWolf.Z)) {

                        windWolvesRotateClockwise=true;

                    }

                    else {

                        windWolvesRotateClockwise=false;

                    }
                
                    System.Threading.Thread.MemoryBarrier();

                    windWolfRotationSemaphore.Set();
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"Phase 1 The First Half Of Millennial Decay (Direction)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:18218"],
            suppress:20000)]
    
        public void Phase_1_The_First_Half_Of_Millennial_Decay_Direction(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            windWolfRotationSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();

            IReadOnlyList<Vector3> point=[new Vector3(100,0,96),new Vector3(104,0,100),new Vector3(100,0,104),new Vector3(96,0,100)];

            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            string prompt=string.Empty;

            if(windWolvesRotateClockwise) {

                for(int i=0;i<=3;++i) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                    
                    currentProperties.Scale=new(2);
                    currentProperties.Position=point[i];
                    currentProperties.TargetPosition=point[(i+1)%4];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                    currentProperties.DestoryAt=16000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                }

                prompt="Clockwise.";

            }

            else {
                
                for(int i=4;i>=1;--i) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                    
                    currentProperties.Scale=new(2);
                    currentProperties.Position=point[i%4];
                    currentProperties.TargetPosition=point[i-1];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                    currentProperties.DestoryAt=18000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                }
                
                prompt="Counterclockwise.";
                
            }
            
            if(!string.IsNullOrWhiteSpace(prompt)) {

                /*
                
                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,1000);
                    
                }
                
                */
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 1 The First Half Of Millennial Decay (Anti-knockback Warning)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41912"])]
    
        public void Phase_1_The_First_Half_Of_Millennial_Decay_AntiKnockback_Warning(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }

            if(stratOfMillennialDecay==StratsOfMillennialDecay.Rinon_Or_RaidPlan_84d) {
                
                string prompt="Enable anti-knockback!";
            
                // System.Threading.Thread.Sleep(500);
            
                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,4500,true);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 1 The First Half Of Millennial Decay (Gust Acquisition)",
            eventType:EventTypeEnum.TargetIcon,
            suppress:2500,
            userControl:false)]
    
        public void Phase_1_The_First_Half_Of_Millennial_Decay_Gust_Acquisition(Event @event,ScriptAccessory accessory) {

            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }

            if(iconId!=0) { // 0x178-0x178=0

                return;
                
            }
            
            accessory.Log.Debug($"An expected icon ID was captured. iconId={iconId}");

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }

            if(roundOfGust>=2) {

                return;
                
            }
            
            System.Threading.Thread.MemoryBarrier();
        
            ++roundOfGust;

            if(!convertObjectId(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf((uint)targetId);
            
            if(!isLegalIndex(targetIndex)) {

                return;

            }

            gustMarksSupporters=isSupporter(targetIndex);
            
            System.Threading.Thread.MemoryBarrier();

            if(roundOfGust==1) {

                gustFirstSetSemaphore.Set();

            }

            else {

                if(roundOfGust==2) {

                    gustSecondSetSemaphore.Set();
                    
                }

            }
            
            accessory.Log.Debug($"gustMarksSupporters={gustMarksSupporters}");
        
        }
        
        [ScriptMethod(name:"Phase 1 The First Half Of Millennial Decay (First Set Guidance)",
            eventType:EventTypeEnum.TargetIcon,
            suppress:7500)]
    
        public void Phase_1_The_First_Half_Of_Millennial_Decay_First_Set_Guidance(Event @event,ScriptAccessory accessory) {

            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }

            if(iconId!=0) { // 0x178-0x178=0

                return;
                
            }
            
            accessory.Log.Debug($"An expected icon ID was captured. iconId={iconId}");

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            gustFirstSetSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();

            if(stratOfMillennialDecay==StratsOfMillennialDecay.Rinon_Or_RaidPlan_84d) {
                
                Vector3 northwest=new Vector3(95.417f,0,90);
                Vector3 northeast=new Vector3(104.583f,0,90);
                Vector3 southwest=new Vector3(95.417f,0,110);
                Vector3 southeast=new Vector3(104.583f,0,110);
                // Initial positions: https://www.geogebra.org/calculator/kt3brffu
                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                
                if(!isLegalIndex(myIndex)) {

                    return;

                }

                if((isSupporter(myIndex)&&!gustMarksSupporters)
                   ||
                   (isDps(myIndex)&&gustMarksSupporters)) {

                    return;

                }
            
                var currentProperties=accessory.Data.GetDefaultDrawProperties();
                Vector3 myPosition=ARENA_CENTER_OF_PHASE_1;
            
                // Northwest: MT(0) or R1(6)
                // Northeast: H2(3) or R2(7)
                // Southwest: H1(2) or M1(4)
                // Southeast: OT(1) or M2(5)

                myPosition=myIndex switch {
                
                    0 => northwest,
                    1 => southeast,
                    2 => southwest,
                    3 => northeast,
                    4 => southwest,
                    5 => southeast,
                    6 => northwest,
                    7 => northeast,
                    _ => ARENA_CENTER_OF_PHASE_1
                
                };

                if(myPosition.Equals(ARENA_CENTER_OF_PHASE_1)) {

                    return;

                }
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=5100;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"Phase 1 The First Half Of Millennial Decay (Second Set Guidance)",
            eventType:EventTypeEnum.TargetIcon,
            suppress:7500)]
    
        public void Phase_1_The_First_Half_Of_Millennial_Decay_Second_Set_Guidance(Event @event,ScriptAccessory accessory) {

            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }

            if(iconId!=0) { // 0x178-0x178=0

                return;
                
            }
            
            accessory.Log.Debug($"An expected icon ID was captured. iconId={iconId}");

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            gustSecondSetSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();

            if(stratOfMillennialDecay==StratsOfMillennialDecay.Rinon_Or_RaidPlan_84d) {
                
                Vector3 northwest=new Vector3(95.417f,0,90);
                Vector3 northeast=new Vector3(104.583f,0,90);
                Vector3 southwest=new Vector3(95.417f,0,110);
                Vector3 southeast=new Vector3(104.583f,0,110);
                // Initial positions: https://www.geogebra.org/calculator/kt3brffu
                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                
                if(!isLegalIndex(myIndex)) {

                    return;

                }

                if((isSupporter(myIndex)&&!gustMarksSupporters)
                   ||
                   (isDps(myIndex)&&gustMarksSupporters)) {

                    return;

                }
                
                var currentProperties=accessory.Data.GetDefaultDrawProperties();
                Vector3 myPosition=ARENA_CENTER_OF_PHASE_1;
                int guidanceDelay=0;
                
                // Northwest: MT(0) or R1(6)
                // Northeast: H2(3) or R2(7)
                // Southwest: H1(2) or M1(4)
                // Southeast: OT(1) or M2(5)

                if(windWolvesRotateClockwise) {
                    
                    northwest=rotatePosition(northwest,ARENA_CENTER_OF_PHASE_1,Math.PI/5*3);
                    northeast=rotatePosition(northeast,ARENA_CENTER_OF_PHASE_1,Math.PI/5*3);
                    southwest=rotatePosition(southwest,ARENA_CENTER_OF_PHASE_1,Math.PI/5*3);
                    southeast=rotatePosition(southeast,ARENA_CENTER_OF_PHASE_1,Math.PI/5*3);

                }

                else {
                    
                    northwest=rotatePosition(northwest,ARENA_CENTER_OF_PHASE_1,-(Math.PI/5*3));
                    northeast=rotatePosition(northeast,ARENA_CENTER_OF_PHASE_1,-(Math.PI/5*3));
                    southwest=rotatePosition(southwest,ARENA_CENTER_OF_PHASE_1,-(Math.PI/5*3));
                    southeast=rotatePosition(southeast,ARENA_CENTER_OF_PHASE_1,-(Math.PI/5*3));
                    
                }

                if(meleeGoFurther) {

                    if(windWolvesRotateClockwise) {
                        
                        myPosition=myIndex switch {
                    
                            0 => northwest,
                            1 => southeast,
                            2 => southwest,
                            3 => northeast,
                            4 => northwest, // Swap with R1.
                            5 => southeast,
                            6 => southwest, // Swap with M1.
                            7 => northeast,
                            _ => ARENA_CENTER_OF_PHASE_1
                    
                        };
                        
                    }

                    else {
                        
                        myPosition=myIndex switch {
                    
                            0 => southwest, // Swap with H1.
                            1 => northeast, // Swap with H2.
                            2 => northwest, // Swap with MT.
                            3 => southeast, // Swap with OT.
                            4 => southwest,
                            5 => northeast, // Swap with R2.
                            6 => northwest,
                            7 => southeast, // Swap with M2.
                            _ => ARENA_CENTER_OF_PHASE_1
                    
                        };
                        
                    }
                    
                    if(myPosition.Equals(ARENA_CENTER_OF_PHASE_1)) {

                        return;

                    }
                    
                    guidanceDelay=myIndex switch {
                        
                        0 => 3300,
                        1 => 3300,
                        2 => 0,
                        3 => 0,
                        4 => 3300,
                        5 => 3300,
                        6 => 0,
                        7 => 0,
                        _ => 0
                    
                    };
                    
                }

                else {
                    
                    myPosition=myIndex switch {
                        
                        0 => northwest,
                        1 => southeast,
                        2 => southwest,
                        3 => northeast,
                        4 => southwest,
                        5 => southeast,
                        6 => northwest,
                        7 => northeast,
                        _ => ARENA_CENTER_OF_PHASE_1
                    
                    };

                    if(myPosition.Equals(ARENA_CENTER_OF_PHASE_1)) {

                        return;

                    }

                    if(windWolvesRotateClockwise) {
                        
                        guidanceDelay=myIndex switch {
                        
                            0 => 3300,
                            1 => 3300,
                            2 => 0,
                            3 => 0,
                            4 => 0,
                            5 => 3300,
                            6 => 3300,
                            7 => 0,
                            _ => 0
                    
                        };
                        
                    }

                    else {
                        
                        guidanceDelay=myIndex switch {
                        
                            0 => 0,
                            1 => 0,
                            2 => 3300,
                            3 => 3300,
                            4 => 3300,
                            5 => 0,
                            6 => 0,
                            7 => 3300,
                            _ => 0
                    
                        };
                        
                    }
                    
                }
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=guidanceDelay;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=guidanceDelay;
                currentProperties.DestoryAt=5100-guidanceDelay;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 1 The First Half Of Millennial Decay (Sub-phase 1 Control)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:41907"],
            suppress:2500,
            userControl:false)]
    
        public void Phase_1_The_First_Half_Of_Millennial_SubPhase_1_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }

            if(roundOfGustApplied>=2) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            ++roundOfGustApplied;
            
            System.Threading.Thread.MemoryBarrier();

            if(roundOfGustApplied>=2) {

                currentSubPhase=2;

                windWolfRotationSemaphore.Reset();
                gustFirstSetSemaphore.Reset();
                gustSecondSetSemaphore.Reset();
                
                accessory.Log.Debug("Now moving to Phase 1 Sub-phase 2.");

            }

        }
        
        [ScriptMethod(name:"Phase 1 The Second Half Of Millennial Decay (Fan)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:regex:^(0039|0001)$"])]
    
        public void Phase_1_The_Second_Half_Of_Millennial_Decay_Fan(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER_OF_PHASE_1;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            if(Vector3.Distance(sourcePosition,ARENA_CENTER_OF_PHASE_1)>8.4d) {
                
                return;
                
            }
            
            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
            
                return;
            
            }

            lock(windWolfTethersHaveBeenDrawn) {

                if(windWolfTethersHaveBeenDrawn.Contains(sourceId)) {

                    return;

                }

                else {
                    
                    windWolfTethersHaveBeenDrawn.Add(sourceId);
                    
                }
                
            }

            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(40);
            currentProperties.Owner=sourceId;
            currentProperties.TargetObject=targetId;
            currentProperties.Radian=float.Pi/6;
            currentProperties.DestoryAt=8000;

            if(targetId==accessory.Data.Me) {
                        
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                        
            }

            else {
                        
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                        
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
        
        }
        
        [ScriptMethod(name:"Phase 1 The Second Half Of Millennial Decay (Anti-knockback Warning)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41912"])]
    
        public void Phase_1_The_Second_Half_Of_Millennial_Decay_AntiKnockback_Warning(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }

            if(stratOfMillennialDecay==StratsOfMillennialDecay.Rinon_Or_RaidPlan_84d) {
                
                string prompt="Don't enable anti-knockback!";
            
                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,4700,true);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 1 The Second Half Of Millennial Decay (Data Acquisition)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:regex:^(0039|0001)$"],
            userControl:false)]
    
        public void Phase_1_The_Second_Half_Of_Millennial_Decay_Data_Acquisition(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }

            if(numberOfWindWolfTethers>=4) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            Vector3 sourcePosition=ARENA_CENTER_OF_PHASE_1;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            if(Vector3.Distance(sourcePosition,ARENA_CENTER_OF_PHASE_1)>8.4d) {
                
                return;
                
            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
            
                return;
            
            }

            int discretizedPosition=discretizePosition(sourcePosition,ARENA_CENTER_OF_PHASE_1,8);
            int targetIndex=accessory.Data.PartyList.IndexOf((uint)targetId);
            
            if(!isLegalIndex(targetIndex)) {

                return;

            }

            lock(getWindWolfTethers) {

                if(getWindWolfTethers[targetIndex]==-1) {
                    
                    ++numberOfWindWolfTethers;
                    
                }
                
                getWindWolfTethers[targetIndex]=discretizedPosition;
                
                System.Threading.Thread.MemoryBarrier();
                
                if(numberOfWindWolfTethers>=4) {

                    if(discretizedPosition%2==0) {
                    
                        windWolvesAreOnTheCardinals=true;

                    }

                    else {
                    
                        windWolvesAreOnTheCardinals=false;
                    
                    }
                
                    System.Threading.Thread.MemoryBarrier();

                    windWolfTetherSemaphore.Set();

                }
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 1 The Second Half Of Millennial Decay (Guidance)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:regex:^(0039|0001)$"],
            suppress:9500)]
    
        public void Phase_1_The_Second_Half_Of_Millennial_Decay_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            windWolfTetherSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();

            if(stratOfMillennialDecay==StratsOfMillennialDecay.Rinon_Or_RaidPlan_84d) {
                
                Vector3 standbyPositionForTowers=new Vector3(100,0,98);
                Vector3 finalPositionForTowers=new Vector3(100,0,90);
                Vector3 standbyPositionForTethers=new Vector3(100,0,97);
                Vector3 finalPositionForTethers=new Vector3(100,0,89);
                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                
                if(!isLegalIndex(myIndex)) {

                    return;

                }
                
                var currentProperties=accessory.Data.GetDefaultDrawProperties();
                Vector3 myStandbyPosition=ARENA_CENTER_OF_PHASE_1;
                Vector3 myFinalPosition=ARENA_CENTER_OF_PHASE_1;
                
                // 7 and 0: MT(0) and R1(6)
                // 1 and 2: H2(3) and R2(7)
                // 3 and 4: OT(1) and M2(5)
                // 5 and 6: H1(2) and M1(4)

                if(getWindWolfTethers[myIndex]!=-1) {

                    int oppositeDiscretizedPosition=(getWindWolfTethers[myIndex]+4)%8;
                    
                    myStandbyPosition=rotatePosition(standbyPositionForTethers,ARENA_CENTER_OF_PHASE_1,Math.PI/4*oppositeDiscretizedPosition);
                    myFinalPosition=rotatePosition(finalPositionForTethers,ARENA_CENTER_OF_PHASE_1,Math.PI/4*oppositeDiscretizedPosition);

                }

                else {

                    int myDiscretizedPosition=-1;

                    if(windWolvesAreOnTheCardinals) {

                        myDiscretizedPosition=myIndex switch {
                            
                            0 => 7,
                            1 => 3,
                            2 => 5,
                            3 => 1,
                            4 => 5,
                            5 => 3,
                            6 => 7,
                            7 => 1,
                            _ => -1
                            
                        };

                    }

                    else {
                        
                        myDiscretizedPosition=myIndex switch {
                            
                            0 => 0,
                            1 => 4,
                            2 => 6,
                            3 => 2,
                            4 => 6,
                            5 => 4,
                            6 => 0,
                            7 => 2,
                            _ => -1
                            
                        };
                        
                    }

                    if(myDiscretizedPosition==-1) {

                        return;

                    }
                    
                    myStandbyPosition=rotatePosition(standbyPositionForTowers,ARENA_CENTER_OF_PHASE_1,Math.PI/4*myDiscretizedPosition);
                    myFinalPosition=rotatePosition(finalPositionForTowers,ARENA_CENTER_OF_PHASE_1,Math.PI/4*myDiscretizedPosition);

                }
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myStandbyPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=5000;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Position=myStandbyPosition;
                currentProperties.TargetPosition=myFinalPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=5000;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myFinalPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=5000;
                currentProperties.DestoryAt=3000;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

                if(getWindWolfTethers[myIndex]==-1) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Position=myFinalPosition;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.Delay=5000;
                    currentProperties.DestoryAt=3000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 1 The Second Half Of Millennial Decay (Sub-phase 2 Control)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41913"],
            userControl:false)]
    
        public void Phase_1_The_Second_Half_Of_Millennial_Decay_SubPhase_2_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            currentSubPhase=3;

            windWolfTetherSemaphore.Reset();
            
            accessory.Log.Debug("Now moving to Phase 1 Sub-phase 3.");

        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Titans (Line)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41926"])]
    
        public void Phase_1_Terrestrial_Titans_Line(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=3) {

                return;

            }

            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
        
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(10,20);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=8000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
        
        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Titans (Cross)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41943"])]
    
        public void Phase_1_Terrestrial_Titans_Cross(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=3) {

                return;

            }

            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
        
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(7,40);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=4000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(7,40);
            currentProperties.Owner=sourceId;
            currentProperties.Rotation=float.Pi/2;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=4000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
        
        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Titans (Line Direction Acquisition)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41926"],
            suppress:2500,
            userControl:false)]
    
        public void Phase_1_Terrestrial_Titans_Line_Direction_Acquisition(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=3) {

                return;

            }

            double sourceRotation=0;

            try {

                sourceRotation=JsonConvert.DeserializeObject<double>(@event["SourceRotation"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourceRotation deserialization failed.");

                return;

            }

            double actualRotation=convertRotation(sourceRotation);
            
            if((Math.Abs(actualRotation-Math.PI/4)<Math.PI*0.05)
               ||
               (Math.Abs(actualRotation-Math.PI/4*5)<Math.PI*0.05)) {

                --riskIndexOfIntercardinals[0];
                --riskIndexOfIntercardinals[2];

            }

            else {
                
                --riskIndexOfIntercardinals[1];
                --riskIndexOfIntercardinals[3];
                
            }
            
            accessory.Log.Debug($"riskIndexOfIntercardinals={JsonConvert.SerializeObject(riskIndexOfIntercardinals)}");
        
        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Titans (Oblique Cross Acquisition)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:18221"],
            userControl:false)]
    
        public void Phase_1_Terrestrial_Titans_Oblique_Cross_Acquisition(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=3) {

                return;

            }

            double sourceRotation=0;

            try {

                sourceRotation=JsonConvert.DeserializeObject<double>(@event["SourceRotation"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourceRotation deserialization failed.");

                return;

            }

            double actualRotation=convertRotation(sourceRotation);
            
            if((Math.Abs(actualRotation-Math.PI/4)<Math.PI*0.05)
               ||
               (Math.Abs(actualRotation-Math.PI/4*3)<Math.PI*0.05)
               ||
               (Math.Abs(actualRotation-Math.PI/4*5)<Math.PI*0.05)
               ||
               (Math.Abs(actualRotation-Math.PI/4*7)<Math.PI*0.05)) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            Vector3 sourcePosition=ARENA_CENTER_OF_PHASE_1;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            IReadOnlyList<IReadOnlyList<int>> affectedPositions=[[3,0],[0,1],[1,2],[2,3]];
            int discretizedPosition=discretizePosition(sourcePosition,ARENA_CENTER_OF_PHASE_1,4);
            
            --riskIndexOfIntercardinals[affectedPositions[discretizedPosition][0]];
            --riskIndexOfIntercardinals[affectedPositions[discretizedPosition][1]];
            
            System.Threading.Thread.MemoryBarrier();

            terrestrialTitansSemaphore.Set();
            
            accessory.Log.Debug($"riskIndexOfIntercardinals={JsonConvert.SerializeObject(riskIndexOfIntercardinals)}");

        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Titans (Guidance)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:18221"],
            suppress:2500)]
    
        public void Phase_1_Terrestrial_Titans_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=3) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            terrestrialTitansSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();

            int unaffectedPosition=riskIndexOfIntercardinals.IndexOf(0);

            if(unaffectedPosition<0||unaffectedPosition>3) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            Vector3 myPosition=rotatePosition(new Vector3(100,0,89),ARENA_CENTER_OF_PHASE_1,Math.PI/4*(2*unaffectedPosition+1));

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=6000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Titans (Sub-phase 3 Control)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:41943"],
            suppress:2500,
            userControl:false)]
    
        public void Phase_1_Terrestrial_Titans_SubPhase_3_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=3) {

                return;

            }

            System.Threading.Thread.MemoryBarrier();

            currentSubPhase=4;

            terrestrialTitansSemaphore.Reset();
            
            accessory.Log.Debug("Now moving to Phase 1 Sub-phase 4.");

        }
        
        [ScriptMethod(name:"Phase 1 Intermission Regins (Sub-phase 4 Control)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41928"],
            userControl:false)]
    
        public void Phase_1_Intermission_Regins_SubPhase_4_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=4) {

                return;

            }

            System.Threading.Thread.MemoryBarrier();

            currentSubPhase=5;
            
            accessory.Log.Debug("Now moving to Phase 1 Sub-phase 5.");
        
        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Add Position Acquisition)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:18219"],
            userControl:false)]
    
        public void Phase_1_Tactical_Pack_Add_Position_Acquisition(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }

            if(addRotationHasBeenDrawn) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER_OF_PHASE_1;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            if(sourcePosition.Equals(ARENA_CENTER_OF_PHASE_1)) {

                return;

            }
                
            positionOfTheWindWolfAdd=sourcePosition;
        
        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Direction Acquisition)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:18262"],
            userControl:false)]
    
        public void Phase_1_Tactical_Pack_Direction_Acquisition(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            if(addRotationHasBeenDrawn) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER_OF_PHASE_1;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            if(sourcePosition.Equals(ARENA_CENTER_OF_PHASE_1)) {

                return;

            }
            
            if((sourcePosition.X>positionOfTheWindWolfAdd.X&&sourcePosition.Z<positionOfTheWindWolfAdd.Z)
               ||
               (sourcePosition.X<positionOfTheWindWolfAdd.X&&sourcePosition.Z>positionOfTheWindWolfAdd.Z)) {

                addsRotateClockwise=true;

            }

            else {

                addsRotateClockwise=false;

            }
                
            System.Threading.Thread.MemoryBarrier();

            addRotationSemaphore.Set();
        
        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Direction)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:18262"])]
    
        public void Phase_1_Tactical_Pack_Direction(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            if(addRotationHasBeenDrawn) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            addRotationSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();

            IReadOnlyList<Vector3> point=[new Vector3(100,0,96),new Vector3(104,0,100),new Vector3(100,0,104),new Vector3(96,0,100)];

            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            string prompt=string.Empty;

            if(addsRotateClockwise) {

                for(int i=0;i<=3;++i) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Name=$"Phase_1_Tactical_Pack_Direction_{i}";
                    currentProperties.Scale=new(2);
                    currentProperties.Position=point[i];
                    currentProperties.TargetPosition=point[(i+1)%4];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                    currentProperties.DestoryAt=90000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                }

                prompt="Clockwise.";

            }

            else {
                
                for(int i=4;i>=1;--i) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                    
                    currentProperties.Name=$"Phase_1_Tactical_Pack_Direction_{4-i}";
                    currentProperties.Scale=new(2);
                    currentProperties.Position=point[i%4];
                    currentProperties.TargetPosition=point[i-1];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                    currentProperties.DestoryAt=90000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                }
                
                prompt="Counterclockwise.";
                
            }
            
            System.Threading.Thread.MemoryBarrier();
            
            addRotationHasBeenDrawn=true;
            
            if(!string.IsNullOrWhiteSpace(prompt)) {
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }

        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Direction Destruction)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:regex:^(18225|18219)$"],
            userControl:false)]
    
        public void Phase_1_Tactical_Pack_Direction_Destruction(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            if(!addRotationHasBeenDrawn) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            addRotationSemaphore.Reset();
            
            for(int i=0;i<=3;++i) {
                
                accessory.Method.RemoveDraw($"Phase_1_Tactical_Pack_Direction_{i}");
                    
            }
        
        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Initial Add Guidance)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:regex:^(0150|014F)$"])]
    
        public void Phase_1_Tactical_Pack_Initial_Add_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
            
                return;
            
            }

            if(sourceId!=accessory.Data.Me&&targetId!=accessory.Data.Me) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            Vector3 myPosition=ARENA_CENTER_OF_PHASE_1;
            String prompt=string.Empty;

            if(sourceId==accessory.Data.Me) {
                
                try {

                    myPosition=JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]);

                } catch(Exception e) {
                
                    accessory.Log.Error("TargetPosition deserialization failed.");

                    return;

                }

                if(targetId==idOfTheWindWolfAdd) {

                    prompt="Go to Wolf of Stone.";

                }
                
                if(targetId==idOfTheStoneWolfAdd) {
                    
                    prompt="Go to Wolf of Wind.";
                    
                }
                
            }

            if(targetId==accessory.Data.Me) {
                
                try {

                    myPosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

                } catch(Exception e) {
                
                    accessory.Log.Error("SourcePosition deserialization failed.");

                    return;

                }
                
                if(sourceId==idOfTheWindWolfAdd) {

                    prompt="Go to Wolf of Stone.";

                }
                
                if(sourceId==idOfTheStoneWolfAdd) {
                    
                    prompt="Go to Wolf of Wind.";
                    
                }
                
            }

            if(myPosition.Equals(ARENA_CENTER_OF_PHASE_1)) {

                return;

            }

            else {

                myPosition=rotatePosition(myPosition,ARENA_CENTER_OF_PHASE_1,Math.PI);

            }
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=6000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            if(!string.IsNullOrWhiteSpace(prompt)) {

                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,6000);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }

        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Add Acquisition)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:regex:^(18225|18219|18262|18261)$"],
            userControl:false)]
    
        public void Phase_1_Tactical_Pack_Add_Acquisition(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }

            if(string.Equals(@event["SourceDataId"],"18225")) {

                idOfTheStoneWolfAdd??=sourceId;

            }
            
            if(string.Equals(@event["SourceDataId"],"18219")) {

                idOfTheWindWolfAdd??=sourceId;

            }
            
            if(string.Equals(@event["SourceDataId"],"18262")) {

                idOfTheEarthFont??=sourceId;

            }
            
            if(string.Equals(@event["SourceDataId"],"18261")) {

                idOfTheWindFont??=sourceId;

            }

        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Firewall Status Maintenance)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:regex:^(4389|4390)$"],
            userControl:false)]
    
        public void Phase_1_Tactical_Pack_Firewall_Status_Maintenance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
            
                return;
            
            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf((uint)targetId);
            
            if(!isLegalIndex(targetIndex)) {

                return;

            }
            
            bool targetHoldsWindpack=false;

            if(string.Equals(@event["StatusID"],"4389")) {

                targetHoldsWindpack=true;

            }
            
            if(string.Equals(@event["StatusID"],"4390")) {

                targetHoldsWindpack=false;

            }
            
            lock(windpackWasApplied) {

                windpackWasApplied[targetIndex]=targetHoldsWindpack;

            }
            
            accessory.Log.Debug($"targetIndex={targetIndex},targetHoldsWindpack={targetHoldsWindpack}");

        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Doom Status Acquisition)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:regex:^(4391|4392)$"],
            userControl:false)]
    
        public void Phase_1_Tactical_Pack_Doom_Status_Acquisition(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
            
                return;
            
            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf((uint)targetId);
            
            if(!isLegalIndex(targetIndex)) {

                return;

            }
            
            bool targetHoldsWindborneEnd=false;

            if(string.Equals(@event["StatusID"],"4392")) {

                targetHoldsWindborneEnd=true;

            }
            
            if(string.Equals(@event["StatusID"],"4391")) {

                targetHoldsWindborneEnd=false;

            }
            
            lock(windborneEndWasApplied) {

                windborneEndWasApplied[targetIndex]=targetHoldsWindborneEnd;

            }

            double targetDuration=0;
            int targetRound=-1;
            
            try {

                targetDuration=JsonConvert.DeserializeObject<double>(@event["Duration"]);

            } catch(Exception e) {
                
                accessory.Log.Error("Duration deserialization failed.");

                return;

            }

            if(Math.Abs(targetDuration)<1||Math.Abs(targetDuration)>9998) {
                
                return;

            }

            if(Math.Abs(targetDuration-21)<1) {

                targetRound=1;

            }

            else {
                
                if(Math.Abs(targetDuration-37)<1) {

                    targetRound=2;

                }

                else {
                    
                    if(Math.Abs(targetDuration-54)<1) {

                        targetRound=3;

                    }
                    
                }
                
            }

            if(targetRound==-1) {

                return;

            }

            lock(roundForCleanse) {
                
                roundForCleanse[targetIndex]=targetRound;
                
            }
            
            System.Threading.Thread.MemoryBarrier();
            
            accessory.Log.Debug($"targetIndex={targetIndex},targetHoldsWindborneEnd={targetHoldsWindborneEnd},targetDuration={targetDuration},targetRound={targetRound}");

        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Round Control)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41932"],
            suppress:2500,
            userControl:false)]
    
        public void Phase_1_Tactical_Pack_Round_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            ++currentAddRound;
            
            System.Threading.Thread.MemoryBarrier();
            
            accessory.Log.Debug($"currentAddRound={currentAddRound}");
            
        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Tank Buster)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41932"])]
    
        public void Phase_1_Tactical_Pack_Tank_Buster(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
            
            /*
            
            if(!accessory.Data.EnmityList.TryGetValue(sourceId, out var currentEnmityList)) {

                return;

            }

            if(currentEnmityList==null||currentEnmityList.Count<1) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(40);
            currentProperties.Owner=sourceId;
            currentProperties.TargetObject=currentEnmityList[0];
            currentProperties.Radian=float.Pi/2;
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=5000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
            // Above code is deprecated, due to a totally unexpected reason, that is...
            // One of the add doesn't own an enmity list!
            // Yes, a selectable add doesn't own an enmity list, you read that right.
            // What spaghetti code, Square Enix?
            
            */
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(40);
            currentProperties.Owner=sourceId;
            currentProperties.TargetObject=0;
            currentProperties.Radian=float.Pi/2;
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=5000;

            if(sourceId==idOfTheWindWolfAdd) {

                currentProperties.TargetObject=accessory.Data.PartyList[windpackWasApplied.IndexOf(false)];

            }

            if(sourceId==idOfTheStoneWolfAdd) {
                
                currentProperties.TargetObject=accessory.Data.PartyList[windpackWasApplied.IndexOf(true)];
                
            }

            if(currentProperties.TargetObject==0) {
                
                return;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Line)",
            eventType:EventTypeEnum.TargetIcon)]
    
        public void Phase_1_Tactical_Pack_Line(Event @event,ScriptAccessory accessory) {

            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }

            if(iconId!=-353) { // 0x17-0x178=-353

                return;
                
            }
            
            accessory.Log.Debug($"An expected icon ID was captured. iconId={iconId}");
            
            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
            
                return;
            
            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf((uint)targetId);
            ulong? addId=null;
            
            if(!isLegalIndex(targetIndex)) {

                return;

            }

            if(windpackWasApplied[targetIndex]) {

                addId=idOfTheStoneWolfAdd;

            }

            else {

                addId=idOfTheWindWolfAdd;

            }

            if(addId==null) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6,40);
            currentProperties.Owner=((ulong)addId);
            currentProperties.TargetObject=targetId;
            currentProperties.DestoryAt=5000;
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(windpackWasApplied[targetIndex]==windpackWasApplied[myIndex]) {
                
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                
            }

            else {
                
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Earthborne End Guidance)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:41956"],
            suppress:2500)]
    
        public void Phase_1_Tactical_Pack_Earthborne_End_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            // 41935 Stalking Wind from Wolf of Wind.
            // 41956 Stalking Stone from Wolf of Stone.
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(myIndex==0||myIndex==1) {

                return;

            }

            if(currentAddRound!=roundForCleanse[myIndex]) {

                return;

            }

            if(windborneEndWasApplied[myIndex]) {

                return;

            }

            if(idOfTheEarthFont==null||idOfTheWindWolfAdd==null) {

                return;

            }
            
            // From 0s to 1s:
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=((ulong)idOfTheEarthFont);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=1000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=((ulong)idOfTheEarthFont);
            currentProperties.TargetObject=((ulong)idOfTheWindWolfAdd);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=1000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(1.5f);
            currentProperties.Owner=((ulong)idOfTheEarthFont);
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=1000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
            // From 1s:
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_1_Tactical_Pack_Earthborne_End_Guidance_1";
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=((ulong)idOfTheEarthFont);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=1000;
            currentProperties.DestoryAt=15500;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_1_Tactical_Pack_Earthborne_End_Guidance_2";
            currentProperties.Scale=new(2);
            currentProperties.Owner=((ulong)idOfTheEarthFont);
            currentProperties.TargetObject=((ulong)idOfTheWindWolfAdd);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=1000;
            currentProperties.DestoryAt=15500;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_1_Tactical_Pack_Earthborne_End_Guidance_3";
            currentProperties.Scale=new(1.5f);
            currentProperties.Owner=((ulong)idOfTheEarthFont);
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=1000;
            currentProperties.DestoryAt=15500;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
            System.Threading.Thread.MemoryBarrier();

            earthGuidanceHasBeenDrawn=true;

        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Earthborne End Guidance 2)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:regex:^(41966|43138|43520)$"],
            suppress:2500,
            userControl:false)]
    
        public void Phase_1_Tactical_Pack_Earthborne_End_Guidance_2(Event @event,ScriptAccessory accessory) {
            
            if(!earthGuidanceHasBeenDrawn) {

                return;

            }

            else {

                earthGuidanceHasBeenDrawn=false;

            }

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            // 41965 Wind Surge, 43137 Wind Surge (Last) and 43519 Wind Surge (Add Death) from Font of Wind Aether.
            // 41966 Sand Surge, 43138 Sand Surge (Last) and 43520 Sand Surge (Add Death) from Font of Earth Aether.
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(currentAddRound!=roundForCleanse[myIndex]) {

                return;

            }

            if(windborneEndWasApplied[myIndex]) {

                return;

            }

            if(idOfTheEarthFont==null||idOfTheWindWolfAdd==null) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            accessory.Method.RemoveDraw("Phase_1_Tactical_Pack_Earthborne_End_Guidance_1");
            accessory.Method.RemoveDraw("Phase_1_Tactical_Pack_Earthborne_End_Guidance_2");
            accessory.Method.RemoveDraw("Phase_1_Tactical_Pack_Earthborne_End_Guidance_3");
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=((ulong)idOfTheWindWolfAdd);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=2500;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            if(!string.Equals(@event["ActionId"],"43520")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(1.5f);
                currentProperties.Owner=((ulong)idOfTheEarthFont);
                currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=2500;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Windborne End Guidance)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:regex:^(41966|43138|43520)$"],
            suppress:2500)]
    
        public void Phase_1_Tactical_Pack_Windborne_End_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            // 41965 Wind Surge, 43137 Wind Surge (Last) and 43519 Wind Surge (Add Death) from Font of Wind Aether.
            // 41966 Sand Surge, 43138 Sand Surge (Last) and 43520 Sand Surge (Add Death) from Font of Earth Aether.
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(myIndex)) {

                return;

            }
            
            if(myIndex==0||myIndex==1) {

                return;

            }

            if(currentAddRound!=roundForCleanse[myIndex]) {

                return;

            }

            if(!windborneEndWasApplied[myIndex]) {

                return;

            }

            if(idOfTheWindFont==null||idOfTheStoneWolfAdd==null) {

                return;

            }
            
            // From 0s to 4s:
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=((ulong)idOfTheWindFont);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=4000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=((ulong)idOfTheWindFont);
            currentProperties.TargetObject=((ulong)idOfTheStoneWolfAdd);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=4000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(1.5f);
            currentProperties.Owner=((ulong)idOfTheWindFont);
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=4000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
            // From 4s:
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_1_Tactical_Pack_Windborne_End_Guidance_1";
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=((ulong)idOfTheWindFont);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=4000;
            currentProperties.DestoryAt=15500;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_1_Tactical_Pack_Windborne_End_Guidance_2";
            currentProperties.Scale=new(2);
            currentProperties.Owner=((ulong)idOfTheWindFont);
            currentProperties.TargetObject=((ulong)idOfTheStoneWolfAdd);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=4000;
            currentProperties.DestoryAt=15500;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_1_Tactical_Pack_Windborne_End_Guidance_3";
            currentProperties.Scale=new(1.5f);
            currentProperties.Owner=((ulong)idOfTheWindFont);
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=4000;
            currentProperties.DestoryAt=15500;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
            System.Threading.Thread.MemoryBarrier();

            windGuidanceHasBeenDrawn=true;

        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Windborne End Guidance 2)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:regex:^(41965|43137|43519)$"],
            suppress:2500,
            userControl:false)]
    
        public void Phase_1_Tactical_Pack_Windborne_End_Guidance_2(Event @event,ScriptAccessory accessory) {
            
            if(!windGuidanceHasBeenDrawn) {

                return;

            }

            else {

                windGuidanceHasBeenDrawn=false;

            }

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            // 41965 Wind Surge, 43137 Wind Surge (Last) and 43519 Wind Surge (Add Death) from Font of Wind Aether.
            // 41966 Sand Surge, 43138 Sand Surge (Last) and 43520 Sand Surge (Add Death) from Font of Earth Aether.
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(currentAddRound!=roundForCleanse[myIndex]) {

                return;

            }

            if(!windborneEndWasApplied[myIndex]) {

                return;

            }

            if(idOfTheWindFont==null||idOfTheStoneWolfAdd==null) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            accessory.Method.RemoveDraw("Phase_1_Tactical_Pack_Windborne_End_Guidance_1");
            accessory.Method.RemoveDraw("Phase_1_Tactical_Pack_Windborne_End_Guidance_2");
            accessory.Method.RemoveDraw("Phase_1_Tactical_Pack_Windborne_End_Guidance_3");
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=((ulong)idOfTheStoneWolfAdd);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=2500;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            if(!string.Equals(@event["ActionId"],"43519")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(1.5f);
                currentProperties.Owner=((ulong)idOfTheWindFont);
                currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=2500;
                            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"Phase 1 Tactical Pack (Sub-phase 5 Control)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42825"],
            userControl:false)]
    
        public void Phase_1_Tactical_Pack_SubPhase_5_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }

            System.Threading.Thread.MemoryBarrier();

            currentSubPhase=6;
            
            accessory.Log.Debug("Now moving to Phase 1 Sub-phase 6.");
            
            for(int i=0;i<=3;++i) {
                
                accessory.Method.RemoveDraw($"Phase_1_Tactical_Pack_Direction_{i}");
                    
            }
            
            accessory.Method.RemoveDraw("Phase_1_Tactical_Pack_Earthborne_End_Guidance_1");
            accessory.Method.RemoveDraw("Phase_1_Tactical_Pack_Earthborne_End_Guidance_2");
            accessory.Method.RemoveDraw("Phase_1_Tactical_Pack_Earthborne_End_Guidance_3");
            
            accessory.Method.RemoveDraw("Phase_1_Tactical_Pack_Windborne_End_Guidance_1");
            accessory.Method.RemoveDraw("Phase_1_Tactical_Pack_Windborne_End_Guidance_2");
            accessory.Method.RemoveDraw("Phase_1_Tactical_Pack_Windborne_End_Guidance_3");
        
        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Rage (Fang Line)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41942"])]
    
        public void Phase_1_Terrestrial_Rage_Fang_Line(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=6) {

                return;

            }

            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
        
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6,30);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=2500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6,30);
            currentProperties.Owner=sourceId;
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=2000;
            currentProperties.DestoryAt=2000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
        
        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Rage (Stack)",
            eventType:EventTypeEnum.TargetIcon)]
    
        public void Phase_1_Terrestrial_Rage_Stack(Event @event,ScriptAccessory accessory) {

            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }

            if(iconId!=-283) { // 0x5D-0x178=-283

                return;
                
            }
            
            accessory.Log.Debug($"An expected icon ID was captured. iconId={iconId}");
            
            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=6) {

                return;

            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
            
                return;
            
            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf((uint)targetId);
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(targetIndex)) {

                return;

            }
            
            if(!isLegalIndex(myIndex)) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6);
            currentProperties.Owner=targetId;
            currentProperties.DestoryAt=5125;

            if(isDps(myIndex)==isDps(targetIndex)) {
                
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                
            }
            
            else {
                
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Rage (Outer Fang Acquisition)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:18220"],
            userControl:false)]
    
        public void Phase_1_Terrestrial_Rage_Outer_Fang_Acquisition(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=6) {

                return;

            }

            if(outerFangHasBeenCaptured) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER_OF_PHASE_1;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            if(Vector3.Distance(sourcePosition,ARENA_CENTER_OF_PHASE_1)<3.3) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            positionOfTheOuterFang=sourcePosition;
            rotationOfTheOuterFang=getRotation(positionOfTheOuterFang,ARENA_CENTER_OF_PHASE_1);
            
            System.Threading.Thread.MemoryBarrier();
            
            outerFangHasBeenCaptured=true;

            newNorthArrowSemaphore.Set();
            newNorthGuidanceSemaphore.Set();

        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Rage (Stack Acquisition)",
            eventType:EventTypeEnum.TargetIcon,
            userControl:false)]
    
        public void Phase_1_Terrestrial_Rage_Stack_Acquisition(Event @event,ScriptAccessory accessory) {

            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }

            if(iconId!=-283) { // 0x5D-0x178=-283

                return;
                
            }
            
            accessory.Log.Debug($"An expected icon ID was captured. iconId={iconId}");
            
            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=6) {

                return;

            }

            if(dpsStackFirst!=null) {

                return;

            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
            
                return;
            
            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf((uint)targetId);
            
            if(!isLegalIndex(targetIndex)) {

                return;

            }

            if(isDps(targetIndex)) {

                dpsStackFirst??=true;

            }

            else {
                
                dpsStackFirst??=false;
                
            }
            
            System.Threading.Thread.MemoryBarrier();

            roleStackSemaphore.Set();

        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Rage (New North Arrow)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:18220"],
            suppress:5000)]
    
        public void Phase_1_Terrestrial_Rage_New_North_Arrow(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=6) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            newNorthArrowSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                    
            currentProperties.Scale=new(2);
            currentProperties.Position=rotatePosition(new Vector3(100,0,97),ARENA_CENTER_OF_PHASE_1,rotationOfTheOuterFang);
            currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,91),ARENA_CENTER_OF_PHASE_1,rotationOfTheOuterFang);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            
            currentProperties.DestoryAt=stratOfTerrestrialRage switch {
                
                StratsOfTerrestrialRage.Full_Rinon_Or_RaidPlan_84d => 14250,
                StratsOfTerrestrialRage.Half_Rinon => 8500,
                _ => 0
                
            };
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);

            if(stratOfTerrestrialRage==StratsOfTerrestrialRage.Half_Rinon) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                    
                currentProperties.Scale=new(2);
                currentProperties.Position=new Vector3(100,0,97);
                currentProperties.TargetPosition=new Vector3(100,0,91);
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=8500;
                currentProperties.DestoryAt=5750;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Rage (First Set Guidance)",
            eventType:EventTypeEnum.TargetIcon)]
    
        public void Phase_1_Terrestrial_Rage_First_Set_Guidance(Event @event,ScriptAccessory accessory) {
            
            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }

            if(iconId!=-283) { // 0x5D-0x178=-283

                return;
                
            }
            
            accessory.Log.Debug($"An expected icon ID was captured. iconId={iconId}");
            
            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=6) {

                return;

            }

            if(firstSetGuidanceHasBeenDrawn) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            newNorthGuidanceSemaphore.WaitOne();
            roleStackSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();

            if(stratOfTerrestrialRage==StratsOfTerrestrialRage.Full_Rinon_Or_RaidPlan_84d
               ||
               stratOfTerrestrialRage==StratsOfTerrestrialRage.Half_Rinon) {
                
                Vector3 leftRangePosition1=new Vector3(90,0,99);
                Vector3 leftRangePosition2=new Vector3(90,0,101);
                Vector3 leftMeleePosition1=new Vector3(96,0,95);
                Vector3 leftMeleePosition2=new Vector3(95,0,93);
                Vector3 rightMeleePosition1=new Vector3(104,0,95);
                Vector3 rightMeleePosition2=new Vector3(105,0,93);
                Vector3 rightRangePosition1=new Vector3(110,0,99);
                Vector3 rightRangePosition2=new Vector3(110,0,101);
                Vector3 stackPosition1=new Vector3(100,0,107);
                Vector3 stackPosition2=new Vector3(100,0,105);
                
                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

                if(!isLegalIndex(myIndex)) {

                    return;

                }

                Vector3 myPosition1=ARENA_CENTER_OF_PHASE_1,myPosition2=ARENA_CENTER_OF_PHASE_1;
                var currentProperties=accessory.Data.GetDefaultDrawProperties();
                string prompt=string.Empty;
                
                if(dpsStackFirst==null) {

                    return;

                }

                if((bool)dpsStackFirst) {

                    if(isDps(myIndex)) {

                        myPosition1=stackPosition1;
                        myPosition2=stackPosition2;

                        prompt="Stack.";

                    }

                    else {

                        myPosition1=myIndex switch {
                            
                            0 => leftMeleePosition1,
                            1 => rightMeleePosition1,
                            2 => leftRangePosition1,
                            3 => rightRangePosition1,
                            _ => ARENA_CENTER_OF_PHASE_1
                            
                        };
                        
                        myPosition2=myIndex switch {
                            
                            0 => leftMeleePosition2,
                            1 => rightMeleePosition2,
                            2 => leftRangePosition2,
                            3 => rightRangePosition2,
                            _ => ARENA_CENTER_OF_PHASE_1
                            
                        };
                        
                        prompt="Spread.";

                    }
                    
                }

                else {
                    
                    if(isDps(myIndex)) {
                        
                        myPosition1=myIndex switch {
                            
                            4 => leftMeleePosition1,
                            5 => rightMeleePosition1,
                            6 => leftRangePosition1,
                            7 => rightRangePosition1,
                            _ => ARENA_CENTER_OF_PHASE_1
                            
                        };
                        
                        myPosition2=myIndex switch {
                            
                            4 => leftMeleePosition2,
                            5 => rightMeleePosition2,
                            6 => leftRangePosition2,
                            7 => rightRangePosition2,
                            _ => ARENA_CENTER_OF_PHASE_1
                            
                        };
                        
                        prompt="Spread.";

                    }

                    else {
                        
                        myPosition1=stackPosition1;
                        myPosition2=stackPosition2;
                        
                        prompt="Stack.";
                        
                    }
                    
                }

                if(Vector3.Equals(myPosition1,ARENA_CENTER_OF_PHASE_1)||Vector3.Equals(myPosition2,ARENA_CENTER_OF_PHASE_1)) {

                    return;

                }

                myPosition1=rotatePosition(myPosition1,ARENA_CENTER_OF_PHASE_1,rotationOfTheOuterFang);
                myPosition2=rotatePosition(myPosition2,ARENA_CENTER_OF_PHASE_1,rotationOfTheOuterFang);
                
                // From 0s to 3.75s:
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition1;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=3750;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Position=myPosition1;
                currentProperties.TargetPosition=myPosition2;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=3750;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                // From 3.75s:
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition2;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=3750;
                currentProperties.DestoryAt=1375;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                if(!string.IsNullOrWhiteSpace(prompt)) {
                    
                    if(enablePrompts) {
                        
                        accessory.Method.TextInfo(prompt,5125);
                        
                    }
                        
                    accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);

                }
                
            }
            
            System.Threading.Thread.MemoryBarrier();

            firstSetGuidanceHasBeenDrawn=true;
            
        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Rage (Shadow Line)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:18216"])]
    
        public void Phase_1_Terrestrial_Rage_Shadow_Line(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=6) {

                return;

            }

            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
        
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(8,40);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=2000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(8,40);
            currentProperties.Owner=sourceId;
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=1500;
            currentProperties.DestoryAt=1500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Rage (Wind Wolf Line)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42890"])]
    
        public void Phase_1_Terrestrial_Rage_Wind_Wolf_Line(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=6) {

                return;

            }

            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
        
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(8,40);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=1500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(8,40);
            currentProperties.Owner=sourceId;
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=1000;
            currentProperties.DestoryAt=1500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Rage (Shadow Acquisition)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:18216"],
            userControl:false)]
    
        public void Phase_1_Terrestrial_Rage_Shadow_Acquisition(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=6) {

                return;

            }

            if(numberOfShadows>=5) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            lock(lockOfShadowNumber) {
                
                ++numberOfShadows;
            
                System.Threading.Thread.MemoryBarrier();
            
                Vector3 sourcePosition=ARENA_CENTER_OF_PHASE_1;

                try {

                    sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

                } catch(Exception e) {
                
                    accessory.Log.Error("SourcePosition deserialization failed.");

                    return;

                }

                if(stratOfTerrestrialRage==StratsOfTerrestrialRage.Full_Rinon_Or_RaidPlan_84d) {
                    
                    shadowsAreOnTheCardinals=true;

                    if(getRotationDifference(positionOfTheOuterFang,sourcePosition,ARENA_CENTER_OF_PHASE_1)>-18.9f.DegToRad()) {
                        // This strat is... very mathematically unfriendly.
                        // There are eight possible positions for the new north at 45 degree intervals, but five for the shadows at 72 degree intervals. Meanwhile, the shadows can be on cardinals or intercardinals.
                        // My initial approach was to capture the shadow closest to the new north, but it went wrong in some situations.
                        // Later I took a different approach that was to always capture the next shadow clockwise. The mistakes became less but still existed.
                        // Finally, after a lot of mathematical work (mainly countless enumeration) combined with checking a ton of replays, I found out that it should allow an angle of -18 degrees, and capture the shadow closest to the new north based on this.
                        // After establishing a solid math model, my only question is, how can a player without plugin assistance find it accurately in combat? I guess this is how the Half Rinon strat was born for.
                        
                        if(Math.Abs(getRotationDifference(positionOfTheOuterFang,sourcePosition,ARENA_CENTER_OF_PHASE_1))<Math.Abs(refinedRotationForFullRinon)) {
                    
                            refinedRotationForFullRinon=getRotationDifference(positionOfTheOuterFang,sourcePosition,ARENA_CENTER_OF_PHASE_1);

                        }
                        
                    }
                    
                }

                else {
                    
                    if(Vector3.Distance(sourcePosition,new Vector3(100,0,92.5f))<0.375) {
                    
                        shadowsAreOnTheCardinals=true;
                    
                    }
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                if(numberOfShadows>=5) {
                    
                    if(stratOfTerrestrialRage==StratsOfTerrestrialRage.Full_Rinon_Or_RaidPlan_84d) {

                        refinedRotationForFullRinon=rotationOfTheOuterFang+refinedRotationForFullRinon;

                    }
                    
                    System.Threading.Thread.MemoryBarrier();

                    shadowSemaphore.Set();
                    
                    accessory.Log.Debug($"shadowsAreOnTheCardinals={shadowsAreOnTheCardinals}");

                }
                
            }

        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Rage (Second Set Guidance)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:18216"],
            suppress:2500)]
    
        public void Phase_1_Terrestrial_Rage_Second_Set_Guidance(Event @event,ScriptAccessory accessory) {
            
            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=6) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            shadowSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();

            if(stratOfTerrestrialRage==StratsOfTerrestrialRage.Full_Rinon_Or_RaidPlan_84d
               ||
               stratOfTerrestrialRage==StratsOfTerrestrialRage.Half_Rinon) {
                
                double topRotation=0;
                
                if(!shadowsAreOnTheCardinals) {

                    topRotation+=Math.PI/5;

                }

                if(stratOfTerrestrialRage==StratsOfTerrestrialRage.Full_Rinon_Or_RaidPlan_84d) {

                    topRotation=refinedRotationForFullRinon;

                }

                double rightmostRotation=topRotation+Math.PI*2/5;
                double lowerRightRotation=topRotation+Math.PI*2/5*2;
                double lowerLeftRotation=topRotation+Math.PI*2/5*3;
                double leftmostRotation=topRotation+Math.PI*2/5*4;
            
                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

                if(!isLegalIndex(myIndex)) {

                    return;

                }

                double myRotation=0;
                Vector3 myPosition1=new Vector3(100,0,90),myPosition2=new Vector3(100,0,90);
                var currentProperties=accessory.Data.GetDefaultDrawProperties();
                string prompt=string.Empty;
                
                if(dpsStackFirst==null) {

                    return;

                }

                if((bool)dpsStackFirst) {

                    if(isDps(myIndex)) {

                        myRotation=myIndex switch {
                            
                            4 => lowerLeftRotation,
                            5 => lowerRightRotation,
                            6 => leftmostRotation,
                            7 => rightmostRotation,
                            _ => 0
                            
                        };

                        prompt="Spread.";

                    }

                    else {

                        myRotation=topRotation;
                        
                        prompt="Stack.";

                    }
                    
                }

                else {
                    
                    if(isDps(myIndex)) {
                        
                        myRotation=topRotation;
                        
                        prompt="Stack.";

                    }

                    else {
                        
                        myRotation=myIndex switch {
                            
                            2 => lowerLeftRotation,
                            3 => lowerRightRotation,
                            0 => leftmostRotation,
                            1 => rightmostRotation,
                            _ => 0
                            
                        };

                        prompt="Spread.";
                        
                    }
                    
                }

                myPosition1=rotatePosition(myPosition1,ARENA_CENTER_OF_PHASE_1,myRotation);
                myPosition2=rotatePosition(myPosition1,ARENA_CENTER_OF_PHASE_1,Math.PI/5);
                
                // From 0s to 3s:
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition1;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=3000;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Position=myPosition1;
                currentProperties.TargetPosition=myPosition2;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=3000;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                // From 3s:
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition2;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=3000;
                currentProperties.DestoryAt=4500;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                System.Threading.Thread.MemoryBarrier();
                
                if(!string.IsNullOrWhiteSpace(prompt)) {
                    
                    if(enablePrompts) {
                        
                        accessory.Method.TextInfo(prompt,3000);
                        
                    }
                        
                    accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);

                }
                
            }
            
        }
        
        [ScriptMethod(name:"Phase 1 Terrestrial Rage (Sub-phase 6 Control)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:42890"],
            suppress:2500,
            userControl:false)]
    
        public void Phase_1_Terrestrial_Rage_SubPhase_6_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=6) {

                return;

            }

            System.Threading.Thread.MemoryBarrier();

            currentSubPhase=7;
            
            newNorthArrowSemaphore.Reset();
            newNorthGuidanceSemaphore.Reset();
            roleStackSemaphore.Reset();
            shadowSemaphore.Reset();
            
            accessory.Log.Debug("Now moving to Phase 1 Sub-phase 7.");
        
        }
        
        [ScriptMethod(name:"Phase 1 Intermission Regins (Sub-phase 7 Control)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41921"],
            userControl:false)]
    
        public void Phase_1_Intermission_Regins_SubPhase_7_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=7) {

                return;

            }

            System.Threading.Thread.MemoryBarrier();

            currentSubPhase=8;

            dpsStackFirst=null;
            firstSetGuidanceHasBeenDrawn=false;

            roleStackSemaphore.Reset();
            
            accessory.Log.Debug("Now moving to Phase 1 Sub-phase 8.");
        
        }
        
        [ScriptMethod(name:"Phase 1 Beckon Moonlight (Cleave)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(41922|41923)$"])]
    
        public void Phase_1_Beckon_Moonlight_Cleave(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=8) {

                return;

            }
            
            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            // 41922: Right
            // 41923: Left

            currentProperties.Scale=new(15);
            currentProperties.Owner=sourceId;
            currentProperties.Radian=float.Pi;
            currentProperties.Offset=new Vector3(0,0,-12);
            currentProperties.DestoryAt=7500;
            currentProperties.Color=accessory.Data.DefaultDangerColor;

            if(string.Equals(@event["ActionId"],"41923")) {
                        
                currentProperties.Rotation=float.Pi/2;
                        
            }

            if(string.Equals(@event["ActionId"],"41922")) {
                        
                currentProperties.Rotation=-float.Pi/2;
                        
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(15);
            currentProperties.Owner=sourceId;
            currentProperties.Radian=float.Pi;
            currentProperties.Offset=new Vector3(0,0,-12);
            currentProperties.Delay=7500;
            currentProperties.DestoryAt=1500;
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);

            if(string.Equals(@event["ActionId"],"41923")) {
                        
                currentProperties.Rotation=float.Pi/2;
                        
            }

            if(string.Equals(@event["ActionId"],"41922")) {
                        
                currentProperties.Rotation=-float.Pi/2;
                        
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Fan,currentProperties);
        
        }
        
        [ScriptMethod(name:"Phase 1 Beckon Moonlight (Stack)",
            eventType:EventTypeEnum.TargetIcon)]
    
        public void Phase_1_Beckon_Moonlight_Stack(Event @event,ScriptAccessory accessory) {

            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }

            if(iconId!=-283) { // 0x5D-0x178=-283

                return;
                
            }
            
            accessory.Log.Debug($"An expected icon ID was captured. iconId={iconId}");
            
            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=8) {

                return;

            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
            
                return;
            
            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf((uint)targetId);
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(targetIndex)) {

                return;

            }
            
            if(!isLegalIndex(myIndex)) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6);
            currentProperties.Owner=targetId;
            currentProperties.DestoryAt=5125;

            if(isDps(myIndex)==isDps(targetIndex)) {
                
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                
            }
            
            else {
                
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"Phase 1 Beckon Moonlight (Cleave Acquisition)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(41922|41923)$"],
            userControl:false)]
    
        public void Phase_1_Beckon_Moonlight_Cleave_Acquisition(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=8) {

                return;

            }

            if(moonbeamsBite.Count>=4) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            Vector3 sourcePosition=ARENA_CENTER_OF_PHASE_1;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            if(sourcePosition.Equals(ARENA_CENTER_OF_PHASE_1)) {

                return;

            }
            
            // 0=Northeast, 1=southeast, 2=southwest, 3=northwest.

            List<int> leftHalf=[2,3];
            List<int> rightHalf=[0,1];
            List<int> topHalf=[3,0];
            List<int> bottomHalf=[1,2];
            
            int discretizedPosition=discretizePosition(sourcePosition,ARENA_CENTER_OF_PHASE_1,4);
            
            // 41922: Right
            // 41923: Left

            switch(discretizedPosition) {

                case 0: {
                    
                    if(string.Equals(@event["ActionId"],"41923")) {
                        
                        moonbeamsBite.Add(rightHalf);
                        
                    }

                    if(string.Equals(@event["ActionId"],"41922")) {
                        
                        moonbeamsBite.Add(leftHalf);
                        
                    }

                    break;

                }
                
                case 1: {
                    
                    if(string.Equals(@event["ActionId"],"41923")) {
                        
                        moonbeamsBite.Add(bottomHalf);
                        
                    }

                    if(string.Equals(@event["ActionId"],"41922")) {
                        
                        moonbeamsBite.Add(topHalf);
                        
                    }

                    break;

                }
                
                case 2: {
                    
                    if(string.Equals(@event["ActionId"],"41923")) {
                        
                        moonbeamsBite.Add(leftHalf);
                        
                    }

                    if(string.Equals(@event["ActionId"],"41922")) {
                        
                        moonbeamsBite.Add(rightHalf);
                        
                    }

                    break;

                }
                
                case 3: {
                    
                    if(string.Equals(@event["ActionId"],"41923")) {
                        
                        moonbeamsBite.Add(topHalf);
                        
                    }

                    if(string.Equals(@event["ActionId"],"41922")) {
                        
                        moonbeamsBite.Add(bottomHalf);
                        
                    }

                    break;

                }

                default: {

                    return;

                }
                
            }
                
            System.Threading.Thread.MemoryBarrier();

            if(moonbeamsBite.Count==2) {

                secondMoonbeamsBiteSemaphore.Set();

            }
            
            if(moonbeamsBite.Count>=4) {

                fourthMoonbeamsBiteSemaphore.Set();

            }

        }
        
        [ScriptMethod(name:"Phase 1 Beckon Moonlight (Stack Acquisition)",
            eventType:EventTypeEnum.TargetIcon,
            userControl:false)]
    
        public void Phase_1_Beckon_Moonlight_Stack_Acquisition(Event @event,ScriptAccessory accessory) {

            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }

            if(iconId!=-283) { // 0x5D-0x178=-283

                return;
                
            }
            
            accessory.Log.Debug($"An expected icon ID was captured. iconId={iconId}");
            
            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=8) {

                return;

            }
            
            if(dpsStackFirst!=null) {

                return;

            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
            
                return;
            
            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf((uint)targetId);
            
            if(!isLegalIndex(targetIndex)) {

                return;

            }
            
            accessory.Log.Debug($"targetindex={targetIndex}");

            if(isDps(targetIndex)) {

                dpsStackFirst??=true;

            }

            else {
                
                dpsStackFirst??=false;
                
            }
            
            System.Threading.Thread.MemoryBarrier();

            roleStackSemaphore.Set();

        }
        
        [ScriptMethod(name:"Phase 1 Beckon Moonlight (First Set Guidance)",
            eventType:EventTypeEnum.TargetIcon)]
    
        public void Phase_1_Beckon_Moonlight_First_Set_Guidance(Event @event,ScriptAccessory accessory) {

            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }

            if(iconId!=-283) { // 0x5D-0x178=-283

                return;
                
            }
            
            accessory.Log.Debug($"An expected icon ID was captured. iconId={iconId}");
            
            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=8) {

                return;

            }
            
            if(firstSetGuidanceHasBeenDrawn) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            secondMoonbeamsBiteSemaphore.WaitOne();
            roleStackSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            if(stratOfBeckonMoonlight==StratsOfBeckonMoonlight.Quad_Or_RaidPlan_84d) {
                
                List<int> riskIndexAfterTheSecondCleave=[0,0,0,0]; // Northeast, southeast, southwest, northwest accordingly.

                if(moonbeamsBite.Count<2) {

                    return;

                }

                ++riskIndexAfterTheSecondCleave[moonbeamsBite[0][0]];
                ++riskIndexAfterTheSecondCleave[moonbeamsBite[0][1]];
            
                ++riskIndexAfterTheSecondCleave[moonbeamsBite[1][0]];
                ++riskIndexAfterTheSecondCleave[moonbeamsBite[1][1]];

                int safeQuarter=riskIndexAfterTheSecondCleave.IndexOf(0);

                if(safeQuarter<0||safeQuarter>3) {

                    return;

                }
                
                accessory.Log.Debug($"Moonbean's Bite 2. safeQuarter={safeQuarter}");
                
                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(12);
                currentProperties.Position=ARENA_CENTER_OF_PHASE_1;
                currentProperties.TargetPosition=new Vector3(100,0,88);
                currentProperties.Radian=float.Pi/2;
                currentProperties.Rotation=-float.Pi/4-(float.Pi/2*safeQuarter);
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=5125;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Fan,currentProperties);
                
                Vector3 leftRangePosition=new Vector3(107.641f,0,91.661f);
                Vector3 leftMeleePosition=new Vector3(103.250f,0,96.453f);
                Vector3 rightMeleePosition=new Vector3(96.750f,0,96.453f);
                Vector3 rightRangePosition=new Vector3(92.359f,0,91.661f);
                Vector3 stackPosition=new Vector3(100,0,88.689f);
                // Initial positions: https://www.geogebra.org/calculator/eanrxfaa
                // Mirror images need to be considered here. Therefore, the left and right sides on the Geogebra graph should be reversed.
                
                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

                if(!isLegalIndex(myIndex)) {

                    return;

                }

                Vector3 myPosition=ARENA_CENTER_OF_PHASE_1;
                string prompt=string.Empty;
                
                if(dpsStackFirst==null) {

                    return;

                }

                if((bool)dpsStackFirst) {

                    if(isDps(myIndex)) {

                        myPosition=stackPosition;

                        prompt="Stack.";

                    }

                    else {

                        myPosition=myIndex switch {
                            
                            0 => leftMeleePosition,
                            1 => rightMeleePosition,
                            2 => leftRangePosition,
                            3 => rightRangePosition,
                            _ => ARENA_CENTER_OF_PHASE_1
                            
                        };
                        
                        prompt="Spread.";

                    }
                    
                }

                else {
                    
                    if(isDps(myIndex)) {
                        
                        myPosition=myIndex switch {
                            
                            4 => leftMeleePosition,
                            5 => rightMeleePosition,
                            6 => leftRangePosition,
                            7 => rightRangePosition,
                            _ => ARENA_CENTER_OF_PHASE_1
                            
                        };
                        
                        prompt="Spread.";

                    }

                    else {
                        
                        myPosition=stackPosition;
                        
                        prompt="Stack.";
                        
                    }
                    
                }

                if(Vector3.Equals(myPosition,ARENA_CENTER_OF_PHASE_1)) {

                    return;

                }

                myPosition=rotatePosition(myPosition,ARENA_CENTER_OF_PHASE_1,float.Pi/4+(float.Pi/2*safeQuarter));
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=5125;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                if(!string.IsNullOrWhiteSpace(prompt)) {
                    
                    if(enablePrompts) {
                        
                        accessory.Method.TextInfo(prompt,5125);
                        
                    }
                        
                    accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);

                }
                
            }
            
            System.Threading.Thread.MemoryBarrier();

            firstSetGuidanceHasBeenDrawn=true;

        }
        
        [ScriptMethod(name:"Phase 1 Beckon Moonlight (Second Set Guidance)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:41920"],
            suppress:2500)]
    
        public void Phase_1_Beckon_Moonlight_Second_Set_Guidance(Event @event,ScriptAccessory accessory) {
            
            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=8) {

                return;

            }
            
            if(secondSetGuidanceHasBeenDrawn) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            fourthMoonbeamsBiteSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            if(stratOfBeckonMoonlight==StratsOfBeckonMoonlight.Quad_Or_RaidPlan_84d) {
                
                List<int> riskIndexAfterTheFourthCleave=[0,0,0,0]; // Northeast, southeast, southwest, northwest accordingly.

                if(moonbeamsBite.Count<4) {

                    return;

                }

                ++riskIndexAfterTheFourthCleave[moonbeamsBite[2][0]];
                ++riskIndexAfterTheFourthCleave[moonbeamsBite[2][1]];
            
                ++riskIndexAfterTheFourthCleave[moonbeamsBite[3][0]];
                ++riskIndexAfterTheFourthCleave[moonbeamsBite[3][1]];

                int safeQuarter=riskIndexAfterTheFourthCleave.IndexOf(0);

                if(safeQuarter<0||safeQuarter>3) {

                    return;

                }
                
                List<int> theLastCleave=[-1,-1,-1,-1]; // Northeast, southeast, southwest, northwest accordingly.

                for(int i=0;i<4;++i) {

                    theLastCleave[moonbeamsBite[i][0]]=i;
                    theLastCleave[moonbeamsBite[i][1]]=i;

                }
                
                int theLastCleaveOfTheSafeQuarter=theLastCleave[safeQuarter];
                
                accessory.Log.Debug($"Moonbean's Bite 4. safeQuarter={safeQuarter}, theLastCleaveOfTheSafeQuarter={theLastCleaveOfTheSafeQuarter}");
            
                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(12);
                currentProperties.Position=ARENA_CENTER_OF_PHASE_1;
                currentProperties.TargetPosition=new Vector3(100,0,88);
                currentProperties.Radian=float.Pi/2;
                currentProperties.Rotation=-float.Pi/4-(float.Pi/2*safeQuarter);
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=Math.Max(1500+2000*theLastCleaveOfTheSafeQuarter,0);
                // The variable theLastCleaveOfTheSafeQuarter could be -1 if the safe quarter remains. The expression result may be negative.
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Fan,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(12);
                currentProperties.Position=ARENA_CENTER_OF_PHASE_1;
                currentProperties.TargetPosition=new Vector3(100,0,88);
                currentProperties.Radian=float.Pi/2;
                currentProperties.Rotation=-float.Pi/4-(float.Pi/2*safeQuarter);
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=Math.Max(1500+2000*theLastCleaveOfTheSafeQuarter,0);
                currentProperties.DestoryAt=8500-Math.Max(1500+2000*theLastCleaveOfTheSafeQuarter,0);
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Fan,currentProperties);
                
                Vector3 leftRangePosition=new Vector3(107.641f,0,91.661f);
                Vector3 leftMeleePosition=new Vector3(103.250f,0,96.453f);
                Vector3 rightMeleePosition=new Vector3(96.750f,0,96.453f);
                Vector3 rightRangePosition=new Vector3(92.359f,0,91.661f);
                Vector3 stackPosition=new Vector3(100,0,88.689f);
                // Initial positions: https://www.geogebra.org/calculator/eanrxfaa
                // Mirror images need to be considered here. Therefore, the left and right sides on the Geogebra graph should be reversed.
                
                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

                if(!isLegalIndex(myIndex)) {

                    return;

                }

                Vector3 myPosition=ARENA_CENTER_OF_PHASE_1;
                string prompt=string.Empty;
                
                if(dpsStackFirst==null) {

                    return;

                }

                if((bool)dpsStackFirst) {

                    if(isDps(myIndex)) {
                        
                        myPosition=myIndex switch {
                            
                            4 => leftMeleePosition,
                            5 => rightMeleePosition,
                            6 => leftRangePosition,
                            7 => rightRangePosition,
                            _ => ARENA_CENTER_OF_PHASE_1
                            
                        };

                        prompt="Spread.";

                    }

                    else {
                        
                        myPosition=stackPosition;

                        prompt="Stack.";

                    }
                    
                }

                else {
                    
                    if(isDps(myIndex)) {
                        
                        myPosition=stackPosition;
                        
                        prompt="Stack.";

                    }

                    else {
                        
                        myPosition=myIndex switch {
                            
                            0 => leftMeleePosition,
                            1 => rightMeleePosition,
                            2 => leftRangePosition,
                            3 => rightRangePosition,
                            _ => ARENA_CENTER_OF_PHASE_1
                            
                        };
                        
                        prompt="Spread.";
                        
                    }
                    
                }

                if(Vector3.Equals(myPosition,ARENA_CENTER_OF_PHASE_1)) {

                    return;

                }

                myPosition=rotatePosition(myPosition,ARENA_CENTER_OF_PHASE_1,float.Pi/4+(float.Pi/2*safeQuarter));
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=Math.Max(1500+2000*theLastCleaveOfTheSafeQuarter,0);
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=Math.Max(1500+2000*theLastCleaveOfTheSafeQuarter,0);
                currentProperties.DestoryAt=8500-Math.Max(1500+2000*theLastCleaveOfTheSafeQuarter,0);
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                if(!string.IsNullOrWhiteSpace(prompt)) {
                    
                    System.Threading.Thread.Sleep(2500);
                    
                    if(enablePrompts) {
                        
                        accessory.Method.TextInfo(prompt,6000);
                        
                    }
                
                    accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);

                }
                
            }
            
            System.Threading.Thread.MemoryBarrier();

            secondSetGuidanceHasBeenDrawn=true;

        }
        
        [ScriptMethod(name:"Phase 1 Beckon Moonlight (Line)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42897"])]
    
        public void Phase_1_Beckon_Moonlight_Line(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=8) {

                return;

            }
            
            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6,25);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=2500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
        
        }
        
        [ScriptMethod(name:"Phase 1 Beckon Moonlight (Sub-phase 8 Control)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:42897"],
            suppress:2500,
            userControl:false)]
    
        public void Phase_1_Beckon_Moonlight_SubPhase_8_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=8) {

                return;

            }

            System.Threading.Thread.MemoryBarrier();

            currentSubPhase=9;

            roleStackSemaphore.Reset();
            secondMoonbeamsBiteSemaphore.Reset();
            fourthMoonbeamsBiteSemaphore.Reset();
            
            accessory.Log.Debug("Now moving to Phase 1 Sub-phase 9.");
        
        }
        
        [ScriptMethod(name:"Phase 1 Cutscenes (Phase 1 Control)",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:18227"],
            suppress:2500,
            userControl:false)]

        public void Phase_1_Cutscenes_Phase_1_Control(Event @event, ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=9) {

                return;

            }

            System.Threading.Thread.MemoryBarrier();

            currentPhase=2;

            currentSubPhase=1;
            
            accessory.Log.Debug("Now moving to Phase 2 Sub-phase 1.");
            
            accessory.Method.RemoveDraw(".*");

        }

        #endregion
        
        #region Phase_2
        
        [ScriptMethod(name:"Phase 2 North-south Axis And Arrows",
            eventType:EventTypeEnum.Targetable)]

        public void Phase_2_NorthSouth_Axis_And_Arrows(Event @event, ScriptAccessory accessory) {

            if(axisAndArrowsHaveBeenDrawn) {

                return;

            }

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }
            
            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }

            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null) {

                return;

            }

            if(sourceObject.DataId!=18222) {

                return;

            }

            System.Threading.Thread.MemoryBarrier();
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_2_NorthSouth_Axis_And_Arrows_1";
            currentProperties.Scale=new(0.5f,51);
            currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
            currentProperties.Color=colourOfTheNorthSouthAxis.V4.WithW(1);
            currentProperties.DestoryAt=420000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Straight,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_2_NorthSouth_Axis_And_Arrows_2";
            currentProperties.Scale=new(2,9);
            currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
            currentProperties.Color=colourOfTheNorthSouthAxis.V4.WithW(1);
            currentProperties.DestoryAt=420000;

            if(arrowsPointSouth) {

                currentProperties.Rotation=0;
                currentProperties.Offset=new Vector3(5.562f,0,4.5f);

            }

            else {
                
                currentProperties.Rotation=float.Pi;
                currentProperties.Offset=new Vector3(5.562f,0,-4.5f);
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_2_NorthSouth_Axis_And_Arrows_3";
            currentProperties.Scale=new(2,9);
            currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
            currentProperties.Color=colourOfTheNorthSouthAxis.V4.WithW(1);
            currentProperties.DestoryAt=420000;
            
            if(arrowsPointSouth) {

                currentProperties.Rotation=0;
                currentProperties.Offset=new Vector3(-5.562f,0,4.5f);

            }

            else {
                
                currentProperties.Rotation=float.Pi;
                currentProperties.Offset=new Vector3(-5.562f,0,-4.5f);
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
            
            System.Threading.Thread.MemoryBarrier();

            axisAndArrowsHaveBeenDrawn=true;

        }

        private static Vector3 getPlatformCenter(PlatformsOfPhase2 targetPlatform) {

            if(((int)targetPlatform)<0||((int)targetPlatform)>4) {

                return ARENA_CENTER_OF_PHASE_2;

            }
            
            return rotatePosition(RAW_PLATFORM_CENTER,ARENA_CENTER_OF_PHASE_2,Math.PI*2/5*((int)targetPlatform));
            
        }
        
        [ScriptMethod(name:"Phase 2 Quake III (Round Control)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42074"],
            userControl:false)]
    
        public void Phase_2_Quake_III_Round_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            ++roundOfQuakeIii;

        }
        
        [ScriptMethod(name:"Phase 2 Quake III (Guidance)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42074"])]
    
        public void Phase_2_Quake_III_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {

                var currentProperties=accessory.Data.GetDefaultDrawProperties();
                string prompt=string.Empty;

                if(isInGroup1(myIndex)) {

                    // The correct platform:
                        
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                    prompt=getPlatformDescription(PlatformsOfPhase2.SOUTHWEST);
                        
                    // The another group:
                        
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=5125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                }

                if(isInGroup2(myIndex)) {
                    
                    // The correct platform:
                        
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                    prompt=getPlatformDescription(PlatformsOfPhase2.SOUTHEAST);
                        
                    // The another group:
                        
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=5125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
                if(!string.IsNullOrWhiteSpace(prompt)) {
                    
                    if(enablePrompts) {
                    
                        accessory.Method.TextInfo(prompt,5125);
                    
                    }
                    
                    accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);

                }
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 2 Gleaming Beam",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42078"])]
    
        public void Phase_2_Gleaming_Beam(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }
            
            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(8,31);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=4000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
        
        }
        
        [ScriptMethod(name:"Phase 2 Ultraviolent Ray (Acquisition)",
            eventType:EventTypeEnum.TargetIcon,
            userControl:false)]
    
        public void Phase_2_Ultraviolent_Ray_Acquisition(Event @event,ScriptAccessory accessory) {

            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }

            if(iconId!=-362) { // 0xE-0x178=-362

                return;
                
            }
            
            accessory.Log.Debug($"An expected icon ID was captured. iconId={iconId}");

            if(currentPhase!=2) {

                return;

            }

            if(numberOfUltraviolentRay>=5) {

                return;
                
            }
            
            System.Threading.Thread.MemoryBarrier();

            lock(playerWasMarkedByAUltraviolentRay) {
                
                ++numberOfUltraviolentRay;
                
                System.Threading.Thread.MemoryBarrier();
                
                if(!convertObjectId(@event["TargetId"], out var targetId)) {
                
                    return;
                
                }

                int targetIndex=accessory.Data.PartyList.IndexOf((uint)targetId);
            
                if(!isLegalIndex(targetIndex)) {

                    return;

                }

                playerWasMarkedByAUltraviolentRay[targetIndex]=true;
                
                System.Threading.Thread.MemoryBarrier();
                
                if(numberOfUltraviolentRay>=5) {

                    ++roundOfUltraviolentRay;
                    
                    System.Threading.Thread.MemoryBarrier();

                    ultraviolentRaySemaphore.Set();

                }

            }
        
        }
        
        private static string getPlatformDescription(PlatformsOfPhase2 targetPlatform) {

            return targetPlatform switch {
                
                PlatformsOfPhase2.NORTHEAST => "Northeast.",
                PlatformsOfPhase2.SOUTHEAST => "Southeast.",
                PlatformsOfPhase2.SOUTH => "South.",
                PlatformsOfPhase2.SOUTHWEST => "Southwest.",
                PlatformsOfPhase2.NORTHWEST => "Northwest.",
                _ => string.Empty
                
            };

        }

        private bool isInGroupNorthwest(int partyIndex) {

            if(!isLegalIndex(partyIndex)) {

                return false;

            }

            return (isTank(partyIndex))||(partyIndex==dpsWithTheCloseTank)||(partyIndex==dpsWithTheFarHealer);

        }
        
        [ScriptMethod(name:"Phase 2 Ultraviolent Ray (Guidance)",
            eventType:EventTypeEnum.TargetIcon,
            suppress:2500)]
    
        public void Phase_2_Ultraviolent_Ray_Guidance(Event @event,ScriptAccessory accessory) {

            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }

            if(iconId!=-362) { // 0xE-0x178=-362

                return;
                
            }
            
            accessory.Log.Debug($"An expected icon ID was captured. iconId={iconId}");

            if(currentPhase!=2) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            ultraviolentRaySemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {
                
                Vector3 myPosition=ARENA_CENTER_OF_PHASE_2;
                string prompt=string.Empty;
                
                if((roundOfUltraviolentRay!=4)
                   ||
                   (roundOfUltraviolentRay==4&&stratOfUltraviolentRay4==StratsOfUltraviolentRay4.Same_As_Usual)) {

                    if(!playerWasMarkedByAUltraviolentRay[myIndex]) {

                        if(isInGroup1(myIndex)) {

                            myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);

                        }

                        if(isInGroup2(myIndex)) {
                    
                            myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                    
                        }

                        if(myPosition.Equals(ARENA_CENTER_OF_PHASE_2)) {

                            return;

                        }

                        prompt="Stay.";

                    }

                    else {
                        
                        List<int> marksOnTheLeft=[],marksOnTheRight=[];

                        if(playerWasMarkedByAUltraviolentRay[0])marksOnTheLeft.Add(0);
                        if(playerWasMarkedByAUltraviolentRay[1])marksOnTheRight.Add(1);
                        
                        if(playerWasMarkedByAUltraviolentRay[2])marksOnTheLeft.Add(2);
                        if(playerWasMarkedByAUltraviolentRay[3])marksOnTheRight.Add(3);
                        
                        if(playerWasMarkedByAUltraviolentRay[6])marksOnTheLeft.Add(6);
                        if(playerWasMarkedByAUltraviolentRay[7])marksOnTheRight.Add(7);
                        
                        if(playerWasMarkedByAUltraviolentRay[4])marksOnTheLeft.Add(4);
                        if(playerWasMarkedByAUltraviolentRay[5])marksOnTheRight.Add(5);

                        int temporaryOrder=-1;

                        if(isInGroup1(myIndex)) {
                        
                            temporaryOrder=marksOnTheLeft.IndexOf(myIndex);
                        
                        }
                    
                        if(isInGroup2(myIndex)) {
                        
                            temporaryOrder=marksOnTheRight.IndexOf(myIndex);
                        
                        }

                        ++temporaryOrder;

                        if(temporaryOrder<1||temporaryOrder>3) {
                            
                            return;
                            
                        }
                        
                        accessory.Log.Debug($"marksOnTheLeft={string.Join(",",marksOnTheLeft)}, marksOnTheRight={string.Join(",",marksOnTheRight)}, temporaryOrder={temporaryOrder}");

                        if(isInGroup1(myIndex)) {

                            switch(temporaryOrder) {

                                case 1: {

                                    myPosition=getPlatformCenter(PlatformsOfPhase2.NORTHWEST);
                                    prompt=getPlatformDescription(PlatformsOfPhase2.NORTHWEST);

                                    break;

                                }
                                
                                case 2: {
                                    
                                    myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                                    prompt="Stay.";

                                    break;

                                }
                                
                                case 3: {
                                    
                                    myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTH);
                                    prompt=getPlatformDescription(PlatformsOfPhase2.SOUTH);

                                    break;

                                }
                                
                                default: {

                                    return;

                                }
                                
                            }

                        }

                        if(isInGroup2(myIndex)) {
                    
                            switch(temporaryOrder) {

                                case 1: {

                                    myPosition=getPlatformCenter(PlatformsOfPhase2.NORTHEAST);
                                    prompt=getPlatformDescription(PlatformsOfPhase2.NORTHEAST);

                                    break;

                                }
                                
                                case 2: {
                                    
                                    myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                                    prompt="Stay.";

                                    break;

                                }
                                
                                case 3: {
                                    
                                    myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTH);
                                    prompt=getPlatformDescription(PlatformsOfPhase2.SOUTH);

                                    break;

                                }
                                
                                default: {

                                    return;

                                }
                                
                            }
                    
                        }

                        if(myPosition.Equals(ARENA_CENTER_OF_PHASE_2)) {

                            return;

                        }
                        
                    }

                }

                else {

                    if(stratOfUltraviolentRay4==StratsOfUltraviolentRay4.Northwest_And_South) {
                        
                        if(!playerWasMarkedByAUltraviolentRay[myIndex]) {

                            if(isInGroupNorthwest(myIndex)) {

                                myPosition=getPlatformCenter(PlatformsOfPhase2.NORTHWEST);

                            }

                            else {
                    
                                myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTH);
                    
                            }

                            prompt="Stay.";

                        }
                        
                        else {
                        
                            List<int> playersOnTheNorthwest=[tankWithTheFarDps,dpsWithTheCloseTank,dpsWithTheFarHealer,tankWithTheCloseDps],
                                      playersOnTheSouth=[healerWithTheFarDps,dpsWithTheFarTank,dpsWithTheCloseHealer,healerWithTheCloseDps];
                            
                            playersOnTheNorthwest.Sort((x,y) => {

                                var xObject=accessory.Data.Objects.SearchById(accessory.Data.PartyList[x]);
                                var yObject=accessory.Data.Objects.SearchById(accessory.Data.PartyList[y]);

                                if(xObject==null||yObject==null) {

                                    return 0;

                                }

                                double xRotation=getRotation(xObject.Position,ARENA_CENTER_OF_PHASE_2);
                                double yRotation=getRotation(yObject.Position,ARENA_CENTER_OF_PHASE_2);

                                if(xRotation>yRotation) {

                                    return -1;

                                }

                                else {
                                    
                                    if(xRotation<yRotation) {

                                        return 1;

                                    }

                                    else {

                                        return 0;

                                    }
                                    
                                }

                            });
                            
                            playersOnTheSouth.Sort((x,y) => {

                                var xObject=accessory.Data.Objects.SearchById(accessory.Data.PartyList[x]);
                                var yObject=accessory.Data.Objects.SearchById(accessory.Data.PartyList[y]);

                                if(xObject==null||yObject==null) {

                                    return 0;

                                }

                                double xRotation=getRotation(xObject.Position,ARENA_CENTER_OF_PHASE_2);
                                double yRotation=getRotation(yObject.Position,ARENA_CENTER_OF_PHASE_2);

                                if(xRotation<yRotation) {

                                    return -1;

                                }

                                else {
                                    
                                    if(xRotation>yRotation) {

                                        return 1;

                                    }

                                    else {

                                        return 0;

                                    }
                                    
                                }

                            });
                            
                            List<int> marksOnTheNorthwest=[],marksOnTheSouth=[];

                            for(int i=0;i<4;++i) {
                                
                                if(playerWasMarkedByAUltraviolentRay[playersOnTheNorthwest[i]])marksOnTheNorthwest.Add(playersOnTheNorthwest[i]);
                                if(playerWasMarkedByAUltraviolentRay[playersOnTheSouth[i]])marksOnTheSouth.Add(playersOnTheSouth[i]);
                                
                            }

                            int temporaryOrder=-1;

                            if(isInGroupNorthwest(myIndex)) {
                        
                                temporaryOrder=marksOnTheNorthwest.IndexOf(myIndex);
                        
                            }
                    
                            else {
                        
                                temporaryOrder=marksOnTheSouth.IndexOf(myIndex);
                        
                            }

                            ++temporaryOrder;

                            if(temporaryOrder<1||temporaryOrder>3) {
                            
                                return;
                            
                            }
                        
                            accessory.Log.Debug($"marksOnTheNorthwest={string.Join(",",marksOnTheNorthwest)}, marksOnTheSouth={string.Join(",",marksOnTheSouth)}, temporaryOrder={temporaryOrder}");
                            
                            if(isInGroupNorthwest(myIndex)) {

                                switch(temporaryOrder) {

                                    case 1: {

                                        myPosition=getPlatformCenter(PlatformsOfPhase2.NORTHEAST);
                                        prompt=getPlatformDescription(PlatformsOfPhase2.NORTHEAST);

                                        break;

                                    }
                                
                                    case 2: {
                                    
                                        myPosition=getPlatformCenter(PlatformsOfPhase2.NORTHWEST);
                                        prompt="Stay.";

                                        break;

                                    }
                                
                                    case 3: {
                                    
                                        myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                                        prompt=getPlatformDescription(PlatformsOfPhase2.SOUTHWEST);

                                        break;

                                    }
                                
                                    default: {

                                        return;

                                    }
                                
                                }

                            }

                            else {
                    
                                switch(temporaryOrder) {

                                    case 1: {

                                        myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                                        prompt=getPlatformDescription(PlatformsOfPhase2.SOUTHEAST);

                                        break;

                                    }
                                
                                    case 2: {
                                    
                                        myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTH);
                                        prompt="Stay.";

                                        break;

                                    }
                                
                                    case 3: {
                                    
                                        myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                                        prompt=getPlatformDescription(PlatformsOfPhase2.SOUTHWEST);

                                        break;

                                    }
                                
                                    default: {

                                        return;

                                    }
                                
                                }
                    
                            }

                            if(myPosition.Equals(ARENA_CENTER_OF_PHASE_2)) {

                                return;

                            }
                            
                        }
                        
                    }
                
                }
                
                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                if(!string.IsNullOrWhiteSpace(prompt)) {
                    
                    if(enablePrompts) {
                    
                        accessory.Method.TextInfo(prompt,6125);
                    
                    }
                    
                    accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);

                }
                
            }

        }
        
        [ScriptMethod(name:"Phase 2 Ultraviolent Ray (Destruction)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:42077"],
            suppress:2500,
            userControl:false)]
    
        public void Phase_2_Ultraviolent_Ray_Destruction(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }
            
            ultraviolentRaySemaphore.Reset();
            numberOfUltraviolentRay=0;
            playerWasMarkedByAUltraviolentRay=[false,false,false,false,false,false,false,false];
        
        }
        
        [ScriptMethod(name:"Phase 2 Twinbite (Round Control)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42189"],
            userControl:false)]
    
        public void Phase_2_Twinbite_Round_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            ++roundOfTwinbite;

        }
        
        [ScriptMethod(name:"Phase 2 Twinbite (Guidance)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42189"])]
    
        public void Phase_2_Twinbite_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {

                var currentProperties=accessory.Data.GetDefaultDrawProperties();
                string prompt=string.Empty;

                if(isTank(myIndex)) {

                    if(myIndex==0) {
                        
                        // The correct platform:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=7125;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=7125;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                        prompt=getPlatformDescription(PlatformsOfPhase2.SOUTHWEST);
                        
                        // The another tank:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.DestoryAt=7125;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                    }

                    if(myIndex==1) {
                        
                        // The correct platform:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=7125;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=7125;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                        prompt=getPlatformDescription(PlatformsOfPhase2.SOUTHEAST);
                        
                        // The another tank:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.DestoryAt=7125;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                    }
                    
                    // Others:
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTH);
                    currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=7125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

                else {
                    
                    // The safe platform:
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=getPlatformCenter(PlatformsOfPhase2.SOUTH);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=7125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTH);
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=7125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                    prompt=getPlatformDescription(PlatformsOfPhase2.SOUTH);
                    
                    // Dangerous platforms:
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                    currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=7125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                    currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=7125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                }
                
                if(!string.IsNullOrWhiteSpace(prompt)) {
                    
                    if(enablePrompts) {
                    
                        accessory.Method.TextInfo(prompt,7125);
                    
                    }
                    
                    accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);

                }
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 2 Hero's Blow (Left Or Right)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(42080|42082)$"])]
    
        public void Phase_2_Heros_Blow_Left_Or_Right(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
        
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            // 42082: Left
            // 42080: Right

            currentProperties.Scale=new(32);
            currentProperties.Owner=sourceId;
            currentProperties.Radian=float.Pi;
            currentProperties.DestoryAt=6875;
            currentProperties.Color=accessory.Data.DefaultDangerColor;

            if(string.Equals(@event["ActionId"],"42082")) {
                        
                currentProperties.Rotation=float.Pi/2;
                        
            }

            if(string.Equals(@event["ActionId"],"42080")) {
                        
                currentProperties.Rotation=-float.Pi/2;
                        
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
        
        }
        
        [ScriptMethod(name:"Phase 2 Hero's Blow (Circle Or Donut)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(42083|42084)$"])]
    
        public void Phase_2_Heros_Blow_Circle_Or_Donut(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
        
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            string prompt=string.Empty;
            
            // 42083: Circle
            // 42084: Donut
            
            if(string.Equals(@event["ActionId"],"42083")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(22);
                currentProperties.Owner=sourceId;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=6875;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                prompt="Get out once in position.";

            }

            if(string.Equals(@event["ActionId"],"42084")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(25);
                currentProperties.InnerScale=new(15);
                currentProperties.Radian=float.Pi*2;
                currentProperties.Owner=sourceId;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=6875;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
                
                prompt="Get in once in position.";
                        
            }
            
            if(!string.IsNullOrWhiteSpace(prompt)) {

                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,6875);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 2 Hero's Blow (Round Control)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(42080|42082)$"],
            userControl:false)]
    
        public void Phase_2_Heros_Blow_Round_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            ++roundOfHerosBlow;
        
        }
        
        [ScriptMethod(name:"Phase 2 Hero's Blow (Guidance)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(42080|42082)$"])]
    
        public void Phase_2_Heros_Blow_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }
            
            double sourceRotation=0;

            try {

                sourceRotation=JsonConvert.DeserializeObject<double>(@event["SourceRotation"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourceRotation deserialization failed.");

                return;

            }

            double actualRotation=convertRotation(sourceRotation);
            int targetPlatform=-1;

            for(int i=0;i<=4;++i) {

                if(Math.Abs((Math.PI/5+Math.PI*2/5*i)-actualRotation)<Math.PI*0.05) {

                    targetPlatform=i;

                    break;

                }
                
            }

            if(targetPlatform<0||targetPlatform>4) {

                return;

            }
            
            accessory.Log.Debug($"targetPlatform={getPlatformDescription(((PlatformsOfPhase2)targetPlatform))}");

            List<bool> platformIsSafe=[true,true,true,true,true];
            
            // 42082: Left
            // 42080: Right
            
            if(string.Equals(@event["ActionId"],"42082")) {

                platformIsSafe[(targetPlatform-1+5)%5]=false;
                platformIsSafe[(targetPlatform-2+5)%5]=false;

            }

            if(string.Equals(@event["ActionId"],"42080")) {
                        
                platformIsSafe[(targetPlatform+1)%5]=false;
                platformIsSafe[(targetPlatform+2)%5]=false;
                        
            }
            
            var myObject=accessory.Data.Objects.SearchById(accessory.Data.Me);

            if(myObject==null) {

                return;

            }

            Vector3 closestPlatformCenter=ARENA_CENTER_OF_PHASE_2;
            double closestDistance=double.PositiveInfinity;
            
            for(int i=0;i<=4;++i) {

                if(platformIsSafe[i]) {
                    
                    if(Vector3.Distance(myObject.Position,getPlatformCenter(((PlatformsOfPhase2)i)))<closestDistance) {
                        
                        closestDistance=Vector3.Distance(myObject.Position,getPlatformCenter(((PlatformsOfPhase2)i)));
                        
                        closestPlatformCenter=getPlatformCenter(((PlatformsOfPhase2)i));

                    }

                }
                
            }

            if(closestPlatformCenter.Equals(ARENA_CENTER_OF_PHASE_2)) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=closestPlatformCenter;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=6875;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"Phase 2 Mooncleaver (Pre-position Guidance)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:42074"],
            suppress:2500)]
    
        public void Phase_2_Mooncleaver_PrePosition_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }

            if(roundOfQuakeIii!=2) {

                return;

            }

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {
                
                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
                if(!isLegalIndex(myIndex)) {

                    return;

                }

                var currentProperties=accessory.Data.GetDefaultDrawProperties();
                string prompt=string.Empty;
                
                // From 0s to 8.25s:
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=getPlatformCenter(PlatformsOfPhase2.SOUTH);
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=8250;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(8);
                currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTH);
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=8250;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
                prompt="Bait Mooncleaver on the south platform.";
                        
                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,5750);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
                // From 8.25s:
                
                prompt=string.Empty;
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(8);
                currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTH);
                currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                currentProperties.Delay=7750;
                currentProperties.DestoryAt=5375;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                if(!isTank(myIndex)) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=8250;
                    currentProperties.DestoryAt=4875;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=8250;
                    currentProperties.DestoryAt=4875;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                    prompt="Go northwest or northeast and standby.";

                }

                else {

                    if(myIndex==0) {
                        
                        // The correct platform:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.Delay=8250;
                        currentProperties.DestoryAt=4875;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.Delay=8250;
                        currentProperties.DestoryAt=4875;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                        prompt=getPlatformDescription(PlatformsOfPhase2.SOUTHWEST);
                        
                        // The another tank:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.Delay=8250;
                        currentProperties.DestoryAt=4875;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                    }

                    if(myIndex==1) {
                        
                        // The correct platform:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.Delay=8250;
                        currentProperties.DestoryAt=4875;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.Delay=8250;
                        currentProperties.DestoryAt=4875;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                        prompt=getPlatformDescription(PlatformsOfPhase2.SOUTHEAST);
                        
                        // The another tank:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.Delay=8250;
                        currentProperties.DestoryAt=4875;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                    }
                    
                }
                
                if(!string.IsNullOrWhiteSpace(prompt)) {
                    
                    System.Threading.Thread.Sleep(8250);
                    
                    if(enablePrompts) {
                    
                        accessory.Method.TextInfo(prompt,2375);
                    
                    }
                    
                    accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);

                }
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 2 Patience Of Wind",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:4394"])]
    
        public void Phase_2_Patience_Of_Wind(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
                
                return;
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(16);
            currentProperties.Owner=targetId;
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=durationMilliseconds;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"Phase 2 Patience Of Stone",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:4395"])]
    
        public void Phase_2_Patience_Of_Stone(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
                
                return;
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6);
            currentProperties.Owner=targetId;
            currentProperties.DestoryAt=durationMilliseconds;
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(isTank(myIndex)) {

                currentProperties.Color=accessory.Data.DefaultDangerColor;

            }

            else {
                
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"Phase 2 Elemental Purge (Acquisition)",
            eventType:EventTypeEnum.TargetIcon,
            userControl:false)]
    
        public void Phase_2_Elemental_Purge_Acquisition(Event @event,ScriptAccessory accessory) {

            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }

            if(iconId!=-353) { // 0x17-0x178=-353

                return;
                
            }
            
            accessory.Log.Debug($"An expected icon ID was captured. iconId={iconId}");

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf((uint)targetId);
            
            if(!isLegalIndex(targetIndex)) {

                return;

            }

            if(!isTank(targetIndex)) {
                
                return;
                
            }
            
            System.Threading.Thread.MemoryBarrier();

            if(targetIndex==0) {
                
                mtWasMarkerByPatienceOfWind=true;
                
            }

            if(targetIndex==1) {

                mtWasMarkerByPatienceOfWind=false;

            }
            
            System.Threading.Thread.MemoryBarrier();

            elementalPurgeSemaphore.Set();

        }
        
        [ScriptMethod(name:"Phase 2 Elemental Purge (Guidance)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42087"])]
    
        public void Phase_2_Elemental_Purge_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            elementalPurgeSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {
                
                string prompt=string.Empty;
                bool isWarning=false;
                
                if(isTank(myIndex)) {

                    if(myIndex==0) {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=10000;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

                        if(mtWasMarkerByPatienceOfWind) {
                            
                            prompt="You're marked, shirk to the another tank.";
                            isWarning=false;

                        }

                        else {

                            prompt="Provoke!";
                            isWarning=true;

                        }
                    
                    }
                
                    if(myIndex==1) {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=10000;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

                        if(!mtWasMarkerByPatienceOfWind) {
                            
                            prompt="You're marked, shirk to the another tank.";
                            isWarning=false;

                        }

                        else {

                            prompt="Provoke!";
                            isWarning=true;

                        }
                    
                    }
                
                }

                else {

                    if(mtWasMarkerByPatienceOfWind) {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=getPlatformCenter(PlatformsOfPhase2.NORTHWEST);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=10000;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                        
                        prompt=getPlatformDescription(PlatformsOfPhase2.NORTHWEST);
                        
                    }

                    else {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=getPlatformCenter(PlatformsOfPhase2.NORTHEAST);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=10000;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                        
                        prompt=getPlatformDescription(PlatformsOfPhase2.NORTHEAST);
                        
                    }
                
                }

                if(mtWasMarkerByPatienceOfWind) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                    currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=10000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.NORTHEAST);
                    currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=10000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

                else {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                    currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=10000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.NORTHWEST);
                    currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=10000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
                if(!string.IsNullOrWhiteSpace(prompt)) {
                    
                    if(enablePrompts) {
                    
                        accessory.Method.TextInfo(prompt,10000,isWarning);
                    
                    }
                    
                    accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);

                }
                
            }

        }
        
        [ScriptMethod(name:"Phase 2 Elemental Purge (Sub-phase 1 Control)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:42087"],
            userControl:false)]
    
        public void Phase_2_Elemental_Purge_SubPhase_1_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }

            System.Threading.Thread.MemoryBarrier();

            currentSubPhase=2;

            elementalPurgeSemaphore.Reset();
            
            accessory.Log.Debug("Now moving to Phase 2 Sub-phase 2.");
        
        }
        
        [ScriptMethod(name:"Phase 2 Prowling Gale (Pre-position Guidance)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:42093"],
            suppress:2500)]
    
        public void Phase_2_Prowling_Gale_PrePosition_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }

            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {
                
                Vector3 myPosition=ARENA_CENTER_OF_PHASE_2;

                myPosition=myIndex switch {
                
                    0 => getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),
                    1 => getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),
                    2 => getPlatformCenter(PlatformsOfPhase2.NORTHWEST),
                    3 => getPlatformCenter(PlatformsOfPhase2.NORTHEAST),
                    4 => getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),
                    5 => getPlatformCenter(PlatformsOfPhase2.NORTHEAST),
                    6 => getPlatformCenter(PlatformsOfPhase2.NORTHWEST),
                    7 => getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),
                    _ => ARENA_CENTER_OF_PHASE_2
                
                };

                if(myPosition.Equals(ARENA_CENTER_OF_PHASE_2)) {

                    return;

                }
            
                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=14000;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Position=myPosition;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=14000;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 2 Twofold Tempest (Line)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42097"])]
    
        public void Phase_2_Twofold_Tempest_Line(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }
            
            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {
                
                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
                if(!isLegalIndex(myIndex)) {

                    return;

                }
                
                long promptTime=0;
                
                System.Threading.Thread.MemoryBarrier();

                tempestLineSemaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();

                if(beginningPlatform==PlatformsOfPhase2.SOUTH) {

                    return;

                }
                
                // First line:
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(16,40);
                currentProperties.Owner=sourceId;
                currentProperties.TargetResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                currentProperties.TargetOrderIndex=1;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=0;
                currentProperties.DestoryAt=7125;

                if((tetherBeginsFromTheWest)
                   &&
                   (myIndex==3||myIndex==5)) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;

                    promptTime=currentProperties.Delay;

                }
                
                if((!tetherBeginsFromTheWest)
                   &&
                   (myIndex==2||myIndex==6)) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;

                    promptTime=currentProperties.Delay;

                }
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                
                // Second line:
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(16,40);
                currentProperties.Owner=sourceId;
                currentProperties.TargetResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                currentProperties.TargetOrderIndex=1;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=7125;
                currentProperties.DestoryAt=7125;

                if((tetherBeginsFromTheWest)
                   &&
                   (myIndex==1||myIndex==7)) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;

                    promptTime=currentProperties.Delay;

                }
                
                if((!tetherBeginsFromTheWest)
                   &&
                   (myIndex==0||myIndex==4)) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;

                    promptTime=currentProperties.Delay;

                }
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                
                // Third line:
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(16,40);
                currentProperties.Owner=sourceId;
                currentProperties.TargetResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                currentProperties.TargetOrderIndex=1;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=14250;
                currentProperties.DestoryAt=7125;

                if((tetherBeginsFromTheWest)
                   &&
                   (myIndex==0||myIndex==4)) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;

                    promptTime=currentProperties.Delay;

                }
                
                if((!tetherBeginsFromTheWest)
                   &&
                   (myIndex==1||myIndex==7)) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;

                    promptTime=currentProperties.Delay;

                }
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                
                // Fourth line:
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(16,40);
                currentProperties.Owner=sourceId;
                currentProperties.TargetResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                currentProperties.TargetOrderIndex=1;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=21375;
                currentProperties.DestoryAt=7125;

                if((tetherBeginsFromTheWest)
                   &&
                   (myIndex==2||myIndex==6)) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;

                    promptTime=currentProperties.Delay;

                }
                
                if((!tetherBeginsFromTheWest)
                   &&
                   (myIndex==3||myIndex==5)) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;

                    promptTime=currentProperties.Delay;

                }
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

                string prompt="Bait the line.";
                
                System.Threading.Thread.Sleep((int)promptTime);
                    
                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,7125);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);

                return;

            }

            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(16,40);
            currentProperties.Owner=sourceId;
            currentProperties.TargetResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
            currentProperties.TargetOrderIndex=1;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=28500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"Phase 2 Twofold Tempest (Initialization And Monitor)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:0054"],
            userControl:false)]
    
        public void Phase_2_Twofold_Tempest_Initialization_And_Monitor(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
            
                return;
            
            }

            if(currentTempestStackTarget==null) {
                
                int targetIndex=accessory.Data.PartyList.IndexOf((uint)targetId);
            
                if(!isLegalIndex(targetIndex)) {

                    return;

                }
                
                lock(lockOfTempestStackTarget) {

                    currentTempestStackTarget=targetId;

                    beginningPlatform=targetIndex switch {
                    
                        0 => PlatformsOfPhase2.SOUTHWEST,
                        1 => PlatformsOfPhase2.SOUTHEAST,
                        2 => PlatformsOfPhase2.NORTHWEST,
                        3 => PlatformsOfPhase2.NORTHEAST,
                        4 => PlatformsOfPhase2.SOUTHWEST,
                        5 => PlatformsOfPhase2.NORTHEAST,
                        6 => PlatformsOfPhase2.NORTHWEST,
                        7 => PlatformsOfPhase2.SOUTHEAST,
                        _ => PlatformsOfPhase2.SOUTH
                    
                    };

                    if(beginningPlatform==PlatformsOfPhase2.SOUTH) {

                        return;

                    }

                    if(beginningPlatform==PlatformsOfPhase2.NORTHWEST||beginningPlatform==PlatformsOfPhase2.SOUTHWEST) {

                        tetherBeginsFromTheWest=true;

                    }

                    else {
                    
                        tetherBeginsFromTheWest=false;
                    
                    }

                    roundOfTwofoldTempest=1;
                
                    System.Threading.Thread.MemoryBarrier();

                    tempestLineSemaphore.Set();
                    tempestGuidanceSemaphore.Set();

                }

            }

            else {
                
                lock(lockOfTempestStackTarget) {

                    if(currentTempestStackTarget==accessory.Data.Me) {

                        tetherCapturingSemaphore.Reset();
                    
                        tetherLeavingSemaphore.Set();
                        
                        accessory.Log.Debug("The tether left.");

                    }
                
                    System.Threading.Thread.MemoryBarrier();

                    currentTempestStackTarget=targetId;
                
                    System.Threading.Thread.MemoryBarrier();

                    if(targetId==accessory.Data.Me) {
                    
                        tetherLeavingSemaphore.Reset();

                        tetherCapturingSemaphore.Set();
                        
                        accessory.Log.Debug("The tether was captured.");

                    }

                }
            
                /*

                System.Threading.Thread.Sleep(125);

                tetherLeavingSemaphore.Reset();

                tetherCapturingSemaphore.Reset();

                */
                
            }

        }
        
        [ScriptMethod(name:"Phase 2 Twofold Tempest (Round Control)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:42098"],
            suppress:2500,
            userControl:false)]
    
        public void Phase_2_Twofold_Tempest_Round_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }

            if(roundOfTwofoldTempest>=5||roundOfTwofoldTempest<1) {

                return;

            }
            
            // 42098: Stack
            // 42099: Line
            
            System.Threading.Thread.MemoryBarrier();
            
            ++roundOfTwofoldTempest;
            
            System.Threading.Thread.MemoryBarrier();

            if(roundOfTwofoldTempest==2) {

                round2Semaphore.Set();
                
                accessory.Log.Debug("Twofold Tempest Round 2.");

            }
            
            if(roundOfTwofoldTempest==3) {

                round3Semaphore.Set();
                
                accessory.Log.Debug("Twofold Tempest Round 3.");

            }
            
            if(roundOfTwofoldTempest==4) {

                round4Semaphore.Set();
                
                accessory.Log.Debug("Twofold Tempest Round 4.");

            }

            if(roundOfTwofoldTempest==5) {

                tempestEndSemaphore.Set();
                
                accessory.Log.Debug("Twofold Tempest ended.");

            }

        }

        private void drawGuidanceForSupporters(int partyIndex,Vector3 targetPosition,int round,ScriptAccessory accessory) {

            if(!isLegalIndex(partyIndex)) {

                return;

            }

            if(round<1||round>4) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.PartyList[partyIndex];
            currentProperties.TargetPosition=targetPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=7125*(round-1);
            currentProperties.DestoryAt=7125;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
        }
        
        [ScriptMethod(name:"Phase 2 Twofold Tempest (Supporter Guidance)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42097"])]
    
        public void Phase_2_Twofold_Tempest_Supporter_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }

            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(!isSupporter(myIndex)) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            tempestGuidanceSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {
                
                int myPlatform=(int)(myIndex switch {
                    
                    0 => PlatformsOfPhase2.SOUTHWEST,
                    1 => PlatformsOfPhase2.SOUTHEAST,
                    2 => PlatformsOfPhase2.NORTHWEST,
                    3 => PlatformsOfPhase2.NORTHEAST,
                    _ => PlatformsOfPhase2.SOUTH
                    
                });

                if(myPlatform==((int)PlatformsOfPhase2.SOUTH)) {

                    return;

                }
                
                Vector3 myStackPosition=rotatePosition(new Vector3(100,-150,75.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myLinePosition=rotatePosition(new Vector3(100,-150,89.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myStandbyPosition=getPlatformCenter((PlatformsOfPhase2)myPlatform);

                if(myIndex==0) {

                    if(tetherBeginsFromTheWest) {
                        
                        drawGuidanceForSupporters(myIndex,myStackPosition,1,accessory);
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,2,accessory);
                        drawGuidanceForSupporters(myIndex,myLinePosition,3,accessory);
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,4,accessory);
                        
                    }

                    else {
                        
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,1,accessory);
                        drawGuidanceForSupporters(myIndex,myLinePosition,2,accessory);
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,3,accessory);
                        drawGuidanceForSupporters(myIndex,myStackPosition,4,accessory);
                        
                    }
                    
                }
                
                if(myIndex==1) {

                    if(tetherBeginsFromTheWest) {
                        
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,1,accessory);
                        drawGuidanceForSupporters(myIndex,myLinePosition,2,accessory);
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,3,accessory);
                        drawGuidanceForSupporters(myIndex,myStackPosition,4,accessory);
                        
                    }

                    else {
                        
                        drawGuidanceForSupporters(myIndex,myStackPosition,1,accessory);
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,2,accessory);
                        drawGuidanceForSupporters(myIndex,myLinePosition,3,accessory);
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,4,accessory);
                        
                    }
                    
                }
                
                if(myIndex==2) {

                    if(tetherBeginsFromTheWest) {
                        
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,1,accessory);
                        drawGuidanceForSupporters(myIndex,myStackPosition,2,accessory);
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,3,accessory);
                        drawGuidanceForSupporters(myIndex,myLinePosition,4,accessory);
                        
                    }

                    else {
                        
                        drawGuidanceForSupporters(myIndex,myLinePosition,1,accessory);
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,2,accessory);
                        drawGuidanceForSupporters(myIndex,myStackPosition,3,accessory);
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,4,accessory);
                        
                    }
                    
                }
                
                if(myIndex==3) {

                    if(tetherBeginsFromTheWest) {
                        
                        drawGuidanceForSupporters(myIndex,myLinePosition,1,accessory);
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,2,accessory);
                        drawGuidanceForSupporters(myIndex,myStackPosition,3,accessory);
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,4,accessory);
                        
                    }

                    else {
                        
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,1,accessory);
                        drawGuidanceForSupporters(myIndex,myStackPosition,2,accessory);
                        drawGuidanceForSupporters(myIndex,myStandbyPosition,3,accessory);
                        drawGuidanceForSupporters(myIndex,myLinePosition,4,accessory);
                        
                    }
                    
                }

            }

        }
        
        private void drawPositionGuidanceForDPS(int partyIndex,Vector3 targetPosition,ScriptAccessory accessory) {

            if(!isLegalIndex(partyIndex)) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=numberOfSteps.ToString();
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.PartyList[partyIndex];
            currentProperties.TargetPosition=targetPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=30000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
        }
        
        private void drawObjectGuidanceForDPS(int sourceIndex,int targetIndex,ScriptAccessory accessory) {

            if(!isLegalIndex(sourceIndex)||!isLegalIndex(targetIndex)) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=numberOfSteps.ToString();
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.PartyList[sourceIndex];
            currentProperties.TargetObject=accessory.Data.PartyList[targetIndex];
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=30000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
        }
        
        [ScriptMethod(name:"Phase 2 Twofold Tempest (M1 Or D1 Guidance)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42097"])]
    
        public void Phase_2_Twofold_Tempest_M1_Or_D1_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }

            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(myIndex!=4) { // Subject to change.

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            tempestGuidanceSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {
                
                int myPlatform=(int)(myIndex switch {
                    
                    4 => PlatformsOfPhase2.SOUTHWEST,
                    5 => PlatformsOfPhase2.NORTHEAST,
                    6 => PlatformsOfPhase2.NORTHWEST,
                    7 => PlatformsOfPhase2.SOUTHEAST,
                    _ => PlatformsOfPhase2.SOUTH
                    
                });

                if(myPlatform==((int)PlatformsOfPhase2.SOUTH)) {

                    return;

                }
                
                int myPartnerIndex=myIndex switch {
                    
                    4 => 0,
                    5 => 3,
                    6 => 2,
                    7 => 1,
                    _ => -1
                    
                };

                if(myPartnerIndex==-1) {

                    return;

                }
                
                int dpsOnMyLeft=myIndex switch {
                    
                    4 => 6,
                    5 => 7,
                    6 => 5,
                    7 => -1,
                    _ => -1
                    
                };
                
                int dpsOnMyRight=myIndex switch {
                    
                    4 => -1,
                    5 => 6,
                    6 => 4,
                    7 => 5,
                    _ => -1
                    
                };
                
                Vector3 myStackPosition=rotatePosition(new Vector3(100,-150,75.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myLinePosition=rotatePosition(new Vector3(100,-150,89.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myStandbyPosition=getPlatformCenter((PlatformsOfPhase2)myPlatform);
                Vector3 myInterceptionPosition=new Vector3(86.82f,-150,99.72f); // Subject to change.
                // Sorry for measuring the position by the mouse pointer, I don't know how to calculate it accurately with geometric.
                // But anyway, I guess it doesn't need to be super accurate and geometrically reproducible.
                
                // ----- Round 1 -----

                if(beginningPlatform==PlatformsOfPhase2.SOUTHWEST) { // Subject to change.

                    if(currentTempestStackTarget!=accessory.Data.Me) {
                        
                        drawObjectGuidanceForDPS(myIndex,myPartnerIndex,accessory);
                        
                        System.Threading.Thread.MemoryBarrier();

                        tetherCapturingSemaphore.WaitOne();
            
                        System.Threading.Thread.MemoryBarrier();
                        
                        accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                        System.Threading.Thread.MemoryBarrier();

                        ++numberOfSteps;
                        
                        System.Threading.Thread.MemoryBarrier();

                    }
                    
                    drawPositionGuidanceForDPS(myIndex,myStackPosition,accessory);
                    
                }

                if(beginningPlatform==PlatformsOfPhase2.NORTHWEST) { // Subject to change.
                    
                    drawPositionGuidanceForDPS(myIndex,myInterceptionPosition,accessory);
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStackPosition,accessory);
                    
                }

                if(!tetherBeginsFromTheWest) { // Subject to change.
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                round2Semaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();
                
                // ----- Round 2 -----
                
                if(tetherBeginsFromTheWest) { // Subject to change.

                    drawObjectGuidanceForDPS(myIndex,dpsOnMyLeft,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherLeavingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForDPS(myIndex,myLinePosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                round3Semaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();
                
                // ---- Round 3 -----

                if(tetherBeginsFromTheWest) { // Subject to change.

                    drawPositionGuidanceForDPS(myIndex,myLinePosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                round4Semaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();
                
                // ---- Round 4 -----

                if(tetherBeginsFromTheWest) { // Subject to change.

                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForDPS(myIndex,myInterceptionPosition,accessory);
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStackPosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                tempestEndSemaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();

            }

        }
        
        [ScriptMethod(name:"Phase 2 Twofold Tempest (R2 Or D4 Guidance)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42097"])]
    
        public void Phase_2_Twofold_Tempest_R2_Or_D4_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }

            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(myIndex!=7) { // Subject to change.

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            tempestGuidanceSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {
                
                int myPlatform=(int)(myIndex switch {
                    
                    4 => PlatformsOfPhase2.SOUTHWEST,
                    5 => PlatformsOfPhase2.NORTHEAST,
                    6 => PlatformsOfPhase2.NORTHWEST,
                    7 => PlatformsOfPhase2.SOUTHEAST,
                    _ => PlatformsOfPhase2.SOUTH
                    
                });

                if(myPlatform==((int)PlatformsOfPhase2.SOUTH)) {

                    return;

                }
                
                int myPartnerIndex=myIndex switch {
                    
                    4 => 0,
                    5 => 3,
                    6 => 2,
                    7 => 1,
                    _ => -1
                    
                };

                if(myPartnerIndex==-1) {

                    return;

                }
                
                int dpsOnMyLeft=myIndex switch {
                    
                    4 => 6,
                    5 => 7,
                    6 => 5,
                    7 => -1,
                    _ => -1
                    
                };
                
                int dpsOnMyRight=myIndex switch {
                    
                    4 => -1,
                    5 => 6,
                    6 => 4,
                    7 => 5,
                    _ => -1
                    
                };
                
                Vector3 myStackPosition=rotatePosition(new Vector3(100,-150,75.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myLinePosition=rotatePosition(new Vector3(100,-150,89.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myStandbyPosition=getPlatformCenter((PlatformsOfPhase2)myPlatform);
                Vector3 myInterceptionPosition=new Vector3(113.20f,-150,99.93f); // Subject to change.
                // Sorry for measuring the position by the mouse pointer, I don't know how to calculate it accurately with geometric.
                // But anyway, I guess it doesn't need to be super accurate and geometrically reproducible.
                
                // ----- Round 1 -----

                if(beginningPlatform==PlatformsOfPhase2.SOUTHEAST) { // Subject to change.

                    if(currentTempestStackTarget!=accessory.Data.Me) {
                        
                        drawObjectGuidanceForDPS(myIndex,myPartnerIndex,accessory);
                        
                        System.Threading.Thread.MemoryBarrier();

                        tetherCapturingSemaphore.WaitOne();
            
                        System.Threading.Thread.MemoryBarrier();
                        
                        accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                        System.Threading.Thread.MemoryBarrier();

                        ++numberOfSteps;
                        
                        System.Threading.Thread.MemoryBarrier();
                        
                    }
                    
                    drawPositionGuidanceForDPS(myIndex,myStackPosition,accessory);
                    
                }

                if(beginningPlatform==PlatformsOfPhase2.NORTHEAST) { // Subject to change.
                    
                    drawPositionGuidanceForDPS(myIndex,myInterceptionPosition,accessory);
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStackPosition,accessory);
                    
                }

                if(tetherBeginsFromTheWest) { // Subject to change.
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                round2Semaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();
                
                // ----- Round 2 -----
                
                if(!tetherBeginsFromTheWest) { // Subject to change.

                    drawObjectGuidanceForDPS(myIndex,dpsOnMyRight,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherLeavingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForDPS(myIndex,myLinePosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                round3Semaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();
                
                // ---- Round 3 -----

                if(!tetherBeginsFromTheWest) { // Subject to change.

                    drawPositionGuidanceForDPS(myIndex,myLinePosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                round4Semaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();
                
                // ---- Round 4 -----

                if(!tetherBeginsFromTheWest) { // Subject to change.

                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForDPS(myIndex,myInterceptionPosition,accessory);
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStackPosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                tempestEndSemaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();

            }

        }
        
        [ScriptMethod(name:"Phase 2 Twofold Tempest (R1 Or D3 Guidance)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42097"])]
    
        public void Phase_2_Twofold_Tempest_R1_Or_D3_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }

            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(myIndex!=6) { // Subject to change.

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            tempestGuidanceSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {
                
                int myPlatform=(int)(myIndex switch {
                    
                    4 => PlatformsOfPhase2.SOUTHWEST,
                    5 => PlatformsOfPhase2.NORTHEAST,
                    6 => PlatformsOfPhase2.NORTHWEST,
                    7 => PlatformsOfPhase2.SOUTHEAST,
                    _ => PlatformsOfPhase2.SOUTH
                    
                });

                if(myPlatform==((int)PlatformsOfPhase2.SOUTH)) {

                    return;

                }
                
                int myPartnerIndex=myIndex switch {
                    
                    4 => 0,
                    5 => 3,
                    6 => 2,
                    7 => 1,
                    _ => -1
                    
                };

                if(myPartnerIndex==-1) {

                    return;

                }
                
                int dpsOnMyLeft=myIndex switch {
                    
                    4 => 6,
                    5 => 7,
                    6 => 5,
                    7 => -1,
                    _ => -1
                    
                };
                
                int dpsOnMyRight=myIndex switch {
                    
                    4 => -1,
                    5 => 6,
                    6 => 4,
                    7 => 5,
                    _ => -1
                    
                };
                
                Vector3 myStackPosition=rotatePosition(new Vector3(100,-150,75.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myLinePosition=rotatePosition(new Vector3(100,-150,89.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myStandbyPosition=getPlatformCenter((PlatformsOfPhase2)myPlatform);
                Vector3 interceptionPositionOnMyLeft=new Vector3(96.54f,-150,87.11f); // Subject to change.
                Vector3 interceptionPositionOnMyRight=new Vector3(89.05f,-150,92.51f); // Subject to change.
                Vector3 myStandbyPositionOnAnotherPlatform=new Vector3(82.13f,-150,99.12f); // Subject to change.
                // Sorry for measuring these three positions by the mouse pointer, I don't know how to calculate it accurately with geometric.
                // But anyway, I guess it doesn't need to be super accurate and geometrically reproducible.
                
                // ----- Round 1 -----

                if(beginningPlatform==PlatformsOfPhase2.SOUTHWEST) { // Subject to change.
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }

                if(beginningPlatform==PlatformsOfPhase2.NORTHWEST) { // Subject to change.
                    
                    if(currentTempestStackTarget!=accessory.Data.Me) {
                        
                        drawObjectGuidanceForDPS(myIndex,myPartnerIndex,accessory);
                        
                        System.Threading.Thread.MemoryBarrier();

                        tetherCapturingSemaphore.WaitOne();
            
                        System.Threading.Thread.MemoryBarrier();
                        
                        accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                        System.Threading.Thread.MemoryBarrier();

                        ++numberOfSteps;
                        
                        System.Threading.Thread.MemoryBarrier();
                        
                    }
                    
                    drawObjectGuidanceForDPS(myIndex,dpsOnMyRight,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherLeavingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPositionOnAnotherPlatform,accessory);
                    
                }

                if(!tetherBeginsFromTheWest) { // Subject to change.
                    
                    drawPositionGuidanceForDPS(myIndex,myLinePosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                round2Semaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();
                
                // ----- Round 2 -----
                
                if(beginningPlatform==PlatformsOfPhase2.SOUTHWEST) { // Subject to change.
                    
                    drawPositionGuidanceForDPS(myIndex,interceptionPositionOnMyRight,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStackPosition,accessory);
                    
                }

                if(beginningPlatform==PlatformsOfPhase2.NORTHWEST) { // Subject to change.
                    
                    drawObjectGuidanceForDPS(myIndex,dpsOnMyRight,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStackPosition,accessory);
                    
                }

                if(!tetherBeginsFromTheWest) { // Subject to change.
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                round3Semaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();
                
                // ---- Round 3 -----

                if(tetherBeginsFromTheWest) { // Subject to change.

                    drawObjectGuidanceForDPS(myIndex,dpsOnMyLeft,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherLeavingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForDPS(myIndex,interceptionPositionOnMyLeft,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStackPosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                round4Semaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();
                
                // ---- Round 4 -----

                if(tetherBeginsFromTheWest) { // Subject to change.

                    drawPositionGuidanceForDPS(myIndex,myLinePosition,accessory);
                    
                }

                else {
                    
                    drawObjectGuidanceForDPS(myIndex,dpsOnMyRight,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherLeavingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                tempestEndSemaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();

            }

        }
        
        [ScriptMethod(name:"Phase 2 Twofold Tempest (M2 Or D2 Guidance)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42097"])]
    
        public void Phase_2_Twofold_Tempest_M2_Or_D2_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }

            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(myIndex!=5) { // Subject to change.

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            tempestGuidanceSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {
                
                int myPlatform=(int)(myIndex switch {
                    
                    4 => PlatformsOfPhase2.SOUTHWEST,
                    5 => PlatformsOfPhase2.NORTHEAST,
                    6 => PlatformsOfPhase2.NORTHWEST,
                    7 => PlatformsOfPhase2.SOUTHEAST,
                    _ => PlatformsOfPhase2.SOUTH
                    
                });

                if(myPlatform==((int)PlatformsOfPhase2.SOUTH)) {

                    return;

                }
                
                int myPartnerIndex=myIndex switch {
                    
                    4 => 0,
                    5 => 3,
                    6 => 2,
                    7 => 1,
                    _ => -1
                    
                };

                if(myPartnerIndex==-1) {

                    return;

                }
                
                int dpsOnMyLeft=myIndex switch {
                    
                    4 => 6,
                    5 => 7,
                    6 => 5,
                    7 => -1,
                    _ => -1
                    
                };
                
                int dpsOnMyRight=myIndex switch {
                    
                    4 => -1,
                    5 => 6,
                    6 => 4,
                    7 => 5,
                    _ => -1
                    
                };
                
                Vector3 myStackPosition=rotatePosition(new Vector3(100,-150,75.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myLinePosition=rotatePosition(new Vector3(100,-150,89.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myStandbyPosition=getPlatformCenter((PlatformsOfPhase2)myPlatform);
                Vector3 interceptionPositionOnMyLeft=new Vector3(111.05f,-150,92.84f); // Subject to change.
                Vector3 interceptionPositionOnMyRight=new Vector3(103.89f,-150,87.60f); // Subject to change.
                Vector3 myStandbyPositionOnAnotherPlatform=new Vector3(117.52f,-150,99.04f); // Subject to change.
                // Sorry for measuring these three positions by the mouse pointer, I don't know how to calculate it accurately with geometric.
                // But anyway, I guess it doesn't need to be super accurate and geometrically reproducible.
                
                // ----- Round 1 -----

                if(beginningPlatform==PlatformsOfPhase2.SOUTHEAST) { // Subject to change.
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }

                if(beginningPlatform==PlatformsOfPhase2.NORTHEAST) { // Subject to change.
                    
                    if(currentTempestStackTarget!=accessory.Data.Me) {
                        
                        drawObjectGuidanceForDPS(myIndex,myPartnerIndex,accessory);
                        
                        System.Threading.Thread.MemoryBarrier();

                        tetherCapturingSemaphore.WaitOne();
            
                        System.Threading.Thread.MemoryBarrier();
                        
                        accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                        System.Threading.Thread.MemoryBarrier();

                        ++numberOfSteps;
                        
                        System.Threading.Thread.MemoryBarrier();
                        
                    }
                    
                    drawObjectGuidanceForDPS(myIndex,dpsOnMyLeft,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherLeavingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPositionOnAnotherPlatform,accessory);
                    
                }

                if(tetherBeginsFromTheWest) { // Subject to change.
                    
                    drawPositionGuidanceForDPS(myIndex,myLinePosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                round2Semaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();
                
                // ----- Round 2 -----
                
                if(beginningPlatform==PlatformsOfPhase2.SOUTHEAST) { // Subject to change.
                    
                    drawPositionGuidanceForDPS(myIndex,interceptionPositionOnMyLeft,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStackPosition,accessory);
                    
                }

                if(beginningPlatform==PlatformsOfPhase2.NORTHEAST) { // Subject to change.
                    
                    drawObjectGuidanceForDPS(myIndex,dpsOnMyLeft,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStackPosition,accessory);
                    
                }

                if(tetherBeginsFromTheWest) { // Subject to change.
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                round3Semaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();
                
                // ---- Round 3 -----

                if(!tetherBeginsFromTheWest) { // Subject to change.

                    drawObjectGuidanceForDPS(myIndex,dpsOnMyRight,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherLeavingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForDPS(myIndex,interceptionPositionOnMyRight,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStackPosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                round4Semaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();
                
                // ---- Round 4 -----

                if(!tetherBeginsFromTheWest) { // Subject to change.

                    drawPositionGuidanceForDPS(myIndex,myLinePosition,accessory);
                    
                }

                else {
                    
                    drawObjectGuidanceForDPS(myIndex,dpsOnMyLeft,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherLeavingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForDPS(myIndex,myStandbyPosition,accessory);
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                tempestEndSemaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                System.Threading.Thread.MemoryBarrier();

                ++numberOfSteps;
                        
                System.Threading.Thread.MemoryBarrier();

            }

        }
        
        [ScriptMethod(name:"Phase 2 Elemental Purge (Sub-phase 2 Control)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42101"],
            userControl:false)]
    
        public void Phase_2_Elemental_Purge_SubPhase_2_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }

            System.Threading.Thread.MemoryBarrier();

            currentSubPhase=3;
            
            tempestLineSemaphore.Reset();
            tempestGuidanceSemaphore.Reset();
            tetherLeavingSemaphore.Reset(); 
            tetherCapturingSemaphore.Reset();
            round2Semaphore.Reset();
            round3Semaphore.Reset();
            round4Semaphore.Reset();
            tempestEndSemaphore.Reset();
            
            accessory.Log.Debug("Now moving to Phase 2 Sub-phase 3.");
        
        }
        
        [ScriptMethod(name:"Phase 2 Gleaming Barrage",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42102"])]
    
        public void Phase_2_Gleaming_Barrage(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }
            
            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(8,31);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=2800;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
        
        }
        
        [ScriptMethod(name:"Phase 2 Champion's Circuit",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(42103|42104)$"])]
    
        public void Phase_2_Champions_Circuit(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=3) {

                return;

            }
            
            double sourceRotation=0;

            try {

                sourceRotation=JsonConvert.DeserializeObject<double>(@event["SourceRotation"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourceRotation deserialization failed.");

                return;

            }

            double actualRotation=convertRotation(sourceRotation);
            int targetPlatform=-1;

            for(int i=0;i<=4;++i) {

                if(Math.Abs((Math.PI/5+Math.PI*2/5*i)-actualRotation)<Math.PI*0.05) {

                    targetPlatform=i;

                    break;

                }
                
            }

            if(targetPlatform<0||targetPlatform>4) {

                return;

            }

            int donutPlatform=(targetPlatform-1+5)%5;
            
            // 42103: Clockwise
            // 42104: Counterclockwise
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            string prompt=string.Empty;
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(12,27);
            currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
            currentProperties.Rotation=((float)sourceRotation)+float.Pi*2/5*0;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=8000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                    
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(13);
            currentProperties.InnerScale=new(4);
            currentProperties.Radian=float.Pi*2;
            currentProperties.Position=getPlatformCenter(((PlatformsOfPhase2)donutPlatform));
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=8000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
                    
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(28.3f);
            currentProperties.InnerScale=new(15.8f);
            currentProperties.Radian=float.Pi*2/5;
            currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
            currentProperties.Rotation=((float)sourceRotation)+float.Pi*2/5*2;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=8000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
                    
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(22);
            currentProperties.Radian=float.Pi*2/5;
            currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
            currentProperties.Rotation=((float)sourceRotation)+float.Pi*2/5*3;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=8000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                    
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(28.3f);
            currentProperties.InnerScale=new(15.8f);
            currentProperties.Radian=float.Pi*2/5;
            currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
            currentProperties.Rotation=((float)sourceRotation)+float.Pi*2/5*4;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=8000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);

            if(string.Equals(@event["ActionId"],"42103")) {

                donutPlatform=(donutPlatform+1)%5;

                for(int i=1;i<5;++i,donutPlatform=(donutPlatform+1)%5) {

                    float currentRotation=((float)sourceRotation)+float.Pi*2/5*(-i);
                    long currentDelay=8000+4375*(i-1);
                    Vector3 donutCenter=getPlatformCenter(((PlatformsOfPhase2)donutPlatform));
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(12,27);
                    currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
                    currentProperties.Rotation=float.Pi*2/5*0+currentRotation;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=currentDelay;
                    currentProperties.DestoryAt=4375;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(13);
                    currentProperties.InnerScale=new(4);
                    currentProperties.Radian=float.Pi*2;
                    currentProperties.Position=donutCenter;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=currentDelay;
                    currentProperties.DestoryAt=4375;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(28.3f);
                    currentProperties.InnerScale=new(15.8f);
                    currentProperties.Radian=float.Pi*2/5;
                    currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
                    currentProperties.Rotation=float.Pi*2/5*2+currentRotation;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=currentDelay;
                    currentProperties.DestoryAt=4375;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(22);
                    currentProperties.Radian=float.Pi*2/5;
                    currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
                    currentProperties.Rotation=float.Pi*2/5*3+currentRotation;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=currentDelay;
                    currentProperties.DestoryAt=4375;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(28.3f);
                    currentProperties.InnerScale=new(15.8f);
                    currentProperties.Radian=float.Pi*2/5;
                    currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
                    currentProperties.Rotation=float.Pi*2/5*4+currentRotation;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=currentDelay;
                    currentProperties.DestoryAt=4375;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);

                }
                
            }
            
            if(string.Equals(@event["ActionId"],"42104")) {

                donutPlatform=(donutPlatform-1+10)%5;
                
                for(int i=1;i<5;++i,donutPlatform=(donutPlatform-1+10)%5) {

                    float currentRotation=((float)sourceRotation)+float.Pi*2/5*i;
                    long currentDelay=8000+4375*(i-1);
                    Vector3 donutCenter=getPlatformCenter(((PlatformsOfPhase2)donutPlatform));
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(12,27);
                    currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
                    currentProperties.Rotation=float.Pi*2/5*0+currentRotation;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=currentDelay;
                    currentProperties.DestoryAt=4375;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(13);
                    currentProperties.InnerScale=new(4);
                    currentProperties.Radian=float.Pi*2;
                    currentProperties.Position=donutCenter;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=currentDelay;
                    currentProperties.DestoryAt=4375;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(28.3f);
                    currentProperties.InnerScale=new(15.8f);
                    currentProperties.Radian=float.Pi*2/5;
                    currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
                    currentProperties.Rotation=float.Pi*2/5*2+currentRotation;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=currentDelay;
                    currentProperties.DestoryAt=4375;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(22);
                    currentProperties.Radian=float.Pi*2/5;
                    currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
                    currentProperties.Rotation=float.Pi*2/5*3+currentRotation;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=currentDelay;
                    currentProperties.DestoryAt=4375;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(28.3f);
                    currentProperties.InnerScale=new(15.8f);
                    currentProperties.Radian=float.Pi*2/5;
                    currentProperties.Position=ARENA_CENTER_OF_PHASE_2;
                    currentProperties.Rotation=float.Pi*2/5*4+currentRotation;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=currentDelay;
                    currentProperties.DestoryAt=4375;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);

                }
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 2 Champion's Circuit (Sub-phase 3 Control)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:42074"],
            suppress:2500,
            userControl:false)]
    
        public void Phase_2_Champions_Circuit_SubPhase_3_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=3) {

                return;

            }

            if(roundOfQuakeIii!=3) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            currentSubPhase=4;
            
            accessory.Log.Debug("Now moving to Phase 2 Sub-phase 4.");
        
        }
        
        [ScriptMethod(name:"Phase 2 Lone Wolf's Lament (Pre-position Guidance)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:42190"],
            suppress:2500)]
    
        public void Phase_2_Lone_Wolfs_Lament_PrePosition_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=4) {

                return;

            }

            if(roundOfTwinbite!=2) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {

                Vector3 myPosition=ARENA_CENTER_OF_PHASE_2;

                if(isDps(myIndex)) {

                    myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);

                }
                
                if(isTank(myIndex)) {

                    myPosition=getPlatformCenter(PlatformsOfPhase2.NORTHWEST);

                }
                
                if(isHealer(myIndex)) {

                    myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTH);

                }

                if(myPosition.Equals(ARENA_CENTER_OF_PHASE_2)) {

                    return;

                }
            
                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=20500;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(8);
                currentProperties.Position=myPosition;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=20500;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 2 Lone Wolf's Lament (Acquisition)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:regex:^(013D|013E)$"],
            userControl:false)]
    
        public void Phase_2_Lone_Wolfs_Lament_Acquisition(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=4) {

                return;

            }

            if(numberOfLamentTethers>=4) {

                return;

            }
            
            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
            
            int sourceIndex=accessory.Data.PartyList.IndexOf((uint)sourceId);
            
            if(!isLegalIndex(sourceIndex)) {

                return;

            }
            
            if(!convertObjectId(@event["TargetId"], out var targetId)) {
            
                return;
            
            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf((uint)targetId);
            
            if(!isLegalIndex(targetIndex)) {

                return;

            }

            bool playersHaveToGetClose=false;

            // O13D: Get close
            // 013E: Stay far
            
            if(string.Equals(@event["Id"],"013D")) {

                playersHaveToGetClose=true;

            }
            
            if(string.Equals(@event["Id"],"013E")) {

                playersHaveToGetClose=false;

            }

            lock(lockOfLamentData) {
                
                ++numberOfLamentTethers;
            
                System.Threading.Thread.MemoryBarrier();
                
                if(isDps(sourceIndex)) {
                
                    if(isTank(targetIndex)) {

                        if(playersHaveToGetClose) {

                            dpsWithTheCloseTank=sourceIndex;
                            tankWithTheCloseDps=targetIndex;

                        }

                        else {

                            dpsWithTheFarTank=sourceIndex;
                            tankWithTheFarDps=targetIndex;

                        }
                
                    }
                
                    if(isHealer(targetIndex)) {
                    
                        if(playersHaveToGetClose) {

                            dpsWithTheCloseHealer=sourceIndex;
                            healerWithTheCloseDps=targetIndex;

                        }

                        else {

                            dpsWithTheFarHealer=sourceIndex;
                            healerWithTheFarDps=targetIndex;

                        }
                
                    }
                
                }

                if(isTank(sourceIndex)) {
                
                    if(isDps(targetIndex)) {
                    
                        if(playersHaveToGetClose) {

                            dpsWithTheCloseTank=targetIndex;
                            tankWithTheCloseDps=sourceIndex;

                        }

                        else {

                            dpsWithTheFarTank=targetIndex;
                            tankWithTheFarDps=sourceIndex;

                        }
                    
                    }
                
                }
                
                if(isHealer(sourceIndex)) {
                
                    if(isDps(targetIndex)) {
                    
                        if(playersHaveToGetClose) {

                            dpsWithTheCloseHealer=targetIndex;
                            healerWithTheCloseDps=sourceIndex;

                        }

                        else {

                            dpsWithTheFarHealer=targetIndex;
                            healerWithTheFarDps=sourceIndex;

                        }
                    
                    }
                
                }
                
                System.Threading.Thread.MemoryBarrier();
            
                if(numberOfLamentTethers>=4) {

                    lamentSemaphore.Set();
                    
                    accessory.Log.Debug($"dpsWithTheCloseHealer={dpsWithTheCloseHealer},dpsWithTheFarTank={dpsWithTheFarTank},dpsWithTheCloseTank={dpsWithTheCloseTank},dpsWithTheFarHealer={dpsWithTheFarHealer}");
                    accessory.Log.Debug($"healerWithTheCloseDps={healerWithTheCloseDps},healerWithTheFarDps={healerWithTheFarDps},tankWithTheFarDps={tankWithTheFarDps},tankWithTheCloseDps={tankWithTheCloseDps}");

                }
                
            }

        }
        
        [ScriptMethod(name:"Phase 2 Lone Wolf's Lament (Two-player Tower Acquisition)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42119"],
            userControl:false)]
    
        public void Phase_2_Lone_Wolfs_Lament_TwoPlayer_Tower_Acquisition(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=4) {

                return;

            }
            
            Vector3 targetPosition=ARENA_CENTER_OF_PHASE_2;

            try {

                targetPosition=JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("TargetPosition deserialization failed.");

                return;

            }

            if(Vector3.Distance(targetPosition,getPlatformCenter(PlatformsOfPhase2.NORTHWEST))<8) {

                twoPlayerTowerIsOnTheWest=true;

            }

            else {

                twoPlayerTowerIsOnTheWest=false;

            }
            
            System.Threading.Thread.MemoryBarrier();

            northTowerSemaphore.Set();

        }
        
        [ScriptMethod(name:"Phase 2 Lone Wolf's Lament (Guidance)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42115"])]
    
        public void Phase_2_Lone_Wolfs_Lament_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=4) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            lamentSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {

                Vector3 myPosition=ARENA_CENTER_OF_PHASE_2;
                bool finalPositionTbd=false;

                if(isHealer(myIndex)) {

                    myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTH);

                }
                
                if(myIndex==dpsWithTheCloseHealer) {

                    myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTH);

                }
                
                if(myIndex==dpsWithTheFarTank) {

                    myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);

                }
                
                if(myIndex==tankWithTheFarDps) {

                    myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);

                }
                
                if(myIndex==dpsWithTheCloseTank||myIndex==tankWithTheCloseDps||myIndex==dpsWithTheFarHealer) {

                    myPosition=getPlatformCenter(PlatformsOfPhase2.NORTHEAST);

                    finalPositionTbd=true;

                }

                if(myPosition.Equals(ARENA_CENTER_OF_PHASE_2)) {

                    return;

                }
            
                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Name="Phase_2_Lone_Wolfs_Lament_Guidance_1";
                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=18250;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

                if(!finalPositionTbd) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Position=myPosition;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=18250;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }

                else {

                    northTowerSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw("Phase_2_Lone_Wolfs_Lament_Guidance_1");
                        
                    System.Threading.Thread.MemoryBarrier();

                    myPosition=ARENA_CENTER_OF_PHASE_2;
                    
                    if(myIndex==dpsWithTheCloseTank||myIndex==tankWithTheCloseDps) {

                        if(twoPlayerTowerIsOnTheWest) {

                            myPosition=getPlatformCenter(PlatformsOfPhase2.NORTHWEST);

                        }

                        else {
                            
                            myPosition=getPlatformCenter(PlatformsOfPhase2.NORTHEAST);
                            
                        }

                    }
                    
                    if(myIndex==dpsWithTheFarHealer) {

                        if(twoPlayerTowerIsOnTheWest) {

                            myPosition=getPlatformCenter(PlatformsOfPhase2.NORTHEAST);

                        }

                        else {
                            
                            myPosition=getPlatformCenter(PlatformsOfPhase2.NORTHWEST);
                            
                        }

                    }
                    
                    if(myPosition.Equals(ARENA_CENTER_OF_PHASE_2)) {

                        return;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=myPosition;
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=8000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Position=myPosition;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=8000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);

                }
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 2 Lone Wolf's Lament (Sub-phase 4 Control)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:42119"],
            suppress:2500,
            userControl:false)]
    
        public void Phase_2_Lone_Wolfs_Lament_SubPhase_4_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=4) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            currentSubPhase=5;

            lamentSemaphore.Reset();
            northTowerSemaphore.Reset();
            
            accessory.Log.Debug("Now moving to Phase 2 Sub-phase 5.");

        }
        
        [ScriptMethod(name:"Phase 2 Ultraviolent Ray 4 (Pre-position Guidance)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:regex:^(42080|42082)$"],
            suppress:2500)]
    
        public void Phase_2_Ultraviolent_Ray_4_PrePosition_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            if(roundOfHerosBlow!=2) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalIndex(myIndex)) {

                return;

            }

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {

                Vector3 myPosition=ARENA_CENTER_OF_PHASE_2;

                if(stratOfUltraviolentRay4==StratsOfUltraviolentRay4.Same_As_Usual) {

                    if(isInGroup1(myIndex)) {

                        myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);

                    }
                    
                    if(isInGroup2(myIndex)) {

                        myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);

                    }
                    
                }
                
                if(stratOfUltraviolentRay4==StratsOfUltraviolentRay4.Northwest_And_South) {

                    if(isInGroupNorthwest(myIndex)) {

                        myPosition=getPlatformCenter(PlatformsOfPhase2.NORTHWEST);

                    }
                    
                    else {

                        myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTH);

                    }
                    
                }

                if(myPosition.Equals(ARENA_CENTER_OF_PHASE_2)) {

                    return;

                }
            
                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(8);
                currentProperties.Position=myPosition;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 2 Mooncleaver (Enrage Pre-position Guidance)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:42077"],
            suppress:2500)]
    
        public void Phase_2_Mooncleaver_Enrage_PrePosition_Guidance(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=5) {

                return;

            }
            
            if(roundOfUltraviolentRay!=4) {

                return;

            }

            if(stratOfPhase2==StratsOfPhase2.Toxic_Friends_RaidPlan_DOG) {
            
                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=getPlatformCenter(PlatformsOfPhase2.SOUTH);
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=6500;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(8);
                currentProperties.Position=getPlatformCenter(PlatformsOfPhase2.SOUTH);
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=6500;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
            }
        
        }
        
        [ScriptMethod(name:"Phase 2 Mooncleaver (Enrage)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42829"])]
    
        public void Phase_2_Mooncleaver_Enrage(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=2) {

                return;

            }
            
            if(currentSubPhase!=5) {

                return;

            }
            
            Vector3 targetPosition=ARENA_CENTER_OF_PHASE_2;

            try {

                targetPosition=JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("TargetPosition deserialization failed.");

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(8);
            currentProperties.Position=targetPosition;
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=4000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

            string prompt="Stay on the next platform at least until the tower appears!";
            
            if(enablePrompts) {
                    
                accessory.Method.TextInfo(prompt,4000,true);
                    
            }
                    
            accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
        
        }
        
        #endregion
        
        #region Commons
        
        private int? baseIdOfTargetIcon=null;
        private readonly Object baseIdLockOfTargetIcon=new Object();
        
        private bool convertTargetIconId(string? rawHexId,out int result) {

            lock(baseIdLockOfTargetIcon) {
                
                result=0;
            
                if(string.IsNullOrWhiteSpace(rawHexId)) {
                
                    return false;
                
                }
            
                string hexId=rawHexId.Trim();
            
                hexId=hexId.StartsWith("0x",StringComparison.OrdinalIgnoreCase)?hexId.Substring(2):hexId;

                if(!int.TryParse(hexId,System.Globalization.NumberStyles.HexNumber,null,out result)) {

                    return false;

                }
                
                baseIdOfTargetIcon??=result;
                result-=baseIdOfTargetIcon.GetValueOrDefault();

                return true;

            }
            
        }

        public static bool convertObjectId(string? rawHexId,out ulong result) {
            
            result=0;

            if(string.IsNullOrWhiteSpace(rawHexId)) {
                
                return false;
                
            }

            string hexId=rawHexId.Trim();
            
            hexId=hexId.StartsWith("0x",StringComparison.OrdinalIgnoreCase)?hexId.Substring(2):hexId;
            
            return ulong.TryParse(hexId,System.Globalization.NumberStyles.HexNumber,null,out result);
            
        }
        
        public static int discretizePosition(Vector3 position,Vector3 center,int numberOfDirections,bool diagonalSplit=true) {

            if(diagonalSplit) {
                
                return (int)(
                
                    (Math.Round(
                    
                        (numberOfDirections/2.0d)-(numberOfDirections/2.0d)*Math.Atan2(position.X-center.X,position.Z-center.Z)/Math.PI
                    
                    )%numberOfDirections+numberOfDirections)%numberOfDirections
                
                );
                
            }

            else {
                
                return (int)(
                
                    (Math.Floor(
                    
                        (numberOfDirections/2.0d)-(numberOfDirections/2.0d)*Math.Atan2(position.X-center.X,position.Z-center.Z)/Math.PI
                    
                    )%numberOfDirections+numberOfDirections)%numberOfDirections
                
                );
                
            }
            
        }
        
        public static double getRotation(Vector3 position,Vector3 center) {
            
            return (position.Equals(center))?
                (0):
                ((Math.PI-Math.Atan2(position.X-center.X,position.Z-center.Z)+2*Math.PI)%(2*Math.PI));
            
        }
        
        public static double getRotationDifference(Vector3 position1,Vector3 position2,Vector3 center) {

            double rawDifference=(getRotation(position2,center)-getRotation(position1,center)+2*Math.PI)%(2*Math.PI);

            return (rawDifference<=Math.PI)?(rawDifference):(rawDifference-2*Math.PI);
            
        }
        
        public static Vector3 rotatePosition(Vector3 position,Vector3 center,double radian,bool preserveHeight=true) {

            Vector2 positionInVector2=new Vector2(position.X-center.X,position.Z-center.Z);
            double polarAngleAfterRotation=Math.PI-Math.Atan2(positionInVector2.X,positionInVector2.Y)+radian;
            
            return new Vector3((float)(center.X+Math.Sin(polarAngleAfterRotation)*positionInVector2.Length()),
                ((preserveHeight)?(position.Y):(center.Y)),
                (float)(center.Z-Math.Cos(polarAngleAfterRotation)*positionInVector2.Length()));
            
        }

        public static double convertRotation(double rawRotation) {
            
            return Math.PI-rawRotation;
            
        }

        public static bool isLegalIndex(int partyIndex) {

            return (0<=partyIndex&&partyIndex<=7);

        }
        
        public static bool isSupporter(int partyIndex) {

            return partyIndex switch {

                0 => true,
                1 => true,
                2 => true,
                3 => true,
                _ => false

            };

        }

        public static bool isDps(int partyIndex) {

            return partyIndex switch {

                4 => true,
                5 => true,
                6 => true,
                7 => true,
                _ => false

            };

        }
        
        public static bool isMelee(int partyIndex) {

            return partyIndex switch {

                0 => true,
                1 => true,
                4 => true,
                5 => true,
                _ => false

            };

        }
        
        public static bool isRanged(int partyIndex) {

            return partyIndex switch {

                2 => true,
                3 => true,
                6 => true,
                7 => true,
                _ => false

            };

        }

        public static bool isTank(int partyIndex) {
            
            return isSupporter(partyIndex)&&isMelee(partyIndex);
            
        }
        
        public static bool isHealer(int partyIndex) {
            
            return isSupporter(partyIndex)&&isRanged(partyIndex);
            
        }
        
        public static bool isMeleeDps(int partyIndex) {
            
            return isDps(partyIndex)&&isMelee(partyIndex);
            
        }
        
        public static bool isRangedDps(int partyIndex) {
            
            return isDps(partyIndex)&&isRanged(partyIndex);
            
        }

        public static bool isInGroup1(int partyIndex) {
            
            return partyIndex switch {

                0 => true,
                2 => true,
                4 => true,
                6 => true,
                _ => false

            };
            
        }
        
        public static bool isInGroup2(int partyIndex) {
            
            return partyIndex switch {

                1 => true,
                3 => true,
                5 => true,
                7 => true,
                _ => false

            };
            
        }
        
        #endregion
        
    }
    
    #region Extensions
    
    public static class ScriptAccessoryExtensions
    {
        
        public static void tts(this ScriptAccessory accessory,string text,bool enableVanillaTts,bool enableDailyRoutinesTts) {
            
            if(enableVanillaTts) {
                    
                accessory.Method.TTS(text);
                    
            }

            else {
                
                if(enableDailyRoutinesTts) {
                    
                    accessory.Method.SendChat($"/pdr tts {text}");
                    
                }
                
            }
            
        }
        
    }
    
    #endregion
    
}