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
        version:"0.0.2.6",
        note:scriptNotes,
        author:"Cicero 灵视")]

    public class Dancing_Mad_Ultimate
    {
        
        public const string scriptNotes=
            """
            妖星乱舞绝境战的脚本。
            
            脚本正在施工中。绘制部分的进度为P3刚刚开始,指路部分尚未开始施工,适配的攻略也尚未确定。
            如果指路不适配你采用的攻略,可以在方法设置中将相关的指路关闭。所有指路方法均标注有"(指路)"后缀。
            
            支持进行小队排序测试,可以在聊天框中输入/e kuwutest来检查小队排序是否正确。
            输入/e kuwuclear清除小队排序测试产生的目标标记。

            如果在使用过程中遇到了异常,请先检查可达鸭本体与脚本是否都更新到了最新版本,小队职能是否已正确设置,异常是否可以稳定复现。
            如果上述三点都没有问题,请带着A Realm Recorded插件的录像文件在可达鸭Discord内联系@_publius_cornelius_scipio_反馈异常。
            
            特别致谢:
                Karlin - 紧急加班做好了绘制淡出与屏蔽技能特效的代码轮子,not all heroes wear capes.
                RyougiMio - 提供了P2咏唱危机的类型数据与塔的EnvControl数据。
                南云铁虎 - 提供了P2消灭之脚、破坏之翼与异三角的绘制数据。
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
        [UserSetting("通用 连环环陷阱状态消失前的绘制持续时间(秒)")]
        public double 连环环陷阱Duration { get; set; } = 5; // To be changed in the future.
        [UserSetting("调试 启用调试日志并输出到Dalamud日志中")]
        public bool enableDebugLogging { get; set; } = false;
        [UserSetting("调试 忽略所有方法中的阶段检查")]
        public bool skipPhaseChecks { get; set; } = false;
        [UserSetting("调试 在转阶段时保留绘制")]
        public bool preserveDrawingsWhileSwitchingPhase { get; set; } = false;
        
        // ----- Major Phase 1 -----
        
        // ----- End Of Major Phase 1 -----
        
        // ----- Major Phase 2 -----
        
        [UserSetting("P2 遗弃末世 塔辅助线的颜色")]
        public ScriptColor phase2sub2_colourOfAuxiliaryLine { get; set; } = new() { V4 = new Vector4(0,1,1, 1) }; // Blue by default.
        
        // ----- End Of Major Phase 2 -----
        
        // ----- Major Phase 3 -----
        
        // ----- End Of Major Phase 3 -----

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
            Phase 2 - [Bowels of Agony 深层痛楚,
            Phase 3 - Placeholder 占位符

        */
        
        // ----- Major Phase 1 -----
        
        private volatile bool phase1_fakeFlagrantFire=false;
        private System.Threading.AutoResetEvent phase1_flagrantFireObfuscationSemaphore=new System.Threading.AutoResetEvent(false);
        private HashSet<ulong> phase1_partyMembersWithFlagrantFire=new HashSet<ulong>();
        private volatile bool phase1_stackFlagrantFireIcon=false;
        private System.Threading.AutoResetEvent phase1_flagrantFireIconSemaphore=new System.Threading.AutoResetEvent(false);
        
        private volatile int phase1sub2_玄乎乎魔法Counter=0; // To be changed in the future.
        private System.Threading.AutoResetEvent phase1sub2_waveCannonSemaphore=new System.Threading.AutoResetEvent(false);
        
        private volatile bool phase1sub3_isFirstHalf=true;
        
        // ----- End Of Major Phase 1 -----
        
        // ----- Major Phase 2 -----
        
        private Phase2Sub2_IconTypes[] phase2sub2_iconType=Enumerable.Range(0,8).Select(i=>Phase2Sub2_IconTypes.UNKNOWN).ToArray();
        private List<int> phase2sub2_towers=new List<int>();
        private ConcurrentDictionary<ulong,int> phase2sub2_drawingCounter=new ConcurrentDictionary<ulong,int>();
        private volatile int phase2sub2_towerCounter=0; // Its read-write lock is PHASE2_SUB2_TOWER_COUNTER_LOCK.
        
        private volatile int phase2sub3_trineCounter=0; // Its read-write lock is PHASE2_SUB3_TRINE_COUNTER_LOCK.
        
        // ----- End Of Major Phase 2 -----
        
        // ----- Major Phase 3 -----
        
        private volatile int phase3sub2_crystalCounter=0; // Its read-write lock is PHASE3_SUB2_CRYSTAL_COUNTER_LOCK.
        private Vector3 phase3sub2_windCrystalPosition=ARENA_CENTER;
        private Vector3 phase3sub2_fireCrystalPosition=ARENA_CENTER;
        private Vector3 phase3sub2_waterCrystalPosition=ARENA_CENTER;
        private System.Threading.ManualResetEvent phase3sub2_crystalSemaphore=new System.Threading.ManualResetEvent(false);
        
        // ----- End Of Major Phase 3 -----
        
        #endregion
        
        #region Constants_And_Locks
        
        private const int COMMON_INTERVAL=2500;
        private const int MAXIMUM_DURATION=7200000;
        private const double COMMON_DEVIATION=1;
        
        private const int SHENANIGAN_DELAY=5000;
        private const int SHENANIGAN_DURATION=10000;
        private const int PARTY_TEST_DURATION=20000;
        
        private static readonly Vector3 ARENA_CENTER=new Vector3(100,0,100);
        private const int ARENA_RADIUS=20;

        private static readonly Vector3 PHASE2_SUB2_RAW_TOWER_POSITION=new Vector3(100,0,92);
        private const int PHASE2_SUB2_TOWER_RADIUS=4;
        private readonly object PHASE2_SUB2_TOWER_COUNTER_LOCK=new object();
        
        private readonly object PHASE2_SUB3_TRINE_COUNTER_LOCK=new object();
        
        private readonly object PHASE3_SUB2_CRYSTAL_COUNTER_LOCK=new object();
        
        #endregion
        
        #region Enumerations_And_Classes
        
        public enum PartyTestChannels {
            
            不发送到任何频道,
            默语频道_仅自己可见,
            小队频道_所有队员可见

        }
        
        public enum Phase2Sub2_IconTypes {
            
            FAN,
            SPREAD,
            STACK,
            UNKNOWN

        }
        
        #endregion
        
        #region Initialization
        
        public void Init(ScriptAccessory accessory) {
            
            accessory.Method.RemoveDraw(".*");
            
            VariableAndSemaphoreInitialization();
            
            if(enableShenanigans) {

                shenaniganSemaphore.Set();

            }

        }

        private void VariableAndSemaphoreInitialization() {

            majorPhase=1;
            phase=1;
            
            // ----- Major Phase 1 -----

            phase1_fakeFlagrantFire=false;
            phase1_flagrantFireObfuscationSemaphore.Reset();
            phase1_partyMembersWithFlagrantFire.Clear();
            phase1_stackFlagrantFireIcon=false;
            phase1_flagrantFireIconSemaphore.Reset();
            
            phase1sub2_玄乎乎魔法Counter=0;
            phase1sub2_waveCannonSemaphore.Reset();

            phase1sub3_isFirstHalf=true;

            // ----- End Of Major Phase 1 -----

            // ----- Major Phase 2 -----

            for(int i=0;i<phase2sub2_iconType.Length;++i)phase2sub2_iconType[i]=Phase2Sub2_IconTypes.UNKNOWN;
            phase2sub2_towers.Clear();
            phase2sub2_drawingCounter.Clear();
            phase2sub2_towerCounter=0;
            
            phase2sub3_trineCounter=0;

            // ----- End Of Major Phase 2 -----

            // ----- Major Phase 3 -----

            phase3sub2_crystalCounter=0;
            phase3sub2_windCrystalPosition=ARENA_CENTER;
            phase3sub2_fireCrystalPosition=ARENA_CENTER;
            phase3sub2_waterCrystalPosition=ARENA_CENTER;
            phase3sub2_crystalSemaphore.Reset();

            // ----- End Of Major Phase 3 -----

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
            
            if(!string.Equals(processedText,"kuwutest")) {

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
            
            if(!string.Equals(processedText,"kuwuclear")) {

                return;

            }
            
            accessory.Method.MarkClear();
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug("Now trying to clear party test signs...");
                
            }

        }
        
        [ScriptMethod(name:"通用 连环环陷阱 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:5078"])]

        public void 通用_连环环陷阱_范围(Event @event,ScriptAccessory accessory) {
            
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
            
            int standardDuration=((int)(连环环陷阱Duration*1000));
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

            currentProperties.Name=$"通用_连环环陷阱_范围_{targetId}";
            currentProperties.Scale=new(6);
            currentProperties.Owner=targetId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.Delay=delay;
            currentProperties.DestoryAt=duration;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"通用 连环环陷阱 (击退指示)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:5078"])]

        public void 通用_连环环陷阱_击退指示(Event @event,ScriptAccessory accessory) {
            
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
            
            int standardDuration=((int)(连环环陷阱Duration*1000));
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

            currentProperties.Name=$"通用_连环环陷阱_击退指示_{targetId}";
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
        
        [ScriptMethod(name:"通用 连环环陷阱 (范围与击退指示清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:5078"],
            userControl:false)]

        public void 通用_连环环陷阱_范围与击退指示清除(Event @event,ScriptAccessory accessory) {
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"通用_连环环陷阱_范围_{targetId}");
            accessory.Method.RemoveDraw($"通用_连环环陷阱_击退指示_{targetId}");
            
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
        
        [ScriptMethod(name:"P1 呼啦啦爆炎 (数据收集1)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:regex:^(02A1|02A2|02A3|02A4|02A5|02A6)$"],
            userControl:false)]

        public void P1_呼啦啦爆炎_数据收集1(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

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

                if(targetObject.DataId!=19504) {

                    return;

                }
                
            }
            
            string log=$"@event[\"Id\"]={@event["Id"]}, ";
            
            if(string.Equals(@event["Id"],"02A1")) {

                phase1_fakeFlagrantFire=true;
                phase1_flagrantFireObfuscationSemaphore.Set();

                log+="fake Flagrant Fire";

            }
            
            if(string.Equals(@event["Id"],"02A2")) {
                
                phase1_fakeFlagrantFire=false;
                phase1_flagrantFireObfuscationSemaphore.Set();

                log+="real Flagrant Fire";

            }
            
            if(string.Equals(@event["Id"],"02A3")) {

                log+="fake 扩大大冰封"; // To be changed in the future.

            }
            
            if(string.Equals(@event["Id"],"02A4")) {
                
                log+="real 扩大大冰封"; // To be changed in the future.

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
        
        [ScriptMethod(name:"P1 呼啦啦爆炎 (数据收集2)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:regex:^(0080|007F)$"],
            userControl:false)]

        public void P1_呼啦啦爆炎_数据收集2(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }

            lock(phase1_partyMembersWithFlagrantFire) {
                
                phase1_partyMembersWithFlagrantFire.Add(targetId);

                if(phase1_partyMembersWithFlagrantFire.Count==2) {
                    
                    if(string.Equals(@event["Id"],"0080")) {

                        phase1_stackFlagrantFireIcon=true;
                        phase1_flagrantFireIconSemaphore.Set();
                        
                        if(enableDebugLogging) {
                        
                            accessory.Log.Debug($"""
                                                 phase1_stackFlagrantFireIcon={phase1_stackFlagrantFireIcon}
                                                 phase1_partyMembersWithFlagrantFire:{string.Join(",",phase1_partyMembersWithFlagrantFire)}
                                                 """);
                        
                        }

                    }
                    
                }
                
                if(phase1_partyMembersWithFlagrantFire.Count==1) {
                    
                    if(string.Equals(@event["Id"],"007F")) {

                        phase1_stackFlagrantFireIcon=false;
                        phase1_flagrantFireIconSemaphore.Set();
                        
                        if(enableDebugLogging) {
                        
                            accessory.Log.Debug($"""
                                                 phase1_stackFlagrantFireIcon={phase1_stackFlagrantFireIcon}
                                                 phase1_partyMembersWithFlagrantFire:{string.Join(",",phase1_partyMembersWithFlagrantFire)}
                                                 """);
                        
                        }

                    }
                    
                }
                
            }

        }
        
        [ScriptMethod(name:"P1 呼啦啦爆炎 (范围)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:regex:^(0080|007F)$"],
            suppress:COMMON_INTERVAL)]

        public void P1_呼啦啦爆炎_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            bool signalled=phase1_flagrantFireObfuscationSemaphore.WaitOne(COMMON_INTERVAL);

            if(!signalled) {

                return;

            }
            
            signalled=phase1_flagrantFireIconSemaphore.WaitOne(COMMON_INTERVAL);

            if(!signalled) {

                return;

            }

            if(phase1_stackFlagrantFireIcon) {

                if(!phase1_fakeFlagrantFire) {
                    
                    foreach(ulong i in phase1_partyMembersWithFlagrantFire) {
                        
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
                
                if(!phase1_fakeFlagrantFire) {
                    
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
        
        [ScriptMethod(name:"P1 呼啦啦爆炎 (技能特效屏蔽)",
            eventType:EventTypeEnum.VfxEvent,
            eventCondition:["Id:regex:^(128|127)$"])]

        public void P1_呼啦啦爆炎_技能特效屏蔽(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

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
        
        [ScriptMethod(name:"P1 扩大大冰封 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47768|47774)$"])]

        public void P1_扩大大冰封_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(40);
            currentProperties.Radian=float.Pi/2;
            currentProperties.Owner=sourceId;
            currentProperties.DestoryAt=5000;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

        }
        
        [ScriptMethod(name:"P1 扩大大冰封 (技能特效屏蔽)",
            eventType:EventTypeEnum.VfxEvent,
            eventCondition:["Id:737"])]

        public void P1_扩大大冰封_技能特效屏蔽(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

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
        
        [ScriptMethod(name:"P1 劈啪啪暴雷 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47775|47777)$"])]

        public void P1_劈啪啪暴雷_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(10,40);
            currentProperties.Owner=sourceId;
            currentProperties.DestoryAt=5000;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"P1 劈啪啪暴雷 (技能特效屏蔽)",
            eventType:EventTypeEnum.VfxEvent,
            eventCondition:["Id:171"])]

        public void P1_劈啪啪暴雷_技能特效屏蔽(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

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
        
        [ScriptMethod(name:"P1 众神之像 (阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:48370"],
            userControl:false)]
    
        public void P1_众神之像_阶段控制(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            phase1_fakeFlagrantFire=false;
            phase1_flagrantFireObfuscationSemaphore.Reset();
            phase1_partyMembersWithFlagrantFire.Clear();
            phase1_stackFlagrantFireIcon=false;
            phase1_flagrantFireIconSemaphore.Reset();

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

            Interlocked.Increment(ref phase1sub2_玄乎乎魔法Counter);

            if(phase1sub2_玄乎乎魔法Counter==1) {

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
        
        [ScriptMethod(name:"P2 遗弃末世 咏唱危机 (数据收集与范围1)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:regex:^(02CD|02CC|02CB)$"])]

        public void P2_遗弃末世_咏唱危机_数据收集与范围1(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            lock(phase2sub2_iconType) {
                
                if(string.Equals(@event["Id"],"02CD")) {

                    phase2sub2_iconType[targetIndex]=Phase2Sub2_IconTypes.FAN;

                }
                
                if(string.Equals(@event["Id"],"02CC")) {

                    phase2sub2_iconType[targetIndex]=Phase2Sub2_IconTypes.SPREAD;

                }
                
                if(string.Equals(@event["Id"],"02CB")) {

                    phase2sub2_iconType[targetIndex]=Phase2Sub2_IconTypes.STACK;

                }
                
                lock(phase2sub2_drawingCounter) {
                
                    int lastDrawing=phase2sub2_drawingCounter.GetOrAdd(targetId,0);
                
                    accessory.Method.RemoveDraw($"P2_遗弃末世_范围_{targetId}_{lastDrawing}_0");
                    accessory.Method.RemoveDraw($"P2_遗弃末世_范围_{targetId}_{lastDrawing}_1");

                    Interlocked.Increment(ref lastDrawing);
                    phase2sub2_drawingCounter[targetId]=lastDrawing;

                    lock(phase2sub2_towers) {

                        for(int i=0;i<int.Min(2,phase2sub2_towers.Count);++i) {

                            switch(phase2sub2_iconType[targetIndex]) {

                                case Phase2Sub2_IconTypes.FAN: {
                                    
                                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
                                    currentProperties.Name=$"P2_遗弃末世_范围_{targetId}_{lastDrawing}_{i}";
                                    currentProperties.Scale=new(40);
                                    currentProperties.Radian=float.Pi/2;
                                    currentProperties.Owner=targetId;
                                    currentProperties.TargetResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                                    currentProperties.TargetOrderIndex=1;
                                    currentProperties.FadeCentrePosition=rotatePosition(PHASE2_SUB2_RAW_TOWER_POSITION,ARENA_CENTER,Math.PI/4*(phase2sub2_towers[i]-1));
                                    currentProperties.FadeDistance=PHASE2_SUB2_TOWER_RADIUS;
                                    currentProperties.FadeMode=FadeMode.OmenCentre;
                                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                                    currentProperties.DestoryAt=MAXIMUM_DURATION;

                                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

                                    break;

                                }
                                
                                case Phase2Sub2_IconTypes.SPREAD: {
                                    
                                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
                                    currentProperties.Name=$"P2_遗弃末世_范围_{targetId}_{lastDrawing}_{i}";
                                    currentProperties.Scale=new(5);
                                    currentProperties.Owner=targetId;
                                    currentProperties.FadeCentrePosition=rotatePosition(PHASE2_SUB2_RAW_TOWER_POSITION,ARENA_CENTER,Math.PI/4*(phase2sub2_towers[i]-1));
                                    currentProperties.FadeDistance=PHASE2_SUB2_TOWER_RADIUS;
                                    currentProperties.FadeMode=FadeMode.OmenCentre;
                                    currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                                    currentProperties.DestoryAt=MAXIMUM_DURATION;

                                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);

                                    break;

                                }
                                
                                case Phase2Sub2_IconTypes.STACK: {
                                    
                                    currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
                                    currentProperties.Name=$"P2_遗弃末世_范围_{targetId}_{lastDrawing}_{i}";
                                    currentProperties.Scale=new(5);
                                    currentProperties.Owner=targetId;
                                    currentProperties.FadeCentrePosition=rotatePosition(PHASE2_SUB2_RAW_TOWER_POSITION,ARENA_CENTER,Math.PI/4*(phase2sub2_towers[i]-1));
                                    currentProperties.FadeDistance=PHASE2_SUB2_TOWER_RADIUS;
                                    currentProperties.FadeMode=FadeMode.OmenCentre;
                                    currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                                    currentProperties.DestoryAt=MAXIMUM_DURATION;

                                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);

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

                }
                
            }

        }
        
        [ScriptMethod(name:"P2 遗弃末世 咏唱危机 (数据收集与范围2)",
            eventType:EventTypeEnum.EnvControl,
            eventCondition:["Flag:2"])]

        public void P2_遗弃末世_咏唱危机_数据收集与范围2(Event @event,ScriptAccessory accessory) {
            
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
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            lock(phase2sub2_towers) {

                if(phase2sub2_towers.Count>=2) {
                    
                    phase2sub2_towers.Clear();

                }
                
                phase2sub2_towers.Add(index);

                if(phase2sub2_towers.Count==2) {
                    
                    lock(phase2sub2_iconType) {

                        for(int i=0;i<8;++i) {

                            if(phase2sub2_iconType[i]==Phase2Sub2_IconTypes.UNKNOWN) {

                                continue;

                            }
                            
                            ulong currentId=accessory.Data.PartyList[i];
                            int currentIndex=i;
                        
                            lock(phase2sub2_drawingCounter) {
                            
                                int lastDrawing=phase2sub2_drawingCounter.GetOrAdd(currentId,0);
                            
                                accessory.Method.RemoveDraw($"P2_遗弃末世_范围_{currentId}_{lastDrawing}_0");
                                accessory.Method.RemoveDraw($"P2_遗弃末世_范围_{currentId}_{lastDrawing}_1"); 
                            
                                Interlocked.Increment(ref lastDrawing);
                                phase2sub2_drawingCounter[currentId]=lastDrawing;
                                
                                for(int j=0;j<int.Min(2,phase2sub2_towers.Count);++j) {

                                    switch(phase2sub2_iconType[currentIndex]) {

                                        case Phase2Sub2_IconTypes.FAN: {
                                    
                                            currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
                                            currentProperties.Name=$"P2_遗弃末世_范围_{currentId}_{lastDrawing}_{j}";
                                            currentProperties.Scale=new(40);
                                            currentProperties.Radian=float.Pi/2;
                                            currentProperties.Owner=currentId;
                                            currentProperties.TargetResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
                                            currentProperties.TargetOrderIndex=1;
                                            currentProperties.FadeCentrePosition=rotatePosition(PHASE2_SUB2_RAW_TOWER_POSITION,ARENA_CENTER,Math.PI/4*(phase2sub2_towers[j]-1));
                                            currentProperties.FadeDistance=PHASE2_SUB2_TOWER_RADIUS;
                                            currentProperties.FadeMode=FadeMode.OmenCentre;
                                            currentProperties.Color=accessory.Data.DefaultDangerColor;
                                            currentProperties.DestoryAt=MAXIMUM_DURATION;

                                            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

                                            break;
                                        
                                        }
                                
                                        case Phase2Sub2_IconTypes.SPREAD: {
                                    
                                            currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
                                            currentProperties.Name=$"P2_遗弃末世_范围_{currentId}_{lastDrawing}_{j}";
                                            currentProperties.Scale=new(5);
                                            currentProperties.Owner=currentId;
                                            currentProperties.FadeCentrePosition=rotatePosition(PHASE2_SUB2_RAW_TOWER_POSITION,ARENA_CENTER,Math.PI/4*(phase2sub2_towers[j]-1));
                                            currentProperties.FadeDistance=PHASE2_SUB2_TOWER_RADIUS;
                                            currentProperties.FadeMode=FadeMode.OmenCentre;
                                            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                                            currentProperties.DestoryAt=MAXIMUM_DURATION;

                                            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);

                                            break;

                                        }
                                
                                        case Phase2Sub2_IconTypes.STACK: {
                                    
                                            currentProperties=accessory.Data.GetDefaultDrawProperties();
                                    
                                            currentProperties.Name=$"P2_遗弃末世_范围_{currentId}_{lastDrawing}_{j}";
                                            currentProperties.Scale=new(5);
                                            currentProperties.Owner=currentId;
                                            currentProperties.FadeCentrePosition=rotatePosition(PHASE2_SUB2_RAW_TOWER_POSITION,ARENA_CENTER,Math.PI/4*(phase2sub2_towers[j]-1));
                                            currentProperties.FadeDistance=PHASE2_SUB2_TOWER_RADIUS;
                                            currentProperties.FadeMode=FadeMode.OmenCentre;
                                            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                                            currentProperties.DestoryAt=MAXIMUM_DURATION;

                                            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);

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
                    
                        }

                    }
                        
                }
                    
            }
            
        }
        
        [ScriptMethod(name:"P2 遗弃末世 (塔辅助线) !!!临时函数,将来可能会移除!!!",
            eventType:EventTypeEnum.EnvControl,
            eventCondition:["Flag:2"])]

        public void P2_遗弃末世_塔辅助线_临时函数_将来可能会移除(Event @event,ScriptAccessory accessory) {
            
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
            currentProperties.Color=phase2sub2_colourOfAuxiliaryLine.V4.WithW(1);
            currentProperties.DestoryAt=10000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Straight,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(0.125f,8);
            currentProperties.Position=towerCenter;
            currentProperties.TargetPosition=relativeWest;
            currentProperties.Color=phase2sub2_colourOfAuxiliaryLine.V4.WithW(1);
            currentProperties.DestoryAt=10000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Straight,currentProperties);
            
        }
        
        [ScriptMethod(name:"P2 遗弃末世 咏唱危机 (范围清除)",
            eventType:EventTypeEnum.EnvControl,
            eventCondition:["Flag:2"],
            userControl:false)]

        public void P2_遗弃末世_咏唱危机_范围清除(Event @event,ScriptAccessory accessory) {
            
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

            lock(PHASE2_SUB2_TOWER_COUNTER_LOCK) {
                
                Interlocked.Increment(ref phase2sub2_towerCounter);

                if(phase2sub2_towerCounter==16) {
                
                    System.Threading.Tasks.Task.Delay(10000).ContinueWith(_=> {
                    
                        accessory.Method.RemoveDraw(@"^P2_遗弃末世_范围_.*$");
                    
                    });
                
                }
                
            }

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

                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                
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

            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

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

            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

        }
        
        [ScriptMethod(name:"P2 制裁之光 (阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47805"],
            userControl:false)]

        public void P2_制裁之光_阶段控制(Event @event,ScriptAccessory accessory) {
            
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
        
        [ScriptMethod(name:"P2 破坏之翼 (单翼范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47821|47822)$"])]

        public void P2_破坏之翼_单翼范围(Event @event,ScriptAccessory accessory) {
            
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
        
        [ScriptMethod(name:"P2 破坏之翼 (双翼范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:50311"])]

        public void P2_破坏之翼_双翼范围(Event @event,ScriptAccessory accessory) {
            
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
        
        [ScriptMethod(name:"P2 异三角 (数据收集与范围)",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:regex:^(2015154|2015155)$"])]

        public void P2_异三角_数据收集与范围(Event @event,ScriptAccessory accessory) {
            
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
            
            phase3sub2_crystalSemaphore.Reset();
            
            phase=2;
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug($"majorPhase={majorPhase}\nphase={phase}");
                
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
        
        [ScriptMethod(name:"P3 深层痛楚 混沌之炎 (范围清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:1600"],
            userControl:false)]

        public void P3_深层痛楚_混沌之炎_范围清除(Event @event,ScriptAccessory accessory) {
            
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
        
        [ScriptMethod(name:"P3 深层痛楚 混沌之水 (范围清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:1601"],
            userControl:false)]

        public void P3_深层痛楚_混沌之水_范围清除(Event @event,ScriptAccessory accessory) {
            
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
            
        }
        
        [ScriptMethod(name:"P3 深层痛楚 暴雷 (死刑范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:47881"])]

        public void P3_深层痛楚_暴雷_死刑范围(Event @event,ScriptAccessory accessory) {
            
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

            currentProperties.Scale=new(5);
            currentProperties.Owner=sourceId;
            currentProperties.CentreResolvePattern=PositionResolvePatternEnum.PlayerNearestOrder;
            currentProperties.CentreOrderIndex=1;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=8125;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

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