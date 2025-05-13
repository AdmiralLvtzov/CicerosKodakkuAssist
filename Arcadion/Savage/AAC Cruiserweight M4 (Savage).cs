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
using Newtonsoft.Json.Linq;

namespace CicerosKodakkuAssist.Arcadion.Savage
{
    
    [ScriptType(name:"AAC Cruiserweight M4 (Savage)",
        territorys:[1263],
        guid:"aeb4391c-e8a6-4daa-ab71-18e44c94fab8",
        version:"0.0.0.1",
        note:notesOfTheScript,
        author:"Cicero 灵视")]

    public class AAC_Cruiserweight_M4_Savage
    {
        
        const string notesOfTheScript=
            """
            This is the English version of the script for AAC Cruiserweight M4 (Savage), which is also known as M8S in short.
            
            The script will adapt to the popular strats among EU Party Finder with priority.
            
            The script is still work in progress.
            
            Link to RaidPlan 84d (Rinon combined with Quad): https://raidplan.io/plan/B5Q3Mk62YKuTy84d
            Link to Toxic Friends RaidPlan DOG: https://raidplan.io/plan/9M-1G-mmOaaroDOG
            Link to Toxic Friends XOs: https://raidplan.io/plan/46uVU6o49FPuYXOs
            """;

        #region User_Settings

        [UserSetting("----- Global Settings ----- (This setting has no practical meaning.)")]
        public bool _____Global_Settings_____ { get; set; } = true;
        
        [UserSetting("Enbale Text Prompts")]
        public bool enablePrompts { get; set; } = true;
        [UserSetting("Enable Vanilla TTS")]
        public bool enableVanillaTts { get; set; } = true;
        [UserSetting("Enable Daily Routines TTS (It requires the plugin \"Daily Routines\" to be installed and enabled!)")]
        public bool enableDailyRoutinesTts { get; set; } = false;
        [UserSetting("Colour Of Highly Dangerous Attacks")]
        public ScriptColor colourOfHighlyDangerousAttacks { get; set; } = new() { V4 = new Vector4(1f,0f,0f,1f) }; // Red by default.
        [UserSetting("Colour Of Approximate Guidance")]
        public ScriptColor colourOfApproximateGuidance { get; set; } = new() { V4 = new Vector4(1f,1f,0f,1f) }; // Yellow by default.
        [UserSetting("Enable Shenanigans")]
        public bool enableShenanigans { get; set; } = false;
        
        [UserSetting("----- Phase 1 Settings ----- (This setting has no practical meaning.)")]
        public bool _____Phase_1_Settings_____ { get; set; } = true;
        
        [UserSetting("Strats Of Millenial Decay")]
        public StratsOfMillenialDecay stratOfMillenialDecay { get; set; }
        [UserSetting("Tanks Or Melee Go Further For The Second Set While Doing RaidPlan 84d During Millenial Decay")]
        public bool meleeGoFurther { get; set; } = true;
        [UserSetting("Strats Of Terrestrial Rage")]
        public StratsOfTerrestrialRage stratOfTerrestrialRage { get; set; }
        [UserSetting("Strats Of Beckon Moonlight")]
        public StratsOfBeckonMoonlight stratOfBeckonMoonlight { get; set; }
        
        [UserSetting("----- Phase 2 Settings ----- (This setting has no practical meaning.)")]
        public bool _____Phase_2_Settings_____ { get; set; } = true;
        
        [UserSetting("Strats Of Phase 2")]
        public StratsOfPhase2 stratOfPhase2 { get; set; }
        [UserSetting("Colour Of Platforms At Risk During Enrage")]
        public ScriptColor colourOfPlatformsAtRisk { get; set; } = new() { V4 = new Vector4(1f,1f,0f,1f) }; // Yellow by default.

        #endregion
        
        #region Variables
        
        private int currentPhase=1;
        private int currentSubPhase=1;

        #endregion

        #region Constants

        private readonly Vector3 ARENA_CENTER_OF_PHASE_1=new Vector3(100,0,100);

        #endregion
        
        #region Enumerations_And_Classes

        public enum StratsOfMillenialDecay {

            Rinon_Or_RaidPlan_84d,
            // Ferring,
            // Murderless_Ferring
            Other_Strats_Are_Work_In_Progress

        }
        
        public enum StratsOfTerrestrialRage {

            Rinon_Or_RaidPlan_84d,
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
            
            shenaniganSemaphore.Set();
            
        }

        #endregion
        
        #region Shenanigans
        
        private System.Threading.AutoResetEvent shenaniganSemaphore=new System.Threading.AutoResetEvent(false);
        private readonly List<string> quotes=[
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

            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
            
            Vector3 rawInnerPosition=new Vector3(100,0,93.5f);
            Vector3 rawOuterPosition=new Vector3(100,0,89.5f);
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
                currentProperties.TargetPosition=rotatePositionClockwise(rawInnerPosition,ARENA_CENTER_OF_PHASE_1,getDegree[myIndex].DegToRad());
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
                currentProperties.TargetPosition=rotatePositionClockwise(rawInnerPosition,ARENA_CENTER_OF_PHASE_1,getDegree[myIndex].DegToRad());
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
                currentProperties.TargetPosition=rotatePositionClockwise(rawOuterPosition,ARENA_CENTER_OF_PHASE_1,getDegree[myIndex].DegToRad());
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
                currentProperties.TargetPosition=rotatePositionClockwise(rawOuterPosition,ARENA_CENTER_OF_PHASE_1,getDegree[myIndex].DegToRad());
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            }
        
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
        
        public static Vector3 rotatePositionClockwise(Vector3 position,Vector3 center,double radian,bool preserveHeight=true) {

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