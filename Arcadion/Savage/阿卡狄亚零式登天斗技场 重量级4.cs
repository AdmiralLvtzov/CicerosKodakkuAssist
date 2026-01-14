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
        version:"0.0.0.13",
        note:scriptNotes,
        author:"Cicero 灵视")]

    public class AAC_Heavyweight_M4_Savage
    {
        
        public const string scriptNotes=
            """
            阿卡狄亚零式登天斗技场重量级4(也就是M12S)的脚本。
            
            门神主要部分已经完工,本体尚未开始施工。作者正在加班加点!
            
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
        [UserSetting("通用 启用团灭搞怪")]
        public bool enableWipeShenanigans { get; set; } = false;
        [UserSetting("调试 启用调试日志并输出到Dalamud日志中")]
        public bool enableDebugLogging { get; set; } = false;
        [UserSetting("调试 忽略所有阶段检查(调试用)")]
        public bool skipPhaseChecks { get; set; } = false;
        
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

        #endregion
        
        #region Variables_And_Semaphores
        
        private volatile bool isInMajorPhase1=true;
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
         
        */

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
        
        #endregion
        
        #region Constants_And_Locks

        private static readonly Vector3 ARENA_CENTER=new Vector3(100,0,100);
        // The arena is ± 15 vertically, ± 20 horizontally.
        // The pattern squares on the arena are all 5x5.
        // Therefore, many mechanism positions could be calculated without precise geometric construction.
        
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
        private static ImmutableList<Vector3> SPREAD_ON_RIGHT=[new Vector3(110.733f,0,86),new Vector3(112.164f,0,92.969f),new Vector3(119,0,86),new Vector3(119,0,94.120f)];
        // The link to the related geometric constructions: https://www.geogebra.org/calculator/pgc9s43t
        // It's in Chinese, since I completed the Chinese version of the script first, unlike M8S.
        
        private static readonly Vector3 LEFT_ARENA_CENTER=new Vector3(90,0,100);
        private static readonly Vector3 RIGHT_ARENA_CENTER=new Vector3(110,0,100);
        private static readonly Vector3 LEFT_KNOCK_BACK_CENTER=new Vector3(82,0,89);
        private static readonly Vector3 RIGHT_KNOCK_BACK_CENTER=new Vector3(118,0,89);
        
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

        }

        public class act2PartyType {

            public bool isAlpha=true;
            public int rawOrder=0;

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
        
        #endregion
        
        #region Initialization

        public void Init(ScriptAccessory accessory) {
            
            accessory.Method.RemoveDraw(".*");
            
            Variable_And_Semaphore_Initialization();
            
            targetIconBaseId=null;

        }

        private void Variable_And_Semaphore_Initialization() {
            
            isInMajorPhase1=true;
            currentPhase=0;
            
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
            eventCondition:["DataId:19195|19202"],
            suppress:13000,
            userControl:false)]

        public void Shenanigans(Event @event,ScriptAccessory accessory) {
            
            if(!enableWipeShenanigans) {

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
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Name="门神_致命灾变_场地分割线";
            currentProperties.Scale=new(0.25f,30);
            currentProperties.Position=ARENA_CENTER;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=23500;
        
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
            currentProperties.Color=((sphere[leftSphere].isGreen)?(colourOfGreenSpheres.V4.WithW(3)):(colourOfPurpleSpheres.V4.WithW(3)));
            currentProperties.DestoryAt=10750+625*currentRound;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(1);
            currentProperties.Position=new Vector3(rightX,0,88+2.5f*currentRound);
            currentProperties.Color=((sphere[rightSphere].isGreen)?(colourOfGreenSpheres.V4.WithW(3)):(colourOfPurpleSpheres.V4.WithW(3)));
            currentProperties.DestoryAt=10750+625*currentRound;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
        
        }
        
        [ScriptMethod(name:"门神 致命灾变 (范围)",
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
        
        [ScriptMethod(name:"门神 致命灾变 (指路)",
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
            eventCondition:["ActionId:46267"])]
    
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
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            slaughtershedIconSemaphore2.WaitOne();
            
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
        
        [ScriptMethod(name:"门神 喋血 (范围)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:regex:^(46283|46285|46284|46286)$"])]
    
        public void 门神_喋血_范围(Event @event,ScriptAccessory accessory) {

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
                currentProperties.Position=LEFT_ARENA_CENTER;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(20,30);
                currentProperties.Position=RIGHT_ARENA_CENTER;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(20,30);
                currentProperties.Position=RIGHT_ARENA_CENTER;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
            }
            
            if(string.Equals(@event["ActionId"],"46285")) {
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(20,30);
                currentProperties.Position=RIGHT_ARENA_CENTER;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(20,30);
                currentProperties.Position=LEFT_ARENA_CENTER;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(20,30);
                currentProperties.Position=LEFT_ARENA_CENTER;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Straight,currentProperties);
                
            }

            if(string.Equals(@event["ActionId"],"46284")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(2,21.095f);
                currentProperties.Position=LEFT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=ARENA_CENTER;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(8.944f,8.944f);
                currentProperties.Position=LEFT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=new Vector3(LEFT_KNOCK_BACK_CENTER.X-1,0,LEFT_KNOCK_BACK_CENTER.Z-1);
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(2,21.095f);
                currentProperties.Position=RIGHT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=ARENA_CENTER;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(8.944f,8.944f);
                currentProperties.Position=RIGHT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=new Vector3(RIGHT_KNOCK_BACK_CENTER.X+1,0,RIGHT_KNOCK_BACK_CENTER.Z-1);
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                
            }
            
            if(string.Equals(@event["ActionId"],"46286")) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(2,21.095f);
                currentProperties.Position=RIGHT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=ARENA_CENTER;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(8.944f,8.944f);
                currentProperties.Position=RIGHT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=new Vector3(RIGHT_KNOCK_BACK_CENTER.X+1,0,RIGHT_KNOCK_BACK_CENTER.Z-1);
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.Delay=7500;
                currentProperties.DestoryAt=6125;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(2,21.095f);
                currentProperties.Position=LEFT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=ARENA_CENTER;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(8.944f,8.944f);
                currentProperties.Position=LEFT_KNOCK_BACK_CENTER;
                currentProperties.TargetPosition=new Vector3(LEFT_KNOCK_BACK_CENTER.X-1,0,LEFT_KNOCK_BACK_CENTER.Z-1);
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.Delay=13625;
                currentProperties.DestoryAt=4625;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);
                
            }

        }
        
        #endregion
        
        #region Major_Phase_2

        
        
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