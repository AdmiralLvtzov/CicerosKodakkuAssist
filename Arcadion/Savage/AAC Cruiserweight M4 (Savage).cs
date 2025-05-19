using System;
using System.Collections.Concurrent;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Script;
using KodakkuAssist.Module.Draw;
using System.Collections.Generic;
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
        version:"0.0.0.7",
        note:scriptNotes,
        author:"Cicero 灵视")]

    public class AAC_Cruiserweight_M4_Savage
    {
        
        public const string scriptNotes=
            """
            This is the English version of the script for AAC Cruiserweight M4 (Savage), which is also known as M8S in short.
            
            The script will adapt to the popular strats among EU Party Finder with priority.
            
            The script is still work in progress.
            
            Link to RaidPlan 84d (Rinon combined with Quad) for Phase 1: https://raidplan.io/plan/B5Q3Mk62YKuTy84d
            Link to Toxic Friends RaidPlan DOG for Phase 2: https://raidplan.io/plan/9M-1G-mmOaaroDOG
            
            "Half Rinon" during Terrestrial Rage is the one combines the first half of Rinon with the second half of Clock.
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

        #endregion
        
        #region Variables
        
        private volatile int currentPhase=1;
        private volatile int currentSubPhase=1;
        
        /*
         
         Phase 1:
         
         Sub-phase 1: The first half of Millennial Decay
         Sub-phase 2: The second half of Millennial Decay
         
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

        #endregion

        #region Constants

        private static readonly Vector3 ARENA_CENTER_OF_PHASE_1=new Vector3(100,0,100);

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

        #endregion
        
        #region Initialization

        public void Init(ScriptAccessory accessory) {
            
            currentPhase=1;
            currentSubPhase=1;
            
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
                currentProperties.Scale=new(19);
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
                
                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                
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
            eventCondition:["Id:0197"],
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
            eventCondition:["Id:0197"],
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
                    
                    currentProperties.Scale=new(2,5.657f);
                    currentProperties.Position=point[i];
                    currentProperties.TargetPosition=point[(i+1)%4];
                    currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                    currentProperties.DestoryAt=16000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                }

                prompt="Clockwise.";

            }

            else {
                
                for(int i=4;i>=1;--i) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                    
                    currentProperties.Scale=new(2,5.657f);
                    currentProperties.Position=point[i%4];
                    currentProperties.TargetPosition=point[i-1];
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

            string prompt="Enable anti-knockback!";
            
            // System.Threading.Thread.Sleep(500);
            
            if(enablePrompts) {
                    
                accessory.Method.TextInfo(prompt,4500,true);
                    
            }
                    
            accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
        
        }
        
        [ScriptMethod(name:"Phase 1 The First Half Of Millennial Decay (Gust Acquisition)",
            eventType:EventTypeEnum.TargetIcon,
            suppress:2500,
            userControl:false)]
    
        public void Phase_1_The_First_Half_Of_Millennial_Decay_Gust_Acquisition(Event @event,ScriptAccessory accessory) {

            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }

            accessory.Log.Debug($"iconId={iconId}");

            if(iconId!=0) { // 0x178-0x178=0

                return;
                
            }

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
            
            accessory.Log.Debug($"iconId={iconId}");

            if(iconId!=0) { // 0x178-0x178=0

                return;
                
            }

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            gustFirstSetSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            Vector3 northwest=new Vector3(95.417f,0,90);
            Vector3 northeast=new Vector3(104.583f,0,90);
            Vector3 southwest=new Vector3(95.417f,0,110);
            Vector3 southeast=new Vector3(104.583f,0,110);
            // Initial positions: https://www.geogebra.org/calculator/kt3brffu
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

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
        
        [ScriptMethod(name:"Phase 1 The First Half Of Millennial Decay (Second Set Guidance)",
            eventType:EventTypeEnum.TargetIcon,
            suppress:7500)]
    
        public void Phase_1_The_First_Half_Of_Millennial_Decay_Second_Set_Guidance(Event @event,ScriptAccessory accessory) {

            if(!convertTargetIconId(@event["Id"], out var iconId)) {
                
                return;
                
            }
            
            accessory.Log.Debug($"iconId={iconId}");

            if(iconId!=0) { // 0x178-0x178=0

                return;
                
            }

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            gustSecondSetSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            Vector3 northwest=new Vector3(95.417f,0,90);
            Vector3 northeast=new Vector3(104.583f,0,90);
            Vector3 southwest=new Vector3(95.417f,0,110);
            Vector3 southeast=new Vector3(104.583f,0,110);
            // Initial positions: https://www.geogebra.org/calculator/kt3brffu
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

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

            string prompt="Don't enable anti-knockback!";
            
            if(enablePrompts) {
                    
                accessory.Method.TextInfo(prompt,4700,true);
                    
            }
                    
            accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
        
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
            
            Vector3 standbyPositionForTowers=new Vector3(100,0,98);
            Vector3 finalPositionForTowers=new Vector3(100,0,90);
            Vector3 standbyPositionForTethers=new Vector3(100,0,97);
            Vector3 finalPositionForTethers=new Vector3(100,0,89);
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
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
        
        [ScriptMethod(name:"Phase 1 The Second Half Of Millennial Decay (Sub-phase 2 Control)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41913"],
            userControl:false)]
    
        public void Phase_1_The_Second_Half_Of_Millennial_SubPhase_2_Control(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            currentSubPhase=3;

            windWolfTetherSemaphore.Reset();

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