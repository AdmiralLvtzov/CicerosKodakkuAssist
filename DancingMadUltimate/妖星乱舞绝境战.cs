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
        version:"0.0.0.8",
        note:scriptNotes,
        author:"Cicero 灵视")]

    public class Dancing_Mad_Ultimate
    {
        
        public const string scriptNotes=
            """
            妖星乱舞绝境战的脚本。
            
            脚本刚刚开始施工,进度为P1众神之像1。指路适配的攻略尚未确定。
            如果指路不适配你采用的攻略,可以在方法设置中将相关的指路关闭。所有指路方法均标注有"(指路)"后缀。
            
            支持进行小队排序测试,可以在聊天框中输入/e kuwutest来检查小队排序是否正确。
            输入/e kuwuclear清除小队排序测试产生的目标标记。

            如果在使用过程中遇到了异常,请先检查可达鸭本体与脚本是否都更新到了最新版本,小队职能是否已正确设置,异常是否可以稳定复现。
            如果上述三点都没有问题,请带着A Realm Recorded插件的录像文件在可达鸭Discord内联系@_publius_cornelius_scipio_反馈异常。
            
            特别致谢:
                Karlin - 提供了P1 众神之像1 屏蔽假技能特效的方法。
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
        
        // ----- End Of Major Phase 2 -----
        
        // ----- Major Phase 3 -----
        
        // ----- End Of Major Phase 3 -----

        #endregion
        
        #region Variables_And_Semaphores
        
        private volatile int majorPhase=1;
        private volatile int phase=1;
        
        /*

        Major Phase 1 - ???:

            Phases are separated by Graven Image.
            阶段由众神之像分隔。
        
        Major Phase 2 - ???:

            Phase 1 - Placeholder 占位符
            Phase 2 - Placeholder 占位符
            Phase 3 - Placeholder 占位符
            
        Major Phase 3 - ???:

            Phase 1 - Placeholder 占位符
            Phase 2 - Placeholder 占位符
            Phase 3 - Placeholder 占位符

        */
        
        // ----- Major Phase 1 -----
        
        private volatile bool phase1sub2_fakeFlagrantFire=false;
        private System.Threading.AutoResetEvent phase1sub2_flagrantFireObfuscationSemaphore=new System.Threading.AutoResetEvent(false);
        private HashSet<ulong> phase1sub2_partyMembersWithFlagrantFire=new HashSet<ulong>();
        private volatile bool phase1sub2_stackFlagrantFireIcon=false;
        private System.Threading.AutoResetEvent phase1sub2_flagrantFireIconSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile int phase1sub2_玄乎乎魔法Counter=0; // To be changed in the future.
        private System.Threading.AutoResetEvent phase1sub2_waveCannonSemaphore=new System.Threading.AutoResetEvent(false);
        
        // ----- End Of Major Phase 1 -----
        
        // ----- Major Phase 2 -----
        
        // ----- End Of Major Phase 2 -----
        
        // ----- Major Phase 3 -----
        
        // ----- End Of Major Phase 3 -----
        
        #endregion
        
        #region Constants_And_Locks
        
        private const int COMMON_INTERVAL=2500;
        private const int MAXIMUM_DURATION=7200000;
        
        private static readonly Vector3 ARENA_CENTER=new Vector3(100,0,100);
        private const int ARENA_RADIUS=20;
        
        private const int SHENANIGAN_DELAY=5000;
        private const int SHENANIGAN_DURATION=10000;
        private const int PARTY_TEST_DURATION=20000;
        private const int VISIBILITY_RECOVERY_DELAY=125;
        
        #endregion
        
        #region Enumerations_And_Classes
        
        public enum PartyTestChannels {
            
            不发送到任何频道,
            默语频道_仅自己可见,
            小队频道_所有队员可见

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

            phase1sub2_fakeFlagrantFire=false;
            phase1sub2_flagrantFireObfuscationSemaphore.Reset();
            phase1sub2_partyMembersWithFlagrantFire.Clear();
            phase1sub2_stackFlagrantFireIcon=false;
            phase1sub2_flagrantFireIconSemaphore.Reset();
            phase1sub2_玄乎乎魔法Counter=0;
            phase1sub2_waveCannonSemaphore.Reset();

            // ----- End Of Major Phase 1 -----

            // ----- Major Phase 2 -----

            // ----- End Of Major Phase 2 -----

            // ----- Major Phase 3 -----

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
                        
                        text+=$"{roles[i]}:{sourceObject.Name}，标记{marks[i].ToString()}。";

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
        
        [ScriptMethod(name:"通用 连环环陷阱 (范围清除)",
            eventType:EventTypeEnum.StatusRemove,
            eventCondition:["StatusID:5078"],
            userControl:false)]

        public void 通用_连环环陷阱_范围清除(Event @event,ScriptAccessory accessory) {
            
            if(!convertObjectIdToDecimal(@event["TargetId"], out var targetId)) {
                
                return;
                
            }
            
            accessory.Method.RemoveDraw($"通用_连环环陷阱_范围_{targetId}");
            
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
        
        [ScriptMethod(name:"P1 众神之像1 呼啦啦爆炎 (数据收集1)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:regex:^(02A1|02A2|02A3|02A4)$"],
            userControl:false)]

        public void P1_众神之像1_呼啦啦爆炎_数据收集1(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

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

                phase1sub2_fakeFlagrantFire=true;
                phase1sub2_flagrantFireObfuscationSemaphore.Set();

                log+="fake Flagrant Fire";

            }
            
            if(string.Equals(@event["Id"],"02A2")) {
                
                phase1sub2_fakeFlagrantFire=false;
                phase1sub2_flagrantFireObfuscationSemaphore.Set();

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
        
        [ScriptMethod(name:"P1 众神之像1 呼啦啦爆炎 (数据收集2)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:regex:^(0080|007F)$"],
            userControl:false)]

        public void P1_众神之像1_呼啦啦爆炎_数据收集2(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }

            if(!convertObjectIdToDecimal(@event["TargetId"],out var targetId)) {
                
                return;
                
            }

            lock(phase1sub2_partyMembersWithFlagrantFire) {
                
                phase1sub2_partyMembersWithFlagrantFire.Add(targetId);

                if(phase1sub2_partyMembersWithFlagrantFire.Count==2) {
                    
                    if(string.Equals(@event["Id"],"0080")) {

                        phase1sub2_stackFlagrantFireIcon=true;
                        phase1sub2_flagrantFireIconSemaphore.Set();
                        
                        if(enableDebugLogging) {
                        
                            accessory.Log.Debug($"""
                                                 phase1sub2_stackFlagrantFireIcon={phase1sub2_stackFlagrantFireIcon}
                                                 phase1sub2_partyMembersWithFlagrantFire:{string.Join(",",phase1sub2_partyMembersWithFlagrantFire)}
                                                 """);
                        
                        }

                    }
                    
                }
                
                if(phase1sub2_partyMembersWithFlagrantFire.Count==8) {
                    
                    if(string.Equals(@event["Id"],"007F")) {

                        phase1sub2_stackFlagrantFireIcon=false;
                        phase1sub2_flagrantFireIconSemaphore.Set();
                        
                        if(enableDebugLogging) {
                        
                            accessory.Log.Debug($"""
                                                 phase1sub2_stackFlagrantFireIcon={phase1sub2_stackFlagrantFireIcon}
                                                 phase1sub2_partyMembersWithFlagrantFire:{string.Join(",",phase1sub2_partyMembersWithFlagrantFire)}
                                                 """);
                        
                        }

                    }
                    
                }
                
            }

        }
        
        [ScriptMethod(name:"P1 众神之像1 呼啦啦爆炎 (范围与提示)",
            eventType:EventTypeEnum.TargetIcon,
            eventCondition:["Id:regex:^(0080|007F)$"],
            suppress:COMMON_INTERVAL)]

        public void P1_众神之像1_呼啦啦爆炎_范围与提示(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            bool signalled=phase1sub2_flagrantFireObfuscationSemaphore.WaitOne(COMMON_INTERVAL);

            if(!signalled) {

                return;

            }
            
            signalled=phase1sub2_flagrantFireIconSemaphore.WaitOne(COMMON_INTERVAL);

            if(!signalled) {

                return;

            }

            if(phase1sub2_stackFlagrantFireIcon) {

                if(!phase1sub2_fakeFlagrantFire) {
                    
                    foreach(ulong i in phase1sub2_partyMembersWithFlagrantFire) {
                        
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
                
                if(!phase1sub2_fakeFlagrantFire) {
                    
                    foreach(ulong i in phase1sub2_partyMembersWithFlagrantFire) {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(5);
                        currentProperties.Owner=i;
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.DestoryAt=5875;
            
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                        
                    }
                    
                }

                else {

                    string prompt="职能四四分摊";

                    if(enablePrompts) {
                    
                        accessory.Method.TextInfo(prompt,5875);
                    
                    }
                    
                    accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                    
                }
                
            }

        }
        
        [ScriptMethod(name:"P1 众神之像1 扩大大冰封 (实际范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47768|47774)$"])]

        public void P1_众神之像1_扩大大冰封_实际范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(40);
            currentProperties.Radian=float.Pi/2;
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=5000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Fan,currentProperties);

        }
        
        [ScriptMethod(name:"P1 众神之像1 扩大大冰封 (技能特效屏蔽) !!!实验性功能!!!",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47768|47771)$"])]

        public void P1_众神之像1_扩大大冰封_技能特效屏蔽_实验性功能(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

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
        
        [ScriptMethod(name:"P1 众神之像1 劈啪啪暴雷 (实际范围)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47775|47777)$"])]

        public void P1_众神之像1_劈啪啪暴雷_实际范围(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

                return;

            }
            
            if(!convertObjectIdToDecimal(@event["SourceId"],out var sourceId)) {
                
                return;
                
            }
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(10,40);
            currentProperties.Owner=sourceId;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=5000;
        
            accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

        }
        
        [ScriptMethod(name:"P1 众神之像1 劈啪啪暴雷 (技能特效屏蔽) !!!实验性功能!!!",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(47775|47776)$"])]

        public void P1_众神之像1_劈啪啪暴雷_技能特效屏蔽_实验性功能(Event @event,ScriptAccessory accessory) {
            
            if(majorPhase!=1&&!skipPhaseChecks) {

                return;

            }
            
            if(phase!=2&&!skipPhaseChecks) {

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