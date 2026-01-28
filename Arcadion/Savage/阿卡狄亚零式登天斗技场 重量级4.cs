using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Numerics;
using System.Linq;
using System.Diagnostics;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Module.Draw;
using KodakkuAssist.Module.GameOperate;
using KodakkuAssist.Script;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Dalamud.Utility.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.STD.Helper;
using Lumina.Data.Parsing;

namespace CicerosKodakkuAssist.Arcadion.Savage.Heavyweight.ChinaDataCenter
{

    [ScriptType(name:"阿卡狄亚零式登天斗技场 重量级4",
        territorys:[1327],
        guid:"d1d8375c-75e4-49a8-8764-aab85a982f0a",
        version:"0.0.2.6",
        note:scriptNotes,
        author:"Cicero 灵视")]

    public class AAC_Heavyweight_M4_Savage
    {
        
        public const string scriptNotes=
            """
            阿卡狄亚零式登天斗技场重量级4(也就是M12S)的脚本。
            
            脚本已经基本完工,目前正在进行少量精修。
            
            此脚本适配的攻略是M12S整合文档攻略,本体境中奇梦(四运)适配的攻略是文档中提及的盗火改。
            M12S整合文档攻略 - 门神: https://docs.qq.com/doc/DUHZiZU54ZGx5eGZV
            M12S整合文档攻略 - 本体: https://docs.qq.com/doc/DUEVnSkFnU0hqdHRt
            如果脚本中的指路不适配你采用的攻略,可以在方法设置中将指路关闭。所有指路方法名称中均标注有"指路"一词。

            如果在使用过程中遇到了电椅或异常,请先检查可达鸭本体与脚本是否更新到了最新版本,小队职能是否正确设置,错误是否可以稳定复现。
            如果上述三项都没有问题,请带着A Realm Recorded插件的录像在可达鸭Discord内联系@_publius_cornelius_scipio_反馈错误。
            """;
        
        #region User_Settings
        
        [UserSetting("通用 启用文字提示")]
        public bool enablePrompts { get; set; } = false;
        [UserSetting("通用 启用原生TTS")]
        public bool enableVanillaTts { get; set; } = false;
        [UserSetting("通用 启用Daily Routines TTS (需要安装并启用Daily Routines插件!)")]
        public bool enableDailyRoutinesTts { get; set; } = false;
        [UserSetting("通用 机制方向的颜色")]
        public ScriptColor colourOfDirectionIndicators { get; set; } = new() { V4 = new Vector4(1,1,0, 1) }; // Yellow by default.
        [UserSetting("通用 高度危险攻击的颜色")]
        public ScriptColor colourOfExtremelyDangerousAttacks { get; set; } = new() { V4 = new Vector4(1,0,0,1) }; // Red by default.
        [UserSetting("通用 启用搞怪")]
        public bool enableShenanigans { get; set; } = false;
        [UserSetting("调试 启用调试日志并输出到Dalamud日志中")]
        public bool enableDebugLogging { get; set; } = false;
        [UserSetting("调试 忽略所有阶段检查")]
        public bool skipPhaseChecks { get; set; } = false;
        [UserSetting("调试 在阶段切换时保留绘制")]
        public bool preserveDrawingsWhileSwitchingPhase { get; set; } = false;
        [UserSetting("调试 从本体开始")]
        public bool startFromMajorPhase2 { get; set; } = false;
        
        // ----- Major Phase 1 -----
        
        /*
        
        [UserSetting("致命灾变引导顺序")]
        public OrdersDuringMortalSlayer orderDuringMortalSlayer { get; set; }
        
        */
        
        [UserSetting("门神 致命灾变 绿球的颜色")]
        public ScriptColor colourOfGreenSpheres { get; set; } = new() { V4 = new Vector4(0,1,0,1) }; // Green by default.
        [UserSetting("门神 致命灾变 紫球的颜色")]
        public ScriptColor colourOfPurpleSpheres { get; set; } = new() { V4 = new Vector4(0.5f,0,0.5f,1) }; // Purple by default.
        [UserSetting("门神 细胞附身·早期 引爆细胞范围绘制延迟(秒,默认11,最大17)")]
        public double grotesquerieStatusDelay { get; set; } = 11; // 11 by default.
        [UserSetting("门神 细胞附身·早期 仅绘制自己的引爆细胞:指向")]
        public bool onlyMyDirectedGrotesquerie { get; set; } = true;
        [UserSetting("门神 细胞附身·晚期 仅绘制自己的分散细胞")]
        public bool onlyMyMitoticPhase { get; set; } = true;
        [UserSetting("门神 细胞附身·晚期 滴液灾变范围绘制延迟(秒,默认7.25,最大9.75)")]
        public double venomousScourgeDelay { get; set; } = 7.25; // 7.25 by default.
        
        // ----- End Of Major Phase 1 -----
        
        // ----- Major Phase 2 -----
        
        [UserSetting("本体 魔力爆发的颜色")]
        public ScriptColor colourOfManaBurst { get; set; } = new() { V4 = new Vector4(0.5f,0,0.5f,1) }; // Purple by default.
        [UserSetting("本体 自我复制(一运) 强力魔法的颜色")]
        public ScriptColor colourOfMightyMagic { get; set; } = new() { V4 = new Vector4(0.5f,0,0.5f,1) }; // Purple by default.
        [UserSetting("本体 自我复制(一运) 天顶猛击的颜色")]
        public ScriptColor colourOfTopTierSlam { get; set; } = new() { V4 = new Vector4(1,0,0,1) }; // Red by default.
        [UserSetting("本体 模仿细胞(二运) 落火飞溅的颜色")]
        public ScriptColor colourOfFirefallSplash { get; set; } = new() { V4 = new Vector4(1,0,0,1) }; // Red by default.
        [UserSetting("本体 变异细胞(三运) 坦克单独撞球")]
        public bool tankStackSoloDuringMutatingCells { get; set; } = false;
        [UserSetting("本体 境中奇梦(四运) 场地指北针")]
        public bool northIndicatorDuringIdyllicDream { get; set; } = true;
        [UserSetting("本体 境中奇梦(四运) 场地指北针的颜色")]
        public ScriptColor colourOfNorthIndicator { get; set; } = new() { V4 = new Vector4(0,1,1, 1) }; // Blue by default.
        [UserSetting("本体 境中奇梦(四运) 启用小队指挥")]
        public bool enablePartyAssistance { get; set; } = false;
        
        // ----- End Of Major Phase 2 -----

        #endregion
        
        #region Variables_And_Semaphores
        
        private volatile bool isInMajorPhase1=true;
        private volatile bool initializedMajorPhase1=false;
        private volatile bool initializedMajorPhase2=false;
        private volatile int currentPhase=0;
        
        /*
         
        Major Phase 1:
        
            Phase 1 - 致命灾变
            Phase 2 - 细胞附身·早期
            Phase 3 - 细胞附身·中期
            Phase 4 - 细胞附身·晚期
            Phase 5 - 细胞附身·末期
            Phase 6 - 致命灾变
            Phase 7 - 喋血
            Phase 8 - 喋血
            Phase 9 - 喋血
        
        Major Phase 2:
        
            Phase 1 - 自我复制(一运)
            Phase 2 - 模仿细胞(二运)
            Phase 3 - 变异细胞(三运)
            Phase 4 - 境中奇梦(四运)
            Phase 5 - 狂暴
         
        */
        
        // ----- Major Phase 1 -----

        private List<sphereType> sphere=new List<sphereType>();
        private List<int> leftOrder=new List<int>();
        private List<int> rightOrder=new List<int>();
        private System.Threading.AutoResetEvent mortalSlayerSphereSemaphore=new System.Threading.AutoResetEvent(false); 
        private System.Threading.AutoResetEvent mortalSlayerRangeSemaphore=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent mortalSlayerGuidanceSemaphore=new System.Threading.AutoResetEvent(false);

        private volatile int act2PartyCount=0;
        private act2PartyType[] act2Party=Enumerable.Range(0,8).Select(i=>new act2PartyType()).ToArray();
        private List<Vector3> act2Tower=new List<Vector3>();
        private volatile int skinsplitterCount=0;
        private System.Threading.AutoResetEvent skinsplitterSemaphore=new System.Threading.AutoResetEvent(false);
        private double exitRotation=0;
        
        private volatile int act3PartyCount=0;
        private DirectionsOfMitoticPhase[] directionOfMitoticPhase=Enumerable.Range(0,8).Select(i=>DirectionsOfMitoticPhase.UNKNOWN).ToArray();
        private System.Threading.AutoResetEvent mitoticPhaseSemaphore=new System.Threading.AutoResetEvent(false);
        private bool? isCardinal=null; // Its read-write lock is isCardinalLock.
        private System.Threading.AutoResetEvent arenaDestructionSemaphore=new System.Threading.AutoResetEvent(false);

        private volatile int act4PartyCount=0;
        private bool[] isRottingFlesh=Enumerable.Range(0,8).Select(i=>false).ToArray();
        private List<Vector3> act4FleshPile=new List<Vector3>();
        private bool areNorthwestAndSoutheast=true;

        private System.Threading.ManualResetEvent slaughtershedInitializationSemaphore=new ManualResetEvent(false);
        private bool isGlobalInitialization=false;
        private List<Vector3> slaughtershedFleshPile=new List<Vector3>();
        private bool isStackOnLeft=false;
        private System.Threading.AutoResetEvent slaughtershedBurstSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile int slaughtershedIconCount=0;
        private TargetIconsOfSlaughtershed[] targetIconOfSlaughtershed=Enumerable.Range(0,8).Select(i=>TargetIconsOfSlaughtershed.NONE).ToArray();
        private System.Threading.AutoResetEvent slaughtershedIconSemaphore1=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent slaughtershedIconSemaphore2=new System.Threading.AutoResetEvent(false);

        // ----- End Of Major Phase 1 -----
        
        // ----- Major Phase 2 -----
        
        private bool? isFrontAndBackInPhase1=null;
        private volatile int phase1LindschratCount=0; // Its read-write lock is phase1LindschratCountLock.
        
        private ulong phase2BossId=0;
        private volatile int phase2PlayerStagingCount=0;
        private int[] phase2PlayerStaging=Enumerable.Range(0,8).Select(i=>-1).ToArray();
        private volatile int phase2LindschratCount=0;
        private Phase2TetherActions[] phase2Lindschrat=Enumerable.Range(0,6).Select(i=>Phase2TetherActions.UNKNOWN).ToArray();
        private System.Threading.AutoResetEvent phase2LindschratSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile int phase2StagingActionCount=0;
        private List<Phase2TetherActions>[] phase2StagingActions=Enumerable.Range(0,8).Select(i=>new List<Phase2TetherActions>()).ToArray();
        private volatile bool phase2DisableGuidance=false;
        private System.Threading.AutoResetEvent phase2StagingActionSemaphore1=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase2StagingActionSemaphore2=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase2StagingActionSemaphore3=new System.Threading.AutoResetEvent(false);
        private volatile bool isRecordingScaldingWavesInPhase2=false;
        private List<int> phase2ScaldingWavesPlayers=new List<int>();
        private volatile bool isRecordingManaBurstInPhase2=false;

        private volatile int phase3PartyCount=0;
        private bool[] isVulnerableInPhase3=Enumerable.Range(0,8).Select(i=>true).ToArray();
        private List<manaSphereType> phase3LeftManaSphere=new List<manaSphereType>(); // Its read-write lock is phase3ManaSphereLock.
        private List<manaSphereType> phase3RightManaSphere=new List<manaSphereType>(); // Its read-write lock is phase3ManaSphereLock.
        private volatile bool isNaturalXOnLeftInPhase3=false;
        private manaSphereType phase3UpperSphereToBeDelayed=null;
        private manaSphereType phase3LowerSphereToBeDelayed=null;
        private System.Threading.AutoResetEvent phase3GuidanceSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile bool phase3DisableDrawings=false;

        private volatile int phase4PlayerStagingCount=0;
        private int[] phase4PlayerStaging=Enumerable.Range(0,8).Select(i=>-1).ToArray();
        private bool? isCardinalFirstInPhase4=null; // Its read-write lock is isCardinalFirstInPhase4Lock.
        private int phase4TwistedVisionCount=0;
        private System.Threading.AutoResetEvent phase4TwistedVision3Semaphore1=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4TwistedVision3Semaphore2=new System.Threading.AutoResetEvent(false);
        private List<KeyValuePair<ulong,string>> phase4LindschratCombo=new List<KeyValuePair<ulong,string>>();
        private volatile int phase4LindschratCount=0;
        private bool?[] isLindschratDefamationInPhase4=Enumerable.Range(0,8).Select(i=>((bool?)null)).ToArray();
        private System.Threading.AutoResetEvent phase4LindschratSemaphore1=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4LindschratSemaphore2=new System.Threading.AutoResetEvent(false);
        private volatile int phase4StagingActionCount=0;
        private bool[] isStagingDefamationInPhase4=Enumerable.Range(0,8).Select(i=>false).ToArray();
        private int[] phase4PlayerOrder=Enumerable.Range(0,8).Select(i=>-1).ToArray();
        private volatile bool phase4DisableGuidance=false;
        private System.Threading.AutoResetEvent phase4StagingActionSemaphore=new System.Threading.AutoResetEvent(false);
        private int phase4TowerCount=0;
        private Phase4Towers[] phase4Tower=Enumerable.Range(0,8).Select(i=>Phase4Towers.UNKNOWN).ToArray();
        private int phase4HitCount=0;
        private bool[] wasHitInPhase4=Enumerable.Range(0,8).Select(i=>false).ToArray();
        private bool[] swapsWithPartnerInPhase4=Enumerable.Range(0,8).Select(i=>false).ToArray();
        private System.Threading.AutoResetEvent phase4TowerPreviewSemaphore=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4TwistedVision4Semaphore1=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4TwistedVision4Semaphore2=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4TwistedVision4Semaphore3=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4TwistedVision4Semaphore4=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4TwistedVision5Semaphore1=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4TwistedVision5Semaphore2=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4TwistedVision5Semaphore3=new System.Threading.AutoResetEvent(false);
        private ulong phase4HiddenLindschrat=0;
        private System.Threading.AutoResetEvent phase4TwistedVision6Semaphore1=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4TwistedVision6Semaphore2=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4TwistedVision7Semaphore1=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4TwistedVision7Semaphore2=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4TwistedVision8Semaphore1=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4TwistedVision8Semaphore2=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase4TwistedVision8Semaphore3=new System.Threading.AutoResetEvent(false);
        
        // ----- End Of Major Phase 2 -----
        
        #endregion
        
        #region Constants_And_Locks

        private static readonly Vector3 ARENA_CENTER=new Vector3(100,0,100);
        // The arena during Major Phase 1 is a rectangle, ± 15 vertically and ± 20 horizontally.
        // All the pattern squares on it are 5×5.
        // Therefore, many positions during Major Phase 1 could be calculated without precise geometric construction.
        // The arena during Major Phase 2 is a circle with a radius of 20.
        
        // ----- Major Phase 1 -----
        
        private static readonly Vector3 LEFT_WHEN_NORMAL_PAIR=new Vector3(96,0,87);
        private static readonly Vector3 RIGHT_WHEN_NORMAL_PAIR=new Vector3(104,0,87);
        private static readonly Vector3 LEFT_WHEN_ON_LEFT_SIDE=new Vector3(90,0,87);
        private static readonly Vector3 RIGHT_WHEN_ON_LEFT_SIDE=new Vector3(98,0,87);
        private static readonly Vector3 LEFT_WHEN_ON_RIGHT_SIDE=new Vector3(102,0,87);
        private static readonly Vector3 RIGHT_WHEN_ON_RIGHT_SIDE=new Vector3(110,0,87);
        
        private readonly object isCardinalLock=new object();
        private static readonly Vector3 NORTH_SUPPORTER_WHEN_CARDINAL=new Vector3(96,0,114);
        private static readonly Vector3 SOUTH_SUPPORTER_WHEN_CARDINAL=new Vector3(96,0,94);
        private static readonly Vector3 WEST_SUPPORTER_WHEN_CARDINAL=new Vector3(111,0,96);
        private static readonly Vector3 EAST_SUPPORTER_WHEN_CARDINAL=new Vector3(81,0,96);
        private static readonly Vector3 NORTH_DPS_WHEN_CARDINAL=new Vector3(104,0,106);
        private static readonly Vector3 SOUTH_DPS_WHEN_CARDINAL=new Vector3(104,0,86);
        private static readonly Vector3 WEST_DPS_WHEN_CARDINAL=new Vector3(119,0,104);
        private static readonly Vector3 EAST_DPS_WHEN_CARDINAL=new Vector3(89,0,104);
        private static readonly Vector3 NORTH_SUPPORTER_WHEN_INTERCARDINAL=new Vector3(81,0,106);
        private static readonly Vector3 SOUTH_SUPPORTER_WHEN_INTERCARDINAL=new Vector3(111,0,86);
        private static readonly Vector3 WEST_SUPPORTER_WHEN_INTERCARDINAL=new Vector3(111,0,106);
        private static readonly Vector3 EAST_SUPPORTER_WHEN_INTERCARDINAL=new Vector3(81,0,86);
        private static readonly Vector3 NORTH_DPS_WHEN_INTERCARDINAL=new Vector3(89,0,114);
        private static readonly Vector3 SOUTH_DPS_WHEN_INTERCARDINAL=new Vector3(119,0,94);
        private static readonly Vector3 WEST_DPS_WHEN_INTERCARDINAL=new Vector3(119,0,114);
        private static readonly Vector3 EAST_DPS_WHEN_INTERCARDINAL=new Vector3(89,0,94);

        private static ImmutableList<Vector3> POSITION_ON_LEFT=[new Vector3(94,0,86),new Vector3(91,0,94),new Vector3(91,0,106),new Vector3(94,0,114)];
        private static ImmutableList<Vector3> POSITION_ON_RIGHT=[new Vector3(106,0,86),new Vector3(109,0,94),new Vector3(109,0,106),new Vector3(106,0,114)];
        private static ImmutableList<Vector3> SUPPORTER_POSITION=[new Vector3(81,0,86),new Vector3(81,0,95),new Vector3(81,0,105),new Vector3(81,0,114)];
        private static ImmutableList<Vector3> DPS_POSITION=[new Vector3(119,0,86),new Vector3(119,0,95),new Vector3(119,0,105),new Vector3(119,0,114)];

        private static readonly Vector3 STACK_ON_LEFT=new Vector3(81,0,86);
        private static readonly Vector3 STACK_ON_RIGHT=new Vector3(119,0,86);
        private static ImmutableList<Vector3> SPREAD_ON_LEFT=[new Vector3(89.267f,0,86),new Vector3(87.836f,0,92.969f),new Vector3(81,0,86),new Vector3(81,0,94.120f)];
        private static ImmutableList<Vector3> SPREAD_ON_RIGHT=[new Vector3(111.477f,0,85.5f),new Vector3(112.944f,0,94.963f),new Vector3(119.5f,0,85.5f),new Vector3(119.5f,0,93.700f)];
        // The two cases are not mirror images! The links to the related geometric constructions:
        // Spread on left: https://www.geogebra.org/calculator/pgc9s43t
        // Spread on right: https://www.geogebra.org/calculator/wkyqdy4y
        // All the geometric constructions in the comments are in Simplified Chinese, since I completed the Simplified Chinese version of the script first, unlike M8S.
        
        private static readonly Vector3 SLAUGHTERSHED_LEFT_ARENA_CENTER=new Vector3(90,0,100);
        private static readonly Vector3 SLAUGHTERSHED_RIGHT_ARENA_CENTER=new Vector3(110,0,100);
        private static readonly Vector3 SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER=new Vector3(82,0,89);
        private static readonly Vector3 SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER=new Vector3(118,0,89);
        
        // ----- End Of Major Phase 1 -----
        
        // ----- Major Phase 2 -----
        
        private readonly object phase1LindschratCountLock=new object();

        private static ImmutableList<Vector3> PHASE2_TETHER_POSITION=[new Vector3(100,0,87),new Vector3(102.296f,0,81.457f),new Vector3(105.543f,0,84.704f),new Vector3(117.502f,0,107.395f),
                                                                      new Vector3(100,0,119),new Vector3(82.498f,0,107.395f),new Vector3(94.457f,0,84.704f),new Vector3(97.704f,0,81.457f)];
        private static readonly Vector3 PHASE2_STAGING2_STACK_POSITION=new Vector3(104.243f,0,91.243f);
        private static readonly Vector3 PHASE2_STAGING6_STACK_POSITION=new Vector3(95.757f,0,91.243f);
        // The link to the related geometric constructions:
        // https://www.geogebra.org/calculator/xpke2dmn
        private static ImmutableList<Vector3> PHASE2_REENACTMENT_POSITION=[new Vector3(103.889f,0,89.889f),new Vector3(103,0,80.804f),new Vector3(104.760f,0,82.347f),new Vector3(105.657f,0,91.657f),
                                                                           new Vector3(96.111f,0,89.889f),new Vector3(94.343f,0,91.657f),new Vector3(95.240f,0,82.347f),new Vector3(97,0,80.804f)];
        // The link to the related geometric constructions:
        // https://www.geogebra.org/calculator/zx3rpyxa
        
        private readonly object phase3ManaSphereLock=new object();
        
        private readonly object isCardinalFirstInPhase4Lock=new object();
        private static readonly Vector3 PHASE4_LEFT_ARENA_CENTER=new Vector3(86,0,100);
        private static readonly Vector3 PHASE4_RIGHT_ARENA_CENTER=new Vector3(114,0,100); 
        // The sub-arena radius is 10.
        private static ImmutableList<Vector3> PHASE4_DEFAULT_TOWER=[new Vector3(90.24f,0,95.76f),new Vector3(109.76f,0,104.24f),new Vector3(81.76f,0,95.76f),new Vector3(118.24f,0,104.24f),
                                                                    new Vector3(90.24f,0,104.24f),new Vector3(109.76f,0,95.76f),new Vector3(81.76f,0,104.24f),new Vector3(118.24f,0,95.76f)];
        private static ImmutableList<Vector3> PHASE4_STANDBY_POSITION=[new Vector3(92.364f,0,93.636f),new Vector3(107.636f,0,106.364f),new Vector3(79.636f,0,93.636f),new Vector3(120.364f,0,106.364f),
                                                                       new Vector3(92.364f,0,106.364f),new Vector3(107.636f,0,93.636f),new Vector3(79.636f,0,106.364f),new Vector3(120.364f,0,93.636f)];
        // The link to the related geometric constructions:
        // https://www.geogebra.org/calculator/p7wjjkvf
        // The distance from a tower to its arena center is 6.
        private static readonly Vector3 PHASE4_C3D4_STACK_POSITION=new Vector3(90.101f,0,90.101f);
        private static readonly Vector3 PHASE4_A1B2_STACK_POSITION=new Vector3(109.899f,0,90.101f);
        private static readonly Vector3 PHASE4_C3D4_DEFAMATION_POSITION=new Vector3(89.102f,0,115.564f);
        private static readonly Vector3 PHASE4_A1B2_DEFAMATION_POSITION=new Vector3(110.898f,0,115.564f);
        // The link to the related geometric constructions:
        // https://www.geogebra.org/calculator/dfcenfhu
        private static readonly Vector3 PHASE4_LEFT_WIND_POSITION=new Vector3(106.206f,0,95.5f);
        private static readonly Vector3 PHASE4_RIGHT_WIND_POSITION=new Vector3(93.794f,0,104.5f);
        private static readonly Vector3 PHASE4_LEFT_DARK_POSITION=new Vector3(95,0,100);
        private static readonly Vector3 PHASE4_RIGHT_DARK_POSITION=new Vector3(105,0,100);
        private static readonly Vector3 PHASE4_LEFT_MELEE_POSITION=new Vector3(93.5f,0,100);
        private static readonly Vector3 PHASE4_RIGHT_MELEE_POSITION=new Vector3(106.5f,0,100);
        private static readonly Vector3 PHASE4_LEFT_RANGED_POSITION=new Vector3(86,0,91);
        private static readonly Vector3 PHASE4_RIGHT_RANGED_POSITION=new Vector3(114,0,109);
        // The link to the related geometric constructions:
        // https://www.geogebra.org/calculator/ywabxmmk
        private static readonly Vector3 PHASE4_LEFT_GROUP_STACK_WHEN_CARDINAL=new Vector3(101.768f,0,84.232f);
        private static readonly Vector3 PHASE4_RIGHT_GROUP_STACK_WHEN_CARDINAL=new Vector3(115.768f,0,98.232f);
        // The link to the related geometric constructions:
        // https://www.geogebra.org/calculator/fnr5yuys
        private static readonly Vector3 PHASE4_LEFT_GROUP_STACK_WHEN_INTERCARDINAL=new Vector3(87.601f,0,90.101f);
        private static readonly Vector3 PHASE4_RIGHT_GROUP_STACK_WHEN_INTERCARDINAL=new Vector3(87.601f,0,109.899f);
        // The link to the related geometric constructions:
        // https://www.geogebra.org/calculator/xtvf4h44
        
        // ----- End Of Major Phase 2 -----
        
        #endregion
        
        #region Enumerations_And_Classes

        /*
        
        public enum OrdersDuringMortalSlayer {
            
            近战_远程_治疗

        }
        
        */

        public class sphereType {
            
            public float x=100;
            public bool isGreen=true;

            public sphereType() {

                this.x=100;
                this.isGreen=true;

            }

            public sphereType(sphereType original) {
                
                this.x=original.x;
                this.isGreen=original.isGreen;
                
            }

        }

        public class act2PartyType {

            public bool isAlpha=true;
            public int rawOrder=0;

            public act2PartyType() {

                this.isAlpha=true;
                this.rawOrder=0;

            }
            
            public act2PartyType(act2PartyType original) {
                
                this.isAlpha=original.isAlpha;
                this.rawOrder=original.rawOrder;
                
            }

        }

        public enum DirectionsOfMitoticPhase {
            
            NORTH,
            SOUTH,
            WEST,
            EAST,
            UNKNOWN
            
        }

        public enum TargetIconsOfSlaughtershed {
            
            NONE,
            STACK,
            SPREAD
            
        }
        
        public enum Phase2TetherActions {
            
            FAN,
            STACK,
            DEFAMATION,
            BOSS_COMBO,
            UNKNOWN
            
        }

        public class manaSphereType {
            
            public ulong objectId=0;
            public Vector3 position=ARENA_CENTER;
            public string dataId=string.Empty;

            public manaSphereType() {

                this.objectId=0;
                this.position=ARENA_CENTER;
                this.dataId=string.Empty;

            }

            public manaSphereType(manaSphereType original) {

                this.objectId=original.objectId;
                this.position=original.position;
                this.dataId=original.dataId;

            }

        }
        
        public enum Phase4Towers {
            
            EARTH,
            FIRE,
            WIND_TRILOGY,
            DARKNESS_TRILOGY,
            UNKNOWN
            
        }
        
        #endregion
        
        #region Initialization

        public void Init(ScriptAccessory accessory) {
            
            accessory.Method.RemoveDraw(".*");
            
            if(enablePartyAssistance) {

                accessory.Method.MarkClear();
                
            }
            
            if(startFromMajorPhase2) {

                isInMajorPhase1=false;

            }

            else {

                isInMajorPhase1=true;

            }
            
            initializedMajorPhase1=false;
            initializedMajorPhase2=false;
            
            VariableAndSemaphoreInitialization();
            
            targetIconBaseId=null;

        }

        private void VariableAndSemaphoreInitialization() {
            
            currentPhase=0;
            
            // ----- Major Phase 1 -----
            
            sphere.Clear();
            leftOrder.Clear();
            rightOrder.Clear();
            mortalSlayerSphereSemaphore.Reset();
            mortalSlayerRangeSemaphore.Reset();
            mortalSlayerGuidanceSemaphore.Reset();
            
            act2PartyCount=0;
            for(int i=0;i<act2Party.Length;++i)act2Party[i]=new act2PartyType();
            act2Tower.Clear();
            skinsplitterCount=0;
            skinsplitterSemaphore.Reset();
            exitRotation=0;

            act3PartyCount=0;
            for(int i=0;i<directionOfMitoticPhase.Length;++i)directionOfMitoticPhase[i]=DirectionsOfMitoticPhase.UNKNOWN;
            mitoticPhaseSemaphore.Reset();
            isCardinal=null;
            arenaDestructionSemaphore.Reset();
            
            act4PartyCount=0;
            for(int i=0;i<isRottingFlesh.Length;++i)isRottingFlesh[i]=false;
            act4FleshPile.Clear();
            areNorthwestAndSoutheast=true;

            isGlobalInitialization=true;
            slaughtershedInitializationSemaphore.Set();
            System.Threading.Tasks.Task.Delay(1000).ContinueWith(_=> {
                slaughtershedInitializationSemaphore.Reset();
                isGlobalInitialization=false;
            });
            slaughtershedFleshPile.Clear();
            isStackOnLeft=false;
            slaughtershedBurstSemaphore.Reset();
            slaughtershedIconCount=0;
            for(int i=0;i<targetIconOfSlaughtershed.Length;++i)targetIconOfSlaughtershed[i]=TargetIconsOfSlaughtershed.NONE;
            slaughtershedIconSemaphore1.Reset();
            slaughtershedIconSemaphore2.Reset();

            // ----- End Of Major Phase 1 -----
            
            // ----- Major Phase 2 -----

            isFrontAndBackInPhase1=null;
            phase1LindschratCount=0;
            
            phase2BossId=0;
            phase2PlayerStagingCount=0;
            for(int i=0;i<phase2PlayerStaging.Length;++i)phase2PlayerStaging[i]=-1;
            phase2LindschratCount=0;
            for(int i=0;i<phase2Lindschrat.Length;++i)phase2Lindschrat[i]=Phase2TetherActions.UNKNOWN;
            phase2LindschratSemaphore.Reset();
            phase2StagingActionCount=0;
            for(int i=0;i<phase2StagingActions.Length;++i)phase2StagingActions[i].Clear();
            phase2DisableGuidance=false;
            phase2StagingActionSemaphore1.Reset();
            phase2StagingActionSemaphore2.Reset();
            phase2StagingActionSemaphore3.Reset();
            isRecordingScaldingWavesInPhase2=false;
            phase2ScaldingWavesPlayers.Clear();
            isRecordingManaBurstInPhase2=false;
            
            phase3PartyCount=0;
            for(int i=0;i<isVulnerableInPhase3.Length;++i)isVulnerableInPhase3[i]=true;
            phase3LeftManaSphere.Clear();
            phase3RightManaSphere.Clear();
            isNaturalXOnLeftInPhase3=false;
            phase3UpperSphereToBeDelayed=null;
            phase3LowerSphereToBeDelayed=null;
            phase3GuidanceSemaphore.Reset();
            phase3DisableDrawings=false;
            
            phase4PlayerStagingCount=0;
            for(int i=0;i<phase4PlayerStaging.Length;++i)phase4PlayerStaging[i]=-1;
            isCardinalFirstInPhase4=null;
            phase4TwistedVisionCount=0;
            phase4TwistedVision3Semaphore1.Reset();
            phase4TwistedVision3Semaphore2.Reset();
            phase4LindschratCombo.Clear();
            phase4LindschratCount=0;
            for(int i=0;i<isLindschratDefamationInPhase4.Length;++i)isLindschratDefamationInPhase4[i]=null;
            phase4LindschratSemaphore1.Reset();
            phase4LindschratSemaphore2.Reset();
            phase4StagingActionCount=0;
            for(int i=0;i<isStagingDefamationInPhase4.Length;++i)isStagingDefamationInPhase4[i]=false;
            for(int i=0;i<phase4PlayerOrder.Length;++i)phase4PlayerOrder[i]=-1;
            phase4DisableGuidance=false;
            phase4StagingActionSemaphore.Reset();
            phase4TowerCount=0;
            for(int i=0;i<phase4Tower.Length;++i)phase4Tower[i]=Phase4Towers.UNKNOWN;
            phase4HitCount=0;
            for(int i=0;i<wasHitInPhase4.Length;++i)wasHitInPhase4[i]=false;
            for(int i=0;i<swapsWithPartnerInPhase4.Length;++i)swapsWithPartnerInPhase4[i]=false;
            phase4TowerPreviewSemaphore.Reset();
            phase4TwistedVision4Semaphore1.Reset();
            phase4TwistedVision4Semaphore2.Reset();
            phase4TwistedVision4Semaphore3.Reset();
            phase4TwistedVision4Semaphore4.Reset();
            phase4TwistedVision5Semaphore1.Reset();
            phase4TwistedVision5Semaphore2.Reset();
            phase4TwistedVision5Semaphore3.Reset();
            phase4HiddenLindschrat=0;
            phase4TwistedVision6Semaphore1.Reset();
            phase4TwistedVision6Semaphore2.Reset();
            phase4TwistedVision7Semaphore1.Reset();
            phase4TwistedVision7Semaphore2.Reset();
            phase4TwistedVision8Semaphore1.Reset();
            phase4TwistedVision8Semaphore2.Reset();
            phase4TwistedVision8Semaphore3.Reset();

            // ----- End Of Major Phase 2 -----

        }

        #endregion
        
        #region Shenanigans
        
        private System.Threading.AutoResetEvent shenaniganSemaphore=new System.Threading.AutoResetEvent(false);
        private static ImmutableList<string> quotes=[
            "问候约旦河的河畔,以及锡安倾倒的高塔...",
            "坟冢之上,风呼啸而过。",
            "奴隶不是铺就你道路的砖石,他们也不是你救赎历史中的章节。",
            "主啊,你造我们是为了你,我们的心如不安息在你怀中,便不会安宁。",
            "不!我还活着!我将永远活着!我心中有些东西是永远不会死去的!",
            "生者不当座上宾,死者却做棺中人。",
            "凡持剑的,必死在剑下。",
            "任何地方的不公不义,都威胁着所有的公平正义。",
            "我至死也未能见到照耀我祖国的曙光。",
            "我生在了一个善良的世界,全心全意地爱着它。我死在了一个邪恶的世界,离别时刻无话可说。",
            "你不能用痛苦养育一个人,也无法用怒火来让他饱腹。",
            "\"我们已经通过了!\"",
            "野牛惨遭屠戮,村民享其残躯。",
            "引火上身的人早晚会意识到灼痛的感觉会与暖身的温度伴随而至。",
            "流沙之上,大厦将倾。",
            "忠诚笃实的人,将满渥福祉。",
            "她凄然地笑着,遁入了无尽的夜空。",
            "目的为手段赋予了正义,但总得有什么为目的赋予正义。",
            "信士接连倒下,蒙昧之祸四散蔓延。",
            "为了保卫沙中之砾,他们流干了最后一滴血。",
            "历史就是人类努力回想起理想的过程。\n——埃蒙·德·瓦莱拉, 1929",
            "让我们致力于希腊人在很多很多年前就曾写下的内容: 驯服人的野蛮并创造这个世界的温雅生活。\n——罗伯特·F·肯尼迪, 1968",
            "昨日的失误已无法弥补,但明天的输赢仍可以拼搏。\n——林登·B·约翰逊, 1964",
            "希望之末,败亡之始。\n——夏尔·戴高乐, 1945",
            "我挂冠回乡之时,惟余两袖清风。\n——安东尼奥·德·奥利韦拉·萨拉查, 1968",
            "拆毁纪念像时,得留下底座。它们将来总会派上用处。\n——斯坦尼斯瓦夫·耶日·莱茨, 1957",
            "不要害怕真理之路上无人同行。\n——罗伯特·F·肯尼迪, 1968",
            "这火箭什么都好,就是目的地选错了星球。\n——韦恩赫尔·冯·布劳恩在V-2火箭首次打击伦敦后, 1944",
            "不要祈祷更安逸的生活,我的朋友,祈祷自己成为更坚强的人。\n——约翰·F·肯尼迪, 1963",
            "乐观主义者认为这个世界是所有可能中最好的,而悲观主义者担心这就是真的。\n——詹姆斯·布朗奇·卡贝尔, 《银马》, 1926",
            "当魔鬼和你勾肩搭背时,你很难认出他来。\n——阿尔伯特·施佩尔, 1972",
            "他们对你的要求不多: 只是要你去恨你所爱的,去爱你所厌的。\n——鲍里斯·帕斯捷尔纳克, 1960",
            "大部分经济学上的谬误都源自于\"定量馅饼\"的前提假设,以为一方得益,另一方就必有所失。\n——米尔顿·弗里德曼, 1980",
            "世界上有三种谎言: 谎言,糟糕透顶的谎言和统计数据。\n——马克·吐温, 1907",
            "咬我一次,可耻在狗;咬我多次,纵容它的我才可耻。\n——菲莉丝·施拉夫利, 1995",
            "风水这个东西,你要信也可以,但是我更相信事在人为。\n——李嘉诚, 1969",
            "建立个人和企业的良好信誉,这是资产负债表之中见不到,但却是价值无限的资产。\n——李嘉诚, 1967",
            "财富聚散无常,唯学问终生受用。\n——何鸿燊, 1966",
            "人们常言道:\"我们生活在一个腐败,虚伪的社会当中。\"这也不尽然。心慈好善的人仍占多数。\n——若望·保禄一世, 1978",
            "这世界上半数的困惑,都来源于我们不知道自己的需求是多么微不足道。\n——理查德·E·伯德海军上将, 于南极洲, 1935"
        ];

        [ScriptMethod(name:"Shenanigans",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:regex:^(19195|19202)$"],
            suppress:13000,
            userControl:false)]

        public void Shenanigans(Event @event,ScriptAccessory accessory) {
            
            if(!enableShenanigans) {

                return;

            }

            shenaniganSemaphore.WaitOne();

            System.Threading.Thread.Sleep(3000);
            
            string prompt=quotes[new System.Random().Next(0,quotes.Count)];

            if(!string.IsNullOrWhiteSpace(prompt)) {

                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,10000);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }

        }

        #endregion
        
        #region Major_Phase_1

        [ScriptMethod(name:"门神 致命灾变 (初始化与阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46229"],
            userControl:false)]
    
        public void 门神_致命灾变_初始化与阶段控制(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }
            
            sphere.Clear();
            leftOrder.Clear();
            rightOrder.Clear();
            mortalSlayerSphereSemaphore.Reset();
            mortalSlayerRangeSemaphore.Reset();
            mortalSlayerGuidanceSemaphore.Reset();

            Interlocked.Increment(ref currentPhase);

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"isInMajorPhase1={isInMajorPhase1}\ncurrentPhase={currentPhase}");
                
            }
        
        }
        
        [ScriptMethod(name:"门神 致命灾变 (数据收集)",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:regex:^(19201|19200)$"],
            userControl:false)]
    
        public void 门神_致命灾变_数据收集(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=1&&currentPhase!=6&&!skipPhaseChecks) {

                return;

            }

            lock(sphere) {
                
                Vector3 sourcePosition=ARENA_CENTER;

                try {

                    sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

                } catch(Exception e) {
                
                    accessory.Log.Error("SourcePosition deserialization failed.");

                    return;

                }
                
                sphereType sphereData=new sphereType();
                
                sphereData.x=sourcePosition.X;

                if(string.Equals(@event["DataId"],"19201")) {

                    sphereData.isGreen=true;

                }

                if(string.Equals(@event["DataId"],"19200")) {

                    sphereData.isGreen=false;

                }
                
                sphere.Add(sphereData);
                
                if(sphere.Count%2==0&&sphere.Count>=2) {
                    
                    int currentRound=sphere.Count/2-1;
                    
                    if(sphere[sphere.Count-1].x<sphere[sphere.Count-2].x) {
                        
                        (sphere[sphere.Count-1],sphere[sphere.Count-2])=(sphere[sphere.Count-2],sphere[sphere.Count-1]);
                        
                    }

                    mortalSlayerSphereSemaphore.Set();

                }
                
                System.Threading.Thread.MemoryBarrier();
                
                if(sphere.Count==8) {

                    // A placeholder for calculation code.

                    mortalSlayerRangeSemaphore.Set();
                    mortalSlayerGuidanceSemaphore.Set();

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"""
                                            sphere.x:{string.Join(",",sphere.Select(s=>s.x))}
                                            sphere.isGreen:{string.Join(",",sphere.Select(s=>s.isGreen))}
                                            leftGroupOrder:{string.Join(",",leftOrder)}
                                            rightGroupOrder:{string.Join(",",rightOrder)}
                                            """);
                        
                    }

                }
                
            }
        
        }
        
        [ScriptMethod(name:"门神 致命灾变 (场地分割线)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46229"])]
    
        public void 门神_致命灾变_场地分割线(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }
            
            System.Threading.Thread.Sleep(1375);
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Name="门神_致命灾变_场地分割线";
            currentProperties.Scale=new(0.25f,30);
            currentProperties.Position=ARENA_CENTER;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=22125;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Straight,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 致命灾变 (球体对指示)",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:regex:^(19201|19200)$"],
            suppress:1000)]
    
        public void 门神_致命灾变_球体对指示(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=1&&currentPhase!=6&&!skipPhaseChecks) {

                return;

            }
            
            mortalSlayerSphereSemaphore.WaitOne();

            int currentSphereCount=sphere.Count;

            if(!(currentSphereCount%2==0&&currentSphereCount>=2)) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            int leftSphere=currentSphereCount-2,rightSphere=currentSphereCount-1,currentRound=currentSphereCount/2-1;
            float leftX=100-1.5f,rightX=100+1.5f;

            if(sphere[leftSphere].x<100&&sphere[rightSphere].x<100) {

                leftX-=3;
                rightX-=3;

            }
            
            if(sphere[leftSphere].x>100&&sphere[rightSphere].x>100) {

                leftX+=3;
                rightX+=3;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(1);
            currentProperties.Position=new Vector3(leftX,0,88+2.5f*currentRound);
            currentProperties.Color=((sphere[leftSphere].isGreen)?(colourOfGreenSpheres.V4.WithW(1)):(colourOfPurpleSpheres.V4.WithW(1)));
            currentProperties.DestoryAt=10750+625*currentRound;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(1);
            currentProperties.Position=new Vector3(rightX,0,88+2.5f*currentRound);
            currentProperties.Color=((sphere[rightSphere].isGreen)?(colourOfGreenSpheres.V4.WithW(1)):(colourOfPurpleSpheres.V4.WithW(1)));
            currentProperties.DestoryAt=10750+625*currentRound;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 致命灾变 (范围) !!!未完工,不生效!!!",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46229"])]
    
        public void 门神_致命灾变_范围(Event @event,ScriptAccessory accessory) {

            return; // To be removed in the future.

            if(!isInMajorPhase1) {

                return;

            }
            
            mortalSlayerRangeSemaphore.WaitOne();
            
            if(currentPhase!=1&&currentPhase!=6&&!skipPhaseChecks) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(int i=0;i<4;++i) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(8);
                currentProperties.TargetObject=accessory.Data.PartyList[leftOrder[i]];
                currentProperties.Color=((isTank(leftOrder[i]))?(colourOfExtremelyDangerousAttacks.V4.WithW(1)):(accessory.Data.DefaultDangerColor));
                currentProperties.Delay=((i==0)?(0):(500+3000*i));
                currentProperties.DestoryAt=((i==0)?(3500):(3000));
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(8);
                currentProperties.TargetObject=accessory.Data.PartyList[rightOrder[i]];
                currentProperties.Color=((isTank(rightOrder[i]))?(colourOfExtremelyDangerousAttacks.V4.WithW(1)):(accessory.Data.DefaultDangerColor));
                currentProperties.Delay=((i==0)?(0):(500+3000*i));
                currentProperties.DestoryAt=((i==0)?(3500):(3000));
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"门神 致命灾变 (指路) !!!未完工,不生效!!!",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46229"])]
    
        public void 门神_致命灾变_指路(Event @event,ScriptAccessory accessory) {

            return; // To be removed in the future.

            if(!isInMajorPhase1) {

                return;

            }

            mortalSlayerGuidanceSemaphore.WaitOne();
            
            if(currentPhase!=1&&currentPhase!=6&&!skipPhaseChecks) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            int myRound=-1;
            
            // A placeholder for calculation code.

            if(myRound<0||myRound>3) {

                return;

            }

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"myIndex={myIndex}\nmyOrder={myRound}");
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            // ----- Standby Before -----

            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(40,16);
            currentProperties.Position=new Vector3(100,0,115);
            currentProperties.TargetPosition=ARENA_CENTER;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=((myRound==0)?(0):(500+3000*myRound));
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
            
            // ----- Bait -----

            Vector3 myPosition=ARENA_CENTER;
            
            // A placeholder for calculation code.
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=((myRound==0)?(0):(500+3000*myRound));
            currentProperties.DestoryAt=3000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            // ----- Standby After -----

            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(40,16);
            currentProperties.Position=new Vector3(100,0,115);
            currentProperties.TargetPosition=ARENA_CENTER;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=3500+3000*myRound;
            currentProperties.DestoryAt=(3500+3000*3)-(3500+3000*myRound);
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
            
        }
        
        [ScriptMethod(name:"门神 细胞附身·早期 (初始化与阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48829"],
            userControl:false)]
    
        public void 门神_细胞附身_早期_初始化与阶段控制(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }

            Interlocked.Increment(ref currentPhase);

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"isInMajorPhase1={isInMajorPhase1}\ncurrentPhase={currentPhase}");
                
            }
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·早期 引爆细胞 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:4761"])]
    
        public void 门神_细胞附身_早期_引爆细胞_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            durationMilliseconds-=((int)(grotesquerieStatusDelay*1000));

            if(durationMilliseconds<=0||durationMilliseconds>=7200000) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6);
            currentProperties.Owner=targetId;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=((int)(grotesquerieStatusDelay*1000));
            currentProperties.DestoryAt=durationMilliseconds;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·早期 引爆细胞:大爆炸 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:4762"])]
    
        public void 门神_细胞附身_早期_引爆细胞_大爆炸_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }
            
            durationMilliseconds-=((int)(grotesquerieStatusDelay*1000));

            if(durationMilliseconds<=0||durationMilliseconds>=7200000) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6);
            currentProperties.Owner=targetId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=((int)(grotesquerieStatusDelay*1000));
            currentProperties.DestoryAt=durationMilliseconds;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·早期 引爆细胞:指向 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:3558"])]
    
        public void 门神_细胞附身_早期_引爆细胞_指向_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            if(onlyMyDirectedGrotesquerie) {

                if(targetId!=accessory.Data.Me) {

                    return;

                }
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"门神_细胞附身_早期_引爆细胞_指向_范围_{targetId}";
            currentProperties.Scale=new(60);
            currentProperties.Owner=targetId;
            currentProperties.Radian=float.Pi/6;
            currentProperties.Delay=((int)(grotesquerieStatusDelay*1000));
            currentProperties.DestoryAt=7200000;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            
            if(string.Equals(@event["Param"],"1036")) {

                currentProperties.Rotation=0;

            }
            
            if(string.Equals(@event["Param"],"1037")) {

                currentProperties.Rotation=float.Pi/2*3;

            }
            
            if(string.Equals(@event["Param"],"1038")) {

                currentProperties.Rotation=float.Pi;

            }
            
            if(string.Equals(@event["Param"],"1039")) {

                currentProperties.Rotation=float.Pi/2;

            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·早期 引爆细胞:指向 (清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:3558"],
            userControl:false)]
    
        public void 门神_细胞附身_早期_引爆细胞_指向_清除(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                accessory.Method.RemoveDraw("^门神_细胞附身_早期_引爆细胞_指向_范围_.*");
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"门神_细胞附身_早期_引爆细胞_指向_范围_{targetId}");
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·早期 极饿伸展 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46237"])]
    
        public void 门神_细胞附身_早期_极饿伸展_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(35);
            currentProperties.Owner=sourceId;
            currentProperties.Radian=float.Pi/3*2;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=4875;
            currentProperties.DestoryAt=5750;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(35);
            currentProperties.Owner=sourceId;
            currentProperties.Radian=float.Pi*2-float.Pi/3*2;
            currentProperties.Rotation=float.Pi;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=4875;
            currentProperties.DestoryAt=5750;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·早期 大爆炸 (范围)",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:2015017"])]
    
        public void 门神_细胞附身_早期_大爆炸_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(!string.Equals(@event["Operate"],"Add")) {

                return;

            }

            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(12);
            currentProperties.Position=sourcePosition;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=13750;
            currentProperties.DestoryAt=6625;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·早期 脏腑爆裂 (范围)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:0158"])]
    
        public void 门神_细胞附身_早期_脏腑爆裂_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6);
            currentProperties.Owner=accessory.Data.PartyList[targetIndex];
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=5125;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"门神 细胞附身·早期 细胞轰炸 (范围)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:00A1"])]
    
        public void 门神_细胞附身_早期_细胞轰炸_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));

            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            if(!isLegalPartyIndex(myIndex)) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6);
            currentProperties.Owner=accessory.Data.PartyList[targetIndex];
            currentProperties.Color=((isTank(myIndex))?(accessory.Data.DefaultDangerColor):(accessory.Data.DefaultSafeColor));
            currentProperties.DestoryAt=5125;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"门神 细胞附身·中期 (初始化与阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48830"],
            userControl:false)]
    
        public void 门神_细胞附身_中期_初始化与阶段控制(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }
            
            act2PartyCount=0;
            for(int i=0;i<act2Party.Length;++i)act2Party[i]=new act2PartyType();
            act2Tower.Clear();
            skinsplitterCount=0;
            skinsplitterSemaphore.Reset();
            exitRotation=0;

            Interlocked.Increment(ref currentPhase);

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"isInMajorPhase1={isInMajorPhase1}\ncurrentPhase={currentPhase}");
                
            }
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·中期 (数据收集)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:regex:^(4752|4754)$"],
            userControl:false)]
    
        public void 门神_细胞附身_中期_数据收集(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }

            if(act2PartyCount>=8) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));

            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>=7200000) {

                return;

            }

            lock(act2Party) {

                if(string.Equals(@event["StatusID"],"4752")) {

                    act2Party[targetIndex].isAlpha=true;

                }
                
                if(string.Equals(@event["StatusID"],"4754")) {

                    act2Party[targetIndex].isAlpha=false;

                }

                if(0<durationMilliseconds&&durationMilliseconds<=27000) {

                    act2Party[targetIndex].rawOrder=1;

                }

                else {
                    
                    if(27000<durationMilliseconds&&durationMilliseconds<=32000) {

                        act2Party[targetIndex].rawOrder=2;

                    }

                    else {
                        
                        if(32000<durationMilliseconds&&durationMilliseconds<=37000) {

                            act2Party[targetIndex].rawOrder=3;

                        }

                        else {
                            
                            if(37000<durationMilliseconds&&durationMilliseconds<=42000) {

                                act2Party[targetIndex].rawOrder=4;

                            }

                            else {

                                act2Party[targetIndex].rawOrder=0;

                            }
                    
                        }
                    
                    }
                    
                }

                if(act2Party[targetIndex].rawOrder<1||act2Party[targetIndex].rawOrder>4) {

                    return;

                }
                
                Interlocked.Increment(ref act2PartyCount);

                if(act2PartyCount==8) {

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"""
                                             act2Party.isAlpha:{string.Join(",",act2Party.Select(a=>a.isAlpha))}
                                             act2Party.rawOrder:{string.Join(",",act2Party.Select(a=>a.rawOrder))}
                                             """);
                        
                    }

                }
                
            }
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·中期 塔 (数据收集)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:19199"],
            userControl:false)]
    
        public void 门神_细胞附身_中期_塔_数据收集(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }

            if(act2Tower.Count>=8) {

                return;

            }

            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            lock(act2Tower) {
                
                act2Tower.Add(sourcePosition);

                if(enableDebugLogging) {
                    
                    accessory.Log.Debug($"act2Tower.Count={act2Tower.Count}\nact2Tower.Last()={act2Tower.Last()}");
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·中期 (轮次控制)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:46268"],
            suppress:4000,
            userControl:false)]
    
        public void 门神_细胞附身_中期_轮次控制(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }

            if(skinsplitterCount>=7) {

                return;

            }
            
            Interlocked.Increment(ref skinsplitterCount);

            if(skinsplitterCount==1) {
                
                if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                    return;
                
                }
                
                System.Threading.Thread.Sleep(2000);
                
                var bossObject=accessory.Data.Objects.SearchById(sourceId);

                if(bossObject!=null) {

                    exitRotation=convertPolarToCartesian(bossObject.Rotation)+8*Math.PI;
                    
                }
                
            }

            else {

                exitRotation-=Math.PI/2;

            }
            
            skinsplitterSemaphore.Set();
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug($"skinsplitterCount={skinsplitterCount}\nexitRotation={exitRotation}");
                
            }

        }
        
        [ScriptMethod(name:"门神 细胞附身·中期 连环有害细胞 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:regex:^(4753|4755)$"])]
    
        public void 门神_细胞附身_中期_连环有害细胞_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>=7200000) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"门神_细胞附身_中期_连环有害细胞_范围_{targetId}";
            currentProperties.Scale=new(4);
            currentProperties.Owner=targetId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=durationMilliseconds;

            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·中期 连环有害细胞 (清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:regex:^(4753|4755)$"],
            userControl:false)]
    
        public void 门神_细胞附身_中期_连环有害细胞_清除(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                accessory.Method.RemoveDraw("^门神_细胞附身_中期_连环有害细胞_范围_.*");
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"门神_细胞附身_中期_连环有害细胞_范围_{targetId}");
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·中期 出口 (指路)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:regex:^(46264|46265|46266|46267)$"])]
    
        public void 门神_细胞附身_中期_出口_指路(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }
            
            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="门神_细胞附身_中期_出口_指路";
            currentProperties.Scale=new(2,12);
            currentProperties.Owner=sourceId;
            currentProperties.Offset=new Vector3(0,0,-8);
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.DestoryAt=42000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·中期 (指路)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:46268"])]
    
        public void 门神_细胞附身_中期_指路(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }

            skinsplitterSemaphore.WaitOne();
            
            if(skinsplitterCount<1||skinsplitterCount>7) {

                return;

            }

            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            act2PartyType myStatus=act2Party[myIndex];

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"myIndex={myIndex}\nmyStatus.isAlpha={myStatus.isAlpha}\nmyStatus.rawOrder={myStatus.rawOrder}");
                
            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(skinsplitterCount==1) {

                return;

            }

            if(skinsplitterCount==2) {

                if(myStatus.isAlpha&&myStatus.rawOrder==1) {
                    
                    accessory.Method.RemoveDraw($"门神_细胞附身_中期_出口_指路");
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2,20);
                    currentProperties.Owner=sourceId;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=1000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2,20);
                    currentProperties.Owner=sourceId;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.Delay=1000;
                    currentProperties.DestoryAt=4000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                }
                
                if(myStatus.isAlpha&&myStatus.rawOrder==3) {

                    if(act2Tower.Count<1) {

                        return;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=act2Tower[0];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(3);
                    currentProperties.Position=act2Tower[0];
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }

                if(!myStatus.isAlpha&&myStatus.rawOrder==1) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,92),ARENA_CENTER,exitRotation+Math.PI);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=1000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,92),ARENA_CENTER,exitRotation+Math.PI);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.Delay=1000;
                    currentProperties.DestoryAt=3000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,92),ARENA_CENTER,exitRotation+Math.PI);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=4000;
                    currentProperties.DestoryAt=1000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                }
                
            }
            
            if(skinsplitterCount==3) {
                
                if(myStatus.isAlpha&&myStatus.rawOrder==1) {

                    if(act2Tower.Count<3) {

                        return;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=act2Tower[2];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(3);
                    currentProperties.Position=act2Tower[2];
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }

                if(myStatus.isAlpha&&myStatus.rawOrder==2) {
                    
                    accessory.Method.RemoveDraw($"门神_细胞附身_中期_出口_指路");
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2,20);
                    currentProperties.Owner=sourceId;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=1000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2,20);
                    currentProperties.Owner=sourceId;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.Delay=1000;
                    currentProperties.DestoryAt=4000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                }
                
                if(myStatus.isAlpha&&myStatus.rawOrder==3) {
                    
                    if(act2Tower.Count<1) {

                        return;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=act2Tower[0];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=2000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(3);
                    currentProperties.Position=act2Tower[0];
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=2000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
                if(myStatus.isAlpha&&myStatus.rawOrder==4) {

                    if(act2Tower.Count<2) {

                        return;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=act2Tower[1];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(3);
                    currentProperties.Position=act2Tower[1];
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }

                if(!myStatus.isAlpha&&myStatus.rawOrder==2) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,92),ARENA_CENTER,exitRotation+Math.PI);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=1000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,92),ARENA_CENTER,exitRotation+Math.PI);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.Delay=1000;
                    currentProperties.DestoryAt=3000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,92),ARENA_CENTER,exitRotation+Math.PI);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=4000;
                    currentProperties.DestoryAt=1000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                }
                
                if(!myStatus.isAlpha&&myStatus.rawOrder==3) {

                    if(act2Tower.Count<5) {

                        return;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=act2Tower[4];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=3000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(3);
                    currentProperties.Position=act2Tower[4];
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=3000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
            }
            
            if(skinsplitterCount==4) {
                
                if(myStatus.isAlpha&&myStatus.rawOrder==1) {
                    
                    if(act2Tower.Count<3) {

                        return;

                    }

                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=act2Tower[2];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=7000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(3);
                    currentProperties.Position=act2Tower[2];
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=7000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }

                if(myStatus.isAlpha&&myStatus.rawOrder==2) {
                    
                    if(act2Tower.Count<4) {

                        return;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=act2Tower[3];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(3);
                    currentProperties.Position=act2Tower[3];
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
                if(myStatus.isAlpha&&myStatus.rawOrder==3) {
                    
                    accessory.Method.RemoveDraw($"门神_细胞附身_中期_出口_指路");
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2,20);
                    currentProperties.Owner=sourceId;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=1000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2,20);
                    currentProperties.Owner=sourceId;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.Delay=1000;
                    currentProperties.DestoryAt=4000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                }
                
                if(myStatus.isAlpha&&myStatus.rawOrder==4) {
                    
                    if(act2Tower.Count<2) {

                        return;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=act2Tower[1];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=2000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(3);
                    currentProperties.Position=act2Tower[1];
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=2000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }

                if(!myStatus.isAlpha&&myStatus.rawOrder==3) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,92),ARENA_CENTER,exitRotation+Math.PI);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=1000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,92),ARENA_CENTER,exitRotation+Math.PI);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.Delay=1000;
                    currentProperties.DestoryAt=3000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,92),ARENA_CENTER,exitRotation+Math.PI);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=4000;
                    currentProperties.DestoryAt=1000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                }
                
                if(!myStatus.isAlpha&&myStatus.rawOrder==4) {
                    
                    if(act2Tower.Count<6) {

                        return;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=act2Tower[5];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=3000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(3);
                    currentProperties.Position=act2Tower[5];
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=3000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
            }
            
            if(skinsplitterCount==5) {

                if(myStatus.isAlpha&&myStatus.rawOrder==2) {
                    
                    if(act2Tower.Count<4) {

                        return;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=act2Tower[3];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=7000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(3);
                    currentProperties.Position=act2Tower[3];
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=7000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
                if(myStatus.isAlpha&&myStatus.rawOrder==4) {
                    
                    accessory.Method.RemoveDraw($"门神_细胞附身_中期_出口_指路");
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2,20);
                    currentProperties.Owner=sourceId;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=1000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2,20);
                    currentProperties.Owner=sourceId;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.Delay=1000;
                    currentProperties.DestoryAt=4000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                }

                if(!myStatus.isAlpha&&myStatus.rawOrder==1) {
                    
                    if(act2Tower.Count<7) {

                        return;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=act2Tower[6];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=3000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(3);
                    currentProperties.Position=act2Tower[6];
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=3000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }

                if(!myStatus.isAlpha&&myStatus.rawOrder==4) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,92),ARENA_CENTER,exitRotation+Math.PI);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=1000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,92),ARENA_CENTER,exitRotation+Math.PI);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.Delay=1000;
                    currentProperties.DestoryAt=3000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,92),ARENA_CENTER,exitRotation+Math.PI);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=4000;
                    currentProperties.DestoryAt=1000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                }
                
            }
            
            if(skinsplitterCount==6) {

                if(!myStatus.isAlpha&&myStatus.rawOrder==2) {
                    
                    if(act2Tower.Count<8) {

                        return;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=act2Tower[7];
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=3000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(3);
                    currentProperties.Position=act2Tower[7];
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=3000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
            }
            
            if(skinsplitterCount==7) {

                if(!myStatus.isAlpha) {
                    
                    accessory.Method.RemoveDraw($"门神_细胞附身_中期_出口_指路");
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2,20);
                    currentProperties.Owner=sourceId;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=1000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2,20);
                    currentProperties.Owner=sourceId;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.Delay=1000;
                    currentProperties.DestoryAt=4000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                }
                
            }

        }
        
        [ScriptMethod(name:"门神 细胞附身·晚期 (初始化与阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48831"],
            userControl:false)]
    
        public void 门神_细胞附身_晚期_初始化与阶段控制(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }
            
            act3PartyCount=0;
            for(int i=0;i<directionOfMitoticPhase.Length;++i)directionOfMitoticPhase[i]=DirectionsOfMitoticPhase.UNKNOWN;
            mitoticPhaseSemaphore.Reset();
            isCardinal=null;
            arenaDestructionSemaphore.Reset();

            Interlocked.Increment(ref currentPhase);

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"isInMajorPhase1={isInMajorPhase1}\ncurrentPhase={currentPhase}");
                
            }
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·晚期 分散细胞 (数据收集)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:3558"],
            userControl:false)]
    
        public void 门神_细胞附身_晚期_分散细胞_数据收集(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(act3PartyCount>=8) {

                return;

            }

            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));

            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            lock(directionOfMitoticPhase) {

                if(string.Equals(@event["Param"],"1078")) {

                    directionOfMitoticPhase[targetIndex]=DirectionsOfMitoticPhase.NORTH;

                }
                
                if(string.Equals(@event["Param"],"1079")) {

                    directionOfMitoticPhase[targetIndex]=DirectionsOfMitoticPhase.EAST;

                }

                if(string.Equals(@event["Param"],"1080")) {

                    directionOfMitoticPhase[targetIndex]=DirectionsOfMitoticPhase.SOUTH;

                }
                
                if(string.Equals(@event["Param"],"1081")) {

                    directionOfMitoticPhase[targetIndex]=DirectionsOfMitoticPhase.WEST;

                }

                if(directionOfMitoticPhase[targetIndex]==DirectionsOfMitoticPhase.UNKNOWN) {

                    return;

                }
                
                Interlocked.Increment(ref act3PartyCount);

                if(act3PartyCount==8) {

                    mitoticPhaseSemaphore.Set();

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"""
                                             directionOfMitoticPhase:{string.Join(",",directionOfMitoticPhase.Select(d=>d.ToString()))}
                                             """);
                        
                    }

                }
                
            }
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·晚期 分散细胞 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:3558"])]
    
        public void 门神_细胞附身_晚期_分散细胞_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            if(onlyMyMitoticPhase) {

                if(targetId!=accessory.Data.Me) {

                    return;

                }
                
            }

            float currentRotation=0;
            int currentDistance=0;
            
            if(string.Equals(@event["Param"],"1078")) {

                currentRotation=float.Pi;
                currentDistance=20;

            }
                
            if(string.Equals(@event["Param"],"1079")) {

                currentRotation=float.Pi/2;
                currentDistance=30;

            }

            if(string.Equals(@event["Param"],"1080")) {

                currentRotation=0;
                currentDistance=20;

            }
                
            if(string.Equals(@event["Param"],"1081")) {

                currentRotation=float.Pi/2*3;
                currentDistance=30;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"门神_细胞附身_晚期_分散细胞_范围_{targetId}";
            currentProperties.Scale=new(2,currentDistance);
            currentProperties.Owner=targetId;
            currentProperties.FixRotation=true;
            currentProperties.Rotation=currentRotation;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.DestoryAt=7200000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
            
            /*

            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"门神_细胞附身_晚期_分散细胞_落点_{targetId}";
            currentProperties.Scale=new(3);
            currentProperties.Owner=targetId;
            currentProperties.FixRotation=true;
            currentProperties.Rotation=0;
            currentProperties.Offset=new Vector3(horizontalOffset,0,verticalOffset);
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.DestoryAt=7200000;

            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);

            // It's non-viable to achieve the intended drawing due to engine limitations.
            // The idea may be re-enabled in the future.

            */
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·晚期 分散细胞 (清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:3558"],
            userControl:false)]
    
        public void 门神_细胞附身_晚期_分散细胞_清除(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                accessory.Method.RemoveDraw($"门神_细胞附身_晚期_分散细胞_范围_.*");
                
                /*
                
                accessory.Method.RemoveDraw("^门神_细胞附身_晚期_分散细胞_箭头_.*");
                accessory.Method.RemoveDraw("^门神_细胞附身_晚期_分散细胞_落点_.*");
                
                */
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"门神_细胞附身_晚期_分散细胞_范围_{targetId}");
            
            /*
            
            accessory.Method.RemoveDraw($"门神_细胞附身_晚期_分散细胞_箭头_{targetId}");
            accessory.Method.RemoveDraw($"门神_细胞附身_晚期_分散细胞_落点_{targetId}");
            
            */
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·晚期 盛大登场 (数据收集)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46241|46242)$"],
            userControl:false)]
    
        public void 门神_细胞附身_晚期_盛大登场_数据收集(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            lock(isCardinalLock) {

                if(isCardinal!=null) {

                    return;

                }

                else {

                    if(string.Equals(@event["ActionId"],"46241")) {

                        isCardinal=true;

                        arenaDestructionSemaphore.Set();

                    }
                    
                    if(string.Equals(@event["ActionId"],"46242")) {

                        isCardinal=false;
                        
                        arenaDestructionSemaphore.Set();

                    }

                    if(isCardinal!=null) {

                        if(enableDebugLogging) {
                            
                            accessory.Log.Debug($"isCardinal={isCardinal}");
                            
                        }
                        
                    }
                    
                }
                
            }
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·晚期 震场 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46241|46242)$"],
            suppress:5000)]
    
        public void 门神_细胞附身_晚期_震场_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            if(string.Equals(@event["ActionId"],"46241")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
            
                currentProperties.Scale=new(20,10);
                currentProperties.Position=ARENA_CENTER;
                currentProperties.Rotation=0;
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=7250;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
            
                currentProperties.Scale=new(15,10);
                currentProperties.Position=new Vector3(87.5f,0,90);
                currentProperties.Rotation=0;
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=7250;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
            
                currentProperties.Scale=new(15,10);
                currentProperties.Position=new Vector3(112.5f,0,90);
                currentProperties.Rotation=0;
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=7250;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
            
                currentProperties.Scale=new(15,10);
                currentProperties.Position=new Vector3(87.5f,0,110);
                currentProperties.Rotation=0;
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=7250;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(15,10);
                currentProperties.Position=new Vector3(112.5f,0,110);
                currentProperties.Rotation=0;
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=7250;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);

            }
            
            if(string.Equals(@event["ActionId"],"46242")) {

                currentProperties=accessory.Data.GetDefaultDrawProperties();
            
                currentProperties.Scale=new(20,30);
                currentProperties.Position=ARENA_CENTER;
                currentProperties.Rotation=0;
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=7250;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(10,10);
                currentProperties.Position=new Vector3(85,0,100);
                currentProperties.Rotation=0;
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=7250;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(10,10);
                currentProperties.Position=new Vector3(115,0,100);
                currentProperties.Rotation=0;
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=7250;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);

            }
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·晚期 细胞爆炸 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46240|46241|46242|46243)$"],
            suppress:5000)]

        public void 门神_细胞附身_晚期_细胞爆炸_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(9);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=10000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }

        [ScriptMethod(name:"门神 细胞附身·晚期 塔 (指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46240|46241|46242|46243)$"],
            suppress:5000)]
    
        public void 门神_细胞附身_晚期_塔_指路(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            mitoticPhaseSemaphore.WaitOne();
            arenaDestructionSemaphore.WaitOne();

            if(isCardinal==null) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"myIndex={myIndex}\ndirectionOfMitoticPhase[{myIndex}]={directionOfMitoticPhase[myIndex].ToString()}");
                
            }

            Vector3 myPosition=ARENA_CENTER;

            if(isCardinal==true) {

                if(isSupporter(myIndex)) {

                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.NORTH) {

                        myPosition=NORTH_SUPPORTER_WHEN_CARDINAL;

                    }
                    
                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.SOUTH) {

                        myPosition=SOUTH_SUPPORTER_WHEN_CARDINAL;

                    }
                    
                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.WEST) {

                        myPosition=WEST_SUPPORTER_WHEN_CARDINAL;

                    }
                    
                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.EAST) {

                        myPosition=EAST_SUPPORTER_WHEN_CARDINAL;

                    }
                    
                }

                if(isDps(myIndex)) {
                    
                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.NORTH) {

                        myPosition=NORTH_DPS_WHEN_CARDINAL;

                    }
                    
                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.SOUTH) {

                        myPosition=SOUTH_DPS_WHEN_CARDINAL;

                    }
                    
                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.WEST) {

                        myPosition=WEST_DPS_WHEN_CARDINAL;

                    }
                    
                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.EAST) {

                        myPosition=EAST_DPS_WHEN_CARDINAL;

                    }
                    
                }
                
            }

            if(isCardinal==false) {
                
                if(isSupporter(myIndex)) {

                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.NORTH) {

                        myPosition=NORTH_SUPPORTER_WHEN_INTERCARDINAL;

                    }
                    
                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.SOUTH) {

                        myPosition=SOUTH_SUPPORTER_WHEN_INTERCARDINAL;

                    }
                    
                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.WEST) {

                        myPosition=WEST_SUPPORTER_WHEN_INTERCARDINAL;

                    }
                    
                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.EAST) {

                        myPosition=EAST_SUPPORTER_WHEN_INTERCARDINAL;

                    }
                    
                }

                if(isDps(myIndex)) {
                    
                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.NORTH) {

                        myPosition=NORTH_DPS_WHEN_INTERCARDINAL;

                    }
                    
                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.SOUTH) {

                        myPosition=SOUTH_DPS_WHEN_INTERCARDINAL;

                    }
                    
                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.WEST) {

                        myPosition=WEST_DPS_WHEN_INTERCARDINAL;

                    }
                    
                    if(directionOfMitoticPhase[myIndex]==DirectionsOfMitoticPhase.EAST) {

                        myPosition=EAST_DPS_WHEN_INTERCARDINAL;

                    }
                    
                }
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=10000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"门神 细胞附身·晚期 分裂灾变 (范围)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:19196"])]

        public void 门神_细胞附身_晚期_分裂灾变_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(10,60);
            currentProperties.Owner=sourceId;
            currentProperties.TargetResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
            currentProperties.TargetOrderIndex=1;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=7250;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"门神 细胞附身·晚期 滴液灾变 (范围)",
            eventType:EventTypeEnum.SetObjPos,
            eventCondition:["SourceDataId:19196"])]

        public void 门神_细胞附身_晚期_滴液灾变_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }

            if(venomousScourgeDelay<0||venomousScourgeDelay>=9.75) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(uint i=1;i<=3;++i) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(5);
                currentProperties.Owner=sourceId;
                currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerFarestOrder;
                currentProperties.CentreOrderIndex=i;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=((int)(venomousScourgeDelay*1000));
                currentProperties.DestoryAt=9750-((int)(venomousScourgeDelay*1000));
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"门神 细胞附身·末期 (初始化与阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48832"],
            userControl:false)]
    
        public void 门神_细胞附身_末期_初始化与阶段控制(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }
            
            act4PartyCount=0;
            for(int i=0;i<isRottingFlesh.Length;++i)isRottingFlesh[i]=false;
            act4FleshPile.Clear();
            areNorthwestAndSoutheast=true;

            Interlocked.Increment(ref currentPhase);

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"isInMajorPhase1={isInMajorPhase1}\ncurrentPhase={currentPhase}");
                
            }
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·末期 (数据收集)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:regex:^(4761|4763)$"],
            userControl:false)]
    
        public void 门神_细胞附身_末期_数据收集(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }

            if(act4PartyCount>=8) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));

            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            lock(isRottingFlesh) {

                if(string.Equals(@event["StatusID"],"4763")) {

                    isRottingFlesh[targetIndex]=true;

                }
                
                if(string.Equals(@event["StatusID"],"4761")) {

                    isRottingFlesh[targetIndex]=false;

                }
                
                Interlocked.Increment(ref act4PartyCount);

                if(act4PartyCount==8) {

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"""
                                             isRottingFlesh:{string.Join(",",isRottingFlesh)}
                                             """);
                        
                    }

                }
                
            }
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·末期 极饿伸展 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46237"])]
    
        public void 门神_细胞附身_末期_极饿伸展_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(35);
                currentProperties.Owner=sourceId;
                currentProperties.Radian=float.Pi/3*2;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=4875;
                currentProperties.DestoryAt=5750;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

            }

            else {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(35);
                currentProperties.Owner=sourceId;
                currentProperties.Radian=float.Pi/3*2;
                currentProperties.Color=((isRottingFlesh[myIndex])?(accessory.Data.DefaultSafeColor):(accessory.Data.DefaultDangerColor));
                currentProperties.Delay=4875;
                currentProperties.DestoryAt=5750;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(35);
                currentProperties.Owner=sourceId;
                currentProperties.Radian=float.Pi*2-float.Pi/3*2;
                currentProperties.Rotation=float.Pi;
                currentProperties.Color=((isRottingFlesh[myIndex])?(accessory.Data.DefaultDangerColor):(accessory.Data.DefaultSafeColor));
                currentProperties.Delay=4875;
                currentProperties.DestoryAt=5750;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                
            }
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·末期 引爆细胞与致死细胞 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:regex:^(4761|4763)$"])]
    
        public void 门神_细胞附身_末期_引爆细胞与致死细胞_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            durationMilliseconds-=7875;

            if(durationMilliseconds<=0||durationMilliseconds>=7200000) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"门神_细胞附身_末期_引爆细胞与致死细胞_范围_{targetId}";
            currentProperties.Scale=new(6);
            currentProperties.Owner=targetId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=7875;
            currentProperties.DestoryAt=durationMilliseconds;

            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·末期 引爆细胞与致死细胞 (清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:regex:^(4761|4763)$"],
            userControl:false)]
    
        public void 门神_细胞附身_末期_引爆细胞与致死细胞_清除(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                accessory.Method.RemoveDraw("^门神_细胞附身_末期_引爆细胞与致死细胞_范围_.*");
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"门神_细胞附身_末期_引爆细胞与致死细胞_范围_{targetId}");
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·末期 极饿伸展 (指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46237"])]
    
        public void 门神_细胞附身_末期_极饿伸展_指路(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }

            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");
                
                return;
                
            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }
            
            Vector3 myPosition=ARENA_CENTER;
            
            if(sourcePosition.X<100) {

                if(isRottingFlesh[myIndex]) {

                    myPosition=POSITION_ON_RIGHT[myIndex%4];

                }

                else {
                    
                    myPosition=POSITION_ON_LEFT[myIndex%4];
                    
                }
                
            }

            if(sourcePosition.X>100) {
                
                if(isRottingFlesh[myIndex]) {

                    myPosition=POSITION_ON_LEFT[myIndex%4];

                }

                else {
                    
                    myPosition=POSITION_ON_RIGHT[myIndex%4];
                    
                }
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=4875;
            currentProperties.DestoryAt=6125;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=ARENA_CENTER;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=11000;
            currentProperties.DestoryAt=4750;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·末期 连环有害细胞α (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:4753"])]
    
        public void 门神_细胞附身_末期_连环有害细胞α_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>=7200000) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"门神_细胞附身_末期_连环有害细胞α_范围_{targetId}";
            currentProperties.Scale=new(4);
            currentProperties.Owner=targetId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=durationMilliseconds;

            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·末期 连环有害细胞α (清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:4753"],
            userControl:false)]
    
        public void 门神_细胞附身_末期_连环有害细胞α_清除(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                accessory.Method.RemoveDraw("^门神_细胞附身_末期_连环有害细胞α_范围_.*");
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"门神_细胞附身_末期_连环有害细胞α_范围_{targetId}");
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·末期 大爆炸 (数据收集)",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:2015017"],
            userControl:false)]
    
        public void 门神_细胞附身_末期_大爆炸_数据收集(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }

            if(!string.Equals(@event["Operate"],"Add")) {

                return;

            }

            if(act4FleshPile.Count>=5) {

                return;

            }

            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            lock(act4FleshPile) {
                
                act4FleshPile.Add(sourcePosition);

                if(act4FleshPile.Count==5) {

                    areNorthwestAndSoutheast=true;

                    for(int i=0;i<act4FleshPile.Count;++i) {

                        if(Vector3.Distance(act4FleshPile[i],new Vector3(81,0,86))<12) {
                            
                            areNorthwestAndSoutheast=false;
                            
                        }
                        
                    }

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"""
                                             areNorthwestAndSoutheast={areNorthwestAndSoutheast}
                                             act4FleshPile:{string.Join(",",act4FleshPile)}
                                             """);
                        
                    }

                }
                
            }
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·末期 大爆炸 (范围)",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:2015017"])]
    
        public void 门神_细胞附身_末期_大爆炸_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }

            if(!string.Equals(@event["Operate"],"Add")) {

                return;

            }

            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(12);
            currentProperties.Position=sourcePosition;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=12000;
            currentProperties.DestoryAt=12500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·末期 连环有害细胞α (指路 阶段1)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:4753"])]
    
        public void 门神_细胞附身_末期_连环有害细胞α_指路_阶段1(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            if(targetId!=accessory.Data.Me) {

                return;

            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>=7200000) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }
            
            Vector3 myPosition1=ARENA_CENTER;
            Vector3 myPosition2=ARENA_CENTER;

            if(isSupporter(myIndex)) {

                myPosition1=SUPPORTER_POSITION[myIndex%4];

            }

            if(isDps(myIndex)) {
                
                myPosition1=DPS_POSITION[myIndex%4];
                
            }

            if(areNorthwestAndSoutheast) {
                
                if(isSupporter(myIndex)) {

                    myPosition2=SUPPORTER_POSITION[0];

                }

                if(isDps(myIndex)) {

                    myPosition2=DPS_POSITION[3];

                }
                
            }

            else {
                
                if(isSupporter(myIndex)) {

                    myPosition2=SUPPORTER_POSITION[3];

                }

                if(isDps(myIndex)) {

                    myPosition2=DPS_POSITION[0];

                }
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="门神_细胞附身_末期_连环有害细胞α_指路_阶段1_1";
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition1;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=durationMilliseconds;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            if(!(Vector3.Distance(myPosition1,myPosition2)<0.05)) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Name="门神_细胞附身_末期_连环有害细胞α_指路_阶段1_2";
                currentProperties.Scale=new(2);
                currentProperties.Position=myPosition1;
                currentProperties.TargetPosition=myPosition2;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=durationMilliseconds;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
            }
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·末期 连环有害细胞α (指路 阶段1 清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:4753"],
            userControl:false)]
    
        public void 门神_细胞附身_末期_连环有害细胞α_指路_阶段1_清除(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            if(targetId!=accessory.Data.Me) {

                return;

            }
            
            accessory.Method.RemoveDraw($"门神_细胞附身_末期_连环有害细胞α_指路_阶段1_1");
            accessory.Method.RemoveDraw($"门神_细胞附身_末期_连环有害细胞α_指路_阶段1_2");
            accessory.Method.RemoveDraw($"门神_细胞附身_末期_连环有害细胞α_指路_阶段1_.*");
        
        }
        
        [ScriptMethod(name:"门神 细胞附身·末期 连环有害细胞α (指路 阶段2)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:4753"])]
    
        public void 门神_细胞附身_末期_连环有害细胞α_指路_阶段2(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            if(targetId!=accessory.Data.Me) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            Vector3 myPosition=ARENA_CENTER;
            
            if(areNorthwestAndSoutheast) {
                
                if(isSupporter(myIndex)) {

                    myPosition=SUPPORTER_POSITION[0];

                }

                if(isDps(myIndex)) {

                    myPosition=DPS_POSITION[3];

                }
                
            }

            else {
                
                if(isSupporter(myIndex)) {

                    myPosition=SUPPORTER_POSITION[3];

                }

                if(isDps(myIndex)) {

                    myPosition=DPS_POSITION[0];

                }
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="门神_细胞附身_末期_连环有害细胞α_指路_阶段2";
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=7200000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"门神 细胞附身·末期 连环有害细胞α (指路 阶段2 清除)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:46239"],
            suppress:1000,
            userControl:false)]
    
        public void 门神_细胞附身_末期_连环有害细胞α_指路_阶段2_清除(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }
            
            accessory.Method.RemoveDraw($"门神_细胞附身_末期_连环有害细胞α_指路_阶段1_1");
            accessory.Method.RemoveDraw($"门神_细胞附身_末期_连环有害细胞α_指路_阶段1_2");
            accessory.Method.RemoveDraw($"门神_细胞附身_末期_连环有害细胞α_指路_阶段1_.*");
            
            accessory.Method.RemoveDraw($"门神_细胞附身_末期_连环有害细胞α_指路_阶段2");
        
        }
        
        [ScriptMethod(name:"门神 喋血 (初始化与阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46275|46276|46277|46278)$"],
            userControl:false)]
    
        public void 门神_喋血_初始化与阶段控制(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(!(6<=currentPhase&&currentPhase<9)&&!skipPhaseChecks) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }
            
            slaughtershedInitializationSemaphore.Reset();
            slaughtershedFleshPile.Clear();
            isStackOnLeft=false;
            slaughtershedBurstSemaphore.Reset();
            slaughtershedIconCount=0;
            for(int i=0;i<targetIconOfSlaughtershed.Length;++i)targetIconOfSlaughtershed[i]=TargetIconsOfSlaughtershed.NONE;
            slaughtershedIconSemaphore1.Reset();
            slaughtershedIconSemaphore2.Reset();

            Interlocked.Increment(ref currentPhase);
            
            slaughtershedInitializationSemaphore.Set();

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"isInMajorPhase1={isInMajorPhase1}\ncurrentPhase={currentPhase}");
                
            }
        
        }
        
        [ScriptMethod(name:"门神 喋血 大爆炸 (数据收集)",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:2015017"],
            userControl:false)]
    
        public void 门神_喋血_大爆炸_数据收集(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            bool isinitialized=slaughtershedInitializationSemaphore.WaitOne(3000);

            if(!isinitialized) {

                return;

            }

            if(isGlobalInitialization) {

                return;

            }
            
            if(!(7<=currentPhase&&currentPhase<=9)&&!skipPhaseChecks) {

                return;

            }

            if(!string.Equals(@event["Operate"],"Add")) {

                return;

            }

            if(slaughtershedFleshPile.Count>=5) {

                return;

            }

            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            lock(slaughtershedFleshPile) {
                
                slaughtershedFleshPile.Add(sourcePosition);

                if(slaughtershedFleshPile.Count==5) {

                    slaughtershedInitializationSemaphore.Reset();

                    isStackOnLeft=false;

                    for(int i=0;i<slaughtershedFleshPile.Count;++i) {

                        if(Vector3.Distance(slaughtershedFleshPile[i],new Vector3(83,0,88))<12) {
                            
                            isStackOnLeft=true;
                            
                        }
                        
                    }

                    slaughtershedBurstSemaphore.Set();

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"""
                                             isStackOnLeft={isStackOnLeft}
                                             slaughtershedFleshPile:{string.Join(",",slaughtershedFleshPile)}
                                             """);
                        
                    }

                }
                
            }
        
        }
        
        [ScriptMethod(name:"门神 喋血 大爆炸 (范围)",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:2015017"])]
    
        public void 门神_喋血_大爆炸_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(!(7<=currentPhase&&currentPhase<=9)&&!skipPhaseChecks) {

                return;

            }

            if(!string.Equals(@event["Operate"],"Add")) {

                return;

            }

            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(12);
            currentProperties.Position=sourcePosition;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=14250;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 喋血 细胞轰炸与细胞爆炸 (数据收集)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:regex:^(0177|013D)$"],
            userControl:false)]
    
        public void 门神_喋血_细胞轰炸与细胞爆炸_数据收集(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(!(7<=currentPhase&&currentPhase<=9)&&!skipPhaseChecks) {

                return;

            }

            if(slaughtershedIconCount>=5) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));

            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            lock(targetIconOfSlaughtershed) {

                if(string.Equals(@event["Id"],"013D")) {

                    targetIconOfSlaughtershed[targetIndex]=TargetIconsOfSlaughtershed.STACK;

                }
                
                if(string.Equals(@event["Id"],"0177")) {

                    targetIconOfSlaughtershed[targetIndex]=TargetIconsOfSlaughtershed.SPREAD;

                }
                
                Interlocked.Increment(ref slaughtershedIconCount);

                if(slaughtershedIconCount==5) {

                    slaughtershedIconSemaphore1.Set();
                    slaughtershedIconSemaphore2.Set();

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"""
                                             targetIconOfSlaughtershed:{string.Join(",",targetIconOfSlaughtershed.Select(t=>t.ToString()))}
                                             """);
                        
                    }

                }
                
            }

        }
        
        [ScriptMethod(name:"门神 喋血 细胞轰炸 (范围)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:013D"])]
    
        public void 门神_喋血_细胞轰炸_范围(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }

            if(!(7<=currentPhase&&currentPhase<=9)&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));

            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            slaughtershedIconSemaphore2.WaitOne();
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            if(!isLegalPartyIndex(myIndex)) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6);
            currentProperties.Owner=accessory.Data.PartyList[targetIndex];
            currentProperties.Color=((targetIconOfSlaughtershed[myIndex]==TargetIconsOfSlaughtershed.SPREAD)?(accessory.Data.DefaultDangerColor):(accessory.Data.DefaultSafeColor));
            currentProperties.DestoryAt=7125;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"门神 喋血 (指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46275|46276|46277|46278)$"])]
    
        public void 门神_喋血_指路(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }
            
            if(!(6<=currentPhase&&currentPhase<=9)&&!skipPhaseChecks) {

                return;

            }

            slaughtershedBurstSemaphore.WaitOne();
            slaughtershedIconSemaphore1.WaitOne();

            if(!(7<=currentPhase&&currentPhase<=9)&&!skipPhaseChecks) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            Vector3 myPosition=ARENA_CENTER;

            if(isStackOnLeft) {

                if(targetIconOfSlaughtershed[myIndex]==TargetIconsOfSlaughtershed.STACK
                   ||
                   targetIconOfSlaughtershed[myIndex]==TargetIconsOfSlaughtershed.NONE) {

                    myPosition=STACK_ON_LEFT;

                }

                if(targetIconOfSlaughtershed[myIndex]==TargetIconsOfSlaughtershed.SPREAD) {

                    myPosition=SPREAD_ON_RIGHT[myIndex%4];

                }
                
            }

            else {
                
                if(targetIconOfSlaughtershed[myIndex]==TargetIconsOfSlaughtershed.STACK
                   ||
                   targetIconOfSlaughtershed[myIndex]==TargetIconsOfSlaughtershed.NONE) {

                    myPosition=STACK_ON_RIGHT;

                }

                if(targetIconOfSlaughtershed[myIndex]==TargetIconsOfSlaughtershed.SPREAD) {

                    myPosition=SPREAD_ON_LEFT[myIndex%4];

                }
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=7125;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"门神 喋血 灾变吐息与追猎重击 (范围与击退指示)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:regex:^(46283|46285|46284|46286)$"])]
    
        public void 门神_喋血_灾变吐息与追猎重击_范围与击退指示(Event @event,ScriptAccessory accessory) {

            if(!isInMajorPhase1) {

                return;

            }
            
            if(!(7<=currentPhase&&currentPhase<=9)&&!skipPhaseChecks) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            // 46283: Left line first
            // 46285: Right line first
            // 46284: Left knock-back first
            // 46286: Right knock-back first

            if(string.Equals(@event["ActionId"],"46283")) {
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(20,30);
                currentProperties.Position=SLAUGHTERSHED_LEFT_ARENA_CENTER;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(20,30);
                currentProperties.Position=SLAUGHTERSHED_RIGHT_ARENA_CENTER;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(20,30);
                currentProperties.Position=SLAUGHTERSHED_RIGHT_ARENA_CENTER;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
            }
            
            if(string.Equals(@event["ActionId"],"46285")) {
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(20,30);
                currentProperties.Position=SLAUGHTERSHED_RIGHT_ARENA_CENTER;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(20,30);
                currentProperties.Position=SLAUGHTERSHED_LEFT_ARENA_CENTER;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(20,30);
                currentProperties.Position=SLAUGHTERSHED_LEFT_ARENA_CENTER;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
            }

            if(string.Equals(@event["ActionId"],"46284")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(2,30);
                currentProperties.Position=SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=new Vector3(SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER.X+1,0,SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER.Z+1);
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(11.314f,4.243f);
                currentProperties.Position=SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=new Vector3(SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER.X-1,0,SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER.Z-1);
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Position=SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER;
                currentProperties.TargetObject=accessory.Data.Me;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2,30);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER;
                currentProperties.Rotation=float.Pi;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(2,30);
                currentProperties.Position=SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=new Vector3(SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER.X-1,0,SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER.Z+1);
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(11.314f,4.243f);
                currentProperties.Position=SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=new Vector3(SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER.X+1,0,SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER.Z-1);
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Position=SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER;
                currentProperties.TargetObject=accessory.Data.Me;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2,30);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER;
                currentProperties.Rotation=float.Pi;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
            }
            
            if(string.Equals(@event["ActionId"],"46286")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(2,30);
                currentProperties.Position=SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=new Vector3(SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER.X-1,0,SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER.Z+1);
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(11.314f,4.243f);
                currentProperties.Position=SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=new Vector3(SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER.X+1,0,SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER.Z-1);
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Position=SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER;
                currentProperties.TargetObject=accessory.Data.Me;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2,30);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=SLAUGHTERSHED_RIGHT_KNOCK_BACK_CENTER;
                currentProperties.Rotation=float.Pi;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(2,30);
                currentProperties.Position=SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=new Vector3(SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER.X+1,0,SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER.Z+1);
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(11.314f,4.243f);
                currentProperties.Position=SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=new Vector3(SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER.X-1,0,SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER.Z-1);
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Position=SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER;
                currentProperties.TargetObject=accessory.Data.Me;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2,30);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=SLAUGHTERSHED_LEFT_KNOCK_BACK_CENTER;
                currentProperties.Rotation=float.Pi;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
            }

        }
        
        #endregion
        
        #region Major_Phase_Control

        [ScriptMethod(name:"门神本体切换控制 门神识别 补天之手",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46295"],
            userControl:false)]
    
        public void 门神本体切换控制_门神识别_补天之手(Event @event,ScriptAccessory accessory) {

            if(initializedMajorPhase1) {

                return;

            }

            else {
                
                initializedMajorPhase1=true;
                isInMajorPhase1=true;
                VariableAndSemaphoreInitialization();

                if(enableDebugLogging) {
                    
                    accessory.Log.Debug($"isInMajorPhase1={isInMajorPhase1}");
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"门神本体切换控制 本体识别 境中奇焰",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46376"],
            userControl:false)]
    
        public void 门神本体切换控制_本体识别_境中奇焰(Event @event,ScriptAccessory accessory) {

            if(initializedMajorPhase2) {

                return;

            }

            else {
                
                initializedMajorPhase2=true;
                isInMajorPhase1=false;
                VariableAndSemaphoreInitialization();

                if(enableDebugLogging) {
                    
                    accessory.Log.Debug($"isInMajorPhase1={isInMajorPhase1}");
                    
                }

            }
        
        }
        
        #endregion
        
        #region Major_Phase_2

        [ScriptMethod(name:"本体 自我复制 (初始化与阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46296"],
            userControl:false)]
    
        public void 本体_自我复制_初始化与阶段控制(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=0&&!skipPhaseChecks) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }

            isFrontAndBackInPhase1=null;
            phase1LindschratCount=0;

            Interlocked.Increment(ref currentPhase);

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"isInMajorPhase1={isInMajorPhase1}\ncurrentPhase={currentPhase}");
                
            }
        
        }
        
        [ScriptMethod(name:"本体 自我复制 有翼灾变 (数据收集)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46298|46299)$"],
            userControl:false)]
    
        public void 本体_自我复制_有翼灾变_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=1&&!skipPhaseChecks) {

                return;

            }

            if(isFrontAndBackInPhase1!=null) {

                return;

            }
            
            // 46298: Front and back
            // 46299: Left and right

            if(string.Equals(@event["ActionId"],"46298")) {

                isFrontAndBackInPhase1=true;

            }
            
            if(string.Equals(@event["ActionId"],"46299")) {
                
                isFrontAndBackInPhase1=false;
                
            }
            
        }
        
        [ScriptMethod(name:"本体 自我复制 有翼灾变 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46298|46299)$"])]
    
        public void 本体_自我复制_有翼灾变_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(string.Equals(@event["ActionId"],"46298")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(50);
                currentProperties.Owner=sourceId;
                currentProperties.Radian=float.Pi/6;
                currentProperties.Rotation=0;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=4000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(50);
                currentProperties.Owner=sourceId;
                currentProperties.Radian=float.Pi/6;
                currentProperties.Rotation=float.Pi;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=4000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                
            }
            
            if(string.Equals(@event["ActionId"],"46299")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(50);
                currentProperties.Owner=sourceId;
                currentProperties.Radian=float.Pi/6;
                currentProperties.Rotation=float.Pi/2;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=4000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(50);
                currentProperties.Owner=sourceId;
                currentProperties.Radian=float.Pi/6;
                currentProperties.Rotation=-float.Pi/2;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=4000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                
            }
            
        }
        
        [ScriptMethod(name:"本体 自我复制 强力魔法 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46303"])]
    
        public void 本体_自我复制_强力魔法_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(5);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
            currentProperties.CentreOrderIndex=1;
            currentProperties.Color=colourOfMightyMagic.V4.WithW(1);
            currentProperties.DestoryAt=4250;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(5);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
            currentProperties.CentreOrderIndex=2;
            currentProperties.Color=colourOfMightyMagic.V4.WithW(1);
            currentProperties.DestoryAt=4250;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 自我复制 天顶猛击 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46301"])]
    
        public void 本体_自我复制_天顶猛击_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(5);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
            currentProperties.CentreOrderIndex=1;
            currentProperties.Color=colourOfTopTierSlam.V4.WithW(1);
            currentProperties.DestoryAt=3250;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 自我复制 蛇踢 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46375"])]
    
        public void 本体_自我复制_蛇踢_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(50);
            currentProperties.Owner=sourceId;
            currentProperties.Radian=float.Pi;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=5000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
        }

        [ScriptMethod(name:"本体 自我复制 双重飞踢 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46368|46373)$"])]

        public void 本体_自我复制_双重飞踢_范围(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;

            }

            if(currentPhase!=1&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {

                return;

            }

            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(40);
            currentProperties.Owner=sourceId;
            currentProperties.Radian=float.Pi;

            if(string.Equals(@event["ActionId"],"46368")) {

                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=5500;

            }
            
            if(string.Equals(@event["ActionId"],"46373")) {

                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=4500;
                
            }
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 自我复制 魔力连击 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46368"])]
    
        public void 本体_自我复制_魔力连击_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(10);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.OwnerEnmityOrder;
            currentProperties.CentreOrderIndex=1;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=10125;
            currentProperties.DestoryAt=2500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(10);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.OwnerEnmityOrder;
            currentProperties.CentreOrderIndex=2;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=10125;
            currentProperties.DestoryAt=2500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 自我复制 人形分身 (类型指示)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:46297"])]
    
        public void 本体_自我复制_人形分身_类型指示(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=1&&!skipPhaseChecks) {

                return;

            }

            if(!string.Equals(@event["SourceDataId"],"19204")) {

                return;

            }

            if(phase1LindschratCount>=8) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            lock(phase1LindschratCountLock) {

                Interlocked.Increment(ref phase1LindschratCount);

                if(phase1LindschratCount<=4) {
                    
                    double sourceRotation=0;

                    try {
                        
                        sourceRotation=JsonConvert.DeserializeObject<double>(@event["SourceRotation"]);

                    } catch(Exception e) {
                
                        accessory.Log.Error("SourceRotation deserialization failed.");

                        return;

                    }

                    if(Math.Abs(sourceRotation-0)>0.00001) {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperties.Scale=new(1);
                        currentProperties.Owner=sourceId;
                        currentProperties.Color=colourOfTopTierSlam.V4.WithW(1);
                        currentProperties.DestoryAt=7375;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperties.Scale=new(5);
                        currentProperties.Owner=sourceId;
                        currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                        currentProperties.CentreOrderIndex=1;
                        currentProperties.Color=colourOfTopTierSlam.V4.WithW(1);
                        currentProperties.Delay=1000;
                        currentProperties.DestoryAt=3625;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                    }

                    else {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperties.Scale=new(1);
                        currentProperties.Owner=sourceId;
                        currentProperties.Color=colourOfMightyMagic.V4.WithW(1);
                        currentProperties.DestoryAt=8500;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperties.Scale=new(5);
                        currentProperties.Owner=sourceId;
                        currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                        currentProperties.CentreOrderIndex=1;
                        currentProperties.Color=colourOfMightyMagic.V4.WithW(1);
                        currentProperties.Delay=1000;
                        currentProperties.DestoryAt=3625;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
                        currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                        currentProperties.Scale=new(5);
                        currentProperties.Owner=sourceId;
                        currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                        currentProperties.CentreOrderIndex=2;
                        currentProperties.Color=colourOfMightyMagic.V4.WithW(1);
                        currentProperties.Delay=1000;
                        currentProperties.DestoryAt=3625;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                    }
                    
                }

                if(5<=phase1LindschratCount&&phase1LindschratCount<=8) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(1);
                    currentProperties.Owner=sourceId;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=7125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                     
                     if(isFrontAndBackInPhase1==null) {

                        return;

                    }

                    else {

                        if((bool)isFrontAndBackInPhase1) {
                            
                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(50);
                            currentProperties.Owner=sourceId;
                            currentProperties.Radian=float.Pi/6;
                            currentProperties.FixRotation=true;
                            currentProperties.Rotation=0;
                            currentProperties.Color=accessory.Data.DefaultDangerColor;
                            currentProperties.Delay=1000;
                            currentProperties.DestoryAt=2500;
        
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(50);
                            currentProperties.Owner=sourceId;
                            currentProperties.Radian=float.Pi/6;
                            currentProperties.FixRotation=true;
                            currentProperties.Rotation=float.Pi;
                            currentProperties.Color=accessory.Data.DefaultDangerColor;
                            currentProperties.Delay=1000;
                            currentProperties.DestoryAt=2500;
        
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                            
                        }

                        else {
                            
                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(50);
                            currentProperties.Owner=sourceId;
                            currentProperties.Radian=float.Pi/6;
                            currentProperties.FixRotation=true;
                            currentProperties.Rotation=float.Pi/2;
                            currentProperties.Color=accessory.Data.DefaultDangerColor;
                            currentProperties.Delay=1000;
                            currentProperties.DestoryAt=2500;
        
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(50);
                            currentProperties.Owner=sourceId;
                            currentProperties.Radian=float.Pi/6;
                            currentProperties.FixRotation=true;
                            currentProperties.Rotation=-float.Pi/2;
                            currentProperties.Color=accessory.Data.DefaultDangerColor;
                            currentProperties.Delay=1000;
                            currentProperties.DestoryAt=2500;
        
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                            
                        }
                        
                    }
                    
                }

            }
            
        }
        
        [ScriptMethod(name:"本体 模仿细胞 (初始化与阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46305"],
            userControl:false)]
    
        public void 本体_模仿细胞_初始化与阶段控制(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }

            phase2BossId=0;
            phase2PlayerStagingCount=0;
            for(int i=0;i<phase2PlayerStaging.Length;++i)phase2PlayerStaging[i]=-1;
            phase2LindschratCount=0;
            for(int i=0;i<phase2Lindschrat.Length;++i)phase2Lindschrat[i]=Phase2TetherActions.UNKNOWN;
            phase2LindschratSemaphore.Reset();
            phase2StagingActionCount=0;
            for(int i=0;i<phase2StagingActions.Length;++i)phase2StagingActions[i].Clear();
            phase2DisableGuidance=false;
            phase2StagingActionSemaphore1.Reset();
            phase2StagingActionSemaphore2.Reset();
            phase2StagingActionSemaphore3.Reset();
            isRecordingScaldingWavesInPhase2=true;
            phase2ScaldingWavesPlayers.Clear();
            isRecordingManaBurstInPhase2=true;
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }

            phase2BossId=sourceId;

            Interlocked.Increment(ref currentPhase);

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"isInMajorPhase1={isInMajorPhase1}\ncurrentPhase={currentPhase}");
                
            }
        
        }
        
        [ScriptMethod(name:"本体 模仿细胞 模仿细胞 (数据收集)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:0175"],
            userControl:false)]
    
        public void 本体_模仿细胞_模仿细胞_数据收集(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase2PlayerStagingCount>=8) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null) {

                return;

            }

            if(sourceObject.DataId!=19210) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            int discretizedPosition=discretizePosition(sourcePosition,ARENA_CENTER,8);

            if(discretizedPosition<0||discretizedPosition>7) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            lock(phase2PlayerStaging) {

                phase2PlayerStaging[targetIndex]=discretizedPosition;

                Interlocked.Increment(ref phase2PlayerStagingCount);

                if(phase2PlayerStagingCount==8) {

                    if(enableDebugLogging) {

                        accessory.Log.Debug($"""
                                             phase2PlayerStaging:{string.Join(",",phase2PlayerStaging)}
                                             """);
                    }
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"本体 模仿细胞 模仿细胞 (分身指示)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:0175"])]
    
        public void 本体_模仿细胞_模仿细胞_分身指示(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null) {

                return;

            }

            if(sourceObject.DataId!=19210) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            if(targetId!=accessory.Data.Me) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=sourcePosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.DestoryAt=12625;
                
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
        
        }
        
        [ScriptMethod(name:"本体 模仿细胞 人形分身 (数据收集)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:regex:^(016F|0171|0170)$"],
            userControl:false)]
    
        public void 本体_模仿细胞_人形分身_数据收集(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase2LindschratCount>=6) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null) {

                return;

            }

            if(sourceObject.DataId!=19204) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            int discretizedPosition=discretizePosition(sourcePosition,ARENA_CENTER,6);

            if(discretizedPosition<0||discretizedPosition>5) {

                return;

            }

            lock(phase2Lindschrat) {
                
                if(phase2Lindschrat[discretizedPosition]!=Phase2TetherActions.UNKNOWN) {

                    return;

                }
                
                // 016F: Fan
                // 0171: Stack
                // 0170: Defamation
                // 0176: Boss combo

                if(string.Equals(@event["Id"],"016F")) {

                    phase2Lindschrat[discretizedPosition]=Phase2TetherActions.FAN;

                }
                
                if(string.Equals(@event["Id"],"0171")) {

                    phase2Lindschrat[discretizedPosition]=Phase2TetherActions.STACK;

                }
                
                if(string.Equals(@event["Id"],"0170")) {

                    phase2Lindschrat[discretizedPosition]=Phase2TetherActions.DEFAMATION;

                }

                Interlocked.Increment(ref phase2LindschratCount);

                if(phase2LindschratCount==6) {

                    phase2LindschratSemaphore.Set();

                    if(enableDebugLogging) {

                        accessory.Log.Debug($"""
                                             phase2Lindschrat:{string.Join(",",phase2Lindschrat.Select(p=>p.ToString()))}
                                             """);
                        
                    }
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"本体 模仿细胞 人形分身 (接线指路)",
            eventType:EventTypeEnum.PlayActionTimeline,
            eventCondition:["Id:7750"],
            suppress:1000)]
    
        public void 本体_模仿细胞_人形分身_接线指路(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(!string.Equals(@event["SourceDataId"],"19204")) {

                return;

            }

            phase2LindschratSemaphore.WaitOne();
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(phase2PlayerStaging[myIndex]==4) {

                for(int i=0;i<6;++i) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Position=rotatePosition(new Vector3(100,0,90),ARENA_CENTER,Math.PI/3*i);
                    currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=8000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=new Vector3(100,0,119);
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=8000;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
            }

            if(phase2PlayerStaging[myIndex]==0) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetObject=phase2BossId;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=8000;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=phase2BossId;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=8000;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                
            }

            if(phase2PlayerStaging[myIndex]!=4&&phase2PlayerStaging[myIndex]!=0) {
                
                int myDiscretizedPosition=-1;
                Vector3 myPosition=ARENA_CENTER;

                Phase2TetherActions myAction=phase2PlayerStaging[myIndex] switch {
                    
                    1 => Phase2TetherActions.FAN,
                    2 => Phase2TetherActions.STACK,
                    3 => Phase2TetherActions.DEFAMATION,
                    4 => Phase2TetherActions.BOSS_COMBO,
                    5 => Phase2TetherActions.DEFAMATION,
                    6 => Phase2TetherActions.STACK,
                    7 => Phase2TetherActions.FAN,
                    _ => Phase2TetherActions.UNKNOWN
                    
                };

                if(myAction==Phase2TetherActions.UNKNOWN||myAction==Phase2TetherActions.BOSS_COMBO) {

                    return;

                }

                if(1<=phase2PlayerStaging[myIndex]&&phase2PlayerStaging[myIndex]<=3) {

                    for(int i=0;i<6;++i) {

                        if(phase2Lindschrat[i]==myAction) {

                            myDiscretizedPosition=i;

                            break;

                        }
                        
                    }
                    
                }
                
                if(5<=phase2PlayerStaging[myIndex]&&phase2PlayerStaging[myIndex]<=7) {

                    for(int i=5;i>=0;--i) {

                        if(phase2Lindschrat[i]==myAction) {

                            myDiscretizedPosition=i;

                            break;

                        }
                        
                    }
                    
                }

                if(myDiscretizedPosition<0||myDiscretizedPosition>5) {

                    return;

                }
                
                myPosition=rotatePosition(new Vector3(100,0,90),ARENA_CENTER,Math.PI/3*myDiscretizedPosition);
                
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

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"myIndex={myIndex}\nphase2PlayerStaging[{myIndex}]={phase2PlayerStaging[myIndex]}");
                
            }

        }
        
        [ScriptMethod(name:"本体 模仿细胞 模仿细胞 (技能记录)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:0175"],
            userControl:false)]
    
        public void 本体_模仿细胞_模仿细胞_技能记录(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase2StagingActionCount>=7) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null) {

                return;

            }

            if(sourceObject.DataId!=19204&&sourceObject.DataId!=19202) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            int targetStaging=phase2PlayerStaging[targetIndex];

            if(targetStaging<0||targetStaging>7) {

                return;

            }

            lock(phase2StagingActions) {

                if(sourceObject.DataId==19202) {
                    
                    phase2StagingActions[targetStaging].Add(Phase2TetherActions.BOSS_COMBO);

                    Interlocked.Increment(ref phase2StagingActionCount);

                }

                if(sourceObject.DataId==19204) {
                    
                    Vector3 sourcePosition=ARENA_CENTER;

                    try {

                        sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

                    } catch(Exception e) {
                
                        accessory.Log.Error("SourcePosition deserialization failed.");

                        return;

                    }
            
                    int discretizedPosition=discretizePosition(sourcePosition,ARENA_CENTER,6);

                    if(discretizedPosition<0||discretizedPosition>5) {

                        return;

                    }

                    if(phase2Lindschrat[discretizedPosition]==Phase2TetherActions.UNKNOWN||phase2Lindschrat[discretizedPosition]==Phase2TetherActions.BOSS_COMBO) {

                        return;

                    }

                    phase2StagingActions[targetStaging].Add(phase2Lindschrat[discretizedPosition]);
                    
                    Interlocked.Increment(ref phase2StagingActionCount);

                }

                if(phase2StagingActionCount==7) {

                    phase2DisableGuidance=false;

                    if(phase2StagingActions[0].Count!=1||phase2StagingActions[0][0]!=Phase2TetherActions.BOSS_COMBO) {

                        phase2DisableGuidance=true;

                    }
                    
                    if(phase2StagingActions[1].Count!=1||phase2StagingActions[1][0]!=Phase2TetherActions.FAN) {

                        phase2DisableGuidance=true;

                    }
                    
                    if(phase2StagingActions[2].Count!=1||phase2StagingActions[2][0]!=Phase2TetherActions.STACK) {

                        phase2DisableGuidance=true;

                    }
                    
                    if(phase2StagingActions[3].Count!=1||phase2StagingActions[3][0]!=Phase2TetherActions.DEFAMATION) {

                        phase2DisableGuidance=true;

                    }
                    
                    if(phase2StagingActions[4].Count!=0) {

                        phase2DisableGuidance=true;

                    }
                    
                    if(phase2StagingActions[5].Count!=1||phase2StagingActions[5][0]!=Phase2TetherActions.DEFAMATION) {

                        phase2DisableGuidance=true;

                    }
                    
                    if(phase2StagingActions[6].Count!=1||phase2StagingActions[6][0]!=Phase2TetherActions.STACK) {

                        phase2DisableGuidance=true;

                    }
                    
                    if(phase2StagingActions[7].Count!=1||phase2StagingActions[7][0]!=Phase2TetherActions.FAN) {

                        phase2DisableGuidance=true;

                    }

                    phase2StagingActionSemaphore1.Set();
                    phase2StagingActionSemaphore2.Set();
                    phase2StagingActionSemaphore3.Set();

                    if(enableDebugLogging) {

                        string log=string.Empty;

                        for(int i=0;i<phase2StagingActions.Length;++i) {

                            log+=$"""
                                  phase2StagingActions[{i}].Count={phase2StagingActions[i].Count}
                                  phase2StagingActions[{i}]:{string.Join(",",phase2StagingActions[i].Select(p=>p.ToString()))}
                                  """;

                            log+="\n";

                        }

                        log+=$"phase2DisableGuidance={phase2DisableGuidance}";
                        
                        accessory.Log.Debug(log);

                    }

                }
                
            }
        
        }
        
        [ScriptMethod(name:"本体 模仿细胞 因接线处理错误禁用指路 (文字提示与TTS)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:0175"],
            suppress:1000)]
    
        public void 本体_模仿细胞_因接线处理错误禁用指路_文字提示与TTS(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null) {

                return;

            }

            if(sourceObject.DataId!=19204&&sourceObject.DataId!=19202) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            phase2StagingActionSemaphore3.WaitOne();

            if(phase2DisableGuidance) {

                if(enablePrompts) {
                    
                    accessory.Method.TextInfo("接线处理错误,指路已禁用。",2500,true);
                    
                }
                
                accessory.tts("接线处理错误,指路已禁用。",enableVanillaTts,enableDailyRoutinesTts);
                
            }

        }
        
        [ScriptMethod(name:"本体 模仿细胞 Boss连击与人形分身连击 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46307"])]

        public void 本体_模仿细胞_Boss连击与人形分身连击_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            phase2StagingActionSemaphore2.WaitOne();
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(int i=0;i<8;++i) {

                if(phase2StagingActions[i].Contains(Phase2TetherActions.BOSS_COMBO)) {

                    int currentIndex=Array.IndexOf(phase2PlayerStaging,i);

                    if(!isLegalPartyIndex(currentIndex)) {

                        continue;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(5);
                    currentProperties.Owner=accessory.Data.PartyList[currentIndex];
                    currentProperties.Color=colourOfFirefallSplash.V4.WithW(1);
                    currentProperties.DestoryAt=5875;
            
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                    for(uint j=1;j<=4;++j) {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(50);
                        currentProperties.Owner=accessory.Data.PartyList[currentIndex];
                        currentProperties.TargetResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                        currentProperties.TargetOrderIndex=j;
                        currentProperties.Radian=float.Pi/12;
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.DestoryAt=6500;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                        
                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(20);
                    currentProperties.Owner=accessory.Data.PartyList[currentIndex];
                    currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerFarestOrder;
                    currentProperties.CentreOrderIndex=1;
                    currentProperties.Color=colourOfManaBurst.V4.WithW(1);
                    currentProperties.DestoryAt=8000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                }
                
            }

            for(int i=0;i<8;++i) {

                if(phase2StagingActions[i].Contains(Phase2TetherActions.DEFAMATION)) {
                    
                    int currentIndex=Array.IndexOf(phase2PlayerStaging,i);
                    
                    if(!isLegalPartyIndex(currentIndex)) {

                        continue;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(20);
                    currentProperties.Owner=accessory.Data.PartyList[currentIndex];
                    currentProperties.Color=colourOfManaBurst.V4.WithW(1);
                    currentProperties.DestoryAt=8000;
            
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

            }
            
            for(int i=0;i<8;++i) {

                if(phase2StagingActions[i].Contains(Phase2TetherActions.STACK)) {
                    
                    int currentIndex=Array.IndexOf(phase2PlayerStaging,i);
                    
                    if(!isLegalPartyIndex(currentIndex)) {

                        continue;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(5);
                    currentProperties.Owner=accessory.Data.PartyList[currentIndex];
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=8000;
                    currentProperties.DestoryAt=5500;
                    
                    /*

                    if(phase2DisableGuidance) {
                        
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        
                    }

                    else {
                        
                        int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
                        if(!isLegalPartyIndex(myIndex)) {

                            currentProperties.Color=accessory.Data.DefaultDangerColor;

                        }

                        else {

                            int currentStaging=i;
                            int myStaging=phase2PlayerStaging[myIndex];
                            
                            currentProperties.Color=accessory.Data.DefaultDangerColor;

                            if((0<=currentStaging&&currentStaging<=3)
                               &&
                               (0<=myStaging&&myStaging<=3)) {
                                
                                currentProperties.Color=accessory.Data.DefaultSafeColor;
                                
                            }
                            
                            if((4<=currentStaging&&currentStaging<=7)
                               &&
                               (4<=myStaging&&myStaging<=7)) {
                                
                                currentProperties.Color=accessory.Data.DefaultSafeColor;
                                
                            }

                        }
                        
                    }
                    
                    // I abandoned the idea of introducing dynamic colours for stack,
                    // since I just realized users would be unable to turn it off when they turned off the guidance feature,
                    // and it wasn't worth a dedicated configuration option either.
                    
                    */
            
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

            }
            
            for(int i=0;i<8;++i) {

                if(phase2StagingActions[i].Contains(Phase2TetherActions.FAN)) {
                    
                    int currentIndex=Array.IndexOf(phase2PlayerStaging,i);
                    
                    if(!isLegalPartyIndex(currentIndex)) {

                        continue;

                    }
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(50);
                    currentProperties.Owner=accessory.Data.PartyList[currentIndex];
                    currentProperties.Radian=float.Pi/6;
                    currentProperties.Delay=8000;
                    currentProperties.DestoryAt=7750;

                    if(accessory.Data.PartyList[currentIndex]==accessory.Data.Me) {

                        currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);

                    }

                    else {
                        
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        
                    }
                    
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                    
                }

            }

        }
        
        [ScriptMethod(name:"本体 模仿细胞 Boss连击与人形分身连击 (指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46307"])]

        public void 本体_模仿细胞_Boss连击与人形分身连击_指路(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase2DisableGuidance) {

                return;

            }

            phase2StagingActionSemaphore1.WaitOne();
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            int myStaging=phase2PlayerStaging[myIndex];

            if(myStaging<0||myStaging>7) {

                return;

            }

            Vector3 myPosition=PHASE2_TETHER_POSITION[myStaging];
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=8000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            if(myStaging==2||myStaging==6) {

                myPosition=ARENA_CENTER;

                if(myStaging==2) {

                    myPosition=PHASE2_STAGING2_STACK_POSITION;

                }
                
                if(myStaging==6) {

                    myPosition=PHASE2_STAGING6_STACK_POSITION;

                }
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
            
                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=8000;
                currentProperties.DestoryAt=5500;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
            }

            else {

                int targetIndex=-1;

                if(0<=myStaging&&myStaging<=3) {

                    targetIndex=Array.IndexOf(phase2PlayerStaging,2);

                }
                
                if(4<=myStaging&&myStaging<=7) {
                    
                    targetIndex=Array.IndexOf(phase2PlayerStaging,6);
                    
                }

                if(!isLegalPartyIndex(targetIndex)) {

                    return;

                }
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
            
                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetObject=accessory.Data.PartyList[targetIndex];
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=8000;
                currentProperties.DestoryAt=5500;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            }
        
        }
        
        [ScriptMethod(name:"本体 模仿细胞 炎波 (数据收集)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:46309"],
            userControl:false)]

        public void 本体_模仿细胞_炎波_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(!isRecordingScaldingWavesInPhase2) {

                return;

            }

            if(!string.Equals(@event["SourceDataId"],"9020")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            lock(phase2ScaldingWavesPlayers) {
                
                phase2ScaldingWavesPlayers.Add(targetIndex);

                if(enableDebugLogging) {
                    
                    accessory.Log.Debug($"targetIndex={targetIndex}\n{targetIndex} was hit by Scalding Waves.");
                    
                }
                
            }
        
        }
        
        [ScriptMethod(name:"本体 模仿细胞 魔力爆发 (数据收集)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:46311"],
            userControl:false)]

        public void 本体_模仿细胞_魔力爆发_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(!isRecordingManaBurstInPhase2) {

                return;

            }

            if(!string.Equals(@event["SourceDataId"],"9020")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }
            
            int targetStaging=phase2PlayerStaging[targetIndex];

            if(targetStaging<0||targetStaging>7) {

                return;

            }

            lock(phase2StagingActions) {

                if(!phase2StagingActions[targetStaging].Contains(Phase2TetherActions.DEFAMATION)) {
                    
                    phase2StagingActions[targetStaging].Add(Phase2TetherActions.DEFAMATION);

                    if(enableDebugLogging) {

                        accessory.Log.Debug($"targetIndex={targetIndex}\ntargetStaging={targetStaging}\n{Phase2TetherActions.DEFAMATION.ToString()} was added to phase2StagingActions[{targetStaging}].");

                    }
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"本体 模仿细胞 炎波与魔力爆发 (数据收集控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46316"],
            userControl:false)]

        public void 本体_模仿细胞_炎波与魔力爆发_数据收集控制(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            isRecordingScaldingWavesInPhase2=false;
            isRecordingManaBurstInPhase2=false;

        }
        
        [ScriptMethod(name:"本体 模仿细胞 蛇踢 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46375"])]
    
        public void 本体_模仿细胞_蛇踢_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(50);
            currentProperties.Owner=sourceId;
            currentProperties.Radian=float.Pi;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=5000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 模仿细胞 近界阴怒与远界阴怒 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46382|46383)$"])]
    
        public void 本体_模仿细胞_近界阴怒与远界阴怒_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(6);
            currentProperties.Owner=sourceId;
            currentProperties.CentreOrderIndex=1;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=6250;

            if(string.Equals(@event["ActionId"],"46382")) {
                
                currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                
            }
            
            if(string.Equals(@event["ActionId"],"46383")) {
                
                currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerFarestOrder;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(6);
            currentProperties.Owner=sourceId;
            currentProperties.CentreOrderIndex=2;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=6250;
            
            if(string.Equals(@event["ActionId"],"46382")) {
                
                currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                
            }
            
            if(string.Equals(@event["ActionId"],"46383")) {
                
                currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerFarestOrder;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 模仿细胞 时空重现 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46316"])]
    
        public void 本体_模仿细胞_时空重现_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }

            Vector3 stagingUp=new Vector3(100,0,86),stagingDown=new Vector3(100,0,114);
            int delay=8300;
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            for(int i=0;i<phase2StagingActions[0].Count;++i) {

                switch(phase2StagingActions[0][i]) {

                    case Phase2TetherActions.DEFAMATION: {

                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(20);
                        currentProperties.Position=stagingUp;
                        currentProperties.Color=colourOfManaBurst.V4.WithW(1);
                        currentProperties.DestoryAt=delay+500;
                            
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                            
                        break;

                    }
                        
                    case Phase2TetherActions.FAN: {

                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(50);
                        currentProperties.Position=stagingUp;
                        currentProperties.TargetPosition=ARENA_CENTER;
                        currentProperties.Radian=float.Pi/6;
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.DestoryAt=delay+500;
                    
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

                        break;

                    }
                        
                    case Phase2TetherActions.STACK: {

                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(5);
                        currentProperties.Position=stagingUp;
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.DestoryAt=delay+500;
                            
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                            
                            
                        break;

                    }
                        
                    case Phase2TetherActions.BOSS_COMBO: {

                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(5);
                        currentProperties.Position=stagingUp;
                        currentProperties.Color=colourOfFirefallSplash.V4.WithW(1);
                        currentProperties.DestoryAt=delay-750;
                            
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                        for(int j=0;j<phase2ScaldingWavesPlayers.Count;++j) {

                            if(!isLegalPartyIndex(phase2ScaldingWavesPlayers[j])) {

                                continue;

                            }
                        
                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(50);
                            currentProperties.Position=stagingUp;
                            currentProperties.TargetObject=accessory.Data.PartyList[phase2ScaldingWavesPlayers[j]];
                            currentProperties.Radian=float.Pi/12;
                            currentProperties.Color=accessory.Data.DefaultDangerColor;
                            currentProperties.DestoryAt=delay+500;
        
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                        
                        }
                            
                        break;

                    }

                    default: {

                        break;

                    }
                        
                }
                    
            }
            
            for(int i=0;i<phase2StagingActions[4].Count;++i) {

                switch(phase2StagingActions[4][i]) {

                    case Phase2TetherActions.DEFAMATION: {

                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(20);
                        currentProperties.Position=stagingDown;
                        currentProperties.Color=colourOfManaBurst.V4.WithW(1);
                        currentProperties.DestoryAt=delay+500;
                            
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                            
                        break;

                    }
                        
                    case Phase2TetherActions.FAN: {

                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(50);
                        currentProperties.Position=stagingDown;
                        currentProperties.TargetPosition=ARENA_CENTER;
                        currentProperties.Radian=float.Pi/6;
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.DestoryAt=delay+500;
                    
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

                        break;

                    }
                        
                    case Phase2TetherActions.STACK: {

                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(5);
                        currentProperties.Position=stagingDown;
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.DestoryAt=delay+500;
                            
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                            
                            
                        break;

                    }
                        
                    case Phase2TetherActions.BOSS_COMBO: {

                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(5);
                        currentProperties.Position=stagingDown;
                        currentProperties.Color=colourOfFirefallSplash.V4.WithW(1);
                        currentProperties.DestoryAt=delay-750;
                            
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                        for(int j=0;j<phase2ScaldingWavesPlayers.Count;++j) {

                            if(!isLegalPartyIndex(phase2ScaldingWavesPlayers[j])) {

                                continue;

                            }
                        
                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(50);
                            currentProperties.Position=stagingDown;
                            currentProperties.TargetObject=accessory.Data.PartyList[phase2ScaldingWavesPlayers[j]];
                            currentProperties.Radian=float.Pi/12;
                            currentProperties.Color=accessory.Data.DefaultDangerColor;
                            currentProperties.DestoryAt=delay+500;
        
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                        
                        }
                            
                        break;

                    }

                    default: {

                        break;

                    }
                        
                }
                    
            }

            for(int round=0;round<4;++round) {

                for(int i=0;i<phase2StagingActions[round].Count;++i) {

                    switch(phase2StagingActions[round][i]) {

                        case Phase2TetherActions.DEFAMATION: {

                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(20);
                            currentProperties.Position=stagingUp;
                            currentProperties.Color=colourOfManaBurst.V4.WithW(1);
                            currentProperties.Delay=delay;
                            currentProperties.DestoryAt=4000;
                            
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                            
                            break;

                        }
                        
                        case Phase2TetherActions.FAN: {

                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(50);
                            currentProperties.Position=stagingUp;
                            currentProperties.TargetPosition=ARENA_CENTER;
                            currentProperties.Radian=float.Pi/6;
                            currentProperties.Color=accessory.Data.DefaultDangerColor;
                            currentProperties.Delay=delay;
                            currentProperties.DestoryAt=4550;
                    
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

                            break;

                        }
                        
                        case Phase2TetherActions.STACK: {

                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(5);
                            currentProperties.Position=stagingUp;
                            currentProperties.Color=accessory.Data.DefaultDangerColor;
                            currentProperties.Delay=delay;
                            currentProperties.DestoryAt=4250;
                            
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                            
                            
                            break;

                        }
                        
                        case Phase2TetherActions.BOSS_COMBO: {

                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(5);
                            currentProperties.Position=stagingUp;
                            currentProperties.Color=colourOfFirefallSplash.V4.WithW(1);
                            currentProperties.Delay=delay-1250;
                            currentProperties.DestoryAt=4000;
                            
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                            for(int j=0;j<phase2ScaldingWavesPlayers.Count;++j) {

                                if(!isLegalPartyIndex(phase2ScaldingWavesPlayers[j])) {

                                    continue;

                                }
                        
                                currentProperties=accessory.Data.GetDefaultDrawProperties();

                                currentProperties.Scale=new(50);
                                currentProperties.Position=stagingUp;
                                currentProperties.TargetObject=accessory.Data.PartyList[phase2ScaldingWavesPlayers[j]];
                                currentProperties.Radian=float.Pi/12;
                                currentProperties.Color=accessory.Data.DefaultDangerColor;
                                currentProperties.Delay=delay;
                                currentProperties.DestoryAt=4300;
        
                                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                        
                            }
                            
                            break;

                        }

                        default: {

                            break;

                        }
                        
                    }
                    
                }
                
                for(int i=0;i<phase2StagingActions[round+4].Count;++i) {
                    
                    switch(phase2StagingActions[round+4][i]) {

                        case Phase2TetherActions.DEFAMATION: {

                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(20);
                            currentProperties.Position=stagingDown;
                            currentProperties.Color=colourOfManaBurst.V4.WithW(1);
                            currentProperties.Delay=delay;
                            currentProperties.DestoryAt=4000;
                            
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                            break;

                        }
                        
                        case Phase2TetherActions.FAN: {

                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(50);
                            currentProperties.Position=stagingDown;
                            currentProperties.TargetPosition=ARENA_CENTER;
                            currentProperties.Radian=float.Pi/6;
                            currentProperties.Color=accessory.Data.DefaultDangerColor;
                            currentProperties.Delay=delay;
                            currentProperties.DestoryAt=4550;
                    
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

                            break;

                        }
                        
                        case Phase2TetherActions.STACK: {

                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(5);
                            currentProperties.Position=stagingDown;
                            currentProperties.Color=accessory.Data.DefaultDangerColor;
                            currentProperties.Delay=delay;
                            currentProperties.DestoryAt=4250;
                            
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                            
                            
                            break;

                        }
                        
                        case Phase2TetherActions.BOSS_COMBO: {

                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(5);
                            currentProperties.Position=stagingDown;
                            currentProperties.Color=colourOfFirefallSplash.V4.WithW(1);
                            currentProperties.Delay=delay-1250;
                            currentProperties.DestoryAt=4000;
                            
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                            for(int j=0;j<phase2ScaldingWavesPlayers.Count;++j) {

                                if(!isLegalPartyIndex(phase2ScaldingWavesPlayers[j])) {

                                    continue;

                                }
                        
                                currentProperties=accessory.Data.GetDefaultDrawProperties();

                                currentProperties.Scale=new(50);
                                currentProperties.Position=stagingDown;
                                currentProperties.TargetObject=accessory.Data.PartyList[phase2ScaldingWavesPlayers[j]];
                                currentProperties.Radian=float.Pi/12;
                                currentProperties.Color=accessory.Data.DefaultDangerColor;
                                currentProperties.Delay=delay;
                                currentProperties.DestoryAt=4300;
        
                                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                        
                            }
                            
                            break;

                        }
                        
                        default: {

                            break;

                        }
                        
                    }
                    
                }

                stagingUp=rotatePosition(stagingUp,ARENA_CENTER,Math.PI/4);
                stagingDown=rotatePosition(stagingDown,ARENA_CENTER,Math.PI/4);
                delay+=4000;

            }
            
        }
        
        [ScriptMethod(name:"本体 模仿细胞 时空重现 (指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46316"])]
    
        public void 本体_模仿细胞_时空重现_指路(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase2DisableGuidance) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            int myStaging=phase2PlayerStaging[myIndex];

            if(myStaging<0||myStaging>7) {

                return;

            }

            Vector3 myPosition=PHASE2_REENACTMENT_POSITION[myStaging];
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=12625;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            if(isMelee(myIndex)) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
            
                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=new Vector3(114,0,100);
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=12625;
                currentProperties.DestoryAt=8000;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
            }
            
            if(isRanged(myIndex)) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
            
                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=new Vector3(86,0,100);
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=12625;
                currentProperties.DestoryAt=4375;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
            
                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=new Vector3(86,0,100);
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=17000;
                currentProperties.DestoryAt=3625;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
            }
        
        }
        
        [ScriptMethod(name:"本体 变异细胞 (初始化与阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46341"],
            userControl:false)]
    
        public void 本体_变异细胞_初始化与阶段控制(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }
            
            phase3PartyCount=0;
            for(int i=0;i<isVulnerableInPhase3.Length;++i)isVulnerableInPhase3[i]=true;
            phase3LeftManaSphere.Clear();
            phase3RightManaSphere.Clear();
            isNaturalXOnLeftInPhase3=false;
            phase3UpperSphereToBeDelayed=null;
            phase3LowerSphereToBeDelayed=null;
            phase3GuidanceSemaphore.Reset();
            phase3DisableDrawings=false;

            Interlocked.Increment(ref currentPhase);

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"isInMajorPhase1={isInMajorPhase1}\ncurrentPhase={currentPhase}");
                
            }
        
        }
        
        [ScriptMethod(name:"本体 变异细胞 变异细胞 (数据收集)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:regex:^(4769|4771)$"],
            userControl:false)]
    
        public void 本体_变异细胞_变异细胞_数据收集(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }

            if(phase3PartyCount>=8) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));

            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            lock(isVulnerableInPhase3) {

                if(string.Equals(@event["StatusID"],"4769")) {

                    isVulnerableInPhase3[targetIndex]=true;

                }
                
                if(string.Equals(@event["StatusID"],"4771")) {

                    isVulnerableInPhase3[targetIndex]=false;

                }

                Interlocked.Increment(ref phase3PartyCount);

                if(phase3PartyCount==8) {

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"""
                                             isVulnerableInPhase3:{string.Join(",",isVulnerableInPhase3)}
                                             """);
                        
                    }
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"本体 变异细胞 魔力晶球 (数据收集)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:46333"],
            userControl:false)]
    
        public void 本体_变异细胞_魔力晶球_数据收集(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }

            if(phase3LeftManaSphere.Count+phase3RightManaSphere.Count>=8) {

                return;

            }

            if(!(new[]{"19206","19207","19208","19209"}.Contains(@event["SourceDataId"]))) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            bool isOnLeft=false;

            if(sourcePosition.X<100) {

                isOnLeft=true;

            }

            else {

                isOnLeft=false;

            }
            
            Vector3 effectPosition=ARENA_CENTER;

            try {

                effectPosition=JsonConvert.DeserializeObject<Vector3>(@event["EffectPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("EffectPosition deserialization failed.");

                return;

            }
            
            manaSphereType currentSphere=new manaSphereType();

            currentSphere.objectId=sourceId;
            currentSphere.position=effectPosition;
            currentSphere.dataId=@event["SourceDataId"];

            lock(phase3ManaSphereLock) {
                
                if(isOnLeft) {
                    
                    phase3LeftManaSphere.Add(currentSphere);
                    
                }

                else {
                    
                    phase3RightManaSphere.Add(currentSphere);
                    
                }

                if(phase3LeftManaSphere.Count+phase3RightManaSphere.Count==8) {

                    isNaturalXOnLeftInPhase3=true;

                    for(int i=0;i<phase3LeftManaSphere.Count;++i) {

                        if(Math.Abs(phase3LeftManaSphere[i].position.Z-100)<7) {

                            isNaturalXOnLeftInPhase3=false;

                            break;

                        }
                        
                    }

                    if(isNaturalXOnLeftInPhase3) {
                        
                        for(int i=0;i<phase3RightManaSphere.Count;++i) {

                            if(Math.Abs(phase3RightManaSphere[i].position.Z-100)<7) {

                                if(phase3RightManaSphere[i].position.Z<100) {

                                    phase3UpperSphereToBeDelayed=new manaSphereType(phase3RightManaSphere[i]);

                                }
                                
                                if(phase3RightManaSphere[i].position.Z>100) {

                                    phase3LowerSphereToBeDelayed=new manaSphereType(phase3RightManaSphere[i]);

                                }

                            }
                        
                        }

                        for(int i=0;i<phase3LeftManaSphere.Count;++i) {

                            if(phase3LeftManaSphere[i].dataId==phase3UpperSphereToBeDelayed.dataId) {

                                phase3UpperSphereToBeDelayed.objectId=phase3LeftManaSphere[i].objectId;
                                phase3UpperSphereToBeDelayed.position=phase3LeftManaSphere[i].position;

                            }
                            
                            if(phase3LeftManaSphere[i].dataId==phase3LowerSphereToBeDelayed.dataId) {

                                phase3LowerSphereToBeDelayed.objectId=phase3LeftManaSphere[i].objectId;
                                phase3LowerSphereToBeDelayed.position=phase3LeftManaSphere[i].position;

                            }
                            
                        }

                    }

                    else {
                        
                        for(int i=0;i<phase3LeftManaSphere.Count;++i) {

                            if(Math.Abs(phase3LeftManaSphere[i].position.Z-100)<7) {

                                if(phase3LeftManaSphere[i].position.Z<100) {

                                    phase3UpperSphereToBeDelayed=new manaSphereType(phase3LeftManaSphere[i]);

                                }
                                
                                if(phase3LeftManaSphere[i].position.Z>100) {

                                    phase3LowerSphereToBeDelayed=new manaSphereType(phase3LeftManaSphere[i]);

                                }

                            }
                            
                        }
                        
                        for(int i=0;i<phase3RightManaSphere.Count;++i) {

                            if(phase3RightManaSphere[i].dataId==phase3UpperSphereToBeDelayed.dataId) {

                                phase3UpperSphereToBeDelayed.objectId=phase3RightManaSphere[i].objectId;
                                phase3UpperSphereToBeDelayed.position=phase3RightManaSphere[i].position;

                            }
                            
                            if(phase3RightManaSphere[i].dataId==phase3LowerSphereToBeDelayed.dataId) {

                                phase3LowerSphereToBeDelayed.objectId=phase3RightManaSphere[i].objectId;
                                phase3LowerSphereToBeDelayed.position=phase3RightManaSphere[i].position;

                            }
                            
                        }
                        
                    }

                    if(phase3UpperSphereToBeDelayed.position.Z>phase3LowerSphereToBeDelayed.position.Z) {

                        (phase3UpperSphereToBeDelayed,phase3LowerSphereToBeDelayed)=(phase3LowerSphereToBeDelayed,phase3UpperSphereToBeDelayed);

                    }

                    phase3GuidanceSemaphore.Set();

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"""
                                             phase3LeftManaSphere.position:{string.Join(",",phase3LeftManaSphere.Select(p=>p.position))}
                                             phase3LeftManaSphere.dataId:{string.Join(",",phase3LeftManaSphere.Select(p=>p.dataId))}
                                             phase3RightManaSphere.position:{string.Join(",",phase3RightManaSphere.Select(p=>p.position))}
                                             phase3RightManaSphere.dataId:{string.Join(",",phase3RightManaSphere.Select(p=>p.dataId))}
                                             isNaturalXOnLeftInPhase3={isNaturalXOnLeftInPhase3}
                                             phase3UpperSphereToBeDelayed.position={phase3UpperSphereToBeDelayed.position}
                                             phase3UpperSphereToBeDelayed.dataId={phase3UpperSphereToBeDelayed.dataId}
                                             phase3LowerSphereToBeDelayed.position={phase3LowerSphereToBeDelayed.position}
                                             phase3LowerSphereToBeDelayed.dataId={phase3LowerSphereToBeDelayed.dataId}
                                             """);
                        
                    }
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"本体 变异细胞 魔力晶球 (指路与危险指示)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:46333"],
            suppress:1000)]
    
        public void 本体_变异细胞_魔力晶球_指路与危险指示(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }

            if(!(new[]{"19206","19207","19208","19209"}.Contains(@event["SourceDataId"]))) {

                return;

            }

            phase3GuidanceSemaphore.WaitOne();
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            ulong mySphere=0;

            if(isTank(myIndex)) {

                mySphere=phase3UpperSphereToBeDelayed.objectId;

            }
            
            if(isHealer(myIndex)) {

                if(tankStackSoloDuringMutatingCells) {
                    
                    mySphere=phase3LowerSphereToBeDelayed.objectId;
                    
                }

                else {
                    
                    mySphere=phase3UpperSphereToBeDelayed.objectId;
                    
                }

            }

            if(isDps(myIndex)) {
                
                mySphere=phase3LowerSphereToBeDelayed.objectId;
                
            }
            
            if(isVulnerableInPhase3[myIndex]) {

                mySphere=0;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(mySphere!=0) {

                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Name="本体_变异细胞_魔力晶球_指路";
                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetObject=mySphere;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=7200000;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            }

            for(int i=0;i<phase3LeftManaSphere.Count;++i) {

                ulong currentSphere=phase3LeftManaSphere[i].objectId;

                if(currentSphere!=mySphere) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Name=$"本体_变异细胞_魔力晶球_危险指示_范围_{currentSphere}";
                    currentProperties.Scale=new(2);
                    currentProperties.Owner=currentSphere;
                    currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=72000000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
            
                    currentProperties.Name=$"本体_变异细胞_魔力晶球_危险指示_箭头_{currentSphere}";
                    currentProperties.Scale=new(1,4);
                    currentProperties.Owner=currentSphere;
                    currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=72000000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                }

            }
            
            for(int i=0;i<phase3RightManaSphere.Count;++i) {

                ulong currentSphere=phase3RightManaSphere[i].objectId;

                if(currentSphere!=mySphere) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Name=$"本体_变异细胞_魔力晶球_危险指示_范围_{currentSphere}";
                    currentProperties.Scale=new(2);
                    currentProperties.Owner=currentSphere;
                    currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=72000000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
            
                    currentProperties.Name=$"本体_变异细胞_魔力晶球_危险指示_箭头_{currentSphere}";
                    currentProperties.Scale=new(1,4);
                    currentProperties.Owner=currentSphere;
                    currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=72000000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                    
                }

            }

        }
        
        [ScriptMethod(name:"本体 变异细胞 魔力晶球 (指路清除)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:46334"],
            userControl:false)]
    
        public void 本体_变异细胞_魔力晶球_指路清除(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            if(targetId!=accessory.Data.Me) {

                return;

            }

            accessory.Method.RemoveDraw("本体_变异细胞_魔力晶球_指路");
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }
            
            if(isVulnerableInPhase3[myIndex]) {

                return;

            }

            ulong mySphere=0;

            if(isTank(myIndex)) {

                mySphere=phase3UpperSphereToBeDelayed.objectId;

            }
            
            if(isHealer(myIndex)) {

                if(tankStackSoloDuringMutatingCells) {
                    
                    mySphere=phase3LowerSphereToBeDelayed.objectId;
                    
                }

                else {
                    
                    mySphere=phase3UpperSphereToBeDelayed.objectId;
                    
                }

            }

            if(isDps(myIndex)) {
                
                mySphere=phase3LowerSphereToBeDelayed.objectId;
                
            }

            if(mySphere==0) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Name=$"本体_变异细胞_魔力晶球_危险指示_范围_{mySphere}";
            currentProperties.Scale=new(2);
            currentProperties.Owner=mySphere;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=72000000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
            currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Name=$"本体_变异细胞_魔力晶球_危险指示_箭头_{mySphere}";
            currentProperties.Scale=new(1,4);
            currentProperties.Owner=mySphere;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=72000000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
        
        }
        
        [ScriptMethod(name:"本体 变异细胞 魔力晶球 (危险指示清除)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:46335"],
            userControl:false)]
    
        public void 本体_变异细胞_魔力晶球_危险指示清除(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }

            if(!(new[]{"19206","19207","19208","19209"}.Contains(@event["SourceDataId"]))) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }

            accessory.Method.RemoveDraw($"本体_变异细胞_魔力晶球_危险指示_范围_{sourceId}");
            accessory.Method.RemoveDraw($"本体_变异细胞_魔力晶球_危险指示_箭头_{sourceId}");
        
        }
        
        [ScriptMethod(name:"本体 变异细胞 魔力晶球 (犯错监测)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:46334"],
            userControl:false)]
    
        public void 本体_变异细胞_魔力晶球_犯错监测(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }
            
            Vector3 targetPosition=ARENA_CENTER;

            try {

                targetPosition=JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("TargetPosition deserialization failed.");

                return;

            }

            if(Vector3.Distance(targetPosition,phase3UpperSphereToBeDelayed.position)>5
               &&
               Vector3.Distance(targetPosition,phase3LowerSphereToBeDelayed.position)>5) {

                phase3DisableDrawings=true;

                if(enableDebugLogging) {
                    
                    accessory.Log.Debug($"targetPosition={targetPosition}\nphase3DisableDrawings={phase3DisableDrawings}");
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"本体 变异细胞 因分摊处理错误禁用绘制 (文字提示与TTS)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46336"])]
    
        public void 本体_变异细胞_因分摊处理错误禁用绘制_文字提示与TTS(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }

            if(!string.Equals(@event["TargetDataId"],"19202")) {

                return;

            }

            if(phase3DisableDrawings) {

                if(enablePrompts) {
                    
                    accessory.Method.TextInfo("分摊处理错误,绘制已禁用。",2500,true);
                    
                }
                
                accessory.tts("分摊处理错误,绘制已禁用。",enableVanillaTts,enableDailyRoutinesTts);
                
            }

        }
        
        [ScriptMethod(name:"本体 变异细胞 魔力球苏醒 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46336"])]
    
        public void 本体_变异细胞_魔力球苏醒_范围(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }

            if(!string.Equals(@event["TargetDataId"],"19202")) {

                return;

            }

            if(phase3DisableDrawings) {

                return;

            }

            for(int i=0;i<phase3LeftManaSphere.Count;++i) {

                if(isNaturalXOnLeftInPhase3) {

                    if(phase3LeftManaSphere[i].dataId!=phase3UpperSphereToBeDelayed.dataId
                       &&
                       phase3LeftManaSphere[i].dataId!=phase3LowerSphereToBeDelayed.dataId) {
                        
                        DrawInPhase3(accessory,phase3LeftManaSphere[i].dataId,true,1);
                        
                    }
                    
                    else {
                        
                        DrawInPhase3(accessory,phase3LeftManaSphere[i].dataId,true,2);
                        
                    }
                    
                }

                else {
                    
                    if(Math.Abs(phase3LeftManaSphere[i].position.Z-100)<7) {
                        
                        DrawInPhase3(accessory,phase3LeftManaSphere[i].dataId,true,1);
                        
                    }

                    else {
                        
                        DrawInPhase3(accessory,phase3LeftManaSphere[i].dataId,true,2);
                        
                    }
                    
                }

            }
            
            for(int i=0;i<phase3RightManaSphere.Count;++i) {

                if(!isNaturalXOnLeftInPhase3) {

                    if(phase3RightManaSphere[i].dataId!=phase3UpperSphereToBeDelayed.dataId
                       &&
                       phase3RightManaSphere[i].dataId!=phase3LowerSphereToBeDelayed.dataId) {
                        
                        DrawInPhase3(accessory,phase3RightManaSphere[i].dataId,false,1);
                        
                    }
                    
                    else {
                        
                        DrawInPhase3(accessory,phase3RightManaSphere[i].dataId,false,2);
                        
                    }
                    
                }

                else {
                    
                    if(Math.Abs(phase3RightManaSphere[i].position.Z-100)<7) {
                        
                        DrawInPhase3(accessory,phase3RightManaSphere[i].dataId,false,1);
                        
                    }

                    else {
                        
                        DrawInPhase3(accessory,phase3RightManaSphere[i].dataId,false,2);
                        
                    }
                    
                }

            }

        }

        public void DrawInPhase3(ScriptAccessory accessory,string dataId,bool isLeft,int round) {
            
            // 19206: Circle
            // 19207: Donut
            // 19208: Front and back
            // 19209: Left and right

            if(string.Equals(dataId,"19206")) {
                
                DrawCircleInPhase3(accessory,isLeft,round);
                
            }
            
            if(string.Equals(dataId,"19207")) {
                
                DrawDonutInPhase3(accessory,isLeft,round);
                
            }
            
            if(string.Equals(dataId,"19208")) {
                
                DrawFrontAndBackInPhase3(accessory,isLeft,round);
                
            }
            
            if(string.Equals(dataId,"19209")) {
                
                DrawLeftAndRightInPhase3(accessory,isLeft,round);
                
            }
            
        }

        private void DrawCircleInPhase3(ScriptAccessory accessory,bool isLeft,int round) {

            if(round!=1&&round!=2) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(8);
            currentProperties.Position=((isLeft)?(new Vector3(90,0,100)):new Vector3(110,0,100));
            currentProperties.Color=accessory.Data.DefaultDangerColor;

            if(round==1) {

                currentProperties.Delay=0;
                currentProperties.DestoryAt=4750;

            }

            if(round==2) {
                
                currentProperties.Delay=4750;
                currentProperties.DestoryAt=5000;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        private void DrawDonutInPhase3(ScriptAccessory accessory,bool isLeft,int round) {

            if(round!=1&&round!=2) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(60);
            currentProperties.InnerScale=new(5);
            currentProperties.Radian=float.Pi*2;
            currentProperties.Position=((isLeft)?(new Vector3(90,0,100)):new Vector3(110,0,100));
            currentProperties.Color=accessory.Data.DefaultDangerColor;

            if(round==1) {

                currentProperties.Delay=0;
                currentProperties.DestoryAt=4750;

            }

            if(round==2) {
                
                currentProperties.Delay=4750;
                currentProperties.DestoryAt=5000;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
            
        }
        
        private void DrawFrontAndBackInPhase3(ScriptAccessory accessory,bool isLeft,int round) {

            if(round!=1&&round!=2) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(40);
            currentProperties.Radian=float.Pi/3*2;
            currentProperties.Rotation=0;
            currentProperties.Position=((isLeft)?(new Vector3(90,0,100)):new Vector3(110,0,100));
            currentProperties.Color=accessory.Data.DefaultDangerColor;

            if(round==1) {

                currentProperties.Delay=0;
                currentProperties.DestoryAt=4750;

            }

            if(round==2) {
                
                currentProperties.Delay=4750;
                currentProperties.DestoryAt=5000;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(40);
            currentProperties.Radian=float.Pi/3*2;
            currentProperties.Rotation=float.Pi;
            currentProperties.Position=((isLeft)?(new Vector3(90,0,100)):new Vector3(110,0,100));
            currentProperties.Color=accessory.Data.DefaultDangerColor;

            if(round==1) {

                currentProperties.Delay=0;
                currentProperties.DestoryAt=4750;

            }

            if(round==2) {
                
                currentProperties.Delay=4750;
                currentProperties.DestoryAt=5000;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
        }
        
        private void DrawLeftAndRightInPhase3(ScriptAccessory accessory,bool isLeft,int round) {

            if(round!=1&&round!=2) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(40);
            currentProperties.Radian=float.Pi/3*2;
            currentProperties.Rotation=float.Pi/2;
            currentProperties.Position=((isLeft)?(new Vector3(90,0,100)):new Vector3(110,0,100));
            currentProperties.Color=accessory.Data.DefaultDangerColor;

            if(round==1) {

                currentProperties.Delay=0;
                currentProperties.DestoryAt=4750;

            }

            if(round==2) {
                
                currentProperties.Delay=4750;
                currentProperties.DestoryAt=5000;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(40);
            currentProperties.Radian=float.Pi/3*2;
            currentProperties.Rotation=-float.Pi/2;
            currentProperties.Position=((isLeft)?(new Vector3(90,0,100)):new Vector3(110,0,100));
            currentProperties.Color=accessory.Data.DefaultDangerColor;

            if(round==1) {

                currentProperties.Delay=0;
                currentProperties.DestoryAt=4750;

            }

            if(round==2) {
                
                currentProperties.Delay=4750;
                currentProperties.DestoryAt=5000;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 变异细胞 阴界近景与阴界远景 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46379|46380)$"])]
    
        public void 本体_变异细胞_阴界近景与阴界远景_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(6);
            currentProperties.Owner=sourceId;
            currentProperties.CentreOrderIndex=1;
            currentProperties.DestoryAt=6250;

            if(string.Equals(@event["ActionId"],"46379")) {
                
                currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                
            }
            
            if(string.Equals(@event["ActionId"],"46380")) {
                
                currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerFarestOrder;
                
            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                currentProperties.Color=accessory.Data.DefaultDangerColor;

            }

            else {

                if(isVulnerableInPhase3[myIndex]) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    
                }

                else {
                    
                    currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                    
                }
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 变异细胞 阴界近景与阴界远景 (安全区指示)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46379|46380)$"])]
    
        public void 本体_变异细胞_阴界近景与阴界远景_安全区指示(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }

            bool isStackNearest=false;

            if(string.Equals(@event["ActionId"],"46379")) {
                
                isStackNearest=true;

            }
            
            if(string.Equals(@event["ActionId"],"46380")) {
                
                isStackNearest=false;
                
            }

            bool shouldStack=false;
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            else {

                if(isVulnerableInPhase3[myIndex]) {
                    
                    shouldStack=true;

                }

                else {
                    
                    shouldStack=false;
                    
                }
                
            }

            bool shouldGetIn=false;

            if(isStackNearest) {

                if(shouldStack) {

                    shouldGetIn=true;

                }

                else {

                    shouldGetIn=false;

                }
                    
            }

            else {
                
                if(shouldStack) {

                    shouldGetIn=false;

                }

                else {

                    shouldGetIn=true;

                }
                
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(7);
            currentProperties.Owner=sourceId;
            currentProperties.Color=((shouldGetIn)?(accessory.Data.DefaultSafeColor):(accessory.Data.DefaultDangerColor));
            currentProperties.DestoryAt=6250;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(40);
            currentProperties.InnerScale=new(7);
            currentProperties.Radian=float.Pi*2;
            currentProperties.Owner=sourceId;
            currentProperties.Color=((shouldGetIn)?(accessory.Data.DefaultDangerColor):(accessory.Data.DefaultSafeColor));
            currentProperties.DestoryAt=6250;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);

        }
        
        [ScriptMethod(name:"本体 变异细胞 双重飞踢 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46368|46373)$"])]

        public void 本体_变异细胞_双重飞踢_范围(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {

                return;

            }

            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(40);
            currentProperties.Owner=sourceId;
            currentProperties.Radian=float.Pi;

            if(string.Equals(@event["ActionId"],"46368")) {

                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=5500;

            }
            
            if(string.Equals(@event["ActionId"],"46373")) {

                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=4500;
                
            }
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 变异细胞 魔力连击 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46368"])]
    
        public void 本体_变异细胞_魔力连击_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(10);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.OwnerEnmityOrder;
            currentProperties.CentreOrderIndex=1;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=10125;
            currentProperties.DestoryAt=2500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(10);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.OwnerEnmityOrder;
            currentProperties.CentreOrderIndex=2;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=10125;
            currentProperties.DestoryAt=2500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 境中奇梦 (初始化与阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46345"],
            userControl:false)]
    
        public void 本体_境中奇梦_初始化与阶段控制(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }
            
            if(enablePartyAssistance) {

                accessory.Method.MarkClear();
                
            }
            
            phase4PlayerStagingCount=0;
            for(int i=0;i<phase4PlayerStaging.Length;++i)phase4PlayerStaging[i]=-1;
            isCardinalFirstInPhase4=null;
            phase4TwistedVisionCount=0;
            phase4TwistedVision3Semaphore1.Reset();
            phase4TwistedVision3Semaphore2.Reset();
            phase4LindschratCombo.Clear();
            phase4LindschratCount=0;
            for(int i=0;i<isLindschratDefamationInPhase4.Length;++i)isLindschratDefamationInPhase4[i]=null;
            phase4LindschratSemaphore1.Reset();
            phase4LindschratSemaphore2.Reset();
            phase4StagingActionCount=0;
            for(int i=0;i<isStagingDefamationInPhase4.Length;++i)isStagingDefamationInPhase4[i]=false;
            for(int i=0;i<phase4PlayerOrder.Length;++i)phase4PlayerOrder[i]=-1;
            phase4DisableGuidance=false;
            phase4StagingActionSemaphore.Reset();
            phase4TowerCount=0;
            for(int i=0;i<phase4Tower.Length;++i)phase4Tower[i]=Phase4Towers.UNKNOWN;
            phase4HitCount=0;
            for(int i=0;i<wasHitInPhase4.Length;++i)wasHitInPhase4[i]=false;
            for(int i=0;i<swapsWithPartnerInPhase4.Length;++i)swapsWithPartnerInPhase4[i]=false;
            phase4TowerPreviewSemaphore.Reset();
            phase4TwistedVision4Semaphore1.Reset();
            phase4TwistedVision4Semaphore2.Reset();
            phase4TwistedVision4Semaphore3.Reset();
            phase4TwistedVision4Semaphore4.Reset();
            phase4TwistedVision5Semaphore1.Reset();
            phase4TwistedVision5Semaphore2.Reset();
            phase4TwistedVision5Semaphore3.Reset();
            phase4HiddenLindschrat=0;
            phase4TwistedVision6Semaphore1.Reset();
            phase4TwistedVision6Semaphore2.Reset();
            phase4TwistedVision7Semaphore1.Reset();
            phase4TwistedVision7Semaphore2.Reset();
            phase4TwistedVision8Semaphore1.Reset();
            phase4TwistedVision8Semaphore2.Reset();
            phase4TwistedVision8Semaphore3.Reset();
            
            Interlocked.Increment(ref currentPhase);

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"isInMajorPhase1={isInMajorPhase1}\ncurrentPhase={currentPhase}");
                
            }
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 场地指北针",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46345"])]
    
        public void 本体_境中奇梦_场地指北针(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if((!(3<=currentPhase&&currentPhase<=4))&&!skipPhaseChecks) {

                return;

            }

            if(phase4TwistedVisionCount!=0) {

                return;

            }

            if(!northIndicatorDuringIdyllicDream) {

                return;

            }
            
            System.Threading.Thread.Sleep(1000);
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Name="本体_境中奇梦_场地指北针";
            currentProperties.Scale=new(2,14);
            currentProperties.Position=new Vector3(100,0,107);
            currentProperties.TargetPosition=new Vector3(100,0,93);
            currentProperties.Color=colourOfNorthIndicator.V4.WithW(1);
            currentProperties.DestoryAt=72000000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 场地指北针 (清除)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46345"],
            userControl:false)]
    
        public void 本体_境中奇梦_场地指北针_清除(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            if(phase4TwistedVisionCount<8) {

                return;

            }
            
            System.Threading.Thread.Sleep(5000);
            
            accessory.Method.RemoveDraw("本体_境中奇梦_场地指北针");
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 模仿细胞 (顺序收集)",
            eventType:EventTypeEnum.PlayActionTimeline,
            eventCondition:["SourceDataId:19210"],
            userControl:false)]
    
        public void 本体_境中奇梦_模仿细胞_顺序收集(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            int discretizedPosition=discretizePosition(sourcePosition,ARENA_CENTER,8);

            if(discretizedPosition<0||discretizedPosition>7) {

                return;

            }

            lock(isCardinalFirstInPhase4Lock) {
                
                if(isCardinalFirstInPhase4!=null) {

                    return;

                }

                else {

                    if(discretizedPosition%2==0) {

                        isCardinalFirstInPhase4=true;

                    }

                    else {
                        
                        isCardinalFirstInPhase4=false;
                        
                    }

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"isCardinalFirstInPhase4={isCardinalFirstInPhase4}");
                        
                    }
                    
                }
                
            }
            
        }
        
        [ScriptMethod(name:"本体 境中奇梦 模仿细胞 (玩家模仿收集)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:0175"],
            userControl:false)]
    
        public void 本体_境中奇梦_模仿细胞_玩家模仿收集(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            if(phase4PlayerStagingCount>=8) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null) {

                return;

            }

            if(sourceObject.DataId!=19210) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            int discretizedPosition=discretizePosition(sourcePosition,ARENA_CENTER,8);

            if(discretizedPosition<0||discretizedPosition>7) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            lock(phase4PlayerStaging) {

                phase4PlayerStaging[targetIndex]=discretizedPosition;

                Interlocked.Increment(ref phase4PlayerStagingCount);

                if(phase4PlayerStagingCount==8) {

                    if(enableDebugLogging) {

                        accessory.Log.Debug($"""
                                             phase4PlayerStaging:{string.Join(",",phase4PlayerStaging)}
                                             """);
                    }
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 模仿细胞 (分身指示)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:0175"])]
    
        public void 本体_境中奇梦_模仿细胞_分身指示(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null) {

                return;

            }

            if(sourceObject.DataId!=19210) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            if(targetId!=accessory.Data.Me) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=sourcePosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.DestoryAt=45750;
                
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影 (轮次控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48098"],
            userControl:false)]
    
        public void 本体_境中奇梦_心象投影_轮次控制(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(phase4TwistedVisionCount>=8) {

                return;

            }
            
            Interlocked.Increment(ref phase4TwistedVisionCount);

            if(phase4TwistedVisionCount==3) {

                phase4TwistedVision3Semaphore1.Set();
                phase4TwistedVision3Semaphore2.Set();

            }

            if(phase4TwistedVisionCount==4) {

                phase4TwistedVision4Semaphore1.Set();
                phase4TwistedVision4Semaphore2.Set();
                phase4TwistedVision4Semaphore3.Set();
                phase4TwistedVision4Semaphore4.Set();

            }

            if(phase4TwistedVisionCount==5) {
                
                phase4TwistedVision5Semaphore1.Set();
                phase4TwistedVision5Semaphore2.Set();
                phase4TwistedVision5Semaphore3.Set();

            }
            
            if(phase4TwistedVisionCount==6) {
                
                phase4TwistedVision6Semaphore1.Set();
                phase4TwistedVision6Semaphore2.Set();
                
            }
            
            if(phase4TwistedVisionCount==7) {
                
                phase4TwistedVision7Semaphore1.Set();
                phase4TwistedVision7Semaphore2.Set();
                
            }
            
            if(phase4TwistedVisionCount==8) {
                
                phase4TwistedVision8Semaphore1.Set();
                phase4TwistedVision8Semaphore2.Set();
                phase4TwistedVision8Semaphore3.Set();

            }
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug($"phase4TwistedVisionCount={phase4TwistedVisionCount}");
                
            }
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影1 人形分身 (数据收集)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46351|46352|46353)$"],
            userControl:false)]
    
        public void 本体_境中奇梦_心象投影1_人形分身_数据收集(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!(1<=phase4TwistedVisionCount&&phase4TwistedVisionCount<3)) {

                return;

            }

            if(!string.Equals(@event["SourceDataId"],"19204")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }

            lock(phase4LindschratCombo) {
                
                phase4LindschratCombo.Add(new KeyValuePair<ulong,string>(sourceId,@event["ActionId"]));
                
                if(enableDebugLogging) {
                
                    accessory.Log.Debug($"sourceId(key)={sourceId}\n@event[\"ActionId\"](value)={@event["ActionId"]}");
                
                }
                
            }
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影2 人形分身 (数据收集)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:regex:^(0171|0170)$"],
            userControl:false)]
    
        public void 本体_境中奇梦_心象投影2_人形分身_数据收集(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!(2<=phase4TwistedVisionCount&&phase4TwistedVisionCount<4)) {

                return;

            }
            
            if(phase4LindschratCount>=8) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null) {

                return;

            }

            if(sourceObject.DataId!=19204) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            int discretizedPosition=discretizePosition(sourcePosition,ARENA_CENTER,8);

            if(discretizedPosition<0||discretizedPosition>7) {

                return;

            }

            lock(isLindschratDefamationInPhase4) {
                
                if(isLindschratDefamationInPhase4[discretizedPosition]!=null) {

                    return;

                }
                
                // 0171: Stack
                // 0170: Defamation
                
                if(string.Equals(@event["Id"],"0171")) {

                    isLindschratDefamationInPhase4[discretizedPosition]=false;

                }
                
                if(string.Equals(@event["Id"],"0170")) {

                    isLindschratDefamationInPhase4[discretizedPosition]=true;

                }

                Interlocked.Increment(ref phase4LindschratCount);

                if(phase4LindschratCount==8) {

                    phase4LindschratSemaphore1.Set();
                    phase4LindschratSemaphore2.Set();

                    if(enableDebugLogging) {

                        accessory.Log.Debug($"""
                                             isLindschratDefamationInPhase4:{string.Join(",",isLindschratDefamationInPhase4)}
                                             """);
                        
                    }
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影2 人形分身 (接线指路)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:regex:^(0171|0170)$"],
            suppress:10000)]
    
        public void 本体_境中奇梦_心象投影2_人形分身_接线指路(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!(2<=phase4TwistedVisionCount&&phase4TwistedVisionCount<4)) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null) {

                return;

            }

            if(sourceObject.DataId!=19204) {

                return;

            }

            phase4LindschratSemaphore1.WaitOne();
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            int myStaging=phase4PlayerStaging[myIndex];

            if(myStaging<0||myStaging>7) {

                return;

            }

            if(isLindschratDefamationInPhase4[myStaging]==null) {

                return;

            }
            
            int myDiscretizedPosition=-1;
            
            if(new[]{1,3,4,6}.Contains(myStaging)) {

                if((bool)isLindschratDefamationInPhase4[myStaging]) {

                    myDiscretizedPosition=myStaging;

                }

                else {

                    myDiscretizedPosition=myStaging switch {
                        
                        1 => 0,
                        3 => 2,
                        4 => 5,
                        6 => 7,
                        _ => -1
                        
                    };

                }

            }
            
            else {

                if(!((bool)isLindschratDefamationInPhase4[myStaging])) {

                    myDiscretizedPosition=myStaging;

                }

                else {

                    myDiscretizedPosition=myStaging switch {
                        
                        0 => 1,
                        2 => 3,
                        5 => 4,
                        7 => 6,
                        _ => -1
                        
                    };

                }

            }

            if(myDiscretizedPosition==-1) {

                return;

            }
            
            Vector3 myPosition=rotatePosition(new Vector3(100,0,90),ARENA_CENTER,Math.PI/4*myDiscretizedPosition);
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

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
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影2 人形分身 (接线小队指挥)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:regex:^(0171|0170)$"],
            suppress:10000)]
    
        public void 本体_境中奇梦_心象投影2_人形分身_接线小队指挥(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!(2<=phase4TwistedVisionCount&&phase4TwistedVisionCount<4)) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null) {

                return;

            }

            if(sourceObject.DataId!=19204) {

                return;

            }

            phase4LindschratSemaphore2.WaitOne();

            if(!enablePartyAssistance) {

                return;

            }

            bool[] isExpectedToBeDefamation=[false,true,false,true,true,false,true,false];
            int[] partyInOrder=[-1,-1,-1,-1,-1,-1,-1,-1];

            for(int i=0;i<8;i+=2) {

                int currentOwner=Array.IndexOf(phase4PlayerStaging,i);

                if(!isLegalPartyIndex(currentOwner)) {

                    continue;

                }
                
                int currentPartner=Array.IndexOf(phase4PlayerStaging,i+1);

                if(!isLegalPartyIndex(currentPartner)) {

                    continue;

                }

                if(isLindschratDefamationInPhase4[currentOwner]==null
                   ||
                   isLindschratDefamationInPhase4[currentPartner]==null) {

                    continue;

                }

                if(((bool)isLindschratDefamationInPhase4[i])==isExpectedToBeDefamation[i]) {

                    partyInOrder[i]=currentOwner;
                    partyInOrder[i+1]=currentPartner;

                }

                else {
                    
                    partyInOrder[i]=currentPartner;
                    partyInOrder[i+1]=currentOwner;
                    
                }

            }
            
            SendTetherInformation(accessory,partyInOrder);

        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影2 模仿细胞 (技能记录)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:0175"],
            userControl:false)]
    
        public void 本体_境中奇梦_心象投影2_模仿细胞_技能记录(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!(2<=phase4TwistedVisionCount&&phase4TwistedVisionCount<4)) {

                return;

            }

            if(phase4StagingActionCount>=8) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null) {

                return;

            }

            if(sourceObject.DataId!=19204) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            int targetStaging=phase4PlayerStaging[targetIndex];

            if(targetStaging<0||targetStaging>7) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            int discretizedPosition=discretizePosition(sourcePosition,ARENA_CENTER,8);

            if(discretizedPosition<0||discretizedPosition>7) {

                return;

            }
            
            if(isLindschratDefamationInPhase4[discretizedPosition]==null) {

                return;

            }

            lock(isStagingDefamationInPhase4) {

                isStagingDefamationInPhase4[targetStaging]=((bool)isLindschratDefamationInPhase4[discretizedPosition]);
                
                Interlocked.Increment(ref phase4StagingActionCount);

                if(phase4StagingActionCount==8) {
                    
                    phase4DisableGuidance=false;

                    if(!isStagingDefamationInPhase4[1]) {

                        phase4DisableGuidance=true;

                    }
                    
                    if(!isStagingDefamationInPhase4[3]) {

                        phase4DisableGuidance=true;

                    }
                    
                    if(!isStagingDefamationInPhase4[4]) {

                        phase4DisableGuidance=true;

                    }
                    
                    if(!isStagingDefamationInPhase4[6]) {

                        phase4DisableGuidance=true;

                    }
                    
                    if(isStagingDefamationInPhase4[0]) {

                        phase4DisableGuidance=true;

                    }
                    
                    if(isStagingDefamationInPhase4[2]) {

                        phase4DisableGuidance=true;

                    }
                    
                    if(isStagingDefamationInPhase4[5]) {

                        phase4DisableGuidance=true;

                    }
                    
                    if(isStagingDefamationInPhase4[7]) {

                        phase4DisableGuidance=true;

                    }

                    if(!phase4DisableGuidance) {

                        for(int i=0;i<phase4PlayerOrder.Length;i+=2) {
                            
                            int cardinalPlayer=Array.IndexOf(phase4PlayerStaging,i);

                            if(!isLegalPartyIndex(cardinalPlayer)) {

                                continue;

                            }
                                
                            int intercardinalPlayer=Array.IndexOf(phase4PlayerStaging,i+1);

                            if(!isLegalPartyIndex(intercardinalPlayer)) {

                                continue;

                            }

                            if(((bool)isLindschratDefamationInPhase4[i])!=isStagingDefamationInPhase4[i]) {

                                phase4PlayerOrder[cardinalPlayer]=i+1;
                                phase4PlayerOrder[intercardinalPlayer]=i;

                            }

                            else {
                                
                                phase4PlayerOrder[cardinalPlayer]=i;
                                phase4PlayerOrder[intercardinalPlayer]=i+1;
                                
                            }
                            
                        }
                        
                    }

                    phase4StagingActionSemaphore.Set();

                    if(enableDebugLogging) {

                        accessory.Log.Debug($"""
                                             isStagingDefamationInPhase4:{string.Join(",",isStagingDefamationInPhase4)}
                                             phase4DisableGuidance={phase4DisableGuidance}
                                             phase4PlayerOrder:{string.Join(",",phase4PlayerOrder)}
                                             """);

                    }

                }
                
            }
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影2 因接线处理错误禁用指路 (文字提示与TTS)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:0175"],
            suppress:1000)]
    
        public void 本体_境中奇梦_心象投影2_因接线处理错误禁用指路_文字提示与TTS(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!(2<=phase4TwistedVisionCount&&phase4TwistedVisionCount<4)) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null) {

                return;

            }

            if(sourceObject.DataId!=19204) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            phase4StagingActionSemaphore.WaitOne();

            if(phase4DisableGuidance) {

                if(enablePrompts) {
                    
                    accessory.Method.TextInfo("接线处理错误,指路已禁用。",2500,true);
                    
                }
                
                accessory.tts("接线处理错误,指路已禁用。",enableVanillaTts,enableDailyRoutinesTts);
                
            }

        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影3 人形分身的连击 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48098"])]
    
        public void 本体_境中奇梦_心象投影3_人形分身的连击_范围(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            bool isTheRound=phase4TwistedVision3Semaphore1.WaitOne(4000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=3) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            // 46351: Bode, left and right
            // 46352: Bode, front and back
            // 46353: Bode, circle
            // ID +4: Corresponding actual action

            for(int i=0;i<phase4LindschratCombo.Count;++i) {

                KeyValuePair<ulong,string> currentLindschrat=phase4LindschratCombo[i];

                if(string.Equals(currentLindschrat.Value,"46351")) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(60);
                    currentProperties.Radian=float.Pi/2;
                    currentProperties.Rotation=float.Pi/2;
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=8625;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(60);
                    currentProperties.Radian=float.Pi/2;
                    currentProperties.Rotation=-float.Pi/2;
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=8625;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                    
                }
                
                if(string.Equals(currentLindschrat.Value,"46352")) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(60);
                    currentProperties.Radian=float.Pi/2;
                    currentProperties.Rotation=0;
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=8625;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(60);
                    currentProperties.Radian=float.Pi/2;
                    currentProperties.Rotation=float.Pi;
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=8625;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                    
                }
                
                if(string.Equals(currentLindschrat.Value,"46353")) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(10);
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=8625;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影3 塔预站位 (指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48098"])]
    
        public void 本体_境中奇梦_心象投影3_塔预站位_指路(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            bool isTheRound=phase4TwistedVision3Semaphore2.WaitOne(4000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=3) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            Vector3 myPosition1=((isInLeftGroup(myIndex))?(PHASE4_LEFT_ARENA_CENTER):(PHASE4_RIGHT_ARENA_CENTER));
            Vector3 myPosition2=PHASE4_STANDBY_POSITION[myIndex];
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition1;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=4000;
            currentProperties.DestoryAt=4625;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(10);
            currentProperties.Position=myPosition1;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=4000;
            currentProperties.DestoryAt=4625;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition2;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=8625;
            currentProperties.DestoryAt=17500;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影3 境中奇奥 (范围)",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:regex:^(2015013|2015014|2015015|2015016)$"],
            suppress:1000)]
    
        public void 本体_境中奇梦_心象投影3_境中奇奥_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(phase4TwistedVisionCount!=3) {

                return;

            }
            
            if(!string.Equals(@event["Operate"],"Add")) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(int i=0;i<8;++i) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(6);
                currentProperties.Owner=accessory.Data.PartyList[i];
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=10125;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
            }
            
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影3 塔 (数据收集)",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:regex:^(2015013|2015014|2015015|2015016)$"],
            userControl:false)]
    
        public void 本体_境中奇梦_心象投影3_塔_数据收集(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(phase4TwistedVisionCount!=3) {

                return;

            }

            if(!string.Equals(@event["Operate"],"Add")) {

                return;

            }
            
            if(phase4TowerCount>=8) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            int towerOwner=-1;

            for(int i=0;i<PHASE4_DEFAULT_TOWER.Count;++i) {

                if(Vector3.Distance(PHASE4_DEFAULT_TOWER[i],sourcePosition)<0.05) {

                    towerOwner=i;

                    break;

                }
                
            }

            if(!isLegalPartyIndex(towerOwner)) {

                return;

            }

            lock(phase4Tower) {
                
                // 2015013: Wind the trilogy
                // 2015014: Darkness the trilogy
                // 2015015: Earth
                // 2015016: Fire
                
                if(string.Equals(@event["DataId"],"2015013")) {

                    phase4Tower[towerOwner]=Phase4Towers.WIND_TRILOGY;

                }
                
                if(string.Equals(@event["DataId"],"2015014")) {

                    phase4Tower[towerOwner]=Phase4Towers.DARKNESS_TRILOGY;

                }
                
                if(string.Equals(@event["DataId"],"2015015")) {

                    phase4Tower[towerOwner]=Phase4Towers.EARTH;

                }
                
                if(string.Equals(@event["DataId"],"2015016")) {

                    phase4Tower[towerOwner]=Phase4Towers.FIRE;

                }

                if(phase4Tower[towerOwner]==Phase4Towers.UNKNOWN) {

                    return;

                }

                Interlocked.Increment(ref phase4TowerCount);

                if(phase4TowerCount==8) {

                    if(enableDebugLogging) {

                        accessory.Log.Debug($"""
                                             phase4Tower:{string.Join(",",phase4Tower.Select(p=>p.ToString()))}
                                             """);
                        
                    }
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影3 境中奇奥 (数据收集)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:47577"],
            userControl:false)]
    
        public void 本体_境中奇梦_心象投影3_境中奇奥_数据收集(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!(3<=phase4TwistedVisionCount&&phase4TwistedVisionCount<5)) {

                return;

            }
            
            if(phase4HitCount>=4) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            lock(wasHitInPhase4) {

                wasHitInPhase4[targetIndex]=true;

                Interlocked.Increment(ref phase4HitCount);

                if(phase4HitCount==4) {

                    for(int i=0;i<4;++i) {

                        if((wasHitInPhase4[i]&&(phase4Tower[i]==Phase4Towers.WIND_TRILOGY||phase4Tower[i]==Phase4Towers.DARKNESS_TRILOGY))
                           ||
                           (!wasHitInPhase4[i]&&(phase4Tower[i]==Phase4Towers.EARTH||phase4Tower[i]==Phase4Towers.FIRE))) {

                            swapsWithPartnerInPhase4[i]=true;
                            swapsWithPartnerInPhase4[i+4]=true;

                        }
                        
                    }

                    phase4TowerPreviewSemaphore.Set();

                    if(enableDebugLogging) {

                        accessory.Log.Debug($"""
                                             wasHitInPhase4:{string.Join(",",wasHitInPhase4)}
                                             swapsWithPartnerInPhase4:{string.Join(",",swapsWithPartnerInPhase4)}
                                             """);
                        
                    }
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影3 境中奇奥 (塔指示)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:47577"],
            suppress:1000)]
    
        public void 本体_境中奇梦_心象投影3_境中奇奥_塔指示(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!(3<=phase4TwistedVisionCount&&phase4TwistedVisionCount<5)) {

                return;

            }

            phase4TowerPreviewSemaphore.WaitOne();
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            int partnersIndex=-1;

            if(isSupporter(myIndex)) {

                partnersIndex=myIndex+4;

            }

            if(isDps(myIndex)) {

                partnersIndex=myIndex-4;

            }

            if(!isLegalPartyIndex(partnersIndex)) {

                return;

            }
            
            Vector3 myTower=ARENA_CENTER;
            Vector3 partnersTower=ARENA_CENTER;

            if(swapsWithPartnerInPhase4[myIndex]) {
                
                myTower=PHASE4_DEFAULT_TOWER[partnersIndex];
                partnersTower=PHASE4_DEFAULT_TOWER[myIndex];
                
            }

            else {

                myTower=PHASE4_DEFAULT_TOWER[myIndex];
                partnersTower=PHASE4_DEFAULT_TOWER[partnersIndex];

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(3);
            currentProperties.Position=myTower;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.DestoryAt=6750;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(3);
            currentProperties.Position=partnersTower;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=6750;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影4 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48098"])]
    
        public void 本体_境中奇梦_心象投影4_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            bool isTheRound=phase4TwistedVision4Semaphore2.WaitOne(4000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=4) {

                return;

            }

            int delay=5500;
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(int round=0;round<4;++round) {

                int upperPlayer=Array.IndexOf(phase4PlayerOrder,round);

                if(!isLegalPartyIndex(upperPlayer)) {

                    continue;

                }
                
                int lowerPlayer=Array.IndexOf(phase4PlayerOrder,round+4);
                
                if(!isLegalPartyIndex(upperPlayer)) {

                    continue;

                }
                
                bool isUpperPlayerDefamation=isStagingDefamationInPhase4[phase4PlayerStaging[upperPlayer]];
                bool isLowerPlayerDefamation=isStagingDefamationInPhase4[phase4PlayerStaging[lowerPlayer]];

                if(isUpperPlayerDefamation) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(20);
                    currentProperties.Owner=accessory.Data.PartyList[upperPlayer];
                    currentProperties.Color=colourOfManaBurst.V4.WithW(1);
                    currentProperties.Delay=delay;
                    currentProperties.DestoryAt=6250;

                    if(round==0) {

                        currentProperties.Delay=0;
                        currentProperties.DestoryAt+=delay;

                    }
                            
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

                else {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(5);
                    currentProperties.Owner=accessory.Data.PartyList[upperPlayer];
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=delay;
                    currentProperties.DestoryAt=5000;

                    if(round==0) {

                        currentProperties.Delay=0;
                        currentProperties.DestoryAt+=delay;

                    }
                            
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
                if(isLowerPlayerDefamation) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(20);
                    currentProperties.Owner=accessory.Data.PartyList[lowerPlayer];
                    currentProperties.Color=colourOfManaBurst.V4.WithW(1);
                    currentProperties.Delay=delay;
                    currentProperties.DestoryAt=6250;

                    if(round==0) {

                        currentProperties.Delay=0;
                        currentProperties.DestoryAt+=delay;

                    }
                            
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

                else {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(5);
                    currentProperties.Owner=accessory.Data.PartyList[lowerPlayer];
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=delay;
                    currentProperties.DestoryAt=5000;

                    if(round==0) {

                        currentProperties.Delay=0;
                        currentProperties.DestoryAt+=delay;

                    }
                            
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
                delay+=5000;

            }
            
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影4 (指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48098"])]
    
        public void 本体_境中奇梦_心象投影4_指路(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            if(phase4DisableGuidance) {

                return;

            }

            bool isTheRound=phase4TwistedVision4Semaphore1.WaitOne(4000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=4) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            bool isInA1B2Group=false;

            if(0<=phase4PlayerOrder[myIndex]&&phase4PlayerOrder[myIndex]<=3) {

                isInA1B2Group=true;

            }
            
            if(4<=phase4PlayerOrder[myIndex]&&phase4PlayerOrder[myIndex]<=7) {

                isInA1B2Group=false;

            }

            bool isLastActionDefamation=false;
            int delay=5500;
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(int round=0;round<4;++round) {

                int upperPlayer=Array.IndexOf(phase4PlayerOrder,round);

                if(!isLegalPartyIndex(upperPlayer)) {

                    continue;

                }
                
                int lowerPlayer=Array.IndexOf(phase4PlayerOrder,round+4);
                
                if(!isLegalPartyIndex(upperPlayer)) {

                    continue;

                }
                
                bool isUpperPlayerDefamation=isStagingDefamationInPhase4[phase4PlayerStaging[upperPlayer]];
                bool isLowerPlayerDefamation=isStagingDefamationInPhase4[phase4PlayerStaging[lowerPlayer]];

                if(myIndex!=upperPlayer&&myIndex!=lowerPlayer) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=((isInA1B2Group)?(PHASE4_A1B2_STACK_POSITION):(PHASE4_C3D4_STACK_POSITION));
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.Delay=delay;
                    currentProperties.DestoryAt=5000;

                    if(round==0) {

                        currentProperties.Delay=0;
                        currentProperties.DestoryAt+=delay;

                    }

                    if(isLastActionDefamation) {

                        isLastActionDefamation=false;
                        
                        currentProperties.Delay+=1250;
                        currentProperties.DestoryAt-=1250;

                    }
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                }

                else {

                    if(myIndex==upperPlayer) {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=((isUpperPlayerDefamation)?(PHASE4_A1B2_DEFAMATION_POSITION):(PHASE4_A1B2_STACK_POSITION));
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.Delay=delay;
                        currentProperties.DestoryAt=((isUpperPlayerDefamation)?(6250):(5000));

                        if(round==0) {

                            currentProperties.Delay=0;
                            currentProperties.DestoryAt+=delay;

                        }
                        
                        if(isLastActionDefamation) {

                            isLastActionDefamation=false;
                        
                            currentProperties.Delay+=1250;
                            currentProperties.DestoryAt-=1250;

                        }
            
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

                        if(isUpperPlayerDefamation) {

                            isLastActionDefamation=true;

                        }
                        
                    }
                    
                    if(myIndex==lowerPlayer) {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=((isLowerPlayerDefamation)?(PHASE4_C3D4_DEFAMATION_POSITION):(PHASE4_C3D4_STACK_POSITION));
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.Delay=delay;
                        currentProperties.DestoryAt=((isLowerPlayerDefamation)?(6250):(5000));

                        if(round==0) {

                            currentProperties.Delay=0;
                            currentProperties.DestoryAt+=delay;

                        }
                        
                        if(isLastActionDefamation) {

                            isLastActionDefamation=false;
                        
                            currentProperties.Delay+=1250;
                            currentProperties.DestoryAt-=1250;

                        }
            
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                        
                        if(isLowerPlayerDefamation) {

                            isLastActionDefamation=true;

                        }
                        
                    }
                    
                }
                
                delay+=5000;

            }
            
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影4 (小队指挥)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48098"])]
    
        public void 本体_境中奇梦_心象投影4_小队指挥(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            if(phase4DisableGuidance) {

                return;

            }

            bool isTheRound=phase4TwistedVision4Semaphore3.WaitOne(4000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=4) {

                return;

            }
            
            if(!enablePartyAssistance) {

                return;

            }
            
            int[] partyInOrder=[-1,-1,-1,-1,-1,-1,-1,-1];

            for(int i=0;i<8;++i) {

                int currentPlayer=Array.IndexOf(phase4PlayerStaging,i);

                if(!isLegalPartyIndex(currentPlayer)) {

                    continue;

                }

                else {

                    partyInOrder[i]=currentPlayer;

                }

            }
            
            int[] rightDefamation=[-1,-1];
            
            rightDefamation[0]=Array.IndexOf(phase4PlayerStaging,1);
            rightDefamation[1]=Array.IndexOf(phase4PlayerStaging,3);
            
            int[] leftDefamation=[-1,-1];
            
            leftDefamation[0]=Array.IndexOf(phase4PlayerStaging,4);
            leftDefamation[1]=Array.IndexOf(phase4PlayerStaging,6);
            
            SendLindschratInformation(accessory,partyInOrder,rightDefamation,leftDefamation);
            
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影4 (小队标记)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48098"])]
    
        public void 本体_境中奇梦_心象投影4_小队标记(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            if(phase4DisableGuidance) {

                return;

            }

            bool isTheRound=phase4TwistedVision4Semaphore4.WaitOne(4000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=4) {

                return;

            }
            
            if(!enablePartyAssistance) {

                return;

            }
            
            int[] rightDefamation=[-1,-1];
            
            rightDefamation[0]=Array.IndexOf(phase4PlayerStaging,1);

            if(!isLegalPartyIndex(rightDefamation[0])) {

                return;

            }
            
            rightDefamation[1]=Array.IndexOf(phase4PlayerStaging,3);
            
            if(!isLegalPartyIndex(rightDefamation[1])) {

                return;

            }
            
            int[] leftDefamation=[-1,-1];
            
            leftDefamation[0]=Array.IndexOf(phase4PlayerStaging,4);
            
            if(!isLegalPartyIndex(leftDefamation[0])) {

                return;

            }
            
            leftDefamation[1]=Array.IndexOf(phase4PlayerStaging,6);
            
            if(!isLegalPartyIndex(leftDefamation[1])) {

                return;

            }

            string log=string.Empty;
            
            accessory.Method.Mark(accessory.Data.PartyList[rightDefamation[0]],MarkType.Stop1);

            if(enableDebugLogging) {

                log+=$"Mark {rightDefamation[0]} as {MarkType.Stop1.ToString()}\n";

            }
            
            accessory.Method.Mark(accessory.Data.PartyList[rightDefamation[1]],MarkType.Stop2);
            
            if(enableDebugLogging) {

                log+=$"Mark {rightDefamation[1]} as {MarkType.Stop2.ToString()}\n";

            }
            
            accessory.Method.Mark(accessory.Data.PartyList[leftDefamation[0]],MarkType.Bind1);

            if(enableDebugLogging) {

                log+=$"Mark {leftDefamation[0]} as {MarkType.Bind1.ToString()}\n";

            }
            
            accessory.Method.Mark(accessory.Data.PartyList[leftDefamation[1]],MarkType.Bind2);
            
            if(enableDebugLogging) {

                log+=$"Mark {leftDefamation[1]} as {MarkType.Bind2.ToString()}";

            }

            if(enableDebugLogging) {
                
                accessory.Log.Debug(log);
                
            }

        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影5 来自塔的技能 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48098"])]
    
        public void 本体_境中奇梦_心象投影5_来自塔的技能_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            bool isTheRound=phase4TwistedVision5Semaphore2.WaitOne(4000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=5) {

                return;

            }

            bool enableBothWindTrilogyTower=false;
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                enableBothWindTrilogyTower=true;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(int i=0;i<phase4Tower.Length;++i) {

                int currentOwner=i;

                if(!isLegalPartyIndex(currentOwner)) {

                    continue;

                }
                
                int currentPartner=-1;

                if(isSupporter(currentOwner)) {

                    currentPartner=currentOwner+4;

                }

                if(isDps(currentOwner)) {

                    currentPartner=currentOwner-4;

                }

                if(!isLegalPartyIndex(currentPartner)) {

                    continue;

                }
                
                if(swapsWithPartnerInPhase4[currentOwner]) {

                    (currentOwner,currentPartner)=(currentPartner,currentOwner);

                }

                switch(phase4Tower[i]) {

                    case Phase4Towers.WIND_TRILOGY: {

                        if(currentOwner!=myIndex&&!enableBothWindTrilogyTower) {

                            continue;

                        }

                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Position=PHASE4_DEFAULT_TOWER[i];
                        currentProperties.TargetObject=accessory.Data.PartyList[currentOwner];
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                        currentProperties.DestoryAt=8375;
                
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2,23);
                        currentProperties.Owner=accessory.Data.PartyList[currentOwner];
                        currentProperties.TargetPosition=PHASE4_DEFAULT_TOWER[i];
                        currentProperties.Rotation=float.Pi;
                        currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                        currentProperties.DestoryAt=8375;
                
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

                        break;

                    }
                    
                    case Phase4Towers.DARKNESS_TRILOGY: {

                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(10,50);
                        currentProperties.Position=PHASE4_DEFAULT_TOWER[i];
                        currentProperties.TargetObject=accessory.Data.PartyList[currentOwner];
                        currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                        currentProperties.DestoryAt=9375;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

                        break;

                    }
                    
                    case Phase4Towers.EARTH: {

                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(5);
                        currentProperties.Position=PHASE4_DEFAULT_TOWER[i];
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.Delay=8375;
                        currentProperties.DestoryAt=5625;
                            
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                        break;

                    }
                    
                    case Phase4Towers.FIRE: {

                        break;

                    }

                    default: {

                        break;

                    }
                    
                }
                
            }
            
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影5 塔 (指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48098"])]
    
        public void 本体_境中奇梦_心象投影5_塔_指路(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            bool isTheRound=phase4TwistedVision5Semaphore1.WaitOne(4000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=5) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            int partnersIndex=-1;

            if(isSupporter(myIndex)) {

                partnersIndex=myIndex+4;

            }

            if(isDps(myIndex)) {

                partnersIndex=myIndex-4;

            }

            if(!isLegalPartyIndex(partnersIndex)) {

                return;

            }
            
            Vector3 myTower=ARENA_CENTER;
            Vector3 partnersTower=ARENA_CENTER;

            if(swapsWithPartnerInPhase4[myIndex]) {
                
                myTower=PHASE4_DEFAULT_TOWER[partnersIndex];
                partnersTower=PHASE4_DEFAULT_TOWER[myIndex];
                
            }

            else {

                myTower=PHASE4_DEFAULT_TOWER[myIndex];
                partnersTower=PHASE4_DEFAULT_TOWER[partnersIndex];

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myTower;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=8375;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(3);
            currentProperties.Position=myTower;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=8375;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(3);
            currentProperties.Position=partnersTower;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=8375;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影5 塔 (踩塔小队指挥)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48098"])]
    
        public void 本体_境中奇梦_心象投影5_塔_踩塔小队指挥(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            bool isTheRound=phase4TwistedVision5Semaphore3.WaitOne(4000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=5) {

                return;

            }
            
            if(!enablePartyAssistance) {

                return;

            }
            
            int[] partyInOrder=[-1,-1,-1,-1,-1,-1,-1,-1];

            for(int i=0;i<4;++i) {

                int currentOwner=i;

                if(!isLegalPartyIndex(currentOwner)) {

                    continue;

                }
                
                int currentPartner=i+4;

                if(!isLegalPartyIndex(currentPartner)) {

                    continue;

                }

                if(swapsWithPartnerInPhase4[i]) {

                    partyInOrder[currentOwner]=currentPartner;
                    partyInOrder[currentPartner]=currentOwner;

                }

                else {
                    
                    partyInOrder[currentOwner]=currentOwner;
                    partyInOrder[currentPartner]=currentPartner;
                    
                }

            }
            
            SendTowerInformation(accessory,partyInOrder);
            
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影5 大蛇的魔力 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:regex:^(4767|4766)$"])]
    
        public void 本体_境中奇梦_心象投影5_大蛇的魔力_范围(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(phase4TwistedVisionCount!=5) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>=7200000) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(60);
            currentProperties.Owner=targetId;
            currentProperties.TargetOrderIndex=1;
            currentProperties.Radian=float.Pi/6;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=durationMilliseconds;

            if(string.Equals(@event["StatusID"],"4767")) {
                
                currentProperties.TargetResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                
            }
            
            if(string.Equals(@event["StatusID"],"4766")) {
                
                currentProperties.TargetResolvePattern=PositionResolvePatternEnum.PlayerFarestOrder;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Fan,currentProperties);
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影5 战火热病 (文字提示与TTS)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:4768"])]
    
        public void 本体_境中奇梦_心象投影5_战火热病_文字提示与TTS(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(phase4TwistedVisionCount!=5) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            if(targetId!=accessory.Data.Me) {

                return;

            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>=7200000) {

                return;

            }
            
            if(enablePrompts) {
                    
                accessory.Method.TextInfo("停止移动,直到这行提示消失。",durationMilliseconds,true);
                    
            }
                
            accessory.tts("停止移动,直到这行提示消失。",enableVanillaTts,enableDailyRoutinesTts);
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影5 大蛇的魔力 (指路)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:46324"],
            suppress:1000)]
    
        public void 本体_境中奇梦_心象投影5_大蛇的魔力_指路(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(phase4TwistedVisionCount!=5) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            int partnersIndex=-1;

            if(isSupporter(myIndex)) {

                partnersIndex=myIndex+4;

            }

            if(isDps(myIndex)) {

                partnersIndex=myIndex-4;

            }

            if(!isLegalPartyIndex(partnersIndex)) {

                return;

            }
            
            int myTowerIndex=-1;
            int partnersTowerIndex=-1;

            if(swapsWithPartnerInPhase4[myIndex]) {
                
                myTowerIndex=partnersIndex;
                partnersTowerIndex=myIndex;
                
            }

            else {

                myTowerIndex=myIndex;
                partnersTowerIndex=partnersIndex;

            }

            if((myTowerIndex<0||myTowerIndex>7)
               ||
               (partnersTowerIndex<0||partnersTowerIndex>7)) {

                return;

            }

            Phase4Towers myTowerType=phase4Tower[myTowerIndex];

            if(myTowerType==Phase4Towers.UNKNOWN) {

                return;

            }

            Vector3 myPosition=ARENA_CENTER;

            if(isInLeftGroup(myIndex)) {

                if(myTowerType==Phase4Towers.WIND_TRILOGY) {

                    myPosition=PHASE4_LEFT_WIND_POSITION;

                }
                
                if(myTowerType==Phase4Towers.DARKNESS_TRILOGY) {

                    myPosition=PHASE4_LEFT_DARK_POSITION;

                }

                if(myTowerType==Phase4Towers.EARTH
                   ||
                   myTowerType==Phase4Towers.FIRE) {

                    if(isMelee(myIndex)) {
                        
                        myPosition=PHASE4_LEFT_MELEE_POSITION;
                        
                    }
                    
                    if(isRanged(myIndex)) {
                        
                        myPosition=PHASE4_LEFT_RANGED_POSITION;
                        
                    }
                    
                } 
                
            }
            
            if(isInRightGroup(myIndex)) {
                
                if(myTowerType==Phase4Towers.WIND_TRILOGY) {

                    myPosition=PHASE4_RIGHT_WIND_POSITION;

                }
                
                if(myTowerType==Phase4Towers.DARKNESS_TRILOGY) {

                    myPosition=PHASE4_RIGHT_DARK_POSITION;

                }
                
                if(myTowerType==Phase4Towers.EARTH
                   ||
                   myTowerType==Phase4Towers.FIRE) {

                    if(isMelee(myIndex)) {
                        
                        myPosition=PHASE4_RIGHT_MELEE_POSITION;
                        
                    }
                    
                    if(isRanged(myIndex)) {
                        
                        myPosition=PHASE4_RIGHT_RANGED_POSITION;
                        
                    }
                    
                }
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=10625;

            if(myTowerType==Phase4Towers.FIRE) {

                currentProperties.Delay=6000;
                currentProperties.DestoryAt=4625;

            }
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            if(myTowerType==Phase4Towers.FIRE) {

                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=myPosition;
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=6000;
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            }
            
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影5 隐藏的人形分身 (数据收集)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:46365"],
            suppress:1000,
            userControl:false)]
    
        public void 本体_境中奇梦_心象投影5_隐藏的人形分身_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(phase4TwistedVisionCount!=5) {

                return;

            }

            if(!string.Equals(@event["SourceDataId"],"19204")) {

                return;

            }

            if(phase4HiddenLindschrat!=0) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }

            phase4HiddenLindschrat=sourceId;

            if(enableDebugLogging) {
                
                Vector3 sourcePosition=ARENA_CENTER;

                try {

                    sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

                } catch(Exception e) {
                
                    accessory.Log.Error("SourcePosition deserialization failed.");

                    return;

                }
                
                accessory.Log.Debug($"phase4HiddenLindschrat={phase4HiddenLindschrat}\nsourcePosition={sourcePosition}");
                
            }

        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影6 (范围)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:48098"],
            suppress:1000)]
    
        public void 本体_境中奇梦_心象投影6_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            bool isTheRound=phase4TwistedVision6Semaphore2.WaitOne(1000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=6) {

                return;

            }
            
            if(isCardinalFirstInPhase4==null) {

                return;

            }

            List<int> stagingList=new List<int>();

            if((bool)isCardinalFirstInPhase4) {

                stagingList=[0,2,4,6];

            }

            else {
                
                stagingList=[1,3,5,7];
                
            }

            if(stagingList.Count!=4) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(int i=0;i<stagingList.Count;++i) {

                Vector3 currentPosition=rotatePosition(new Vector3(100,0,86),ARENA_CENTER,float.Pi/4*stagingList[i]);

                if(isStagingDefamationInPhase4[stagingList[i]]) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(20);
                    currentProperties.Position=currentPosition;
                    currentProperties.Color=colourOfManaBurst.V4.WithW(1);
                    currentProperties.DestoryAt=9500;
                            
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

                else {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(5);
                    currentProperties.Position=currentPosition;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=9750;
                            
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

            }
            
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影6 (指路)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:48098"],
            suppress:1000)]
    
        public void 本体_境中奇梦_心象投影6_指路(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            bool isTheRound=phase4TwistedVision6Semaphore1.WaitOne(1000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=6) {

                return;

            }
            
            if(isCardinalFirstInPhase4==null) {

                return;

            }
            
            if(phase4DisableGuidance) {

                return;

            }

            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            Vector3 myPosition=ARENA_CENTER;

            if((bool)isCardinalFirstInPhase4) {

                if(isInLeftGroup(myIndex)) {

                    myPosition=PHASE4_LEFT_GROUP_STACK_WHEN_CARDINAL;

                }
                
                if(isInRightGroup(myIndex)) {

                    myPosition=PHASE4_RIGHT_GROUP_STACK_WHEN_CARDINAL;

                }
                
            }

            else {
                
                if(isInLeftGroup(myIndex)) {

                    myPosition=PHASE4_LEFT_GROUP_STACK_WHEN_INTERCARDINAL;

                }
                
                if(isInRightGroup(myIndex)) {

                    myPosition=PHASE4_RIGHT_GROUP_STACK_WHEN_INTERCARDINAL;

                }
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=9750;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影7 人形分身的连击 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48098"])]
    
        public void 本体_境中奇梦_心象投影7_人形分身的连击_范围(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            bool isTheRound=phase4TwistedVision7Semaphore2.WaitOne(4000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=7) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(int i=0;i<phase4LindschratCombo.Count;++i) {

                KeyValuePair<ulong,string> currentLindschrat=phase4LindschratCombo[i];

                if(currentLindschrat.Key==phase4HiddenLindschrat) {

                    continue;

                } 

                if(string.Equals(currentLindschrat.Value,"46351")) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(60);
                    currentProperties.Radian=float.Pi/2;
                    currentProperties.Rotation=float.Pi/2;
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=8625;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(60);
                    currentProperties.Radian=float.Pi/2;
                    currentProperties.Rotation=-float.Pi/2;
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=8625;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                    
                }
                
                if(string.Equals(currentLindschrat.Value,"46352")) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(60);
                    currentProperties.Radian=float.Pi/2;
                    currentProperties.Rotation=0;
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=8625;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(60);
                    currentProperties.Radian=float.Pi/2;
                    currentProperties.Rotation=float.Pi;
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=8625;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                    
                }
                
                if(string.Equals(currentLindschrat.Value,"46353")) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(10);
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=8500;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影7 人形分身的连击 (指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48098"])]
    
        public void 本体_境中奇梦_心象投影7_人形分身的连击_指路(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            bool isTheRound=phase4TwistedVision7Semaphore1.WaitOne(4000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=7) {

                return;

            }

            ulong targetLindschrat=0;

            for(int i=0;i<phase4LindschratCombo.Count;++i) {

                KeyValuePair<ulong,string> currentLindschrat=phase4LindschratCombo[i];

                if(currentLindschrat.Key==phase4HiddenLindschrat) {

                    continue;

                }

                if(string.Equals(currentLindschrat.Value,"46353")) {

                    continue;

                }

                if(string.Equals(currentLindschrat.Value,"46351")
                   ||
                   string.Equals(currentLindschrat.Value,"46352")) {

                    targetLindschrat=currentLindschrat.Key;

                    break;

                }

            }

            if(targetLindschrat==0) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=targetLindschrat;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=8625;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(10);
            currentProperties.Owner=targetLindschrat;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=8625;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
        
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影8 (范围)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:48098"],
            suppress:1000)]
    
        public void 本体_境中奇梦_心象投影8_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            bool isTheRound=phase4TwistedVision8Semaphore2.WaitOne(1000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=8) {

                return;

            }
            
            if(isCardinalFirstInPhase4==null) {

                return;

            }

            List<int> stagingList=new List<int>();

            if(!((bool)isCardinalFirstInPhase4)) {

                stagingList=[0,2,4,6];

            }

            else {
                
                stagingList=[1,3,5,7];
                
            }

            if(stagingList.Count!=4) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(int i=0;i<stagingList.Count;++i) {

                Vector3 currentPosition=rotatePosition(new Vector3(100,0,86),ARENA_CENTER,float.Pi/4*stagingList[i]);

                if(isStagingDefamationInPhase4[stagingList[i]]) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(20);
                    currentProperties.Position=currentPosition;
                    currentProperties.Color=colourOfManaBurst.V4.WithW(1);
                    currentProperties.DestoryAt=7625;
                            
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

                else {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(5);
                    currentProperties.Position=currentPosition;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=8000;
                            
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

            }
            
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影8 (指路)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:48098"],
            suppress:1000)]
    
        public void 本体_境中奇梦_心象投影8_指路(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            bool isTheRound=phase4TwistedVision8Semaphore1.WaitOne(1000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=8) {

                return;

            }
            
            if(isCardinalFirstInPhase4==null) {

                return;

            }
            
            if(phase4DisableGuidance) {

                return;

            }

            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            Vector3 myPosition=ARENA_CENTER;

            if(!((bool)isCardinalFirstInPhase4)) {

                if(isInLeftGroup(myIndex)) {

                    myPosition=PHASE4_LEFT_GROUP_STACK_WHEN_CARDINAL;

                }
                
                if(isInRightGroup(myIndex)) {

                    myPosition=PHASE4_RIGHT_GROUP_STACK_WHEN_CARDINAL;

                }
                
            }

            else {
                
                if(isInLeftGroup(myIndex)) {

                    myPosition=PHASE4_LEFT_GROUP_STACK_WHEN_INTERCARDINAL;

                }
                
                if(isInRightGroup(myIndex)) {

                    myPosition=PHASE4_RIGHT_GROUP_STACK_WHEN_INTERCARDINAL;

                }
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=8000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 境中奇梦 心象投影8 隐藏的人形分身 (范围)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:48098"],
            suppress:1000)]
    
        public void 本体_境中奇梦_心象投影8_隐藏的人形分身_范围(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            bool isTheRound=phase4TwistedVision8Semaphore3.WaitOne(1000);

            if(!isTheRound) {

                return;

            }
            
            if(phase4TwistedVisionCount!=8) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(int i=0;i<phase4LindschratCombo.Count;++i) {

                KeyValuePair<ulong,string> currentLindschrat=phase4LindschratCombo[i];

                if(currentLindschrat.Key!=phase4HiddenLindschrat) {

                    continue;

                } 

                if(string.Equals(currentLindschrat.Value,"46351")) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(60);
                    currentProperties.Radian=float.Pi/2;
                    currentProperties.Rotation=float.Pi/2;
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=8000;
                    currentProperties.DestoryAt=4750;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(60);
                    currentProperties.Radian=float.Pi/2;
                    currentProperties.Rotation=-float.Pi/2;
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=8000;
                    currentProperties.DestoryAt=4750;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                    
                }
                
                if(string.Equals(currentLindschrat.Value,"46352")) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(60);
                    currentProperties.Radian=float.Pi/2;
                    currentProperties.Rotation=0;
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=8000;
                    currentProperties.DestoryAt=4750;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(60);
                    currentProperties.Radian=float.Pi/2;
                    currentProperties.Rotation=float.Pi;
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=8000;
                    currentProperties.DestoryAt=4750;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                    
                }
                
                if(string.Equals(currentLindschrat.Value,"46353")) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                    currentProperties.Scale=new(10);
                    currentProperties.Owner=currentLindschrat.Key;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.Delay=8000;
                    currentProperties.DestoryAt=4625;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

            }
        
        }
        
        [ScriptMethod(name:"本体 狂暴 (初始化与阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46345"],
            userControl:false)]
    
        public void 本体_狂暴_初始化与阶段控制(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=4&&!skipPhaseChecks) {

                return;

            }

            if(phase4TwistedVisionCount<8) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();
            
            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }

            if(enablePartyAssistance) {

                accessory.Method.MarkClear();
                
            }
            
            Interlocked.Increment(ref currentPhase);

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"isInMajorPhase1={isInMajorPhase1}\ncurrentPhase={currentPhase}");
                
            }
        
        }
        
        [ScriptMethod(name:"本体 狂暴 双重飞踢 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(46368|46373)$"])]

        public void 本体_狂暴_双重飞踢_范围(Event @event,ScriptAccessory accessory) {

            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {

                return;

            }

            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(40);
            currentProperties.Owner=sourceId;
            currentProperties.Radian=float.Pi;

            if(string.Equals(@event["ActionId"],"46368")) {

                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=5500;

            }
            
            if(string.Equals(@event["ActionId"],"46373")) {

                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=4500;
                
            }
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 狂暴 魔力连击 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:46368"])]
    
        public void 本体_狂暴_魔力连击_范围(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(10);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.OwnerEnmityOrder;
            currentProperties.CentreOrderIndex=1;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=10125;
            currentProperties.DestoryAt=2500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(10);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.OwnerEnmityOrder;
            currentProperties.CentreOrderIndex=2;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=10125;
            currentProperties.DestoryAt=2500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"本体 狂暴 (搞怪)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:46188"],
            suppress:1000,
            userControl:false)]
    
        public void 本体_狂暴_搞怪(Event @event,ScriptAccessory accessory) {
            
            if(isInMajorPhase1) {

                return;
                
            }

            if(currentPhase!=5&&!skipPhaseChecks) {

                return;

            }

            if(!enableShenanigans) {

                return;

            }

            if(!enablePartyAssistance) {

                return;

            }

            List<string> ChurchillsSpeech=[
                "我们要坚持到最后! <se.7>",
                "我们将在海上作战—— <se.9>",
                "我们将以越来越强大的信心和实力在空中作战—— <se.9>",
                "我们将保卫我们的岛屿,无论代价如何! <se.7>",
                "我们将在海滩上战斗—— <se.9>",
                "我们将在登陆地上战斗—— <se.9>",
                "我们将在田野和街道上战斗—— <se.9>",
                "我们将在山丘上战斗—— <se.9>",
                "我们决不投降! <se.7>",
                "直到新世界在上帝认为合适的时刻—— <se.9>",
                "以其所有的力量,站出来拯救并解放这个旧世界! <se.7>",
                "以及,或许我们还将抡起破酒瓶与他们战斗,因为那时我们就只剩这个了。 <se.11>"
            ];

            for(int i=0;i<ChurchillsSpeech.Count;++i) {
                
                accessory.Method.SendChat("/p "+ChurchillsSpeech[i]);
                
                System.Threading.Thread.Sleep(2500);
                
            }

        }
        
        private void SendLindschratInformation(ScriptAccessory accessory,int[] partyInOrder,int[] rightDefamation,int[] leftDefamation) {

            if(partyInOrder==null) {

                return;

            }

            if(partyInOrder.Length!=8) {

                return;

            }

            if(rightDefamation==null) {

                return;

            }

            if(rightDefamation.Length!=2) {

                return;

            }
            
            if(leftDefamation==null) {

                return;

            }

            if(leftDefamation.Length!=2) {

                return;

            }

            for(int i=0;i<partyInOrder.Length;++i) {

                if(!isLegalPartyIndex(partyInOrder[i])) {

                    return;

                }
                
            }
            
            for(int i=0;i<rightDefamation.Length;++i) {

                if(!isLegalPartyIndex(rightDefamation[i])) {

                    return;

                }
                
            }
            
            for(int i=0;i<leftDefamation.Length;++i) {

                if(!isLegalPartyIndex(leftDefamation[i])) {

                    return;

                }
                
            }

            string log=$"""

                                      北
                        {ConvertIndexToAsianStyleRole(partyInOrder[4])}{ConvertIndexToAsianStyleRole(partyInOrder[5])}         {ConvertIndexToAsianStyleRole(partyInOrder[0])}{ConvertIndexToAsianStyleRole(partyInOrder[1])}
                        {ConvertIndexToAsianStyleRole(partyInOrder[6])}{ConvertIndexToAsianStyleRole(partyInOrder[7])}         {ConvertIndexToAsianStyleRole(partyInOrder[2])}{ConvertIndexToAsianStyleRole(partyInOrder[3])}
                        
                          {ConvertIndexToAsianStyleRole(leftDefamation[0])}  大圈1   {ConvertIndexToAsianStyleRole(rightDefamation[0])}
                          {ConvertIndexToAsianStyleRole(leftDefamation[1])}  大圈2   {ConvertIndexToAsianStyleRole(rightDefamation[1])}
                        锁链禁止是大圈
                        锁链去左西,禁止去右东
                        1先放圈
                        <se.1> <se.1> <se.1>
                        """;
            
            accessory.Method.SendChat("/p "+log);

            if(enableDebugLogging) {
                
                accessory.Log.Debug(log);
                
            }
            
            /*

            accessory.Method.SendChat($"""

                                                     北
                                       MTH1         STH2
                                       D1D3         D2D4
                                       
                                         MT  大圈1   ST
                                         D1  大圈2   D2
                                       """);

            */

        }
        
        private void SendTowerInformation(ScriptAccessory accessory,int[] partyInOrder) {

            if(partyInOrder==null) {

                return;

            }

            if(partyInOrder.Length!=8) {

                return;

            }

            for(int i=0;i<partyInOrder.Length;++i) {

                if(!isLegalPartyIndex(partyInOrder[i])) {

                    return;

                }
                
            }

            string log=$"""

                                          北
                        {ConvertIndexToAsianStyleRole(partyInOrder[2])}    {ConvertIndexToAsianStyleRole(partyInOrder[0])}         {ConvertIndexToAsianStyleRole(partyInOrder[5])}    {ConvertIndexToAsianStyleRole(partyInOrder[7])}
                                       Boss
                        {ConvertIndexToAsianStyleRole(partyInOrder[6])}    {ConvertIndexToAsianStyleRole(partyInOrder[4])}          {ConvertIndexToAsianStyleRole(partyInOrder[1])}     {ConvertIndexToAsianStyleRole(partyInOrder[3])}
                        <se.1> <se.1> <se.1>
                        """;
            
            accessory.Method.SendChat("/p "+log);

            if(enableDebugLogging) {
                
                accessory.Log.Debug(log);
                
            }
            
            /*

            accessory.Method.SendChat($"""

                                                         北
                                       H1    MT         D2    D4
                                                      Boss
                                       D3    D1          ST     H2
                                       
                                       """);

            */

        }

        private void SendTetherInformation(ScriptAccessory accessory,int[] partyInOrder) {

            if(partyInOrder==null) {

                return;

            }

            if(partyInOrder.Length!=8) {

                return;

            }

            for(int i=0;i<partyInOrder.Length;++i) {

                if(!isLegalPartyIndex(partyInOrder[i])) {

                    return;

                }
                
            }

            string log=$"""

                                 {ConvertIndexToAsianStyleRole(partyInOrder[0])}
                            {ConvertIndexToAsianStyleRole(partyInOrder[7])}     {ConvertIndexToAsianStyleRole(partyInOrder[1])}
                        {ConvertIndexToAsianStyleRole(partyInOrder[6])}             {ConvertIndexToAsianStyleRole(partyInOrder[2])}
                            {ConvertIndexToAsianStyleRole(partyInOrder[5])}     {ConvertIndexToAsianStyleRole(partyInOrder[3])}
                                 {ConvertIndexToAsianStyleRole(partyInOrder[4])}
                        <se.1> <se.1> <se.1>
                        """;
            
            accessory.Method.SendChat("/p "+log);

            if(enableDebugLogging) {
                
                accessory.Log.Debug(log);
                
            }
            
            /*
            
            accessory.Method.SendChat($"""

                                                MT
                                           D3     D4
                                       H1             H2
                                           D1     D2
                                                ST
                                       """);
            
            */

        }

        private string ConvertIndexToAsianStyleRole(int index) {

            if(!isLegalPartyIndex(index)) {

                return string.Empty;

            }

            else {

                return index switch{
                    
                    0 => "MT",
                    1 => "ST",
                    2 => "H1",
                    3 => "H2",
                    4 => "D1",
                    5 => "D2",
                    6 => "D3",
                    7 => "D4",
                    _ => string.Empty
                    
                };

            }
            
        }

        #endregion
        
        #region Commons
        
        private int? targetIconBaseId=null;
        private readonly Object lockOfTargetIconBaseId=new Object();
        
        private bool convertTargetIconIdToRelative(string? rawHexId,out int result) {

            lock(lockOfTargetIconBaseId) {
                
                result=0;
            
                if(string.IsNullOrWhiteSpace(rawHexId)) {
                
                    return false;
                
                }
            
                string hexId=rawHexId.Trim();
            
                hexId=hexId.StartsWith("0x",StringComparison.OrdinalIgnoreCase)?hexId.Substring(2):hexId;

                if(!int.TryParse(hexId,System.Globalization.NumberStyles.HexNumber,null,out result)) {

                    return false;

                }
                
                targetIconBaseId??=result;
                result-=targetIconBaseId.GetValueOrDefault();

                return true;

            }
            
        }

        public static bool convertObjectIdToDecimal(string? rawHexId,out ulong result) {
            
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

        public static double convertPolarToCartesian(double polarRotation) {
            
            return Math.PI-polarRotation;
            
        }
        
        public static double convertDegreesToRadians(double degree) {
            
            return degree*Math.PI/180.0;
            
        }

        public static bool isLegalPartyIndex(int partyIndex) {

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

        public static bool isInLeftGroup(int partyIndex) {
            
            return partyIndex switch {

                0 => true,
                2 => true,
                4 => true,
                6 => true,
                _ => false

            };
            
        }
        
        public static bool isInRightGroup(int partyIndex) {
            
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