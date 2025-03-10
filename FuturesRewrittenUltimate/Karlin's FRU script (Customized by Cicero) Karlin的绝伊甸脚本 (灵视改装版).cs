using System;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Script;
using KodakkuAssist.Module.GameEvent.Struct;
using KodakkuAssist.Module.Draw;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommons;
using System.Numerics;
using Newtonsoft.Json;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Utility.Numerics;
using ECommons.MathHelpers;
using Newtonsoft.Json.Linq;

namespace CicerosKodakkuAssist.FuturesRewrittenUltimate
{
    
    [ScriptType(name:"Karlin's FRU script (Customized by Cicero) Karlin的绝伊甸脚本 (灵视改装版)",
        territorys:[1238],
        guid:"148718fd-575d-493a-8ac7-1cc7092aff85",
        version:"0.0.0.61",
        note:notesOfTheScript,
        author:"Karlin")]
    
    public class Futures_Rewritten_Ultimate
    {
        const string notesOfTheScript=
        """
        ***** Please read the note here carefully before running the script! *****
        ***** 请在使用此脚本前仔细阅读此处的说明! *****
        
        This is a customized version of Karlin's script for Futures Rewritten (Ultimate).
        The script was branched out from the version 0.0.0.10 and extensively customized by Cicero.
        Please configure the user settings of the script according to your user settings of the vanilla script before running it!
        And of course, please don't run the customized script and the vanilla script simultaneously.
        
        这是Karlin的另一个未来(绝伊甸)脚本的改装版本。
        脚本是基于0.0.0.10版本的,灵视对脚本进行了大幅度改装。
        在使用前请记得按照原版脚本重新配置一下这个脚本的用户设置!
        当然也请不要同时开着改装脚本和原版脚本。
        
        ***** Credits *****
        ***** 致谢 *****
        
        The original author also the founder: @karlin_z
        Helpers (sorted lexicographically):
         - @abigseal provided Fixed_H1_H2_R2_The_Rest_Fill_Vacancies for towers at the end of Phase 1.
         - @alexandria_prime provided Single_Line_In_HTD_Order, Single_Line_In_H1TDH2_Order and Face_The_Boss for Fall Of Faith in Phase 1.
        
        原作者兼奠基人: @karlin_z
        提供帮助的人(按字典序排序):
        - @abigseal为P1末尾踩塔提供了打法"固定H1_H2_D4剩余人补位"。
        - @alexandria_prime为P1信仰崩塌(四连抓)提供了打法"按HTD顺序单排","按H1TDH2顺序单排"和"面向Boss"。
        
        ***** New Features *****
        ***** 新功能 *****
        
        Phase 1:
         - Refinements for the entire phase.
         - Lots of new strats for Utopian Sky, Turn Of The Heaven, Fall Of Faith and the towers at the end.
        Phase 3:
         - Guidance of the second half (including the Double Group strat and the Locomotive strat);
        Phase 4:
         - Guidance related to Drachen Wanderer residues of the second half;
         - Fixes and refinements for vanilla guidance of the second half;
        Phase 5:
         - Guidance of Wings Dark And Light;
         - Guidance of Polarizing Strikes.
        
        P1:
         - 整个阶段的精修;
         - 乐园绝技(雾龙), 光轮召唤, 信仰崩塌(四连抓)和最后踩塔都增加了大量新打法;
        P3:
         - 二运指路(包括双分组法和车头法);
        P4:
         - 二运圣龙气息(龙头)白圈相关的指路;
         - 二运原版指路修复和细化;
        P5:
         - 光与暗之翼(踩塔)指路;
         - 极化打击(挡枪)指路。
        """;
        
        [UserSetting("-----全局设置----- (No actual meaning for this setting/此设置无实际意义)")]
        public bool _____Global_Settings_____ { get; set; } = true;
        [UserSetting("启用文本提示")]
        public bool Enable_Text_Prompts { get; set; } = true;
        [UserSetting("提示的语言")]
        public Languages_Of_Prompts Language_Of_Prompts { get; set; }

        [UserSetting("-----TTS设置----- (No actual meaning for this setting/此设置无实际意义)")]
        public bool _____TTS_Settings_____ { get; set; } = true;
        [UserSetting("TTS-Introduction (Please choose between TTS and DRTTS Prompts, do not enable both, if you don't know what is DR, do not turn it on)")]
        public bool TTS_introduction { get; set; } = true;
        [UserSetting("启用原生TTS提示(请与DR TTS提示二选一, 不要一起打开)")]
        public bool Enable_TTS_Prompts { get; set; } = true;
        [UserSetting("启用DR TTS提示(请与原生TTS提示二选一, 不要一起打开, 并确保你有Daily Routines插件(Make sure you have `Daily Routines` Plugin))")]
        public bool Enable_DRTTS_Prompts { get; set; } = false;

        [UserSetting("-----P1设置----- (No actual meaning for this setting/此设置无实际意义)")]
        public bool _____Phase1_Settings_____ { get; set; } = true;
        [UserSetting("P1 乐园绝技(雾龙)待机站位")]
        public Phase1_Standby_Positions_Of_Utopian_Sky Phase1_Standby_Position_Of_Utopian_Sky { get; set; }
        [UserSetting("P1 燃烧击(火雷直线)特性的颜色")]
        public ScriptColor Phase1_Colour_Of_Burnt_Strike_Characteristics { get; set; } = new() { V4=new(1f,1f,0f,1f) };
        [UserSetting("P1 光轮召唤分组")]
        public Phase1_Groups_Of_Turn_Of_The_Heavens Phase1_Group_Of_Turn_Of_The_Heavens { get; set; }
        [UserSetting("P1 信仰崩塌(四连抓)攻略")]
        public Phase1_Strats_Of_Fall_Of_Faith Phase1_Strat_Of_Fall_Of_Faith { get; set; }
        [UserSetting("P1 标记被连线的玩家 (Make sure only one in the party enable this!/小队内只能有一人启用此选项!)")]
        public bool Phase1_Mark_Players_Were_Tethered { get; set; } = false;
        [UserSetting("P1 确定左右的基准")]
        public Phase1_Benchmarks_During_Fall_Of_Faith Phase1_Benchmark_During_Fall_Of_Faith { get; set; }
        [UserSetting("P1 踩塔攻略")]
        public Phase1_Strats_Of_Towers Phase1_Strat_Of_Towers { get; set; }

        [UserSetting("-----P2设置----- (No actual meaning for this setting/此设置无实际意义)")]
        public bool _____Phase2_Settings_____ { get; set; } = true;
        [UserSetting("P2_光爆拉线方式")]
        public P2LightRampantTetherEmum P2LightRampantTetherDeal { get; set; }
        [UserSetting("P2_光爆八方站位方式")]
        public P2LightRampant8DirEmum P2LightRampant8DirSet { get; set; }

        [UserSetting("-----P3设置----- (No actual meaning for this setting/此设置无实际意义)")]
        public bool _____Phase3_Settings_____ { get; set; } = true;
        [UserSetting("P3_分灯方式")]
        public P3LampEmum P3LampDeal { get; set; }
        [UserSetting("P3二运 攻略")]
        public Phase3_Strats_Of_The_Second_Half Phase3_Strat_Of_The_Second_Half { get; set; }
        [UserSetting("P3二运 双分组法的分支")]
        public Phase3_Branches_Of_The_Double_Group_Strat Phase3_Branch_Of_The_Double_Group_Strat { get; set; }
        [UserSetting("P3二运 车头低换法的分支")]
        public Phase3_Branches_Of_The_Locomotive_Strat Phase3_Branch_Of_The_Locomotive_Strat { get; set; }
        [UserSetting("P3二运 场地划分方式")]
        public Phase3_Divisions_Of_The_Zone Phase3_Division_Of_The_Zone { get; set; } = Phase3_Divisions_Of_The_Zone.North_To_Southwest_For_The_Left_Group_左组从正北到西南;
        [UserSetting("P3二运 粗略指路与倒数第二次启示(地火)的颜色")]
        public ScriptColor Phase3_Colour_Of_Rough_Guidance_And_The_Penultimate_Apocalypse { get; set; } = new() { V4=new(0,1f,1f,1f) };
        [UserSetting("P3二运 引导暗夜舞蹈(最远死刑)的T")]
        public Tanks Phase3_Tank_Who_Baits_Darkest_Dance { get; set; } = Tanks.OT_ST;
        [UserSetting("P3二运 暗夜舞蹈(最远死刑)的颜色")]
        public ScriptColor Phase3_Colour_Of_Darkest_Dance { get; set; } = new() { V4=new(1f,0f,0f,1f) };

        [UserSetting("-----P4设置----- (No actual meaning for this setting/此设置无实际意义)")]
        public bool _____Phase4_Settings_____ { get; set; } = true;
        [UserSetting("P4_二运常/慢灯AOE显示时间(ms)")]
        public uint P4LampDisplayDur { get; set; } =3000;
        [UserSetting("P4二运 圣龙气息(龙头)碰撞箱的颜色")]
        public ScriptColor Phase4_Colour_Of_Drachen_Wanderer_Hitboxes { get; set; } = new() { V4=new(0,1f,1f,1f) };
        [UserSetting("P4二运 圣龙气息(龙头)碰撞箱的长度")]
        public float Phase4_Length_Of_Drachen_Wanderer_Hitboxes { get; set; } = 1.5f;
        [UserSetting("P4二运 光之潮汐(地火)的颜色")]
        public ScriptColor Phase4_Colour_Of_Tidal_Light { get; set; } = new() { V4=new(1f,1f,0f,1f) };
        [UserSetting("P4二运 暗炎喷发(分散)的白圈")]
        public Phase4_Relative_Positions_Of_Residues Phase4_Residue_Belongs_To_Dark_Eruption { get; set; } = Phase4_Relative_Positions_Of_Residues.Eastmost_最东侧;
        [UserSetting("P4二运 黑暗神圣(后分摊)的白圈")]
        public Phase4_Relative_Positions_Of_Residues Phase4_Residue_Belongs_To_Unholy_Darkness { get; set; } = Phase4_Relative_Positions_Of_Residues.About_East_次东侧;
        [UserSetting("P4二运 黑暗冰封(月环)的白圈")]
        public Phase4_Relative_Positions_Of_Residues Phase4_Residue_Belongs_To_Dark_Blizzard_III { get; set; } = Phase4_Relative_Positions_Of_Residues.About_West_次西侧;
        [UserSetting("P4二运 黑暗狂水(先分摊)的白圈")]
        public Phase4_Relative_Positions_Of_Residues Phase4_Residue_Belongs_To_Dark_Water_III { get; set; } = Phase4_Relative_Positions_Of_Residues.Westmost_最西侧;
        [UserSetting("P4二运 白圈指路的颜色")]
        public ScriptColor Phase4_Colour_Of_Residue_Guidance { get; set; } = new() { V4=new(1f,1f,0f,1f) };

        [UserSetting("-----P5设置----- (No actual meaning for this setting/此设置无实际意义)")]
        public bool _____Phase5_Settings_____ { get; set; } = true;
        [UserSetting("P5_地火颜色")]
        public ScriptColor P5PathColor { get; set; } = new() { V4=new(0,1,1,1)};
        [UserSetting("P5 Boss中轴线的颜色")]
        public ScriptColor Phase5_Colour_Of_The_Boss_Central_Axis { get; set; } = new() { V4=new(1f,0f,0f,1f) };
        [UserSetting("P5 璀璨之刃(地火)后Boss面向人群")]
        public bool Phase5_Boss_Faces_Players_After_Fulgent_Blade { get; set; } = true;
        [UserSetting("P5 光与暗之翼(踩塔)攻略")]
        public Phase5_Strats_Of_Wings_Dark_And_Light Phase5_Strat_Of_Wings_Dark_And_Light { get; set; }
        [UserSetting("P5 挑衅提醒")]
        public bool Phase5_Reminder_To_Provoke { get; set; } = true;
        [UserSetting("P5 极化打击(挡枪)顺序")]
        public Phase5_Orders_During_Polarizing_Strikes Phase5_Order_During_Polarizing_Strikes { get; set; }
        
        [UserSetting("-----开发者设置----- (No actual meaning for this setting/此设置无实际意义)")]
        public bool _____Developer_Settings_____ { get; set; } = true;
        [UserSetting("启用开发者模式")]
        public bool Enable_Developer_Mode { get; set; } = false;

        int? firstTargetIcon = null;
        double parse = 0;
        volatile bool isInPhase5=false;

        int P1雾龙计数 =0;
        int[] P1雾龙记录 = [0, 0, 0, 0];
        bool P1雾龙雷=false;
        bool P1转轮召雷 = false;
        List<int> P1转轮召抓人 = [0, 0, 0, 0, 0, 0, 0, 0];
        List<int> P1四连线 = [];
        bool P1四连线开始 = false;
        List<int> P1塔 = [0, 0, 0, 0];

        bool P2DDDircle = false;
        List<int> P2DDIceDir = [];
        List<int> P2RedMirror = [];
        ulong P2BossId = 0;
        List<int> P2LightRampantCircle = [];
        List<int> P2LightRampantBuff = [0, 0, 0, 0, 0, 0, 0, 0];
        bool P2LightRampantTetherDone = new();

        volatile string phase3_bossId="";
        List<int> P3FireBuff = [0, 0, 0, 0, 0, 0, 0, 0];
        List<int> P3WaterBuff = [0, 0, 0, 0, 0, 0, 0, 0];
        List<int> P3ReturnBuff = [0, 0, 0, 0, 0, 0, 0, 0];
        List<int> P3Lamp = [0, 0, 0, 0, 0, 0, 0, 0];
        List<int> P3LampWise = [0, 0, 0, 0, 0, 0, 0, 0];
        List<int> P3Stack = [0, 0, 0, 0, 0, 0, 0, 0];
        bool P3FloorFireDone = false;
        int P3FloorFire = 0;
        volatile List<Phase3_Types_Of_Dark_Water_III> phase3_typeOfDarkWaterIii=[
            Phase3_Types_Of_Dark_Water_III.NONE,
            Phase3_Types_Of_Dark_Water_III.NONE,
            Phase3_Types_Of_Dark_Water_III.NONE,
            Phase3_Types_Of_Dark_Water_III.NONE,
            Phase3_Types_Of_Dark_Water_III.NONE,
            Phase3_Types_Of_Dark_Water_III.NONE,
            Phase3_Types_Of_Dark_Water_III.NONE,
            Phase3_Types_Of_Dark_Water_III.NONE
        ];
        volatile int phase3_numberOfDarkWaterIiiHasBeenProcessed=0;
        volatile int phase3_roundOfDarkWaterIii=0;
        volatile int phase3_rangeSemaphoreOfDarkWaterIii=0;
        volatile int phase3_guidanceSemaphoreOfDarkWaterIii=0;
        List<int> phase3_doubleGroup_priority_asAConstant=[2,3,0,1,4,5,6,7];
        // The priority would be H1 H2 MT OT M1 M2 R1 R2 or H1 H2 MT ST D1 D2 D3 D4 temporarily if the Double Group strat is adopted.
        List<int> phase3_locomotive_priority_asAConstant=[0,1,2,3,7,6,5,4];
        // The priority would be MT OT H1 H2 R2 R1 M2 M1 or MT ST H1 H2 D4 D3 D2 D1 temporarily if the Locomotive strat is adopted.
        volatile bool phase3_hasConfirmedInitialSafePositions=false;
        Vector3 phase3_doubleGroup_initialSafePositionOfTheLeftGroup=new Vector3(100,0,100);
        Vector3 phase3_doubleGroup_initialSafePositionOfTheRightGroup=new Vector3(100,0,100);
        Vector3 phase3_doubleGroup_leftPositionToStackOfTheSecondRound=new Vector3(100,0,100);
        Vector3 phase3_doubleGroup_rightPositionToStackOfTheSecondRound=new Vector3(100,0,100);
        Vector3 phase3_locomotive_initialSafePositionOfTheLeftGroup=new Vector3(100,0,100);
        Vector3 phase3_locomotive_initialSafePositionOfTheRightGroup=new Vector3(100,0,100);
        Vector3 phase3_locomotive_leftPositionToStackOfTheSecondRound=new Vector3(100,0,100);
        Vector3 phase3_locomotive_rightPositionToStackOfTheSecondRound=new Vector3(100,0,100);
        Vector3 phase3_bossPositionAfterDarkestDance=new Vector3(100,0,100);
        Vector3 phase3_finalPositionOfTheBoss=new Vector3(100,0,100);
        
        ulong P4FragmentId;
        List<int> P4Tether = [-1, -1, -1, -1, -1, -1, -1, -1];
        List<int> P4Stack = [0, 0, 0, 0, 0, 0, 0, 0];
        bool P4TetherDone = false;
        List<int> P4ClawBuff = [0, 0, 0, 0, 0, 0, 0, 0];
        List<int> P4OtherBuff = [0, 0, 0, 0, 0, 0, 0, 0];
        int P4BlueTether = 0;
        List<Vector3> P4WhiteCirclePos = [];
        List<Vector3> P4WaterPos = [];
        volatile string phase4_id1OfTheDrachenWanderers="";
        volatile string phase4_id2OfTheDrachenWanderers="";
        readonly Object phase4_ReadwriteLockOfDrachenWandererIds_AsAConstant=new Object();
        volatile int phase4_timesTheWyrmclawDebuffWasRemoved=0;
        volatile List<ulong> phase4_residueIdsFromEastToWest=[0,0,0,0];
        // The leftmost (0), the about left (1), the about right (2), the rightmost (3) while facing south.
        volatile bool phase4_guidanceOfResiduesHasBeenGenerated=false;

        volatile string phase5_bossId="";
        volatile bool phase5_hasAcquiredTheFirstTower=false;
        volatile string phase5_indexOfTheFirstTower="";
        volatile bool phase5_hasConfirmedTheInitialPosition=false;
        Vector3 phase5_leftSideOfTheSouth_asAConstant=new Vector3(98,0,107);
        Vector3 phase5_rightSideOfTheSouth_asAConstant=new Vector3(102,0,107);
        Vector3 phase5_leftSideOfTheNortheast_asAConstant=new Vector3(107.06f,0,98.23f);
        Vector3 phase5_rightSideOfTheNortheast_asAConstant=new Vector3(105.06f,0,94.77f);
        Vector3 phase5_leftSideOfTheNorthwest_asAConstant=new Vector3(94.94f,0,94.77f);
        Vector3 phase5_rightSideOfTheNorthwest_asAConstant=new Vector3(92.94f,0,98.23f);
        Vector3 phase5_standbyPointBetweenSouthAndNortheast_asAConstant=new Vector3(106.06f,0,103.50f);
        Vector3 phase5_standbyPointBetweenSouthAndNorthwest_asAConstant=new Vector3(93.94f,0,103.50f);
        Vector3 phase5_standbyPointBetweenNortheastAndNorthwest_asAConstant=new Vector3(100,0,93);
        Vector3 phase5_positionToTakeHitsOnTheLeft_asAConstant=new Vector3(95.93f,0,104.07f);
        Vector3 phase5_positionToBeCoveredOnTheLeft_asAConstant=new Vector3(93.81f,0,106.19f);
        Vector3 phase5_positionToStandbyOnTheLeft_asAConstant=new Vector3(99.24f,0,108.72f);
        Vector3 phase5_positionToTakeHitsOnTheRight_asAConstant=new Vector3(104.07f,0,104.07f);
        Vector3 phase5_positionToBeCoveredOnTheRight_asAConstant=new Vector3(106.19f,0,106.19f);
        Vector3 phase5_positionToStandbyOnTheRight_asAConstant=new Vector3(100.76f,0,108.72f);
        // The left and right here refer to the left and right while facing the center of the zone (100,0,100).
        
        public enum Languages_Of_Prompts {
        
            Simplified_Chinese_简体中文,
            English_英文
        
        }
        
        public enum Tanks {
            
            MT,
            OT_ST
            
        }

        public enum Phase1_Standby_Positions_Of_Utopian_Sky {
            
            Swap_OT_And_M2_交换ST与D4,
            Both_Tanks_Go_To_The_Center_双T去中间
            
        }
        
        public enum Phase1_Groups_Of_Turn_Of_The_Heavens {
            
            MT_OT_H1_H2_Go_North_MT_ST_H1_H2去北,
            MT_H1_M1_R1_Go_North_MT_H1_D1_D3去北,
            MT_OT_R1_R2_Go_North_MT_ST_D3_D4去北
            
        }

        public enum Phase1_Strats_Of_Fall_Of_Faith {
            
            Single_Line_In_THD_Order_按THD顺序单排,
            Single_Line_In_HTD_Order_按HTD顺序单排,
            Single_Line_In_H1TDH2_Order_按H1TDH2顺序单排,
            Double_Lines_H12MOT_Left_M12R12_Right_双排左H12MST右D1234,
            Double_Lines_MOTH12_Left_M12R12_Right_双排左MSTH12右D1234,
            Other_Strats_Are_Work_In_Progress_其他攻略正在施工中
            
        }

        public enum Phase1_Benchmarks_During_Fall_Of_Faith {
            
            Face_True_North_面向正北,
            Face_The_Boss_面向Boss
            
        }

        public enum Phase1_Strats_Of_Towers {
            
            Purely_Based_On_The_Priority_纯粹根据优先级,
            Fixed_H1_H2_R2_Priority_For_The_Rest_固定H1_H2_D4剩余人优先级,
            Fixed_H1_H2_R2_The_Rest_Fill_Vacancies_固定H1_H2_D4剩余人补位
            
        }
        
        public enum P2LightRampant8DirEmum
        {
            Normal,
            TN_Up
        }
        public enum P2LightRampantTetherEmum
        {
            CircleNum,
            LTeam,
            AC_Cross,
            NewGrey9
        }

        public enum P3LampEmum
        {
            MGL
        }

        public enum Phase3_Strats_Of_The_Second_Half {
            
            Double_Group_双分组法,
            High_Priority_As_Locomotives_车头低换法,
            Other_Strats_Are_Work_In_Progress_其他攻略正在施工中
            
        }

        public enum Phase3_Branches_Of_The_Double_Group_Strat {
            
            Based_On_Safe_Positions_安全区为基准,
            Based_On_The_Second_Apocalypse_第二次启示为基准
            
        }

        public enum Phase3_Branches_Of_The_Locomotive_Strat {
            
            MT_And_M1_As_Locomotives_MT和D1为车头,
            Others_As_Locomotives_人群为车头
            
        }

        public enum Phase3_Divisions_Of_The_Zone {
            
            North_To_Southwest_For_The_Left_Group_左组从正北到西南,
            Northwest_To_South_For_The_Left_Group_左组从西北到正南,
            Other_Allocations_Are_Work_In_Progress_其他分配方式正在施工中
            
        }
        
        public enum Phase3_Types_Of_Dark_Water_III {
            
            LONG,
            MEDIUM,
            SHORT,
            NONE
            
        }

        public enum Phase4_Relative_Positions_Of_Residues {

            Eastmost_最东侧,
            About_East_次东侧,
            About_West_次西侧,
            Westmost_最西侧,
            Unknown_未知

        }
        
        public enum Phase5_Strats_Of_Wings_Dark_And_Light {
            
            Grey9_Brain_Dead_灰九脑死法,
            Other_Strats_Are_Work_In_Progress_其他攻略正在施工中
            
        }

        public enum Phase5_Orders_During_Polarizing_Strikes {
            
            Tanks_Melees_Ranges_Healers_坦克近战远程奶妈,
            Tanks_Healers_Melees_Ranges_坦克奶妈近战远程,
            Other_Orders_Are_Work_In_Progress_其他顺序正在施工中
            
        }

        public void Init(ScriptAccessory accessory)
        {
            accessory.Method.RemoveDraw(".*");
            if (Phase1_Mark_Players_Were_Tethered)
                accessory.Method.MarkClear();
            parse = 1d;
            isInPhase5=false;
            
            P1雾龙记录 = [0, 0, 0, 0];
            P1雾龙计数 = 0;
            P1转轮召抓人 = [0, 0, 0, 0, 0, 0, 0, 0];
            P1四连线 = [];
            P1四连线开始 = false;
            P1塔 = [0, 0, 0, 0];

            P2DDIceDir.Clear();

            phase3_bossId="";
            P3FloorFireDone = false;
            P3Stack = [0, 0, 0, 0, 0, 0, 0, 0];
            phase3_typeOfDarkWaterIii=[
                Phase3_Types_Of_Dark_Water_III.NONE,
                Phase3_Types_Of_Dark_Water_III.NONE,
                Phase3_Types_Of_Dark_Water_III.NONE,
                Phase3_Types_Of_Dark_Water_III.NONE,
                Phase3_Types_Of_Dark_Water_III.NONE,
                Phase3_Types_Of_Dark_Water_III.NONE,
                Phase3_Types_Of_Dark_Water_III.NONE,
                Phase3_Types_Of_Dark_Water_III.NONE
            ];
            phase3_numberOfDarkWaterIiiHasBeenProcessed=0;
            phase3_roundOfDarkWaterIii=0;
            phase3_rangeSemaphoreOfDarkWaterIii=0;
            phase3_guidanceSemaphoreOfDarkWaterIii=0;
            phase3_hasConfirmedInitialSafePositions=false;
            phase3_doubleGroup_initialSafePositionOfTheLeftGroup=new Vector3(100,0,100);
            phase3_doubleGroup_initialSafePositionOfTheRightGroup=new Vector3(100,0,100);
            phase3_doubleGroup_leftPositionToStackOfTheSecondRound=new Vector3(100,0,100);
            phase3_doubleGroup_rightPositionToStackOfTheSecondRound=new Vector3(100,0,100);
            phase3_locomotive_initialSafePositionOfTheLeftGroup=new Vector3(100,0,100);
            phase3_locomotive_initialSafePositionOfTheRightGroup=new Vector3(100,0,100);
            phase3_locomotive_leftPositionToStackOfTheSecondRound=new Vector3(100,0,100);
            phase3_locomotive_rightPositionToStackOfTheSecondRound=new Vector3(100,0,100);
            phase3_bossPositionAfterDarkestDance=new Vector3(100,0,100);
            phase3_finalPositionOfTheBoss=new Vector3(100,0,100);

            phase4_id1OfTheDrachenWanderers="";
            phase4_id2OfTheDrachenWanderers="";
            phase4_timesTheWyrmclawDebuffWasRemoved=0;
            phase4_residueIdsFromEastToWest=[0,0,0,0];
            phase4_guidanceOfResiduesHasBeenGenerated=false;

            phase5_bossId="";
            phase5_hasAcquiredTheFirstTower=false;
            phase5_indexOfTheFirstTower="";
            phase5_hasConfirmedTheInitialPosition=false;
        }

        #region P1
        
        [ScriptMethod(name:"----- Phase 1 ----- (No actual meaning for this toggle/此开关无实际意义)",
            eventType:EventTypeEnum.NpcYell,
            eventCondition:["Give me your tired",
                            "给我你们疲倦的人"])]
        
        public void Phase1_Placeholder(Event @event, ScriptAccessory accessory) { }

        [ScriptMethod(name: "P1_八方雷火_引导扇形",eventType: EventTypeEnum.StartCasting,eventCondition: ["ActionId:regex:^((4014[48])|40329|40330)$"])]
        public void P1_八方雷火_引导扇形(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            foreach (var pm in accessory.Data.PartyList)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P1_八方雷火_引导扇形";
                dp.Scale = new(60);
                dp.Radian = float.Pi / 8;
                dp.Owner = sid;
                dp.TargetObject=pm;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.DestoryAt = 7000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
            }

        }
        [ScriptMethod(name: "P1_八方雷火_后续扇形", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^(40145)$", "TargetIndex:1"])]
        public void P1_八方雷火_后续扇形(Event @event, ScriptAccessory accessory)
        {
            var dur = 2000;
            if (parse != 1d) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            if (!float.TryParse(@event["SourceRotation"], out var rot)) return;
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_八方雷火_后续扇形1";
            dp.Scale = new(60);
            dp.FixRotation = true;
            dp.Rotation = rot;
            dp.Radian = float.Pi / 8;
            dp.Owner = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = dur;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_八方雷火_后续扇形2";
            dp.Scale = new(60);
            dp.Radian = float.Pi / 8;
            dp.FixRotation = true;
            dp.Rotation = rot + float.Pi / -8;
            dp.Owner = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 2000;
            dp.DestoryAt = dur;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_八方雷火_后续扇形3";
            dp.Scale = new(60);
            dp.FixRotation = true;
            dp.Rotation = rot + float.Pi / -4;
            dp.Radian = float.Pi / 8;
            dp.Owner = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 4000;
            dp.DestoryAt = dur;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

        }
        [ScriptMethod(name: "P1_八方雷火_分散分摊", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^((4014[48])|40329|40330)$"])]
        public void P1_八方雷火_分散分摊(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            string prompt="";

            if (@event["ActionId"]== "40148" || @event["ActionId"] == "40330")
            {
                foreach (var pm in accessory.Data.PartyList)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_八方雷火_分散";
                    dp.Scale = new(6);
                    dp.Owner = pm;
                    dp.Color = accessory.Data.DefaultDangerColor;
                    dp.Delay = 5000;
                    dp.DestoryAt = 4000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
                }
                
                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                    prompt="分散";

                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                    prompt="Spread";

                }
                
            }
            else
            {
                int[] group = [6, 7, 4, 5, 2, 3, 0, 1];
                var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                for(int i=4;i<=7;++i) {
                    // The drawing owners here were adjusted a bit by Cicero.
                    // Here's an interesting fact - the action Sinsmoke (stack) will always target DPS.
                    
                    var ismygroup = myindex == i || group[i] == myindex;

                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_八方雷火_分摊";
                    dp.Scale = new(6);
                    dp.Owner = accessory.Data.PartyList[i];
                    dp.Color = ismygroup ? accessory.Data.DefaultSafeColor : accessory.Data.DefaultDangerColor;
                    dp.Delay = 5000;
                    dp.DestoryAt = 4000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
                }
                
                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                    prompt="分摊";

                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                    prompt="Stack";

                }
                
            }
            
            System.Threading.Thread.Sleep(5000);

            if(!prompt.Equals("")) {
                
                if(Enable_Text_Prompts) {
                    
                    accessory.Method.TextInfo(prompt,1500);
                
                }

                accessory.TTS($"{prompt}", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

            }

        }
        [ScriptMethod(name: "P1_八方雷火_引导位置", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^((4014[48])|40329|40330)$"])]
        public void P1_八方雷火_引导位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            var spread = @event["ActionId"] == "40148"|| @event["ActionId"] == "40330";
            var rot8 = myindex switch
            {
                0 => 0,
                1 => 2,
                2 => 6,
                3 => 4,
                4 => 5,
                5 => 3,
                6 => 7,
                7 => 1,
                _ => 0,
            };
            var outPoint = spread && (myindex == 2 || myindex == 3 || myindex == 6 || myindex == 7);
            var mPosEnd = RotatePoint(outPoint? new(100, 0, 90) : new(100, 0, 95), new(100, 0, 100), float.Pi / 4 * rot8);

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_八方雷火_引导位置";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = mPosEnd;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 7000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

        }
        [ScriptMethod(name: "P1_T死刑Buff爆炸", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:4166"])]
        public void P1_T死刑Buff爆炸(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            if(!int.TryParse(@event["DurationMilliseconds"], out var dur)) return;
            string prompt="";

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_T死刑Buff爆炸1";
            dp.Scale = new(10);
            dp.Owner = tid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = dur- 5000;
            dp.DestoryAt = 5000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_T死刑Buff爆炸2";
            dp.Scale = new(10);
            dp.Owner = tid;
            dp.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
            dp.CentreOrderIndex = 1;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = dur - 5000;
            dp.DestoryAt = 5000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            if(accessory.Data.PartyList.IndexOf(accessory.Data.Me)==0
               ||
               accessory.Data.PartyList.IndexOf(accessory.Data.Me)==1) {
                
                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                    prompt="稍微靠近另一个T";

                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                    prompt="Get slightly closer to another tank";

                }
                
            }
            
            if(2<=accessory.Data.PartyList.IndexOf(accessory.Data.Me)
               &&
               accessory.Data.PartyList.IndexOf(accessory.Data.Me)<=7) {
                
                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                    prompt="远离双T";

                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                    prompt="Stay away from both tanks";

                }
                
            }
            
            System.Threading.Thread.Sleep(dur-5000);
            
            if(!prompt.Equals("")) {
                
                if(Enable_Text_Prompts) {
                    
                    accessory.Method.TextInfo(prompt,1500);
                
                }

                accessory.TTS($"{prompt}", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

            }

        }
        [ScriptMethod(name: "P1_雾龙_位置记录", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(40158)$"], userControl: false)]
        public void P1_雾龙_位置记录(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            var obj= accessory.Data.Objects.SearchByEntityId(((uint)sid)+1);
            if(obj == null) return;
            var dir8= PositionTo8Dir(obj.Position, new(100, 0, 100));
            P1雾龙记录[dir8 % 4] = 1;
        }
        [ScriptMethod(name: "P1_雾龙_雷火记录", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^(4015[45])$"], userControl: false)]
        public void P1_雾龙_雷火记录(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;
            P1雾龙雷 = (@event["ActionId"] == "40155");
        }
        [ScriptMethod(name: "P1_雾龙_范围", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(40158)$"])]
        public void P1_雾龙_范围(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_雾龙范围";
            dp.Scale = new(16,50);
            dp.Owner = sid+1;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 9000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);

        }
        [ScriptMethod(name: "P1_雾龙_分散分摊", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^(4015[45])$"])]
        public void P1_雾龙_分散分摊(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;
            string prompt="";

            if (@event["ActionId"] == "40155")
            {
                foreach (var pm in accessory.Data.PartyList)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_雾龙_分散";
                    dp.Scale = new(5);
                    dp.Owner = pm;
                    dp.Color = accessory.Data.DefaultDangerColor;
                    dp.Delay = 10000;
                    dp.DestoryAt = 5000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
                }
                
                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                    prompt="分散";

                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                    prompt="Spread";

                }
                
            }
            else
            {
                List<int> h1group = [0, 2, 4, 6];
                var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);

                var isH1group = h1group.Contains(myindex);

                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P1_雾龙_分摊1";
                dp.Scale = new(6);
                dp.Owner = accessory.Data.PartyList[2];
                dp.Color = isH1group ? accessory.Data.DefaultSafeColor : accessory.Data.DefaultDangerColor;
                dp.Delay = 10000;
                dp.DestoryAt = 5000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P1_雾龙_分摊2";
                dp.Scale = new(6);
                dp.Owner = accessory.Data.PartyList[3];
                dp.Color = !isH1group ? accessory.Data.DefaultSafeColor : accessory.Data.DefaultDangerColor;
                dp.Delay = 10000;
                dp.DestoryAt = 5000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
                
                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                    prompt="分摊";

                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                    prompt="Stack";

                }
                
            }
            
            System.Threading.Thread.Sleep(10000);
            
            if(!prompt.Equals("")) {
                
                if(Enable_Text_Prompts) {
                    
                    accessory.Method.TextInfo(prompt,1500);
                
                }

                accessory.TTS($"{prompt}", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

            }

        }
        
        [ScriptMethod(name:"Phase1 Standby Position Of Utopian Sky 乐园绝技(雾龙)待机位置",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(4015[45])$"])]
        
        public void Phase1_Standby_Position_Of_Utopian_Sky_乐园绝技待机位置(Event @event, ScriptAccessory accessory) {

            if(parse!=1d) {

                return;
                
            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            if(Phase1_Standby_Position_Of_Utopian_Sky==Phase1_Standby_Positions_Of_Utopian_Sky.Swap_OT_And_M2_交换ST与D4) {

                int rotationMultiplier=myIndex switch {
                    0 => 0,
                    1 => 1,
                    2 => 6,
                    3 => 4,
                    4 => 5,
                    5 => 3,
                    6 => 7,
                    7 => 2
                };
                
                var myPosition=RotatePoint(new(100,0,81),new(100,0,100),float.Pi/4*rotationMultiplier);
                
                if(myIndex==0) {
                    
                    myPosition=RotatePoint(myPosition,new(100,0,100),float.Pi/72);
                    
                }

                if(myIndex==1) {
                    
                    myPosition=RotatePoint(myPosition,new(100,0,100),-(float.Pi/72));
                    
                }

                if(myIndex==6) {
                    
                    myPosition=RotatePoint(myPosition,new(100,0,100),-(float.Pi/36));
                    
                }
                
                if(myIndex==7) {
                    
                    myPosition=RotatePoint(myPosition,new(100,0,100),float.Pi/36);
                    
                }

                var currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                currentProperty.Name="Phase1_Standby_Position_Of_Utopian_Sky_乐园绝技待机位置";
                currentProperty.Scale=new(2);
                currentProperty.Owner=accessory.Data.Me;
                currentProperty.TargetPosition=myPosition;
                currentProperty.ScaleMode|=ScaleMode.YByDistance;
                currentProperty.Color=accessory.Data.DefaultSafeColor;
                currentProperty.DestoryAt=9000;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

            }

            if(Phase1_Standby_Position_Of_Utopian_Sky==Phase1_Standby_Positions_Of_Utopian_Sky.Both_Tanks_Go_To_The_Center_双T去中间) {

                var myPosition=new Vector3(100,0,100);
                
                if(myIndex==0) {

                    myPosition=new Vector3(100f,0f,94.5f);

                }

                if(myIndex==1) {
                    
                    myPosition=new Vector3(100f,0f,105.5f);
                    
                }

                if(2<=myIndex&&myIndex<=7) {

                    int rotationMultiplier=myIndex switch {
                        2 => 6,
                        3 => 2,
                        4 => 5,
                        5 => 3,
                        6 => 7,
                        7 => 1
                    };

                    myPosition=RotatePoint(new(100,0,81),new(100,0,100),float.Pi/4*rotationMultiplier);
                    
                }

                if(myPosition.Equals(new Vector3(100,0,100))) {

                    return;

                }

                var currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                currentProperty.Name="Phase1_Standby_Position_Of_Utopian_Sky_乐园绝技待机位置";
                currentProperty.Scale=new(2);
                currentProperty.Owner=accessory.Data.Me;
                currentProperty.TargetPosition=myPosition;
                currentProperty.ScaleMode|=ScaleMode.YByDistance;
                currentProperty.Color=accessory.Data.DefaultSafeColor;
                currentProperty.DestoryAt=9000;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                
            }

        }
        
        [ScriptMethod(name: "P1_雾龙_处理位置", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(40158)$"])]
        public void P1_雾龙_处理位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;

            lock (this)
            {
                P1雾龙计数 ++; 
                if(P1雾龙计数 != 3) return;
                Task.Delay(250).ContinueWith(t =>
                {
                    if (!P1雾龙雷)
                    {
                        var safeDir = P1雾龙记录.IndexOf(0);
                        List<int> h1group = [0, 2, 4, 6];
                        var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                        var isH1group = h1group.Contains(myindex);
                        var rot8 = safeDir switch
                        {
                            0 => isH1group ? 0 : 4,
                            1 => isH1group ? 5 : 1,
                            2 => isH1group ? 6 : 2,
                            3 => isH1group ? 7 : 3,
                            _ => 0
                        };
                        var mPosEnd = RotatePoint(new(100,0,84), new(100, 0, 100), float.Pi / 4 * rot8);

                        var dp = accessory.Data.GetDefaultDrawProperties();
                        dp.Name = "P1_雾龙_分摊处理位置";
                        dp.Scale = new(2);
                        dp.Owner = accessory.Data.Me;
                        dp.TargetPosition = mPosEnd;
                        dp.ScaleMode |= ScaleMode.YByDistance;
                        dp.Color = accessory.Data.DefaultSafeColor;
                        dp.DestoryAt = 9000;
                        accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                    }
                    else
                    {
                        var safeDir = P1雾龙记录.IndexOf(0);
                        List<int> h1group = [0, 2, 4, 6];
                        var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                        var isH1group = h1group.Contains(myindex);
                        Vector3 p1 = new(100.0f, 0, 88.0f);
                        Vector3 p2 = new(100.0f, 0, 80.5f);
                        Vector3 p3 = new(106.5f, 0, 81.5f);
                        Vector3 p4 = new(093.5f, 0, 81.5f);
                        var rot8 = safeDir switch
                        {
                            0 => isH1group ? 0 : 4,
                            1 => isH1group ? 5 : 1,
                            2 => isH1group ? 6 : 2,
                            3 => isH1group ? 7 : 3,
                            _ => 0
                        };
                        var myPosA = myindex switch
                        {
                            0 => p2,
                            1 => p2,
                            2 => p1,
                            3 => p1,
                            4 => p3,
                            5 => p3,
                            6 => p4,
                            7 => p4,
                            _ => p1,
                        };
                        var mPosEnd = RotatePoint(myPosA, new(100, 0, 100), float.Pi / 4 * rot8);

                        var dp = accessory.Data.GetDefaultDrawProperties();
                        dp.Name = "P1_雾龙_分散处理位置";
                        dp.Scale = new(2);
                        dp.Owner = accessory.Data.Me;
                        dp.TargetPosition = mPosEnd;
                        dp.ScaleMode |= ScaleMode.YByDistance;
                        dp.Color = accessory.Data.DefaultSafeColor;
                        dp.DestoryAt = 9000;
                        accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                    }
                });
                
            }

        }


        [ScriptMethod(name: "P1_转轮召_雷火记录", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(4015[01])$"], userControl: false)]
        public void P1_转轮召_雷火记录(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;
            P1转轮召雷 = (@event["ActionId"] == "40151");
        }
        
        [ScriptMethod(name:"Phase1 Thunder Burnt Strike 雷燃烧击",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(40164)$"])]
        
        public void Phase1_Thunder_Burnt_Strike_雷燃烧击(Event @event, ScriptAccessory accessory) {

            if(parse!=1d) {
                
                return;
                
            }

            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }

            var currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            currentProperty.Name="Phase1_Second_Strike_Of_Thunder_Burnt_Strike_雷燃烧击第二击";
            currentProperty.Scale=new(20,40);
            currentProperty.Owner=sourceId;
            currentProperty.Color=Phase1_Colour_Of_Burnt_Strike_Characteristics.V4.WithW(1f);
            currentProperty.Delay=4000;
            currentProperty.DestoryAt=5750;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperty);
            
            currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            currentProperty.Name="Phase1_First_Strike_Of_Thunder_Burnt_Strike_雷燃烧击第一击";
            currentProperty.Scale=new(10,40);
            currentProperty.Owner=sourceId;
            currentProperty.Color=accessory.Data.DefaultDangerColor.WithW(3f);
            currentProperty.Delay=4000;
            currentProperty.DestoryAt=3750;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperty);

        }
        
        [ScriptMethod(name:"Phase1 Fire Burnt Strike 火燃烧击",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(40161)$"])]
        
        public void Phase1_Fire_Burnt_Strike_火燃烧击(Event @event, ScriptAccessory accessory) {
            
            if(parse!=1d) {
                
                return;
                
            }
            
            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }

            var currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            currentProperty.Name="Phase1_First_Strike_Of_Fire_Burnt_Strike_火燃烧击第一击";
            currentProperty.Scale=new(10,40);
            currentProperty.Owner=sourceId;
            currentProperty.Color=accessory.Data.DefaultDangerColor.WithW(3f);
            currentProperty.Delay=4000;
            currentProperty.DestoryAt=3750;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperty);
            
            currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            currentProperty.Name="Phase1_Central_Axis_Of_Fire_Burnt_Strike_火燃烧击中轴线";
            currentProperty.Scale=new(0.5f,40f);
            currentProperty.Owner=sourceId;
            currentProperty.Color=Phase1_Colour_Of_Burnt_Strike_Characteristics.V4.WithW(25f);
            currentProperty.Delay=4000;
            currentProperty.DestoryAt=5750;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperty);

            for(int i=1;i<=3;++i) {
                
                currentProperty=accessory.Data.GetDefaultDrawProperties();
            
                currentProperty.Name="Phase1_Knockback_Direction_Of_Fire_Burnt_Strike_火燃烧击击退方向";
                currentProperty.Scale=new(2f,7f);
                currentProperty.Owner=sourceId;
                currentProperty.Color=Phase1_Colour_Of_Burnt_Strike_Characteristics.V4.WithW(1f);
                currentProperty.Offset=new Vector3(0,0,-(i*10));
                currentProperty.Rotation=float.Pi/2;
                currentProperty.Delay=4000;
                currentProperty.DestoryAt=5750;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperty);
                
                currentProperty=accessory.Data.GetDefaultDrawProperties();
            
                currentProperty.Name="Phase1_Knockback_Direction_Of_Fire_Burnt_Strike_火燃烧击击退方向";
                currentProperty.Scale=new(2f,7f);
                currentProperty.Owner=sourceId;
                currentProperty.Color=Phase1_Colour_Of_Burnt_Strike_Characteristics.V4.WithW(1f);
                currentProperty.Offset=new Vector3(0,0,-(i*10));
                currentProperty.Rotation=-(float.Pi/2);
                currentProperty.Delay=4000;
                currentProperty.DestoryAt=5750;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperty);
                
            }

        }

        [ScriptMethod(name: "P1_转轮召_抓人记录", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:4165"],userControl:false)]
        public void P1_转轮召_抓人记录(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            lock (this)
            {
                P1转轮召抓人[accessory.Data.PartyList.IndexOf(((uint)tid))] = 1;
            }
        }
        
        [ScriptMethod(name:"Phase1 Stack Range Of Turn Of The Heavens 光轮召唤分摊范围",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(40152)$"])]
        
        public void Phase1_Stack_Range_Of_Turn_Of_The_Heavens_光轮召唤分摊范围(Event @event, ScriptAccessory accessory) {

            if(parse!=1d) {
                
                return;
                
            }
            
            var currentPosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            if(MathF.Abs(currentPosition.Z-100)>1) {
                
                return;
                
            }
            
            bool hasSelectedAStrat=false;
            int highPriorityStack=P1转轮召抓人.IndexOf(1);
            int lowPriorityStack=P1转轮召抓人.LastIndexOf(1);
            List<int> membersOfTheNorthGroup=[];
            
            if(Phase1_Group_Of_Turn_Of_The_Heavens==Phase1_Groups_Of_Turn_Of_The_Heavens.MT_OT_H1_H2_Go_North_MT_ST_H1_H2去北) {

                hasSelectedAStrat=true;
                
                membersOfTheNorthGroup.Add(highPriorityStack);

                if(1!=highPriorityStack&&1!=lowPriorityStack) {
                    
                    membersOfTheNorthGroup.Add(1);
                    
                }

                if(2!=highPriorityStack&&2!=lowPriorityStack) {

                    membersOfTheNorthGroup.Add(2);
                    
                }

                if(3!=highPriorityStack&&3!=lowPriorityStack) {
                    
                    membersOfTheNorthGroup.Add(3);
                    
                }

                if(membersOfTheNorthGroup.Count<4
                   &&
                   0!=highPriorityStack
                    &&
                   0!=lowPriorityStack) {
                    
                    membersOfTheNorthGroup.Add(0);
                    
                }

                if(membersOfTheNorthGroup.Count<4
                   &&
                   4!=highPriorityStack
                   &&
                   4!=lowPriorityStack) {
                    
                    membersOfTheNorthGroup.Add(4);
                    
                }
                
            }
            
            if(Phase1_Group_Of_Turn_Of_The_Heavens==Phase1_Groups_Of_Turn_Of_The_Heavens.MT_H1_M1_R1_Go_North_MT_H1_D1_D3去北) {

                hasSelectedAStrat=true;
                
                membersOfTheNorthGroup.Add(highPriorityStack);

                if(2!=highPriorityStack&&2!=lowPriorityStack) {
                    
                    membersOfTheNorthGroup.Add(2);
                    
                }

                if(4!=highPriorityStack&&4!=lowPriorityStack) {

                    membersOfTheNorthGroup.Add(4);
                    
                }

                if(6!=highPriorityStack&&6!=lowPriorityStack) {
                    
                    membersOfTheNorthGroup.Add(6);
                    
                }

                if(membersOfTheNorthGroup.Count<4
                   &&
                   0!=highPriorityStack
                   &&
                   0!=lowPriorityStack) {
                    
                    membersOfTheNorthGroup.Add(0);
                    
                }

                if(membersOfTheNorthGroup.Count<4
                   &&
                   1!=highPriorityStack
                   &&
                   1!=lowPriorityStack) {
                    
                    membersOfTheNorthGroup.Add(1);
                    
                }
                
            }
            
            if(Phase1_Group_Of_Turn_Of_The_Heavens==Phase1_Groups_Of_Turn_Of_The_Heavens.MT_OT_R1_R2_Go_North_MT_ST_D3_D4去北) {

                hasSelectedAStrat=true;
                
                membersOfTheNorthGroup.Add(highPriorityStack);

                if(1!=highPriorityStack&&1!=lowPriorityStack) {
                    
                    membersOfTheNorthGroup.Add(1);
                    
                }

                if(6!=highPriorityStack&&6!=lowPriorityStack) {

                    membersOfTheNorthGroup.Add(6);
                    
                }

                if(7!=highPriorityStack&&7!=lowPriorityStack) {
                    
                    membersOfTheNorthGroup.Add(7);
                    
                }

                if(membersOfTheNorthGroup.Count<4
                   &&
                   0!=highPriorityStack
                   &&
                   0!=lowPriorityStack) {
                    
                    membersOfTheNorthGroup.Add(0);
                    
                }

                if(membersOfTheNorthGroup.Count<4
                   &&
                   4!=highPriorityStack
                   &&
                   4!=lowPriorityStack) {
                    
                    membersOfTheNorthGroup.Add(4);
                    
                }
                
            }

            if(!hasSelectedAStrat
               ||
               membersOfTheNorthGroup.Count!=4) {
                
                var currentProperty=accessory.Data.GetDefaultDrawProperties();
                                        
                currentProperty.Name="Phase1_Stack_Range_Of_Turn_Of_The_Heavens_光轮召唤分摊范围";
                currentProperty.Scale=new(6);
                currentProperty.Owner=accessory.Data.PartyList[highPriorityStack];
                currentProperty.Color=accessory.Data.DefaultDangerColor;
                currentProperty.Delay=6000;
                currentProperty.DestoryAt=5000;
                
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperty);
                
                currentProperty=accessory.Data.GetDefaultDrawProperties();
                                        
                currentProperty.Name="Phase1_Stack_Range_Of_Turn_Of_The_Heavens_光轮召唤分摊范围";
                currentProperty.Scale=new(6);
                currentProperty.Owner=accessory.Data.PartyList[lowPriorityStack];
                currentProperty.Color=accessory.Data.DefaultDangerColor;
                currentProperty.Delay=6000;
                currentProperty.DestoryAt=5000;
                
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperty);
                
            }

            else {

                bool inTheNorthGroup=membersOfTheNorthGroup.Contains(accessory.Data.PartyList.IndexOf(accessory.Data.Me));
                var currentProperty=accessory.Data.GetDefaultDrawProperties();
                                        
                currentProperty.Name="Phase1_Stack_Range_Of_Turn_Of_The_Heavens_光轮召唤分摊范围";
                currentProperty.Scale=new(6);
                currentProperty.Owner=accessory.Data.PartyList[highPriorityStack];
                currentProperty.Delay=6000;
                currentProperty.DestoryAt=5000;

                if(inTheNorthGroup) {

                    currentProperty.Color=accessory.Data.DefaultSafeColor;

                }

                else {
                    
                    currentProperty.Color=accessory.Data.DefaultDangerColor;

                }
                
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperty);
                
                currentProperty=accessory.Data.GetDefaultDrawProperties();
                                        
                currentProperty.Name="Phase1_Stack_Range_Of_Turn_Of_The_Heavens_光轮召唤分摊范围";
                currentProperty.Scale=new(6);
                currentProperty.Owner=accessory.Data.PartyList[lowPriorityStack];
                currentProperty.Delay=6000;
                currentProperty.DestoryAt=5000;

                if(inTheNorthGroup) {

                    currentProperty.Color=accessory.Data.DefaultDangerColor;

                }

                else {
                    
                    currentProperty.Color=accessory.Data.DefaultSafeColor;

                }
                
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperty);
                
            }
            
        }
        
        [ScriptMethod(name: "P1_转轮召_击退处理位置", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(40152)$"])]
        public void P1_转轮召_击退处理位置(Event @event, ScriptAccessory accessory)
        {
            //dy 7
            if (parse != 1d) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            var pos= JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            if (MathF.Abs(pos.Z - 100) > 1) return;
            
            var atEast = pos.X - 100 > 1;
            var o1= P1转轮召抓人.IndexOf(1);
            var o2 = P1转轮召抓人.LastIndexOf(1);
            List<int> upGroup = [];
            if (Phase1_Group_Of_Turn_Of_The_Heavens==Phase1_Groups_Of_Turn_Of_The_Heavens.MT_OT_H1_H2_Go_North_MT_ST_H1_H2去北)
            {
                upGroup.Add(o1);
                if (o1 != 1 && o2 != 1) upGroup.Add(1);
                if (o1 != 2 && o2 != 2) upGroup.Add(2);
                if (o1 != 3 && o2 != 3) upGroup.Add(3);
                if (upGroup.Count < 4 && o1 != 0 && o2 != 0) upGroup.Add(0);
                if (upGroup.Count < 4 && o1 != 4 && o2 != 4) upGroup.Add(4);
            }
            if (Phase1_Group_Of_Turn_Of_The_Heavens == Phase1_Groups_Of_Turn_Of_The_Heavens.MT_H1_M1_R1_Go_North_MT_H1_D1_D3去北)
            {
                upGroup.Add(o1);
                if (o1 != 2 && o2 != 2) upGroup.Add(2);
                if (o1 != 4 && o2 != 4) upGroup.Add(4);
                if (o1 != 6 && o2 != 6) upGroup.Add(6);
                if (upGroup.Count < 4 && o1 != 0 && o2 != 0) upGroup.Add(0);
                if (upGroup.Count < 4 && o1 != 1 && o2 != 1) upGroup.Add(1);
            }
            if (Phase1_Group_Of_Turn_Of_The_Heavens == Phase1_Groups_Of_Turn_Of_The_Heavens.MT_OT_R1_R2_Go_North_MT_ST_D3_D4去北)
            {
                /*
                List<int> upIndex = [0, 1, 6, 7];
                if (upIndex.Contains(o1) && !upIndex.Contains(o2)) upGroup.Add(o1);
                if (upIndex.Contains(o2) && !upIndex.Contains(o1)) upGroup.Add(o2);
                if (upIndex.Contains(o1) && !upIndex.Contains(o2))
                {
                    if (upIndex.IndexOf(o1)<upIndex.IndexOf(o2))
                    {
                        upGroup.Add(o1);
                    }
                    else
                    {
                        upGroup.Add(o2);
                    }
                }
                var up0 = upGroup[0];
                var down0 = up0 == o1 ? o2 : o1;
                if (up0 != 1 && down0 != 1) upGroup.Add(1);
                if (up0 != 6 && down0 != 6) upGroup.Add(6);
                if (up0 != 7 && down0 != 7) upGroup.Add(7);
                if (upGroup.Count < 4 && up0 != 0 && down0 != 0) upGroup.Add(0);
                if (upGroup.Count < 4 && up0 != 4 && down0 != 4) upGroup.Add(4);
                */
                
                // After carefully examining the above algorithm, Cicero may consider that it's wrong.
                // Even if it's correct, it may contain a bunch of redundant steps.
                // Therefore, Cicero just simply decided to comment it out and rework a new algorithm similar to that of other groups.
                
                upGroup.Add(o1);
                if (o1 != 1 && o2 != 1) upGroup.Add(1);
                if (o1 != 6 && o2 != 6) upGroup.Add(6);
                if (o1 != 7 && o2 != 7) upGroup.Add(7);
                if (upGroup.Count < 4 && o1 != 0 && o2 != 0) upGroup.Add(0);
                if (upGroup.Count < 4 && o1 != 4 && o2 != 4) upGroup.Add(4);
            }

            var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            var dealpos1 = new Vector3(atEast ? 105.5f : 94.5f, 0, upGroup.Contains(myindex) ? 93 : 107);
            var dealpos2 = new Vector3(atEast ? 102 : 98, 0, upGroup.Contains(myindex) ? 93 : 107);
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_转轮召_击退处理位置1";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos1;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 4000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_转轮召_击退处理位置2";
            dp.Scale = new(2);
            dp.Position = dealpos1;
            dp.TargetPosition = dealpos2;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 4000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_转轮召_击退处理位置3";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos2;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.Delay = 4000;
            dp.DestoryAt = 2000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);


        }

        [ScriptMethod(name: "P1_四连线_清除连线记录器", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(40170)$"],userControl:false)]
        public void P1_四连线_清除连线记录器(Event @event, ScriptAccessory accessory)
        {
            
            if (parse != 1d) return;
            P1四连线.Clear();
            P1四连线开始 =true;
            if (Phase1_Mark_Players_Were_Tethered)
                accessory.Method.MarkClear();
        }
        [ScriptMethod(name: "P1_四连线_连线记录器", eventType: EventTypeEnum.Tether, eventCondition: ["Id:regex:^(00F9|011F)$"],userControl:false)]
        public void P1_四连线_连线记录器(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var index= accessory.Data.PartyList.IndexOf(((uint)tid));
            var id = @event["Id"] == "00F9" ? 10 : 20;
            P1四连线.Add(id + index);
        }
        [ScriptMethod(name: "P1_四连线_头顶标记", eventType: EventTypeEnum.Tether, eventCondition: ["Id:regex:^(00F9|011F)$"],userControl:false)]
        public void P1_四连线_头顶标记(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;
            if (!Phase1_Mark_Players_Were_Tethered) return;
            if (!P1四连线开始) return;
            Task.Delay(250).ContinueWith(t =>
            {
                var index = P1四连线.Last() % 10;
                accessory.Method.Mark(accessory.Data.PartyList[index], (KodakkuAssist.Module.GameOperate.MarkType)P1四连线.Count);
                //accessory.Log.Debug($"{index} {(KodakkuAssist.Module.GameOperate.MarkType)P1四连线.Count}");
            });
        }
        [ScriptMethod(name: "P1_四连线_处理位置", eventType: EventTypeEnum.Tether, eventCondition: ["Id:regex:^(00F9|011F)$"])]
        public void P1_四连线_处理位置(Event @event, ScriptAccessory accessory)
        {
            if (!P1四连线开始) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var dis = 2.5f;//距离点名人
            var far = 5f;//距离boss
            Task.Delay(250).ContinueWith(t =>
            {
                var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                Vector3 t1p1 = new(100, 0, 100 - far);
                Vector3 t1p2 = new(100, 0, 100-far-dis);
                Vector3 t2p1 = new(100, 0, 100 + far);
                Vector3 t2p2 = new(100, 0, 100 + far + dis);
                Vector3 t3p1 = new(100, 0, 100 - far - dis);
                Vector3 t3p2 = new(100, 0, 100 - far);
                Vector3 t4p1 = new(100, 0, 100 + far + dis);
                Vector3 t4p2 = new(100, 0, 100 + far);
                
                if (P1四连线.Count ==1 && tid==accessory.Data.Me)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_四连线_线1处理位置1";
                    dp.Scale = new(2);
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = t1p1;
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.DestoryAt = 13000;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_四连线_线1处理位置2";
                    dp.Scale = new(2);
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = t1p2;
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.Delay =13000;
                    dp.DestoryAt = 6000;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                }
                if (P1四连线.Count == 2 && tid == accessory.Data.Me)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_四连线_线2处理位置1";
                    dp.Scale = new(2);
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = t2p1;
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.DestoryAt = 13500;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_四连线_线2处理位置2";
                    dp.Scale = new(2);
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = t2p2;
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.Delay = 13500;
                    dp.DestoryAt = 5000;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                }
                if (P1四连线.Count == 3 && tid == accessory.Data.Me)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_四连线_线3处理位置1";
                    dp.Scale = new(2);
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = t3p1;
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.DestoryAt = 7500;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_四连线_线3处理位置2";
                    dp.Scale = new(3);
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = t3p2;
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.Delay = 7500;
                    dp.DestoryAt = 6000;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                }
                if (P1四连线.Count == 4 && tid == accessory.Data.Me)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_四连线_线4处理位置1";
                    dp.Scale = new(2);
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = t4p1;
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.DestoryAt = 8500;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_四连线_线4处理位置2";
                    dp.Scale = new(3);
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = t4p2;
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.Delay = 8500;
                    dp.DestoryAt = 5000;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                }
                if (P1四连线.Count == 4)
                {
                    var tehterObjIndex = P1四连线.Select(o => o % 10).ToList();
                    var tehterIsFire = P1四连线.Select(o => o < 20).ToList();
                    List<int> idleObjIndex = [];
                    if (Phase1_Strat_Of_Fall_Of_Faith==Phase1_Strats_Of_Fall_Of_Faith.Single_Line_In_THD_Order_按THD顺序单排)
                    {
                        for (int i = 0; i < accessory.Data.PartyList.Count; i++)
                        {
                            if (!tehterObjIndex.Contains(i))
                            { idleObjIndex.Add(i); }
                        }
                    }
                    
                    if(Phase1_Strat_Of_Fall_Of_Faith==Phase1_Strats_Of_Fall_Of_Faith.Single_Line_In_HTD_Order_按HTD顺序单排) {
                        // The addition of this strat credits to @alexandria_prime. Appreciate!
                        
                        List<int> htdOrder=new List<int>{2,3,0,1,4,5,6,7};
                        
                        for(int i=0;i<htdOrder.Count;++i) {

                            if(!tehterObjIndex.Contains(htdOrder[i])) {
                                
                                idleObjIndex.Add(htdOrder[i]);
                                
                            }
                            
                        }
                        
                    }
                    
                    if(Phase1_Strat_Of_Fall_Of_Faith==Phase1_Strats_Of_Fall_Of_Faith.Single_Line_In_H1TDH2_Order_按H1TDH2顺序单排) {
                        // The addition of this strat credits to @alexandria_prime. Appreciate!
                        
                        List<int> h1tdh2Order=new List<int>{2,0,1,4,5,6,7,3};
                        
                        for(int i=0;i<h1tdh2Order.Count;++i) {

                            if(!tehterObjIndex.Contains(h1tdh2Order[i])) {
                                
                                idleObjIndex.Add(h1tdh2Order[i]);
                                
                            }
                            
                        }
                        
                    }
                    
                    if(Phase1_Strat_Of_Fall_Of_Faith==Phase1_Strats_Of_Fall_Of_Faith.Double_Lines_H12MOT_Left_M12R12_Right_双排左H12MST右D1234)
                    {
                        List<int> group1 = [1, 0, 3, 2];
                        List<int> group2 = [4, 5, 6, 7];
                        group1.RemoveAll(x => tehterObjIndex.Contains(x));
                        while (group1.Count>2)
                        {
                            var m = group1.First();
                            group1.Remove(m);
                            group2.Insert(0,m);
                        }
                        idleObjIndex.AddRange(group1);
                        idleObjIndex.AddRange(group2);
                    }
                    
                    if(Phase1_Strat_Of_Fall_Of_Faith==Phase1_Strats_Of_Fall_Of_Faith.Double_Lines_MOTH12_Left_M12R12_Right_双排左MSTH12右D1234)
                    {
                        List<int> group1 = [3, 2, 1, 0];
                        List<int> group2 = [4, 5, 6, 7];
                        group1.RemoveAll(x => tehterObjIndex.Contains(x));
                        while (group1.Count>2)
                        {
                            var m = group1.First();
                            group1.Remove(m);
                            group2.Insert(0,m);
                        }
                        idleObjIndex.AddRange(group1);
                        idleObjIndex.AddRange(group2);
                    }
                    
                    if (!idleObjIndex.Contains(myindex)) return;
                    
                    Vector3 i1p1=new Vector3(100,0,100);
                    Vector3 i1p2=new Vector3(100,0,100);
                    Vector3 i2p1=new Vector3(100,0,100);
                    Vector3 i2p2=new Vector3(100,0,100);
                    Vector3 i3p1=new Vector3(100,0,100);
                    Vector3 i3p2=new Vector3(100,0,100);
                    Vector3 i4p1=new Vector3(100,0,100);
                    Vector3 i4p2=new Vector3(100,0,100);
                    Vector3 dealpos1=default;
                    Vector3 dealpos2=default;

                    if(Phase1_Benchmark_During_Fall_Of_Faith==Phase1_Benchmarks_During_Fall_Of_Faith.Face_True_North_面向正北) {
                        
                        i1p1=tehterIsFire[0]?new(100,0,100-far-dis):new(100-dis,0,100-far);
                        i1p2=tehterIsFire[2]?new(100,0,100-far-dis):new(100-dis,0,100-far);
                        i2p1=tehterIsFire[0]?new(100,0,100-far-dis):new(100+dis,0,100-far);
                        i2p2=tehterIsFire[2]?new(100,0,100-far-dis):new(100+dis,0,100-far);
                        i3p1=tehterIsFire[1]?new(100,0,100+far+dis):new(100-dis,0,100+far);
                        i3p2=tehterIsFire[3]?new(100,0,100+far+dis):new(100-dis,0,100+far);
                        i4p1=tehterIsFire[1]?new(100,0,100+far+dis):new(100+dis,0,100+far);
                        i4p2=tehterIsFire[3]?new(100,0,100+far+dis):new(100+dis,0,100+far);
                        
                    }
                    
                    if(Phase1_Benchmark_During_Fall_Of_Faith==Phase1_Benchmarks_During_Fall_Of_Faith.Face_The_Boss_面向Boss) {
                        // The addition of this benchmark credits to @alexandria_prime. Appreciate!
                        
                        i1p1=tehterIsFire[0]?new(100,0,100-far-dis):new(100+dis,0,100-far);
                        i1p2=tehterIsFire[2]?new(100,0,100-far-dis):new(100+dis,0,100-far);
                        i2p1=tehterIsFire[0]?new(100,0,100-far-dis):new(100-dis,0,100-far);
                        i2p2=tehterIsFire[2]?new(100,0,100-far-dis):new(100-dis,0,100-far);
                        i3p1=tehterIsFire[1]?new(100,0,100+far+dis):new(100-dis,0,100+far);
                        i3p2=tehterIsFire[3]?new(100,0,100+far+dis):new(100-dis,0,100+far);
                        i4p1=tehterIsFire[1]?new(100,0,100+far+dis):new(100+dis,0,100+far);
                        i4p2=tehterIsFire[3]?new(100,0,100+far+dis):new(100+dis,0,100+far);
                        
                    }

                    if(i1p1.Equals(new Vector3(100,0,100))) {

                        return;

                    }

                    dealpos1 = idleObjIndex.IndexOf(myindex) switch
                    {
                        0 => i1p1,
                        1 => i2p1,
                        2 => i3p1,
                        3 => i4p1,
                    };
                    dealpos2 = idleObjIndex.IndexOf(myindex) switch
                    {
                        0 => i1p2,
                        1 => i2p2,
                        2 => i3p2,
                        3 => i4p2,
                    };
                    var upgroup = (idleObjIndex.IndexOf(myindex) == 0 || idleObjIndex.IndexOf(myindex) == 1);

                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_四连线_处理位置1";
                    dp.Scale = new(2);
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = dealpos1;
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.DestoryAt = upgroup ? 5000 : 8500;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_四连线_处理位置2";
                    dp.Scale = new(2);
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = dealpos2;
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.Delay = upgroup ? 5000 : 8500;
                    dp.DestoryAt = upgroup ? 6000 : 5000;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                }
            });
        }

        [ScriptMethod(name: "P1_塔_塔记录器", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(4012[234567]|4013[15])$"], userControl: false)]
        public void P1_塔_塔记录器(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;
            lock (this)
            {
                var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
                var count = @event["ActionId"] switch
                {
                    "40135" => 1,
                    "40131" => 1,
                    "40122" => 2,
                    "40123" => 3,
                    "40124" => 4,
                    "40125" => 2,
                    "40126" => 3,
                    "40127" => 4,
                };
                if (MathF.Abs(pos.Z - 100) < 1)
                {
                    P1塔[1] = count;
                }
                else
                {
                    if (pos.Z - 100 > 1) P1塔[2] = count;
                    else P1塔[0] = count;
                }
                if (pos.X - 100 > 1)
                {
                    P1塔[3] = 1;
                }
            }
        }
        
        [ScriptMethod(name:"Phase1_Burnt_Strike_With_Towers_And_Tank_Busters_带有塔和死刑的燃烧击(最后机制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(40134|40129)$"])]
        
        public void Phase1_Burnt_Strike_With_Towers_And_Tank_Busters_带有塔和死刑的燃烧击(Event @event, ScriptAccessory accessory) {

            if(parse!=1d) {
                
                return;
                
            }

            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            if(@event["ActionId"].Equals("40134")) {
                // Thunder Burnt Strike.
                
                var currentProperty=accessory.Data.GetDefaultDrawProperties();

                currentProperty.Name="Phase1_Second_Strike_Of_Thunder_Burnt_Strike_雷燃烧击第二击";
                currentProperty.Scale=new(20,40);
                currentProperty.Owner=sourceId;
                currentProperty.Color=Phase1_Colour_Of_Burnt_Strike_Characteristics.V4.WithW(1f);
                currentProperty.DestoryAt=8200;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperty);
            
                currentProperty=accessory.Data.GetDefaultDrawProperties();
            
                currentProperty.Name="Phase1_First_Strike_Of_Thunder_Burnt_Strike_雷燃烧击第一击";
                currentProperty.Scale=new(10,40);
                currentProperty.Owner=sourceId;
                currentProperty.Color=accessory.Data.DefaultDangerColor.WithW(3f);
                currentProperty.DestoryAt=6500;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperty);

            }
            
            if(@event["ActionId"].Equals("40129")) {
                // Fire Burnt Strike.
                
                var currentProperty=accessory.Data.GetDefaultDrawProperties();
            
                currentProperty.Name="Phase1_First_Strike_Of_Fire_Burnt_Strike_火燃烧击第一击";
                currentProperty.Scale=new(10,40);
                currentProperty.Owner=sourceId;
                currentProperty.Color=accessory.Data.DefaultDangerColor.WithW(3f);
                currentProperty.DestoryAt=6500;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperty);
            
                currentProperty=accessory.Data.GetDefaultDrawProperties();
            
                currentProperty.Name="Phase1_Central_Axis_Of_Fire_Burnt_Strike_火燃烧击中轴线";
                currentProperty.Scale=new(0.5f,40f);
                currentProperty.Owner=sourceId;
                currentProperty.Color=Phase1_Colour_Of_Burnt_Strike_Characteristics.V4.WithW(25f);
                currentProperty.DestoryAt=8200;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperty);

                for(int i=-1;i<=1;++i) {
                
                    currentProperty=accessory.Data.GetDefaultDrawProperties();
            
                    currentProperty.Name="Phase1_Knockback_Direction_Of_Fire_Burnt_Strike_火燃烧击击退方向";
                    currentProperty.Scale=new(2f,7f);
                    currentProperty.Owner=sourceId;
                    currentProperty.Color=Phase1_Colour_Of_Burnt_Strike_Characteristics.V4.WithW(1f);
                    currentProperty.Offset=new Vector3(0,0,-(i*10));
                    currentProperty.Rotation=float.Pi/2;
                    currentProperty.DestoryAt=8200;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperty);
                
                    currentProperty=accessory.Data.GetDefaultDrawProperties();
            
                    currentProperty.Name="Phase1_Knockback_Direction_Of_Fire_Burnt_Strike_火燃烧击击退方向";
                    currentProperty.Scale=new(2f,7f);
                    currentProperty.Owner=sourceId;
                    currentProperty.Color=Phase1_Colour_Of_Burnt_Strike_Characteristics.V4.WithW(1f);
                    currentProperty.Offset=new Vector3(0,0,-(i*10));
                    currentProperty.Rotation=-(float.Pi/2);
                    currentProperty.DestoryAt=8200;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperty);
                
                }
                
            }

        }
        
        [ScriptMethod(name: "P1_塔_塔处理位置", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(40134|40129)$"])]
        public void P1_塔_塔处理位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1d) return;
            Task.Delay(250).ContinueWith(t =>
            {
                var myIndex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                if (@event["ActionId"] == "40134")
                {
                    var eastTower = P1塔[3] == 1;
                    //雷
                    if (myIndex==0|| myIndex==1)
                    {
                        var dx = eastTower ? -10.5f : 10.5f;
                        var dy = myIndex == 0 ? -5.5f : 5.5f;
                        // The expression was myindex == 1 ? -5.5f : 5.5f before. Obviously it reverses the situation of MT and OT.
                        // The bug fix here credits to @alexandria_prime. Appreciate!
                        var dp = accessory.Data.GetDefaultDrawProperties();
                        dp.Name = "P1_雷塔_塔处理位置_T";
                        dp.Scale = new(2);
                        dp.Owner = accessory.Data.Me;
                        dp.TargetPosition = new(100+dx,0,100+dy);
                        dp.ScaleMode |= ScaleMode.YByDistance;
                        dp.Color = accessory.Data.DefaultSafeColor;
                        dp.DestoryAt = 10500;
                        accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                    }
                    else
                    {
                        int myTowerIndex=myIndex-1;
                        Vector3 standbyPosition=new Vector3(100,0,100);
                        Vector3 towerPosition=new Vector3(100,0,100);
                        
                        if(Phase1_Strat_Of_Towers==Phase1_Strats_Of_Towers.Purely_Based_On_The_Priority_纯粹根据优先级) {
                            
                            if (myTowerIndex > 0 && myTowerIndex <= P1塔[0]) standbyPosition = new(eastTower ? 113.08f : 86.92f, 0, 90.81f);
                            if (myTowerIndex > P1塔[0] && myTowerIndex <= P1塔[0]+ P1塔[1]) standbyPosition = new(eastTower ? 115.98f : 84.02f, 0, 100f);
                            if (myTowerIndex > P1塔[0] + P1塔[1] && myTowerIndex <= P1塔[0] + P1塔[1]+ P1塔[2]) standbyPosition = new(eastTower ? 113.08f : 86.92f, 0, 109.18f);

                            if (myTowerIndex > 0 && myTowerIndex <= P1塔[0]) towerPosition = new(eastTower ? 113.08f : 86.92f, 0, 90.81f);
                            if (myTowerIndex > P1塔[0] && myTowerIndex <= P1塔[0] + P1塔[1]) towerPosition = new(eastTower ? 115.98f : 84.02f, 0, 100f);
                            if (myTowerIndex > P1塔[0] + P1塔[1] && myTowerIndex <= P1塔[0] + P1塔[1] + P1塔[2]) towerPosition = new(eastTower ? 113.08f : 86.92f, 0, 109.18f);
                            
                        }

                        if(Phase1_Strat_Of_Towers==Phase1_Strats_Of_Towers.Fixed_H1_H2_R2_Priority_For_The_Rest_固定H1_H2_D4剩余人优先级) {

                            bool fixedPartyMember=false;

                            if(myIndex==2) {
                                
                                fixedPartyMember=true;

                                standbyPosition=new(eastTower?113.08f:86.92f,0,90.81f);
                                towerPosition=new(eastTower?113.08f:86.92f,0,90.81f);

                            }
                            
                            if(myIndex==3) {
                                
                                fixedPartyMember=true;

                                standbyPosition=new(eastTower?115.98f:84.02f,0,100f);
                                towerPosition=new(eastTower?115.98f:84.02f,0,100f);

                            }

                            if(myIndex==7) {
                                
                                fixedPartyMember=true;
                                
                                standbyPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                towerPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                
                            }

                            if(!fixedPartyMember) {
                                
                                int newTower0=P1塔[0]-1;
                                int newTower1=P1塔[1]-1;
                                int newTower2=P1塔[2]-1;
                                int myNewTowerIndex=myIndex-3;
                                
                                if(Enable_Developer_Mode) {

                                    accessory.Method.SendChat($"""
                                                               /e 
                                                               newTower0={newTower0}
                                                               newTower1={newTower1}
                                                               newTower2={newTower2}
                                                               myNewTowerIndex={myNewTowerIndex}
                                                               
                                                               """);

                                }

                                if(newTower0>0
                                    &&
                                    0<myNewTowerIndex&&myNewTowerIndex<=newTower0) {
                                    
                                    standbyPosition=new(eastTower?113.08f:86.92f,0,90.81f);
                                    towerPosition=new(eastTower?113.08f:86.92f,0,90.81f);
                                    
                                }
                                
                                if(newTower1>0
                                   &&
                                   newTower0<myNewTowerIndex&&myNewTowerIndex<=newTower0+newTower1) {
                                    
                                    standbyPosition=new(eastTower?115.98f:84.02f,0,100f);
                                    towerPosition=new(eastTower?115.98f:84.02f,0,100f);
                                    
                                }
                                
                                if(newTower2>0
                                   &&
                                   newTower0+newTower1<myNewTowerIndex&&myNewTowerIndex<=newTower0+newTower1+newTower2) {
                                    
                                    standbyPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                    towerPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                    
                                }
                                
                            }

                        }

                        if(Phase1_Strat_Of_Towers==Phase1_Strats_Of_Towers.Fixed_H1_H2_R2_The_Rest_Fill_Vacancies_固定H1_H2_D4剩余人补位) {
                            // The algorithm implementation of this strat was inspired by @abigseal's script.
                            // Therefore, the following code should credit to him. Appreciate!
                            
                            bool fixedPartyMember=false;

                            if(myIndex==2) {
                                
                                fixedPartyMember=true;

                                standbyPosition=new(eastTower?113.08f:86.92f,0,90.81f);
                                towerPosition=new(eastTower?113.08f:86.92f,0,90.81f);

                            }
                            
                            if(myIndex==3) {
                                
                                fixedPartyMember=true;

                                standbyPosition=new(eastTower?115.98f:84.02f,0,100f);
                                towerPosition=new(eastTower?115.98f:84.02f,0,100f);

                            }

                            if(myIndex==7) {
                                
                                fixedPartyMember=true;
                                
                                standbyPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                towerPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                
                            }

                            if(!fixedPartyMember) {
                                
                                if(Enable_Developer_Mode) {

                                    accessory.Method.SendChat($"""
                                                               /e 
                                                               P1塔[0]={P1塔[0]}
                                                               P1塔[1]={P1塔[1]}
                                                               P1塔[2]={P1塔[2]}
                                                               myTowerIndex={myTowerIndex}

                                                               """);

                                }

                                if(myIndex==4) {

                                    if(P1塔[0]>=2) {
                                        
                                        standbyPosition=new(eastTower?113.08f:86.92f,0,90.81f);
                                        towerPosition=new(eastTower?113.08f:86.92f,0,90.81f);
                                        
                                    }

                                    else {

                                        if(P1塔[1]>=3) {
                                            
                                            standbyPosition=new(eastTower?115.98f:84.02f,0,100f);
                                            towerPosition=new(eastTower?115.98f:84.02f,0,100f);
                                            
                                        }
                                        
                                        if(P1塔[2]>=3) {
                                            
                                            standbyPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                            towerPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                            
                                        }
                                        
                                    }
                                    
                                }
                                
                                if(myIndex==5) {

                                    if(P1塔[1]>=2) {
                                        
                                        standbyPosition=new(eastTower?115.98f:84.02f,0,100f);
                                        towerPosition=new(eastTower?115.98f:84.02f,0,100f);
                                        
                                    }

                                    else {
                                        
                                        if(P1塔[0]>=3) {
                                            
                                            standbyPosition=new(eastTower?113.08f:86.92f,0,90.81f);
                                            towerPosition=new(eastTower?113.08f:86.92f,0,90.81f);
                                            
                                        }
                                        
                                        if(P1塔[2]>=3) {
                                            
                                            standbyPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                            towerPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                            
                                        }
                                        
                                    }
                                    
                                }
                                
                                if(myIndex==6) {

                                    if(P1塔[2]>=2) {
                                        
                                        standbyPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                        towerPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                        
                                    }

                                    else {
                                        
                                        if(P1塔[0]>=3) {
                                            
                                            standbyPosition=new(eastTower?113.08f:86.92f,0,90.81f);
                                            towerPosition=new(eastTower?113.08f:86.92f,0,90.81f);
                                            
                                        }
                                        
                                        if(P1塔[1]>=3) {
                                            
                                            standbyPosition=new(eastTower?115.98f:84.02f,0,100f);
                                            towerPosition=new(eastTower?115.98f:84.02f,0,100f);
                                            
                                        }
                                        
                                    }
                                    
                                }
                                
                            }
                            
                        }
                        
                        if(Enable_Developer_Mode) {

                            accessory.Method.SendChat($"""
                                                       /e 
                                                       standbyPosition={standbyPosition}
                                                       towerPosition={towerPosition}

                                                       """);

                        }

                        if(standbyPosition.Equals(new Vector3(100,0,100))||towerPosition.Equals(new Vector3(100,0,100))) {

                            return;

                        }

                        var dp = accessory.Data.GetDefaultDrawProperties();
                        dp.Name = "P1_雷塔_塔处理位置_ND";
                        dp.Scale = new(2);
                        dp.Owner = accessory.Data.Me;
                        dp.TargetPosition = standbyPosition;
                        dp.ScaleMode |= ScaleMode.YByDistance;
                        dp.Color = accessory.Data.DefaultSafeColor;
                        dp.DestoryAt = 10500;
                        accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                        dp = accessory.Data.GetDefaultDrawProperties();
                        dp.Name = "P1_雷塔_塔_ND";
                        dp.Scale = new(4);
                        dp.Position = towerPosition;
                        dp.Color = accessory.Data.DefaultSafeColor;
                        dp.DestoryAt = 10500;
                        accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Circle, dp);

                    }
                }
                else
                {
                    var eastTower = P1塔[3] == 1;
                    //火
                    if (myIndex == 0 || myIndex == 1)
                    {
                        var dx2 = eastTower ? -2f : 2f;
                        var dx1 = eastTower ? -5.5f : 5.5f;
                        var dy = myIndex == 0 ? -5.5f : 5.5f;
                        // The expression was myindex == 1 ? -5.5f : 5.5f before. Same background as the previous one.
                        // The bug fix here credits to @alexandria_prime. Appreciate!

                        var dp = accessory.Data.GetDefaultDrawProperties();
                        dp.Name = "P1_火塔_塔处理位置_T1";
                        // The name of the drawing here was once incorrectly labeled as towers with thunder Burnt Strike.
                        // Same situation for the other four names below.
                        // The corrections credit to @alexandria_prime. Appreciate!
                        dp.Scale = new(2);
                        dp.Owner = accessory.Data.Me;
                        dp.TargetPosition = new(100 + dx1, 0, 100 + dy);
                        dp.ScaleMode |= ScaleMode.YByDistance;
                        dp.Color = accessory.Data.DefaultSafeColor;
                        dp.DestoryAt = 6500;
                        accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                        dp = accessory.Data.GetDefaultDrawProperties();
                        dp.Name = "P1_火塔_塔处理位置_T2";
                        dp.Scale = new(2);
                        dp.Position = new(100 + dx1, 0, 100 + dy);
                        dp.TargetPosition = new(100 + dx2, 0, 100 + dy);
                        dp.ScaleMode |= ScaleMode.YByDistance;
                        dp.Color = accessory.Data.DefaultSafeColor;
                        dp.DestoryAt = 6500;
                        accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                        dp = accessory.Data.GetDefaultDrawProperties();
                        dp.Name = "P1_火塔_塔处理位置_T3";
                        dp.Scale = new(2);
                        dp.Owner = accessory.Data.Me;
                        dp.TargetPosition = new(100 + dx2, 0, 100 + dy);
                        dp.ScaleMode |= ScaleMode.YByDistance;
                        dp.Color = accessory.Data.DefaultSafeColor;
                        dp.Delay = 6500;
                        dp.DestoryAt = 1700;
                        accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                    }
                    else
                    {
                        var myTowerIndex=myIndex-1;
                        Vector3 standbyPosition=new Vector3(100,0,100);
                        Vector3 towerPosition=new Vector3(100,0,100);

                        if(Phase1_Strat_Of_Towers==Phase1_Strats_Of_Towers.Purely_Based_On_The_Priority_纯粹根据优先级) {
                            
                            if (myTowerIndex > 0 && myTowerIndex <= P1塔[0]) standbyPosition = new(eastTower ? 102f : 98f, 0, 90.81f);
                            if (myTowerIndex > P1塔[0] && myTowerIndex <= P1塔[0] + P1塔[1]) standbyPosition = new(eastTower ? 102f : 98f, 0, 100f);
                            if (myTowerIndex > P1塔[0] + P1塔[1] && myTowerIndex <= P1塔[0] + P1塔[1] + P1塔[2]) standbyPosition = new(eastTower ? 102f : 98f, 0, 109.18f);

                            if (myTowerIndex > 0 && myTowerIndex <= P1塔[0]) towerPosition = new(eastTower ? 113.08f : 86.92f, 0, 90.81f);
                            if (myTowerIndex > P1塔[0] && myTowerIndex <= P1塔[0] + P1塔[1]) towerPosition = new(eastTower ? 115.98f : 84.02f, 0, 100f);
                            if (myTowerIndex > P1塔[0] + P1塔[1] && myTowerIndex <= P1塔[0] + P1塔[1] + P1塔[2]) towerPosition = new(eastTower ? 113.08f : 86.92f, 0, 109.18f);
                            
                        }

                        if(Phase1_Strat_Of_Towers==Phase1_Strats_Of_Towers.Fixed_H1_H2_R2_Priority_For_The_Rest_固定H1_H2_D4剩余人优先级) {

                            bool fixedPartyMember=false;

                            if(myIndex==2) {
                                
                                fixedPartyMember=true;

                                standbyPosition=new(eastTower?102f:98f,0,90.81f);
                                towerPosition=new(eastTower?113.08f:86.92f,0,90.81f);

                            }
                            
                            if(myIndex==3) {
                                
                                fixedPartyMember=true;

                                standbyPosition=new(eastTower?102f:98f,0,100f);
                                towerPosition=new(eastTower?115.98f:84.02f,0,100f);

                            }

                            if(myIndex==7) {
                                
                                fixedPartyMember=true;
                                
                                standbyPosition=new(eastTower?102f:98f,0,109.18f);
                                towerPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                
                            }

                            if(!fixedPartyMember) {
                                
                                int newTower0=P1塔[0]-1;
                                int newTower1=P1塔[1]-1;
                                int newTower2=P1塔[2]-1;
                                int myNewTowerIndex=myIndex-3;
                                
                                if(Enable_Developer_Mode) {

                                    accessory.Method.SendChat($"""
                                                               /e 
                                                               newTower0={newTower0}
                                                               newTower1={newTower1}
                                                               newTower2={newTower2}
                                                               myNewTowerIndex={myNewTowerIndex}

                                                               """);

                                }

                                if(newTower0>0
                                    &&
                                    0<myNewTowerIndex&&myNewTowerIndex<=newTower0) {
                                    
                                    standbyPosition=new(eastTower?102f:98f,0,90.81f);
                                    towerPosition=new(eastTower?113.08f:86.92f,0,90.81f);
                                    
                                }
                                
                                if(newTower1>0
                                   &&
                                   newTower0<myNewTowerIndex&&myNewTowerIndex<=newTower0+newTower1) {
                                    
                                    standbyPosition=new(eastTower?102f:98f,0,100f);
                                    towerPosition=new(eastTower?115.98f:84.02f,0,100f);
                                    
                                }
                                
                                if(newTower2>0
                                   &&
                                   newTower0+newTower1<myNewTowerIndex&&myNewTowerIndex<=newTower0+newTower1+newTower2) {
                                    
                                    standbyPosition=new(eastTower?102f:98f,0,109.18f);
                                    towerPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                    
                                }
                                
                            }

                        }

                        if(Phase1_Strat_Of_Towers==Phase1_Strats_Of_Towers.Fixed_H1_H2_R2_The_Rest_Fill_Vacancies_固定H1_H2_D4剩余人补位) {
                            // Same as before, the following credits to @abigseal. Appreciate!
                            
                            bool fixedPartyMember=false;

                            if(myIndex==2) {
                                
                                fixedPartyMember=true;

                                standbyPosition=new(eastTower?102f:98f,0,90.81f);
                                towerPosition=new(eastTower?113.08f:86.92f,0,90.81f);

                            }
                            
                            if(myIndex==3) {
                                
                                fixedPartyMember=true;

                                standbyPosition=new(eastTower?102f:98f,0,100f);
                                towerPosition=new(eastTower?115.98f:84.02f,0,100f);

                            }

                            if(myIndex==7) {
                                
                                fixedPartyMember=true;
                                
                                standbyPosition=new(eastTower?102f:98f,0,109.18f);
                                towerPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                
                            }

                            if(!fixedPartyMember) {
                                
                                if(Enable_Developer_Mode) {

                                    accessory.Method.SendChat($"""
                                                               /e 
                                                               P1塔[0]={P1塔[0]}
                                                               P1塔[1]={P1塔[1]}
                                                               P1塔[2]={P1塔[2]}
                                                               myTowerIndex={myTowerIndex}

                                                               """);

                                }

                                if(myIndex==4) {

                                    if(P1塔[0]>=2) {
                                        
                                        standbyPosition=new(eastTower?102f:98f,0,90.81f);
                                        towerPosition=new(eastTower?113.08f:86.92f,0,90.81f);
                                        
                                    }

                                    else {

                                        if(P1塔[1]>=3) {
                                            
                                            standbyPosition=new(eastTower?102f:98f,0,100f);
                                            towerPosition=new(eastTower?115.98f:84.02f,0,100f);
                                            
                                        }
                                        
                                        if(P1塔[2]>=3) {
                                            
                                            standbyPosition=new(eastTower?102f:98f,0,109.18f);
                                            towerPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                            
                                        }
                                        
                                    }
                                    
                                }
                                
                                if(myIndex==5) {

                                    if(P1塔[1]>=2) {
                                        
                                        standbyPosition=new(eastTower?102f:98f,0,100f);
                                        towerPosition=new(eastTower?115.98f:84.02f,0,100f);
                                        
                                    }

                                    else {
                                        
                                        if(P1塔[0]>=3) {
                                            
                                            standbyPosition=new(eastTower?102f:98f,0,90.81f);
                                            towerPosition=new(eastTower?113.08f:86.92f,0,90.81f);
                                            
                                        }
                                        
                                        if(P1塔[2]>=3) {
                                            
                                            standbyPosition=new(eastTower?102f:98f,0,109.18f);
                                            towerPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                            
                                        }
                                        
                                    }
                                    
                                }
                                
                                if(myIndex==6) {

                                    if(P1塔[2]>=2) {
                                        
                                        standbyPosition=new(eastTower?102f:98f,0,109.18f);
                                        towerPosition=new(eastTower?113.08f:86.92f,0,109.18f);
                                        
                                    }

                                    else {
                                        
                                        if(P1塔[0]>=3) {
                                            
                                            standbyPosition=new(eastTower?102f:98f,0,90.81f);
                                            towerPosition=new(eastTower?113.08f:86.92f,0,90.81f);
                                            
                                        }
                                        
                                        if(P1塔[1]>=3) {
                                            
                                            standbyPosition=new(eastTower?102f:98f,0,100f);
                                            towerPosition=new(eastTower?115.98f:84.02f,0,100f);
                                            
                                        }
                                        
                                    }
                                    
                                }
                                
                            }
                            
                        }
                        
                        if(Enable_Developer_Mode) {

                            accessory.Method.SendChat($"""
                                                       /e 
                                                       standbyPosition={standbyPosition}
                                                       towerPosition={towerPosition}

                                                       """);

                        }

                        if(standbyPosition.Equals(new Vector3(100,0,100))||towerPosition.Equals(new Vector3(100,0,100))) {

                            return;

                        }

                        var dp = accessory.Data.GetDefaultDrawProperties();
                        dp.Name = "P1_火塔_塔处理位置_ND";
                        dp.Scale = new(2);
                        dp.Owner = accessory.Data.Me;
                        dp.TargetPosition = standbyPosition;
                        dp.ScaleMode |= ScaleMode.YByDistance;
                        dp.Color = accessory.Data.DefaultSafeColor;
                        dp.DestoryAt = 9000;
                        accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                        dp = accessory.Data.GetDefaultDrawProperties();
                        dp.Name = "P1_火塔_塔_ND";
                        dp.Scale = new(4);
                        dp.Position = towerPosition;
                        dp.Color = accessory.Data.DefaultSafeColor;
                        dp.DestoryAt = 10500;
                        accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Circle, dp);

                    }
                }
            });
            
        }

        #endregion

        #region P2
        
        [ScriptMethod(name:"----- Phase 2 ----- (No actual meaning for this toggle/此开关无实际意义)",
            eventType:EventTypeEnum.NpcYell,
            eventCondition:["Your poor",
                            "给我你们贫穷的人"])]
        
        public void Phase2_Placeholder(Event @event, ScriptAccessory accessory) { }
        
        [ScriptMethod(name: "P2_换P", eventType: EventTypeEnum.Director, eventCondition: ["Instance:800375BF", "Command:8000001E"],userControl:false)]
        public void P2_换P(Event @event, ScriptAccessory accessory)
        {
            parse = 2d;
        }

        [ScriptMethod(name: "P2_钻石星尘_BossId记录", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(40180)$"], userControl: false)]
        public void P2_钻石星尘_BossId记录(Event @event, ScriptAccessory accessory)
        {
            parse = 2.1d;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            P2BossId = sid;
            P2DDIceDir.Clear();
        }
        [ScriptMethod(name: "P2_钻石星尘_钢铁月环记录", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^((4020[23]))$"],userControl: false)]
        public void P2_钻石星尘_钢铁月环记录(Event @event, ScriptAccessory accessory)
        {
            P2DDDircle = (@event["ActionId"] == "40202");//钢铁
        }
        [ScriptMethod(name: "P2_钻石星尘_钢铁月环", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^((4020[23]))$"])]
        public void P2_钻石星尘_钢铁月环(Event @event, ScriptAccessory accessory)
        {
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            if (@event["ActionId"]=="40202")//钢铁
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P2_钻石星尘_钢铁";
                dp.Scale = new(16);
                dp.Owner = sid;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.DestoryAt = 6000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
            }
            else
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P2_钻石星尘_月环";
                dp.Scale = new(20);
                dp.InnerScale = new(4);
                dp.Radian = float.Pi * 2;
                dp.Owner = sid;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.DestoryAt = 6000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);
            }
        }
        [ScriptMethod(name: "P2_钻石星尘_扇形引导", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^((4020[23]))$"])]
        public void P2_钻石星尘_扇形引导(Event @event, ScriptAccessory accessory)
        {
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            var dur = 3000;
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_钻石星尘_扇形引导1";
            dp.Scale = new(60);
            dp.Radian = float.Pi / 6;
            dp.Owner = sid;
            dp.TargetResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
            dp.TargetOrderIndex = 1;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 7000 - dur;
            dp.DestoryAt = dur;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_钻石星尘_扇形引导2";
            dp.Scale = new(60);
            dp.Radian = float.Pi / 6;
            dp.Owner = sid;
            dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
            dp.TargetOrderIndex = 2;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 7000 - dur;
            dp.DestoryAt = dur;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_钻石星尘_扇形引导3";
            dp.Scale = new(60);
            dp.Radian = float.Pi / 6;
            dp.Owner = sid;
            dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
            dp.TargetOrderIndex = 3;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 7000 - dur;
            dp.DestoryAt = dur;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_钻石星尘_扇形引导4";
            dp.Scale = new(60);
            dp.Radian = float.Pi / 6;
            dp.Owner = sid;
            dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
            dp.TargetOrderIndex = 4;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 7000 - dur;
            dp.DestoryAt = dur;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);


        }
        [ScriptMethod(name: "P2_钻石星尘_冰花放置位置", eventType: EventTypeEnum.TargetIcon)]
        public void P2_钻石星尘_冰花放置位置(Event @event, ScriptAccessory accessory)
        {
            //accessory.Log.Debug($"{ParsTargetIcon(@event["Id"])}");
            if (ParsTargetIcon(@event["Id"]) != 127) return;
            if (parse != 2.1) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            if (tid != accessory.Data.Me) return;
            var myIndex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            var rot = myIndex switch
            {
                0 => 6,
                1 => 0,
                2 => 4,
                3 => 2,
                4 => 4,
                5 => 2,
                6 => 6,
                7 => 0,
                _ => 0,
            };
            Vector3 epos1 = P2DDDircle ? new(119.5f, 0, 100.0f) : new(103.5f, 0, 100.0f);
            Vector3 epos2 = P2DDDircle ? new(119.5f, 0, 100.0f) : new(108.0f, 0, 100.0f);
            var dir8 = P2DDIceDir.FirstOrDefault() % 4;
            var dr = dir8 == 0 || dir8 == 2 ? -1 : 0;
            var dealpos1 = RotatePoint(epos1, new(100, 0, 100), float.Pi / 4 * (rot + dr));
            var dealpos2 = RotatePoint(epos2, new(100, 0, 100), float.Pi / 4 * (rot + dr));
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_钻石星尘_冰花放置位置1";
            dp.Scale = new(2);
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition= dealpos1;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 5500;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_钻石星尘_冰花放置位置3";
            dp.Scale = new(2);
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Position = dealpos1;
            dp.TargetPosition = dealpos2;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 5500;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_钻石星尘_冰花放置位置3";
            dp.Scale = new(2);
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos2;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.Delay = 5500;
            dp.DestoryAt = 2500;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        [ScriptMethod(name: "P2_钻石星尘_扇形引导位置", eventType: EventTypeEnum.TargetIcon)]
        public void P2_钻石星尘_扇形引导位置(Event @event, ScriptAccessory accessory)
        {
            //accessory.Log.Debug($"{ParsTargetIcon(@event["Id"])}");
            if (ParsTargetIcon(@event["Id"]) != 127) return;
            if (parse != 2.1) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var myIndex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            int[] group = [6, 7, 4, 5, 2, 3, 0, 1];
            if (accessory.Data.PartyList.IndexOf(((uint)tid)) != group[myIndex]) return;
            var rot = myIndex switch
            {
                0 => 6,
                1 => 0,
                2 => 4,
                3 => 2,
                4 => 4,
                5 => 2,
                6 => 6,
                7 => 0,
                _ => 0,
            };
            var dir8 = P2DDIceDir.FirstOrDefault() % 4;
            var dr = dir8 == 0 || dir8 == 2 ? 0 : -1;
            Vector3 epos = P2DDDircle ? new(116.5f, 0, 100f): new(101f, 0, 100f);
            var dealpos = RotatePoint(epos, new(100, 0, 100), float.Pi / 4 * (rot+dr));
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_钻石星尘_扇形引导位置";
            dp.Scale = new(2);
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 6500;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        [ScriptMethod(name: "P2_钻石星尘_九连环记录", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(40198)$"], userControl: false)]
        public void P2_钻石星尘_九连环记录(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.1) return;
            var pos= JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            lock (P2DDIceDir)
            {
                P2DDIceDir.Add(PositionTo8Dir(pos, new(100, 0, 100)));
            }
        }
        [ScriptMethod(name: "P2_钻石星尘_击退位置", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^((4020[23]))$", "TargetIndex:1"])]
        public void P2_钻石星尘_击退位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.1) return;
            Task.Delay(2500).ContinueWith(t =>
            {
                var nPos = new Vector3(100, 0, 96);
                var dir8 = P2DDIceDir.FirstOrDefault() % 4;
                int[] h1Group = [0, 2, 4, 6];
                var myIndex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                var isH1Group = h1Group.Contains(myIndex);
                
                var rot = dir8 switch
                {
                    0 => 4,
                    1 => 1,
                    2 => 2,
                    3 => 3,
                };
                
                rot += isH1Group ? 4 : 0;
                var dealpos = RotatePoint(nPos, new(100, 0, 100), float.Pi / 4 * rot);
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P2_钻石星尘_击退位置";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 6000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            });
            
        }
        [ScriptMethod(name: "P2_钻石星尘_连续剑分身位置", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^40208$", "TargetIndex:1"])]
        public void P2_钻石星尘_连续剑分身位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.1) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            Vector3 dealpos = new(100 + (pos.X - 100) * 1.4f, 0, 100 + (pos.Z - 100) * 1.4f);
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_钻石星尘_连续剑分身位置";
            dp.Scale = new(2);
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 9000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);


        }
        [ScriptMethod(name: "P2_钻石星尘_连续剑范围", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^4019[34]$"])]
        public void P2_钻石星尘_连续剑范围(Event @event, ScriptAccessory accessory)
        {
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            var time = 300;
            //93 先正面
            if (@event["ActionId"]=="40193")
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P2_钻石星尘_连续剑范围正1";
                dp.Scale = new(30);
                dp.Radian = float.Pi / 2 * 3;
                dp.Owner = sid;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.DestoryAt = 3500-time;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P2_钻石星尘_连续剑范围反2";
                dp.Scale = new(30);
                dp.Radian = float.Pi / 2;
                dp.Rotation = float.Pi;
                dp.Owner = sid;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.Delay = 3500-time;
                dp.DestoryAt = 2000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
            }
            else
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P2_钻石星尘_连续剑范围反1";
                dp.Scale = new(30);
                dp.Radian = float.Pi / 2;
                dp.Rotation = float.Pi;
                dp.Owner = sid;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.DestoryAt = 3500-time;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P2_钻石星尘_连续剑范围正2";
                dp.Scale = new(30);
                dp.Radian = float.Pi / 2 * 3;
                dp.Owner = sid;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.Delay = 3500-time;
                dp.DestoryAt = 2000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
            }
        }
        [ScriptMethod(name: "P2_钻石星尘_Boss背对", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^40208$", "TargetIndex:1"])]
        public void P2_钻石星尘_Boss背对(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.1) return;
            
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_钻石星尘_Boss背对";
            dp.Scale = new(5);
            dp.Owner = accessory.Data.Me;
            dp.TargetObject=P2BossId;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 3000;
            dp.DestoryAt = 6000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.SightAvoid, dp);


        }

        [ScriptMethod(name: "P2_双镜_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(40179)$"], userControl: false)]
        public void P2_双镜_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 2.2d;
            P2RedMirror.Clear();
        }
        [ScriptMethod(name: "P2_双镜_分散分摊", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(4022[01])$"])]
        public void P2_双镜_分散分摊(Event @event, ScriptAccessory accessory)
        {
            if(parse != 2.2) return;
            if (@event["ActionId"]=="40221")
            {
                foreach (var pm in accessory.Data.PartyList)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P2_双镜_分散";
                    dp.Scale = new(5);
                    dp.Owner = pm;
                    dp.Color = accessory.Data.DefaultDangerColor;
                    dp.DestoryAt = 5000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
                }
            }
            else
            {
                int[] group = [6, 7, 4, 5, 2, 3, 0, 1];
                var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                for (int i = 0; i < 4; i++)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P2_双镜_分摊";
                    dp.Scale = new(5);
                    dp.Owner = accessory.Data.PartyList[i];
                    dp.Color = group[myindex]==i||i==myindex?accessory.Data.DefaultSafeColor: accessory.Data.DefaultDangerColor;
                    dp.DestoryAt = 5000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
                }
            }
            
        }
        [ScriptMethod(name: "P2_双镜_蓝镜月环加引导", eventType: EventTypeEnum.EnvControl, eventCondition: ["DirectorId:800375BF", "State:00020001"])]
        public void P2_双镜_蓝镜月环加引导(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.2) return;
            if (!int.TryParse(@event["Index"], out var dir8)) return;
            Vector3 npos = new(100, 0, 80);
            dir8--;
            Vector3 dealpos = RotatePoint(npos, new(100, 0, 100), float.Pi / 4 * dir8);
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_双镜_蓝镜月环";
            dp.Scale = new(20);
            dp.InnerScale = new(4);
            dp.Radian = float.Pi * 2;
            dp.Position = dealpos;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 6000;
            dp.DestoryAt = 6000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);

             dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_双镜_蓝镜扇形引导1";
            dp.Scale = new(60);
            dp.Radian = float.Pi / 6;
            dp.Position = dealpos;
            dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
            dp.TargetOrderIndex = 1;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 6000;
            dp.DestoryAt = 7000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_双镜_蓝镜扇形引导2";
            dp.Scale = new(60);
            dp.Radian = float.Pi / 6;
            dp.Position = dealpos;
            dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
            dp.TargetOrderIndex = 2;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 6000;
            dp.DestoryAt = 7000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_双镜_蓝镜扇形引导3";
            dp.Scale = new(60);
            dp.Radian = float.Pi / 6;
            dp.Position = dealpos;
            dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
            dp.TargetOrderIndex = 3;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 6000;
            dp.DestoryAt = 7000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_双镜_蓝镜扇形引导4";
            dp.Scale = new(60);
            dp.Radian = float.Pi / 6;
            dp.Position = dealpos;
            dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
            dp.TargetOrderIndex = 4;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 6000;
            dp.DestoryAt = 7000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

        }
        [ScriptMethod(name: "P2_双镜_红镜月环加引导", eventType: EventTypeEnum.EnvControl, eventCondition: ["DirectorId:800375BF", "State:02000100"])]
        public void P2_双镜_红月环加引导(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.2) return;
            if (!int.TryParse(@event["Index"], out var dir8)) return;
            Vector3 npos = new(100, 0, 80);
            dir8--;
            Vector3 dealpos = RotatePoint(npos, new(100, 0, 100), float.Pi / 4 * dir8);
            var dur = 3000;
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_双镜_红镜月环";
            dp.Scale = new(20);
            dp.InnerScale = new(4);
            dp.Radian = float.Pi * 2;
            dp.Position = dealpos;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 17500;
            dp.DestoryAt = 5000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_双镜_红镜扇形引导1";
            dp.Scale = new(60);
            dp.Radian = float.Pi / 6;
            dp.Position = dealpos;
            dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
            dp.TargetOrderIndex = 1;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 23000-dur;
            dp.DestoryAt = dur;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_双镜_红镜扇形引导2";
            dp.Scale = new(60);
            dp.Radian = float.Pi / 6;
            dp.Position = dealpos;
            dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
            dp.TargetOrderIndex = 2;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 23000 - dur;
            dp.DestoryAt = dur;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_双镜_红镜扇形引导3";
            dp.Scale = new(60);
            dp.Radian = float.Pi / 6;
            dp.Position = dealpos;
            dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
            dp.TargetOrderIndex = 3;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 23000 - dur;
            dp.DestoryAt = dur;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_双镜_红镜扇形引导4";
            dp.Scale = new(60);
            dp.Radian = float.Pi / 6;
            dp.Position = dealpos;
            dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
            dp.TargetOrderIndex = 4;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 23000 - dur;
            dp.DestoryAt = dur;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

        }
        [ScriptMethod(name: "P2_双镜_蓝镜月环加引导位置", eventType: EventTypeEnum.EnvControl, eventCondition: ["DirectorId:800375BF", "State:00020001"])]
        public void P2_双镜_蓝镜月环加引导位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.2) return;
            if (!int.TryParse(@event["Index"], out var dir8)) return;
            Vector3 npos = new(100, 0, 80);
            dir8--;
            var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            if (myindex == 0 || myindex == 1 || myindex == 4 || myindex == 5)
            {
                dir8 += 4;
                npos = new(100, 0, 85);
            }
           
            Vector3 dealpos = RotatePoint(npos, new(100, 0, 100), float.Pi / 4 * dir8);
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_双镜_蓝镜月环加引导位置";
            dp.Scale = new(2);
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 12000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        [ScriptMethod(name: "P2_双镜_红镜引导位置", eventType: EventTypeEnum.EnvControl, eventCondition: ["DirectorId:800375BF", "State:02000100"])]
        public void P2_双镜_红镜引导位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.2) return;
            if (!int.TryParse(@event["Index"], out var dir8)) return;
            dir8--;
            lock (P2RedMirror)
            {
                P2RedMirror.Add(dir8);
                if (P2RedMirror.Count != 2) return;
            }
            var leftRot8 = (P2RedMirror[0] - P2RedMirror[1] == -2 || P2RedMirror[0] - P2RedMirror[1] - 8 == -2) ? P2RedMirror[0] : P2RedMirror[1];
            var rightRot8 = (P2RedMirror[0] - P2RedMirror[1] == 2 || P2RedMirror[0] + 8 - P2RedMirror[1] == 2) ? P2RedMirror[0] : P2RedMirror[1];

            var myrot = leftRot8;
            var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            if (myindex == 0 || myindex == 1 || myindex == 4 || myindex == 5)
            {
                myrot = rightRot8;
            }
            Vector3 npos = myindex switch
            {
                0 => new(102f, 0, 80.5f),
                1 => new(98f, 0, 80.5f),
                2 => new(102f, 0, 80.5f),
                3 => new(98f, 0, 80.5f),
                4 => new(101.3f, 0, 83f),
                5 => new(98.7f, 0, 83f),
                6 => new(101.3f, 0, 83f),
                7 => new(98.7f, 0, 83f),
                _ => new(100, 0, 80)
            };
            var dealpos = RotatePoint(npos, new(100, 0, 100), myrot * float.Pi / 4);
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_双镜_红镜引导位置";
            dp.Scale = new(2);
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.Delay = 13500;
            dp.DestoryAt = 9000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }

        [ScriptMethod(name: "P2_光之暴走_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(40212)$"], userControl: false)]
        public void P2_光之暴走_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 2.3d;
            P2LightRampantCircle.Clear();
            P2LightRampantTetherDone = false;
            P2LightRampantBuff = [0, 0, 0, 0, 0, 0, 0, 0];
        }
        [ScriptMethod(name: "P2_光之暴走_大圈收集", eventType: EventTypeEnum.TargetIcon,userControl:false)]
        public void P2_光之暴走_大圈收集(Event @event, ScriptAccessory accessory)
        {
            if (ParsTargetIcon(@event["Id"]) != 157) return;
            if (parse != 2.3) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var index=accessory.Data.PartyList.IndexOf(((uint)tid));
            lock (P2LightRampantCircle)
            {
                P2LightRampantCircle.Add(index);
            }
        }
        [ScriptMethod(name: "P2_光之暴走_Buff收集", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:2257"], userControl: false)]
        public void P2_光之暴走_Buff收集(Event @event, ScriptAccessory accessory)
        {
            
            if (parse != 2.3) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            if (!int.TryParse(@event["StackCount"], out var count)) return;
            var index = accessory.Data.PartyList.IndexOf(((uint)tid));
            lock (P2LightRampantBuff)
            {
                P2LightRampantBuff[index] = count;
            }
        }
        [ScriptMethod(name: "P2_光之暴走_分散分摊", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(4022[01])$"])]
        public void P2_光之暴走_分散分摊(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.3) return;
            if (@event["ActionId"] == "40221")
            {
                foreach (var pm in accessory.Data.PartyList)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P2_光之暴走_分散";
                    dp.Scale = new(5);
                    dp.Owner = pm;
                    dp.Color = accessory.Data.DefaultDangerColor;
                    dp.DestoryAt = 5000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
                }
            }
            else
            {
                int[] group = [6, 7, 4, 5, 2, 3, 0, 1];
                var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                for (int i = 0; i < 4; i++)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P2_光之暴走_分摊";
                    dp.Scale = new(5);
                    dp.Owner = accessory.Data.PartyList[i];
                    dp.Color = group[myindex] == i || i == myindex ? accessory.Data.DefaultSafeColor : accessory.Data.DefaultDangerColor;
                    dp.DestoryAt = 5000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
                }
            }

        }
        [ScriptMethod(name: "P2_光之暴走_分摊buff", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:4159"])]
        public void P2_光之暴走_分摊buff(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.3) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_光之暴走_分摊buff";
            dp.Scale = new(5);
            dp.Owner = tid;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.Delay = 12000;
            dp.DestoryAt = 5000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        }
        [ScriptMethod(name: "P2_光之暴走_塔处理位置", eventType: EventTypeEnum.TargetIcon)]
        public void P2_光之暴走_塔处理位置(Event @event, ScriptAccessory accessory)
        {

            if (ParsTargetIcon(@event["Id"]) != 157) return;
            if (parse != 2.3) return;
            lock (this)
            {
                if (P2LightRampantTetherDone) return;
                P2LightRampantTetherDone = true;
            }
            Task.Delay(250).ContinueWith(t =>
            {
                var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                if (P2LightRampantCircle.Contains(myindex)) return;
                
                List<int> tetherGroup = [];
                if (P2LightRampant8DirSet == P2LightRampant8DirEmum.Normal)
                {
                    if (!P2LightRampantCircle.Contains(2)) tetherGroup.Add(2);
                    if (!P2LightRampantCircle.Contains(6)) tetherGroup.Add(6);
                    if (!P2LightRampantCircle.Contains(0)) tetherGroup.Add(0);
                    if (!P2LightRampantCircle.Contains(7)) tetherGroup.Add(7);
                    if (!P2LightRampantCircle.Contains(3)) tetherGroup.Add(3);
                    if (!P2LightRampantCircle.Contains(5)) tetherGroup.Add(5);
                    if (!P2LightRampantCircle.Contains(1)) tetherGroup.Add(1);
                    if (!P2LightRampantCircle.Contains(4)) tetherGroup.Add(4);
                }
                if (P2LightRampant8DirSet == P2LightRampant8DirEmum.TN_Up)
                {
                    if (!P2LightRampantCircle.Contains(0)) tetherGroup.Add(0);
                    if (!P2LightRampantCircle.Contains(1)) tetherGroup.Add(1);
                    if (!P2LightRampantCircle.Contains(2)) tetherGroup.Add(2);
                    if (!P2LightRampantCircle.Contains(3)) tetherGroup.Add(3);
                    if (!P2LightRampantCircle.Contains(7)) tetherGroup.Add(7);
                    if (!P2LightRampantCircle.Contains(6)) tetherGroup.Add(6);
                    if (!P2LightRampantCircle.Contains(5)) tetherGroup.Add(5);
                    if (!P2LightRampantCircle.Contains(4)) tetherGroup.Add(4);
                }


                var myGroupIndex = tetherGroup.IndexOf(myindex);
                Vector3 t1 = new(100.00f, 0, 084.00f);
                Vector3 t2 = new(113.85f, 0, 092.00f);
                Vector3 t3 = new(113.85f, 0, 108.00f);
                Vector3 t4 = new(100.00f, 0, 116.00f);
                Vector3 t5 = new(086.14f, 0, 108.00f);
                Vector3 t6 = new(086.14f, 0, 092.00f);

                Vector3 pa = new(100.00f, 0, 82.00f);
                Vector3 pb = new(118.00f, 0, 100.00f);
                Vector3 pc = new(100.00f, 0, 118.00f);
                Vector3 pd = new(82.00f, 0, 100.00f);


                Vector3 dealpos = default;
                Vector3 dealpos2 = default;
                if (P2LightRampantTetherDeal == P2LightRampantTetherEmum.CircleNum)
                {
                    var count = 0;
                    if (myindex == 0)
                    {
                        dealpos = t4;
                    }
                    count += P2LightRampantCircle.Contains(0) ? 1 : 0;
                    if (myindex == 7)
                    {
                        dealpos = P2LightRampantCircle.Contains(0) ? t4 : t2;
                    }
                    count += P2LightRampantCircle.Contains(7) ? 1 : 0;
                    if (myindex == 1)
                    {
                        if (count == 0) dealpos = t6;
                        if (count == 1) dealpos = t2;
                        if (count == 2) dealpos = t4;
                    }
                    count += P2LightRampantCircle.Contains(1) ? 1 : 0;
                    if (myindex == 5)
                    {
                        if (count == 0) dealpos = t3;
                        if (count == 1) dealpos = t6;
                        if (count == 2) dealpos = t2;
                    }
                    count += P2LightRampantCircle.Contains(5) ? 1 : 0;
                    if (myindex == 3)
                    {
                        if (count == 0) dealpos = t5;
                        if (count == 1) dealpos = t3;
                        if (count == 2) dealpos = t6;
                    }
                    count += P2LightRampantCircle.Contains(3) ? 1 : 0;
                    if (myindex == 4)
                    {
                        if (count == 0) dealpos = t1;
                        if (count == 1) dealpos = t5;
                        if (count == 2) dealpos = t3;
                    }
                    count += P2LightRampantCircle.Contains(4) ? 1 : 0;
                    if (myindex == 2)
                    {
                        dealpos = P2LightRampantCircle.Contains(6) ? t1 : t5;
                    }
                    if (myindex == 6)
                    {
                        dealpos = t1;
                    }


                    if ((dealpos - t1).Length() < 1 || (dealpos - t2).Length() < 1 || (dealpos - t3).Length() < 1)
                    {
                        dealpos2 = pb;
                    }
                    else
                    {
                        dealpos2 = pd;
                    }
                }
                if (P2LightRampantTetherDeal == P2LightRampantTetherEmum.NewGrey9)
                {
                    var count = 0;
                    if (myindex == 0)
                    {
                        dealpos = t4;
                    }
                    count += P2LightRampantCircle.Contains(0) ? 1 : 0;
                    if (myindex == 7)
                    {
                        dealpos = P2LightRampantCircle.Contains(0) ? t4 : t6;
                    }
                    count += P2LightRampantCircle.Contains(7) ? 1 : 0;
                    if (myindex == 1)
                    {
                        if (count == 0) dealpos = t2;
                        if (count == 1) dealpos = t6;
                        if (count == 2) dealpos = t4;
                    }
                    count += P2LightRampantCircle.Contains(1) ? 1 : 0;
                    if (myindex == 5)
                    {
                        if (count == 0) dealpos = t5;
                        if (count == 1) dealpos = t2;
                        if (count == 2) dealpos = t6;
                    }
                    count += P2LightRampantCircle.Contains(5) ? 1 : 0;
                    if (myindex == 3)
                    {
                        if (count == 0) dealpos = t3;
                        if (count == 1) dealpos = t5;
                        if (count == 2) dealpos = t2;
                    }
                    count += P2LightRampantCircle.Contains(3) ? 1 : 0;
                    if (myindex == 4)
                    {
                        if (count == 0) dealpos = t1;
                        if (count == 1) dealpos = t3;
                        if (count == 2) dealpos = t5;
                    }
                    count += P2LightRampantCircle.Contains(4) ? 1 : 0;
                    if (myindex == 2)
                    {
                        dealpos = P2LightRampantCircle.Contains(6) ? t1 : t3;
                    }
                    if (myindex == 6)
                    {
                        dealpos = t1;
                    }

                    if ((dealpos - t2).Length() < 1 || (dealpos - t3).Length() < 1 || (dealpos - t4).Length() < 1)
                    {
                        dealpos2 = pb;
                    }
                    else
                    {
                        dealpos2 = pd;
                    }
                }
                if (P2LightRampantTetherDeal == P2LightRampantTetherEmum.LTeam)
                {
                    dealpos = myGroupIndex switch
                    {
                        1 => t1,
                        4 => t4,
                        0 => t5,
                        2 => t3,
                        3 => t6,
                        5 => t2,
                    };
                    if ((dealpos - t1).Length() < 1 || (dealpos - t2).Length() < 1 || (dealpos - t6).Length() < 1)
                    {
                        dealpos2 = pa;
                    }
                    else
                    {
                        dealpos2 = pc;
                    }
                }
                if (P2LightRampantTetherDeal == P2LightRampantTetherEmum.AC_Cross)
                {
                    dealpos = myGroupIndex switch
                    {
                        1 => t4,
                        4 => t1,
                        0 => t6,
                        2 => t2,
                        3 => t5,
                        5 => t3,
                    };
                    if ((dealpos - t1).Length() < 1 || (dealpos - t2).Length() < 1 || (dealpos - t6).Length() < 1)
                    {
                        dealpos2 = pa;
                    }
                    else
                    {
                        dealpos2 = pc;
                    }
                }

                

                var dur = 10000;
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P2_光之暴走_塔处理位置";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = dur;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P2_光之暴走_塔处理位置";
                dp.Scale = new(4);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Position = dealpos;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = dur;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Circle, dp);

                

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P2_光之暴走_集合位置";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Position = dealpos;
                dp.TargetPosition = dealpos2;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = dur;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P2_光之暴走_集合位置";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos2;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = dur;
                dp.DestoryAt = 6000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            });

        }
        [ScriptMethod(name: "P2_双镜_中央踩塔位置", eventType: EventTypeEnum.EnvControl, eventCondition: ["DirectorId:800375BF", "State:00020001", "Index:00000015"])]
        public void P2_光之暴走_中央踩塔位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.3) return;
            var myindex= accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            if (P2LightRampantBuff[myindex]<=2)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P2_光之暴走_中央踩塔位置";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = new(100,0,100);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 8000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P2_光之暴走_塔处理位置";
                dp.Scale = new(4);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Position = new(100, 0, 100);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 8000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Circle, dp);
            }
        }
        
        [ScriptMethod(name: "P2_光之暴走_八方分散位置", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^(4022[01])$"])]
        public void P2_光之暴走_八方分散位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.3) return;
            var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            var rot8 = myindex switch
            {
                0 => 0,
                1 => 2,
                2 => 6,
                3 => 4,
                4 => 5,
                5 => 3,
                6 => 7,
                7 => 1,
                _ => 0,
            };
            var mPosEnd = RotatePoint(new(100, 0, 95), new(100, 0, 100), float.Pi / 4 * rot8);

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_光之暴走_八方分散位置";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = mPosEnd;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 9000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

        }
        
        [ScriptMethod(name:"----- Phase 2.5 ----- (No actual meaning for this toggle/此开关无实际意义)",
            eventType:EventTypeEnum.NpcYell,
            eventCondition:["Your huddled masses yearning to breathe free",
                            "蜷缩着祈盼自由呼吸的人"])]
        
        public void Phase2point5_Placeholder(Event @event, ScriptAccessory accessory) { }

        [ScriptMethod(name: "P2.5_暗水晶AOE", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:40262"])]
        public void P2_暗水晶AOE(Event @event, ScriptAccessory accessory)
        {
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2.5_暗水晶AOE";
            dp.Scale = new(50);
            dp.Radian = float.Pi / 9;
            dp.Owner = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 3000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Fan, dp);
        }
        #endregion

        #region P3
        
        [ScriptMethod(name:"----- Phase 3 ----- (No actual meaning for this toggle/此开关无实际意义)",
            eventType:EventTypeEnum.NpcYell,
            eventCondition:["The wretched refuse of your teeming shore",
                            "被你们的繁荣拒之门外受苦的人"])]
        
        public void Phase3_Placeholder(Event @event, ScriptAccessory accessory) { }
        
        [ScriptMethod(name: "P3_时间压缩_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(40266)$"], userControl: false)]
        public void P3_时间压缩_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 3.1d;
            phase3_bossId=@event["SourceId"];
            P3FireBuff = [0, 0, 0, 0, 0, 0, 0, 0];
            P3WaterBuff= [0, 0, 0, 0, 0, 0, 0, 0];
            P3ReturnBuff = [0, 0, 0, 0, 0, 0, 0, 0];
            P3Lamp = [0, 0, 0, 0, 0, 0, 0, 0];
            P3LampWise = [0, 0, 0, 0, 0, 0, 0, 0];
        }
        [ScriptMethod(name: "P3_时间压缩_Buff记录", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:regex:^(2455|2456|2464|2462|2461|2460)$"], userControl: false)]
        public void P3_时间压缩_Buff记录(Event @event, ScriptAccessory accessory)
        {
            if (parse != 3.1) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            if(!float.TryParse(@event["Duration"], out var dur)) return;
            var index = accessory.Data.PartyList.IndexOf(((uint)tid));
            if (index == -1) return;
            //冰
            if (@event["StatusID"] == "2462")
            {
                lock (P3FireBuff)
                {
                    P3FireBuff[index] = 4;
                }
            }
            //火
            if (@event["StatusID"] == "2455")
            {
                
                var count = 1;
                if (dur > 20) count = 2;
                if (dur > 30) count = 3;
                lock (P3FireBuff)
                {
                    P3FireBuff[index] = count;
                }
            }
            //回返
            if (@event["StatusID"] == "2464")
            {
                var count = 1;
                if (dur > 20) count = 3;
                lock (P3ReturnBuff)
                {
                    P3ReturnBuff[index] = count;
                }
            }
            //水
            if (@event["StatusID"] == "2461")
            {
                lock (P3WaterBuff)
                {
                    P3WaterBuff[index] = 1;
                }
            }
            //圈
            if (@event["StatusID"] == "2460")
            {
                lock (P3WaterBuff)
                {
                    P3WaterBuff[index] = 2;
                }
            }
            //背对
            if (@event["StatusID"] == "2456")
            {
                lock (P3WaterBuff)
                {
                    P3WaterBuff[index] = 3;
                }
            }



        }
        [ScriptMethod(name: "P3_时间压缩_灯记录", eventType: EventTypeEnum.Tether, eventCondition: ["Id:regex:^(0085|0086)$"], userControl: false)]
        public void P3_时间压缩_灯记录(Event @event, ScriptAccessory accessory)
        {
            //0085紫
            //0086黄
            if (parse != 3.1) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            var dir8= PositionTo8Dir(pos, new(100, 0, 100));
            lock (P3Lamp)
            {
                P3Lamp[dir8] = @event["Id"] == "0086" ? 1 : 2;
            }
        }
        [ScriptMethod(name: "P3_时间压缩_灯顺逆记录", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:2970"],userControl:false)]
        public void P3_时间压缩_灯顺逆记录(Event @event, ScriptAccessory accessory)
        {
            //buff2970, 13 269顺时针 92 348逆时针
            if (parse != 3.1) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]);
            Vector3 centre = new(100, 0, 100);
            var dir8 = PositionTo8Dir(pos, centre);
            P3LampWise[dir8] = @event["StackCount"] == "92" ? 1 : 0;
        }
        [ScriptMethod(name: "P3_时间压缩_灯AOE", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:40235", "TargetIndex:1"])]
        public void P3_时间压缩_灯AOE(Event @event, ScriptAccessory accessory)
        {
            if (parse != 3.1) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            var rot= JsonConvert.DeserializeObject<float>(@event["SourceRotation"]);
            Vector3 centre = new(100, 0, 100);
            var dir8 = PositionTo8Dir(pos, centre);
            var isWise = P3LampWise[dir8] == 1;
            for (int i = 0; i < 9; i++)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_灯AOE";
                dp.Scale = new(5,50);
                dp.Position = pos;
                dp.Rotation = rot + (i + 1) * float.Pi / 12 * (isWise ? -1 : 1);
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.DestoryAt = 2000+(i*1000);
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
            }
        }
        [ScriptMethod(name: "P3_时间压缩_Buff处理位置", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:40293"])]
        public void P3_时间压缩_Buff处理位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 3.1) return;
            var myIndex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            if (myIndex == -1) return;
            var myDir8 = MyLampIndex(myIndex);
            //accessory.Log.Debug($"myDir8 {myDir8}");
            if (myDir8 == -1) return;
            var myRot = myDir8 * float.Pi / 4;

            Vector3 centre = new(100, 0, 100);
            Vector3 fireN = new(100, 0, 84.5f);
            Vector3 returnPosN = P3WaterBuff[myIndex] == 2 ? new(100, 0, 91.5f) : new(100, 0, 98);
            Vector3 stopPos = new(100, 0, 101);
            //火
            var myFire = P3FireBuff[myIndex];
            //短火
            if (myFire == 1)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_短火_放火";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = RotatePoint(fireN, centre, myRot);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 7500;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_短火_放回溯";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = RotatePoint(returnPosN, centre, myRot);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 7500;
                dp.DestoryAt = 5000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_短火_场中分摊";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = centre;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 12500;
                dp.DestoryAt = 5000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);


                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_短火_输出位置";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = RotatePoint(stopPos, centre, myRot);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 22500;
                dp.DestoryAt = 15000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }

            //中火
            if (myFire == 2)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_中火_中场分摊";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = centre;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 7500;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_中火_放回溯";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = RotatePoint(returnPosN, centre, myRot);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 7500;
                dp.DestoryAt = 5000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_中火_放火";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = RotatePoint(fireN, centre, myRot);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 12500;
                dp.DestoryAt = 5000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_中火_中场";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition =centre;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 17500;
                dp.DestoryAt = 10000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_中火_输出位置";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = RotatePoint(stopPos, centre, myRot);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 32500;
                dp.DestoryAt = 5000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

            }

            //长火
            if (myFire == 3)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_长火_中场分摊";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = centre;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 7500;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_长火_中场分摊";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = centre;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 12500;
                dp.DestoryAt = 5000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_长火_回溯";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = RotatePoint(returnPosN, centre, myRot);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 17500;
                dp.DestoryAt = 5000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_长火_放火";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = RotatePoint(fireN, centre, myRot);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 22500;
                dp.DestoryAt = 5000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_长火_输出";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = RotatePoint(stopPos, centre, myRot);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 27500;
                dp.DestoryAt = 10000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }

            if (myFire == 4)
            {
                if (myIndex <4)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P3_时间压缩_冰TH_放冰";
                    dp.Scale = new(2);
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = centre;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.DestoryAt = 7500;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P3_时间压缩_冰TH_放回溯";
                    dp.Scale = new(2);
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = RotatePoint(returnPosN, centre, myRot);
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.Delay = 7500;
                    dp.DestoryAt = 5000;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P3_时间压缩_冰TH_场中分摊";
                    dp.Scale = new(2);
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = centre;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.Delay = 12500;
                    dp.DestoryAt = 5000;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);


                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P3_时间压缩_冰TH_输出位置";
                    dp.Scale = new(2);
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = RotatePoint(stopPos, centre, myRot);
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.Delay = 22500;
                    dp.DestoryAt = 15000;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                }
                else
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P3_时间压缩_冰D_中场分摊";
                    dp.Scale = new(2);
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = centre;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.DestoryAt = 7500;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P3_时间压缩_冰D_中场分摊";
                    dp.Scale = new(2);
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = centre;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.Delay = 12500;
                    dp.DestoryAt = 5000;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P3_时间压缩_冰D_回溯";
                    dp.Scale = new(2);
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = RotatePoint(returnPosN, centre, myRot);
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.Delay = 17500;
                    dp.DestoryAt = 5000;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P3_时间压缩_冰D_放冰";
                    dp.Scale = new(2);
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = centre;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.Delay = 22500;
                    dp.DestoryAt = 5000;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P3_时间压缩_长火_输出";
                    dp.Scale = new(2);
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = RotatePoint(stopPos, centre, myRot);
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.Delay = 27500;
                    dp.DestoryAt = 10000;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                }
            }
        }
        [ScriptMethod(name: "P3_时间压缩_灯处理位置", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:2970"])]
        public void P3_时间压缩_灯处理位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 3.1) return;
            //buff2970, 13 269顺时针 92 348逆时针
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]);
            Vector3 centre = new(100, 0, 100);
            var myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            var dir8 = PositionTo8Dir(pos, centre);
            Vector3 nPos = @event["StackCount"] == "92" ? new(98, 0, 90) : new(102, 0, 90);
            if (dir8 == MyLampIndex(myIndex))
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_时间压缩_灯处理位置";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = RotatePoint(nPos,centre,dir8*float.Pi/4);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 4000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
        }

        [ScriptMethod(name:"Phase3 Prompt Before Shell Crusher 破盾一击前提示",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:40286"])]
        
        public void Phase3_Prompt_Before_Shell_Crusher_破盾一击前提示(Event @event, ScriptAccessory accessory) {

            if(parse!=3.1) {    

                return;

            }

            if(Enable_Text_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.Method.TextInfo("场中集合分摊",3000);
                    
                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.Method.TextInfo("Stack in the center",3000);
                    
                }
                
            }
            
            if (Enable_TTS_Prompts || Enable_DRTTS_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.TTS("场中集合分摊", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.TTS("Stack in the center", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }
                
            }
            
        }
        
        [ScriptMethod(name: "P3_时间压缩_黑暗光环", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:40290"])]
        public void P3_时间压缩_黑暗光环(Event @event, ScriptAccessory accessory)
        {
            if (parse != 3.1) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var myindex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_时间压缩_黑暗光环";
            dp.Scale = new(20);
            dp.Owner = sid;
            dp.TargetObject = tid;
            dp.Color = myindex == 0 || myindex == 1 ? accessory.Data.DefaultSafeColor : accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 5000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
        }

        [ScriptMethod(name:"Phase3 Initial Orientation Before The Second Half 二运前的初始面向",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:40290"])]
        
        public void Phase3_Initial_Orientation_Before_The_Second_Half_二运前的初始面向(Event @event, ScriptAccessory accessory) {
            
            if(parse!=3.1) {

                return;

            }
            
            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {

                return;
                
            }

            if(!accessory.Data.EnmityList.TryGetValue(sourceId, out var enmityListOfBoss)) {

                return;

            }
            
            if(Enable_Developer_Mode) {

                accessory.Method.SendChat($"""
                                           /e 
                                           accessory.Data.Me={accessory.Data.Me}
                                           enmityListOfTheBoss[0]={enmityListOfBoss[0]}

                                           """);

            }
            
            if(accessory.Data.Me!=enmityListOfBoss[0]) {

                return;

            }
            
            var currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            currentProperty.Name="Phase3_Initial_Orientation_Before_The_Second_Half_二运前的初始面向";
            currentProperty.Scale=new(2);
            currentProperty.ScaleMode|=ScaleMode.YByDistance;
            currentProperty.Owner=accessory.Data.Me;
            currentProperty.TargetPosition=new Vector3(100,0,94);
            currentProperty.Color=accessory.Data.DefaultSafeColor;
            currentProperty.DestoryAt=12500;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
            
            if(Enable_Text_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.Method.TextInfo("让Boss面向正北",12500);
                    
                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.Method.TextInfo("Make the Boss orient to the north",12500);
                    
                }
                
            }
            
            if (Enable_TTS_Prompts || Enable_DRTTS_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.TTS("让Boss面向正北", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.TTS("Make the Boss orient to the north", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }
                
            }
            
        }

        [ScriptMethod(name: "P3_延迟咏唱回响_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(40269)$"], userControl: false)]
        public void P3_延迟咏唱回响_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 3.2d;
            P3FloorFire = -1;
            phase3_typeOfDarkWaterIii=[
                Phase3_Types_Of_Dark_Water_III.NONE,
                Phase3_Types_Of_Dark_Water_III.NONE,
                Phase3_Types_Of_Dark_Water_III.NONE,
                Phase3_Types_Of_Dark_Water_III.NONE,
                Phase3_Types_Of_Dark_Water_III.NONE,
                Phase3_Types_Of_Dark_Water_III.NONE,
                Phase3_Types_Of_Dark_Water_III.NONE,
                Phase3_Types_Of_Dark_Water_III.NONE
            ];
            phase3_numberOfDarkWaterIiiHasBeenProcessed=0;
            phase3_roundOfDarkWaterIii=0;
            phase3_rangeSemaphoreOfDarkWaterIii=0;
            phase3_guidanceSemaphoreOfDarkWaterIii=0;
            phase3_hasConfirmedInitialSafePositions=false;
            phase3_doubleGroup_initialSafePositionOfTheLeftGroup=new Vector3(100,0,100);
            phase3_doubleGroup_initialSafePositionOfTheRightGroup=new Vector3(100,0,100);
            phase3_doubleGroup_leftPositionToStackOfTheSecondRound=new Vector3(100,0,100);
            phase3_doubleGroup_rightPositionToStackOfTheSecondRound=new Vector3(100,0,100);
            phase3_locomotive_initialSafePositionOfTheLeftGroup=new Vector3(100,0,100);
            phase3_locomotive_initialSafePositionOfTheRightGroup=new Vector3(100,0,100);
            phase3_locomotive_leftPositionToStackOfTheSecondRound=new Vector3(100,0,100);
            phase3_locomotive_rightPositionToStackOfTheSecondRound=new Vector3(100,0,100);
            phase3_bossPositionAfterDarkestDance=new Vector3(100,0,100);
            phase3_finalPositionOfTheBoss=new Vector3(100,0,100);
        }
        
        [ScriptMethod(name:"Phase3 Determine Types Of Dark Water III 确定黑暗狂水(分摊)类型",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:2461"],
            userControl:false)]
        
        public void Phase3_Determine_Types_Of_Dark_Water_III_确定黑暗狂水类型(Event @event, ScriptAccessory accessory) {
            
            if(parse!=3.2) {

                return;

            }

            if(!ParseObjectId(@event["TargetId"], out var targetId)) {

                return;

            }

            int currentIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            int duration=Convert.ToInt32(@event["DurationMilliseconds"],10);

            if(currentIndex<0||currentIndex>7) {

                return;

            }

            if(duration>36000) {
                // Actually it's 38000ms (38s), but just in case.

                lock(phase3_typeOfDarkWaterIii) {

                    phase3_typeOfDarkWaterIii[currentIndex]=Phase3_Types_Of_Dark_Water_III.LONG;

                }

            }

            else {

                if(duration>27000) {
                    // Actually it's 29000ms (29s), but just in case.

                    lock(phase3_typeOfDarkWaterIii) {

                        phase3_typeOfDarkWaterIii[currentIndex]=Phase3_Types_Of_Dark_Water_III.MEDIUM;

                    }

                }

                else {

                    if(duration>8000) {
                        // Actually it's 10000ms (10s), but just in case.

                        lock(phase3_typeOfDarkWaterIii) {

                            phase3_typeOfDarkWaterIii[currentIndex]=Phase3_Types_Of_Dark_Water_III.SHORT;

                        }

                    }

                }

            }

            ++phase3_numberOfDarkWaterIiiHasBeenProcessed;

            if(Enable_Developer_Mode) {

                accessory.Method.SendChat($"""
                                           /e 
                                           currentIndex={currentIndex}
                                           duration={duration}
                                           phase3_typeOfDarkWaterIii={phase3_typeOfDarkWaterIii[currentIndex]}
                                           
                                           """);

            }
            
        }
        
        [ScriptMethod(name:"Phase3 Prompt Before Dark Water III 暗黑狂水(分摊)前提示",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:2461"],
            suppress:2000)]

        public void Phase3_Prompt_Before_Dark_Water_III_暗黑狂水前提示(Event @event, ScriptAccessory accessory) {
            
            if(parse!=3.2) {

                return;

            }

            while(phase3_numberOfDarkWaterIiiHasBeenProcessed<6) {
                
                System.Threading.Thread.Sleep(1);
                
            }
            
            if(Phase3_Strat_Of_The_Second_Half==Phase3_Strats_Of_The_Second_Half.Double_Group_双分组法) {
                
                bool goLeft=phase3_doubleGroup_shouldGoLeft(accessory.Data.PartyList.IndexOf(accessory.Data.Me));
                bool stayInTheGroup=phase3_doubleGroup_shouldStayInTheGroup(accessory.Data.PartyList.IndexOf(accessory.Data.Me));
                string prompt="";
                var currentProperty=accessory.Data.GetDefaultDrawProperties();
            
                currentProperty.Name="Phase3_Prompt_Before_Dark_Water_III_暗黑狂水前提示";
                currentProperty.Scale=new(2);
                currentProperty.ScaleMode|=ScaleMode.YByDistance;
                currentProperty.Owner=accessory.Data.Me;
                currentProperty.TargetPosition=(goLeft)?(new Vector3(93,0,100)):(new Vector3(107,0,100));
                currentProperty.Color=accessory.Data.DefaultSafeColor;
                currentProperty.DestoryAt=5000;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                if(goLeft) {

                    if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                        prompt+="去左组分摊第一第三次，";

                    }
                
                    if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                        prompt+="Go left for the first and third, ";

                    }
                
                }

                else {
                
                    if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                        prompt+="去右组分摊第一第三次，";

                    }
                
                    if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                        prompt+="Go right for the first and third, ";

                    }
                
                }
            
                if(stayInTheGroup) {

                    if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                        prompt+="第二次留在本组";

                    }
                
                    if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                        prompt+="stay in the current group for the second";

                    }
                
                }

                else {
                
                    if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                        prompt+="第二次换去对组";

                    }
                
                    if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                        prompt+="move to the opposite group for the second";

                    }
                
                }
            
                if(Enable_Text_Prompts) {
                
                    accessory.Method.TextInfo(prompt,4000);
                
                }

                accessory.TTS($"{prompt}", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

            }
            
            if(Phase3_Strat_Of_The_Second_Half==Phase3_Strats_Of_The_Second_Half.High_Priority_As_Locomotives_车头低换法) {

                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                bool goLeft=phase3_locomotive_shouldGoLeft(myIndex);
                string prompt="";
                var currentProperty=accessory.Data.GetDefaultDrawProperties();
            
                currentProperty.Name="Phase3_Prompt_Before_Dark_Water_III_暗黑狂水前提示";
                currentProperty.Scale=new(2);
                currentProperty.ScaleMode|=ScaleMode.YByDistance;
                currentProperty.Owner=accessory.Data.Me;
                currentProperty.TargetPosition=(goLeft)?(new Vector3(93,0,100)):(new Vector3(107,0,100));
                currentProperty.Color=accessory.Data.DefaultSafeColor;
                currentProperty.DestoryAt=5000;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                if(goLeft) {

                    if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                        prompt+="去左组分摊，";

                    }
                
                    if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                        prompt+="Go left to stack, ";

                    }
                
                }

                else {
                
                    if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                        prompt+="去右组分摊，";

                    }
                
                    if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                        prompt+="Go right to stack, ";

                    }
                
                }

                if(Phase3_Branch_Of_The_Locomotive_Strat==Phase3_Branches_Of_The_Locomotive_Strat.MT_And_M1_As_Locomotives_MT和D1为车头) {

                    if(myIndex!=0&&myIndex!=4) {

                        if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                            prompt+=(goLeft)?("跟随MT"):("跟随D1");

                        }

                        if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                            prompt+=(goLeft)?("follow MT"):("follow M1");

                        }

                    }

                    if(myIndex==0||myIndex==4) {

                        if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                            prompt+="你是车头";

                        }

                        if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                            prompt+="you are the locomotive";

                        }

                    }

                }
                
                if(Phase3_Branch_Of_The_Locomotive_Strat==Phase3_Branches_Of_The_Locomotive_Strat.Others_As_Locomotives_人群为车头) {

                    if(myIndex!=0&&myIndex!=4) {

                        if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                            prompt+="你是人群车头之一";

                        }

                        if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                            prompt+="you are one of the locomotives";

                        }

                    }

                    if(myIndex==0||myIndex==4) {

                        if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                            prompt+="跟随组内人群";

                        }

                        if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                            prompt+="follow others in the group";

                        }

                    }

                }

                if(Enable_Text_Prompts) {
                
                    accessory.Method.TextInfo(prompt,3500);
                
                }

                accessory.TTS($"{prompt}", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

            }
            
        }

        [ScriptMethod(name:"Phase3 Release The Semaphore Of Dark Water III 释放黑暗狂水(分摊)的信号灯",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:2458"],
            suppress:2000,
            userControl:false)]

        public void Phase3_Release_The_Semaphore_Of_Dark_Water_III_释放黑暗狂水的信号灯(Event @event, ScriptAccessory accessory) {
            
            if(parse!=3.2) {

                return;

            }

            ++phase3_roundOfDarkWaterIii;

            phase3_rangeSemaphoreOfDarkWaterIii=1;
            phase3_guidanceSemaphoreOfDarkWaterIii=1;

        }

        [ScriptMethod(name:"Phase3 Range Of Dark Water III 黑暗狂水(分摊)范围",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:2458"],
            suppress:2000)]

        public void Phase3_Range_Of_Dark_Water_III_黑暗狂水范围(Event @event, ScriptAccessory accessory) {

            while(System.Threading.Interlocked.CompareExchange(ref phase3_rangeSemaphoreOfDarkWaterIii,0,1)==0) {
                
                System.Threading.Thread.Sleep(1);
                
            }

            Phase3_Types_Of_Dark_Water_III currentType=Phase3_Types_Of_Dark_Water_III.NONE;

            switch(phase3_roundOfDarkWaterIii) {

                case 1: {

                    currentType=Phase3_Types_Of_Dark_Water_III.SHORT;

                    break;

                }
                
                case 2: {

                    currentType=Phase3_Types_Of_Dark_Water_III.MEDIUM;

                    break;

                }
                
                case 3: {

                    currentType=Phase3_Types_Of_Dark_Water_III.LONG;

                    break;

                }

                default: {
                    // Just a placeholder and should never be reached.

                    return;

                }
                
            }
            
            var currentProperty=accessory.Data.GetDefaultDrawProperties();

            if(Phase3_Strat_Of_The_Second_Half==Phase3_Strats_Of_The_Second_Half.Double_Group_双分组法) {
                
                if(phase3_numberOfDarkWaterIiiHasBeenProcessed==6) {
                    
                    int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                    bool goLeft=phase3_doubleGroup_shouldGoLeft(myIndex);
                    bool stayInTheGroup=phase3_doubleGroup_shouldStayInTheGroup(myIndex);
            
                    for(int i=0;i<8;++i) {

                        if(phase3_typeOfDarkWaterIii[i]==currentType) {
                                        
                            currentProperty=accessory.Data.GetDefaultDrawProperties();
                                        
                            currentProperty.Name="Phase3_Range_Of_Dark_Water_III_黑暗狂水范围";
                            currentProperty.Scale=new(6);
                            currentProperty.Owner=accessory.Data.PartyList[i];
                            currentProperty.DestoryAt=5000;

                            if(phase3_roundOfDarkWaterIii==1||phase3_roundOfDarkWaterIii==3) {
                            
                                if(phase3_doubleGroup_shouldGoLeft(i)==goLeft) {

                                    currentProperty.Color=accessory.Data.DefaultSafeColor;

                                }

                                else {

                                    currentProperty.Color=accessory.Data.DefaultDangerColor;

                                }
                            
                            }

                            if(phase3_roundOfDarkWaterIii==2) {

                                bool endUpWithTheLeftGroup=true;
                                int doubleGroupIndexOfMyMedium=0;

                                if(0<=myIndex&&myIndex<=3) {

                                    endUpWithTheLeftGroup=true;

                                }

                                if(4<=myIndex&&myIndex<=7) {

                                    endUpWithTheLeftGroup=false;

                                }

                                if(!stayInTheGroup) {

                                    endUpWithTheLeftGroup=(!endUpWithTheLeftGroup);

                                }

                                if(endUpWithTheLeftGroup) {

                                    for(doubleGroupIndexOfMyMedium=0;
                                        phase3_typeOfDarkWaterIii[phase3_doubleGroup_priority_asAConstant[doubleGroupIndexOfMyMedium]]!=Phase3_Types_Of_Dark_Water_III.MEDIUM 
                                        && 
                                        doubleGroupIndexOfMyMedium<8;
                                        ++doubleGroupIndexOfMyMedium);

                                }

                                else {
                                
                                    for(doubleGroupIndexOfMyMedium=7;
                                        phase3_typeOfDarkWaterIii[phase3_doubleGroup_priority_asAConstant[doubleGroupIndexOfMyMedium]]!=Phase3_Types_Of_Dark_Water_III.MEDIUM
                                        &&
                                        doubleGroupIndexOfMyMedium>=0;
                                        --doubleGroupIndexOfMyMedium);
                                
                                }

                                if(doubleGroupIndexOfMyMedium<0||doubleGroupIndexOfMyMedium>7) {
                                
                                    currentProperty.Color=accessory.Data.DefaultDangerColor;
                                
                                }

                                else {
                                
                                    if(phase3_doubleGroup_priority_asAConstant[doubleGroupIndexOfMyMedium]==i) {
                                
                                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                                
                                    }

                                    else {
                                
                                        currentProperty.Color=accessory.Data.DefaultDangerColor;
                                
                                    }
                                
                                }

                            }
                                        
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperty);
                                        
                        }
                                    
                    }

                    return;

                }

            }
            
            if(Phase3_Strat_Of_The_Second_Half==Phase3_Strats_Of_The_Second_Half.High_Priority_As_Locomotives_车头低换法) {
                
                if(phase3_numberOfDarkWaterIiiHasBeenProcessed==6) {
                    
                    bool goLeft=phase3_locomotive_shouldGoLeft(accessory.Data.PartyList.IndexOf(accessory.Data.Me));
            
                    for(int i=0;i<8;++i) {

                        if(phase3_typeOfDarkWaterIii[i]==currentType) {
                                        
                            currentProperty=accessory.Data.GetDefaultDrawProperties();
                                        
                            currentProperty.Name="Phase3_Range_Of_Dark_Water_III_黑暗狂水范围";
                            currentProperty.Scale=new(6);
                            currentProperty.Owner=accessory.Data.PartyList[i];
                            currentProperty.DestoryAt=5000;

                            if(phase3_locomotive_shouldGoLeft(i)==goLeft) {

                                currentProperty.Color=accessory.Data.DefaultSafeColor;

                            }

                            else {

                                currentProperty.Color=accessory.Data.DefaultDangerColor;

                            }
                                        
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperty);
                                        
                        }
                                    
                    }

                    return;

                }

            }

            for(int i=0;i<8;++i) {

                if(phase3_typeOfDarkWaterIii[i]==currentType) {
                                        
                    currentProperty=accessory.Data.GetDefaultDrawProperties();
                                        
                    currentProperty.Name="Phase3_Range_Of_Dark_Water_III_黑暗狂水范围";
                    currentProperty.Scale=new(6);
                    currentProperty.Owner=accessory.Data.PartyList[i];
                    currentProperty.Color=accessory.Data.DefaultDangerColor;
                    currentProperty.DestoryAt=5000;
                                        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperty);
                                        
                }
                                    
            }
                
            if(Enable_Text_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.Method.TextInfo("分摊",2000);
                    
                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.Method.TextInfo("Stack",2000);
                    
                }
                
            }
            
            if (Enable_TTS_Prompts || Enable_DRTTS_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.TTS("分摊", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.TTS("Stack", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }
                
            }

        }

        [ScriptMethod(name:"Phase3 Guidance Of Dark Water III 黑暗狂水(分摊)指路",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:2458"],
            suppress:2000)]
        
        public void Phase3_Guidance_Of_Dark_Water_III_黑暗狂水指路(Event @event, ScriptAccessory accessory) {

            while(System.Threading.Interlocked.CompareExchange(ref phase3_guidanceSemaphoreOfDarkWaterIii,0,1)==0) {
                
                System.Threading.Thread.Sleep(1);
                
            }

            if(phase3_numberOfDarkWaterIiiHasBeenProcessed!=6) {

                return;

            }
            
            bool targetPositionConfirmed=false;
            string prompt = "";
            var currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            currentProperty.Name="Phase3_Guidance_Of_Dark_Water_III_黑暗狂水指路";
            currentProperty.Scale=new(2);
            currentProperty.ScaleMode|=ScaleMode.YByDistance;
            currentProperty.Owner=accessory.Data.Me;
            currentProperty.Color=accessory.Data.DefaultSafeColor;
            currentProperty.DestoryAt=5000;

            if(Phase3_Strat_Of_The_Second_Half==Phase3_Strats_Of_The_Second_Half.Double_Group_双分组法) {
                
                bool goLeft=phase3_doubleGroup_shouldGoLeft(accessory.Data.PartyList.IndexOf(accessory.Data.Me));
                bool stayInTheGroup=phase3_doubleGroup_shouldStayInTheGroup(accessory.Data.PartyList.IndexOf(accessory.Data.Me));

                if(Enable_Developer_Mode) {

                    accessory.Method.SendChat($"""
                                               /e 
                                               goLeft={goLeft}
                                               stayInTheGroup={stayInTheGroup}
                                               phase3_roundOfDarkWaterIii={phase3_roundOfDarkWaterIii}
                                               
                                               """);

                }

                switch(phase3_roundOfDarkWaterIii) {

                    case 1: {
                        
                        currentProperty.TargetPosition=(goLeft)?(new Vector3(93,0,100)):(new Vector3(107,0,100));
                    
                        targetPositionConfirmed=true;

                        if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                            prompt=(goLeft)?("左侧分摊"):("右侧分摊");

                        }
                    
                        if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                            prompt=(goLeft)?("Stack on the left"):("Stack on the right");

                        }

                        break;

                    }
                    
                    case 2: {

                        if(stayInTheGroup) {

                            if(0<=accessory.Data.PartyList.IndexOf(accessory.Data.Me)
                               &&
                               accessory.Data.PartyList.IndexOf(accessory.Data.Me)<=3) {

                                currentProperty.TargetPosition=phase3_doubleGroup_leftPositionToStackOfTheSecondRound;
                                    
                                targetPositionConfirmed=true; 
                                
                            }

                            if(4<=accessory.Data.PartyList.IndexOf(accessory.Data.Me)
                               &&
                               accessory.Data.PartyList.IndexOf(accessory.Data.Me)<=7) {

                                currentProperty.TargetPosition=phase3_doubleGroup_rightPositionToStackOfTheSecondRound;
                                    
                                targetPositionConfirmed=true;
                                    
                            }
                                
                        }

                        else {
                            
                            if(0<=accessory.Data.PartyList.IndexOf(accessory.Data.Me)
                               &&
                               accessory.Data.PartyList.IndexOf(accessory.Data.Me)<=3) {

                                currentProperty.TargetPosition=phase3_doubleGroup_rightPositionToStackOfTheSecondRound;
                                    
                                targetPositionConfirmed=true; 
                                
                            }

                            if(4<=accessory.Data.PartyList.IndexOf(accessory.Data.Me)
                               &&
                               accessory.Data.PartyList.IndexOf(accessory.Data.Me)<=7) {

                                currentProperty.TargetPosition=phase3_doubleGroup_leftPositionToStackOfTheSecondRound;
                                    
                                targetPositionConfirmed=true;
                                    
                            }
                            
                        }
                            
                        if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                            prompt=(stayInTheGroup)?("本组分摊"):("对组分摊");

                        }
                    
                        if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                            prompt=(stayInTheGroup)?("Stack in the current group"):("Stack in the opposite group");

                        }

                        break;

                    }

                    case 3: {
                        
                        if(ParseObjectId(phase3_bossId, out var bossId)) {

                            var bossObject=accessory.Data.Objects.SearchById(bossId);

                            if(bossObject!=null) {
                                
                                float currentRotation=bossObject.Rotation;
                                currentRotation=-(currentRotation-float.Pi);

                                Vector3 groupPosition=new Vector3(100,0,100);
            
                                if(Enable_Developer_Mode) {

                                    accessory.Method.SendChat($"""
                                                               /e 
                                                               currentRotation={currentRotation}

                                                               """);

                                }

                                if(goLeft) {

                                    groupPosition=new Vector3(bossObject.Position.X-6.89f,
                                                              bossObject.Position.Y,
                                                              bossObject.Position.Z+6.89f);

                                }

                                else {
                                    
                                    groupPosition=new Vector3(bossObject.Position.X+6.89f,
                                                              bossObject.Position.Y,
                                                              bossObject.Position.Z+6.89f);
                                    
                                }

                                groupPosition=RotatePoint(groupPosition,bossObject.Position,currentRotation);
                                
                                currentProperty.TargetPosition=groupPosition;
                                
                                targetPositionConfirmed=true;

                            }

                        }
                        
                        if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                                
                            prompt=(goLeft)?("左侧分摊"):("右侧分摊");
                                
                        }
                            
                        if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                                
                            prompt=(goLeft)?("Stack on the left"):("Stack on the right");
                                
                        }

                        break;

                    }

                    default: {
                        // Just a placeholder and should never be reached.

                        break;

                    }
                    
                }
                
            }
            
            if(Phase3_Strat_Of_The_Second_Half==Phase3_Strats_Of_The_Second_Half.High_Priority_As_Locomotives_车头低换法) {
                
                bool goLeft=phase3_locomotive_shouldGoLeft(accessory.Data.PartyList.IndexOf(accessory.Data.Me));

                if(Enable_Developer_Mode) {

                    accessory.Method.SendChat($"""
                                               /e 
                                               goLeft={goLeft}
                                               phase3_roundOfDarkWaterIii={phase3_roundOfDarkWaterIii}
                                               
                                               """);

                }

                switch(phase3_roundOfDarkWaterIii) {

                    case 1: {
                        
                        currentProperty.TargetPosition=(goLeft)?(new Vector3(93,0,100)):(new Vector3(107,0,100));
                    
                        targetPositionConfirmed=true;

                        if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                            prompt= (goLeft)?("左侧分摊"):("右侧分摊");

                        }
                    
                        if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                            prompt=(goLeft)?("Stack on the left"):("Stack on the right");

                        }

                        break;

                    }
                    
                    case 2: {
                        
                        currentProperty.TargetPosition=(goLeft)?
                            (phase3_locomotive_leftPositionToStackOfTheSecondRound):
                            (phase3_locomotive_rightPositionToStackOfTheSecondRound);
                    
                        targetPositionConfirmed=true;

                        if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                            prompt="场中当前侧分摊";

                        }
                    
                        if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                            prompt="Stack on this side of the center";

                        }

                        break;

                    }

                    case 3: {
                        
                        if(ParseObjectId(phase3_bossId, out var bossId)) {

                            var bossObject=accessory.Data.Objects.SearchById(bossId);

                            if(bossObject!=null) {
                                
                                float currentRotation=bossObject.Rotation;
                                currentRotation=-(currentRotation-float.Pi);

                                Vector3 groupPosition=new Vector3(100,0,100);
            
                                if(Enable_Developer_Mode) {

                                    accessory.Method.SendChat($"""
                                                               /e 
                                                               currentRotation={currentRotation}

                                                               """);

                                }

                                if(goLeft) {

                                    groupPosition=new Vector3(bossObject.Position.X-6.89f,
                                                              bossObject.Position.Y,
                                                              bossObject.Position.Z+6.89f);

                                }

                                else {
                                    
                                    groupPosition=new Vector3(bossObject.Position.X+6.89f,
                                                              bossObject.Position.Y,
                                                              bossObject.Position.Z+6.89f);
                                    
                                }

                                groupPosition=RotatePoint(groupPosition,bossObject.Position,currentRotation);
                                
                                currentProperty.TargetPosition=groupPosition;
                                
                                targetPositionConfirmed=true;

                            }

                        }
                        
                        if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                                
                            prompt=(goLeft)?("左侧分摊"):("右侧分摊");
                                
                        }
                            
                        if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                                
                            prompt=(goLeft)?("Stack on the left"):("Stack on the right");
                                
                        }

                        break;

                    }

                    default: {
                        // Just a placeholder and should never be reached.

                        break;

                    }
                    
                }
                
            }
            
            if(targetPositionConfirmed) {

                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                
            }
            
            if(!prompt.Equals("")) {
                    
                if(Enable_Text_Prompts) {

                    accessory.Method.TextInfo(prompt, 2500);
                
                }

                accessory.TTS($"{prompt}", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

            }
            
        }

        private bool phase3_doubleGroup_shouldStayInTheGroup(int currentIndex) {

            bool inTheLeftGroup=true;
            
            if(0<=currentIndex&&currentIndex<=3) {

                inTheLeftGroup=true;

            }

            if(4<=currentIndex&&currentIndex<=7) {

                inTheLeftGroup=false;

            }

            if(inTheLeftGroup==phase3_doubleGroup_shouldGoLeft(currentIndex)) {

                return true;

            }

            else {

                return false;

            }

        }

        private bool phase3_doubleGroup_shouldGoLeft(int currentIndex) {

            if(currentIndex<0||currentIndex>7) {

                return true;

            }
            
            int doubleGroupIndex=phase3_doubleGroup_getDoubleGroupIndex(currentIndex);
            Phase3_Types_Of_Dark_Water_III currentType=phase3_typeOfDarkWaterIii[currentIndex];
            bool goLeft=true;

            for(int i=0;i<8;++i) {

                if(phase3_typeOfDarkWaterIii[phase3_doubleGroup_priority_asAConstant[i]]==currentType&&i!=doubleGroupIndex) {

                    if(i>doubleGroupIndex) {

                        goLeft=true;
                        // Should go left.

                        break;

                    }

                    if(i<doubleGroupIndex) {

                        goLeft=false;
                        // Should go right.

                        break;

                    }
                        
                }
                    
            }

            return goLeft;

        }

        private int phase3_doubleGroup_getDoubleGroupIndex(int currentIndex) {

            for(int i=0;i<8;++i) {

                if(currentIndex==phase3_doubleGroup_priority_asAConstant[i]) {

                    return i;

                }
                
            }

            return currentIndex;
            // Just a placeholder and should never be reached.

        }
        
        private bool phase3_locomotive_shouldGoLeft(int currentIndex) {

            if(currentIndex<0||currentIndex>7) {

                return true;

            }
            
            int locomotiveIndex=phase3_locomotive_getLocomotiveIndex(currentIndex);
            Phase3_Types_Of_Dark_Water_III currentType=phase3_typeOfDarkWaterIii[currentIndex];
            bool goLeft=true;

            for(int i=0;i<8;++i) {

                if(phase3_typeOfDarkWaterIii[phase3_locomotive_priority_asAConstant[i]]==currentType&&i!=locomotiveIndex) {

                    if(i>locomotiveIndex) {

                        goLeft=true;
                        // Should go left.

                        break;

                    }

                    if(i<locomotiveIndex) {

                        goLeft=false;
                        // Should go right.

                        break;

                    }
                        
                }
                    
            }

            return goLeft;

        }
        
        private int phase3_locomotive_getLocomotiveIndex(int currentIndex) {

            for(int i=0;i<8;++i) {

                if(currentIndex==phase3_locomotive_priority_asAConstant[i]) {

                    return i;

                }
                
            }

            return currentIndex;
            // Just a placeholder and should never be reached.

        }
        
        [ScriptMethod(name:"Phase3 Range Of Spirit Taker 碎灵一击(分散)范围",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:40288"])]
        
        public void Phase3_Range_Of_Spirit_Taker_碎灵一击范围(Event @event,ScriptAccessory accessory) {

            if(parse!=3.2) {

                return;

            }
            
            for(int i=0;i<8;++i) {
                
                var currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                currentProperty.Name="Phase3_Range_Of_Spirit_Taker_碎灵一击范围";
                currentProperty.Scale=new(5);
                currentProperty.Owner=accessory.Data.PartyList[i];
                currentProperty.Color=accessory.Data.DefaultDangerColor;
                currentProperty.Delay=1250;
                currentProperty.DestoryAt=2500;
                
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperty);
                
            }
            
            System.Threading.Thread.Sleep(1000);
            
            if(Enable_Text_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.Method.TextInfo("分散",2000);
                    
                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.Method.TextInfo("Spread",2000);
                    
                }
                
            }
            
            if (Enable_TTS_Prompts || Enable_DRTTS_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.TTS("分散", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.TTS("Spread", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }
                
            }

        }
        
        [ScriptMethod(name:"Phase3 Guidance Of Spirit Taker 碎灵一击(分散)指路",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:40288"])]
        
        public void Phase3_Guidance_Of_Spirit_Taker_碎灵一击指路(Event @event,ScriptAccessory accessory) {

            if(parse!=3.2) {

                return;

            }

            bool targetPositionConfirmed=false;
            var currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            currentProperty.Name="Phase3_Guidance_Of_Spirit_Taker_碎灵一击指路";
            currentProperty.Scale=new(2);
            currentProperty.ScaleMode|=ScaleMode.YByDistance;
            currentProperty.Owner=accessory.Data.Me;
            currentProperty.Color=accessory.Data.DefaultSafeColor;
            currentProperty.Delay=1250;
            currentProperty.DestoryAt=2500;

            if(Phase3_Strat_Of_The_Second_Half==Phase3_Strats_Of_The_Second_Half.Double_Group_双分组法) {

                int myDoubleGroupIndex=phase3_doubleGroup_getDoubleGroupIndex(accessory.Data.PartyList.IndexOf(accessory.Data.Me));

                switch(myDoubleGroupIndex) {

                    case 0: {
                        // H1
                        
                        currentProperty.TargetPosition=new Vector3(85,0,100);
                        targetPositionConfirmed=true;

                        break;
                        
                    }

                    case 1: {
                        // H2
                        
                        bool goLeft=phase3_doubleGroup_shouldGoLeft(accessory.Data.PartyList.IndexOf(accessory.Data.Me));

                        if(Enable_Developer_Mode) {

                            accessory.Method.SendChat($"""
                                                       /e 
                                                       goLeft={goLeft}

                                                       """);

                        }
                        
                        currentProperty.TargetPosition=(goLeft)?(new Vector3(93,0,92)):(new Vector3(107,0,92));
                        targetPositionConfirmed=true;

                        break;

                    }

                    case 2: {
                        // MT
                        
                        currentProperty.TargetPosition=new Vector3(100,0,92);
                        targetPositionConfirmed=true;

                        break;
                        
                    }
                    
                    case 3: {
                        // OT or ST
                        
                        currentProperty.TargetPosition=new Vector3(100,0,100);
                        targetPositionConfirmed=true;

                        break;
                        
                    }
                    
                    case 4: {
                        // M1 or D1
                        
                        bool goLeft=phase3_doubleGroup_shouldGoLeft(accessory.Data.PartyList.IndexOf(accessory.Data.Me));

                        if(Enable_Developer_Mode) {

                            accessory.Method.SendChat($"""
                                                       /e 
                                                       goLeft={goLeft}

                                                       """);

                        }
                        
                        currentProperty.TargetPosition=(goLeft)?(new Vector3(93,0,100)):(new Vector3(107,0,100));
                        targetPositionConfirmed=true;

                        break;
                        
                    }
                    
                    case 5: {
                        // M2 or D2
                        
                        currentProperty.TargetPosition=new Vector3(100,0,108);
                        targetPositionConfirmed=true;

                        break;
                        
                    }
                    
                    case 6: {
                        // R1 or D3
                        
                        bool goLeft=phase3_doubleGroup_shouldGoLeft(accessory.Data.PartyList.IndexOf(accessory.Data.Me));

                        if(Enable_Developer_Mode) {

                            accessory.Method.SendChat($"""
                                                       /e 
                                                       goLeft={goLeft}

                                                       """);

                        }
                        
                        currentProperty.TargetPosition=(goLeft)?(new Vector3(93,0,108)):(new Vector3(107,0,108));
                        targetPositionConfirmed=true;

                        break;
                        
                    }
                    
                    case 7: {
                        // R2 or D4
                        
                        currentProperty.TargetPosition=new Vector3(115,0,100);
                        targetPositionConfirmed=true;

                        break;
                        
                    }
                    
                    default: {
                        // Just a placeholder and should never be reached.

                        break;
                        
                    }
                    
                }

            }

            if(targetPositionConfirmed) {

                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                
            }

        }
        
        [ScriptMethod(name:"Phase3 Determine Initial Safe Positions Of Apocalypse 确定启示(地火)初始安全位置",
            eventType:EventTypeEnum.ObjectEffect,
            eventCondition:["Id1:4","Id2:regex:^(16|64)$"],
            userControl:false,
            suppress:2000)]
        
        public void Phase3_Determine_Initial_Safe_Positions_Of_Apocalypse_确定启示初始安全位置(Event @event, ScriptAccessory accessory) {

            if(parse!=3.2) {
                
                return;
                
            }

            if(phase3_hasConfirmedInitialSafePositions) {
                    
                return;
                    
            }
            
            Vector3 position1OfTheSecond=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            Vector3 position2OfTheSecond = RotatePoint(position1OfTheSecond,new Vector3(100,0,100),float.Pi);
            int clockwise=(@event["Id2"].Equals("64"))?(-1):(1);
            Vector3 position1OfTheLast=RotatePoint(position1OfTheSecond,new Vector3(100,0,100),float.Pi/4*3*clockwise);
            Vector3 position2OfTheLast=RotatePoint(position1OfTheSecond,new Vector3(100,0,100),float.Pi/4*3*clockwise+float.Pi);
            Vector3 position1OfThePenultimate=RotatePoint(position1OfTheSecond,new Vector3(100,0,100),float.Pi/2*clockwise);
            Vector3 position2OfThePenultimate=RotatePoint(position1OfTheSecond,new Vector3(100,0,100),float.Pi/2*clockwise+float.Pi);
            int direction1OfTheLast=PositionTo8Dir(position1OfTheLast,new Vector3(100,0,100));
            int direction1OfThePenultimate=PositionTo8Dir(position1OfThePenultimate,new Vector3(100,0,100));
            int direction1OfTheSecond=PositionTo8Dir(position1OfTheSecond,new Vector3(100,0,100));
            
            if(Enable_Developer_Mode) {

                accessory.Method.SendChat($"""
                                           /e 
                                           position1OfTheLast={position1OfTheLast}
                                           position2OfTheLast={position2OfTheLast}
                                           clockwise={clockwise}
                                           position1OfThePenultimate={position1OfThePenultimate}
                                           position2OfThePenultimate={position2OfThePenultimate}
                                           position1OfTheSecond={position1OfTheSecond}
                                           position2OfTheSecond={position2OfTheSecond}
                                           direction1OfTheLast={direction1OfTheLast}
                                           direction1OfThePenultimate={direction1OfThePenultimate}
                                           direction1OfTheSecond={direction1OfTheSecond}
                                           
                                           """);

            }

            if(Phase3_Strat_Of_The_Second_Half==Phase3_Strats_Of_The_Second_Half.Double_Group_双分组法) {

                if(Phase3_Branch_Of_The_Double_Group_Strat==Phase3_Branches_Of_The_Double_Group_Strat.Based_On_Safe_Positions_安全区为基准) {

                    if(Phase3_Division_Of_The_Zone==Phase3_Divisions_Of_The_Zone.North_To_Southwest_For_The_Left_Group_左组从正北到西南) {

                        if(direction1OfTheLast==0
                           ||
                           direction1OfTheLast==7
                           ||
                           direction1OfTheLast==6
                           ||
                           direction1OfTheLast==5) {
                            
                            phase3_doubleGroup_initialSafePositionOfTheLeftGroup=position1OfTheLast;
                            phase3_doubleGroup_leftPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                               position1OfTheLast.Y,
                                                                                               (position1OfTheLast.Z-100)/3+100);
                            phase3_doubleGroup_initialSafePositionOfTheRightGroup=position2OfTheLast;
                            phase3_doubleGroup_rightPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                                position2OfTheLast.Y,
                                                                                                (position2OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                        if(direction1OfTheLast==1
                           ||
                           direction1OfTheLast==2
                           ||
                           direction1OfTheLast==3
                           ||
                           direction1OfTheLast==4) {
                            
                            phase3_doubleGroup_initialSafePositionOfTheLeftGroup=position2OfTheLast;
                            phase3_doubleGroup_leftPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                               position2OfTheLast.Y,
                                                                                               (position2OfTheLast.Z-100)/3+100);
                            phase3_doubleGroup_initialSafePositionOfTheRightGroup=position1OfTheLast;
                            phase3_doubleGroup_rightPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                                position1OfTheLast.Y,
                                                                                                (position1OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                    }

                    if(Phase3_Division_Of_The_Zone==Phase3_Divisions_Of_The_Zone.Northwest_To_South_For_The_Left_Group_左组从西北到正南) {

                        if(direction1OfTheLast==7
                           ||
                           direction1OfTheLast==6
                           ||
                           direction1OfTheLast==5
                           ||
                           direction1OfTheLast==4) {
                            
                            phase3_doubleGroup_initialSafePositionOfTheLeftGroup=position1OfTheLast;
                            phase3_doubleGroup_leftPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100, 
                                                                                               position1OfTheLast.Y,
                                                                                               (position1OfTheLast.Z-100)/3+100);
                            phase3_doubleGroup_initialSafePositionOfTheRightGroup=position2OfTheLast;
                            phase3_doubleGroup_rightPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                                position2OfTheLast.Y,
                                                                                                (position2OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                        if(direction1OfTheLast==0
                           ||
                           direction1OfTheLast==1
                           ||
                           direction1OfTheLast==2
                           ||
                           direction1OfTheLast==3) {
                            
                            phase3_doubleGroup_initialSafePositionOfTheLeftGroup=position2OfTheLast;
                            phase3_doubleGroup_leftPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                               position2OfTheLast.Y,
                                                                                               (position2OfTheLast.Z-100)/3+100);
                            phase3_doubleGroup_initialSafePositionOfTheRightGroup=position1OfTheLast;
                            phase3_doubleGroup_rightPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                                position1OfTheLast.Y,
                                                                                                (position1OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                    }

                }
                
                if(Phase3_Branch_Of_The_Double_Group_Strat==Phase3_Branches_Of_The_Double_Group_Strat.Based_On_The_Second_Apocalypse_第二次启示为基准) {

                    if(Phase3_Division_Of_The_Zone==Phase3_Divisions_Of_The_Zone.North_To_Southwest_For_The_Left_Group_左组从正北到西南) {

                        if(direction1OfTheSecond==0
                           ||
                           direction1OfTheSecond==7
                           ||
                           direction1OfTheSecond==6
                           ||
                           direction1OfTheSecond==5) {
                            
                            phase3_doubleGroup_initialSafePositionOfTheLeftGroup=position2OfTheLast;
                            phase3_doubleGroup_leftPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                               position2OfTheLast.Y,
                                                                                               (position2OfTheLast.Z-100)/3+100);
                            phase3_doubleGroup_initialSafePositionOfTheRightGroup=position1OfTheLast;
                            phase3_doubleGroup_rightPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                                position1OfTheLast.Y,
                                                                                                (position1OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                        if(direction1OfTheSecond==1
                           ||
                           direction1OfTheSecond==2
                           ||
                           direction1OfTheSecond==3
                           ||
                           direction1OfTheSecond==4) {
                            
                            phase3_doubleGroup_initialSafePositionOfTheLeftGroup=position1OfTheLast;
                            phase3_doubleGroup_leftPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                               position1OfTheLast.Y,
                                                                                               (position1OfTheLast.Z-100)/3+100);
                            phase3_doubleGroup_initialSafePositionOfTheRightGroup=position2OfTheLast;
                            phase3_doubleGroup_rightPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                                position2OfTheLast.Y,
                                                                                                (position2OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                    }

                    if(Phase3_Division_Of_The_Zone==Phase3_Divisions_Of_The_Zone.Northwest_To_South_For_The_Left_Group_左组从西北到正南) {

                        if(direction1OfTheSecond==7
                           ||
                           direction1OfTheSecond==6
                           ||
                           direction1OfTheSecond==5
                           ||
                           direction1OfTheSecond==4) {
                            
                            phase3_doubleGroup_initialSafePositionOfTheLeftGroup=position2OfTheLast;
                            phase3_doubleGroup_leftPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                               position2OfTheLast.Y,
                                                                                               (position2OfTheLast.Z-100)/3+100);
                            phase3_doubleGroup_initialSafePositionOfTheRightGroup=position1OfTheLast;
                            phase3_doubleGroup_rightPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                                position1OfTheLast.Y,
                                                                                                (position1OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                        if(direction1OfTheSecond==0
                           ||
                           direction1OfTheSecond==1
                           ||
                           direction1OfTheSecond==2
                           ||
                           direction1OfTheSecond==3) {
                            
                            phase3_doubleGroup_initialSafePositionOfTheLeftGroup=position1OfTheLast;
                            phase3_doubleGroup_leftPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                               position1OfTheLast.Y,
                                                                                               (position1OfTheLast.Z-100)/3+100);
                            phase3_doubleGroup_initialSafePositionOfTheRightGroup=position2OfTheLast;
                            phase3_doubleGroup_rightPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                                position2OfTheLast.Y,
                                                                                                (position2OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                    }

                }

            }
            
            if(Phase3_Strat_Of_The_Second_Half==Phase3_Strats_Of_The_Second_Half.High_Priority_As_Locomotives_车头低换法) {

                if(Phase3_Branch_Of_The_Locomotive_Strat==Phase3_Branches_Of_The_Locomotive_Strat.MT_And_M1_As_Locomotives_MT和D1为车头) {

                    if(Phase3_Division_Of_The_Zone==Phase3_Divisions_Of_The_Zone.North_To_Southwest_For_The_Left_Group_左组从正北到西南) {

                        if(direction1OfThePenultimate==0 
                           ||
                           direction1OfThePenultimate==7
                           ||
                           direction1OfThePenultimate==6
                           ||
                           direction1OfThePenultimate==5) {

                            phase3_locomotive_initialSafePositionOfTheLeftGroup=position1OfThePenultimate;
                            phase3_locomotive_leftPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                              position1OfTheLast.Y,
                                                                                              (position1OfTheLast.Z-100)/3+100);
                            phase3_locomotive_initialSafePositionOfTheRightGroup=position2OfThePenultimate;
                            phase3_locomotive_rightPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                               position2OfTheLast.Y,
                                                                                               (position2OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                        if (direction1OfThePenultimate==1
                            ||
                            direction1OfThePenultimate==2
                            ||
                            direction1OfThePenultimate==3
                            ||
                            direction1OfThePenultimate==4) {

                            phase3_locomotive_initialSafePositionOfTheLeftGroup=position2OfThePenultimate;
                            phase3_locomotive_leftPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                              position2OfTheLast.Y,
                                                                                              (position2OfTheLast.Z-100)/3+100);
                            phase3_locomotive_initialSafePositionOfTheRightGroup=position1OfThePenultimate;
                            phase3_locomotive_rightPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                               position1OfTheLast.Y,
                                                                                               (position1OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                    }

                    if(Phase3_Division_Of_The_Zone==Phase3_Divisions_Of_The_Zone.Northwest_To_South_For_The_Left_Group_左组从西北到正南) {

                        if (direction1OfThePenultimate==7
                            ||
                            direction1OfThePenultimate==6
                            ||
                            direction1OfThePenultimate==5
                            ||
                            direction1OfThePenultimate==4) {

                            phase3_locomotive_initialSafePositionOfTheLeftGroup=position1OfThePenultimate;
                            phase3_locomotive_leftPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                              position1OfTheLast.Y,
                                                                                              (position1OfTheLast.Z-100)/3+100);
                            phase3_locomotive_initialSafePositionOfTheRightGroup=position2OfThePenultimate;
                            phase3_locomotive_rightPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                               position2OfTheLast.Y,
                                                                                               (position2OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                        if (direction1OfThePenultimate==0
                            ||
                            direction1OfThePenultimate==1
                            ||
                            direction1OfThePenultimate==2
                            ||
                            direction1OfThePenultimate==3) {

                            phase3_locomotive_initialSafePositionOfTheLeftGroup=position2OfThePenultimate;
                            phase3_locomotive_leftPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                              position2OfTheLast.Y,
                                                                                              (position2OfTheLast.Z-100)/3+100);
                            phase3_locomotive_initialSafePositionOfTheRightGroup=position1OfThePenultimate;
                            phase3_locomotive_rightPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                               position1OfTheLast.Y,
                                                                                               (position1OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                    }

                }

                if(Phase3_Branch_Of_The_Locomotive_Strat==Phase3_Branches_Of_The_Locomotive_Strat.Others_As_Locomotives_人群为车头) {
                    
                    if(Phase3_Division_Of_The_Zone==Phase3_Divisions_Of_The_Zone.North_To_Southwest_For_The_Left_Group_左组从正北到西南) {

                        if(direction1OfTheLast==0
                           ||
                           direction1OfTheLast==7
                           ||
                           direction1OfTheLast==6
                           ||
                           direction1OfTheLast==5) {
                            
                            phase3_locomotive_initialSafePositionOfTheLeftGroup=position1OfTheLast;
                            phase3_locomotive_leftPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                              position1OfTheLast.Y,
                                                                                              (position1OfTheLast.Z-100)/3+100);
                            phase3_locomotive_initialSafePositionOfTheRightGroup=position2OfTheLast;
                            phase3_locomotive_rightPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                               position2OfTheLast.Y,
                                                                                               (position2OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                        if(direction1OfTheLast==1
                           ||
                           direction1OfTheLast==2
                           ||
                           direction1OfTheLast==3
                           ||
                           direction1OfTheLast==4) {
                            
                            phase3_locomotive_initialSafePositionOfTheLeftGroup=position2OfTheLast;
                            phase3_locomotive_leftPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                              position2OfTheLast.Y,
                                                                                              (position2OfTheLast.Z-100)/3+100);
                            phase3_locomotive_initialSafePositionOfTheRightGroup=position1OfTheLast;
                            phase3_locomotive_rightPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                               position1OfTheLast.Y,
                                                                                               (position1OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                    }

                    if(Phase3_Division_Of_The_Zone==Phase3_Divisions_Of_The_Zone.Northwest_To_South_For_The_Left_Group_左组从西北到正南) {

                        if(direction1OfTheLast==7
                           ||
                           direction1OfTheLast==6
                           ||
                           direction1OfTheLast==5
                           ||
                           direction1OfTheLast==4) {
                            
                            phase3_locomotive_initialSafePositionOfTheLeftGroup=position1OfTheLast;
                            phase3_locomotive_leftPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                              position1OfTheLast.Y,
                                                                                              (position1OfTheLast.Z-100)/3+100);
                            phase3_locomotive_initialSafePositionOfTheRightGroup=position2OfTheLast;
                            phase3_locomotive_rightPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                               position2OfTheLast.Y,
                                                                                               (position2OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                        if(direction1OfTheLast==0
                           ||
                           direction1OfTheLast==1
                           ||
                           direction1OfTheLast==2
                           ||
                           direction1OfTheLast==3) {
                            
                            phase3_locomotive_initialSafePositionOfTheLeftGroup=position2OfTheLast;
                            phase3_locomotive_leftPositionToStackOfTheSecondRound=new Vector3((position2OfTheLast.X-100)/3+100,
                                                                                              position2OfTheLast.Y,
                                                                                              (position2OfTheLast.Z-100)/3+100);
                            phase3_locomotive_initialSafePositionOfTheRightGroup=position1OfTheLast;
                            phase3_locomotive_rightPositionToStackOfTheSecondRound=new Vector3((position1OfTheLast.X-100)/3+100,
                                                                                               position1OfTheLast.Y,
                                                                                               (position1OfTheLast.Z-100)/3+100);

                            phase3_hasConfirmedInitialSafePositions=true;

                        }

                    }
                    
                }

            }

        }
        
        [ScriptMethod(name: "P3_延迟咏唱回响_地火", eventType: EventTypeEnum.ObjectEffect, eventCondition: ["Id1:4", "Id2:regex:^(16|64)$"], suppress: 2000)]
        public void P3_延迟咏唱回响_地火(Event @event, ScriptAccessory accessory)
        {
            if (parse != 3.2) return;
            if (P3FloorFireDone) return;
            P3FloorFireDone = true;
            Vector3 centre = new(100, 0, 100);
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            var clockwise = @event["Id2"] == "64" ? -1 : 1;
            var preTime = 100;
            //间隔11 2 2 2 2 2

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_中心";
            dp.Scale = new(9);
            dp.Position = centre;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 3000;
            dp.DestoryAt = 9700;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_起始点_11";
            dp.Scale = new(9);
            dp.Position = pos;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 3000;
            dp.DestoryAt = 12000 - preTime;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_起始点_12";
            dp.Scale = new(9);
            dp.Position = pos;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 17000 - preTime;
            dp.DestoryAt = 4000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_起始点_21";
            dp.Scale = new(9);
            dp.Position = RotatePoint(pos, centre, float.Pi);
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 3000;
            dp.DestoryAt = 12000 - preTime;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_起始点_22";
            dp.Scale = new(9);
            dp.Position = RotatePoint(pos, centre, float.Pi);
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 17000 - preTime;
            dp.DestoryAt = 4000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_第二点_11";
            dp.Scale = new(9);
            dp.Position = RotatePoint(pos, centre, float.Pi / 4 * clockwise);
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 3000;
            dp.DestoryAt = 14000 - preTime;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_第二点_12";
            dp.Scale = new(9);
            dp.Position = RotatePoint(pos, centre, float.Pi / 4 * clockwise);
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 19000 - preTime;
            dp.DestoryAt = 2000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_第二点_21";
            dp.Scale = new(9);
            dp.Position = RotatePoint(pos, centre, float.Pi / 4 * clockwise + float.Pi);
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 3000;
            dp.DestoryAt = 14000 - preTime;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_第二点_22";
            dp.Scale = new(9);
            dp.Position = RotatePoint(pos, centre, float.Pi / 4 * clockwise + float.Pi);
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 19000 - preTime;
            dp.DestoryAt = 2000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_第三点_11";
            dp.Scale = new(9);
            dp.Position = RotatePoint(pos, centre, float.Pi / 2 * clockwise);
            dp.Color = Phase3_Colour_Of_Rough_Guidance_And_The_Penultimate_Apocalypse.V4;
            dp.Delay = 3000;
            dp.DestoryAt = 8000 - preTime;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_第三点_12";
            dp.Scale = new(9);
            dp.Position = RotatePoint(pos, centre, float.Pi / 2 * clockwise);
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 11000 - preTime;
            dp.DestoryAt = 8000 - preTime;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_第三点_21";
            dp.Scale = new(9);
            dp.Position = RotatePoint(pos, centre, float.Pi / 2 * clockwise + float.Pi);
            dp.Color = Phase3_Colour_Of_Rough_Guidance_And_The_Penultimate_Apocalypse.V4;
            dp.Delay = 3000;
            dp.DestoryAt = 8000 - preTime;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_第三点_22";
            dp.Scale = new(9);
            dp.Position = RotatePoint(pos, centre, float.Pi / 2 * clockwise + float.Pi);
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 11000 - preTime;
            dp.DestoryAt = 8000 - preTime;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_第四点_11";
            dp.Scale = new(9);
            dp.Position = RotatePoint(pos, centre, float.Pi / 4 * 3 * clockwise);
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 15000 - preTime;
            dp.DestoryAt = 6000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_二运_地火_第四点_21";
            dp.Scale = new(9);
            dp.Position = RotatePoint(pos, centre, float.Pi / 4 * 3 * clockwise + float.Pi);
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 15000 - preTime;
            dp.DestoryAt = 6000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

        }
        
        [ScriptMethod(name:"Phase3 Rough Guidance Of Initial Safe Positions 初始安全位置粗略指路",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:40289"])]
        
        public void Phase3_Rough_Guidance_Of_Initial_Safe_Positions_初始安全位置粗略指路(Event @event,ScriptAccessory accessory) {

            if(parse!=3.2) {

                return;

            }

            bool targetPositionConfirmed=false;
            var currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            currentProperty.Name="Phase3_Rough_Guidance_Of_Initial_Safe_Positions_初始安全位置粗略指路";
            currentProperty.Scale=new(2);
            currentProperty.ScaleMode|=ScaleMode.YByDistance;
            currentProperty.Owner=accessory.Data.Me;
            currentProperty.Color=Phase3_Colour_Of_Rough_Guidance_And_The_Penultimate_Apocalypse.V4;
            currentProperty.Delay=500;
            currentProperty.DestoryAt=6500;

            if(Phase3_Strat_Of_The_Second_Half==Phase3_Strats_Of_The_Second_Half.Double_Group_双分组法) {

                if(0<=accessory.Data.PartyList.IndexOf(accessory.Data.Me)
                   &&
                   accessory.Data.PartyList.IndexOf(accessory.Data.Me)<=3) {

                    currentProperty.TargetPosition=phase3_doubleGroup_initialSafePositionOfTheLeftGroup;
                    
                    targetPositionConfirmed=true;
                    
                }
                
                if(4<=accessory.Data.PartyList.IndexOf(accessory.Data.Me)
                   &&
                   accessory.Data.PartyList.IndexOf(accessory.Data.Me)<=7) {

                    currentProperty.TargetPosition=phase3_doubleGroup_initialSafePositionOfTheRightGroup;
                    
                    targetPositionConfirmed=true;
                    
                }

            }
            
            if(Phase3_Strat_Of_The_Second_Half==Phase3_Strats_Of_The_Second_Half.High_Priority_As_Locomotives_车头低换法) {

                bool goLeft=phase3_locomotive_shouldGoLeft(accessory.Data.PartyList.IndexOf(accessory.Data.Me));

                if(goLeft) {

                    currentProperty.TargetPosition=phase3_locomotive_initialSafePositionOfTheLeftGroup;
                    
                    targetPositionConfirmed=true;
                    
                }

                else {

                    currentProperty.TargetPosition=phase3_locomotive_initialSafePositionOfTheRightGroup;
                    
                    targetPositionConfirmed=true;
                    
                }

            }

            if(targetPositionConfirmed) {

                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                
            }

        }
        
        [ScriptMethod(name:"Phase3 Range Of Darkest Dance 暗夜舞蹈(最远死刑)范围",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:40181"])]
        
        public void Phase3_Range_Of_Darkest_Dance_暗夜舞蹈范围(Event @event, ScriptAccessory accessory) {
            
            if(parse!=3.2) {
                
                return;
                
            }

            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {

                return;
                
            }

            bool goBait=false;

            if(Phase3_Tank_Who_Baits_Darkest_Dance==Tanks.MT
               &&
               accessory.Data.PartyList.IndexOf(accessory.Data.Me)==0) { 
                
                goBait=true;

            }

            if(Phase3_Tank_Who_Baits_Darkest_Dance==Tanks.OT_ST
               &&
               accessory.Data.PartyList.IndexOf(accessory.Data.Me)==1) {
                
                goBait=true;

            }
            
            var currentProperty=accessory.Data.GetDefaultDrawProperties();

            currentProperty.Name="Phase3_Range_Of_Darkest_Dance_暗夜舞蹈范围";
            currentProperty.Scale=new(8);
            currentProperty.Owner=sourceId;
            currentProperty.CentreResolvePattern=PositionResolvePatternEnum.PlayerFarestOrder;
            currentProperty.Color=Phase3_Colour_Of_Darkest_Dance.V4.WithW(3f);
            currentProperty.Delay=2200;
            currentProperty.DestoryAt=4000;

            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperty);
            
            System.Threading.Thread.Sleep(2200);
            
            if(goBait) {
                
                if(Enable_Text_Prompts) {

                    if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                        accessory.Method.TextInfo("最远死刑",1500);
                    
                    }

                    if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                        accessory.Method.TextInfo("Stay away and bait",1500);
                    
                    }
                
                }
                
                if (Enable_TTS_Prompts || Enable_DRTTS_Prompts) {

                    if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                        accessory.TTS("最远死刑", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                    }

                    if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                        accessory.TTS("Stay away and bait", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                    }
                
                }

            }

            else {

                if(Phase3_Tank_Who_Baits_Darkest_Dance==Tanks.MT) {
                    
                    if(Enable_Text_Prompts) {

                        if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                            accessory.Method.TextInfo("远离MT",1500);
                    
                        }

                        if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                            accessory.Method.TextInfo("Stay away from MT",1500);
                    
                        }
                
                    }
                    
                    if (Enable_TTS_Prompts || Enable_DRTTS_Prompts) {

                        if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                            accessory.TTS("远离MT", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                        }

                        if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                            accessory.TTS("Stay away from MT", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                        }
                
                    }
                    
                }

                if(Phase3_Tank_Who_Baits_Darkest_Dance==Tanks.OT_ST) {
                    
                    if(Enable_Text_Prompts) {

                        if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                            accessory.Method.TextInfo("远离ST",1500);
                    
                        }

                        if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                            accessory.Method.TextInfo("Stay away from OT",1500);
                    
                        }
                
                    }
                    
                    if (Enable_TTS_Prompts || Enable_DRTTS_Prompts) {

                        if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                            accessory.TTS("远离ST", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                        }

                        if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                            accessory.TTS("Stay away from OT", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                        }
                
                    }
                    
                }

            }

        }
        
        [ScriptMethod(name:"Phase3 Guidance Of Darkest Dance 暗夜舞蹈(最远死刑)指路",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:40181"])]
        
        public void Phase3_Guidance_Of_Darkest_Dance_暗夜舞蹈指路(Event @event, ScriptAccessory accessory) {
            
            if(parse!=3.2) {
                
                return;
                
            }

            var tankWhoBaitsDarkestDance=accessory.Data.Objects.SearchById(accessory.Data.PartyList[1]);
            bool goBait=false;

            if(Phase3_Tank_Who_Baits_Darkest_Dance==Tanks.MT) {

                tankWhoBaitsDarkestDance=accessory.Data.Objects.SearchById(accessory.Data.PartyList[0]);

                if(accessory.Data.PartyList.IndexOf(accessory.Data.Me)==0) {

                    goBait=true;

                }

            }

            if(Phase3_Tank_Who_Baits_Darkest_Dance==Tanks.OT_ST) {

                tankWhoBaitsDarkestDance=accessory.Data.Objects.SearchById(accessory.Data.PartyList[1]);
                
                if(accessory.Data.PartyList.IndexOf(accessory.Data.Me)==1) {

                    goBait=true;

                }

            }

            if(tankWhoBaitsDarkestDance==null) {
                
                return;
                
            }
            
            // ----- Calculations of the position where the tank should bait -----
            // This part was directly inherited from Karlin's script.
            // The algorithm seems to be too mysterious to me, and it definitely works nice.
            // So as a result, except the position was tuned a bit towards the edge, I just keep the part as is.

            var dir8=P3FloorFire%10%4;
            Vector3 posN=new(100,0,86);
            var rot=dir8 switch {
                0=>6,
                1=>7,
                2=>0,
                3=>5
            };
            var pos1=RotatePoint(posN,new(100,0,100),float.Pi/4*rot);
            var pos2=RotatePoint(posN,new(100,0,100),float.Pi/4*rot+float.Pi);
            var dealpos=((pos1-tankWhoBaitsDarkestDance.Position).Length()<(pos2-tankWhoBaitsDarkestDance.Position).Length())?(pos1):(pos2);

            Vector3 positionToBait=new Vector3((dealpos.X-100)/3*4+100,
                                               dealpos.Y,
                                               (dealpos.Z-100)/3*4+100);
            
            // ----- -----
            
            var currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            if(goBait) {
                
                currentProperty.Owner=accessory.Data.Me;
                currentProperty.Color=accessory.Data.DefaultSafeColor;
                
            }

            else {

                if(Phase3_Tank_Who_Baits_Darkest_Dance==Tanks.MT) {

                    currentProperty.Owner=accessory.Data.PartyList[0];
                    currentProperty.Color=Phase3_Colour_Of_Darkest_Dance.V4.WithW(1f);

                }

                else {

                    currentProperty.Owner=accessory.Data.PartyList[1];
                    currentProperty.Color=Phase3_Colour_Of_Darkest_Dance.V4.WithW(1f);

                }

            }
            
            currentProperty.Name="Phase3_Guidance_Of_Darkest_Dance_暗夜舞蹈指路";
            currentProperty.Scale=new(2);
            currentProperty.ScaleMode|=ScaleMode.YByDistance;
            currentProperty.TargetPosition=positionToBait;
            currentProperty.Delay=2200;
            currentProperty.DestoryAt=4000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

        }
        
        [ScriptMethod(name: "P3_延迟咏唱回响_击退提示", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:40182", "TargetIndex:1"])]
        public void P3_延迟咏唱回响_击退提示(Event @event, ScriptAccessory accessory)
        {
            if (parse != 3.2) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_延迟咏唱回响_击退提示1";
            dp.Scale = new(2,21);
            dp.Owner = accessory.Data.Me;
            dp.TargetObject = sid;
            dp.Rotation = float.Pi;
            dp.Color = accessory.Data.DefaultDangerColor.WithW(3);
            dp.DestoryAt = 3000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Displacement, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_延迟咏唱回响_击退提示2";
            dp.Scale = new(2);
            dp.Owner = sid;
            dp.TargetObject = accessory.Data.Me;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultDangerColor.WithW(3);
            dp.DestoryAt = 3000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Displacement, dp);
        }
        [ScriptMethod(name: "P3_延迟咏唱回响_地火记录", eventType: EventTypeEnum.ObjectEffect, eventCondition: ["Id1:4", "Id2:regex:^(16|64)$"],userControl:false)]
        public void P3_延迟咏唱回响_地火记录(Event @event, ScriptAccessory accessory)
        {
            if (parse != 3.2) return;
            lock (this)
            {
                if (P3FloorFire != -1) return;
                Vector3 centre = new(100, 0, 100);
                var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
                P3FloorFire = PositionTo8Dir(pos,new(100,0,100));
                P3FloorFire+= @event["Id2"] == "64" ? 10 : 20;
            }
            
        }
        
        [ScriptMethod(name:"Phase3 Determine The Final Position Of The Boss 确定Boss的最终位置",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:40300"],
            userControl:false)]

        public void Phase3_Determine_The_Final_Position_Of_The_Boss_确定Boss的最终位置(Event @event, ScriptAccessory accessory) {
            
            if(parse!=3.2) {

                return;

            }
            
            phase3_finalPositionOfTheBoss=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            
        }
        
        [ScriptMethod(name:"Phase3 Initial Position Of The Boss In Phase4 P4时Boss的初始位置",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:40300"])]

        public void Phase3_Initial_Position_Of_The_Boss_In_Phase4_P4时Boss的初始位置(Event @event, ScriptAccessory accessory) {
            
            if(parse!=3.2) {

                return;

            }

            if(phase3_finalPositionOfTheBoss.Equals(new Vector3(100,0,100))) {

                return;

            }

            bool inTheNorth=true;
            
            if(phase3_finalPositionOfTheBoss.Z<=100) {

                inTheNorth=false;

            }

            if(phase3_finalPositionOfTheBoss.Z>=100) {

                inTheNorth=true;

            }
            
            var currentProperty=accessory.Data.GetDefaultDrawProperties();
                
            currentProperty.Name="Phase3_Initial_Position_Of_The_Boss_In_Phase4_P4时Boss的初始位置";
            currentProperty.Scale=new(7);
            currentProperty.Position=(inTheNorth)?(new Vector3(100,0,90)):(new Vector3(100,0,110));
            currentProperty.Color=accessory.Data.DefaultSafeColor;
            currentProperty.DestoryAt=9250;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperty);
            
            System.Threading.Thread.Sleep(2000);
            
            if(Enable_Text_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.Method.TextInfo(((inTheNorth)?("Boss即将出现在正北"):("Boss即将出现在正南")),7250);
                    
                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.Method.TextInfo(((inTheNorth)?("The Boss will appear in the north"):("The Boss will appear in the south")),7250);
                    
                }
                
            }

            if (Language_Of_Prompts == Languages_Of_Prompts.Simplified_Chinese_简体中文)
            {

                accessory.TTS($"{((inTheNorth) ? ("Boss即将出现在正北") : ("Boss即将出现在正南"))}",
                                Enable_TTS_Prompts, Enable_DRTTS_Prompts);

            }

            if (Language_Of_Prompts == Languages_Of_Prompts.English_英文)
            {

                accessory.TTS($"{((inTheNorth) ? ("The Boss will appear in the north") : ("The Boss will appear in the south"))}",
                                Enable_TTS_Prompts, Enable_DRTTS_Prompts);

            }

        }

        private int MyLampIndex(int myPartyIndex)
        {
            var nLampIndex = 0;
            for (int i = 0; i < 8; i++)
            {
                if (P3Lamp[i] == 1 && P3Lamp[(i+3)%8]==1 && P3Lamp[(i + 5) % 8] == 1)
                {
                    nLampIndex=i;
                    break;
                }
            }
            if (P3LampDeal==P3LampEmum.MGL)
            {
                //短火
                if (P3FireBuff[myPartyIndex] == 1)
                {    
                    if (myPartyIndex<4)
                    {
                        return (nLampIndex + 4) % 8;
                    }
                    else
                    {
                        var lowIndex = P3FireBuff.LastIndexOf(1);
                        if (lowIndex != myPartyIndex)
                        {
                            return (nLampIndex + 7) % 8;
                        }
                        else
                        {
                            return (nLampIndex + 1) % 8;
                        }
                    }
                    
                }
                //中火
                if (P3FireBuff[myPartyIndex] == 2)
                {
                    if (myPartyIndex < 4) return (nLampIndex + 6) % 8;
                    else return (nLampIndex + 2) % 8;
                }
                //长火
                if (P3FireBuff[myPartyIndex] == 3)
                {
                    if (myPartyIndex<4)
                    {
                        var highIndex = P3FireBuff.IndexOf(3);
                        if (highIndex == myPartyIndex)
                        {
                            return (nLampIndex + 5) % 8;
                        }
                        else
                        {
                            return (nLampIndex + 3) % 8;
                        }
                    }
                    else
                    {
                        return (nLampIndex + 0) % 8;
                    }
                    
                }
                //冰
                if (P3FireBuff[myPartyIndex] == 4)
                {
                    if (myPartyIndex < 4) return (nLampIndex + 4) % 8;
                    else return (nLampIndex + 0) % 8;
                }
            }

            return -1;
        }
        #endregion

        #region P4
        
        [ScriptMethod(name:"----- Phase 4 ----- (No actual meaning for this toggle/此开关无实际意义)",
            eventType:EventTypeEnum.NpcYell,
            eventCondition:["Send these, the homeless, tempest-tost to me",
                            "送来那些无家可归，被风吹雨淋的人"])]
        
        public void Phase4_Placeholder(Event @event, ScriptAccessory accessory) { }
        
        [ScriptMethod(name: "P4_具象化_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:40246"], userControl: false)]
        public void P4_具象化_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 4.1d;
        }
        [ScriptMethod(name: "P4_时间结晶_记忆水晶收集", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:40174"], userControl: false)]
        public void P4_时间结晶_记忆水晶收集(Event @event, ScriptAccessory accessory)
        {
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            P4FragmentId = sid;
        }
        [ScriptMethod(name: "P4_具象化_天光轮回", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:40237"])]
        public void P4_具象化_天光轮回(Event @event, ScriptAccessory accessory)
        {
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_具象化_天光轮回";
            dp.Scale = new(4);
            dp.Owner = sid;
            dp.TargetObject = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 8000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        }
        
        [ScriptMethod(name:"Phase4 Prompt Before Akh Rhai 天光轮回前提示",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:40246"])]
        
        public void Phase4_Prompt_Before_Akh_Rhai_天光轮回前提示(Event @event, ScriptAccessory accessory) {
            
            if(Enable_Text_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.Method.TextInfo("集合并远离未来的碎片",9500);
                    
                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.Method.TextInfo("Get together and stay away from Fragment of Fate",9500);
                    
                }
                
            }
            
            if (Enable_TTS_Prompts || Enable_DRTTS_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.TTS("集合并远离未来的碎片", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.TTS("Get together and stay away from Fragment of Fate", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }
                
            }
            
        }
        
        [ScriptMethod(name:"Phase4 Prompt To Dodge Akh Rhai 天光轮回躲避提示",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:40186"])]
        
        public void Phase4_Prompt_To_Dodge_Akh_Rhai_天光轮回躲避提示(Event @event, ScriptAccessory accessory) {
            
            if(Enable_Text_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.Method.TextInfo("跑！",3000);
                    
                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.Method.TextInfo("Run!",3000);
                    
                }
                
            }
            
            if (Enable_TTS_Prompts || Enable_DRTTS_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.TTS("跑！", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.TTS("Run!", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }
                
            }
            
        }

        [ScriptMethod(name: "P4_暗光龙诗_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:40239"], userControl: false)]
        public void P4_暗光龙诗_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 4.2d;
            P4Tether = [-1, -1, -1, -1, -1, -1, -1, -1];
            P4Stack = [0, 0, 0, 0, 0, 0, 0, 0];
            P4TetherDone = false;
        }
        [ScriptMethod(name: "P4_暗光龙诗_Buff记录", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:2461"], userControl: false)]
        public void P4_暗光龙诗_Buff记录(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.2) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var tIndex = accessory.Data.PartyList.IndexOf(((uint)tid));
            P4Stack[tIndex] = 1;
        }
        [ScriptMethod(name: "P4_暗光龙诗_连线收集", eventType: EventTypeEnum.Tether, eventCondition: ["Id:006E"], userControl: false)]
        public void P4_暗光龙诗_连线收集(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.2) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var sIndex = accessory.Data.PartyList.IndexOf(((uint)sid));
            var tIndex = accessory.Data.PartyList.IndexOf(((uint)tid));
            P4Tether[sIndex] = tIndex;
        }
        [ScriptMethod(name: "P4_暗光龙诗_引导扇形", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:40187"])]
        public void P4_暗光龙诗_引导扇形(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.2) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            for (uint i = 1; i < 5; i++)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_暗光龙诗_引导扇形";
                dp.Scale = new(20);
                dp.Radian = float.Pi / 3;
                dp.Owner = sid;
                dp.TargetResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                dp.TargetOrderIndex = i;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.Delay = 4000;
                dp.DestoryAt = 5000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
            }
        }
        [ScriptMethod(name: "P4_暗光龙诗_碎灵一击", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:40187"])]
        public void P4_暗光龙诗_碎灵一击(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.2) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_暗光龙诗_碎灵一击_水晶";
            dp.Scale = new(8.5f);
            dp.Owner = P4FragmentId;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 3500;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            for (int i = 0; i < 8; i++)
            {
                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_暗光龙诗_碎灵一击";
                dp.Scale = new(5);
                dp.Owner = accessory.Data.PartyList[i];
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.DestoryAt = 3500;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
            }
        }
        [ScriptMethod(name: "P4_暗光龙诗_神圣之翼", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(4022[78])$"])]
        public void P4_暗光龙诗_神圣之翼(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.2) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_暗光龙诗_神圣之翼";
            dp.Scale = new(40,20);
            dp.Owner = sid;
            dp.Rotation = @event["ActionId"] == "40227" ? float.Pi / 2 : float.Pi / -2;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 5000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);

        }
        [ScriptMethod(name: "P4_暗光龙诗_水分摊", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(4022[78])$"])]
        public void P4_暗光龙诗_水分摊(Event @event, ScriptAccessory accessory)
        {
            var tIndex = P4Tether[0] == -1 ? 1 : 0;
            var nIndex = P4Tether[2] == -1 ? 3 : 2;
            var d1Index = -1;
            var d2Index = -1;
            List<int> upGroup = [];
            List<int> downGroup = [];
            for (int i = 4; i < 7; i++)
            {
                for (int j = i + 1; j < 8; j++)
                {
                    if (P4Tether[i] != -1 && P4Tether[j] != -1)
                    {
                        d1Index = i;
                        d2Index = j;
                    }
                }
            }
            // t连线 高d 低d 蝴蝶结
            if ((P4Tether[tIndex] == d1Index && P4Tether[d2Index] == tIndex) || (P4Tether[tIndex] == d2Index && P4Tether[d1Index] == tIndex))
            {
                upGroup.Add(tIndex);
                upGroup.Add(nIndex);
                downGroup.Add(d1Index);
                downGroup.Add(d2Index);
            }
            // t连线 高d n 方块
            if ((P4Tether[tIndex] == d1Index && P4Tether[nIndex] == tIndex) || (P4Tether[d1Index] == tIndex && P4Tether[tIndex] == nIndex))
            {
                upGroup.Add(d1Index);
                upGroup.Add(nIndex);
                downGroup.Add(tIndex);
                downGroup.Add(d2Index);
            }
            // t连线 低d n 沙漏
            if ((P4Tether[tIndex] == d2Index && P4Tether[nIndex] == tIndex) || (P4Tether[d2Index] == tIndex && P4Tether[tIndex] == nIndex))
            {
                upGroup.Add(tIndex);
                upGroup.Add(d1Index);
                downGroup.Add(nIndex);
                downGroup.Add(d2Index);
            }

            var stack1 = P4Stack.IndexOf(1);
            var stack2 = P4Stack.LastIndexOf(1);
            var tetherStack = P4Tether[stack1] == -1 ? stack2 : stack1;
            var idleStack= P4Tether[stack1] == -1 ? stack1 : stack2;

            List<int> idles = [];
            for (int i = 0; i < 8; i++)
            {
                if (P4Tether[i] == -1)
                {
                    idles.Add(i);
                }
            }
            var ii = idles.IndexOf(idleStack);

            if (upGroup.Contains(tetherStack))
            {
                //线分摊在上
                if (ii==0||ii==2)
                {
                    downGroup.Add(idles[0]);
                    downGroup.Add(idles[2]);
                    upGroup.Add(idles[1]);
                    upGroup.Add(idles[3]);
                }
                if (ii == 1 || ii == 3)
                {
                    downGroup.Add(idles[1]);
                    downGroup.Add(idles[3]);
                    upGroup.Add(idles[0]);
                    upGroup.Add(idles[2]);
                }
            }
            if (downGroup.Contains(tetherStack))
            {
                //线分摊在下
                if (ii == 0 || ii == 2)
                {
                    upGroup.Add(idles[0]);
                    upGroup.Add(idles[2]);
                    downGroup.Add(idles[1]);
                    downGroup.Add(idles[3]);
                }
                if (ii == 1 || ii == 3)
                {
                    upGroup.Add(idles[1]);
                    upGroup.Add(idles[3]);
                    downGroup.Add(idles[0]);
                    downGroup.Add(idles[2]);
                }
            }

            var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_暗光龙诗_分摊";
            dp.Scale = new(6);
            dp.Owner = accessory.Data.PartyList[tetherStack];
            dp.Color = upGroup.Contains(tetherStack)==upGroup.Contains(myindex)?accessory.Data.DefaultSafeColor: accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 5000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_暗光龙诗_分摊";
            dp.Scale = new(6);
            dp.Owner = accessory.Data.PartyList[idleStack];
            dp.Color = upGroup.Contains(idleStack) == upGroup.Contains(myindex) ? accessory.Data.DefaultSafeColor : accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 5000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_暗光龙诗_分摊_水晶";
            dp.Scale = new(9.5f);
            dp.Owner = P4FragmentId;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 5000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        }
        [ScriptMethod(name: "P4_暗光龙诗_无尽顿悟", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:40249"])]
        public void P4_暗光龙诗_无尽顿悟(Event @event, ScriptAccessory accessory)
        {
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_暗光龙诗_无尽顿悟";
            dp.Scale = new(4);
            dp.Owner =sid;
            dp.CentreResolvePattern=PositionResolvePatternEnum.OwnerTarget;
            dp.Color =accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 6000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        }
        #region 远近跳
        //[ScriptMethod(name: "P4_暗光龙诗_远近跳", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:40283"])]
        //public void P4_暗光龙诗_远近跳(Event @event, ScriptAccessory accessory)
        //{
        //    if (parse != 4.2) return;
        //    if (!ParseObjectId(@event["SourceId"], out var sid)) return;

        //    var dp = accessory.Data.GetDefaultDrawProperties();
        //    dp.Name = "P4_暗光龙诗_远跳";
        //    dp.Scale = new(8);
        //    dp.Owner = sid;
        //    dp.CentreResolvePattern = PositionResolvePatternEnum.PlayerFarestOrder;
        //    dp.Color = accessory.Data.DefaultDangerColor;
        //    dp.DestoryAt = 5000;
        //    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

        //    dp = accessory.Data.GetDefaultDrawProperties();
        //    dp.Name = "P4_暗光龙诗_近跳";
        //    dp.Scale = new(8);
        //    dp.Owner = sid;
        //    dp.CentreResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
        //    dp.Color = accessory.Data.DefaultDangerColor;
        //    dp.Delay = 5000;
        //    dp.Delay = 3500;
        //    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

        //}
        #endregion
        [ScriptMethod(name: "P4_暗光龙诗_塔处理位置", eventType: EventTypeEnum.Tether, eventCondition: ["Id:006E"])]
        public void P4_暗光龙诗_塔处理位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.2) return;
            
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            if (sid != accessory.Data.Me) return;
            //accessory.Log.Debug("线");
            Task.Delay(250).ContinueWith(t =>
            {
                var tIndex = P4Tether[0] == -1 ? 1 : 0;
                var nIndex = P4Tether[2] == -1 ? 3 : 2;
                var d1Index = -1;
                var d2Index = -1;
                List<int> upGroup = [];
                List<int> downGroup = [];
                for (int i = 4; i < 7; i++)
                {
                    for (int j = i + 1; j < 8; j++)
                    {
                        if (P4Tether[i] != -1 && P4Tether[j] != -1)
                        {
                            d1Index = i;
                            d2Index = j;
                        }
                    }
                }
                // t连线 高d 低d 蝴蝶结
                if ((P4Tether[tIndex] == d1Index && P4Tether[d2Index] == tIndex) || (P4Tether[tIndex] == d2Index && P4Tether[d1Index] == tIndex))
                {
                    upGroup.Add(tIndex);
                    upGroup.Add(nIndex);
                    downGroup.Add(d1Index);
                    downGroup.Add(d2Index);
                }
                // t连线 高d n 方块
                if ((P4Tether[tIndex] == d1Index && P4Tether[nIndex] == tIndex) || (P4Tether[d1Index] == tIndex && P4Tether[tIndex] == nIndex))
                {
                    upGroup.Add(d1Index);
                    upGroup.Add(nIndex);
                    downGroup.Add(tIndex);
                    downGroup.Add(d2Index);
                }
                // t连线 低d n 沙漏
                if ((P4Tether[tIndex] == d2Index && P4Tether[nIndex] == tIndex) || (P4Tether[d2Index] == tIndex && P4Tether[tIndex] == nIndex))
                {
                    upGroup.Add(tIndex);
                    upGroup.Add(d1Index);
                    downGroup.Add(nIndex);
                    downGroup.Add(d2Index);
                }

                var myIndex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                Vector3 dealpos = upGroup.Contains(myIndex) ? new(100, 0, 92) : new(100, 0, 108);

                var dur = 10000;
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_暗光龙诗_塔处理位置";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = dur;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_暗光龙诗_塔处理位置";
                dp.Scale = new(4);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Position = dealpos;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = dur;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Circle, dp);
            });
            
        }
        [ScriptMethod(name: "P4_暗光龙诗_引导处理位置", eventType: EventTypeEnum.Tether, eventCondition: ["Id:006E"])]
        public void P4_暗光龙诗_引导处理位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.2) return;
            lock (this)
            {
                if (P4TetherDone) return;
                P4TetherDone = true;
            }
            Task.Delay(250).ContinueWith(t =>
            {
                List<int> idles = [];
                for (int i = 0; i < 8; i++)
                {
                    if (P4Tether[i] == -1)
                    {
                        idles.Add(i);
                    }
                }
                var myIndex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                if (!idles.Contains(myIndex)) return;
                Vector3 dealpos = idles.IndexOf(myIndex) switch
                {
                    0 => new(095.8f, 0, 098.0f),
                    1 => new(104.2f, 0, 098.0f),
                    2 => new(095.8f, 0, 102.0f),
                    3 => new(104.2f, 0, 102.0f),
                };

                var dur = 10000;
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_暗光龙诗_引导处理位置";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = dur;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            });

        }
        [ScriptMethod(name: "P4_暗光龙诗_分摊处理位置", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(4022[78])$"])]
        public void P4_暗光龙诗_分摊处理位置(Event @event, ScriptAccessory accessory)
        {
            var tIndex = P4Tether[0] == -1 ? 1 : 0;
            var nIndex = P4Tether[2] == -1 ? 3 : 2;
            var d1Index = -1;
            var d2Index = -1;
            List<int> upGroup = [];
            List<int> downGroup = [];
            for (int i = 4; i < 7; i++)
            {
                for (int j = i + 1; j < 8; j++)
                {
                    if (P4Tether[i] != -1 && P4Tether[j] != -1)
                    {
                        d1Index = i;
                        d2Index = j;
                    }
                }
            }
            // t连线 高d 低d 蝴蝶结
            if ((P4Tether[tIndex] == d1Index && P4Tether[d2Index] == tIndex) || (P4Tether[tIndex] == d2Index && P4Tether[d1Index] == tIndex))
            {
                upGroup.Add(tIndex);
                upGroup.Add(nIndex);
                downGroup.Add(d1Index);
                downGroup.Add(d2Index);
            }
            // t连线 高d n 方块
            if ((P4Tether[tIndex] == d1Index && P4Tether[nIndex] == tIndex) || (P4Tether[d1Index] == tIndex && P4Tether[tIndex] == nIndex))
            {
                upGroup.Add(d1Index);
                upGroup.Add(nIndex);
                downGroup.Add(tIndex);
                downGroup.Add(d2Index);
            }
            // t连线 低d n 沙漏
            if ((P4Tether[tIndex] == d2Index && P4Tether[nIndex] == tIndex) || (P4Tether[d2Index] == tIndex && P4Tether[tIndex] == nIndex))
            {
                upGroup.Add(tIndex);
                upGroup.Add(d1Index);
                downGroup.Add(nIndex);
                downGroup.Add(d2Index);
            }

            var stack1 = P4Stack.IndexOf(1);
            var stack2 = P4Stack.LastIndexOf(1);
            var tetherStack = P4Tether[stack1] == -1 ? stack2 : stack1;
            var idleStack = P4Tether[stack1] == -1 ? stack1 : stack2;

            List<int> idles = [];
            for (int i = 0; i < 8; i++)
            {
                if (P4Tether[i] == -1)
                {
                    idles.Add(i);
                }
            }
            var ii = idles.IndexOf(idleStack);

            if (upGroup.Contains(tetherStack))
            {
                //线分摊在上
                if (ii == 0 || ii == 2)
                {
                    downGroup.Add(idles[0]);
                    downGroup.Add(idles[2]);
                    upGroup.Add(idles[1]);
                    upGroup.Add(idles[3]);
                }
                if (ii == 1 || ii == 3)
                {
                    downGroup.Add(idles[1]);
                    downGroup.Add(idles[3]);
                    upGroup.Add(idles[0]);
                    upGroup.Add(idles[2]);
                }
            }
            if (downGroup.Contains(tetherStack))
            {
                //线分摊在下
                if (ii == 0 || ii == 2)
                {
                    upGroup.Add(idles[0]);
                    upGroup.Add(idles[2]);
                    downGroup.Add(idles[1]);
                    downGroup.Add(idles[3]);
                }
                if (ii == 1 || ii == 3)
                {
                    upGroup.Add(idles[1]);
                    upGroup.Add(idles[3]);
                    downGroup.Add(idles[0]);
                    downGroup.Add(idles[2]);
                }
            }

            var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            Vector3 dealpos = new(@event["ActionId"] == "40227" ? 105 : 95, 0, upGroup.Contains(myindex) ? 92.5f : 107.5f);

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_暗光龙诗_分摊处理位置";
            dp.Scale = new(2);
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 5000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);


        }

        [ScriptMethod(name: "P4_时间结晶_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:40240"], userControl: false)]
        public void P4_时间结晶_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 4.3d;
            P4ClawBuff = [0, 0, 0, 0, 0, 0, 0, 0];
            P4OtherBuff = [0, 0, 0, 0, 0, 0, 0, 0];
            P4WhiteCirclePos = [];
            P4WaterPos = [];
            phase4_id1OfTheDrachenWanderers="";
            phase4_id2OfTheDrachenWanderers="";
            phase4_timesTheWyrmclawDebuffWasRemoved=0;
            phase4_residueIdsFromEastToWest=[0,0,0,0];
            phase4_guidanceOfResiduesHasBeenGenerated=false;
        }
        [ScriptMethod(name: "P4_时间结晶_Buff收集", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:regex:^(326[34]|2454|246[0123])$"], userControl: false)]
        public void P4_时间结晶_Buff收集(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.3) return;
            var id = @event["StatusID"];
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var index = accessory.Data.PartyList.IndexOf(((uint)tid));
            //3623红爪 1短2长
            if (id == "3263")
            {
                if (!float.TryParse(@event["Duration"], out float dur)) return;
                P4ClawBuff[index] = dur > 20 ? 2 : 1;
            }

            if (id == "3264")
            {
                P4ClawBuff[index] = 3;
            }
            //暗 4
            if (id == "2460")
            {
                P4OtherBuff[index] = 4;
            }
            //水 3
            if (id == "2461")
            {
                P4OtherBuff[index] = 3;
            }
            //冰 1
            if (id == "2462")
            {
                P4OtherBuff[index] = 1;
            }
            //风 2
            if (id == "2463")
            {
                P4OtherBuff[index] = 2;
            }
            //土 5
            if (id == "2454")
            {
                P4OtherBuff[index] = 5;
            }
        }
        [ScriptMethod(name: "P4_时间结晶_蓝线收集", eventType: EventTypeEnum.Tether, eventCondition: ["Id:0085"], userControl: false)]
        public void P4_时间结晶_蓝线收集(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.3) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            P4BlueTether = PositionTo6Dir(pos, new(100, 0, 100)) % 3;
        }
        [ScriptMethod(name: "P4_时间结晶_灯AOE", eventType: EventTypeEnum.Tether, eventCondition: ["Id:0085"])]
        public void P4_时间结晶_灯AOE(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.3) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            Vector3 normalPos = new(pos.X, 0, 200 - pos.Z);
            Vector3 fastPos = new(100, 0, pos.Z > 100 ? 111 : 89);

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_时间结晶_灯AOE_快";
            dp.Scale = new(12);
            dp.Position = fastPos;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 7500;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
            
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_时间结晶_灯AOE_中";
            dp.Scale = new(12);
            dp.Position = normalPos;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 13000 - P4LampDisplayDur;
            dp.DestoryAt = P4LampDisplayDur;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_时间结晶_灯AOE_慢";
            dp.Scale = new(12);
            dp.Position = pos;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 18000 - P4LampDisplayDur;
            dp.DestoryAt = P4LampDisplayDur;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        }
        [ScriptMethod(name: "P4_时间结晶_土分摊范围", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:2454"])]
        public void P4_时间结晶_土分摊范围(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.3) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_时间结晶_土分摊范围";
            dp.Scale = new(6);
            dp.Owner = tid;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.Delay = 14000;
            dp.DestoryAt = 3000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_时间结晶_土分摊范围_水晶";
            dp.Scale = new(9.5f);
            dp.Owner = P4FragmentId;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 14000;
            dp.DestoryAt = 3000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        }
        [ScriptMethod(name: "P4_时间结晶_碎灵一击", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:2452"])]
        public void P4_时间结晶_碎灵一击(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.3) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            if (tid != accessory.Data.Me) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_时间结晶_碎灵一击_水晶";
            dp.Scale = new(8.5f);
            dp.Owner = P4FragmentId;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 3500;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
            for (int i = 0; i < 8; i++)
            {
                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_碎灵一击";
                dp.Scale = new(5);
                dp.Owner = accessory.Data.PartyList[i];
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.DestoryAt = 3500;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
            }
            
            
        }
        [ScriptMethod(name: "P4_时间结晶_Buff处理位置", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:40293"])]
        public void P4_时间结晶_Buff处理位置(Event @event, ScriptAccessory accessory)
        {
            
            //buff后3.5s
            if (parse != 4.3) return;
            var myIndex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            //短红
            if (P4ClawBuff[myIndex] == 1)
            {
                var isHigh = P4ClawBuff.IndexOf(1) == myIndex;
                Vector3 dealpos= isHigh ? new(87, 0, 100) : new(113, 0, 100);

                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_Buff处理位置_撞龙头";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 10500;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                //高分摊 088 085 -> 093 082
                //高闲   081 103 -> 081 097
                //低分摊 112 085 -> 107 082
                //低闲   119 103 -> 119 97
                Vector3 dealpos2 = isHigh ? (P4BlueTether == 1 ? new(081, 0, 103) : new(088, 0, 085)) : (P4BlueTether == 1 ? new(112, 0, 085) : new(119, 0, 103));
                Vector3 dealpos3 = isHigh ? (P4BlueTether == 1 ? new(081, 0, 097) : new(093, 0, 082)) : (P4BlueTether == 1 ? new(107, 0, 082) : new(119, 0, 097));

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_Buff处理位置_pos2预连线";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Position = dealpos;
                dp.TargetPosition = dealpos2;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 10500;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_Buff处理位置_pos2位置";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos2;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 10500;
                dp.DestoryAt = 3000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_Buff处理位置_pos3预连线";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Position = dealpos2;
                dp.TargetPosition = dealpos3;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 13500;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_Buff处理位置_pos3位置";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos3;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 13500;
                dp.DestoryAt = 3000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
            //长红
            if (P4ClawBuff[myIndex] == 2)
            {
                var isHigh = P4ClawBuff.IndexOf(2) == myIndex;
                Vector3 dealpos1 = isHigh ? new(088.5f, 0, 115.5f) : new(111.5f, 0, 115.5f);
                Vector3 dealpos2 = isHigh ? new(090.2f, 0, 117.0f) : new(109.8f, 0, 117.0f);
                Vector3 dealpos3 = isHigh ? new(092.5f, 0, 118.0f) : new(107.5f, 0, 118.0f);
                Vector3 dealpos4 = isHigh ? new(092.53f, 0, 110.40f) : new(107.47f, 0, 110.40f);
                // The previous coordinates were: isHigh ? new(092.0f, 0, 110.0f) : new(108.0f, 0, 110.0f);
                
                // ----- 0s -> 7.5s -----

                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_Buff处理位置_躲ac";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos1;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 7500;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_Buff处理位置_躲ac->击退";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Position = dealpos1;
                dp.TargetPosition = dealpos2;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 7500;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                
                // ----- -----
                
                // ----- 7.5s -> 10.5s -----
                
                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_Buff处理位置_击退";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos2;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 7500;
                dp.DestoryAt = 3000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_Buff处理位置_击退->躲斜点";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Position = dealpos2;
                dp.TargetPosition = dealpos3;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 10500;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                
                // ----- -----
                
                // ----- 10.5s -> 13s -----
                
                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_Buff处理位置_躲斜点";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos3;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 10500;
                dp.DestoryAt = 2500;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_Buff处理位置_躲斜点->撞头";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Position = dealpos3;
                dp.TargetPosition = dealpos4;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 13000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                
                // ----- -----
                
                // ----- 13s -> 16s -----
                
                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_Buff处理位置_撞头";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos4;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 13000;
                dp.DestoryAt = 3000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                
                // ----- -----
                
                // There were some issues in the guidance here which is for the players with long Wyrmclaw debuff.
                // Cicero has adjusted the process a little bit, and the issues has been fixed now.
                
            }
            //蓝
            if (P4ClawBuff[myIndex] == 3)
            {
                if (P4OtherBuff[myIndex] == 4)
                {
                    Vector3 dealpos1 = P4BlueTether == 1 ? new(112, 0, 85) : new(88, 0, 85);
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P4_时间结晶_Buff处理位置_躲灯1";
                    dp.Scale = new(2);
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = dealpos1;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.DestoryAt = 14500;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                }
                else
                {
                    Vector3 dealpos1 = P4BlueTether == 1 ? new(88, 0, 115) : new(112, 0, 115);
                    Vector3 dealpos2 = P4BlueTether == 1 ? new(090.8f, 0, 116.0f) : new(109.2f, 0, 116.0f);
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P4_时间结晶_Buff处理位置_躲灯ac";
                    dp.Scale = new(2);
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = dealpos1;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.DestoryAt = 7500;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P4_时间结晶_Buff处理位置_躲ac->击退";
                    dp.Scale = new(2);
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Position = dealpos1;
                    dp.TargetPosition = dealpos2;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.DestoryAt = 7500;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P4_时间结晶_Buff处理位置_击退";
                    dp.Scale = new(2);
                    dp.ScaleMode |= ScaleMode.YByDistance;
                    dp.Owner = accessory.Data.Me;
                    dp.TargetPosition = dealpos2;
                    dp.Color = accessory.Data.DefaultSafeColor;
                    dp.Delay = 7500;
                    dp.DestoryAt = 3000;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                }
            }
        }
        [ScriptMethod(name: "P4_时间结晶_白圈位置指示", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:40241", "TargetIndex:1"])]
        public void P4_时间结晶_白圈位置指示(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.3) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["EffectPosition"]);
            lock (P4WhiteCirclePos)
            {
                P4WhiteCirclePos.Add(pos);
                if (P4WhiteCirclePos.Count == 1 || P4WhiteCirclePos.Count == 3) return;

            }
        }
        [ScriptMethod(name: "P4_时间结晶_放回返位置", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:40251"])]
        public void P4_时间结晶_放回返位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.3) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            P4WaterPos.Add(pos);
            if (P4WaterPos.Count == 1) return;
            var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            Vector3 centre = new(100, 0, 100);
            var dir8 = PositionTo8Dir((P4WaterPos[0] + P4WaterPos[1]) / 2, centre)-1;
            Vector3 mtPos = new(107, 0, 88);
            Vector3 stPos = new(112, 0, 93);
            Vector3 mtgPos = new(106, 0, 92);
            Vector3 stgPos = new(108, 0, 94);
            if (myindex==0)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_放回返位置_MT";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = RotatePoint(mtPos, centre, float.Pi / 4 * dir8);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 9000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
            if (myindex == 1)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_放回返位置_ST";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = RotatePoint(stPos, centre, float.Pi / 4 * dir8);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 9000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
            if (myindex == 2 || myindex == 4 || myindex == 6)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_放回返位置_MTG";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = RotatePoint(mtgPos, centre, float.Pi / 4 * dir8);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 9000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
            if (myindex == 3 || myindex == 5 || myindex == 7)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_时间结晶_放回返位置_STG";
                dp.Scale = new(2);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = RotatePoint(stgPos, centre, float.Pi / 4 * dir8);
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 9000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
        }
        
        [ScriptMethod(name:"Phase4 Acquire IDs Of Drachen Wanderers 获取圣龙气息(龙头)ID",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:17836"],
            userControl:false)]

        public void Phase4_Acquire_IDs_Of_Drachen_Wanderers_获取圣龙气息ID(Event @event, ScriptAccessory accessory) {

            if(parse!=4.3) {

                return;

            }

            lock(phase4_ReadwriteLockOfDrachenWandererIds_AsAConstant) {

                if(phase4_id1OfTheDrachenWanderers.Equals("")) {

                    phase4_id1OfTheDrachenWanderers=@event["SourceId"];

                }

                else {
                    
                    if(phase4_id2OfTheDrachenWanderers.Equals("")) {

                        phase4_id2OfTheDrachenWanderers=@event["SourceId"];

                    }
                    
                }
                
            }

        }
        
        [ScriptMethod(name:"Phase4 Hitbox Of Drachen Wanderers 圣龙气息(龙头)碰撞箱",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:17836"])]

        public void Phase4_Hitbox_Of_Drachen_Wanderers_圣龙气息碰撞箱(Event @event, ScriptAccessory accessory) {

            if(parse!=4.3) {

                return;

            }

            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {

                return;

            }

            var currentProperty=accessory.Data.GetDefaultDrawProperties();

            currentProperty.Name=$"Phase4_Hitbox_Of_Drachen_Wanderers_圣龙气息碰撞箱_{sourceId}";
            currentProperty.Scale=new(2f,Phase4_Length_Of_Drachen_Wanderer_Hitboxes>=0?
                                                Phase4_Length_Of_Drachen_Wanderer_Hitboxes:
                                                1.5f);
            currentProperty.Color=Phase4_Colour_Of_Drachen_Wanderer_Hitboxes.V4.WithW(25f);
            currentProperty.Offset=new(0f,0f,-1f);
            currentProperty.Owner=sourceId;
            currentProperty.DestoryAt=34000;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Displacement,currentProperty);

        }
        
        [ScriptMethod(name:"Phase4 Explosion Range Of Drachen Wanderers 圣龙气息(龙头)爆炸范围",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:17836"])]

        public void Phase4_Explosion_Range_Of_Drachen_Wanderers_圣龙气息爆炸范围(Event @event, ScriptAccessory accessory) {

            if(parse!=4.3) {

                return;

            }

            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {

                return;

            }

            var currentProperty=accessory.Data.GetDefaultDrawProperties();
                
            currentProperty.Name=$"Phase4_Explosion_Range_Of_Drachen_Wanderers_圣龙气息爆炸范围_{sourceId}";
            currentProperty.Scale=new(12);
            currentProperty.Owner=sourceId;
            currentProperty.Color=accessory.Data.DefaultDangerColor;
            currentProperty.DestoryAt=34000;
                
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperty);

        }
        
        [ScriptMethod(name:"Phase4 Remove Hitboxes And Explosion Ranges Of Drachen Wanderers 移除圣龙气息(龙头)碰撞箱与爆炸范围",
            eventType:EventTypeEnum.RemoveCombatant,
            eventCondition:["DataId:17836"],
            userControl:false)]

        public void Phase4_Remove_Hitboxes_And_Explosion_Ranges_Of_Drachen_Wanderers_移除圣龙气息碰撞箱与爆炸范围(Event @event, ScriptAccessory accessory) {

            if(parse!=4.3) {

                return;

            }

            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {

                return;

            }

            accessory.Method.RemoveDraw($"Phase4_Hitbox_Of_Drachen_Wanderers_圣龙气息碰撞箱_{sourceId}");
            accessory.Method.RemoveDraw($"Phase4_Explosion_Range_Of_Drachen_Wanderers_圣龙气息爆炸范围_{sourceId}");

        }
        
        [ScriptMethod(name:"Phase4 Remove Hitboxes And Explosion Ranges Of Drachen Wanderers In Advance 提前移除圣龙气息(龙头)碰撞箱与爆炸范围",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:3263"],
            userControl:false)]
        
        // The ObjectChanged event with the field "Operate" as "Remove" would be triggered almost three seconds after the Drachen Wanderer is gone.
        // If the drawing removal relies on the event, it would be too late and may cause confusion.
        // Here is an optimized method for players with the Wyrmclaw debuff (the red debuff), which is to monitor the StatusRemove events of the Wyrmclaw debuff and acquire the closest Drachen Wanderer.
        // Obviously, the method would not help if a player with the Wyrmfang debuff (the red debuff) hits a Drachen Wanderer. However, that's already a wipe, so whatever.
        // Thanks to Cyf5119 for providing a Dalamud way to detect if the player is dead, so that the method would skip the StatusRemove events caused by death.

        public void Phase4_Remove_Hitboxes_And_Explosion_Ranges_Of_Drachen_Wanderers_In_Advance_提前移除圣龙气息碰撞箱与爆炸范围(Event @event, ScriptAccessory accessory) {
            
            if(parse!=4.3) {

                return;

            }

            if(!ParseObjectId(@event["TargetId"], out var targetId)) {

                return;

            }
            
            var targetObject=accessory.Data.Objects.SearchById(targetId);

            if(targetObject==null) {

                return;

            }

            if(((IBattleChara?)targetObject)==null) {

                return;

            }

            if(((IBattleChara?)targetObject).IsDead) {
                // Ignore the situations that the debuff was removed due to a death.

                return;

            }
            
            ++phase4_timesTheWyrmclawDebuffWasRemoved;

            if(phase4_timesTheWyrmclawDebuffWasRemoved<3||phase4_timesTheWyrmclawDebuffWasRemoved>4) {

                return;

            }
            
            if(!ParseObjectId(phase4_id1OfTheDrachenWanderers, out var drachenWandererId1)) {

                return;

            }
            
            if(!ParseObjectId(phase4_id2OfTheDrachenWanderers, out var drachenWandererId2)) {

                return;

            }
            
            var drachenWandererObject1=accessory.Data.Objects.SearchById(drachenWandererId1);

            if(drachenWandererObject1==null) {

                return;

            }
            
            var drachenWandererObject2=accessory.Data.Objects.SearchById(drachenWandererId2);
            
            if(drachenWandererObject2==null) {

                return;

            }

            if(Vector3.Distance(targetObject.Position, drachenWandererObject1.Position)
               <=
               Vector3.Distance(targetObject.Position, drachenWandererObject2.Position)) {
                
                accessory.Method.RemoveDraw($"Phase4_Hitbox_Of_Drachen_Wanderers_圣龙气息碰撞箱_{drachenWandererId1}");
                accessory.Method.RemoveDraw($"Phase4_Explosion_Range_Of_Drachen_Wanderers_圣龙气息爆炸范围_{drachenWandererId1}");

            }

            else {
                
                accessory.Method.RemoveDraw($"Phase4_Hitbox_Of_Drachen_Wanderers_圣龙气息碰撞箱_{drachenWandererId2}");
                accessory.Method.RemoveDraw($"Phase4_Explosion_Range_Of_Drachen_Wanderers_圣龙气息爆炸范围_{drachenWandererId2}");
                
            }

        }
        
        [ScriptMethod(name:"Phase4 Tidal Light 光之潮汐(地火)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:regex:^(40252|40253)$"])]

        public void Phase4_Tidal_Light_光之潮汐(Event @event, ScriptAccessory accessory) {

            if(parse!=4.3) {

                return;

            }
            
            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }

            var currentProperty=accessory.Data.GetDefaultDrawProperties();
        
            currentProperty.Owner=sourceId;
            currentProperty.Offset=new Vector3(0,0,-10);
            currentProperty.Scale=new(40,10);
            currentProperty.DestoryAt=2100;
            currentProperty.Color=Phase4_Colour_Of_Tidal_Light.V4.WithW(3f);
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperty);

        }
        
        [ScriptMethod(name:"Phase4 Determine Relative Positions Of Residues 确定白圈相对位置",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:2014529"],
            userControl:false)]
        
        public void Phase4_Determine_Relative_Positions_Of_Residues_确定白圈相对位置(Event @event, ScriptAccessory accessory) {
            
            if(parse!=4.3) {

                return;

            }
            
            if(!@event["Operate"].Equals("Add")) {

                return;

            }

            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
            
            var sourcePositionInJson=JObject.Parse(@event["SourcePosition"]);
            float currentX=sourcePositionInJson["X"]?.Value<float>()??0;
            
            if(currentX<100) {

                if(phase4_residueIdsFromEastToWest[3]!=0) {
                    
                    lock(phase4_residueIdsFromEastToWest) {

                        phase4_residueIdsFromEastToWest[2]=sourceId;
                        // The about right one while facing south.
                        
                    }
                    
                }

                else {
                    
                    lock(phase4_residueIdsFromEastToWest) {

                        phase4_residueIdsFromEastToWest[3]=sourceId;
                        // The rightmost one while facing south.
                        
                    }
                    
                }

            }

            if(currentX>100) {

                if(phase4_residueIdsFromEastToWest[0]!=0) {
                    
                    lock(phase4_residueIdsFromEastToWest) {

                        phase4_residueIdsFromEastToWest[1]=sourceId;
                        // The about left one while facing south.
                        
                    }
                    
                }

                else {
                    
                    lock(phase4_residueIdsFromEastToWest) {

                        phase4_residueIdsFromEastToWest[0]=sourceId;
                        // The leftmost one while facing south.
                        
                    }
                    
                }

            }

            if(Enable_Developer_Mode) {

                accessory.Method.SendChat($"""
                                           /e 
                                           @event["SourceId"]={@event["SourceId"]}
                                           sourceId={sourceId}
                                           @event["SourcePosition"]={@event["SourcePosition"]}
                                           currentX={currentX}
                                           
                                           """);

            }
        
        }

        [ScriptMethod(name:"Phase4 Guidance Of Residues 白圈指路",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:regex:^(40252|40253)$"])]

        public void Phase4_Guidance_Of_Residues_白圈指路(Event @event, ScriptAccessory accessory) {

            if(parse!=4.3) {

                return;

            }

            if(phase4_guidanceOfResiduesHasBeenGenerated) {

                return;

            }

            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            Phase4_Relative_Positions_Of_Residues relativePositionOfMyResidue=phase4_getRelativePosition(myIndex);
            ulong idOfMyResidue=phase4_getResidueId(relativePositionOfMyResidue);

            if(Enable_Developer_Mode) {

                accessory.Method.SendChat($"""
                                           /e 
                                           phase4_residueIdsFromEastToWest[]={phase4_residueIdsFromEastToWest[0]},{phase4_residueIdsFromEastToWest[1]},{phase4_residueIdsFromEastToWest[2]},{phase4_residueIdsFromEastToWest[3]}
                                           P4ClawBuff={P4ClawBuff[myIndex]}
                                           P4OtherBuff={P4OtherBuff[myIndex]}
                                           relativePositionOfMyResidue={relativePositionOfMyResidue}
                                           idOfMyResidue={idOfMyResidue}
                                           
                                           """);
                
            }

            if(relativePositionOfMyResidue!=Phase4_Relative_Positions_Of_Residues.Unknown_未知
               &&
               idOfMyResidue!=0) { 

                var currentProperty=accessory.Data.GetDefaultDrawProperties();

                currentProperty.Name="Phase4_Guidance_Of_Residues_白圈指路";
                currentProperty.Scale=new(2);
                currentProperty.ScaleMode|=ScaleMode.YByDistance;
                currentProperty.Owner=accessory.Data.Me;
                currentProperty.Color=Phase4_Colour_Of_Residue_Guidance.V4.WithW(1f);
                currentProperty.DestoryAt=23000;

                var residueObject=accessory.Data.Objects.SearchById(idOfMyResidue);

                if(residueObject!=null) {
                        
                    phase4_guidanceOfResiduesHasBeenGenerated=true;
                    
                    currentProperty.TargetPosition=residueObject.Position;

                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                    if(Enable_Text_Prompts) {
                        
                        accessory.Method.TextInfo(phase4_getResidueDescription(relativePositionOfMyResidue),2500);
                        
                    }

                    accessory.TTS($"{phase4_getResidueDescription(relativePositionOfMyResidue)}", 
                                    Enable_TTS_Prompts, Enable_DRTTS_Prompts);



                    if (Enable_Developer_Mode) {

                        accessory.Method.SendChat($"""
                                                   /e 
                                                   residueObject.Position={residueObject.Position}
                                                   
                                                   """);

                    }
                    
                }

            }

        }
        
        [ScriptMethod(name:"Phase4 Remove Guidance Of Residues 移除白圈指路",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:3264"],
            userControl:false)]

        public void Phase4_Remove_Guidance_Of_Residues_移除白圈指路(Event @event, ScriptAccessory accessory) {

            if(parse!=4.3) {

                return;

            }

            if(!ParseObjectId(@event["TargetId"], out var targetId)) {

                return;

            }

            if(targetId!=accessory.Data.Me) {

                return;

            }
            
            accessory.Method.RemoveDraw("Phase4_Guidance_Of_Residues_白圈指路");

        }

        [ScriptMethod(name:"Phase4 Highlight Of Residues 白圈高亮",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:2014529"])]

        public void Phase4_Highlight_Of_Residues_白圈高亮(Event @event, ScriptAccessory accessory) {

            if(parse!=4.3) {

                return;

            }

            if(!@event["Operate"].Equals("Add")) {

                return;

            }

            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {

                return;

            }

            var currentProperty=accessory.Data.GetDefaultDrawProperties();

            currentProperty.Name=$"Phase4_Highlight_Of_Residues_白圈高亮_{sourceId}";
            currentProperty.Scale=new(1f);
            currentProperty.InnerScale=new(0.8f);
            currentProperty.Color=accessory.Data.DefaultDangerColor.WithW(25f);
            currentProperty.Radian=float.Pi*2;
            currentProperty.Owner=sourceId;
            currentProperty.DestoryAt=17000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Donut,currentProperty);

        }

        [ScriptMethod(name:"Phase4 Remove Highlights Of Residues 移除白圈高亮",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:2014529"],
            userControl:false)]

        public void Phase4_Remove_Highlights_Of_Residues_移除白圈高亮(Event @event, ScriptAccessory accessory) {

            if(parse!=4.3) {

                return;
    
            }

            if(!@event["Operate"].Equals("Remove")) {

                return;

            }

            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"Phase4_Highlight_Of_Residues_白圈高亮_{sourceId}");

            ulong idOfMyResidue=phase4_getResidueId(phase4_getRelativePosition(accessory.Data.PartyList.IndexOf(accessory.Data.Me)));

            if(idOfMyResidue!=0
               &&
               idOfMyResidue==sourceId) {
                
                accessory.Method.RemoveDraw("Phase4_Guidance_Of_Residues_白圈指路");
                
            }

        }
        
        [ScriptMethod(name:"Phase4 Remove Highlights Of Residues In Advance 提前移除白圈高亮",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:3264"],
            userControl:false)]
        
        // The background and implementation are almost the same as the removal of Hitboxes and Explosion Ranges before.
        // Please refer to the comments following that method for details.

        public void Phase4_Remove_Highlights_Of_Residues_In_Advance_提前移除白圈高亮(Event @event, ScriptAccessory accessory) {
            
            if(parse!=4.3) {

                return;

            }

            if(!ParseObjectId(@event["TargetId"], out var targetId)) {

                return;

            }
            
            var targetObject=accessory.Data.Objects.SearchById(targetId);

            if(targetObject==null) {

                return;

            }
            
            Vector3 targetPosition=targetObject.Position;

            if(((IBattleChara?)targetObject)==null) {

                return;

            }

            if(((IBattleChara?)targetObject).IsDead) {
                // Ignore the situations that the debuff was removed due to a death.

                return;

            }

            int closestResidue=-1;
            float distanceToTheClosestResidue=float.PositiveInfinity;

            for(int i=0;i<4;++i) {

                var residueObject=accessory.Data.Objects.SearchById(phase4_residueIdsFromEastToWest[i]);

                if(residueObject!=null) {

                    if(Vector3.Distance(targetPosition,residueObject.Position)<distanceToTheClosestResidue) {

                        closestResidue=i;
                        distanceToTheClosestResidue=Vector3.Distance(targetPosition,residueObject.Position);

                    }
                    
                }

            }

            if(0<=closestResidue&&closestResidue<=3) {
                
                accessory.Method.RemoveDraw($"Phase4_Highlight_Of_Residues_白圈高亮_{phase4_residueIdsFromEastToWest[closestResidue]}");

                if(targetId!=accessory.Data.Me) {
                    
                    ulong idOfMyResidue=phase4_getResidueId(phase4_getRelativePosition(accessory.Data.PartyList.IndexOf(accessory.Data.Me)));

                    if(idOfMyResidue!=0
                       &&
                       idOfMyResidue==phase4_residueIdsFromEastToWest[closestResidue]) {
                
                        accessory.Method.RemoveDraw("Phase4_Guidance_Of_Residues_白圈指路");
                
                    }
                    
                }
                
            }

        }
        
        private Phase4_Relative_Positions_Of_Residues phase4_getRelativePosition(int currentIndex) {

            if(currentIndex<0||currentIndex>7) {
                
                return Phase4_Relative_Positions_Of_Residues.Unknown_未知;
                
            }
            
            if(P4ClawBuff[currentIndex]==1||P4ClawBuff[currentIndex]==2) {
                // 1 stands for short Wyrmclaw (the red debuff), 2 stands for long Wyrmclaw (also the red debuff).

                return Phase4_Relative_Positions_Of_Residues.Unknown_未知;

            }

            if(P4ClawBuff[currentIndex]==3) {
                // 3 stands for Wyrmfang (the blue debuff).

                if(P4OtherBuff[currentIndex]==4) {
                    // 4 stands for Dark Eruption.

                    return Phase4_Residue_Belongs_To_Dark_Eruption;

                }

                if(P4OtherBuff[currentIndex]==5) {
                    // 5 stands for Unholy Darkness.

                    return Phase4_Residue_Belongs_To_Unholy_Darkness;

                }

                if(P4OtherBuff[currentIndex]==1) {
                    // 1 stands for Dark Blizzard III.

                    return Phase4_Residue_Belongs_To_Dark_Blizzard_III;

                }

                if(P4OtherBuff[currentIndex]==3) {
                    // 3 stands for Dark Water III.

                    return Phase4_Residue_Belongs_To_Dark_Water_III;

                }

            }

            return Phase4_Relative_Positions_Of_Residues.Unknown_未知;
            // Just a placeholder and should never be reached.

        }

        private ulong phase4_getResidueId(Phase4_Relative_Positions_Of_Residues relativePosition) {
            
            switch(relativePosition) {

                case(Phase4_Relative_Positions_Of_Residues.Eastmost_最东侧): {

                    return phase4_residueIdsFromEastToWest[0];

                }
                
                case(Phase4_Relative_Positions_Of_Residues.About_East_次东侧): {

                    return phase4_residueIdsFromEastToWest[1];

                }
                
                case(Phase4_Relative_Positions_Of_Residues.About_West_次西侧): {

                    return phase4_residueIdsFromEastToWest[2];

                }

                case(Phase4_Relative_Positions_Of_Residues.Westmost_最西侧): {

                    return phase4_residueIdsFromEastToWest[3];

                }

                case(Phase4_Relative_Positions_Of_Residues.Unknown_未知): {

                    return 0;

                }

                default: {

                    return 0;
                    // Just a placeholder and should never be reached.

                }
                
            }
            
        }

        private String phase4_getResidueDescription(Phase4_Relative_Positions_Of_Residues relativePosition) {

            switch (relativePosition) {

                case(Phase4_Relative_Positions_Of_Residues.Eastmost_最东侧): {
                    
                    if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                        return "最左/最东";

                    }

                    if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                        return "Leftmost/Eastmost";

                    }
                    
                    return "";
                    // Just a placeholder and should never be reached.

                }
                
                case(Phase4_Relative_Positions_Of_Residues.About_East_次东侧): {
                    
                    if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                        return "次左/次东";

                    }

                    if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                        return "About left/About east";

                    }
                    
                    return "";
                    // Just a placeholder and should never be reached.

                }
                
                case(Phase4_Relative_Positions_Of_Residues.About_West_次西侧): {
                    
                    if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                        return "次右/次西";

                    }

                    if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                        return "About right/About west";

                    }
                    
                    return "";
                    // Just a placeholder and should never be reached.

                }

                case(Phase4_Relative_Positions_Of_Residues.Westmost_最西侧): {
                    
                    if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {

                        return "最右/最西";

                    }

                    if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {

                        return "Rightmost/Westmost";

                    }
                    
                    return "";
                    // Just a placeholder and should never be reached.

                }

                case(Phase4_Relative_Positions_Of_Residues.Unknown_未知): {
                    
                    return "";
                    
                }

                default: {

                    return "";
                    // Just a placeholder and should never be reached.

                }
                
            }

        }

        #endregion

        #region P5
        
        [ScriptMethod(name:"----- Phase 5 ----- (No actual meaning for this toggle/此开关无实际意义)",
            eventType:EventTypeEnum.NpcYell,
            eventCondition:["I lift my lamp beside the golden door!",
                            "我在金门旁为他们将灯举起!"])]
        
        public void Phase5_Placeholder(Event @event, ScriptAccessory accessory) { }
        
        [ScriptMethod(name:"Phase5 Initialization 初始化",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:17839"],
            userControl:false)]
        
        public void Phase5_Initialization_初始化(Event @event, ScriptAccessory accessory) {
            
            isInPhase5=true;
            
            phase5_bossId=@event["SourceId"];
            phase5_hasAcquiredTheFirstTower=false;
            phase5_indexOfTheFirstTower="";
            phase5_hasConfirmedTheInitialPosition=false;

        }
        
        [ScriptMethod(name:"Phase5 Destruction 析构",
            eventType:EventTypeEnum.RemoveCombatant,
            eventCondition:["DataId:17839"],
            userControl:false)]
        
        public void Phase5_Destruction_析构(Event @event, ScriptAccessory accessory) {

            isInPhase5=false;
            
            phase5_bossId="";
            phase5_hasAcquiredTheFirstTower=false;
            phase5_indexOfTheFirstTower="";
            phase5_hasConfirmedTheInitialPosition=false;

        }
        
        [ScriptMethod(name: "P5_地火", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(40118|40307)$"])]
        public void P5_地火(Event @event, ScriptAccessory accessory)
        {
            if(!isInPhase5) {

                return;

            }
            
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_地火";
            dp.Scale = new(80, 5);
            dp.Owner = sid;
            dp.Color = P5PathColor.V4.WithW(3);
            dp.DestoryAt = 7000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = $"P5_地火_前进_{@event["SourceId"]}";
            dp.Scale = new(80, 5);
            dp.Offset = new(0,0,-5);
            dp.Owner = sid;
            dp.Color = P5PathColor.V4.WithW(3);
            dp.Delay = 7000;
            dp.DestoryAt = 20000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);

        }
        [ScriptMethod(name: "P5_地火消除", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^(40118|4030[789])$"],userControl:false)]
        public void P5_地火消除(Event @event, ScriptAccessory accessory)
        {
            if(!isInPhase5) {

                return;

            }
            
            if (!float.TryParse(@event["SourceRotation"],out var rot)) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            Vector3 centre = new(100, 0, 100);
            Vector3 posNext = new(pos.X + 5 * MathF.Sin(rot), 0, pos.Z + 5 * MathF.Cos(rot));
            if ((posNext- centre).Length()>20)
            {
                accessory.Method.RemoveDraw($"P5_地火_前进_{@event["SourceId"]}");
            }
        }
        
        [ScriptMethod(name:"Phase5 Boss Central Axis After Fulgent Blade 璀璨之刃(地火)后Boss中轴线",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:40310"])]
        
        public void Phase5_Boss_Central_Axis_After_Fulgent_Blade_璀璨之刃后Boss中轴线(Event @event, ScriptAccessory accessory) {
            
            if(!isInPhase5) {

                return;

            }

            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }

            var currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            currentProperty.Name="Phase5_Boss_Front_Axis_After_Fulgent_Blade_璀璨之刃后Boss前轴线";
            currentProperty.Owner=sourceId;
            currentProperty.Scale=new(0.5f,10);
            currentProperty.Color=Phase5_Colour_Of_The_Boss_Central_Axis.V4.WithW(25f);
            currentProperty.DestoryAt=9000;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperty);

            currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            currentProperty.Name="Phase5_Boss_Rear_Axis_After_Fulgent_Blade_璀璨之刃后Boss后轴线";
            currentProperty.Owner=sourceId;
            currentProperty.Scale=new(0.5f,10);
            currentProperty.Rotation=float.Pi;
            currentProperty.Color=Phase5_Colour_Of_The_Boss_Central_Axis.V4.WithW(25f);
            currentProperty.DestoryAt=9000;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperty);

        }
        
        [ScriptMethod(name:"Phase5 Side To Stack After Fulgent Blade 璀璨之刃(地火)后的分摊侧",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:40310"])]
        
        public void Phase5_Side_To_Stack_After_Fulgent_Blade_璀璨之刃后的分摊侧(Event @event, ScriptAccessory accessory) {
            
            if(!isInPhase5) {

                return;

            }

            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {

                return;

            }

            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            if(myIndex<0||myIndex>7) {

                return;

            }
            
            bool goLeft=false;

            if(myIndex==0
               ||
               myIndex==2
               ||
               myIndex==4
               ||
               myIndex==6) { 
                
                goLeft=true;

            }
            
            if(myIndex==1
               ||
               myIndex==3
               ||
               myIndex==5
               ||
               myIndex==7) { 
                
                goLeft=false;

            }

            var currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            currentProperty.Name="Phase5_Left_Side_After_Fulgent_Blade_璀璨之刃后的左侧";
            currentProperty.Owner=sourceId;
            currentProperty.Scale=new(4);
            currentProperty.Radian=float.Pi;
            currentProperty.Rotation=float.Pi/2;
            currentProperty.Offset=new Vector3(-0.25f,0,0);
            currentProperty.DestoryAt=9000;

            if(Phase5_Boss_Faces_Players_After_Fulgent_Blade) {

                currentProperty.Color=goLeft?
                    accessory.Data.DefaultDangerColor.WithW(25f):
                    accessory.Data.DefaultSafeColor.WithW(25f);

            }

            else {
                
                currentProperty.Color=goLeft?
                    accessory.Data.DefaultSafeColor.WithW(25f):
                    accessory.Data.DefaultDangerColor.WithW(25f);
                
            }
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperty);

            currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            currentProperty.Name="Phase5_Right_Side_After_Fulgent_Blade_璀璨之刃后的右侧";
            currentProperty.Owner=sourceId;
            currentProperty.Scale=new(4);
            currentProperty.Radian=float.Pi;
            currentProperty.Rotation=-(float.Pi/2);
            currentProperty.Offset=new Vector3(0.25f,0,0);
            currentProperty.DestoryAt=9000;
            
            if(Phase5_Boss_Faces_Players_After_Fulgent_Blade) {

                currentProperty.Color=goLeft?
                    accessory.Data.DefaultSafeColor.WithW(25f):
                    accessory.Data.DefaultDangerColor.WithW(25f);

            }

            else {
                
                currentProperty.Color=goLeft?
                    accessory.Data.DefaultDangerColor.WithW(25f):
                    accessory.Data.DefaultSafeColor.WithW(25f);
                
            }
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperty);
            
            if(Enable_Text_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.Method.TextInfo(((goLeft)?("左侧分摊"):("右侧分摊")),9000);
                    
                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.Method.TextInfo(((goLeft)?("Stack on the left"):("Stack on the right")),9000);
                    
                }
                
            }
            
                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.TTS($"{((goLeft) ? ("左侧分摊") : ("右侧分摊"))}", 
                                    Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.TTS($"{((goLeft) ? ("Stack on the left") : ("Stack on the right"))}", 
                                    Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }

        }
        
        [ScriptMethod(name:"Phase5 Initialization Of Wings Dark And Light 光与暗之翼(踩塔)初始化",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:40319"],
            userControl:false)]
        
        public void Phase5_Initialization_Of_Wings_Dark_And_Light_光与暗之翼初始化(Event @event, ScriptAccessory accessory) {
            
            if(!isInPhase5) {

                return;

            }
            
            phase5_hasAcquiredTheFirstTower=false;
            phase5_indexOfTheFirstTower="";
            phase5_hasConfirmedTheInitialPosition=false;
            
        }

        [ScriptMethod(name: "P5_光与暗之翼", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(40313|40233)$"])]
        public void P5_光与暗之翼(Event @event, ScriptAccessory accessory)
        {
            if(!isInPhase5) {

                return;

            }
            
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;

            var r = 225f;
            var rot = (180 - r / 2) / 180f * float.Pi;
            
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_光与暗之翼";
            dp.Scale = new(20);
            dp.Owner = sid;
            dp.Radian = r / 180 * float.Pi;
            dp.TargetObject = accessory.Data.EnmityList[sid][0];
            dp.Rotation = @event["ActionId"] == "40313" ? rot : -rot;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 7300;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_光与暗之翼_远离靠近";
            dp.Scale = new(4);
            dp.Owner = sid;
            dp.CentreResolvePattern = @event["ActionId"] == "40313"? PositionResolvePatternEnum.PlayerFarestOrder: PositionResolvePatternEnum.PlayerNearestOrder;
            dp.Rotation = @event["ActionId"] == "40313" ? rot : -rot;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 7300;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_光与暗之翼";
            dp.Scale = new(20);
            dp.Owner = sid;
            dp.Radian = r / 180 * float.Pi;
            dp.TargetResolvePattern = PositionResolvePatternEnum.OwnerTarget;
            dp.Rotation = @event["ActionId"] == "40313" ? -rot : rot;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 7300;
            dp.DestoryAt = 4000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_光与暗之翼_远离靠近";
            dp.Scale = new(4);
            dp.Owner = sid;
            dp.CentreResolvePattern = @event["ActionId"] == "40313" ? PositionResolvePatternEnum.PlayerNearestOrder : PositionResolvePatternEnum.PlayerFarestOrder;
            dp.Rotation = @event["ActionId"] == "40313" ? rot : -rot;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 7300;
            dp.DestoryAt = 4000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        }
        
        [ScriptMethod(name:"Phase5 Acquire The First Tower Of Wings Dark And Light 获取光与暗之翼(踩塔)一塔",
            eventType:EventTypeEnum.EnvControl,
            eventCondition:["DirectorId:800375BF","State:00010004","Index:regex:^(0000003[012])"],
            userControl:false)]
        
        public void Phase5_Acquire_The_First_Tower_Of_Wings_Dark_And_Light_获取光与暗之翼一塔(Event @event, ScriptAccessory accessory) {
            
            if(!isInPhase5) {

                return;

            }

            if(!phase5_hasAcquiredTheFirstTower) {
                
                phase5_indexOfTheFirstTower=@event["Index"];
                
                phase5_hasAcquiredTheFirstTower=true;
                
            }
            
        }
        
        [ScriptMethod(name:"Phase5 Initial Position Before Towers 踩塔前的起始位置",
            eventType:EventTypeEnum.EnvControl,
            eventCondition:["DirectorId:800375BF","State:00010004","Index:regex:^(0000003[012])"])]
        
        public void Phase5_Initial_Position_Before_Towers_踩塔前的起始位置(Event @event, ScriptAccessory accessory) {
            
            if(!isInPhase5) {

                return;

            }
            
            if(phase5_hasConfirmedTheInitialPosition) {

                return;
                
            }

            else {
                
                phase5_hasConfirmedTheInitialPosition=true;
                
            }
            
            if(!ParseObjectId(phase5_bossId, out var bossId)) {

                return;

            }

            if(!accessory.Data.EnmityList.TryGetValue(bossId, out var enmityListOfBoss)) {

                return;

            }
            
            if(Enable_Developer_Mode) {

                accessory.Method.SendChat($"""
                                           /e 
                                           accessory.Data.Me={accessory.Data.Me}
                                           enmityListOfTheBoss[0]={enmityListOfBoss[0]}

                                           """);

            }
            
            if(accessory.Data.Me!=enmityListOfBoss[0]) {

                return;

            }

            while(!phase5_hasAcquiredTheFirstTower) {
                
                System.Threading.Thread.Sleep(1);
                
            }

            Vector3 positionOfTheFirstTower=new Vector3(0,0,0);

            if(phase5_indexOfTheFirstTower.Equals("00000030")) {

                positionOfTheFirstTower=new Vector3(93.94f,0,96.50f);

            }
            
            if(phase5_indexOfTheFirstTower.Equals("00000031")) {
                
                positionOfTheFirstTower=new Vector3(106.06f,0,96.50f);
                
            }
            
            if(phase5_indexOfTheFirstTower.Equals("00000032")) {
                
                positionOfTheFirstTower=new Vector3(100,0,107);
                
            }

            if(positionOfTheFirstTower.Equals(new Vector3(0,0,0))) {

                return;

            }

            if(Phase5_Strat_Of_Wings_Dark_And_Light==Phase5_Strats_Of_Wings_Dark_And_Light.Grey9_Brain_Dead_灰九脑死法) {

                var currentProperty=accessory.Data.GetDefaultDrawProperties();

                currentProperty.Name="Phase5_Initial_Position_Before_Towers_踩塔前的起始位置";
                currentProperty.Scale=new(2);
                currentProperty.Owner=accessory.Data.Me;
                currentProperty.TargetPosition=RotatePoint(positionOfTheFirstTower,new Vector3(100, 0, 100),float.Pi);
                currentProperty.ScaleMode|=ScaleMode.YByDistance;
                currentProperty.Color=accessory.Data.DefaultSafeColor;
                currentProperty.DestoryAt=2300;

                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, currentProperty);

            }

        }
        
        [ScriptMethod(name:"Phase5 Guidance For Tanks During Towers 坦克踩塔指路",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(40313|40233)$"])]
        
        public void Phase5_Guidance_For_Tanks_During_Towers_坦克踩塔指路(Event @event, ScriptAccessory accessory) {
            
            if(!isInPhase5) {

                return;

            }
            
            if(!phase5_hasAcquiredTheFirstTower) {

                return;
                
            }
            
            if(accessory.Data.PartyList.IndexOf(accessory.Data.Me)!=0
               &&
               accessory.Data.PartyList.IndexOf(accessory.Data.Me)!=1) {

                return;

            }

            bool isCurrentMt=true;
            
            if(!ParseObjectId(phase5_bossId, out var bossId)) {

                return;

            }

            if(!accessory.Data.EnmityList.TryGetValue(bossId, out var enmityListOfBoss)) {

                return;

            }
            
            if(Enable_Developer_Mode) {

                accessory.Method.SendChat($"""
                                           /e 
                                           accessory.Data.Me={accessory.Data.Me}
                                           enmityListOfTheBoss[0]={enmityListOfBoss[0]}

                                           """);

            }
            
            if(accessory.Data.Me==enmityListOfBoss[0]) {

                isCurrentMt=true;

            }

            else {

                isCurrentMt=false;

            }

            bool isLeftFirstAndFarFirst=true;

            if(@event["ActionId"].Equals("40313")) {
                // 40313 stands for left first then right, far first then close.

                isLeftFirstAndFarFirst=true;

            }

            if(@event["ActionId"].Equals("40233")) {
                // 40233 stands for right first then left, close first then far.

                isLeftFirstAndFarFirst=false;

            }

            Vector3 positionOfTheFirstTower=new Vector3(0,0,0);

            if(phase5_indexOfTheFirstTower.Equals("00000030")) {

                positionOfTheFirstTower=new Vector3(93.94f,0,96.50f);

            }
            
            if(phase5_indexOfTheFirstTower.Equals("00000031")) {
                
                positionOfTheFirstTower=new Vector3(106.06f,0,96.50f);
                
            }
            
            if(phase5_indexOfTheFirstTower.Equals("00000032")) {
                
                positionOfTheFirstTower=new Vector3(100,0,107);
                
            }

            if(positionOfTheFirstTower.Equals(new Vector3(0,0,0))) {

                return;

            }

            if(Phase5_Strat_Of_Wings_Dark_And_Light==Phase5_Strats_Of_Wings_Dark_And_Light.Grey9_Brain_Dead_灰九脑死法) {

                Vector3 position1OfCurrentMt=RotatePoint(positionOfTheFirstTower,new Vector3(100,0,100),float.Pi);
                // Just opposite the first tower.
                Vector3 position2OfCurrentMt=isLeftFirstAndFarFirst?
                    new((position1OfCurrentMt.X-100)/7+100,0,(position1OfCurrentMt.Z-100)/7+100):
                    new((position1OfCurrentMt.X-100)/7*18+100,0,(position1OfCurrentMt.Z-100)/7*18+100);
                // The calculations of Position 2 were directly inherited from Karlin's script.
                // I don't know the mathematical ideas behind the algorithm, but it works and it definitely works great.
                // So as a result, except the multiplier was adjusted from 15 to 18, I just keep the part as is.
                
                Vector3 position2OfCurrentOt=RotatePoint(position1OfCurrentMt,new(100,0,100),isLeftFirstAndFarFirst?
                    120f.DegToRad():
                    // Rotate right, since the boss will hit left.
                    -120f.DegToRad());
                    // Rotate left.
                Vector3 position1OfCurrentOt=isLeftFirstAndFarFirst? 
                    new((position2OfCurrentOt.X-100)/7*18+100,0,(position2OfCurrentOt.Z-100)/7*18+100):
                    new((position2OfCurrentOt.X-100)/7+100,0,(position2OfCurrentOt.Z-100)/7+100);
                
                if(isCurrentMt) {

                    var currentProperty=accessory.Data.GetDefaultDrawProperties();
                    currentProperty.Name="Phase5_Guidance_1_For_The_Current_MT_During_Towers_当前MT踩塔指路1";
                    currentProperty.Scale=new(2);
                    currentProperty.Owner=accessory.Data.Me;
                    currentProperty.TargetPosition=position1OfCurrentMt;
                    currentProperty.ScaleMode|=ScaleMode.YByDistance;
                    currentProperty.Color=accessory.Data.DefaultSafeColor;
                    currentProperty.DestoryAt=7150;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                    currentProperty=accessory.Data.GetDefaultDrawProperties();
                    currentProperty.Name="Phase5_Guidance_2_Preview_For_The_Current_MT_During_Towers_当前MT踩塔指路2预览";
                    currentProperty.Scale=new(2);
                    currentProperty.Position=position1OfCurrentMt;
                    currentProperty.TargetPosition=position2OfCurrentMt;
                    currentProperty.ScaleMode|=ScaleMode.YByDistance;
                    currentProperty.Color=accessory.Data.DefaultSafeColor;
                    currentProperty.DestoryAt=7150;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                    currentProperty=accessory.Data.GetDefaultDrawProperties();
                    currentProperty.Name="Phase5_Guidance_2_For_The_Current_MT_During_Towers_当前MT踩塔指路2";
                    currentProperty.Scale=new(2);
                    currentProperty.Owner=accessory.Data.Me;
                    currentProperty.TargetPosition=position2OfCurrentMt;
                    currentProperty.ScaleMode|=ScaleMode.YByDistance;
                    currentProperty.Color=accessory.Data.DefaultSafeColor;
                    currentProperty.Delay=7150;
                    currentProperty.DestoryAt=4250;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                    
                    if(Phase5_Reminder_To_Provoke) {
                        
                        System.Threading.Thread.Sleep(1500);
                            
                        if(Enable_Text_Prompts) {

                            if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                                accessory.Method.TextInfo("等待挑衅后退避",2500);
                    
                            }

                            if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                                accessory.Method.TextInfo("Wait for provocation then shirk",2500);
                    
                            }
                
                        }
                        
                        if (Enable_TTS_Prompts || Enable_DRTTS_Prompts) {

                            if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                                accessory.TTS("等待挑衅后退避", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                            }

                            if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                                accessory.TTS("Wait for provocation then shirk", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                            }
                
                        }
                        
                    }

                }

                else {

                    var currentProperty=accessory.Data.GetDefaultDrawProperties();
                    currentProperty.Name="Phase5_Guidance_1_For_The_Current_OT_During_Towers_当前ST踩塔指路1";
                    currentProperty.Scale=new(2);
                    currentProperty.Owner=accessory.Data.Me;
                    currentProperty.TargetPosition=position1OfCurrentOt;
                    currentProperty.ScaleMode|=ScaleMode.YByDistance;
                    currentProperty.Color=accessory.Data.DefaultSafeColor;
                    currentProperty.DestoryAt=7650;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                    currentProperty=accessory.Data.GetDefaultDrawProperties();
                    currentProperty.Name="Phase5_Guidance_2_Preview_For_The_Current_OT_During_Towers_当前ST踩塔指路2预览";
                    currentProperty.Scale=new(2);
                    currentProperty.Position=position1OfCurrentOt;
                    currentProperty.TargetPosition=position2OfCurrentOt;
                    currentProperty.ScaleMode|=ScaleMode.YByDistance;
                    currentProperty.Color=accessory.Data.DefaultSafeColor;
                    currentProperty.DestoryAt=7650;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                    
                    currentProperty=accessory.Data.GetDefaultDrawProperties();
                    currentProperty.Name="Phase5_Guidance_2_For_The_Current_OT_During_Towers_当前ST踩塔指路2";
                    currentProperty.Scale=new(2);
                    currentProperty.Owner=accessory.Data.Me;
                    currentProperty.TargetPosition=position2OfCurrentOt;
                    currentProperty.ScaleMode|=ScaleMode.YByDistance;
                    currentProperty.Color=accessory.Data.DefaultSafeColor;
                    currentProperty.Delay=7650;
                    currentProperty.DestoryAt=3750;
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                    if(Phase5_Reminder_To_Provoke) {
                        
                        System.Threading.Thread.Sleep(1000);
                        
                        if(Enable_Text_Prompts) {

                            if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                                accessory.Method.TextInfo("立即挑衅！",2500);
                    
                            }

                            if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                                accessory.Method.TextInfo("Now provoke!",2500);
                    
                            }
                
                        }
                        
                        if (Enable_TTS_Prompts || Enable_DRTTS_Prompts) {

                            if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                                accessory.TTS("立即挑衅！", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                            }

                            if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                                accessory.TTS("Now provoke!", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                            }
                
                        }
                        
                    }

                }
                
            }

        }
        
        [ScriptMethod(name:"Phase5 Guidance For Others During Towers 人群踩塔指路",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(40313|40233)$"])]
        
        public void Phase5_Guidance_For_Others_During_Towers_人群踩塔指路(Event @event, ScriptAccessory accessory) {
            
            if(!isInPhase5) {

                return;

            }
            
            if(!phase5_hasAcquiredTheFirstTower) {

                return;
                
            }
            
            if(accessory.Data.PartyList.IndexOf(accessory.Data.Me)==0
               ||
               accessory.Data.PartyList.IndexOf(accessory.Data.Me)==1) {

                return;

            }

            bool isLeftFirstAndFarFirst=true;

            if(@event["ActionId"].Equals("40313")) {

                isLeftFirstAndFarFirst=true;

            }

            if(@event["ActionId"].Equals("40233")) {

                isLeftFirstAndFarFirst=false;

            }
            
            if(Phase5_Strat_Of_Wings_Dark_And_Light==Phase5_Strats_Of_Wings_Dark_And_Light.Grey9_Brain_Dead_灰九脑死法) {
                
                var currentProperty=accessory.Data.GetDefaultDrawProperties();

                if(phase5_indexOfTheFirstTower.Equals("00000030")) {
                    // The first tower is in the northwest.
                    
                    if(accessory.Data.PartyList.IndexOf(accessory.Data.Me)==4
                       ||
                       accessory.Data.PartyList.IndexOf(accessory.Data.Me)==5) {
                        // The melee group, that is M1 & M2 or D1 & D2.
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_1_For_Melees_During_Towers_近战踩塔指路1";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=isLeftFirstAndFarFirst?phase5_rightSideOfTheNorthwest_asAConstant:phase5_leftSideOfTheNorthwest_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=7300;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_Preview_For_Melees_During_Towers_近战踩塔指路2预览";
                        currentProperty.Scale=new(2);
                        currentProperty.Position=isLeftFirstAndFarFirst?phase5_rightSideOfTheNorthwest_asAConstant:phase5_leftSideOfTheNorthwest_asAConstant;
                        currentProperty.TargetPosition=phase5_standbyPointBetweenSouthAndNortheast_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=7300;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_For_Melees_During_Towers_近战踩塔指路2";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=phase5_standbyPointBetweenSouthAndNortheast_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.Delay=7300;
                        currentProperty.DestoryAt=7100;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                    }
                    
                    if(accessory.Data.PartyList.IndexOf(accessory.Data.Me)==6
                       ||
                       accessory.Data.PartyList.IndexOf(accessory.Data.Me)==7) {
                        // The range group, that is R1 & R2 or D3 & D4.
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_1_For_Ranges_During_Towers_远程踩塔指路1";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=isLeftFirstAndFarFirst?phase5_standbyPointBetweenSouthAndNorthwest_asAConstant:phase5_standbyPointBetweenNortheastAndNorthwest_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=6900;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_Preview_For_Ranges_During_Towers_远程踩塔指路2预览";
                        currentProperty.Scale=new(2);
                        currentProperty.Position=isLeftFirstAndFarFirst?phase5_standbyPointBetweenSouthAndNorthwest_asAConstant:phase5_standbyPointBetweenNortheastAndNorthwest_asAConstant;
                        currentProperty.TargetPosition=phase5_rightSideOfTheSouth_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=6900;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_For_Ranges_During_Towers_远程踩塔指路2";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=phase5_rightSideOfTheSouth_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.Delay=6900;
                        currentProperty.DestoryAt=7500;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                    }
                    
                    if(accessory.Data.PartyList.IndexOf(accessory.Data.Me)==2
                       ||
                       accessory.Data.PartyList.IndexOf(accessory.Data.Me)==3) {
                        // The healer group, that is H1 & H2.
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_1_For_Healers_During_Towers_奶妈踩塔指路1";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=isLeftFirstAndFarFirst?phase5_standbyPointBetweenSouthAndNorthwest_asAConstant:phase5_standbyPointBetweenNortheastAndNorthwest_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=6900;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_Preview_For_Healers_During_Towers_奶妈踩塔指路2预览";
                        currentProperty.Scale=new(2);
                        currentProperty.Position=isLeftFirstAndFarFirst?phase5_standbyPointBetweenSouthAndNorthwest_asAConstant:phase5_standbyPointBetweenNortheastAndNorthwest_asAConstant;
                        currentProperty.TargetPosition=phase5_leftSideOfTheNortheast_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=6900;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_For_Healers_During_Towers_奶妈踩塔指路2";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=phase5_leftSideOfTheNortheast_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.Delay=6900;
                        currentProperty.DestoryAt=7500;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                    }

                }
            
                if(phase5_indexOfTheFirstTower.Equals("00000031")) {
                    // The first tower is in the northeast.
                    
                    if(accessory.Data.PartyList.IndexOf(accessory.Data.Me)==4
                       ||
                       accessory.Data.PartyList.IndexOf(accessory.Data.Me)==5) {
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_1_For_Melees_During_Towers_近战踩塔指路1";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=isLeftFirstAndFarFirst?phase5_rightSideOfTheNortheast_asAConstant:phase5_leftSideOfTheNortheast_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=7300;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_Preview_For_Melees_During_Towers_近战踩塔指路2预览";
                        currentProperty.Scale=new(2);
                        currentProperty.Position=isLeftFirstAndFarFirst?phase5_rightSideOfTheNortheast_asAConstant:phase5_leftSideOfTheNortheast_asAConstant;
                        currentProperty.TargetPosition=phase5_standbyPointBetweenSouthAndNorthwest_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=7300;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_For_Melees_During_Towers_近战踩塔指路2";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=phase5_standbyPointBetweenSouthAndNorthwest_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.Delay=7300;
                        currentProperty.DestoryAt=7100;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                    }
                    
                    if(accessory.Data.PartyList.IndexOf(accessory.Data.Me)==6
                       ||
                       accessory.Data.PartyList.IndexOf(accessory.Data.Me)==7) {
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_1_For_Ranges_During_Towers_远程踩塔指路1";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=isLeftFirstAndFarFirst?phase5_standbyPointBetweenNortheastAndNorthwest_asAConstant:phase5_standbyPointBetweenSouthAndNortheast_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=6900;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_Preview_For_Ranges_During_Towers_远程踩塔指路2预览";
                        currentProperty.Scale=new(2);
                        currentProperty.Position=isLeftFirstAndFarFirst?phase5_standbyPointBetweenNortheastAndNorthwest_asAConstant:phase5_standbyPointBetweenSouthAndNortheast_asAConstant;
                        currentProperty.TargetPosition=phase5_rightSideOfTheNorthwest_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=6900;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_For_Ranges_During_Towers_远程踩塔指路2";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=phase5_rightSideOfTheNorthwest_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.Delay=6900;
                        currentProperty.DestoryAt=7500;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                    }
                    
                    if(accessory.Data.PartyList.IndexOf(accessory.Data.Me)==2
                       ||
                       accessory.Data.PartyList.IndexOf(accessory.Data.Me)==3) {
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_1_For_Healers_During_Towers_奶妈踩塔指路1";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=isLeftFirstAndFarFirst?phase5_standbyPointBetweenNortheastAndNorthwest_asAConstant:phase5_standbyPointBetweenSouthAndNortheast_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=6900;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_Preview_For_Healers_During_Towers_奶妈踩塔指路2预览";
                        currentProperty.Scale=new(2);
                        currentProperty.Position=isLeftFirstAndFarFirst?phase5_standbyPointBetweenNortheastAndNorthwest_asAConstant:phase5_standbyPointBetweenSouthAndNortheast_asAConstant;
                        currentProperty.TargetPosition=phase5_leftSideOfTheSouth_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=6900;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_For_Healers_During_Towers_奶妈踩塔指路2";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=phase5_leftSideOfTheSouth_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.Delay=6900;
                        currentProperty.DestoryAt=7500;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                    }
                
                }
            
                if(phase5_indexOfTheFirstTower.Equals("00000032")) {
                    // The first tower is in the south.
                    
                    if(accessory.Data.PartyList.IndexOf(accessory.Data.Me)==4
                       ||
                       accessory.Data.PartyList.IndexOf(accessory.Data.Me)==5) {
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_1_For_Melees_During_Towers_近战踩塔指路1";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=isLeftFirstAndFarFirst?phase5_rightSideOfTheSouth_asAConstant:phase5_leftSideOfTheSouth_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=7300;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_Preview_For_Melees_During_Towers_近战踩塔指路2预览";
                        currentProperty.Scale=new(2);
                        currentProperty.Position=isLeftFirstAndFarFirst?phase5_rightSideOfTheSouth_asAConstant:phase5_leftSideOfTheSouth_asAConstant;
                        currentProperty.TargetPosition=phase5_standbyPointBetweenNortheastAndNorthwest_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=7300;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_For_Melees_During_Towers_近战踩塔指路2";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=phase5_standbyPointBetweenNortheastAndNorthwest_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.Delay=7300;
                        currentProperty.DestoryAt=7100;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                    }
                    
                    if(accessory.Data.PartyList.IndexOf(accessory.Data.Me)==6
                       ||
                       accessory.Data.PartyList.IndexOf(accessory.Data.Me)==7) {
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_1_For_Ranges_During_Towers_远程踩塔指路1";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=isLeftFirstAndFarFirst?phase5_standbyPointBetweenSouthAndNortheast_asAConstant:phase5_standbyPointBetweenSouthAndNorthwest_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=6900;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_Preview_For_Ranges_During_Towers_远程踩塔指路2预览";
                        currentProperty.Scale=new(2);
                        currentProperty.Position=isLeftFirstAndFarFirst?phase5_standbyPointBetweenSouthAndNortheast_asAConstant:phase5_standbyPointBetweenSouthAndNorthwest_asAConstant;
                        currentProperty.TargetPosition=phase5_rightSideOfTheNortheast_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=6900;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_For_Ranges_During_Towers_远程踩塔指路2";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=phase5_rightSideOfTheNortheast_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.Delay=6900;
                        currentProperty.DestoryAt=7500;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                    }
                    
                    if(accessory.Data.PartyList.IndexOf(accessory.Data.Me)==2
                       ||
                       accessory.Data.PartyList.IndexOf(accessory.Data.Me)==3) {
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_1_For_Healers_During_Towers_奶妈踩塔指路1";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=isLeftFirstAndFarFirst?phase5_standbyPointBetweenSouthAndNortheast_asAConstant:phase5_standbyPointBetweenSouthAndNorthwest_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=6900;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                        
                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_Preview_For_Healers_During_Towers_奶妈踩塔指路2预览";
                        currentProperty.Scale=new(2);
                        currentProperty.Position=isLeftFirstAndFarFirst?phase5_standbyPointBetweenSouthAndNortheast_asAConstant:phase5_standbyPointBetweenSouthAndNorthwest_asAConstant;
                        currentProperty.TargetPosition=phase5_leftSideOfTheNorthwest_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.DestoryAt=6900;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                        currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperty.Name="Phase5_Guidance_2_For_Healers_During_Towers_奶妈踩塔指路2";
                        currentProperty.Scale=new(2);
                        currentProperty.Owner=accessory.Data.Me;
                        currentProperty.TargetPosition=phase5_leftSideOfTheNorthwest_asAConstant;
                        currentProperty.ScaleMode|=ScaleMode.YByDistance;
                        currentProperty.Color=accessory.Data.DefaultSafeColor;
                        currentProperty.Delay=6900;
                        currentProperty.DestoryAt=7500;
                        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);

                    }
                
                }

                
            }

        }
        
        [ScriptMethod(name:"Phase5 Boss Central Axis During Polarizing Strikes 极化打击(挡枪)期间Boss中轴线",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:40316"])]
        
        public void Phase5_Boss_Central_Axis_During_Polarizing_Strikes_极化打击期间Boss中轴线(Event @event, ScriptAccessory accessory) {
            
            if(!isInPhase5) {

                return;

            }

            if(!ParseObjectId(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }

            var currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            currentProperty.Name="Phase5_Boss_Front_Axis_During_Polarizing_Strikes_极化打击期间Boss前轴线";
            currentProperty.Owner=sourceId;
            currentProperty.Scale=new(0.5f,10);
            currentProperty.Color=Phase5_Colour_Of_The_Boss_Central_Axis.V4.WithW(25f);
            currentProperty.DestoryAt=24000;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperty);

            currentProperty=accessory.Data.GetDefaultDrawProperties();
            
            currentProperty.Name="Phase5_Boss_Rear_Axis_During_Polarizing_Strikes_极化打击期间Boss后轴线";
            currentProperty.Owner=sourceId;
            currentProperty.Scale=new(0.5f,10);
            currentProperty.Rotation=float.Pi;
            currentProperty.Color=Phase5_Colour_Of_The_Boss_Central_Axis.V4.WithW(25f);
            currentProperty.DestoryAt=24000;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperty);

        }
        
        [ScriptMethod(name:"Phase5 Guidance Of Polarizing Strikes 极化打击(挡枪)指路",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:40316"])]
        
        public void Phase5_Guidance_Of_Polarizing_Strikes_极化打击指路(Event @event, ScriptAccessory accessory) {
            
            if(!isInPhase5) {

                return;

            }

            if(!float.TryParse(@event["SourceRotation"], out float currentRotation)) {

                return;

            }

            currentRotation=-(currentRotation-float.Pi);
            
            if(Enable_Developer_Mode) {

                accessory.Method.SendChat($"""
                                           /e 
                                           currentRotation={currentRotation}

                                           """);

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            int myRoundToTakeHits=phase5_getRoundToTakeHits(myIndex);
            bool inTheLeftGroup=true;
            int timelineControl=0;
            int timeToTakeHits=0;
            var currentProperty=accessory.Data.GetDefaultDrawProperties();

            if(myRoundToTakeHits<1||myRoundToTakeHits>4) {

                return;

            } 

            if(myIndex==0
               ||
               myIndex==2
               ||
               myIndex==4
               ||
               myIndex==6) {

                inTheLeftGroup=true;

            }
            
            if(myIndex==1
               ||
               myIndex==3
               ||
               myIndex==5
               ||
               myIndex==7) {

                inTheLeftGroup=false;

            }
            
            // ----- Initial guidance -----

            if(myRoundToTakeHits==1) {
                
                currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                currentProperty.Name="Phase5_Initial_Guidance_Of_Polarizing_Strikes_极化打击初始指路";
                currentProperty.Scale=new(2);
                currentProperty.Owner=accessory.Data.Me;
                currentProperty.TargetPosition=inTheLeftGroup?
                    RotatePoint(phase5_positionToTakeHitsOnTheLeft_asAConstant,new Vector3(100,0,100),currentRotation):
                    RotatePoint(phase5_positionToTakeHitsOnTheRight_asAConstant,new Vector3(100,0,100),currentRotation);
                currentProperty.ScaleMode|=ScaleMode.YByDistance;
                currentProperty.Color=accessory.Data.DefaultSafeColor;
                currentProperty.DestoryAt=4550;
                timelineControl+=4550;
                        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                
            }

            else {
                
                currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                currentProperty.Name="Phase5_Initial_Guidance_Of_Polarizing_Strikes_极化打击初始指路";
                currentProperty.Scale=new(2);
                currentProperty.Owner=accessory.Data.Me;
                currentProperty.TargetPosition=inTheLeftGroup?
                    RotatePoint(phase5_positionToBeCoveredOnTheLeft_asAConstant,new Vector3(100,0,100),currentRotation):
                    RotatePoint(phase5_positionToBeCoveredOnTheRight_asAConstant,new Vector3(100,0,100),currentRotation);
                currentProperty.ScaleMode|=ScaleMode.YByDistance;
                currentProperty.Color=accessory.Data.DefaultSafeColor;
                currentProperty.DestoryAt=4550;
                timelineControl+=4550;
                        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                
            }
            
            // ----- Be covered in the current group -----

            for(int i=1;i<myRoundToTakeHits;++i) {
                
                currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                currentProperty.Name="Phase5_Inward_Guidance_Of_Polarizing_Strikes_In_The_Current_Group_极化打击当前组进指路";
                currentProperty.Scale=new(2);
                currentProperty.Owner=accessory.Data.Me;
                currentProperty.TargetPosition=inTheLeftGroup?
                    RotatePoint(phase5_positionToBeCoveredOnTheLeft_asAConstant,new Vector3(100,0,100),currentRotation):
                    RotatePoint(phase5_positionToBeCoveredOnTheRight_asAConstant,new Vector3(100,0,100),currentRotation);
                currentProperty.ScaleMode|=ScaleMode.YByDistance;
                currentProperty.Color=accessory.Data.DefaultSafeColor;
                currentProperty.Delay=timelineControl;
                currentProperty.DestoryAt=2450;
                timelineControl+=2450;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                
                currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                currentProperty.Name="Phase5_Outward_Guidance_Of_Polarizing_Strikes_In_The_Current_Group_极化打击当前组出指路";
                currentProperty.Scale=new(2);
                currentProperty.Owner=accessory.Data.Me;
                currentProperty.TargetPosition=inTheLeftGroup?
                    RotatePoint(phase5_positionToStandbyOnTheLeft_asAConstant,new Vector3(100,0,100),currentRotation):
                    RotatePoint(phase5_positionToStandbyOnTheRight_asAConstant,new Vector3(100,0,100),currentRotation);
                currentProperty.ScaleMode|=ScaleMode.YByDistance;
                currentProperty.Color=accessory.Data.DefaultSafeColor;
                currentProperty.Delay=timelineControl;
                currentProperty.DestoryAt=2250;
                timelineControl+=2250;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                
            }
            
            // ----- -----
            
            // ----- Take hits and swap the group -----

            timeToTakeHits=timelineControl-250;
            
            currentProperty=accessory.Data.GetDefaultDrawProperties();
                
            currentProperty.Name="Phase5_Inward_Guidance_Of_Polarizing_Strikes_While_Taking_Hits_极化打击挡枪进指路";
            currentProperty.Scale=new(2);
            currentProperty.Owner=accessory.Data.Me;
            currentProperty.TargetPosition=inTheLeftGroup?
                RotatePoint(phase5_positionToTakeHitsOnTheLeft_asAConstant,new Vector3(100,0,100),currentRotation):
                RotatePoint(phase5_positionToTakeHitsOnTheRight_asAConstant,new Vector3(100,0,100),currentRotation);
            currentProperty.ScaleMode|=ScaleMode.YByDistance;
            currentProperty.Color=accessory.Data.DefaultSafeColor;
            currentProperty.Delay=timelineControl;
            currentProperty.DestoryAt=2450;
            timelineControl+=2450;
                
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                
            currentProperty=accessory.Data.GetDefaultDrawProperties();
                
            currentProperty.Name="Phase5_Outward_Guidance_Of_Polarizing_Strikes_While_Taking_Hits_极化打击挡枪出指路";
            currentProperty.Scale=new(2);
            currentProperty.Owner=accessory.Data.Me;
            currentProperty.TargetPosition=inTheLeftGroup?
                RotatePoint(phase5_positionToStandbyOnTheRight_asAConstant,new Vector3(100,0,100),currentRotation):
                RotatePoint(phase5_positionToStandbyOnTheLeft_asAConstant,new Vector3(100,0,100),currentRotation);
            currentProperty.ScaleMode|=ScaleMode.YByDistance;
            currentProperty.Color=accessory.Data.DefaultSafeColor;
            currentProperty.Delay=timelineControl;
            currentProperty.DestoryAt=2250;
            timelineControl+=2250;
                
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
            
            // ----- -----
            
            // ----- Be covered in the opposite group -----
            
            for(int i=myRoundToTakeHits+1;i<=4;++i) {
                
                currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                currentProperty.Name="Phase5_Inward_Guidance_Of_Polarizing_Strikes_In_The_Opposite_Group_极化打击对组进指路";
                currentProperty.Scale=new(2);
                currentProperty.Owner=accessory.Data.Me;
                currentProperty.TargetPosition=inTheLeftGroup?
                    RotatePoint(phase5_positionToBeCoveredOnTheRight_asAConstant,new Vector3(100,0,100),currentRotation):
                    RotatePoint(phase5_positionToBeCoveredOnTheLeft_asAConstant,new Vector3(100,0,100),currentRotation);
                currentProperty.ScaleMode|=ScaleMode.YByDistance;
                currentProperty.Color=accessory.Data.DefaultSafeColor;
                currentProperty.Delay=timelineControl;
                currentProperty.DestoryAt=2450;
                timelineControl+=2450;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                
                currentProperty=accessory.Data.GetDefaultDrawProperties();
                
                currentProperty.Name="Phase5_Outward_Guidance_Of_Polarizing_Strikes_In_The_Opposite_Group_极化打击对组出指路";
                currentProperty.Scale=new(2);
                currentProperty.Owner=accessory.Data.Me;
                currentProperty.TargetPosition=inTheLeftGroup?
                    RotatePoint(phase5_positionToStandbyOnTheRight_asAConstant,new Vector3(100,0,100),currentRotation):
                    RotatePoint(phase5_positionToStandbyOnTheLeft_asAConstant,new Vector3(100,0,100),currentRotation);
                currentProperty.ScaleMode|=ScaleMode.YByDistance;
                currentProperty.Color=accessory.Data.DefaultSafeColor;
                currentProperty.Delay=timelineControl;
                currentProperty.DestoryAt=2250;
                timelineControl+=2250;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperty);
                
            }
            
            System.Threading.Thread.Sleep(timeToTakeHits);
            
            if(Enable_Text_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.Method.TextInfo("挡枪然后换组",1500);
                    
                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.Method.TextInfo("Take hits and swap the group",1500);
                    
                }
                
            }
            
            if (Enable_TTS_Prompts || Enable_DRTTS_Prompts) {

                if(Language_Of_Prompts==Languages_Of_Prompts.Simplified_Chinese_简体中文) {
                    
                    accessory.TTS("挡枪然后换组", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }

                if(Language_Of_Prompts==Languages_Of_Prompts.English_英文) {
                    
                    accessory.TTS("Take hits and swap the group", Enable_TTS_Prompts, Enable_DRTTS_Prompts);

                }
                
            }

        }

        private int phase5_getRoundToTakeHits(int currentIndex) {

            if(Phase5_Order_During_Polarizing_Strikes==Phase5_Orders_During_Polarizing_Strikes.Tanks_Melees_Ranges_Healers_坦克近战远程奶妈) {

                if(currentIndex==0||currentIndex==1) {
                    // Tanks.

                    return 1;

                }
                
                if(currentIndex==4||currentIndex==5) {
                    // Melees.

                    return 2;

                }
                
                if(currentIndex==6||currentIndex==7) {
                    // Ranges.

                    return 3;

                }
                
                if(currentIndex==2||currentIndex==3) {
                    // Healers.

                    return 4;

                }
                
            }
            
            if(Phase5_Order_During_Polarizing_Strikes==Phase5_Orders_During_Polarizing_Strikes.Tanks_Healers_Melees_Ranges_坦克奶妈近战远程) {
                
                if(currentIndex==0||currentIndex==1) {
                    // Tanks.

                    return 1;

                }
                
                if(currentIndex==2||currentIndex==3) {
                    // Healers.

                    return 2;

                }
                
                if(currentIndex==4||currentIndex==5) {
                    // Melees.

                    return 3;

                }
                
                if(currentIndex==6||currentIndex==7) {
                    // Ranges.

                    return 4;

                }
                
            }

            return -1;
            // Just a placeholder and should never be reached.

        }

        #endregion

        private int ParsTargetIcon(string id)
        {
            firstTargetIcon ??= int.Parse(id, System.Globalization.NumberStyles.HexNumber);
            return int.Parse(id, System.Globalization.NumberStyles.HexNumber) - (int)firstTargetIcon;
        }
        private static bool ParseObjectId(string? idStr, out ulong id)
        {
            id = 0;
            if (string.IsNullOrEmpty(idStr)) return false;
            try
            {
                var idStr2 = idStr.Replace("0x", "");
                id = ulong.Parse(idStr2, System.Globalization.NumberStyles.HexNumber);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// 向近的取
        /// </summary>
        /// <param name="point"></param>
        /// <param name="centre"></param>
        /// <returns></returns>
        private int PositionTo8Dir(Vector3 point, Vector3 centre)
        {
            // Dirs: N = 0, NE = 1, ..., NW = 7
            var r = Math.Round(4 - 4 * Math.Atan2(point.X - centre.X, point.Z - centre.Z) / Math.PI) % 8;
            return (int)r;

        }
        private int PositionTo6Dir(Vector3 point, Vector3 centre)
        {
            var r = Math.Round(3 - 3 * Math.Atan2(point.X - centre.X, point.Z - centre.Z) / Math.PI) % 6;
            return (int)r;

        }
        private Vector3 RotatePoint(Vector3 point, Vector3 centre, float radian)
        {

            Vector2 v2 = new(point.X - centre.X, point.Z - centre.Z);

            var rot = (MathF.PI - MathF.Atan2(v2.X, v2.Y) + radian);
            var lenth = v2.Length();
            return new(centre.X + MathF.Sin(rot) * lenth, centre.Y, centre.Z - MathF.Cos(rot) * lenth);
        }
    }
}

public static class Extensions
{
    public static void TTS(this ScriptAccessory accessory, string text, bool isTTS, bool isDRTTS)
    {
        if (isTTS && isDRTTS) accessory.Method.TTS(text);
        if (isDRTTS)
        {
            accessory.Method.SendChat($"/pdr tts {text}");
        }
        else if (isTTS)
        {
            accessory.Method.TTS(text);
        }
    }
}

