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

namespace CicerosKodakkuAssist.WeaponsRefrainUltimate.ChinaDataCenter
{

    [ScriptType(name:"究极神兵绝境战",
        territorys:[777],
        guid:"ba05255f-37df-413f-8ddb-f0a61a9bacbe",
        version:"0.0.1.4",
        note:scriptNotes,
        author:"Cicero 灵视")]

    public class Weapons_Refrain_Ultimate
    {
        
        public const string scriptNotes=
            """
            究极神兵绝境战的脚本。
            由于先前的究极神兵绝境战脚本(作者@baelixac)已经停止维护很久了,在最新版本的可达鸭上会出现编译错误,因此我决定从零完全重写这个副本的脚本。
            
            目前风神阶段已经完工,火神阶段完成了大约一半。施工进度随缘,可能很慢。

            适配的攻略是国服野队一套。
            如果指路不适配你采用的攻略,可以在方法设置中将相关的指路关闭。所有指路方法均标注有"(指路)"后缀。

            如果在使用过程中遇到了异常,请先检查可达鸭本体与脚本是否都更新到了最新版本,小队职能是否已正确设置,异常是否可以稳定复现。
            如果上述三点都没有问题,请带着A Realm Recorded插件的录像文件在可达鸭Discord内联系@_publius_cornelius_scipio_反馈异常。
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
        [UserSetting("调试 忽略所有方法中的阶段检查")]
        public bool skipPhaseChecks { get; set; } = false;
        [UserSetting("调试 在转阶段时保留绘制")]
        public bool preserveDrawingsWhileSwitchingPhase { get; set; } = false;
        
        // ----- Major Phase 1 -----
        
        [UserSetting("迦楼罗 D2粗略指路的颜色")]
        public ScriptColor phase1_colourOfM2ImpreciseGuidance { get; set; } = new() { V4 = new Vector4(0,1,1, 1) }; // Blue by default.
        [UserSetting("迦楼罗 第二次寒风之歌粗略范围的颜色")]
        public ScriptColor phase1_colourOfImpreciseRangeOfMistralSong { get; set; } = new() { V4 = new Vector4(0,1,1, 1) }; // Blue by default.
        
        // ----- End Of Major Phase 1 -----
        
        // ----- Major Phase 2 -----
        
        [UserSetting("伊弗利特 火狱之楔 指北针的颜色")]
        public ScriptColor phase2_colourOfNorthIndicator { get; set; } = new() { V4 = new Vector4(0,1,1, 1) }; // Blue by default.
        [UserSetting("伊弗利特 火狱之楔 标记顺序")]
        public bool phase2_enableNailOrderAssistance { get; set; } = false;
        
        // ----- End Of Major Phase 2 -----
        
        // ----- Major Phase 3 -----
        
        
        
        // ----- End Of Major Phase 3 -----
        
        // ----- Major Phase 4 -----
        
        
        
        // ----- End Of Major Phase 4 -----
        
        // ----- Major Phase 5 -----
        
        
        
        // ----- End Of Major Phase 5 -----

        #endregion
        
        #region Variables_And_Semaphores
        
        private volatile int majorPhase=1;
        private volatile int phase=1;
        
        /*

        Major Phase 1 - 迦楼罗:

            Phases are separated by Feather Rain.
            阶段由飞翎雨分隔。
        
        Major Phase 2 - 伊弗利特:
        
            Phase 1 - (~,First Incinerate 第一次烈焰焚烧)
            Phase 2 - [First Incinerate 第一次烈焰焚烧,First Eruption 第一次地火喷发]
            Phase 3 - Placeholder 占位符
            
        Major Phase 3 - 泰坦:

            Phase 1 - Placeholder 占位符
            Phase 2 - Placeholder 占位符
            Phase 3 - Placeholder 占位符
            
        Major Phase 4 - 无影拉哈布雷亚:

            Phase 1 - Placeholder 占位符
            Phase 2 - Placeholder 占位符
            Phase 3 - Placeholder 占位符
            
        Major Phase 5 - 究极神兵:

            Phase 1 - Placeholder 占位符
            Phase 2 - Placeholder 占位符
            Phase 3 - Placeholder 占位符

        */
        
        // ----- Major Phase 1 -----

        private volatile int phase1_slipstreamCounter=0;
        private ulong phase1_targetOfMistralSong=0;
        private System.Threading.AutoResetEvent phase1_mistralSongSemaphore=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase1_downburstSemaphore=new System.Threading.AutoResetEvent(false);

        private Vector3 phase1_gigastormPosition=ARENA_CENTER;
        private System.Threading.ManualResetEvent phase1_gigastormSemaphore=new System.Threading.ManualResetEvent(false);
        private int[] stackOfThermalLow=Enumerable.Range(0,8).Select(i=>0).ToArray();
        private bool[] phase1_hasEliminatedThermalLow=Enumerable.Range(0,8).Select(i=>false).ToArray();
        
        private bool[] phase1_tankBuster=Enumerable.Range(0,4).Select(i=>false).ToArray();
        
        private ConcurrentDictionary<ulong,int> phase1_mesohighDrawingCounter=new ConcurrentDictionary<ulong,int>();
        private volatile bool garudaHasWoken=false;
        
        // ----- End Of Major Phase 1 -----
        
        // ----- Major Phase 2 -----
        
        private bool[] phase2_initialSafeZone=Enumerable.Range(0,4).Select(i=>true).ToArray();
        private System.Threading.AutoResetEvent phase2_firstCrimsonCycloneSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile int phase2_radiantPlumeCounter=0;
        private System.Threading.AutoResetEvent phase2_radiantPlumeSemaphore=new System.Threading.AutoResetEvent(false);
        
        private System.Threading.AutoResetEvent phase2_firstIncinerateSemaphore=new System.Threading.AutoResetEvent(false);
        private bool[] phase2_infernalNailDeployed=Enumerable.Range(0,8).Select(i=>false).ToArray();
        private ulong[] phase2_infernalNailId=Enumerable.Range(0,8).Select(i=>((ulong)0)).ToArray();
        private volatile int phase2_infernalNailCounter=0;
        private int[] phase2_infernalNail=Enumerable.Range(0,4).Select(i=>-1).ToArray();
        private double phase2_temporaryRotation=0;
        private System.Threading.AutoResetEvent phase2_infernalNailSemaphore1=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase2_infernalNailSemaphore2=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase2_infernalNailSemaphore3=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent phase2_infernalNailSemaphore4=new System.Threading.AutoResetEvent(false);
        private volatile int phase2_infernalFetterDrawingCounter=0;
        private volatile int phase2_eruptionCounter=0; // Its read-write lock is phase2_eruptionCounterLock
        
        // ----- End Of Major Phase 2 -----
        
        // ----- Major Phase 3 -----
        
        
        
        // ----- End Of Major Phase 3 -----
        
        // ----- Major Phase 4 -----
        
        
        
        // ----- End Of Major Phase 4 -----
        
        // ----- Major Phase 5 -----
        
        
        
        // ----- End Of Major Phase 5 -----
        
        #endregion
        
        #region Constants_And_Locks
        
        private const int MAXIMUM_DURATION=7200000;
        private const int COMMON_INTERVAL=2500;
        
        private static readonly Vector3 ARENA_CENTER=new Vector3(100,0,100);
        // The arena is a circle with a radius of 19.5.
        
        private static readonly object phase2_eruptionCounterLock=new object();
        
        #endregion
        
        #region Enumerations_And_Classes
        
        
        
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
            
            phase1_slipstreamCounter=0;
            phase1_targetOfMistralSong=0;
            phase1_mistralSongSemaphore.Reset();
            phase1_downburstSemaphore.Reset();
            
            phase1_gigastormPosition=ARENA_CENTER;
            phase1_gigastormSemaphore.Reset();
            for(int i=0;i<stackOfThermalLow.Length;++i)stackOfThermalLow[i]=0;
            for(int i=0;i<phase1_hasEliminatedThermalLow.Length;++i)phase1_hasEliminatedThermalLow[i]=false;
            
            for(int i=0;i<phase1_tankBuster.Length;++i)phase1_tankBuster[i]=false;
            
            phase1_mesohighDrawingCounter.Clear();
            garudaHasWoken=false;

            // ----- End Of Major Phase 1 -----

            // ----- Major Phase 2 -----

            for(int i=0;i<phase2_initialSafeZone.Length;++i)phase2_initialSafeZone[i]=true;
            phase2_firstCrimsonCycloneSemaphore.Reset();
            phase2_radiantPlumeCounter=0;
            phase2_radiantPlumeSemaphore.Reset();

            phase2_firstIncinerateSemaphore.Reset();
            for(int i=0;i<phase2_infernalNailDeployed.Length;++i)phase2_infernalNailDeployed[i]=false;
            for(int i=0;i<phase2_infernalNailId.Length;++i)phase2_infernalNailId[i]=((ulong)0);
            phase2_infernalNailCounter=0;
            for(int i=0;i<phase2_infernalNail.Length;++i)phase2_infernalNail[i]=-1;
            phase2_temporaryRotation=0;
            phase2_infernalNailSemaphore1.Reset();
            phase2_infernalNailSemaphore2.Reset();
            phase2_infernalNailSemaphore3.Reset();
            phase2_infernalNailSemaphore4.Reset();
            phase2_infernalFetterDrawingCounter=0;
            phase2_eruptionCounter=0;

            // ----- End Of Major Phase 2 -----

            // ----- Major Phase 3 -----



            // ----- End Of Major Phase 3 -----

            // ----- Major Phase 4 -----



            // ----- End Of Major Phase 4 -----

            // ----- Major Phase 5 -----



            // ----- End Of Major Phase 5 -----

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
            eventCondition:["DataId:8722"],
            suppress:14000,
            userControl:false)]

        public void Shenanigans(Event @event,ScriptAccessory accessory) {
            
            if(!enableShenanigans) {

                return;

            }

            bool signalled=shenaniganSemaphore.WaitOne(14000);

            if(!signalled) {

                return;

            }

            System.Threading.Thread.Sleep(4000);
            
            string prompt=quotes[new System.Random().Next(0,quotes.Count)];

            if(!string.IsNullOrWhiteSpace(prompt)) {

                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,10000);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }

        }

        #endregion
        
        #region Global
        
        [ScriptMethod(name:"状态 低气压 (更新)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:1525"],
            userControl:false)]

        public void 状态_低气压_更新(Event @event,ScriptAccessory accessory) {

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }
            
            if(!convertStringToSignedInteger(@event["StackCount"], out var stackCount)) {
                
                return;
                
            }

            if(stackCount<0) {

                return;

            }

            int stackBefore=0;

            lock(stackOfThermalLow) {
                
                stackBefore=stackOfThermalLow[targetIndex];
                
                stackOfThermalLow[targetIndex]=stackCount;
                
            }

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"stackOfThermalLow[{targetIndex}]:{stackBefore}->{stackCount}");
                
            }

        }
        
        [ScriptMethod(name:"状态 低气压 (移除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:1525"],
            userControl:false)]

        public void 状态_低气压_移除(Event @event,ScriptAccessory accessory) {

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            bool anomalousStackCount=false;
            
            if(!convertStringToSignedInteger(@event["StackCount"], out var stackCount)) {

                anomalousStackCount=true;

            }

            if(stackCount<0) {

                anomalousStackCount=true;

            }

            bool recordMismatch=false;
            int expectedStack=0;

            lock(stackOfThermalLow) {
                
                expectedStack=stackOfThermalLow[targetIndex];
                
                stackOfThermalLow[targetIndex]=0;
                
            }
            
            if(!anomalousStackCount) {

                if(expectedStack!=stackCount) {

                    recordMismatch=true;

                }
                
            }

            else {

                recordMismatch=true;

            }

            if(enableDebugLogging) {

                if(anomalousStackCount) {
                    
                    accessory.Log.Debug($"stackOfThermalLow[{targetIndex}]:?->0\nanomalousStackCount={anomalousStackCount}\nexpectedStack={expectedStack}");
                    
                }

                else {

                    if(recordMismatch) {
                        
                        accessory.Log.Debug($"stackOfThermalLow[{targetIndex}]:{stackCount}->0\nexpectedStack={expectedStack}");
                        
                    }

                    else {
                        
                        accessory.Log.Debug($"stackOfThermalLow[{targetIndex}]:{stackCount}->0");
                        
                    }
                    
                }
                
            }

        }
        
        [ScriptMethod(name:"觉醒 (数据获取)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:1529"],
            userControl:false)]
    
        public void 觉醒_数据获取(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&majorPhase!=2&&majorPhase!=3&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }
            
            var targetObject=accessory.Data.Objects.SearchById(targetId);

            if(targetObject==null) {

                return;

            }

            switch(targetObject.DataId) {

                case 8722: {
                    
                    garudaHasWoken=true;

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"garudaHasWoken={garudaHasWoken}");
                        
                    }

                    break;

                }

                default: {

                    break;

                }
                
            }

        }
        
        [ScriptMethod(name:"通用 灼热 (范围)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:1578"])]

        public void 通用_灼热_范围(Event @event,ScriptAccessory accessory) {
            
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

            if(durationMilliseconds<=0||durationMilliseconds>MAXIMUM_DURATION) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"通用_灼热_范围_{targetId}";
            currentProperties.Scale=new(15);
            currentProperties.Owner=targetId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=durationMilliseconds;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
            
        }
        
        [ScriptMethod(name:"通用 灼热 (范围清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:1578"],
            userControl:false)]

        public void 通用_灼热_范围清除(Event @event,ScriptAccessory accessory) {
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"通用_灼热_范围_{targetId}");
            
        }
        
        #endregion
        
        #region Garuda
        
        [ScriptMethod(name:"迦楼罗 向东拉Boss (指示,仅MT)",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:8722"],
            suppress:COMMON_INTERVAL)]

        public void 迦楼罗_向东拉Boss_指示_仅MT(Event @event,ScriptAccessory accessory) {
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            System.Threading.Tasks.Task.Delay(4000).ContinueWith(_=> {
                
                if(majorPhase!=1&&!skipPhaseChecks) {

                    return;

                }

                if(phase!=1&&!skipPhaseChecks) {

                    return;

                }
                
                int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
                if(!isLegalPartyIndex(myIndex)) {

                    return;

                }

                if(myIndex!=0) {

                    return;

                }
            
                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Name="迦楼罗_向东拉Boss_指示_仅MT";
                currentProperties.Scale=new(2);
                currentProperties.Owner=sourceId;
                currentProperties.TargetPosition=new Vector3(116.25f,0,100);
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
                currentProperties.DestoryAt=MAXIMUM_DURATION;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
            });

        }
        
        [ScriptMethod(name:"迦楼罗 向东拉Boss (指示清除)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11091"],
            userControl:false)]

        public void 迦楼罗_向东拉Boss_指示清除(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }

            if(phase!=1&&!skipPhaseChecks) {

                return;

            }
            
            accessory.Method.RemoveDraw("迦楼罗_向东拉Boss_指示_仅MT");

        }
        
        [ScriptMethod(name:"迦楼罗 螺旋气流 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11091"])]

        public void 迦楼罗_螺旋气流_范围(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(12);
            currentProperties.Owner=sourceId;
            currentProperties.Radian=float.Pi/2;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=2500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 螺旋气流 (计数器)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11091"],
            userControl:false)]

        public void 迦楼罗_螺旋气流_计数器(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }

            Interlocked.Increment(ref phase1_slipstreamCounter);

            if(phase1_slipstreamCounter==2||phase1_slipstreamCounter==3) {

                phase1_downburstSemaphore.Set();

            }
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug($"phase1_slipstreamCounter={phase1_slipstreamCounter}");
                
            }

        }
        
        [ScriptMethod(name:"迦楼罗 第一次寒风之歌 (数据获取)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:0010"],
            userControl:false)]

        public void 迦楼罗_第一次寒风之歌_数据获取(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }

            if(phase!=1&&!skipPhaseChecks) {

                return;

            }

            if(phase1_targetOfMistralSong!=0) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }
            
            phase1_targetOfMistralSong=targetId;

            phase1_mistralSongSemaphore.Set();

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"phase1_targetOfMistralSong={phase1_targetOfMistralSong}");
                
            }

        }
        
        [ScriptMethod(name:"迦楼罗 第一次寒风之歌 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11091"])]

        public void 迦楼罗_第一次寒风之歌_范围(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }

            if(phase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }

            bool signalled=phase1_mistralSongSemaphore.WaitOne(COMMON_INTERVAL);
            
            if(!signalled) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(3.5f,40);
            currentProperties.Owner=sourceId;
            currentProperties.TargetObject=phase1_targetOfMistralSong;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=5250;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 第一次大龙卷风 (范围)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:11074"])]

        public void 迦楼罗_第一次大龙卷风_范围(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }

            if(phase!=1&&!skipPhaseChecks) {

                return;

            }

            if(!string.Equals(@event["TargetIndex"],"1")) {

                return;

            }
            
            Vector3 targetPosition=ARENA_CENTER;

            try {

                targetPosition=JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("TargetPosition deserialization failed.");

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(8);
            currentProperties.Position=targetPosition;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=18375;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 拉刺羽 (指示,仅ST)",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:8726"])]

        public void 迦楼罗_拉刺羽_指示_仅ST(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }

            if(phase!=1&&!skipPhaseChecks) {

                return;

            }
                
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            if(myIndex!=1) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="迦楼罗_拉刺羽_指示_仅ST";
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=sourceId;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.DestoryAt=MAXIMUM_DURATION;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 拉刺羽 (指示清除)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:0011"],
            userControl:false)]

        public void 迦楼罗_拉刺羽_指示清除(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }

            if(phase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var sourceObject=accessory.Data.Objects.SearchById(sourceId);

            if(sourceObject==null) {

                return;

            }

            if(sourceObject.DataId!=8726) {

                return;

            }
            
            accessory.Method.RemoveDraw("迦楼罗_拉刺羽_指示_仅ST");

        }
        
        [ScriptMethod(name:"迦楼罗 下行突风 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11091"])]

        public void 迦楼罗_下行突风_范围(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            bool signalled=phase1_downburstSemaphore.WaitOne(COMMON_INTERVAL);
            
            if(!signalled) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(12);
            currentProperties.Owner=sourceId;
            currentProperties.TargetResolvePattern=PositionResolvePatternEnum.OwnerEnmityOrder;
            currentProperties.TargetOrderIndex=1;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.Delay=2500;
            currentProperties.DestoryAt=3500;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 飞翎雨 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11085"])]

        public void 迦楼罗_飞翎雨_范围(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

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

            currentProperties.Scale=new(3);
            currentProperties.Position=effectPosition;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=1000;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 飞翎雨 (阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11085"],
            suppress:COMMON_INTERVAL,
            userControl:false)]

        public void 迦楼罗_飞翎雨_阶段控制(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }

            Interlocked.Increment(ref phase);
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug($"majorPhase={majorPhase}\nphase={phase}");
                
            }

        }
        
        [ScriptMethod(name:"迦楼罗 大暴风 (精确范围)",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:2002792"])]

        public void 迦楼罗_大暴风_精确范围(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=1&&phase!=2&&!skipPhaseChecks) {

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

            currentProperties.Scale=new(6);
            currentProperties.Position=sourcePosition;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.DestoryAt=23000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 大暴风 (数据获取)",
            eventType:EventTypeEnum.ObjectChanged,
            eventCondition:["DataId:2002792"],
            userControl:false)]

        public void 迦楼罗_大暴风_数据获取(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=1&&phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            bool signalled=phase1_gigastormSemaphore.WaitOne(0);

            if(signalled) {

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

            phase1_gigastormPosition=sourcePosition;

            phase1_gigastormSemaphore.Set();

        }
        
        [ScriptMethod(name:"迦楼罗 烈风刃 (指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11080"])]

        public void 迦楼罗_烈风刃_指路(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

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

            bool mtDodges=false;
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(5);
            currentProperties.Owner=targetId;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=2000;

            if(myIndex==0) {

                if(stackOfThermalLow[myIndex]<1) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    
                }

                else {
                    
                    currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);

                    mtDodges=true;

                }
                
            }

            else {
                
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                
            }
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

            if(myIndex!=targetIndex&&!mtDodges) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
            
                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetObject=accessory.Data.PartyList[targetIndex];
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=2000;
            
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"迦楼罗 低气压 (指路,仅ST)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:1525"])]

        public void 迦楼罗_低气压_指路_仅ST(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=1&&phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            if(myIndex!=1) {

                return;

            }

            if(phase1_hasEliminatedThermalLow[myIndex]) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            if(targetId!=accessory.Data.Me) {

                return;

            }
            
            if(!convertStringToSignedInteger(@event["StackCount"], out var stackCount)) {
                
                return;
                
            }

            if(stackCount!=2) {

                return;

            }
            
            bool signalled=phase1_gigastormSemaphore.WaitOne(35250);
            
            if(!signalled) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="迦楼罗_低气压_指路_仅ST";
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=phase1_gigastormPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=MAXIMUM_DURATION;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 低气压 (指路清除,仅ST)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:1525"],
            userControl:false)]

        public void 迦楼罗_低气压_指路清除_仅ST(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=1&&phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(phase1_hasEliminatedThermalLow[1]) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            if(targetIndex!=1) {

                return;

            }
            
            if(!convertStringToSignedInteger(@event["StackCount"], out var stackCount)) {
                
                return;
                
            }

            if(stackCount<2) {

                return;

            }

            phase1_hasEliminatedThermalLow[1]=true;
            
            accessory.Method.RemoveDraw("迦楼罗_低气压_指路_仅ST");

        }
        
        [ScriptMethod(name:"迦楼罗 低气压 (指路,远程DPS与治疗)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:1525"])]

        public void 迦楼罗_低气压_指路_远程DPS与治疗(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            if(!isRanged(myIndex)) {

                return;

            }

            if(phase1_hasEliminatedThermalLow[myIndex]) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            if(targetId!=accessory.Data.Me) {

                return;

            }
            
            if(!convertStringToSignedInteger(@event["StackCount"], out var stackCount)) {
                
                return;
                
            }

            if(stackCount!=1) {

                return;

            }
            
            bool signalled=phase1_gigastormSemaphore.WaitOne(35250);
            
            if(!signalled) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"迦楼罗_低气压_指路_远程DPS与治疗_{myIndex}";
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=phase1_gigastormPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=MAXIMUM_DURATION;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 低气压 (指路清除,远程DPS与治疗)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:1525"],
            userControl:false)]

        public void 迦楼罗_低气压_指路清除_远程DPS与治疗(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            if(!isRanged(targetIndex)) {

                return;

            }
            
            if(phase1_hasEliminatedThermalLow[targetIndex]) {

                return;

            }
            
            if(!convertStringToSignedInteger(@event["StackCount"], out var stackCount)) {
                
                return;
                
            }

            if(stackCount<1) {

                return;

            }

            phase1_hasEliminatedThermalLow[targetIndex]=true;
            
            accessory.Method.RemoveDraw($"迦楼罗_低气压_指路_远程DPS与治疗_{targetIndex}");

        }
        
        [ScriptMethod(name:"迦楼罗 低气压 (指路,仅D1)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:1525"])]

        public void 迦楼罗_低气压_指路_仅D1(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            if(myIndex!=4) {

                return;

            }

            if(phase1_hasEliminatedThermalLow[myIndex]) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            if(targetId!=accessory.Data.Me) {

                return;

            }
            
            if(!convertStringToSignedInteger(@event["StackCount"], out var stackCount)) {
                
                return;
                
            }

            if(stackCount!=2) {

                return;

            }
            
            bool signalled=phase1_gigastormSemaphore.WaitOne(35250);
            
            if(!signalled) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"迦楼罗_低气压_指路_近战DPS_{myIndex}";
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=phase1_gigastormPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=MAXIMUM_DURATION;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 低气压 (粗略指路,仅D2)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:1525"])]

        public void 迦楼罗_低气压_粗略指路_仅D2(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            if(myIndex!=5) {

                return;

            }

            if(phase1_hasEliminatedThermalLow[myIndex]) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            if(targetIndex!=4) {

                return;

            }
            
            if(!convertStringToSignedInteger(@event["StackCount"], out var stackCount)) {
                
                return;
                
            }

            if(stackCount<2) {

                return;

            }
            
            bool signalled=phase1_gigastormSemaphore.WaitOne(35250);
            
            if(!signalled) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"迦楼罗_低气压_指路_近战DPS_{myIndex}";
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=phase1_gigastormPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=phase1_colourOfM2ImpreciseGuidance.V4.WithW(1);
            currentProperties.DestoryAt=MAXIMUM_DURATION;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 低气压 (指路清除,近战DPS)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:1525"],
            userControl:false)]

        public void 迦楼罗_低气压_指路清除_近战DPS(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;

            }

            if(!isMeleeDps(targetIndex)) {

                return;

            }
            
            if(phase1_hasEliminatedThermalLow[targetIndex]) {

                return;

            }
            
            if(!convertStringToSignedInteger(@event["StackCount"], out var stackCount)) {
                
                return;
                
            }

            if(stackCount<2) {

                return;

            }

            phase1_hasEliminatedThermalLow[targetIndex]=true;
            
            accessory.Method.RemoveDraw($"迦楼罗_低气压_指路_近战DPS_{targetIndex}");

        }
        
        [ScriptMethod(name:"迦楼罗 向东北拉Boss (指示,仅MT)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:11093"],
            suppress:COMMON_INTERVAL)]

        public void 迦楼罗_向东北拉Boss_指示_仅MT(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&!skipPhaseChecks) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            if(myIndex!=0) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="迦楼罗_向东北拉Boss_指示_仅MT";
            currentProperties.Scale=new(2);
            currentProperties.Owner=sourceId;
            currentProperties.TargetPosition=new Vector3(108.132f,0,91.868f);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.DestoryAt=20500;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 第二次寒风之歌 (数据获取)",
            eventType:EventTypeEnum.PlayActionTimeline,
            eventCondition:["SourceDataId:8723"],
            userControl:false)]

        public void 迦楼罗_第二次寒风之歌_数据获取(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&phase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["Id"],"7747")) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            if(Vector3.Distance(sourcePosition,ARENA_CENTER)<18.5f) {

                return;

            }
            
            int discretizedPosition=discretizePosition(sourcePosition,ARENA_CENTER,4);

            lock(phase1_tankBuster) {

                phase1_tankBuster[discretizedPosition]=true;

            }

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"phase1_tankBusters[{discretizedPosition}]=true");
                
            }

        }
        
        [ScriptMethod(name:"迦楼罗 台风眼 (精确范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11090"])]

        public void 迦楼罗_台风眼_精确范围(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=4&&phase!=5&&!skipPhaseChecks) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                
            currentProperties.Scale=new(25);
            currentProperties.InnerScale=new(11.5f);
            currentProperties.Radian=float.Pi*2;
            currentProperties.Position=ARENA_CENTER;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=3000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Donut,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 第二次寒风之歌 (粗略范围)",
            eventType:EventTypeEnum.PlayActionTimeline,
            eventCondition:["SourceDataId:8723"])]

        public void 迦楼罗_第二次寒风之歌_粗略范围(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=3&&phase!=4&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["Id"],"7747")) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }

            if(Vector3.Distance(sourcePosition,ARENA_CENTER)<18.5f) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(3.5f,40);
            currentProperties.Position=sourcePosition;
            currentProperties.TargetPosition=ARENA_CENTER;
            currentProperties.Color=phase1_colourOfImpreciseRangeOfMistralSong.V4.WithW(1);
            currentProperties.DestoryAt=7125;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 第二次寒风之歌 (指路,DPS与治疗)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:0010"],
            suppress:COMMON_INTERVAL)]

        public void 迦楼罗_第二次寒风之歌_指路_DPS与治疗(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=4&&!skipPhaseChecks) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            if(!isDps(myIndex)&&!isHealer(myIndex)) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=ARENA_CENTER;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=5125;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 第二次寒风之歌 (指路,坦克)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11086"])]

        public void 迦楼罗_第二次寒风之歌_指路_坦克(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=4&&!skipPhaseChecks) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            if(!isTank(myIndex)) {

                return;

            }

            int myDiscretizedPosition=0;
            bool anomalousPosition=false;

            if(myIndex==0) {
                
                myDiscretizedPosition=0;

                while(!phase1_tankBuster[myDiscretizedPosition]) {

                    ++myDiscretizedPosition;

                    if(myDiscretizedPosition>=3) {

                        anomalousPosition=true;

                        break;

                    }

                }
                
            }
            
            if(myIndex==1) {
                
                myDiscretizedPosition=3;
                
                while(!phase1_tankBuster[myDiscretizedPosition]) {

                    --myDiscretizedPosition;

                    if(myDiscretizedPosition<=0) {

                        anomalousPosition=true;

                        break;

                    }

                }
                
            }

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"myIndex={myIndex}\ndiscretizedPosition={myDiscretizedPosition}\nanomalousPosition={anomalousPosition}");
                
            }

            if(anomalousPosition) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,89.5f),ARENA_CENTER,Math.PI/2*myDiscretizedPosition);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=3000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 第二次大龙卷风 (范围)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:11083"])]

        public void 迦楼罗_第二次大龙卷风_范围(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=4&&!skipPhaseChecks) {

                return;

            }

            if(!string.Equals(@event["TargetIndex"],"1")) {

                return;

            }
            
            Vector3 targetPosition=ARENA_CENTER;

            try {

                targetPosition=JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("TargetPosition deserialization failed.");

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(8);
            currentProperties.Position=targetPosition;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=6250;
            
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 中高压 (范围)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:0004"])]
    
        public void 迦楼罗_中高压_范围(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=5&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            lock(phase1_mesohighDrawingCounter) {
                
                int lastDrawing=phase1_mesohighDrawingCounter.GetOrAdd(sourceId,0);
            
                accessory.Method.RemoveDraw($"迦楼罗_中高压_范围_{sourceId}_{lastDrawing}");

                ++lastDrawing;
                phase1_mesohighDrawingCounter[sourceId]=lastDrawing;
            
                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Name=$"迦楼罗_中高压_范围_{sourceId}_{lastDrawing}";
                currentProperties.Scale=new(3);
                currentProperties.Owner=targetId;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.DestoryAt=MAXIMUM_DURATION;

                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
            }
        
        }
        
        [ScriptMethod(name:"迦楼罗 中高压 (范围清除)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:11081"],
            suppress:COMMON_INTERVAL,
            userControl:false)]
    
        public void 迦楼罗_中高压_范围清除(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=5&&phase!=6&&!skipPhaseChecks) {

                return;

            }
            
            accessory.Method.RemoveDraw(@"^迦楼罗_中高压_范围_.*$");
            
            phase1_mesohighDrawingCounter.Clear();
        
        }
        
        [ScriptMethod(name:"迦楼罗 中高压 (指路,ST与D3)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:0004"],
            suppress:5000+COMMON_INTERVAL)]
    
        public void 迦楼罗_中高压_指路_ST与D3(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=5&&!skipPhaseChecks) {

                return;

            }
            
            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            if(myIndex!=1&&myIndex!=6) {

                return;

            }

            Vector3 myPosition=ARENA_CENTER;

            if(myIndex==1) {

                myPosition=new Vector3(110.5f,0,100);

            }

            if(myIndex==6) {
                
                myPosition=new Vector3(89.5f,0,100);
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=5000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"迦楼罗 最后一次邪轮旋风 (范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11086"])]

        public void 迦楼罗_最后一次邪轮旋风_范围(Event @event,ScriptAccessory accessory) {

            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=6&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(garudaHasWoken) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(20);
                currentProperties.InnerScale=new(8.5f);
                currentProperties.Radian=float.Pi*2;
                currentProperties.Owner=sourceId;
                currentProperties.Color=accessory.Data.DefaultDangerColor;
                currentProperties.Delay=3000;
                currentProperties.DestoryAt=2125;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Donut,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(12);
                currentProperties.Owner=sourceId;
                currentProperties.TargetResolvePattern=PositionResolvePatternEnum.OwnerEnmityOrder;
                currentProperties.TargetOrderIndex=1;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.Delay=5125;
                currentProperties.DestoryAt=3500;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                
            }

            else {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(12);
                currentProperties.Owner=sourceId;
                currentProperties.TargetResolvePattern=PositionResolvePatternEnum.OwnerEnmityOrder;
                currentProperties.TargetOrderIndex=1;
                currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
                currentProperties.Delay=3000;
                currentProperties.DestoryAt=3500;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);
                
            }

        }
        
        #endregion
        
        #region Ifrit
        
        [ScriptMethod(name:"伊弗利特 第一次深红旋风 (阶段控制与数据获取)",
            eventType:EventTypeEnum.PlayActionTimeline,
            eventCondition:["SourceDataId:8730"],
            userControl:false)]

        public void 伊弗利特_第一次深红旋风_阶段控制与数据获取(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["Id"],"7747")) {

                return;

            }

            Interlocked.Increment(ref majorPhase);
            phase=1;

            if(!preserveDrawingsWhileSwitchingPhase) {
                
                accessory.Method.RemoveDraw(".*");
                
            }

            phase2_firstCrimsonCycloneSemaphore.Set();
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            int discretizedPosition=discretizePosition(sourcePosition,ARENA_CENTER,4);
            
            phase2_initialSafeZone[discretizedPosition]=false;
            phase2_initialSafeZone[(discretizedPosition+2)%4]=false;

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"majorPhase={majorPhase}\nphase={phase}\nphase2_initialSafeZone[{discretizedPosition}]=false\nphase2_initialSafeZone[{(discretizedPosition+2)%4}]=false");
                
            }

        }
        
        [ScriptMethod(name:"伊弗利特 第一次深红旋风 (范围)",
            eventType:EventTypeEnum.PlayActionTimeline,
            eventCondition:["SourceDataId:8730"])]

        public void 伊弗利特_第一次深红旋风_范围(Event @event,ScriptAccessory accessory) {
            
            if(!string.Equals(@event["Id"],"7747")) {

                return;

            }
            
            Vector3 sourcePosition=ARENA_CENTER;

            try {

                sourcePosition=JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourcePosition deserialization failed.");

                return;

            }
            
            bool signalled=phase2_firstCrimsonCycloneSemaphore.WaitOne(COMMON_INTERVAL);

            if(!signalled) {

                return;

            }
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase!=1&&!skipPhaseChecks) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(18,44);
            currentProperties.Position=sourcePosition;
            currentProperties.TargetPosition=ARENA_CENTER;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=5125;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"伊弗利特 光辉炎柱 (数据获取)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11105"],
            userControl:false)]

        public void 伊弗利特_光辉炎柱_数据获取(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase!=1&&!skipPhaseChecks) {

                return;

            }

            if(phase2_radiantPlumeCounter>=10) {

                return;

            }
            
            Vector3 effectPosition=ARENA_CENTER;

            try {

                effectPosition=JsonConvert.DeserializeObject<Vector3>(@event["EffectPosition"]);

            } catch(Exception e) {
                
                accessory.Log.Error("EffectPosition deserialization failed.");

                return;

            }

            for(int i=0;i<4;++i) {

                if(Vector3.Distance(effectPosition,rotatePosition(new Vector3(100,0,82),ARENA_CENTER,Math.PI/2*i))<0.1) {

                    phase2_initialSafeZone[i]=false;

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"phase2_initialSafeZone[{i}]=false");
                        
                    }

                }
                
            }

            lock(phase2_radiantPlumeSemaphore) {

                Interlocked.Increment(ref phase2_radiantPlumeCounter);

                if(phase2_radiantPlumeCounter==10) {

                    phase2_radiantPlumeSemaphore.Set();

                }

            }
                
        }
        
        [ScriptMethod(name:"伊弗利特 光辉炎柱 (指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11105"],
            suppress:COMMON_INTERVAL)]

        public void 伊弗利特_光辉炎柱_指路(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase!=1&&!skipPhaseChecks) {

                return;

            }
            
            bool signalled=phase2_radiantPlumeSemaphore.WaitOne(COMMON_INTERVAL);

            if(!signalled) {

                return;

            }

            int discretizedSafeZone=Array.IndexOf(phase2_initialSafeZone,true);

            if(enableDebugLogging) {
                
                accessory.Log.Debug($"discretizedSafeZone={discretizedSafeZone}");
                
            }
            
            if(discretizedSafeZone<0||discretizedSafeZone>3) {

                return;

            }

            Vector3 myPosition=rotatePosition(new Vector3(100,0,81.5f),ARENA_CENTER,Math.PI/2*discretizedSafeZone);
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=4000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"伊弗利特 火神爆裂 (击退指示)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11102"])]

        public void 伊弗利特_火神爆裂_击退指示(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=sourceId;
            currentProperties.TargetObject=accessory.Data.Me;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.Delay=3000;
            currentProperties.DestoryAt=8125;
                
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2,10);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=sourceId;
            currentProperties.Rotation=float.Pi;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.Delay=3000;
            currentProperties.DestoryAt=8125;
                
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

        }
        
        [ScriptMethod(name:"伊弗利特 火神爆裂 (阶段控制)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:11095"],
            suppress:COMMON_INTERVAL,
            userControl:false)]

        public void 伊弗利特_火神爆裂_阶段控制(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase!=1&&!skipPhaseChecks) {

                return;

            }

            phase=2;

            phase2_firstIncinerateSemaphore.Set();
            
            if(enableDebugLogging) {
                
                accessory.Log.Debug($"majorPhase={majorPhase}\nphase={phase}");
                
            }

        }
        
        [ScriptMethod(name:"伊弗利特 第一次烈焰焚烧 (范围)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:11095"],
            suppress:COMMON_INTERVAL)]

        public void 伊弗利特_第一次烈焰焚烧_范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            bool signalled=phase2_firstIncinerateSemaphore.WaitOne(COMMON_INTERVAL);

            if(!signalled) {

                return;

            }

            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(15);
            currentProperties.Radian=float.Pi/3*2;
            currentProperties.Owner=sourceId;
            currentProperties.TargetResolvePattern=PositionResolvePatternEnum.OwnerEnmityOrder;
            currentProperties.TargetOrderIndex=1;
            currentProperties.Color=colourOfExtremelyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=10125;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

        }
        
        [ScriptMethod(name:"伊弗利特 火狱之楔 (数据获取)",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:8731"],
            userControl:false)]

        public void 伊弗利特_火狱之楔_数据获取(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase2_infernalNailCounter>=4) {

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
            
            int discretizedPosition=discretizePosition(sourcePosition,ARENA_CENTER,8);

            lock(phase2_infernalNailDeployed) {

                phase2_infernalNailId[discretizedPosition]=sourceId;
                phase2_infernalNailDeployed[discretizedPosition]=true;

                Interlocked.Increment(ref phase2_infernalNailCounter);

                if(phase2_infernalNailCounter==4) {

                    int theFourthNail=0;

                    while(theFourthNail<8) {

                        int theNextNail=(theFourthNail+1)%8;

                        if(phase2_infernalNailDeployed[theFourthNail]&&phase2_infernalNailDeployed[theNextNail]) {

                            break;

                        }

                        else {
                            
                            ++theFourthNail;

                        }

                    }

                    if(theFourthNail>=8) {

                        return;

                    }
                    
                    phase2_infernalNail[3]=theFourthNail;
                    phase2_infernalNail[2]=(theFourthNail+1)%8;
                    phase2_infernalNail[1]=(theFourthNail+6)%8;
                    phase2_infernalNail[0]=(theFourthNail+3)%8;

                    phase2_temporaryRotation=Math.PI/4*(0.5d+theFourthNail);

                    phase2_infernalNailSemaphore1.Set();
                    phase2_infernalNailSemaphore2.Set();
                    phase2_infernalNailSemaphore3.Set();
                    phase2_infernalNailSemaphore4.Set();

                    if(enableDebugLogging) {
                        
                        accessory.Log.Debug($"""
                                             phase2_infernalNailDeployed:{string.Join(",",phase2_infernalNailDeployed)}
                                             phase2_infernalNail:{string.Join(",",phase2_infernalNail)}
                                             phase2_temporaryRotation={phase2_temporaryRotation}
                                             """);
                        
                    }

                }

            }

        }
        
        [ScriptMethod(name:"伊弗利特 火狱之楔 (目标指示,仅DPS)",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:8731"],
            suppress:COMMON_INTERVAL)]

        public void 伊弗利特_火狱之楔_目标指示_仅DPS(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase!=2&&!skipPhaseChecks) {

                return;

            }

            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            if(!isDps(myIndex)) {

                return;

            }
            
            bool signalled=phase2_infernalNailSemaphore1.WaitOne(COMMON_INTERVAL);
            
            if(!signalled) {

                return;

            }

            int myNail=myIndex switch {
                
                4 => phase2_infernalNail[0],
                5 => phase2_infernalNail[1],
                6 => phase2_infernalNail[2],
                7 => phase2_infernalNail[3],
                _ => -1
                
            };

            if(myNail==-1) {

                return;

            }

            ulong idOfMyNail=phase2_infernalNailId[myNail];
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"伊弗利特_火狱之楔_目标指示_仅DPS_{idOfMyNail}";
            currentProperties.Scale=new(0.25f);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=idOfMyNail;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.DestoryAt=MAXIMUM_DURATION;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"伊弗利特 火狱之楔 (目标指示清除)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:11096"],
            userControl:false)]

        public void 伊弗利特_火狱之楔_目标指示清除(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!string.Equals(@event["TargetIndex"],"1")) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"伊弗利特_火狱之楔_目标指示_仅DPS_{sourceId}");

        }
        
        [ScriptMethod(name:"伊弗利特 地火喷发 (指路,远程DPS与MT)",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:8731"],
            suppress:COMMON_INTERVAL)]

        public void 伊弗利特_地火喷发_指路_远程DPS与MT(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase!=2&&!skipPhaseChecks) {

                return;

            }

            int myIndex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            
            if(!isLegalPartyIndex(myIndex)) {

                return;

            }

            if(!isRangedDps(myIndex)&&myIndex!=0) {

                return;

            }
            
            bool signalled=phase2_infernalNailSemaphore2.WaitOne(COMMON_INTERVAL);
            
            if(!signalled) {

                return;

            }

            Vector3 myPosition=myIndex switch {
                
                0 => new Vector3(100,0,88.5f),
                6 => new Vector3(113.643f,0,108.358f),
                7 => new Vector3(86.357f,0,108.358f),
                _ => ARENA_CENTER
                
            };
            // Geometric Construction:
            // https://www.geogebra.org/calculator/bs4sbfem

            myPosition=rotatePosition(myPosition,ARENA_CENTER,phase2_temporaryRotation);
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=myPosition;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;

            if(isRangedDps(myIndex)) {
                
                currentProperties.DestoryAt=11250;
                
            }
            
            if(myIndex==0) {
                
                currentProperties.DestoryAt=17250;
                
            }
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            if(isRangedDps(myIndex)) {
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();
                
                currentProperties.Scale=new(17.5f);
                currentProperties.InnerScale=new(14.5f);
                currentProperties.Radian=((float)(convertDegreesToRadians(121.492)));
                currentProperties.Position=ARENA_CENTER;
                currentProperties.TargetPosition=myPosition;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=17250;

                if(myIndex==6) {

                    currentProperties.Rotation=((float)(convertDegreesToRadians(121.492/2)));

                }
                
                if(myIndex==7) {
                    
                    currentProperties.Rotation=-((float)(convertDegreesToRadians(121.492/2)));
                    
                }
                
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Donut,currentProperties);
                
            }

        }
        
        [ScriptMethod(name:"伊弗利特 火狱之楔 (指北针)",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:8731"],
            suppress:COMMON_INTERVAL)]

        public void 伊弗利特_火狱之楔_指北针(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            bool signalled=phase2_infernalNailSemaphore3.WaitOne(COMMON_INTERVAL);
            
            if(!signalled) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Scale=new(2,14);
            currentProperties.Position=rotatePosition(new Vector3(100,0,107),ARENA_CENTER,phase2_temporaryRotation);
            currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,93),ARENA_CENTER,phase2_temporaryRotation);
            currentProperties.Color=phase2_colourOfNorthIndicator.V4.WithW(1);
            currentProperties.DestoryAt=17250;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);

        }
        
        [ScriptMethod(name:"伊弗利特 火狱之楔 (标记顺序)",
            eventType:EventTypeEnum.AddCombatant,
            eventCondition:["DataId:8731"],
            suppress:COMMON_INTERVAL)]

        public void 伊弗利特_火狱之楔_标记顺序(Event @event,ScriptAccessory accessory) {

            if(!phase2_enableNailOrderAssistance) {

                return;

            }
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            bool signalled=phase2_infernalNailSemaphore4.WaitOne(COMMON_INTERVAL);
            
            if(!signalled) {

                return;

            }
            
            accessory.Method.Mark(((uint)phase2_infernalNailId[phase2_infernalNail[0]]),MarkType.Attack1);
            accessory.Method.Mark(((uint)phase2_infernalNailId[phase2_infernalNail[1]]),MarkType.Attack2);
            accessory.Method.Mark(((uint)phase2_infernalNailId[phase2_infernalNail[2]]),MarkType.Attack3);
            accessory.Method.Mark(((uint)phase2_infernalNailId[phase2_infernalNail[3]]),MarkType.Attack4);

            if(enableDebugLogging) {

                accessory.Log.Debug($"""
                                     Mark {phase2_infernalNailId[phase2_infernalNail[0]]} as {MarkType.Attack1}
                                     Mark {phase2_infernalNailId[phase2_infernalNail[1]]} as {MarkType.Attack2}
                                     Mark {phase2_infernalNailId[phase2_infernalNail[2]]} as {MarkType.Attack3}
                                     Mark {phase2_infernalNailId[phase2_infernalNail[3]]} as {MarkType.Attack4}
                                     """);

            }

        }
        
        [ScriptMethod(name:"伊弗利特 火狱之锁 (连线指示)",
            eventType:EventTypeEnum.Tether,
            eventCondition:["Id:0009"])]

        public void 伊弗利特_火狱之锁_连线指示(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            accessory.Method.RemoveDraw($"伊弗利特_火狱之锁_连线指示_{phase2_infernalFetterDrawingCounter}");
            
            if(!convertObjectIdToDecimal(@event["SourceId"], out var sourceId)) {
                
                return;
                
            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            if(accessory.Data.Me!=sourceId&&accessory.Data.Me!=targetId) {

                return;

            }
            
            int sourceIndex=accessory.Data.PartyList.IndexOf(((uint)sourceId));
            
            if(!isLegalPartyIndex(sourceIndex)) {

                return;

            }
            
            int targetIndex=accessory.Data.PartyList.IndexOf(((uint)targetId));
            
            if(!isLegalPartyIndex(targetIndex)) {

                return;
            }

            ulong sourceInDrawing=sourceId,targetInDrawing=targetId;
            bool anomalousTether=true;

            if(isTank(sourceIndex)&&isDps(targetIndex)) {

                anomalousTether=false;

                sourceInDrawing=sourceId;
                targetInDrawing=targetId;

            }
            
            if(isTank(targetIndex)&&isDps(sourceIndex)) {

                anomalousTether=false;

                sourceInDrawing=targetId;
                targetInDrawing=sourceId;

            }

            if(anomalousTether) {

                if(accessory.Data.Me==sourceId) {
                    
                    sourceInDrawing=sourceId;
                    targetInDrawing=targetId;

                }
                
                if(accessory.Data.Me==targetId) {
                    
                    sourceInDrawing=targetId;
                    targetInDrawing=sourceId;

                }
                
            }
            
            Interlocked.Increment(ref phase2_infernalFetterDrawingCounter);
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name=$"伊弗利特_火狱之锁_连线指示_{phase2_infernalFetterDrawingCounter}";
            currentProperties.Scale=new(2);
            currentProperties.Owner=sourceInDrawing;
            currentProperties.TargetObject=targetInDrawing;
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.DestoryAt=21000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
        }
        
        [ScriptMethod(name:"伊弗利特 地火喷发 (阶段控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:11098"],
            userControl:false)]

        public void 伊弗利特_地火喷发_阶段控制(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase2_eruptionCounter>=8) {

                return;

            }
            
            lock(phase2_eruptionCounterLock) {

                Interlocked.Increment(ref phase2_eruptionCounter);

                if(phase2_eruptionCounter==8) {

                    phase=3;
                    
                    if(enableDebugLogging) {
                
                        accessory.Log.Debug($"majorPhase={majorPhase}\nphase={phase}");
                
                    }

                }

            }
            
        }
        
        [ScriptMethod(name:"伊弗利特 第一次灼热 (指路)",
            eventType:EventTypeEnum.StatusAdd,
            eventCondition:["StatusID:1578"])]

        public void 伊弗利特_第一次灼热_指路(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=2&&!skipPhaseChecks) {

                return;

            }

            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }

            if(accessory.Data.Me!=targetId) {

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
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
            
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetPosition=rotatePosition(new Vector3(100,0,118.5f),ARENA_CENTER,phase2_temporaryRotation);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.DestoryAt=durationMilliseconds;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
        }
        
        #endregion
        
        #region Titan
        
        
        
        #endregion
        
        #region Ascian_Lahabrea
        
        
        
        #endregion
        
        #region Ultima_Weapon
        
        
        
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