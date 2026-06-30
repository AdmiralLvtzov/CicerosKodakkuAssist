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
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.STD.Helper;
using KodakkuAssist.Data;
using Lumina.Data.Parsing;

namespace CicerosKodakkuAssist.DancingMadUltimate.ChinaDataCenter
{

    [ScriptType(name:"妖星乱舞绝境战",
        territorys:[1363],
        guid:"f9948da9-ce35-44d1-b410-02375c941458",
        version:"0.0.4.12",
        note:scriptNotes,
        author:"Cicero 灵视")]

    public class Dancing_Mad_Ultimate
    {
        
        public const string scriptNotes=
            """
            妖星乱舞绝境战的脚本。脚本应该算是...完工了?
            和灵视以往的作品不同,这次可能不会为妖星乱舞绝境战制作一个包含全程绘制与指路的全能型脚本。此脚本仅提供P1到P4的绘制与少数几处指路。
            完整的妖星乱舞绝境战可达鸭体验将由数个不同作者的作品组成,包括但不限于如下作者(按副本阶段排序):
                Coda - P1的指路。
                RyougiMio - P2与P4的指路。
                Usami - P3的指路。
                Errer - P5的绘制与指路。
            此脚本将是灵视最后一个作品,灵视即将收手退休。也许灵视会在未来回归,但短期内将不再制作新的脚本。感谢陪伴!
            
            此脚本仅在P3深层痛楚(一运)的究极冲击波和P4生者黑暗光与死者黑暗光(鸳鸯锅)提供指路。
            如果指路不适配你采用的攻略,可以在方法设置中将相关的指路关闭。所有指路方法均标注有"(指路)"后缀。
            
            支持进行小队排序测试,可以在聊天框中输入/e kdmutest来检查小队排序是否正确。
            输入/e kdmuclear清除小队排序测试产生的目标标记。
            
            P1技能特效屏蔽需要在用户设置中手动启用。目前提供两种不同的方法,以缓解当前可达鸭版本的绘制丢失问题:
                传统方法 - 即改变透明施法者的可见性。该方法不会丢失绘制,但极其不建议与其他任何提供相似功能的科技同时运行。
                引擎方法 - 即根据handle移除指定特效。该方法目前有丢失绘制的报告,正在着手修复与优化。该方法可以与其他提供相似功能的科技同时运行。

            如果在使用过程中遇到了异常,请先检查可达鸭本体与脚本是否都更新到了最新版本,小队职能是否已正确设置,异常是否可以稳定复现。
            如果上述三点都没有问题,请带着A Realm Recorded插件的录像文件在可达鸭Discord内联系@_publius_cornelius_scipio_反馈异常。
            
            特别致谢(按副本阶段排序):
                Karlin - 提供了两种技能特效屏蔽的实现,还紧急加班做好了绘制淡出与屏蔽技能特效的代码轮子,not all heroes wear capes.
                RyougiMio - 提供了P2遗弃末世的绘制数据。
                南云铁虎 - 提供了大量P2与P3的绘制数据。
                莫灵喵 - 提供了大量P4的绘制数据。
                以及每一位录像提供者,出于隐私保护的目的没有列出常用名。
            
            
            
            社会的确是一种契约。...因为它并不是一种仅服务于短暂易逝的粗浅动物性存在的合伙契约。
            它是关于一切科学的合伙契约,是关于一切艺术的合伙契约,是关于每一种美德,关于一切完善(过程)的合伙契约。
            由于这样一种合伙契约的目标无法在数代人之内实现,它(便)不仅仅是生者之间的合伙契约,而是生者,逝者以及尚未出生者之间的合伙契约。
            ......
            偏见在紧急情况下可以立即发挥作用;它将人的思想预先引导到一条稳定的智慧与美德之路上,使人在做决定时不再犹豫不决,怀疑一切,迷惘困惑,迟迟不付诸行动。
            偏见使人的德性成为他的习惯,而不是一连串孤立的行为。通过正当的偏见,人的义务便成为其本性的一部分。
            
            ——埃德蒙·伯克,《法国革命论》,1790年
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
        [UserSetting("通用 小队排序测试文本发送到的频道")]
        public PartyTestChannels partyTestChannel { get; set; } = PartyTestChannels.默语频道_仅自己可见;
        [UserSetting("通用 团灭后清除目标标记")]
        public bool clearSignsAfterWipe { get; set; } = false;
        [UserSetting("通用 为呼啦啦爆炎、扩大大冰封和劈啪啪暴雷启用技能特效屏蔽")]
        public bool enableAnimationBlockade { get; set; } = false;
        [UserSetting("通用 技能特效屏蔽的方法")]
        public AnimationBlockadeModes animationBlockadeMode { get; set; } = AnimationBlockadeModes.传统方法_改变透明施法者的可见性;
        [UserSetting("通用 连环环陷阱状态消失前的绘制持续时间(秒)")]
        public double doubleTroubleTrapDuration { get; set; } = 5;
        [UserSetting("调试 启用调试日志并输出到Dalamud日志中")]
        public bool enableDebugLogging { get; set; } = false;
        [UserSetting("调试 忽略所有方法中的阶段检查")]
        public bool skipPhaseChecks { get; set; } = false;
        [UserSetting("调试 在转阶段时保留绘制")]
        public bool preserveDrawingsWhileSwitchingPhase { get; set; } = false;
        
        // ----- Major Phase 1 -----
        
        // ----- End Of Major Phase 1 -----
        
        // ----- Major Phase 2 -----
        
        [UserSetting("P2 遗弃末世 绘制的视觉类型")]
        public Phase2Sub2_DrawingTypes phase2sub2_drawingType { get; set; } = Phase2Sub2_DrawingTypes.默认;
        [UserSetting("P2 遗弃末世 咏唱危机 塔判定前的绘制持续时间(秒,最多7)")]
        public double phase2sub2_drawingDuration { get; set; } = 5;
        [UserSetting("P2 遗弃末世 咏唱危机 仅绘制咏唱危机·波动(扇形)")]
        public bool phase2sub2_spellwaveOnly { get; set; } = false;
        [UserSetting("P2 遗弃末世 塔辅助线的颜色")]
        public ScriptColor phase2sub2_colourOfAuxiliaryLines { get; set; } = new() { V4 = new Vector4(0,1,1, 1) }; // Blue by default.
        
        // ----- End Of Major Phase 2 -----
        
        // ----- Major Phase 3 -----
        
        [UserSetting("P3 深层痛楚(一运) 启用坦克LB解法")]
        public bool phase3sub2_tankLimitBreakStrat { get; set; } = false;
        [UserSetting("P3 深层痛楚(一运) 究极冲击波的解法")]
        public Phase3Sub2_UltimaBlasterStrats phase3sub2_ultimaBlasterStrat { get; set; } = Phase3Sub2_UltimaBlasterStrats.与预兆的顺逆相反;
        
        // ----- End Of Major Phase 3 -----
        
        // ----- Major Phase 4 -----
        
        [UserSetting("P4 诅咒之嚎 不指示自身的诅咒之嚎")]
        public bool phase4_disableCursedShriekOnMe { get; set; } = false;
        [UserSetting("P4 混沌之炎与混沌之水 仅绘制自身的引导范围")]
        public bool phase4_disableStrayBaitOnOthers { get; set; } = true;
        
        // ----- End Of Major Phase 4 -----

        #endregion
        
        #region Variables_And_Semaphores
        
        private volatile int majorPhase=1;
        private volatile int phase=1;
        
        /*

        Major Phase 1:

            Phases are separated by Graven Image.
            阶段由众神之像分隔。
        
        Major Phase 2:

            Phase 1 - (~,Forsaken 遗弃末世)
            Phase 2 - [Forsaken 遗弃末世,Light of Judgment 制裁之光)
            Phase 3 - [Light of Judgment 制裁之光,~)
            
        Major Phase 3:

            Phase 1 - (~,Bowels of Agony 深层痛楚)
            Phase 2 - [Bowels of Agony 深层痛楚,Earthquake 地震)
            Phase 3 - [Earthquake 地震,~)
            
        Major Phase 4:

            No phase separation.
            无阶段分隔。
            
        Major Phase 5:
        
            I'm retired! For now :D
            我退休了!

        */
        
        private volatile bool fakeFlagrantFire=false;
        private System.Threading.AutoResetEvent flagrantFireObfuscationSemaphore=new System.Threading.AutoResetEvent(false);
        private HashSet<ulong> partyMembersWithFlagrantFire=new HashSet<ulong>();
        private volatile bool stackFlagrantFireIcon=false;
        private System.Threading.AutoResetEvent flagrantFireIconSemaphore=new System.Threading.AutoResetEvent(false);
        
        // ----- Major Phase 1 -----
        
        private volatile int phase1sub2_mysteryMagicCounter=0;
        private System.Threading.AutoResetEvent phase1sub2_waveCannonSemaphore=new System.Threading.AutoResetEvent(false);
        
        private volatile bool phase1sub3_isFirstHalf=true;
        
        // ----- End Of Major Phase 1 -----
        
        // ----- Major Phase 2 -----
        
        private Phase2Sub2_IconTypes[] phase2sub2_iconType=Enumerable.Range(0,8).Select(i=>Phase2Sub2_IconTypes.UNKNOWN).ToArray();
        private volatile int phase2sub2_iconUpdateCounter=0;
        private System.Threading.AutoResetEvent[] phase2sub2_roundSemaphore=Enumerable.Range(0,7).Select(i=>new System.Threading.AutoResetEvent(false)).ToArray();
        private volatile int phase2sub2_pathOfLightCounter=0; // Its read-write lock is PHASE2_SUB2_PATH_OF_LIGHT_COUNTER_LOCK.
        private HashSet<int> phase2sub2_groupA=new HashSet<int>();
        private volatile int initialLeftStack=-1,initialRightStack=-1;
        private HashSet<int> phase2sub2_groupB=new HashSet<int>();
        private int[] phase2sub2_discretizedTowerGap=Enumerable.Range(0,9).Select(i=>-1).ToArray();
        private volatile int phase2sub2_towerCounter=0;
        private volatile int phase2sub2_discretizedPositionOfLastTower=-1;
        private System.Threading.AutoResetEvent phase2sub2_discretizedTowerGap1Semaphore=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase2sub2_discretizedTowerGap2Semaphore=new System.Threading.AutoResetEvent(false);
        
        private volatile int phase2sub3_trineCounter=0; // Its read-write lock is PHASE2_SUB3_TRINE_COUNTER_LOCK.
        
        // ----- End Of Major Phase 2 -----
        
        // ----- Major Phase 3 -----
        
        private Vector3 phase3sub2_fireCrystalPosition=ARENA_CENTER;
        private System.Threading.AutoResetEvent phase3sub2_fireCrystalSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile int phase3sub2_infernoCounter=2;
        private Vector3 phase3sub2_waterCrystalPosition=ARENA_CENTER;
        private System.Threading.AutoResetEvent phase3sub2_waterCrystalSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile int phase3sub2_tsunamiCounter=2;
        private Vector3 phase3sub2_windCrystalPosition=ARENA_CENTER;
        private bool[] phase3sub2_shouldFaceBoss=Enumerable.Range(0,8).Select(i=>false).ToArray();
        private volatile int phase3sub2_ultimaBlasterCounter=0; // Its read-write lock is PHASE3_SUB2_ULTIMA_BLASTER_COUNTER_LOCK.
        private volatile int phase3sub2_firstUltimaBlasterPosition=-1;
        private volatile bool phase3sub2_baitClockwise=false;
        
        private ConcurrentDictionary<ulong,int> phase3sub3_blackHoleDrawingCounter=new ConcurrentDictionary<ulong,int>();
        private volatile int phase3sub3_nothingnessCounter=0;
        private List<Vector3> phase3sub3_stackPositions=new List<Vector3>();
        private System.Threading.AutoResetEvent phase3sub3_bigBangSemaphore=new System.Threading.AutoResetEvent(false);
        
        // ----- End Of Major Phase 3 -----
        
        // ----- Major Phase 4 -----
        
        private volatile bool phase4_fakeNeoExdeathAction=false;
        private volatile bool phase4_fakeChaosAction=false;
        private bool[] phase4_isFakeStrayFlames=Enumerable.Range(0,8).Select(i=>false).ToArray();
        private bool[] phase4_isFakeStraySpray=Enumerable.Range(0,8).Select(i=>false).ToArray();
        private volatile int phase4_statusWithDurationCounter=0;
        private bool[] phase4_isBeyondDeath=Enumerable.Range(0,8).Select(i=>false).ToArray();
        private volatile int phase4_statusWithoutDurationCounter=0;
        private bool[] phase4_isWhiteWound=Enumerable.Range(0,8).Select(i=>false).ToArray();
        
        // ----- End Of Major Phase 4 -----
        
        #endregion
        
        #region Constants_And_Locks
        
        private const int COMMON_INTERVAL=2500;
        private const int MAXIMUM_DURATION=7200000;
        private const int VISIBILITY_RECOVERY_DELAY=125;
        private const double COMMON_DEVIATION=1;
        
        private const int SHENANIGAN_DELAY=5000;
        private const int SHENANIGAN_DURATION=10000;
        private const int PARTY_TEST_DURATION=20000;
        
        private static readonly Vector3 ARENA_CENTER=new Vector3(100,0,100);
        private const int ARENA_RADIUS=20;

        private static readonly Vector3 PHASE2_SUB2_RAW_TOWER_POSITION=new Vector3(100,0,92);
        private const int PHASE2_SUB2_TOWER_RADIUS=4;
        private readonly object PHASE2_SUB2_PATH_OF_LIGHT_COUNTER_LOCK=new object();
        
        private readonly object PHASE2_SUB3_TRINE_COUNTER_LOCK=new object();
        
        private readonly object PHASE3_SUB2_ULTIMA_BLASTER_COUNTER_LOCK=new object();
        
        #endregion
        
        #region Enumerations_And_Classes
        
        public enum PartyTestChannels {
            
            不发送到任何频道,
            默语频道_仅自己可见,
            小队频道_所有队员可见

        }
        
        public enum AnimationBlockadeModes {
            
            传统方法_改变透明施法者的可见性,
            引擎方法_根据handle移除指定特效

        }
        
        public enum Phase2Sub2_IconTypes {
            
            FAN,
            SPREAD,
            STACK,
            UNKNOWN

        }
        
        public enum Phase2Sub2_DrawingTypes {
            
            默认,
            全VFX,
            作者推荐_圆形ImGui_其余VFX,
            全ImGui

        }
        
        public enum Phase3Sub2_UltimaBlasterStrats {
            
            与预兆的顺逆相反,
            固定顺时针,
            固定逆时针

        }
        
        #endregion
        
        #region Initialization
        
        public void Init(ScriptAccessory accessory) {
            
            accessory.Method.RemoveDraw(".*");
            
            if(clearSignsAfterWipe) {

                accessory.Method.MarkClear();
                
            }
            
            VariableAndSemaphoreInitialization();
            
            if(enableShenanigans) {

                shenaniganSemaphore.Set();

            }

        }

        private void VariableAndSemaphoreInitialization() {

            majorPhase=1;
            phase=1;
            
            fakeFlagrantFire=false;
            flagrantFireObfuscationSemaphore.Reset();
            partyMembersWithFlagrantFire.Clear();
            stackFlagrantFireIcon=false;
            flagrantFireIconSemaphore.Reset();
            
            // ----- Major Phase 1 -----
            
            phase1sub2_mysteryMagicCounter=0;
            phase1sub2_waveCannonSemaphore.Reset();

            phase1sub3_isFirstHalf=true;

            // ----- End Of Major Phase 1 -----

            // ----- Major Phase 2 -----

            for(int i=0;i<phase2sub2_iconType.Length;++i)phase2sub2_iconType[i]=Phase2Sub2_IconTypes.UNKNOWN;
            phase2sub2_iconUpdateCounter=0;
            for(int i=0;i<phase2sub2_roundSemaphore.Length;++i)phase2sub2_roundSemaphore[i].Reset();
            phase2sub2_pathOfLightCounter=0;
            phase2sub2_groupA.Clear();
            initialLeftStack=-1;initialRightStack=-1;
            phase2sub2_groupB.Clear();
            for(int i=0;i<phase2sub2_discretizedTowerGap.Length;++i)phase2sub2_discretizedTowerGap[i]=-1;
            phase2sub2_towerCounter=0;
            phase2sub2_discretizedPositionOfLastTower=-1;
            phase2sub2_discretizedTowerGap1Semaphore.Reset();
            phase2sub2_discretizedTowerGap2Semaphore.Reset();
            
            phase2sub3_trineCounter=0;

            // ----- End Of Major Phase 2 -----

            // ----- Major Phase 3 -----

            phase3sub2_fireCrystalPosition=ARENA_CENTER;
            phase3sub2_fireCrystalSemaphore.Reset();
            phase3sub2_infernoCounter=2;
            phase3sub2_waterCrystalPosition=ARENA_CENTER;
            phase3sub2_waterCrystalSemaphore.Reset();
            phase3sub2_tsunamiCounter=2;
            phase3sub2_windCrystalPosition=ARENA_CENTER;
            for(int i=0;i<phase3sub2_shouldFaceBoss.Length;++i)phase3sub2_shouldFaceBoss[i]=false;
            phase3sub2_ultimaBlasterCounter=0;
            phase3sub2_firstUltimaBlasterPosition=-1;
            phase3sub2_baitClockwise=false;
            
            phase3sub3_blackHoleDrawingCounter.Clear();
            phase3sub3_nothingnessCounter=0;
            phase3sub3_stackPositions.Clear();
            phase3sub3_bigBangSemaphore.Reset();

            // ----- End Of Major Phase 3 -----
            
            // ----- Major Phase 4 -----

            phase4_fakeNeoExdeathAction=false;
            phase4_fakeChaosAction=false;
            for(int i=0;i<phase4_isFakeStrayFlames.Length;++i)phase4_isFakeStrayFlames[i]=false;
            for(int i=0;i<phase4_isFakeStraySpray.Length;++i)phase4_isFakeStraySpray[i]=false;
            phase4_statusWithDurationCounter=0;
            for(int i=0;i<phase4_isBeyondDeath.Length;++i)phase4_isBeyondDeath[i]=false;
            phase4_statusWithoutDurationCounter=0;
            for(int i=0;i<phase4_isWhiteWound.Length;++i)phase4_isWhiteWound[i]=false;
            
            // ----- End Of Major Phase 4 -----

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
            eventCondition:["DataId:19504"],
            suppress:SHENANIGAN_DELAY+SHENANIGAN_DURATION,
            userControl:false)]

        public void Shenanigans(Event @event,ScriptAccessory accessory) {
            
            if(!enableShenanigans) {

                return;

            }

            bool signalled=shenaniganSemaphore.WaitOne(SHENANIGAN_DELAY);

            if(!signalled) {

                return;

            }
            
            string prompt=quotes[new System.Random().Next(0,quotes.Count)];
            
            System.Threading.Tasks.Task.Delay(SHENANIGAN_DELAY).ContinueWith(_ => {
                
                if(!string.IsNullOrWhiteSpace(prompt)) {

                    if(enablePrompts) {
                    
                        accessory.Method.TextInfo(prompt,SHENANIGAN_DURATION);
                    
                    }
                    
                    accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
                }
                
            });

        }

        #endregion
        
        #region Global
        
        [ScriptMethod(name:"通用 小队排序测试",
            eventType:EventTypeEnum.Chat,
            eventCondition:["Type:Echo"])]

        public void 通用_小队排序测试(Event @event,ScriptAccessory accessory) {

            string processedText=(@event["Message"]).Trim().ToLower();
            
            if(!string.Equals(processedText,"kdmutest")) {

                return;

            }
            
            string text="请确认如下小队排序是否正确:\n";
            string log=string.Empty;
            KodakkuAssist.Data.IGameObject? sourceObject=null;
            string[] roles=["MT",
                            "ST",
                            "H1",
                            "H2",
                            "D1",
                            "D2",
                            "D3",
                            "D4"];
            KodakkuAssist.Module.GameOperate.MarkType[] marks=[MarkType.Stop1, // MT
                                                               MarkType.Stop2, // OT (ST)
                                                               MarkType.Bind1, // H1
                                                               MarkType.Bind2, // H2
                                                               MarkType.Attack1, // M1 (D1)
                                                               MarkType.Attack2, // M2 (D2)
                                                               MarkType.Attack3, // R1 (D3)
                                                               MarkType.Attack4]; // R2 (D4)

            for(int i=0;i<marks.Length;++i) {
                
                accessory.Method.Mark(accessory.Data.PartyList[i],marks[i]);
                
                sourceObject=accessory.Data.Objects.SearchById(accessory.Data.PartyList[i]);
                
                if(sourceObject==null||!sourceObject.IsValid()) {

                    continue;
                
                }
                
                else {
                
                    if(sourceObject is not ICharacter sourceICharacter) {

                        continue;
                    
                    }

                    else {
                        
                        text+=$"{roles[i]}:{sourceICharacter.Name}，标记{marks[i].ToString()}。";

                        if(i<marks.Length-1) {

                            text+="\n";

                        }
                        
                        log+=$"Mark {accessory.Data.PartyList[i]} as {marks[i].ToString()}\n";

                    }
                
                }
                
            }

            switch(partyTestChannel) {

                case PartyTestChannels.不发送到任何频道: {

                    break;

                }
                
                case PartyTestChannels.默语频道_仅自己可见: {
                    
                    accessory.Method.SendChat($"/e \n{text}");

                    break;

                }
                
                case PartyTestChannels.小队频道_所有队员可见: {
                    
                    accessory.Method.SendChat($"/p \n{text}");

                    break;

                }
                
                default: {

                    break;

                }
                
            }

            if(enablePrompts) {

                accessory.Method.TextInfo(text,PARTY_TEST_DURATION);
                
            }
            
            accessory.tts(text,enableVanillaTts,enableDailyRoutinesTts);

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"\n----- Party Test Text -----\n{text}\n\n----- Party Test Log -----\n{log}");
                
            }

        }
        
        [ScriptMethod(name:"通用 小队排序测试清除",
            eventType:EventTypeEnum.Chat,
            eventCondition:["Type:Echo"],
            userControl:false)]

        public void 通用_小队排序测试清除(Event @event,ScriptAccessory accessory) {

            string processedText=(@event["Message"]).Trim().ToLower();
            
            if(!string.Equals(processedText,"kdmuclear")) {

                return;

            }
            
            accessory.Method.MarkClear();
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug("Now trying to clear party test signs...");
                
            }

        }
        
        [ScriptMethod(name:"通用 呼啦啦爆炎 (数据收集1)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:regex:^(02A1|02A2|02A3|02A4|02A5|02A6)$"],
            userControl:false)]

        public void 通用_呼啦啦爆炎_数据收集1(Event @event,ScriptAccessory accessory) {
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            var targetObject=accessory.Data.Objects.SearchById(targetId);

            if(targetObject==null||!targetObject.IsValid()) {

                return;

            }

            else {

                if(targetObject.DataId!=19504) {

                    return;

                }
                
            }
            
            string log=$"@event[\"Id\"]={@event["Id"]}, ";
            
            if(string.Equals(@event["Id"],"02A1")) {

                fakeFlagrantFire=true;
                flagrantFireObfuscationSemaphore.Set();

                log+="fake Flagrant Fire";

            }
            
            if(string.Equals(@event["Id"],"02A2")) {
                
                fakeFlagrantFire=false;
                flagrantFireObfuscationSemaphore.Set();

                log+="real Flagrant Fire";

            }
            
            if(string.Equals(@event["Id"],"02A3")) {

                log+="fake Blizzard Blowout";

            }
            
            if(string.Equals(@event["Id"],"02A4")) {
                
                log+="real Blizzard Blowout";

            }
            
            if(string.Equals(@event["Id"],"02A5")) {

                log+="fake Thrumming Thunder";

            }
            
            if(string.Equals(@event["Id"],"02A6")) {
                
                log+="real Thrumming Thunder";

            }
            
            if(enableDebugLogging) {
                    
                accessory.Log.Debug(log);
                    
            }

        }
        
        [ScriptMethod(name:"通用 呼啦啦爆炎 (数据收集2)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:regex:^(0080|007F)$"],
            userControl:false)]

        public void 通用_呼啦啦爆炎_数据收集2(Event @event,ScriptAccessory accessory) {

            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }

            lock(partyMembersWithFlagrantFire) {
                
                partyMembersWithFlagrantFire.Add(targetId);

                if(partyMembersWithFlagrantFire.Count==2) {
                    
                    if(string.Equals(@event["Id"],"0080")) {

                        stackFlagrantFireIcon=true;
                        flagrantFireIconSemaphore.Set();
                        
                        if(enableDebugLogging) {
                        
                            accessory.Log.Debug($"""
                                                 stackFlagrantFireIcon={stackFlagrantFireIcon}
                                                 partyMembersWithFlagrantFire:{string.Join(",",partyMembersWithFlagrantFire)}
                                                 """);
                        
                        }

                    }
                    
                }
                
                if(partyMembersWithFlagrantFire.Count==1) {
                    
                    if(string.Equals(@event["Id"],"007F")) {

                        stackFlagrantFireIcon=false;
                        flagrantFireIconSemaphore.Set();
                        
                        if(enableDebugLogging) {
                        
                            accessory.Log.Debug($"""
                                                 stackFlagrantFireIcon={stackFlagrantFireIcon}
                                                 partyMembersWithFlagrantFire:{string.Join(",",partyMembersWithFlagrantFire)}
                                                 """);
                        
                        }

                    }
                    
                }
                
            }

        }
        
        [ScriptMethod(name:"通用 呼啦啦爆炎 (范围)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:regex:^(0080|007F)$"],
            suppress:COMMON_INTERVAL)]

        public void 通用_呼啦啦爆炎_范围(Event @event,ScriptAccessory accessory) {
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            bool signalled=flagrantFireObfuscationSemaphore.WaitOne(COMMON_INTERVAL);

            if(!signalled) {

                return;

            }
            
            signalled=flagrantFireIconSemaphore.WaitOne(COMMON_INTERVAL);

            if(!signalled) {

                return;

            }

            if(stackFlagrantFireIcon) {

                if(!fakeFlagrantFire) {
                    
                    foreach(ulong i in partyMembersWithFlagrantFire) {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(6);
                        currentProperties.Owner=i;
                        currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                        currentProperties.DestoryAt=5875;
            
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                        
                    }
                    
                }

                else {

                    for(int i=0;i<8;++i) {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(5);
                        currentProperties.Owner=accessory.Data.PartyList[i];
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.DestoryAt=5875;
            
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                        
                    }
                    
                }

            }

            else {
                
                if(!fakeFlagrantFire) {
                    
                    for(int i=0;i<8;++i) {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(5);
                        currentProperties.Owner=accessory.Data.PartyList[i];
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.DestoryAt=5875;
            
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                        
                    }
                    
                }

                else {

                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(6);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                    currentProperties.DestoryAt=5875;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
            }

        }
        
        [ScriptMethod(name:"通用 呼啦啦爆炎 (技能特效屏蔽,引擎方法)",
            eventType:EventTypeEnum.VfxEvent,
            eventCondition:["Id:regex:^(128|127)$"])]

        public void 通用_呼啦啦爆炎_技能特效屏蔽_引擎方法(Event @event,ScriptAccessory accessory) {
            
            if(!enableAnimationBlockade) {

                return;

            }

            if(animationBlockadeMode!=AnimationBlockadeModes.引擎方法_根据handle移除指定特效) {

                return;

            }
            
            if(!string.Equals(@event["Type"],"LockOn")) {

                return;

            }
            
            if(!convertHandleIdToDecimal(@event["Handle"],out var handleId)) {
                
                return;
                
            }
            
            accessory.Method.VfxMethod.RemoveVfx(handleId,VfxType.LockOn);

        }
        
        [ScriptMethod(name:"通用 呼啦啦爆炎 (数据清除)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:regex:^(47778|47779)$"],
            suppress:COMMON_INTERVAL,
            userControl:false)]

        public void 通用_呼啦啦爆炎_数据清除(Event @event,ScriptAccessory accessory) {
            
            fakeFlagrantFire=false;
            flagrantFireObfuscationSemaphore.Reset();
            partyMembersWithFlagrantFire.Clear();
            stackFlagrantFireIcon=false;
            flagrantFireIconSemaphore.Reset();

        }
        
        [ScriptMethod(name:"通用 扩大大冰封 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47768|47774)$"])]

        public void 通用_扩大大冰封_范围(Event @event,ScriptAccessory accessory) {
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(40);
            currentProperties.Radian=float.Pi/2;
            currentProperties.Owner=sourceId;
            currentProperties.DestoryAt=5000;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);

            if(!enableAnimationBlockade) {
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Fan,currentProperties);
                
            }

            else {
                
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"通用 扩大大冰封 (技能特效屏蔽,引擎方法)",
            eventType:EventTypeEnum.VfxEvent,
            eventCondition:["Id:737"])]

        public void 通用_扩大大冰封_技能特效屏蔽_引擎方法(Event @event,ScriptAccessory accessory) {
            
            if(!enableAnimationBlockade) {

                return;

            }

            if(animationBlockadeMode!=AnimationBlockadeModes.引擎方法_根据handle移除指定特效) {

                return;

            }
            
            if(!string.Equals(@event["Type"],"Omen")) {

                return;

            }
            
            if(!convertHandleIdToDecimal(@event["Handle"],out var handleId)) {
                
                return;
                
            }
            
            accessory.Method.VfxMethod.RemoveVfx(handleId,VfxType.Omen);

        }
        
        [ScriptMethod(name:"通用 扩大大冰封 (技能特效屏蔽,传统方法)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47768|47771)$"])]

        public void 通用_扩大大冰封_技能特效屏蔽_传统方法(Event @event,ScriptAccessory accessory) {
            
            if(!enableAnimationBlockade) {

                return;

            }
            
            if(animationBlockadeMode!=AnimationBlockadeModes.传统方法_改变透明施法者的可见性) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null||!sourceObject.IsValid()) {

                return;

            }

            else {
                
                adjustVisibility(accessory,sourceObject,false,VISIBILITY_RECOVERY_DELAY+5000);
                
            }

        }
        
        [ScriptMethod(name:"通用 劈啪啪暴雷 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47775|47777)$"])]

        public void 通用_劈啪啪暴雷_范围(Event @event,ScriptAccessory accessory) {
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(10,40);
            currentProperties.Owner=sourceId;
            currentProperties.DestoryAt=5000;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);

            if(!enableAnimationBlockade) {
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Rect,currentProperties);
                
            }

            else {
                
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"通用 劈啪啪暴雷 (技能特效屏蔽,引擎方法)",
            eventType:EventTypeEnum.VfxEvent,
            eventCondition:["Id:171"])]

        public void 通用_劈啪啪暴雷_技能特效屏蔽_引擎方法(Event @event,ScriptAccessory accessory) {
            
            if(!enableAnimationBlockade) {

                return;

            }

            if(animationBlockadeMode!=AnimationBlockadeModes.引擎方法_根据handle移除指定特效) {

                return;

            }
            
            if(!string.Equals(@event["Type"],"Omen")) {

                return;

            }
            
            if(!convertHandleIdToDecimal(@event["Handle"],out var handleId)) {
                
                return;
                
            }
            
            accessory.Method.VfxMethod.RemoveVfx(handleId,VfxType.Omen);

        }
        
        [ScriptMethod(name:"通用 劈啪啪暴雷 (技能特效屏蔽,传统方法)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47775|47776)$"])]

        public void 通用_劈啪啪暴雷_技能特效屏蔽_传统方法(Event @event,ScriptAccessory accessory) {

            if(!enableAnimationBlockade) {

                return;

            }
            
            if(animationBlockadeMode!=AnimationBlockadeModes.传统方法_改变透明施法者的可见性) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null||!sourceObject.IsValid()) {

                return;

            }

            else {
                
                adjustVisibility(accessory,sourceObject,false,VISIBILITY_RECOVERY_DELAY+5000);
                
            }

        }
        
        #endregion
        
        #region Major_Phase_1
        
        [ScriptMethod(name:"P1 恶狠狠毁荡 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:50179"])]

        public void P1_恶狠狠毁荡_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(100);
            currentProperties.Radian=float.Pi/3*2;
            currentProperties.Owner=sourceId;
            currentProperties.TargetObject=targetId;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=5000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(100);
            currentProperties.Radian=float.Pi/3*2;
            currentProperties.Owner=sourceId;
            currentProperties.TargetResolvePattern=PositionResolvePatternEnum.OwnerEnmityOrder;
            currentProperties.TargetOrderIndex=2;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=5000;
            currentProperties.DestoryAt=3375;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

        }
        
        [ScriptMethod(name:"P1 超驱动 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:50722"])]

        public void P1_超驱动_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(5);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.OwnerEnmityOrder;
            currentProperties.CentreOrderIndex=1;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=5000;
            currentProperties.DestoryAt=7375;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"P1 连环环陷阱 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:5078"])]

        public void P1_连环环陷阱_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>MAXIMUM_DURATION) {

                return;

            }
            
            int standardDuration=((int)(doubleTroubleTrapDuration*1000));
            int delay=0;
            int duration=0;

            if(durationMilliseconds<=standardDuration) {

                delay=0;
                duration=durationMilliseconds;

            }

            else {

                delay=durationMilliseconds-standardDuration;
                duration=standardDuration;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"P1_连环环陷阱_范围_{targetId}";
            currentProperties.Scale=new(6);
            currentProperties.Owner=targetId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=delay;
            currentProperties.DestoryAt=duration;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"P1 连环环陷阱 (击退指示)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:5078"])]

        public void P1_连环环陷阱_击退指示(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }

            if(targetId==accessory.Data.Me) {

                return;

            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>MAXIMUM_DURATION) {

                return;

            }
            
            int standardDuration=((int)(doubleTroubleTrapDuration*1000));
            int delay=0;
            int duration=0;

            if(durationMilliseconds<=standardDuration) {

                delay=0;
                duration=durationMilliseconds;

            }

            else {

                delay=durationMilliseconds-standardDuration;
                duration=standardDuration;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"P1_连环环陷阱_击退指示_{targetId}";
            currentProperties.Scale=new(2,14);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=targetId;
            currentProperties.Rotation=float.Pi;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.FadeDistance=6;
            currentProperties.FadeCentreObject=targetId;
            currentProperties.Delay=delay;
            currentProperties.DestoryAt=duration;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
            
        }
        
        [ScriptMethod(name:"P1 连环环陷阱 (范围与击退指示清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:5078"],
            userControl:false)]

        public void P1_连环环陷阱_范围与击退指示清除(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"P1_连环环陷阱_范围_{targetId}");
            accessory.Method.RemoveDraw($"P1_连环环陷阱_击退指示_{targetId}");
            
        }
        
        [ScriptMethod(name:"P1 众神之像 (阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48370"],
            userControl:false)]
    
        public void P1_众神之像_阶段控制(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }

            Interlocked.Increment(ref phase);

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"majorPhase={majorPhase}\nphase={phase}");
                
            }

        }
        
        [ScriptMethod(name:"P1 众神之像1 波动弹 (击退指示)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:002D"])]
    
        public void P1_众神之像1_波动弹_击退指示(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }

            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }

            if(targetId!=accessory.Data.Me) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2,13);
            currentProperties.Owner=targetId;
            currentProperties.FixRotation=true;
            currentProperties.Rotation=0;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.DestoryAt=5125;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
        
        }
        
        [ScriptMethod(name:"P1 众神之像1 玄乎乎魔法 (数据收集)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47764"],
            userControl:false)]

        public void P1_众神之像1_玄乎乎魔法_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }

            Interlocked.Increment(ref phase1sub2_mysteryMagicCounter);

            if(phase1sub2_mysteryMagicCounter==1) {

                phase1sub2_waveCannonSemaphore.Set();

            }

        }
        
        [ScriptMethod(name:"P1 众神之像1 波动炮 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47764"])]

        public void P1_众神之像1_波动炮_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            bool signalled=phase1sub2_waveCannonSemaphore.WaitOne(COMMON_INTERVAL);

            if(!signalled) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(int i=0;i<8;++i) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(6,100);
                currentProperties.Position=new Vector3(100,0,65);
                currentProperties.TargetObject=accessory.Data.PartyList[i];
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=5625;
                currentProperties.DestoryAt=4375;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"P1 众神之像1 爆炸 (精确范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47786"])]

        public void P1_众神之像1_爆炸_精确范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            Vector3 effectPosition=ARENA_CENTER;

            try {

                effectPosition=JsonConvert.DeserializeObject<Vector3>(@event["EffectPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("EffectPosition deserialization failed.");

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(4);
            currentProperties.Position=effectPosition;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.DestoryAt=3000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"P1 众神之像2 重力弹 (范围)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:002D"])]

        public void P1_众神之像2_重力弹_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            if(Vector3.Distance(sourcePosition,new Vector3(102.5f,22.5f,27))>COMMON_DEVIATION) {

                return;

            }

            int delay=-1;
            int duration=-1;
            
            if(phase1sub3_isFirstHalf) {

                delay=0;
                duration=6500;

            }

            else {
                    
                delay=3875;
                duration=4625;
                    
            }

            if(delay<0||duration<0) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(5);
            currentProperties.Owner=targetId;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.Delay=delay;
            currentProperties.DestoryAt=duration;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"P1 众神之像2 岩石弹 (范围)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:002D"])]

        public void P1_众神之像2_岩石弹_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            if(Vector3.Distance(sourcePosition,new Vector3(126,7,41.5f))>COMMON_DEVIATION) {

                return;

            }

            int delay=-1;
            
            if(phase1sub3_isFirstHalf) {

                delay=6500;

            }

            else {
                    
                delay=8500;
                    
            }

            if(delay<0) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(5);
            currentProperties.Owner=targetId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=delay;
            currentProperties.DestoryAt=4000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"P1 众神之像2 恶狠狠毁荡 (阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:50179"],
            userControl:false)]

        public void P1_众神之像2_恶狠狠毁荡_阶段控制(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }

            phase1sub3_isFirstHalf=false;

        }
        
        [ScriptMethod(name:"P1 众神之像2 重力波与扑杀的神气 (范围)",
            eventType:EventTypeEnum.ObjectEffect,
            eventCondition:["Id1:64"])]

        public void P1_众神之像2_重力波与扑杀的神气_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["Id2"],"128")) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            Vector3 orientation=ARENA_CENTER;
            
            if(Vector3.Distance(sourcePosition,new Vector3(92,15,27))<COMMON_DEVIATION) {

                orientation=new Vector3(ARENA_CENTER.X-10,ARENA_CENTER.Y,ARENA_CENTER.Z);

            }

            if(Vector3.Distance(sourcePosition,new Vector3(116,6.5f,43))<COMMON_DEVIATION) {
                
                orientation=new Vector3(ARENA_CENTER.X+10,ARENA_CENTER.Y,ARENA_CENTER.Z);

            }
            
            if(Vector3.Distance(orientation,ARENA_CENTER)<COMMON_DEVIATION) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(100);
            currentProperties.Radian=float.Pi;
            currentProperties.Position=ARENA_CENTER;
            currentProperties.TargetPosition=orientation;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=5000;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
        }
        
        [ScriptMethod(name:"P1 众神之像3 睡魔的神气 (范围)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:002D"])]

        public void P1_众神之像3_睡魔的神气_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            if(Vector3.Distance(sourcePosition,new Vector3(107,8.5f,43))<COMMON_DEVIATION) {

                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(5);
                currentProperties.Owner=targetId;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=3000;
                currentProperties.DestoryAt=6000;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);

            }
            
        }
        
        [ScriptMethod(name:"P1 众神之像3 懒惰的神气与圣母颂 (面向指示)",
            eventType:EventTypeEnum.ObjectEffect,
            eventCondition:["Id1:64"])]

        public void P1_众神之像3_懒惰的神气与圣母颂_面向指示(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["Id2"],"128")) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            float rotation=-(float.Pi*2);
            
            if(Vector3.Distance(sourcePosition,new Vector3(105.25f,13.5f,34))<COMMON_DEVIATION) {

                rotation=float.Pi*2;

            }

            if(Vector3.Distance(sourcePosition,new Vector3(95,12.5f,25))<COMMON_DEVIATION) {

                rotation=float.Pi;

            }
            
            if(rotation<0) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(1,3);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.FixRotation=true;
            currentProperties.Rotation=rotation;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=9875;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
            
        }
        
        #endregion

        #region Major_Phase_2

        [ScriptMethod(name:"P2 (阶段控制)",
            eventType:EventTypeEnum.PlayActionTimeline,
            eventCondition:["Id:4565"],
            userControl:false)]

        public void P2_阶段控制(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["SourceDataId"],"19506")) {

                return;

            }
            
            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }

            majorPhase=2;
            phase=1;
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug($"majorPhase={majorPhase}\nphase={phase}");
                
            }

        }
        
        [ScriptMethod(name:"P2 终末双腕 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:49740"])]

        public void P2_终末双腕_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(5);
            currentProperties.Owner=targetId;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=5000;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"P2 遗弃末世 (阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47804"],
            userControl:false)]

        public void P2_遗弃末世_阶段控制(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=1&&!skipPhaseChecks) {

                return;

            }
            
            phase=2;
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug($"majorPhase={majorPhase}\nphase={phase}");
                
            }

        }

        private bool groupPartyMembersAtBeginning(ScriptAccessory accessory) {
            
            int[][] group=[[0,2],
                           [1,3],
                           [4,6],
                           [5,7]];

            for(int i=0;i<4;++i) {

                int member1=group[i][0];
                int member2=group[i][1];

                if(phase2sub2_iconType[member1]==Phase2Sub2_IconTypes.STACK
                   ||
                   phase2sub2_iconType[member2]==Phase2Sub2_IconTypes.STACK) {

                    phase2sub2_groupA.Add(member1);
                    phase2sub2_groupA.Add(member2);
                    
                    if(phase2sub2_iconType[member1]==Phase2Sub2_IconTypes.FAN) {

                        initialLeftStack=member2;

                    }
                    
                    if(phase2sub2_iconType[member2]==Phase2Sub2_IconTypes.FAN) {

                        initialLeftStack=member1;

                    }
                    
                    if(phase2sub2_iconType[member1]==Phase2Sub2_IconTypes.SPREAD) {

                        initialRightStack=member2;

                    }
                    
                    if(phase2sub2_iconType[member2]==Phase2Sub2_IconTypes.SPREAD) {

                        initialRightStack=member1;

                    }

                }

                else {
                    
                    phase2sub2_groupB.Add(member1);
                    phase2sub2_groupB.Add(member2);
                    
                }
                
            }

            if(enableDebugLogging) {
                        
                accessory.Log.Debug($"""
                                     phase2sub2_groupA:{string.Join(",",phase2sub2_groupA)}
                                     initialLeftStack={initialLeftStack},initialRightStack={initialRightStack}
                                     phase2sub2_groupB:{string.Join(",",phase2sub2_groupB)}
                                     """);
                        
            }

            if(phase2sub2_groupA.Count==4
               &&
               phase2sub2_groupB.Count==4
               &&
               !phase2sub2_groupA.Overlaps(phase2sub2_groupB)) {

                return true;

            }

            else {

                return false;

            }

        }
        
        [ScriptMethod(name:"P2 遗弃末世 咏唱危机 (数据收集)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:regex:^(02CD|02CC|02CB)$"],
            userControl:false)]

        public void P2_遗弃末世_咏唱危机_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase2sub2_iconUpdateCounter>=8+4*6) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            lock(phase2sub2_iconType) {

                bool validUpdate=false;
                
                if(string.Equals(@event["Id"],"02CD")) {

                    phase2sub2_iconType[targetIndex]=Phase2Sub2_IconTypes.FAN;

                    validUpdate=true;

                }
                
                if(string.Equals(@event["Id"],"02CC")) {

                    phase2sub2_iconType[targetIndex]=Phase2Sub2_IconTypes.SPREAD;
                    
                    validUpdate=true;

                }
                
                if(string.Equals(@event["Id"],"02CB")) {

                    phase2sub2_iconType[targetIndex]=Phase2Sub2_IconTypes.STACK;
                    
                    validUpdate=true;

                }

                if(validUpdate) {

                    Interlocked.Increment(ref phase2sub2_iconUpdateCounter);

                    if(phase2sub2_iconUpdateCounter>=8) {

                        if(phase2sub2_iconUpdateCounter==8) {

                            if(!groupPartyMembersAtBeginning(accessory)) {

                                return;

                            }

                            phase2sub2_roundSemaphore[0].Set();

                            if(enableDebugLogging) {
                                
                                accessory.Log.Debug("The initialization semaphore has been signalled.");
                                
                            }

                        }

                        else {

                            int updateCountAfterInitialization=phase2sub2_iconUpdateCounter-8;

                            if(updateCountAfterInitialization%4==0) {
                                
                                phase2sub2_roundSemaphore[updateCountAfterInitialization/4].Set();
                                
                                if(enableDebugLogging) {

                                    accessory.Log.Debug($"""
                                                        updateCountAfterInitialization={updateCountAfterInitialization}
                                                        Round {updateCountAfterInitialization/4} semaphore has been signalled.
                                                        """);

                                }
                                
                            }

                        }
                        
                    }

                }
                
            }

        }

        private int getDiscretizedTowerGap(int discretizedTower1,int discretizedTower2) {

            int result=-1;
            
            if(discretizedTower1<0||discretizedTower1>7) {

                return result;

            }
            
            if(discretizedTower2<0||discretizedTower2>7) {

                return result;

            }

            if(Math.Abs(discretizedTower1-discretizedTower2)==2) {

                result=((discretizedTower1+discretizedTower2)/2+16)%8;

            }
            
            if(Math.Abs(discretizedTower1-discretizedTower2)==6) {

                result=((discretizedTower1+discretizedTower2+8)/2+16)%8;

            }

            return result;

        }
        
        [ScriptMethod(name:"P2 遗弃末世 塔 (数据收集)",
            eventType:EventTypeEnum.EnvControl,
            eventCondition:["Flag:2"],
            userControl:false)]

        public void P2_遗弃末世_塔_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase2sub2_towerCounter>=16) {

                return;

            }
            
            if(!convertStringToSignedInteger(@event["Index"], out var index)) {
                
                return;
                
            }

            if(index<1||index>8) {

                return;

            }

            lock(phase2sub2_discretizedTowerGap) {

                Interlocked.Increment(ref phase2sub2_towerCounter);

                if(phase2sub2_towerCounter%2==1) {

                    phase2sub2_discretizedPositionOfLastTower=(index-1+8)%8;

                }
                
                if(phase2sub2_towerCounter%2==0) {

                    int discretizedPositionOfCurrentTower=(index-1+8)%8;
                    int currentRound=phase2sub2_towerCounter/2;

                    phase2sub2_discretizedTowerGap[currentRound]=getDiscretizedTowerGap(phase2sub2_discretizedPositionOfLastTower,discretizedPositionOfCurrentTower);

                    if(phase2sub2_discretizedTowerGap[currentRound]==-1) {

                        return;

                    }

                    if(currentRound==1) {

                        phase2sub2_discretizedTowerGap1Semaphore.Set();
                        
                        if(enableDebugLogging) {

                            accessory.Log.Debug($"""
                                                 phase2sub2_towerCounter={phase2sub2_towerCounter}
                                                 currentRound={currentRound}
                                                 phase2sub2_discretizedPositionOfLastTower={phase2sub2_discretizedPositionOfLastTower}
                                                 discretizedPositionOfCurrentTower={discretizedPositionOfCurrentTower}
                                                 phase2sub2_discretizedTowerGap[{currentRound}]={phase2sub2_discretizedTowerGap[currentRound]}
                                                 The gap 1 semaphore has been signalled.
                                                 """);

                        }

                    }

                    if(currentRound==2) {

                        int trend=phase2sub2_discretizedTowerGap[2]-phase2sub2_discretizedTowerGap[1];

                        if(trend==-7) {

                            trend=1;

                        }
                        
                        if(trend==7) {

                            trend=-1;

                        }

                        for(int i=3;i<phase2sub2_discretizedTowerGap.Length;++i) {

                            phase2sub2_discretizedTowerGap[i]=(phase2sub2_discretizedTowerGap[i-1]+trend+16)%8;

                        }

                        phase2sub2_discretizedTowerGap1Semaphore.Set();
                        
                        if(enableDebugLogging) {

                            accessory.Log.Debug($"""
                                                 phase2sub2_towerCounter={phase2sub2_towerCounter}
                                                 currentRound={currentRound}
                                                 phase2sub2_discretizedPositionOfLastTower={phase2sub2_discretizedPositionOfLastTower}
                                                 discretizedPositionOfCurrentTower={discretizedPositionOfCurrentTower}
                                                 trend={trend}
                                                 phase2sub2_discretizedTowerGap:{string.Join(",",phase2sub2_discretizedTowerGap)}
                                                 The gap 2 semaphore has been signalled.
                                                 """);

                        }

                    }

                }

            }
            
        }
        
        [ScriptMethod(name:"P2 遗弃末世 (指路,第1轮) !!!尚未完工,不生效!!!",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:47804"],
            suppress:COMMON_INTERVAL)]

        public void P2_遗弃末世_指路_第1轮(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            bool signalled=phase2sub2_roundSemaphore[0].WaitOne(1500+COMMON_INTERVAL);

            if(!signalled) {

                return;

            }
            
            // To be done.

        }
        
        [ScriptMethod(name:"P2 遗弃末世 (指路,第2轮到第8轮) !!!尚未完工,不生效!!!",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:47806"])]

        public void P2_遗弃末世_指路_第2轮到第8轮(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["TargetIndex"],"1")) {

                return;

            }

            if(phase2sub2_pathOfLightCounter>=2*8) {

                return;

            }

            bool lastRoundEnded=false;
            int lastRound=0;

            lock(PHASE2_SUB2_PATH_OF_LIGHT_COUNTER_LOCK) {

                Interlocked.Increment(ref phase2sub2_pathOfLightCounter);

                if(phase2sub2_pathOfLightCounter%2==0) {

                    lastRoundEnded=true;
                    lastRound=phase2sub2_pathOfLightCounter/2;
                                
                    if(enableDebugLogging) {

                        accessory.Log.Debug($"""
                                             phase2sub2_pathOfLightCounter={phase2sub2_pathOfLightCounter}
                                             lastRoundEnded={lastRoundEnded}
                                             lastRound={lastRound}
                                             """);

                    }
                    
                }

            }

            if(lastRoundEnded
               &&
               (1<=lastRound&&lastRound<=7)) {

                if(1<=lastRound&&lastRound<=6) {
                    
                    bool signalled=phase2sub2_roundSemaphore[lastRound].WaitOne(625+COMMON_INTERVAL);

                    if(!signalled) {

                        return;

                    }
                    
                }
                
                // To be done.
                
            }

        }

        private DrawModeEnum getDrawModeEnum(DrawTypeEnum currentType) {

            switch(phase2sub2_drawingType) {
                
                case Phase2Sub2_DrawingTypes.默认: {

                    return DrawModeEnum.Default;

                }

                case Phase2Sub2_DrawingTypes.全VFX: {

                    return DrawModeEnum.Vfx;

                }
                
                case Phase2Sub2_DrawingTypes.作者推荐_圆形ImGui_其余VFX: {

                    if(currentType==DrawTypeEnum.Circle) {

                        return DrawModeEnum.Imgui;

                    }

                    else {

                        return DrawModeEnum.Vfx;

                    }

                }
                
                case Phase2Sub2_DrawingTypes.全ImGui: {

                    return DrawModeEnum.Imgui;

                }

                default: {

                    return DrawModeEnum.Default;

                }
                
            }
            
        }
        
        [ScriptMethod(name:"P2 遗弃末世 咏唱危机 (范围)",
            eventType:EventTypeEnum.EnvControl,
            eventCondition:["Flag:8"])]

        public void P2_遗弃末世_咏唱危机_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertStringToSignedInteger(@event["Index"], out var index)) {
                
                return;
                
            }

            if(index<1||index>8) {

                return;

            }
            
            int drawingDuration=((int)(phase2sub2_drawingDuration*1000));

            if(drawingDuration<=0) {

                return;

            }
            
            int delay=0;
            int duration=0;

            if(drawingDuration>7000) {

                delay=125;
                duration=7000;

            }

            else {

                delay=7125-drawingDuration;
                duration=drawingDuration;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(int i=0;i<8;++i) {
                
                if(phase2sub2_iconType[i]==Phase2Sub2_IconTypes.UNKNOWN) {
                    
                    continue;
                    
                }
                
                switch(phase2sub2_iconType[i]) {

                    case Phase2Sub2_IconTypes.FAN: {
                                    
                        currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
                        currentProperties.Scale=new(40);
                        currentProperties.Radian=float.Pi/2;
                        currentProperties.Owner=accessory.Data.PartyList[i];
                        currentProperties.TargetResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                        currentProperties.TargetOrderIndex=1;
                        currentProperties.FadeCentrePosition=rotatePosition(PHASE2_SUB2_RAW_TOWER_POSITION,ARENA_CENTER,Math.PI/4*(index-1));
                        currentProperties.FadeDistance=PHASE2_SUB2_TOWER_RADIUS;
                        currentProperties.FadeMode=FadeMode.OmenCentre;
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.Delay=delay;
                        currentProperties.DestoryAt=duration;

                        accessory.Method.SendDraw(getDrawModeEnum(DrawTypeEnum.Fan),DrawTypeEnum.Fan,currentProperties);

                        break;
                                        
                    }
                                
                    case Phase2Sub2_IconTypes.SPREAD: {

                        if(phase2sub2_spellwaveOnly) {

                            break;

                        }
                                    
                        currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
                        currentProperties.Scale=new(5);
                        currentProperties.Owner=accessory.Data.PartyList[i];
                        currentProperties.FadeCentrePosition=rotatePosition(PHASE2_SUB2_RAW_TOWER_POSITION,ARENA_CENTER,Math.PI/4*(index-1));
                        currentProperties.FadeDistance=PHASE2_SUB2_TOWER_RADIUS;
                        currentProperties.FadeMode=FadeMode.OmenCentre;
                        currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                        currentProperties.Delay=delay;
                        currentProperties.DestoryAt=duration;

                        accessory.Method.SendDraw(getDrawModeEnum(DrawTypeEnum.Circle),DrawTypeEnum.Circle,currentProperties);

                        break;

                    }
                                
                    case Phase2Sub2_IconTypes.STACK: {
                        
                        if(phase2sub2_spellwaveOnly) {

                            break;

                        }
                                    
                        currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
                        currentProperties.Scale=new(5);
                        currentProperties.Owner=accessory.Data.PartyList[i];
                        currentProperties.FadeCentrePosition=rotatePosition(PHASE2_SUB2_RAW_TOWER_POSITION,ARENA_CENTER,Math.PI/4*(index-1));
                        currentProperties.FadeDistance=PHASE2_SUB2_TOWER_RADIUS;
                        currentProperties.FadeMode=FadeMode.OmenCentre;
                        currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                        currentProperties.Delay=delay;
                        currentProperties.DestoryAt=duration;

                        accessory.Method.SendDraw(getDrawModeEnum(DrawTypeEnum.Circle),DrawTypeEnum.Circle,currentProperties);

                        break;

                    }

                    case Phase2Sub2_IconTypes.UNKNOWN: {

                        break;
                                
                    }
                            
                    default: {

                        break;

                    }
                            
                }
                    
            }
            
        }
        
        [ScriptMethod(name:"P2 遗弃末世 (塔辅助线)",
            eventType:EventTypeEnum.EnvControl,
            eventCondition:["Flag:2"])]

        public void P2_遗弃末世_塔辅助线(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertStringToSignedInteger(@event["Index"], out var index)) {
                
                return;
                
            }

            if(index<1||index>8) {

                return;

            }
            
            Vector3 towerCenter=PHASE2_SUB2_RAW_TOWER_POSITION;
            Vector3 relativeNorth=new Vector3(PHASE2_SUB2_RAW_TOWER_POSITION.X,PHASE2_SUB2_RAW_TOWER_POSITION.Y,PHASE2_SUB2_RAW_TOWER_POSITION.Z+1);
            Vector3 relativeWest=new Vector3(PHASE2_SUB2_RAW_TOWER_POSITION.X+1,PHASE2_SUB2_RAW_TOWER_POSITION.Y,PHASE2_SUB2_RAW_TOWER_POSITION.Z);
            
            towerCenter=rotatePosition(towerCenter,ARENA_CENTER,Math.PI/4*(index-1));
            relativeNorth=rotatePosition(relativeNorth,ARENA_CENTER,Math.PI/4*(index-1));
            relativeWest=rotatePosition(relativeWest,ARENA_CENTER,Math.PI/4*(index-1));
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(0.125f,8);
            currentProperties.Position=towerCenter;
            currentProperties.TargetPosition=relativeNorth;
            currentProperties.Color=phase2sub2_colourOfAuxiliaryLines.V4.WithW(1);
            currentProperties.DestoryAt=10000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Straight,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(0.125f,8);
            currentProperties.Position=towerCenter;
            currentProperties.TargetPosition=relativeWest;
            currentProperties.Color=phase2sub2_colourOfAuxiliaryLines.V4.WithW(1);
            currentProperties.DestoryAt=10000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Straight,currentProperties);
            
        }
        
        [ScriptMethod(name:"P2 遗弃末世 过去终结与未来终结 (引导范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47826|47827)$"])]

        public void P2_遗弃末世_过去终结与未来终结_引导范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(uint i=1;i<=4;++i) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
                currentProperties.Scale=new(5);
                currentProperties.Owner=sourceId;
                currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                currentProperties.CentreOrderIndex=i;
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=6750;

                accessory.Method.SendDraw(getDrawModeEnum(DrawTypeEnum.Circle),DrawTypeEnum.Circle,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"P2 遗弃末世 消灭之脚 (引导范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47826|47827)$"])]

        public void P2_遗弃末世_消灭之脚_引导范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
            currentProperties.Scale=new(100);
            currentProperties.Radian=float.Pi;
            currentProperties.Owner=sourceId;
            currentProperties.TargetObject=accessory.Data.Me;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=6750;
            currentProperties.DestoryAt=6500;

            if(string.Equals(@event["ActionId"],"47826")) {

                currentProperties.Rotation=0;

            }
            
            if(string.Equals(@event["ActionId"],"47827")) {

                currentProperties.Rotation=float.Pi;

            }

            accessory.Method.SendDraw(getDrawModeEnum(DrawTypeEnum.Fan),DrawTypeEnum.Fan,currentProperties);

        }
        
        [ScriptMethod(name:"P2 遗弃末世 消灭之脚 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47836|47837)$"])]

        public void P2_遗弃末世_消灭之脚_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
            currentProperties.Scale=new(100);
            currentProperties.Radian=float.Pi;
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=5000;
            
            if(string.Equals(@event["ActionId"],"47836")) {

                currentProperties.Rotation=0;

            }
            
            if(string.Equals(@event["ActionId"],"47837")) {

                currentProperties.Rotation=float.Pi;

            }

            accessory.Method.SendDraw(getDrawModeEnum(DrawTypeEnum.Fan),DrawTypeEnum.Fan,currentProperties);

        }
        
        [ScriptMethod(name:"P2 遗弃末世后 制裁之光 (阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47805"],
            userControl:false)]

        public void P2_遗弃末世后_制裁之光_阶段控制(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }

            phase=3;
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug($"majorPhase={majorPhase}\nphase={phase}");
                
            }

        }
        
        [ScriptMethod(name:"P2 遗弃末世后 破坏之翼 (单翼范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47821|47822)$"])]

        public void P2_遗弃末世后_破坏之翼_单翼范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
            currentProperties.Scale=new(40,80);
            currentProperties.Owner=sourceId;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=4000;
            
            if(string.Equals(@event["ActionId"],"47821")) {

                currentProperties.Rotation=float.Pi/2;

            }
            
            if(string.Equals(@event["ActionId"],"47822")) {

                currentProperties.Rotation=-float.Pi/2;

            }

            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"P2 遗弃末世后 破坏之翼 (双翼范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:50311"])]

        public void P2_遗弃末世后_破坏之翼_双翼范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
            currentProperties.Scale=new(7);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
            currentProperties.CentreOrderIndex=1;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=4000;

            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
            currentProperties.Scale=new(7);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerFarestOrder;
            currentProperties.CentreOrderIndex=1;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=4000;

            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"P2 遗弃末世后 异三角 (数据收集与范围)",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:regex:^(2015154|2015155)$"])]

        public void P2_遗弃末世后_异三角_数据收集与范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["Operate"],"Add")) {

                return;

            }

            if(phase2sub3_trineCounter>=7) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            int delay=-1;
            int duration1=-1;
            int duration2=-1;

            lock(PHASE2_SUB3_TRINE_COUNTER_LOCK) {

                Interlocked.Increment(ref phase2sub3_trineCounter);

                if(phase2sub3_trineCounter<=3) {

                    delay=0;
                    duration1=9875;
                    duration2=2000;

                }

                else {

                    delay=7875;
                    duration1=2000;
                    duration2=2000;
                    
                }

            }

            Vector3 position=sourcePosition;
            
            if(string.Equals(@event["DataId"],"2015154")) {

                position=new Vector3(sourcePosition.X-3,sourcePosition.Y,sourcePosition.Z+5);

            }
            
            if(string.Equals(@event["DataId"],"2015155")) {

                position=new Vector3(sourcePosition.X+3,sourcePosition.Y,sourcePosition.Z+5);

            }
            
            if(string.Equals(@event["DataId"],"2015154")
               &&
               Vector3.Distance(sourcePosition,new Vector3(88.45f,0,90))<COMMON_DEVIATION) {

                position=new Vector3(sourcePosition.X+3,sourcePosition.Y,sourcePosition.Z+5); // To be changed in the future.

            }
            
            if(Vector3.Distance(position,sourcePosition)<COMMON_DEVIATION) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(int i=0;i<3;++i) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
                currentProperties.Scale=new(6);
                currentProperties.Position=rotatePosition(position,sourcePosition,Math.PI/3*2*i);
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=delay;
                currentProperties.DestoryAt=duration1;

                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
                currentProperties.Scale=new(6);
                currentProperties.Position=rotatePosition(position,sourcePosition,Math.PI/3*2*i);
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.Delay=delay+duration1;
                currentProperties.DestoryAt=duration2;

                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                
            }

        }
        
        #endregion

        #region Major_Phase_3

        [ScriptMethod(name:"P3 (阶段控制)",
            eventType:EventTypeEnum.PlayActionTimeline,
            eventCondition:["Id:3218"],
            userControl:false)]

        public void P3_阶段控制(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["SourceDataId"],"19504")) {

                return;

            }
            
            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }

            majorPhase=3;
            phase=1;
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug($"majorPhase={majorPhase}\nphase={phase}");
                
            }

        }
        
        [ScriptMethod(name:"P3 暴雷 (死刑范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47881"])]

        public void P3_暴雷_死刑范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(5);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
            currentProperties.CentreOrderIndex=1;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=8125;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"P3 经度聚爆与纬度聚爆 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47870|47869)$"])]

        public void P3_经度聚爆与纬度聚爆_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            if(string.Equals(@event["ActionId"],"47869")) {

                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(40);
                currentProperties.Radian=float.Pi/2;
                currentProperties.Owner=sourceId;
                currentProperties.Rotation=0;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=0;
                currentProperties.DestoryAt=5750;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(40);
                currentProperties.Radian=float.Pi/2;
                currentProperties.Owner=sourceId;
                currentProperties.Rotation=float.Pi;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=0;
                currentProperties.DestoryAt=5750;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(40);
                currentProperties.Radian=float.Pi/2;
                currentProperties.Owner=sourceId;
                currentProperties.Rotation=float.Pi/2;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=5750;
                currentProperties.DestoryAt=2000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(40);
                currentProperties.Radian=float.Pi/2;
                currentProperties.Owner=sourceId;
                currentProperties.Rotation=-(float.Pi/2);
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=5750;
                currentProperties.DestoryAt=2000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

            }
            
            if(string.Equals(@event["ActionId"],"47870")) {

                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(40);
                currentProperties.Radian=float.Pi/2;
                currentProperties.Owner=sourceId;
                currentProperties.Rotation=float.Pi/2;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=0;
                currentProperties.DestoryAt=5750;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(40);
                currentProperties.Radian=float.Pi/2;
                currentProperties.Owner=sourceId;
                currentProperties.Rotation=-(float.Pi/2);
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=0;
                currentProperties.DestoryAt=5750;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(40);
                currentProperties.Radian=float.Pi/2;
                currentProperties.Owner=sourceId;
                currentProperties.Rotation=0;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=5750;
                currentProperties.DestoryAt=2000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(40);
                currentProperties.Radian=float.Pi/2;
                currentProperties.Owner=sourceId;
                currentProperties.Rotation=float.Pi;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=5750;
                currentProperties.DestoryAt=2000;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

            }

        }
        
        [ScriptMethod(name:"P3 深层痛楚 (阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47858"],
            userControl:false)]

        public void P3_深层痛楚_阶段控制(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=1&&!skipPhaseChecks) {

                return;

            }
            
            phase=2;
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug($"majorPhase={majorPhase}\nphase={phase}");
                
            }

        }
        
        [ScriptMethod(name:"P3 深层痛楚 水晶 (数据收集)",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:regex:^(2015290|2015291|2015292)$"],
            userControl:false)]

        public void P3_深层痛楚_水晶_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

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
            
            if(string.Equals(@event["DataId"],"2015290")) {

                if(Vector3.Distance(phase3sub2_fireCrystalPosition,ARENA_CENTER)>COMMON_DEVIATION) {

                    return;

                }

                phase3sub2_fireCrystalPosition=sourcePosition;
                phase3sub2_fireCrystalSemaphore.Set();
                
                if(enableDebugLogging) {
                        
                    accessory.Log.Debug($"phase3sub2_fireCrystalPosition={phase3sub2_fireCrystalPosition}");
                        
                }

            }
                
            if(string.Equals(@event["DataId"],"2015291")) {
                
                if(Vector3.Distance(phase3sub2_waterCrystalPosition,ARENA_CENTER)>COMMON_DEVIATION) {

                    return;

                }

                phase3sub2_waterCrystalPosition=sourcePosition;
                phase3sub2_waterCrystalSemaphore.Set();
                
                if(enableDebugLogging) {
                        
                    accessory.Log.Debug($"phase3sub2_waterCrystalPosition={phase3sub2_waterCrystalPosition}");
                        
                }

            }
            
            if(string.Equals(@event["DataId"],"2015292")) {
                
                if(Vector3.Distance(phase3sub2_windCrystalPosition,ARENA_CENTER)>COMMON_DEVIATION) {

                    return;

                }

                phase3sub2_windCrystalPosition=sourcePosition;
                
                if(enableDebugLogging) {
                        
                    accessory.Log.Debug($"phase3sub2_windCrystalPosition={phase3sub2_windCrystalPosition}");
                        
                }

            }

        }
        
        [ScriptMethod(name:"P3 深层痛楚 混沌之风与混沌之逆风 (数据收集)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:regex:^(1602|1603)$"],
            userControl:false)]

        public void P3_深层痛楚_混沌之风与混沌之逆风_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            lock(phase3sub2_shouldFaceBoss) {
                
                if(string.Equals(@event["StatusID"],"1602")) {

                    phase3sub2_shouldFaceBoss[targetIndex]=false;

                }
                
                if(string.Equals(@event["StatusID"],"1603")) {

                    phase3sub2_shouldFaceBoss[targetIndex]=true;

                }

            }
            
        }
        
        [ScriptMethod(name:"P3 深层痛楚 暴雷 (钢铁范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47890"])]

        public void P3_深层痛楚_暴雷_钢铁范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(14.8f);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=7000;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"P3 深层痛楚 混沌之炎 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:1600"])]

        public void P3_深层痛楚_混沌之炎_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>MAXIMUM_DURATION) {

                return;

            }

            int standardDuration=0;

            if(durationMilliseconds<=32500) {
                
                standardDuration=6750;
                
            }

            else {
                
                standardDuration=4000;
                
            }
            
            int delay=0;
            int duration=0;

            if(durationMilliseconds<=standardDuration) {

                delay=0;
                duration=durationMilliseconds;

            }

            else {

                delay=durationMilliseconds-standardDuration;
                duration=standardDuration;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"P3_深层痛楚_混沌之炎_范围_{targetId}";
            currentProperties.Scale=new(5);
            currentProperties.Owner=targetId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=delay;
            currentProperties.DestoryAt=duration;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"P3 深层痛楚 烈焰 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:1600","SourceId:E0000000"],
            suppress:COMMON_INTERVAL)]

        public void P3_深层痛楚_烈焰_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>MAXIMUM_DURATION) {

                return;

            }

            int standardDuration=0;

            if(durationMilliseconds<=32500) {
                
                standardDuration=6750;
                
            }

            else {
                
                standardDuration=4000;
                
            }
            
            int delay=0;
            int duration=0;

            if(durationMilliseconds<=standardDuration) {

                delay=0;
                duration=durationMilliseconds;

            }

            else {

                delay=durationMilliseconds-standardDuration;
                duration=standardDuration;

            }
            
            bool signalled=phase3sub2_fireCrystalSemaphore.WaitOne(COMMON_INTERVAL);

            if(!signalled) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"P3_深层痛楚_烈焰_范围_2";
            currentProperties.Scale=new(10);
            currentProperties.InnerScale=new(5);
            currentProperties.Radian=float.Pi*2;
            currentProperties.Position=phase3sub2_fireCrystalPosition;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
            currentProperties.CentreOrderIndex=2;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=delay;
            currentProperties.DestoryAt=duration+2875;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Donut,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"P3_深层痛楚_烈焰_范围_1";
            currentProperties.Scale=new(10);
            currentProperties.InnerScale=new(5);
            currentProperties.Radian=float.Pi*2;
            currentProperties.Position=phase3sub2_fireCrystalPosition;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
            currentProperties.CentreOrderIndex=1;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=delay;
            currentProperties.DestoryAt=duration+2875;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Donut,currentProperties);
            
        }
        
        [ScriptMethod(name:"P3 深层痛楚 混沌之炎与烈焰 (范围清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:1600"],
            userControl:false)]

        public void P3_深层痛楚_混沌之炎与烈焰_范围清除(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"P3_深层痛楚_混沌之炎_范围_{targetId}");

            lock(phase3sub2_fireCrystalSemaphore) {

                if(phase3sub2_infernoCounter>=1) {
                    
                    accessory.Method.RemoveDraw($"P3_深层痛楚_烈焰_范围_{phase3sub2_infernoCounter}");

                    --phase3sub2_infernoCounter;
                    
                }

            }
            
        }
        
        [ScriptMethod(name:"P3 深层痛楚 混沌之水 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:1601"])]

        public void P3_深层痛楚_混沌之水_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>MAXIMUM_DURATION) {

                return;

            }

            int standardDuration=0;

            if(durationMilliseconds<=32500) {
                
                standardDuration=6750;
                
            }

            else {
                
                standardDuration=4000;
                
            }
            
            int delay=0;
            int duration=0;

            if(durationMilliseconds<=standardDuration) {

                delay=0;
                duration=durationMilliseconds;

            }

            else {

                delay=durationMilliseconds-standardDuration;
                duration=standardDuration;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"P3_深层痛楚_混沌之水_范围_{targetId}";
            currentProperties.Scale=new(10);
            currentProperties.InnerScale=new(5);
            currentProperties.Radian=float.Pi*2;
            currentProperties.Owner=targetId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=delay;
            currentProperties.DestoryAt=duration;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Donut,currentProperties);
            
        }
        
        [ScriptMethod(name:"P3 深层痛楚 海啸 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:1601","SourceId:E0000000"],
            suppress:COMMON_INTERVAL)]

        public void P3_深层痛楚_海啸_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>MAXIMUM_DURATION) {

                return;

            }

            int standardDuration=0;

            if(durationMilliseconds<=32500) {
                
                standardDuration=6750;
                
            }

            else {
                
                standardDuration=4000;
                
            }
            
            int delay=0;
            int duration=0;

            if(durationMilliseconds<=standardDuration) {

                delay=0;
                duration=durationMilliseconds;

            }

            else {

                delay=durationMilliseconds-standardDuration;
                duration=standardDuration;

            }
            
            bool signalled=phase3sub2_waterCrystalSemaphore.WaitOne(COMMON_INTERVAL);

            if(!signalled) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"P3_深层痛楚_海啸_范围_2";
            currentProperties.Scale=new(5);
            currentProperties.Position=phase3sub2_waterCrystalPosition;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
            currentProperties.CentreOrderIndex=2;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=delay;
            currentProperties.DestoryAt=duration+2875;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"P3_深层痛楚_海啸_范围_1";
            currentProperties.Scale=new(5);
            currentProperties.Position=phase3sub2_waterCrystalPosition;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
            currentProperties.CentreOrderIndex=1;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=delay;
            currentProperties.DestoryAt=duration+2875;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"P3 深层痛楚 混沌之水与海啸 (范围清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:1601"],
            userControl:false)]

        public void P3_深层痛楚_混沌之水与海啸_范围清除(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"P3_深层痛楚_混沌之水_范围_{targetId}");
            
            lock(phase3sub2_waterCrystalSemaphore) {

                if(phase3sub2_tsunamiCounter>=1) {
                    
                    accessory.Method.RemoveDraw($"P3_深层痛楚_海啸_范围_{phase3sub2_tsunamiCounter}");

                    --phase3sub2_tsunamiCounter;
                    
                }

            }
            
        }
        
        [ScriptMethod(name:"P3 深层痛楚 本影爆碎 (引导指示)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47870|47869)$"])]

        public void P3_深层痛楚_本影爆碎_引导指示(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="P3_深层痛楚_本影爆碎_引导指示1";
            currentProperties.Scale=new(2);
            currentProperties.Owner=sourceId;
            currentProperties.TargetResolvePattern=PositionResolvePatternEnum.PlayerFarestOrder;
            currentProperties.TargetOrderIndex=1;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.Delay=9875;
            currentProperties.DestoryAt=MAXIMUM_DURATION;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="P3_深层痛楚_本影爆碎_引导指示2";
            currentProperties.Scale=new(20);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerFarestOrder;
            currentProperties.CentreOrderIndex=1;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.Delay=9875;
            currentProperties.DestoryAt=MAXIMUM_DURATION;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"P3 深层痛楚 本影爆碎 (引导指示清除)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47872"],
            userControl:false)]

        public void P3_深层痛楚_本影爆碎_引导指示清除(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }

            accessory.Method.RemoveDraw("P3_深层痛楚_本影爆碎_引导指示1");
            accessory.Method.RemoveDraw("P3_深层痛楚_本影爆碎_引导指示2");

        }
        
        [ScriptMethod(name:"P3 深层痛楚 本影爆碎 (落点指示)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47872"])]

        public void P3_深层痛楚_本影爆碎_落点指示(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            Vector3 effectPosition=ARENA_CENTER;

            try {

                effectPosition=JsonConvert.DeserializeObject<Vector3>(@event["EffectPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("EffectPosition deserialization failed.");

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=sourceId;
            currentProperties.TargetPosition=effectPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=5000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(20);
            currentProperties.Position=effectPosition;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=5000;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"P3 深层痛楚 真空波 (面向指示,仅坦克LB解法)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47891"])]

        public void P3_深层痛楚_真空波_面向指示_仅坦克LB解法(Event @event,ScriptAccessory accessory) {
            
            if(!phase3sub2_tankLimitBreakStrat) {

                return;

            }
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(1,3);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=sourceId;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=4750;
            currentProperties.DestoryAt=4250;

            if(phase3sub2_shouldFaceBoss[myIndex]) {

                currentProperties.Rotation=0;

            }

            else {

                currentProperties.Rotation=float.Pi;

            }
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);

        }
        
        /*
        
        [ScriptMethod(name:"P3 深层痛楚 真空波 (正确处理时的击退指示,仅坦克LB解法)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47891"])]

        public void P3_深层痛楚_真空波_正确处理时的击退指示_仅坦克LB解法(Event @event,ScriptAccessory accessory) {
            
            if(!phase3sub2_tankLimitBreakStrat) {

                return;

            }
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2,10);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=sourceId;
            currentProperties.Rotation=float.Pi;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.Delay=5000;
            currentProperties.DestoryAt=4000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);

        }
        
        */
        
        [ScriptMethod(name:"P3 深层痛楚 龙卷风 (范围,仅坦克LB解法)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47891"])]

        public void P3_深层痛楚_龙卷风_范围_仅坦克LB解法(Event @event,ScriptAccessory accessory) {

            if(!phase3sub2_tankLimitBreakStrat) {

                return;

            }
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(uint i=1;i<=8;++i) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(6);
                currentProperties.Position=phase3sub2_windCrystalPosition;
                currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                currentProperties.CentreOrderIndex=i;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=9000;
                currentProperties.DestoryAt=2875;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"P3 深层痛楚 究极冲击波 (数据收集)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:47843"])]

        public void P3_深层痛楚_究极冲击波_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["TargetIndex"],"1")) {

                return;

            }

            if(phase3sub2_ultimaBlasterCounter>=8) {

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

            lock(PHASE3_SUB2_ULTIMA_BLASTER_COUNTER_LOCK) {

                Interlocked.Increment(ref phase3sub2_ultimaBlasterCounter);

                if(phase3sub2_ultimaBlasterCounter==1) {

                    phase3sub2_firstUltimaBlasterPosition=discretizedPosition;

                }
                
                if(phase3sub2_ultimaBlasterCounter==2) {

                    if((discretizedPosition-1+8)%8==phase3sub2_firstUltimaBlasterPosition) {

                        phase3sub2_baitClockwise=false;

                    }

                    if((discretizedPosition+1+8)%8==phase3sub2_firstUltimaBlasterPosition) {

                        phase3sub2_baitClockwise=true;

                    }

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"phase3sub2_firstUltimaBlasterPosition={phase3sub2_firstUltimaBlasterPosition}\nphase3sub2_baitClockwise={phase3sub2_baitClockwise}");
                        
                    }

                }

            }

        }

        private int getTargetIconNumber(string id) {

            if(string.Equals(id,"0150")) {

                return 1;

            }
            
            if(string.Equals(id,"0151")) {

                return 2;

            }
            
            if(string.Equals(id,"0152")) {

                return 3;

            }
            
            if(string.Equals(id,"0153")) {

                return 4;

            }
            
            if(string.Equals(id,"01B5")) {

                return 5;

            }
            
            if(string.Equals(id,"01B6")) {

                return 6;

            }
            
            if(string.Equals(id,"01B7")) {

                return 7;

            }
            
            if(string.Equals(id,"01B8")) {

                return 8;

            }

            return -1;

        }
        
        [ScriptMethod(name:"P3 深层痛楚 究极冲击波 (范围)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:regex:^(0150|0151|0152|0153|01B5|01B6|01B7|01B8)$"])]

        public void P3_深层痛楚_究极冲击波_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }

            int targetIconNumber=getTargetIconNumber(@event["Id"]);

            if(!isLegalPartyIndex(targetIconNumber-1)) {

                return;

            }

            int discretizedPosition=-1;

            if(phase3sub2_baitClockwise) {

                discretizedPosition=(phase3sub2_firstUltimaBlasterPosition+(targetIconNumber-1)+16)%8;

            }

            else {
                
                discretizedPosition=(phase3sub2_firstUltimaBlasterPosition-(targetIconNumber-1)+16)%8;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6,100);
            currentProperties.Position=rotatePosition(new Vector3(100,0,80),ARENA_CENTER,Math.PI/4*discretizedPosition);
            currentProperties.TargetObject=targetId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=1125;
            currentProperties.DestoryAt=11000+250*(targetIconNumber-1);
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"P3 深层痛楚 究极冲击波 (指路)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:regex:^(0150|0151|0152|0153|01B5|01B6|01B7|01B8)$"])]

        public void P3_深层痛楚_究极冲击波_指路(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }

            if(targetId!=accessory.Data.Me) {

                return;

            }

            int targetIconNumber=getTargetIconNumber(@event["Id"]);

            if(!isLegalPartyIndex(targetIconNumber-1)) {

                return;

            }

            int discretizedPosition=-1;

            if(phase3sub2_baitClockwise) {

                discretizedPosition=(phase3sub2_firstUltimaBlasterPosition+(targetIconNumber-1)+16)%8;

            }

            else {
                
                discretizedPosition=(phase3sub2_firstUltimaBlasterPosition-(targetIconNumber-1)+16)%8;
                
            }
            
            discretizedPosition=(discretizedPosition+4+16)%8;

            Vector3 myPosition=rotatePosition(new Vector3(100,0,81),ARENA_CENTER,Math.PI/4*discretizedPosition);

            switch(phase3sub2_ultimaBlasterStrat) {

                case Phase3Sub2_UltimaBlasterStrats.与预兆的顺逆相反: {

                    if(phase3sub2_baitClockwise) {
                        
                        myPosition=rotatePosition(myPosition,ARENA_CENTER,Math.PI/8);
                        
                    }

                    else {
                        
                        myPosition=rotatePosition(myPosition,ARENA_CENTER,-(Math.PI/8));
                        
                    }

                    break;

                }
                
                case Phase3Sub2_UltimaBlasterStrats.固定顺时针: {
                    
                    myPosition=rotatePosition(myPosition,ARENA_CENTER,Math.PI/8);

                    break;

                }
                
                case Phase3Sub2_UltimaBlasterStrats.固定逆时针: {
                    
                    myPosition=rotatePosition(myPosition,ARENA_CENTER,-(Math.PI/8));

                    break;

                }
                
                default: {

                    break;

                }
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=1125;
            currentProperties.DestoryAt=11000+250*(targetIconNumber-1);
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"P3 地震 (阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:50546"],
            userControl:false)]

        public void P3_地震_阶段控制(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            phase=3;
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug($"majorPhase={majorPhase}\nphase={phase}");
                
            }

        }
        
        [ScriptMethod(name:"P3 地震 响亮亮耳光 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47846|47847)$"])]

        public void P3_地震_响亮亮耳光_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }

            int xOffset=0;
            
            if(string.Equals(@event["ActionId"],"47846")) {

                xOffset=10;

            }
            
            if(string.Equals(@event["ActionId"],"47847")) {

                xOffset=-10;

            }

            if(xOffset==0) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(13);
            currentProperties.Owner=sourceId;
            currentProperties.Offset=new Vector3(xOffset,0,10);
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=5750;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(13);
            currentProperties.Owner=sourceId;
            currentProperties.Offset=new Vector3(xOffset,0,0);
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=6375;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(13);
            currentProperties.Owner=sourceId;
            currentProperties.Offset=new Vector3(xOffset,0,-10);
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=6875;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(6);
            currentProperties.Position=ARENA_CENTER;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=5125;
            currentProperties.DestoryAt=3250;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
            if(string.Equals(@event["ActionId"],"47846")) {

                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(100);
                currentProperties.Radian=float.Pi/3;
                currentProperties.Position=ARENA_CENTER;
                currentProperties.TargetObject=accessory.Data.Me;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=5125;
                currentProperties.DestoryAt=3375;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

            }
            
            if(string.Equals(@event["ActionId"],"47847")) {
                
                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
                if(!isLegalPartyIndex(myIndex)) {

                    return;

                }

                for(int i=0;i<8;++i) {
                    
                    int currentIndex=accessory.Data.PartyList.IndexOf(accessory.Data.PartyList[i]);
            
                    if(!isLegalPartyIndex(currentIndex)) {

                        continue;

                    }

                    else {
                        
                        if(isTank(currentIndex)==isTank(myIndex)
                           &&
                           isHealer(currentIndex)==isHealer(myIndex)
                           &&
                           isDps(currentIndex)==isDps(myIndex)) {
                        
                            currentProperties=accessory.Data.GetDefaultDrawProperties();

                            currentProperties.Scale=new(100);
                            currentProperties.Radian=float.Pi/3;
                            currentProperties.Position=ARENA_CENTER;
                            currentProperties.TargetObject=accessory.Data.PartyList[i];
                            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                            currentProperties.Delay=5125;
                            currentProperties.DestoryAt=3375;
                    
                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                        
                        }

                        else {

                            continue;

                        }
                        
                    }
                    
                }

            }

        }
        
        [ScriptMethod(name:"P3 地震 黑洞 (碰撞箱范围)",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:19512"])]

        public void P3_地震_黑洞_碰撞箱范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"P3_地震_黑洞_碰撞箱范围_{sourceId}";
            currentProperties.Scale=new(1);
            currentProperties.Owner=sourceId;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=MAXIMUM_DURATION;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"P3 地震 黑洞 (碰撞箱范围清除)",
            eventType:EventTypeEnum.RemoveCombatant,
            eventCondition:["DataId:19512"],
            userControl:false)]

        public void P3_地震_黑洞_碰撞箱范围清除(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }

            accessory.Method.RemoveDraw($"P3_地震_黑洞_碰撞箱范围_{sourceId}");

        }
        
        [ScriptMethod(name:"P3 地震 黑洞 (连线范围)",
            eventType:EventTypeEnum.VfxEvent,
            eventCondition:["Id:84"])]
    
        public void P3_地震_黑洞_连线范围(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }

            if(!string.Equals(@event["Type"],"Channeling")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            lock(phase3sub3_blackHoleDrawingCounter) {
                
                int lastDrawing=phase3sub3_blackHoleDrawingCounter.GetOrAdd(sourceId,0);
            
                accessory.Method.RemoveDraw($"P3_地震_黑洞_连线范围_{sourceId}_{lastDrawing}");

                Interlocked.Increment(ref lastDrawing);
                phase3sub3_blackHoleDrawingCounter[sourceId]=lastDrawing;
            
                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Name=$"P3_地震_黑洞_连线范围_{sourceId}_{lastDrawing}";
                currentProperties.Scale=new(6,125);
                currentProperties.Owner=sourceId;
                currentProperties.TargetObject=targetId;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=MAXIMUM_DURATION;

                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                
            }
        
        }
        
        [ScriptMethod(name:"P3 地震 黑洞 (连线范围清除1)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:47868"],
            userControl:false)]
    
        public void P3_地震_黑洞_连线范围清除1(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["TargetIndex"],"1")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }

            lock(phase3sub3_blackHoleDrawingCounter) {

                Interlocked.Increment(ref phase3sub3_nothingnessCounter);
                
                if(!(new List<int>{4,5,6,7,8,9,13,14,15,16,17,18}.Contains(phase3sub3_nothingnessCounter))) {

                    accessory.Method.RemoveDraw(@$"^P3_地震_黑洞_连线范围_{sourceId}_.*$");

                    phase3sub3_blackHoleDrawingCounter.TryRemove(sourceId,out _);

                }

            }
        
        }
        
        [ScriptMethod(name:"P3 地震 黑洞 (连线范围清除2)",
            eventType:EventTypeEnum.RemoveCombatant,
            eventCondition:["DataId:19512"],
            userControl:false)]
    
        public void P3_地震_黑洞_连线范围清除2(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }

            lock(phase3sub3_blackHoleDrawingCounter) {
                
                accessory.Method.RemoveDraw(@$"^P3_地震_黑洞_连线范围_{sourceId}_.*$");

                phase3sub3_blackHoleDrawingCounter.TryRemove(sourceId,out _);

            }
        
        }
        
        [ScriptMethod(name:"P3 地震 诅咒敕令 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47873"])]

        public void P3_地震_诅咒敕令_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(80,60);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=5000;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"P3 地震 本色出演的我 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47854"])]

        public void P3_地震_本色出演的我_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(16,100);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=5000;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);

        }
        
        [ScriptMethod(name:"P3 地震 轰击 (范围)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:00A1"])]

        public void P3_地震_轰击_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(targetId==accessory.Data.Me) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(6);
                currentProperties.Owner=targetId;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=5125;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
            }

            else {
                
                int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
                if(!isLegalPartyIndex(targetIndex)) {

                    return;

                }
                
                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
                if(!isLegalPartyIndex(myIndex)) {

                    return;

                }

                if(isSupporter(targetIndex)==isSupporter(myIndex)
                   &&
                   isDps(targetIndex)==isDps(myIndex)) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(6);
                    currentProperties.Owner=targetId;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5125;
            
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                    /*
                
                    currentProperties=accessory.Data.GetDefaultDrawProperties();
            
                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetObject=targetId;
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5125;
            
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                    
                    // The guidance part has been removed to prevent confusion.
                    
                    */
                    
                }

                else {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(6);
                    currentProperties.Owner=targetId;
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=5125;
            
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
            }

        }
        
        [ScriptMethod(name:"P3 地震 轰击 (数据收集)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:47875"],
            userControl:false)]

        public void P3_地震_轰击_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["TargetIndex"],"1")) {

                return;

            }

            if(phase3sub3_stackPositions.Count>=2) {

                return;

            }
            
            Vector3 targetPosition=ARENA_CENTER;

            try {

                targetPosition=JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("TargetPosition deserialization failed.");

                return;

            }

            lock(phase3sub3_stackPositions) {
                
                phase3sub3_stackPositions.Add(targetPosition);

                if(phase3sub3_stackPositions.Count==2) {

                    phase3sub3_bigBangSemaphore.Set();

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"""
                                             phase3sub3_stackPositions:{string.Join(",",phase3sub3_stackPositions)}
                                             """);
                        
                    }
                    
                }
                
            }

        }
        
        [ScriptMethod(name:"P3 地震 顶起 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47877"])]

        public void P3_地震_顶起_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }
            
            bool signalled=phase3sub3_bigBangSemaphore.WaitOne(COMMON_INTERVAL);

            if(!signalled) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            for(int i=0;i<2;++i) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(6);
                currentProperties.Position=phase3sub3_stackPositions[i];
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=4500;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
            }

        }

        #endregion

        #region Major_Phase_4

        [ScriptMethod(name:"P4 (阶段控制)",
            eventType:EventTypeEnum.PlayActionTimeline,
            eventCondition:["Id:7747"],
            userControl:false)]

        public void P4_阶段控制(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["SourceDataId"],"18475")) {

                return;

            }
            
            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }

            majorPhase=4;
            phase=1;
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug($"majorPhase={majorPhase}\nphase={phase}");
                
            }

        }
        
        [ScriptMethod(name:"P4 新生艾克斯迪司 (数据收集)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:2056"],
            userControl:false)]

        public void P4_新生艾克斯迪司_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            var targetObject=accessory.Data.Objects.SearchById(targetId);

            if(targetObject==null||!targetObject.IsValid()) {

                return;

            }

            else {

                if(targetObject.DataId!=19510) {

                    return;

                }
                
            }
            
            if(string.Equals(@event["Param"],"1122")) {

                phase4_fakeNeoExdeathAction=false;

            }
            
            if(string.Equals(@event["Param"],"1121")) {

                phase4_fakeNeoExdeathAction=true;

            }

        }
        
        [ScriptMethod(name:"P4 卡奥斯 (数据收集)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:2056"],
            userControl:false)]

        public void P4_卡奥斯_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            var targetObject=accessory.Data.Objects.SearchById(targetId);

            if(targetObject==null||!targetObject.IsValid()) {

                return;

            }

            else {

                if(targetObject.DataId!=19507) {

                    return;

                }
                
            }
            
            if(string.Equals(@event["Param"],"1120")) {

                phase4_fakeChaosAction=false;

            }
            
            if(string.Equals(@event["Param"],"1119")) {

                phase4_fakeChaosAction=true;

            }

        }
        
        [ScriptMethod(name:"P4 诅咒之嚎 (指示)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:5543"])]

        public void P4_诅咒之嚎_指示(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }

            if(targetId==accessory.Data.Me) {

                if(phase4_disableCursedShriekOnMe) {

                    return;

                }

            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>MAXIMUM_DURATION) {

                return;

            }
            
            int standardDuration=8000;
            int delay=0;
            int duration=0;

            if(durationMilliseconds<=standardDuration) {

                delay=0;
                duration=durationMilliseconds;

            }

            else {

                delay=durationMilliseconds-standardDuration;
                duration=standardDuration;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(!phase4_fakeNeoExdeathAction) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Name=$"P4_诅咒之嚎_指示1_{targetId}";
                currentProperties.Scale=new(0.125f,6);
                currentProperties.Owner=targetId;
                currentProperties.FixRotation=true;
                currentProperties.Rotation=float.Pi/4;
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(4);
                currentProperties.Delay=delay;
                currentProperties.DestoryAt=duration;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Name=$"P4_诅咒之嚎_指示2_{targetId}";
                currentProperties.Scale=new(0.125f,6);
                currentProperties.Owner=targetId;
                currentProperties.FixRotation=true;
                currentProperties.Rotation=-(float.Pi/4);
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(4);
                currentProperties.Delay=delay;
                currentProperties.DestoryAt=duration;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Straight,currentProperties);
                
            }

            else {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Name=$"P4_诅咒之嚎_指示1_{targetId}";
                currentProperties.Scale=new(0.125f,2);
                currentProperties.Owner=targetId;
                currentProperties.FixRotation=true;
                currentProperties.Rotation=-(float.Pi/4*3);
                currentProperties.Color=accessory.Data.DefaultSafeColor.WithW(4);
                currentProperties.Delay=delay;
                currentProperties.DestoryAt=duration;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Rect,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Name=$"P4_诅咒之嚎_指示2_{targetId}";
                currentProperties.Scale=new(0.125f,4);
                currentProperties.Owner=targetId;
                currentProperties.FixRotation=true;
                currentProperties.Rotation=float.Pi/4*3;
                currentProperties.Color=accessory.Data.DefaultSafeColor.WithW(4);
                currentProperties.Delay=delay;
                currentProperties.DestoryAt=duration;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Rect,currentProperties);
                
            }
            
        }
        
        [ScriptMethod(name:"P4 诅咒之嚎 (指示清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:5543"],
            userControl:false)]

        public void P4_诅咒之嚎_指示清除(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"P4_诅咒之嚎_指示1_{targetId}");
            accessory.Method.RemoveDraw($"P4_诅咒之嚎_指示2_{targetId}");
            
        }
        
        [ScriptMethod(name:"P4 叉形闪电 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:5544"])]

        public void P4_叉形闪电_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>MAXIMUM_DURATION) {

                return;

            }
            
            int standardDuration=4250;
            int delay=0;
            int duration=0;

            if(durationMilliseconds<=standardDuration) {

                delay=0;
                duration=durationMilliseconds;

            }

            else {

                delay=durationMilliseconds-standardDuration;
                duration=standardDuration;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Name=$"P4_叉形闪电_范围_{targetId}";
            currentProperties.Scale=new(8);
            currentProperties.Owner=targetId;
            currentProperties.Delay=delay;
            currentProperties.DestoryAt=duration;

            if(!phase4_fakeNeoExdeathAction) {
                
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                
            }

            else {
                
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                
            }
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"P4 叉形闪电 (范围清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:5544"],
            userControl:false)]

        public void P4_叉形闪电_范围清除(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"P4_叉形闪电_范围_{targetId}");
            
        }
        
        [ScriptMethod(name:"P4 水属性压缩 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:5545"])]

        public void P4_水属性压缩_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>MAXIMUM_DURATION) {

                return;

            }
            
            int standardDuration=4250;
            int delay=0;
            int duration=0;

            if(durationMilliseconds<=standardDuration) {

                delay=0;
                duration=durationMilliseconds;

            }

            else {

                delay=durationMilliseconds-standardDuration;
                duration=standardDuration;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Name=$"P4_水属性压缩_范围_{targetId}";
            currentProperties.Scale=new(8);
            currentProperties.Owner=targetId;
            currentProperties.Delay=delay;
            currentProperties.DestoryAt=duration;

            if(!phase4_fakeNeoExdeathAction) {
                
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                
            }

            else {
                
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                
            }
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"P4 水属性压缩 (范围清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:5545"],
            userControl:false)]

        public void P4_水属性压缩_范围清除(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"P4_水属性压缩_范围_{targetId}");
            
        }
        
        [ScriptMethod(name:"P4 混沌之炎 (引导范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:5547"])]

        public void P4_混沌之炎_引导范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }

            if(phase4_disableStrayBaitOnOthers) {

                if(targetId!=accessory.Data.Me) {

                    return;

                }
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>MAXIMUM_DURATION) {

                return;

            }
            
            int standardDuration=5500;
            int delay=0;
            int duration=0;

            if(durationMilliseconds<=standardDuration) {

                delay=0;
                duration=durationMilliseconds;

            }

            else {

                delay=durationMilliseconds-standardDuration;
                duration=standardDuration;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(!phase4_fakeChaosAction) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Name=$"P4_混沌之炎_引导范围_{targetId}";
                currentProperties.Scale=new(6);
                currentProperties.Owner=targetId;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=delay;
                currentProperties.DestoryAt=duration;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
            }

            else {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Name=$"P4_混沌之炎_引导范围_{targetId}";
                currentProperties.Scale=new(40);
                currentProperties.InnerScale=new(6);
                currentProperties.Radian=float.Pi*2;
                currentProperties.Owner=targetId;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=delay;
                currentProperties.DestoryAt=duration;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
                
            }
            
        }
        
        [ScriptMethod(name:"P4 混沌之炎 (引导范围清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:5547"],
            userControl:false)]

        public void P4_混沌之炎_引导范围清除(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"P4_混沌之炎_引导范围_{targetId}");
            
        }
        
        [ScriptMethod(name:"P4 混沌之水 (引导范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:5548"])]

        public void P4_混沌之水_引导范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            if(phase4_disableStrayBaitOnOthers) {

                if(targetId!=accessory.Data.Me) {

                    return;

                }
                
            }
            
            int durationMilliseconds=0;

            try {

                durationMilliseconds=JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]);

            } catch(Exception e) {
                
                accessory.Log.Error("DurationMilliseconds deserialization failed.");

                return;

            }

            if(durationMilliseconds<=0||durationMilliseconds>MAXIMUM_DURATION) {

                return;

            }
            
            int standardDuration=5500;
            int delay=0;
            int duration=0;

            if(durationMilliseconds<=standardDuration) {

                delay=0;
                duration=durationMilliseconds;

            }

            else {

                delay=durationMilliseconds-standardDuration;
                duration=standardDuration;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(!phase4_fakeChaosAction) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Name=$"P4_混沌之水_引导范围_{targetId}";
                currentProperties.Scale=new(40);
                currentProperties.InnerScale=new(6);
                currentProperties.Radian=float.Pi*2;
                currentProperties.Owner=targetId;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=delay;
                currentProperties.DestoryAt=duration;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
                
            }

            else {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Name=$"P4_混沌之水_引导范围_{targetId}";
                currentProperties.Scale=new(6);
                currentProperties.Owner=targetId;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=delay;
                currentProperties.DestoryAt=duration;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
            }
            
        }
        
        [ScriptMethod(name:"P4 混沌之水 (引导范围清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:5548"],
            userControl:false)]

        public void P4_混沌之水_引导范围清除(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"P4_混沌之水_引导范围_{targetId}");
            
        }
        
        [ScriptMethod(name:"P4 混沌之炎 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47906|47907)$"])]

        public void P4_混沌之炎_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            Vector3 effectPosition=ARENA_CENTER;

            try {

                effectPosition=JsonConvert.DeserializeObject<Vector3>(@event["EffectPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("EffectPosition deserialization failed.");

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(string.Equals(@event["ActionId"],"47906")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(6);
                currentProperties.Position=effectPosition;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=5000;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
            }
            
            if(string.Equals(@event["ActionId"],"47907")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(40);
                currentProperties.InnerScale=new(6);
                currentProperties.Radian=float.Pi*2;
                currentProperties.Position=effectPosition;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=5000;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"P4 混沌之水 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47908|47909)$"])]

        public void P4_混沌之水_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            Vector3 effectPosition=ARENA_CENTER;

            try {

                effectPosition=JsonConvert.DeserializeObject<Vector3>(@event["EffectPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("EffectPosition deserialization failed.");

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            if(string.Equals(@event["ActionId"],"47908")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(40);
                currentProperties.InnerScale=new(6);
                currentProperties.Radian=float.Pi*2;
                currentProperties.Position=effectPosition;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=5000;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
                
            }
            
            if(string.Equals(@event["ActionId"],"47909")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(6);
                currentProperties.Position=effectPosition;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=5000;
            
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"P4 超越死亡与亚拉戈领域 (数据收集)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:regex:^(1382|454|5464)$"],
            userControl:false)]

        public void P4_超越死亡与亚拉戈领域_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(phase4_statusWithDurationCounter>=8) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            lock(phase4_isBeyondDeath) {
                
                if(string.Equals(@event["StatusID"],"1382")) {

                    phase4_isBeyondDeath[targetIndex]=true;

                }
                
                if(string.Equals(@event["StatusID"],"5464")) { // Square Enix sucks.

                    phase4_isBeyondDeath[targetIndex]=true;

                }
                
                if(string.Equals(@event["StatusID"],"454")) {

                    phase4_isBeyondDeath[targetIndex]=false;

                }
                
                Interlocked.Increment(ref phase4_statusWithDurationCounter);

                if(phase4_statusWithDurationCounter==8) {
                    
                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"""
                                             phase4_isBeyondDeath:{string.Join(",",phase4_isBeyondDeath)}
                                             """);
                        
                    }
                    
                }

            }

        }
        
        [ScriptMethod(name:"P4 生者之伤与死者之伤 (数据收集)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:regex:^(5541|5542|4887|4888)$"],
            userControl:false)]

        public void P4_生者之伤与死者之伤_数据收集(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(string.Equals(@event["SourceId"],"00000000")) {

                return;

            }
            
            if(phase4_statusWithoutDurationCounter>=8) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            lock(phase4_isWhiteWound) {
                
                if(string.Equals(@event["StatusID"],"5541")) {

                    phase4_isWhiteWound[targetIndex]=true;

                }
                
                if(string.Equals(@event["StatusID"],"4887")) { // Square Enix sucks again.

                    phase4_isWhiteWound[targetIndex]=true;

                }
                
                if(string.Equals(@event["StatusID"],"5542")) {

                    phase4_isWhiteWound[targetIndex]=false;

                }
                
                if(string.Equals(@event["StatusID"],"4888")) { // Square Enix sucks again.

                    phase4_isWhiteWound[targetIndex]=false;

                }
                
                Interlocked.Increment(ref phase4_statusWithoutDurationCounter);

                if(phase4_statusWithoutDurationCounter==8) {
                    
                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"""
                                             phase4_isWhiteWound:{string.Join(",",phase4_isWhiteWound)}
                                             """);
                        
                    }
                    
                }
                
            }

        }
        
        [ScriptMethod(name:"P4 生死之境 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:50070"])]

        public void P4_生死之境_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2,48);
            currentProperties.Owner=sourceId;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(2);
            currentProperties.DestoryAt=5500;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"P4 生者黑暗光与死者黑暗光 (安全区范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(50068|50069)$"])]

        public void P4_生者黑暗光与死者黑暗光_安全区范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            if(phase4_isBeyondDeath[myIndex]) {

                if(phase4_isWhiteWound[myIndex]) {
                    
                    if(string.Equals(@event["ActionId"],"50069")) {

                        return;

                    }
                    
                }

                else {
                    
                    if(string.Equals(@event["ActionId"],"50068")) {

                        return;

                    }
                    
                }
                
            }

            else {
                
                if(phase4_isWhiteWound[myIndex]) {
                    
                    if(string.Equals(@event["ActionId"],"50068")) {

                        return;

                    }
                    
                }

                else {
                    
                    if(string.Equals(@event["ActionId"],"50069")) {

                        return;

                    }
                    
                }
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(21,47);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=5500;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

        }

        #endregion

        #region Major_Phase_5
        
        [ScriptMethod(name:"P5 (阶段控制)",
            eventType:EventTypeEnum.Targetable,
            eventCondition:["DataId:19511"],
            userControl:false)]

        public void P5_阶段控制(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["Targetable"],"True")) {

                return;

            }
            
            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }

            majorPhase=5;
            phase=1;
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug($"majorPhase={majorPhase}\nphase={phase}");
                
            }

        }

        #endregion
        
        #region Commons

        public static bool convertObjectIdToDecimal(string? rawObjectId,out ulong result) {
            
            result=0;

            if(string.IsNullOrWhiteSpace(rawObjectId)) {
                
                return false;
                
            }

            string objectId=rawObjectId.Trim();
            
            objectId=objectId.StartsWith("0x",StringComparison.OrdinalIgnoreCase)?objectId.Substring(2):objectId;
            
            return ulong.TryParse(objectId,System.Globalization.NumberStyles.HexNumber,null,out result);
            
        }
        
        public static bool convertHandleIdToDecimal(string? rawHandle,out nint result) {
            
            result=0;

            if(string.IsNullOrWhiteSpace(rawHandle)) {
                
                return false;
                
            }

            string handleId=rawHandle.Trim();

            handleId=handleId.StartsWith("0x",StringComparison.OrdinalIgnoreCase)?handleId.Substring(2):handleId;

            return nint.TryParse(handleId,System.Globalization.NumberStyles.HexNumber,null,out result);
            
        }
        
        public static bool convertStringToSignedInteger(string? rawString,out int result) {
    
            result=0;

            if(string.IsNullOrWhiteSpace(rawString)) {
        
                return false;
        
            }

            string cleanString=rawString.Trim();

            return int.TryParse(cleanString,System.Globalization.NumberStyles.Integer,null,out result);
    
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
        
        #region Unsafe_Commons

        public static unsafe void adjustVisibility(ScriptAccessory accessory,IGameObject? targetIGameObject,bool isVisible,int recoveryDelay=-1) {

            if(targetIGameObject==null||!targetIGameObject.IsValid()) {

                return;
                    
            }

            try {
                
                var targetGameObject=((GameObject*)(targetIGameObject.Address));
                var originalVisibility=targetGameObject->RenderFlags;

                if(isVisible) {

                    targetGameObject->RenderFlags=VisibilityFlags.None;

                }

                else {
                    
                    targetGameObject->RenderFlags=VisibilityFlags.Model;
                    
                }
                
                if(recoveryDelay<=0) {
                
                    return;
                
                }
                
                System.Threading.Tasks.Task.Delay(recoveryDelay).ContinueWith(_=> {

                    if(targetIGameObject==null||!targetIGameObject.IsValid()) {
                        
                        return;
                        
                    }

                    try {
                        
                        var targetGameObject=((GameObject*)(targetIGameObject.Address));
                        
                        targetGameObject->RenderFlags=originalVisibility;
                        
                    } catch(Exception e) {
                        
                        accessory.Log.Error(e.ToString());
                        
                    }
                    
                });
                
            } catch(Exception e) {
                
                accessory.Log.Error(e.ToString());
                
            }
                
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