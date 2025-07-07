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

namespace CicerosKodakkuAssist.Arcadion.Savage.ChinaDataCenter
{
    
    [ScriptType(name:"阿卡狄亚零式登天斗技场 中量级4",
        territorys:[1263],
        guid:"d9de6d9a-f6f5-41c6-a15b-9332fa1e6c33",
        version:"0.0.1.14",
        note:scriptNotes,
        author:"Cicero 灵视")]

    public class AAC_Cruiserweight_M4_Savage
    {
        
        public const string scriptNotes=
            """
            阿卡狄亚零式登天斗技场中量级4(也就是M8S)的脚本。
            
            此脚本基于其国际服版本创建。画图部分已经全部完成,指路部分正在进行对国服的适配。优先适配MMW攻略组&苏帕酱噗的视频攻略。
            如果指路不适配你采用的攻略,可以在方法设置中将指路关闭。所有指路方法名称中均标注有"Guidance"或者"指路"。
            
            门神指路已经完全适配国服。
            本体指路适配进度: 魔光 √, 铠袖一触 √, 风震魔印 √, 飓风之相 √, 回天动地 √, 咒刃之相 √
            
            此脚本的国际服版本已经完工,适配了欧服野队攻略,也就是门神Raidplan 84d,本体Raidplan DOG。
            对于尚未完成国服适配的机制,其指路将仍然是基于欧服攻略的。适配预计将在一周内完成。
            
            门神RaidPlan 84d的链接: https://raidplan.io/plan/B5Q3Mk62YKuTy84d
            本体Toxic Friends RaidPlan DOG的链接: https://raidplan.io/plan/9M-1G-mmOaaroDOG
            MMW攻略组门神小抄: https://xivstrat.com/07/m8s1/
            MMW攻略组本体小抄: https://xivstrat.com/07/m8s2/
            """;

        #region User_Settings
        
        [UserSetting("启用文字提示")]
        public bool enablePrompts { get; set; } = true;
        [UserSetting("启用原生TTS")]
        public bool enableVanillaTts { get; set; } = true;
        [UserSetting("启用Daily Routines TTS (需要安装并启用Daily Routines插件!)")]
        public bool enableDailyRoutinesTts { get; set; } = false;
        [UserSetting("机制方向的颜色")]
        public ScriptColor colourOfDirectionIndicators { get; set; } = new() { V4 = new Vector4(1,1,0, 1) }; // Yellow by default.
        [UserSetting("高度危险攻击的颜色")]
        public ScriptColor colourOfHighlyDangerousAttacks { get; set; } = new() { V4 = new Vector4(1,0,0,1) }; // Red by default.
        [UserSetting("启用搞怪")]
        public bool enableShenanigans { get; set; } = false;
        
        [UserSetting("门神攻略")]
        public StratsOfPhase1 stratOfPhase1 { get; set; }
        
        
        [UserSetting("本体攻略")]
        public StratsOfPhase2 stratOfPhase2 { get; set; }
        [UserSetting("本体Boss中轴线及其附属箭头的颜色")]
        public ScriptColor colourOfTheBossAxis { get; set; } = new() { V4 = new Vector4(0,1,1, 1) }; // Blue by default.

        #endregion
        
        #region Variables
        
        private volatile int currentPhase=1;
        private volatile int currentSubPhase=1;
        
        /*
         
         Phase 1:
         
         Sub-phase 1: 千年风化前半
         Sub-phase 2: 千年风化后半
         Sub-phase 3: 大地的呼唤
         Sub-phase 4: 期间群狼剑机制
         Sub-phase 5: 光狼召唤
         Sub-phase 6: 大地之怒
         Sub-phase 7: 期间群狼剑机制
         Sub-phase 8: 幻狼召唤
         Sub-phase 9: 期间魔技机制
         
         Phase 2:
         
         Sub-phase 1: 风震魔印
         Sub-phase 2: Twofold Tempest
         Sub-phase 3: Champion's Circuit
         Sub-phase 4: Lone Wolf's Lament
         Sub-phase 5: 狂暴
         
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
        private System.Threading.AutoResetEvent newNorthGuidanceSemaphore=new System.Threading.AutoResetEvent(false);
        private bool? dpsStackFirst=null;
        private System.Threading.AutoResetEvent roleStackSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile bool firstSetGuidanceHasBeenDrawn=false;
        private volatile bool shadowsAreOnTheCardinals=false; // Its read-write lock is lockOfShadowNumber.
        private volatile int numberOfShadows=0; // Its read-write lock is lockOfShadowNumber.
        private System.Threading.AutoResetEvent shadowSemaphore=new System.Threading.AutoResetEvent(false);

        private volatile List<List<int>> moonbeamsBite=[]; // 0=Northeast, 1=southeast, 2=southwest, 3=northwest.
        private System.Threading.AutoResetEvent secondMoonbeamsBiteSemaphore=new System.Threading.AutoResetEvent(false);
        private System.Threading.AutoResetEvent fourthMoonbeamsBiteSemaphore=new System.Threading.AutoResetEvent(false);
        private volatile bool secondSetGuidanceHasBeenDrawn=false;
        private volatile bool stoneWolvesAreOnTheCardinals=false;

        private volatile string phase2BossId=string.Empty;
        
        private volatile bool axisAndArrowsHaveBeenDrawn=false;
        
        private volatile int roundOfQuakeIii=0;

        private volatile int numberOfUltraviolentRay=0;
        private volatile List<bool> playerWasMarkedByAUltraviolentRay=[false,false,false,false,false,false,false,false];
        private System.Threading.AutoResetEvent ultraviolentRaySemaphore=new System.Threading.AutoResetEvent(false);
        private volatile int roundOfUltraviolentRay=0;

        private volatile int roundOfTwinbite=0;
        
        private volatile int roundOfHerosBlow=0;

        private double? bossRotationDuringPurgeAndTempest=null;
        private System.Threading.AutoResetEvent firstMooncleaverSemaphore=new System.Threading.AutoResetEvent(false);
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

        public enum StratsOfPhase1 {

            MMW攻略组与苏帕酱噗,
            其他攻略正在施工中

        }
        
        public enum StratsOfPhase2 {

            MMW攻略组与苏帕酱噗,
            其他攻略正在施工中

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
            
            currentPhase=2;
            currentSubPhase=2; // 记得测试后改回去!
            
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
            newNorthGuidanceSemaphore.Reset();
            dpsStackFirst=null;
            roleStackSemaphore.Reset();
            firstSetGuidanceHasBeenDrawn=false;
            shadowsAreOnTheCardinals=false;
            numberOfShadows=0;
            shadowSemaphore.Reset();
            
            moonbeamsBite=[];
            secondMoonbeamsBiteSemaphore.Reset();
            fourthMoonbeamsBiteSemaphore.Reset();
            secondSetGuidanceHasBeenDrawn=false;
            stoneWolvesAreOnTheCardinals=false;
            
            phase2BossId="40010DC0";
            
            axisAndArrowsHaveBeenDrawn=false;
            
            roundOfQuakeIii=0;
            
            numberOfUltraviolentRay=0;
            playerWasMarkedByAUltraviolentRay=[false,false,false,false,false,false,false,false];
            ultraviolentRaySemaphore.Reset();
            roundOfUltraviolentRay=0;
            
            roundOfTwinbite=0;

            roundOfHerosBlow=0;

            bossRotationDuringPurgeAndTempest=0.0;
            firstMooncleaverSemaphore.Reset();
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
            "唯有死亡才算是责任的尾声。",
            "目的为手段赋予了正义,但总得有什么为目的赋予正义。",
            "高唱你的死亡咏赞,如归乡英雄般死去。",
            "叛变者骑着战马,消失于夜色之中。",
            "历史上,每当人们开始认为,可以用对形式框架的虚伪尊重取代对道德义务的真心服从时,嗜杀成性的暴戾恶鬼就会重返人间。",
            "没有什么东西比战争更残酷和不人道,没有什么事物比和平更令人神往。但没有互相尊重对方权利的因,就得不到和平的果。",
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
            "一个人被打败不算完蛋,当他放弃就真正完蛋了。\n——理查德·尼克松, 1962",
            "不要祈祷更安逸的生活,我的朋友,祈祷自己成为更坚强的人。\n——约翰·F·肯尼迪, 1963",
            "大自然不解消亡,只解演变。\n——韦恩赫尔·冯·布劳恩, 1962",
            "乐观主义者认为这个世界是所有可能中最好的,而悲观主义者担心这就是真的。\n——詹姆斯·布朗奇·卡贝尔, 《银马》, 1926",
            "当魔鬼和你勾肩搭背时,你很难认出他来。\n——阿尔伯特·施佩尔, 1972",
            "战时无法律。\n——马库斯·图利乌斯·西塞罗, 52BC",
            "他们对你的要求不多: 只是要你去恨你所爱的,去爱你所厌的。\n——鲍里斯·帕斯捷尔纳克, 1960",
            "大部分经济学上的谬误都源自于\"定量馅饼\"的前提假设,以为一方得益,另一方就必有所失。\n——米尔顿·弗里德曼, 1980",
            "世界上有三种谎言: 谎言,糟糕透顶的谎言和统计数据。\n——马克·吐温, 1907",
            "咬我一次,可耻在狗;咬我多次,纵容它的我才可耻。\n——菲莉丝·施拉夫利, 1995",
            "我不知道第三次世界大战会使用何种武器,但我知道,第四次世界大战会使用棍子和石头。\n——阿尔伯特·爱因斯坦, 1949",
            "风水这个东西,你要信也可以,但是我更相信事在人为。\n——李嘉诚, 1969",
            "金钱是补偿一个人的工作的唯一办法,我认为这种看法大错特错。人们是需要钱,但他们也需要在工作中得到愉快和自豪。\n——盛田昭夫, 1966",
            "建立个人和企业的良好信誉,这是资产负债表之中见不到,但却是价值无限的资产。\n——李嘉诚, 1967",
            "财富聚散无常,唯学问终生受用。\n——何鸿燊, 1966",
            "人们常言道:\"我们生活在一个腐败,虚伪的社会当中。\"这也不尽然。心慈好善的人仍占多数。\n——若望·保禄一世, 1978",
            "这世界上半数的困惑,都来源于我们不知道自己的需求是多么微不足道。\n——理查德·E·伯德海军上将, 于南极洲, 1935"
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

        [ScriptMethod(name:"门神 风土之魔技",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:regex:^(41885|41886|41889|41890)$"])]
    
        public void Phase_1_Windfang_And_Stonefang(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(!convertObjectId(@event["SourceId"], out var sourceId)) {
            
                return;
            
            }
            
            IReadOnlyList<int> getPartner=[6,7,4,5,2,3,0,1];
        
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

                if(string.Equals(@event["ActionId"],"41885")) {
                    
                    prompt="靠近,斜点分摊";
                    
                }
                
                if(string.Equals(@event["ActionId"],"41886")) {
                    
                    prompt="靠近,正点分摊";
                    
                }

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

                prompt="远离,分散";

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

            }

            if(!string.IsNullOrWhiteSpace(prompt)) {

                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,6000);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }
        
        }
        
        [ScriptMethod(name:"门神 风土之魔技 (指路)",
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
                
                IReadOnlyList<float> getDegree=[315,45,225,135,225,135,315,45];
                
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
                
                IReadOnlyList<float> getDegree=[0,90,270,180,270,180,0,90];
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=rotatePosition(innerPosition,ARENA_CENTER_OF_PHASE_1,getDegree[myIndex].DegToRad());
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

            }
            
            if(string.Equals(@event["ActionId"],"41889")||string.Equals(@event["ActionId"],"41890")) {
                
                IReadOnlyList<float> getDegree=[337.5f,67.5f,247.5f,157.5f,202.5f,112.5f,292.5f,22.5f];
                
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
        
        [ScriptMethod(name:"门神 扫旋击群狼剑 (钢铁)",
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
        
        [ScriptMethod(name:"门神 扫旋击群狼剑 (方向)",
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

                prompt="后续扇形,靠近";

            }
            
            if(string.Equals(@event["ActionId"],"43313")) {

                prompt="后续钢铁,远离";

            }
            
            if(!string.IsNullOrWhiteSpace(prompt)) {

                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,9100);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }
        
        }
        
        [ScriptMethod(name:"门神 扫旋击群狼剑 (ID获取)",
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
        
        [ScriptMethod(name:"门神 扫旋击群狼剑 (后续连击)",
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
        
        [ScriptMethod(name:"门神 扫旋击群狼剑 (指路)",
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
        
        [ScriptMethod(name:"门神 扫旋击群狼剑 (小怪)",
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
        
        [ScriptMethod(name:"门神 千年风化前半 (直线)",
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
        
        [ScriptMethod(name:"门神 千年风化前半 (方向获取)",
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
        
        [ScriptMethod(name:"门神 千年风化前半 (方向)",
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

                prompt="顺时针";

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
                
                prompt="逆时针";
                
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
        
        [ScriptMethod(name:"门神 千年风化前半 (防击退警告)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41912"])]
    
        public void Phase_1_The_First_Half_Of_Millennial_Decay_AntiKnockback_Warning(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }

            if(stratOfPhase1==StratsOfPhase1.MMW攻略组与苏帕酱噗) {
                
                string prompt="防击退!";
            
                // System.Threading.Thread.Sleep(500);
            
                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,4500,true);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }
        
        }
        
        [ScriptMethod(name:"门神 千年风化前半 (狂风获取)",
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
        
        [ScriptMethod(name:"门神 千年风化前半 (第一轮指路)",
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

            if(stratOfPhase1==StratsOfPhase1.MMW攻略组与苏帕酱噗) {
                
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
            
                // Northwest: MT(0) or D3(6)
                // Northeast: ST(1) or D4(7)
                // Southwest: H1(2) or D1(4)
                // Southeast: H2(3) or D2(5)

                myPosition=myIndex switch {
                
                    0 => northwest,
                    1 => northeast,
                    2 => southwest,
                    3 => southeast,
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
        
        [ScriptMethod(name:"门神 千年风化前半 (第二轮指路)",
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

            if(stratOfPhase1==StratsOfPhase1.MMW攻略组与苏帕酱噗) {
                
                Vector3 trueNorth=new Vector3(100,0,89);
                Vector3 trueSouth=new Vector3(100,0,111);
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

                if(windWolvesRotateClockwise) {
                        
                    myPosition=myIndex switch {
                    
                        0 => northwest,
                        1 => southeast, // Swap with H2.
                        2 => trueNorth,
                        3 => trueSouth,
                        4 => northwest, // Swap with D3.
                        5 => southeast,
                        6 => trueNorth,
                        7 => trueSouth,
                        _ => ARENA_CENTER_OF_PHASE_1
                    
                    };
                        
                }

                else {
                        
                    myPosition=myIndex switch {
                    
                        0 => southwest, // Swap with H1.
                        1 => northeast, 
                        2 => trueSouth,
                        3 => trueNorth,
                        4 => southwest,
                        5 => northeast, // Swap with D4.
                        6 => trueSouth,
                        7 => trueNorth,
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
        
        [ScriptMethod(name:"门神 千年风化前半 (子阶段1控制)",
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
        
        [ScriptMethod(name:"门神 千年风化后半 (扇形)",
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
        
        [ScriptMethod(name:"门神 千年风化后半 (防击退警告)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:41912"])]
    
        public void Phase_1_The_Second_Half_Of_Millennial_Decay_AntiKnockback_Warning(Event @event,ScriptAccessory accessory) {

            if(currentPhase!=1) {

                return;

            }

            if(currentSubPhase!=2) {

                return;

            }

            if(stratOfPhase1==StratsOfPhase1.MMW攻略组与苏帕酱噗) {
                
                string prompt="不要防击退!";
            
                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,4700,true);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }
        
        }
        
        [ScriptMethod(name:"门神 千年风化后半 (数据获取)",
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
        
        [ScriptMethod(name:"门神 千年风化后半 (指路)",
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

            if(stratOfPhase1==StratsOfPhase1.MMW攻略组与苏帕酱噗) {
                
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
                
                // 7 and 0: MT(0) or D3(6)
                // 1 and 2: ST(1) or D4(7)
                // 3 and 4: H2(3) or D2(5)
                // 5 and 6: H1(2) or D1(4)

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
                            1 => 1,
                            2 => 5,
                            3 => 3,
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
                            1 => 2,
                            2 => 6,
                            3 => 4,
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
        
        [ScriptMethod(name:"门神 千年风化后半 (子阶段2控制)",
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
        
        [ScriptMethod(name:"门神 大地的呼唤 (直线)",
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
        
        [ScriptMethod(name:"门神 大地的呼唤 (十字)",
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
        
        [ScriptMethod(name:"门神 大地的呼唤 (直线方向获取)",
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
        
        [ScriptMethod(name:"门神 大地的呼唤 (斜十字获取)",
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
        
        [ScriptMethod(name:"门神 大地的呼唤 (指路)",
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
        
        [ScriptMethod(name:"门神 大地的呼唤 (子阶段3控制)",
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
        
        [ScriptMethod(name:"门神 期间群狼剑机制 (子阶段4控制)",
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
        
        [ScriptMethod(name:"门神 光狼召唤 (小怪位置获取)",
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
        
        [ScriptMethod(name:"门神 光狼召唤 (方向获取)",
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
        
        [ScriptMethod(name:"门神 光狼召唤 (方向)",
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

                prompt="顺时针";

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
                
                prompt="逆时针";
                
            }
            
            System.Threading.Thread.MemoryBarrier();
            
            addRotationHasBeenDrawn=true;
            
            if(!string.IsNullOrWhiteSpace(prompt)) {
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }

        }
        
        [ScriptMethod(name:"门神 光狼召唤 (方向销毁)",
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
        
        [ScriptMethod(name:"门神 光狼召唤 (初始小怪指路)",
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

                    prompt="去土狼首";

                }
                
                if(targetId==idOfTheStoneWolfAdd) {
                    
                    prompt="去风狼首";
                    
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

                    prompt="去土狼首";

                }
                
                if(sourceId==idOfTheStoneWolfAdd) {
                    
                    prompt="去风狼首";
                    
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
        
        [ScriptMethod(name:"门神 光狼召唤 (小怪获取)",
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
        
        [ScriptMethod(name:"门神 光狼召唤 (防火墙状态维护)",
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
        
        [ScriptMethod(name:"门神 光狼召唤 (死宣状态获取)",
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
        
        [ScriptMethod(name:"门神 光狼召唤 (轮次控制)",
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
        
        [ScriptMethod(name:"门神 光狼召唤 (死刑)",
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
        
        [ScriptMethod(name:"门神 光狼召唤 (直线)",
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
        
        [ScriptMethod(name:"门神 光狼召唤 (风之咒痕指路)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:41956"],
            suppress:2500)]
    
        public void Phase_1_Tactical_Pack_Windborne_End_Guidance(Event @event,ScriptAccessory accessory) {

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

            if(!windborneEndWasApplied[myIndex]) {

                return;

            }

            if(idOfTheEarthFont==null||idOfTheWindWolfAdd==null) {

                return;

            }
            
            // From 0s to 1s:
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=((ulong)idOfTheWindFont);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=1000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=((ulong)idOfTheWindFont);
            currentProperties.TargetObject=((ulong)idOfTheStoneWolfAdd);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=1000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(1.5f);
            currentProperties.Owner=((ulong)idOfTheWindFont);
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=1000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
            // From 1s:
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_1_Tactical_Pack_Windborne_End_Guidance_1";
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=((ulong)idOfTheWindFont);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=1000;
            currentProperties.DestoryAt=15500;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_1_Tactical_Pack_Windborne_End_Guidance_2";
            currentProperties.Scale=new(2);
            currentProperties.Owner=((ulong)idOfTheWindFont);
            currentProperties.TargetObject=((ulong)idOfTheStoneWolfAdd);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=1000;
            currentProperties.DestoryAt=15500;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_1_Tactical_Pack_Windborne_End_Guidance_3";
            currentProperties.Scale=new(1.5f);
            currentProperties.Owner=((ulong)idOfTheWindFont);
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=1000;
            currentProperties.DestoryAt=15500;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
            System.Threading.Thread.MemoryBarrier();

            windGuidanceHasBeenDrawn=true;

        }
        
        [ScriptMethod(name:"门神 光狼召唤 (风之咒痕指路2)",
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

            if(idOfTheEarthFont==null||idOfTheWindWolfAdd==null) {

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
        
        [ScriptMethod(name:"门神 光狼召唤 (土之咒痕指路)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:regex:^(41965|43137|43519)$"],
            suppress:2500)]
    
        public void Phase_1_Tactical_Pack_Earthborne_End_Guidance(Event @event,ScriptAccessory accessory) {

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

            if(windborneEndWasApplied[myIndex]) {

                return;

            }

            if(idOfTheWindFont==null||idOfTheStoneWolfAdd==null) {

                return;

            }
            
            // From 0s to 4s:
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=((ulong)idOfTheEarthFont);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=4000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(2);
            currentProperties.Owner=((ulong)idOfTheEarthFont);
            currentProperties.TargetObject=((ulong)idOfTheWindWolfAdd);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultDangerColor;
            currentProperties.DestoryAt=4000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Scale=new(1.5f);
            currentProperties.Owner=((ulong)idOfTheEarthFont);
            currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
            currentProperties.DestoryAt=4000;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
            // From 4s:
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_1_Tactical_Pack_Earthborne_End_Guidance_1";
            currentProperties.Scale=new(2);
            currentProperties.Owner=accessory.Data.Me;
            currentProperties.TargetObject=((ulong)idOfTheEarthFont);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=4000;
            currentProperties.DestoryAt=15500;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_1_Tactical_Pack_Earthborne_End_Guidance_2";
            currentProperties.Scale=new(2);
            currentProperties.Owner=((ulong)idOfTheEarthFont);
            currentProperties.TargetObject=((ulong)idOfTheWindWolfAdd);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=4000;
            currentProperties.DestoryAt=15500;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_1_Tactical_Pack_Earthborne_End_Guidance_3";
            currentProperties.Scale=new(1.5f);
            currentProperties.Owner=((ulong)idOfTheEarthFont);
            currentProperties.Color=accessory.Data.DefaultSafeColor;
            currentProperties.Delay=4000;
            currentProperties.DestoryAt=15500;
            
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
            
            System.Threading.Thread.MemoryBarrier();

            earthGuidanceHasBeenDrawn=true;

        }
        
        [ScriptMethod(name:"门神 光狼召唤 (土之咒痕指路2)",
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

            if(idOfTheWindFont==null||idOfTheStoneWolfAdd==null) {

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
        
        [ScriptMethod(name:"门神 光狼召唤 (子阶段5控制)",
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
        
        [ScriptMethod(name:"门神 大地之怒 (光牙直线)",
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
        
        [ScriptMethod(name:"门神 大地之怒 (分摊)",
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
        
        [ScriptMethod(name:"门神 大地之怒 (外侧光牙获取)",
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

            newNorthGuidanceSemaphore.Set();

        }
        
        [ScriptMethod(name:"门神 大地之怒 (分摊获取)",
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
        
        [ScriptMethod(name:"门神 大地之怒 (真北箭头)",
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
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();
                    
            currentProperties.Scale=new(2);
            currentProperties.Position=new Vector3(100,0,97);
            currentProperties.TargetPosition=new Vector3(100,0,91);
            currentProperties.ScaleMode|=ScaleMode.YByDistance;
            currentProperties.Color=colourOfDirectionIndicators.V4.WithW(1);
            currentProperties.Delay=8500;
            currentProperties.DestoryAt=5750;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);

        }
        
        [ScriptMethod(name:"门神 大地之怒 (第一轮指路)",
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

            if(stratOfPhase1==StratsOfPhase1.MMW攻略组与苏帕酱噗) {
                
                Vector3 rawNorthPosition=new Vector3(100,0,89);
                Vector3 rawEastPosition=new Vector3(111,0,100);
                Vector3 rawSouthPosition=new Vector3(100,0,111);
                Vector3 rawWestPosition=new Vector3(89,0,100);
                Vector3 stackPosition1=rotatePosition(new Vector3(100,0,99),ARENA_CENTER_OF_PHASE_1,rotationOfTheOuterFang);
                Vector3 stackPosition2=rotatePosition(new Vector3(100,0,101),ARENA_CENTER_OF_PHASE_1,rotationOfTheOuterFang);
                Vector3 northPosition1=ARENA_CENTER_OF_PHASE_1,northPosition2=ARENA_CENTER_OF_PHASE_1;
                Vector3 eastPosition1=ARENA_CENTER_OF_PHASE_1,eastPosition2=ARENA_CENTER_OF_PHASE_1;
                Vector3 southPosition1=ARENA_CENTER_OF_PHASE_1,southPosition2=ARENA_CENTER_OF_PHASE_1;
                Vector3 westPosition1=ARENA_CENTER_OF_PHASE_1,westPosition2=ARENA_CENTER_OF_PHASE_1;

                if(Math.Abs(rotationOfTheOuterFang-Math.PI/4*1)<Math.PI*0.05) {
                    
                    northPosition1=rotatePosition(rawNorthPosition,ARENA_CENTER_OF_PHASE_1,-Math.PI/9);
                    northPosition2=rawNorthPosition;
                    
                    eastPosition1=rotatePosition(rawEastPosition,ARENA_CENTER_OF_PHASE_1,Math.PI/9);
                    eastPosition2=rawEastPosition;
                    
                    southPosition1=rawSouthPosition;
                    southPosition2=rotatePosition(rawSouthPosition,ARENA_CENTER_OF_PHASE_1,-Math.PI/9);
                    
                    westPosition1=rawWestPosition;
                    westPosition2=rotatePosition(rawWestPosition,ARENA_CENTER_OF_PHASE_1,Math.PI/9);

                }
                
                if(Math.Abs(rotationOfTheOuterFang-Math.PI/4*3)<Math.PI*0.05) {
                    
                    northPosition1=rawNorthPosition;
                    northPosition2=rotatePosition(rawNorthPosition,ARENA_CENTER_OF_PHASE_1,Math.PI/9);
                    
                    eastPosition1=rotatePosition(rawEastPosition,ARENA_CENTER_OF_PHASE_1,-Math.PI/9);
                    eastPosition2=rawEastPosition;
                    
                    southPosition1=rotatePosition(rawSouthPosition,ARENA_CENTER_OF_PHASE_1,Math.PI/9);
                    southPosition2=rawSouthPosition;
                    
                    westPosition1=rawWestPosition;
                    westPosition2=rotatePosition(rawWestPosition,ARENA_CENTER_OF_PHASE_1,-Math.PI/9);

                }
                
                if(Math.Abs(rotationOfTheOuterFang-Math.PI/4*5)<Math.PI*0.05) {
                    
                    northPosition1=rawNorthPosition;
                    northPosition2=rotatePosition(rawNorthPosition,ARENA_CENTER_OF_PHASE_1,-Math.PI/9);
                    
                    eastPosition1=rawEastPosition;
                    eastPosition2=rotatePosition(rawEastPosition,ARENA_CENTER_OF_PHASE_1,Math.PI/9);
                    
                    southPosition1=rotatePosition(rawSouthPosition,ARENA_CENTER_OF_PHASE_1,-Math.PI/9);
                    southPosition2=rawSouthPosition;
                    
                    westPosition1=rotatePosition(rawWestPosition,ARENA_CENTER_OF_PHASE_1,Math.PI/9);
                    westPosition2=rawWestPosition;

                }
                
                if(Math.Abs(rotationOfTheOuterFang-Math.PI/4*7)<Math.PI*0.05) {
                    
                    northPosition1=rotatePosition(rawNorthPosition,ARENA_CENTER_OF_PHASE_1,Math.PI/9);
                    northPosition2=rawNorthPosition;
                    
                    eastPosition1=rawEastPosition;
                    eastPosition2=rotatePosition(rawEastPosition,ARENA_CENTER_OF_PHASE_1,-Math.PI/9);
                    
                    southPosition1=rawSouthPosition;
                    southPosition2=rotatePosition(rawSouthPosition,ARENA_CENTER_OF_PHASE_1,Math.PI/9);
                    
                    westPosition1=rotatePosition(rawWestPosition,ARENA_CENTER_OF_PHASE_1,-Math.PI/9);
                    westPosition2=rawWestPosition;

                }
                
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

                        prompt="分摊";

                    }

                    else {

                        myPosition1=myIndex switch {
                            
                            0 => northPosition1,
                            1 => eastPosition1,
                            2 => westPosition1,
                            3 => southPosition1,
                            _ => ARENA_CENTER_OF_PHASE_1
                            
                        };
                        
                        myPosition2=myIndex switch {
                            
                            0 => northPosition2,
                            1 => eastPosition2,
                            2 => westPosition2,
                            3 => southPosition2,
                            _ => ARENA_CENTER_OF_PHASE_1
                            
                        };
                        
                        prompt="分散";

                    }
                    
                }

                else {
                    
                    if(isDps(myIndex)) {
                        
                        myPosition1=myIndex switch {
                            
                            4 => westPosition1,
                            5 => southPosition1,
                            6 => northPosition1,
                            7 => eastPosition1,
                            _ => ARENA_CENTER_OF_PHASE_1
                            
                        };
                        
                        myPosition2=myIndex switch {
                            
                            4 => westPosition2,
                            5 => southPosition2,
                            6 => northPosition2,
                            7 => eastPosition2,
                            _ => ARENA_CENTER_OF_PHASE_1
                            
                        };
                        
                        prompt="分散";

                    }

                    else {
                        
                        myPosition1=stackPosition1;
                        myPosition2=stackPosition2;
                        
                        prompt="分摊";
                        
                    }
                    
                }

                if(Vector3.Equals(myPosition1,ARENA_CENTER_OF_PHASE_1)||Vector3.Equals(myPosition2,ARENA_CENTER_OF_PHASE_1)) {

                    return;

                }
                
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
        
        [ScriptMethod(name:"门神 大地之怒 (残影直线)",
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
        
        [ScriptMethod(name:"门神 大地之怒 (风狼首直线)",
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
        
        [ScriptMethod(name:"门神 大地之怒 (残影获取)",
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

                if(Vector3.Distance(sourcePosition,new Vector3(100,0,92.5f))<0.375) {
                    
                    shadowsAreOnTheCardinals=true;
                    
                }
                
                System.Threading.Thread.MemoryBarrier();

                if(numberOfShadows>=5) {

                    shadowSemaphore.Set();
                    
                    accessory.Log.Debug($"shadowsAreOnTheCardinals={shadowsAreOnTheCardinals}");

                }
                
            }

        }
        
        [ScriptMethod(name:"门神 大地之怒 (第二轮指路)",
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

            if(stratOfPhase1==StratsOfPhase1.MMW攻略组与苏帕酱噗) {
                
                double topRotation=0;
                
                if(!shadowsAreOnTheCardinals) {

                    topRotation+=Math.PI/5;

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

                        prompt="分散";

                    }

                    else {

                        myRotation=topRotation;
                        
                        prompt="分摊";

                    }
                    
                }

                else {
                    
                    if(isDps(myIndex)) {
                        
                        myRotation=topRotation;
                        
                        prompt="分摊";

                    }

                    else {
                        
                        myRotation=myIndex switch {
                            
                            2 => lowerLeftRotation,
                            3 => lowerRightRotation,
                            0 => leftmostRotation,
                            1 => rightmostRotation,
                            _ => 0
                            
                        };

                        prompt="分散";
                        
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
        
        [ScriptMethod(name:"门神 大地之怒 (子阶段6控制)",
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
            
            newNorthGuidanceSemaphore.Reset();
            roleStackSemaphore.Reset();
            shadowSemaphore.Reset();
            
            accessory.Log.Debug("Now moving to Phase 1 Sub-phase 7.");
        
        }
        
        [ScriptMethod(name:"门神 期间群狼剑机制 (子阶段7控制)",
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
        
        [ScriptMethod(name:"门神 幻狼召唤 (半场刀)",
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
        
        [ScriptMethod(name:"门神 幻狼召唤 (分摊)",
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
        
        [ScriptMethod(name:"门神 幻狼召唤 (半场刀获取)",
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
        
        [ScriptMethod(name:"门神 幻狼召唤 (分摊获取)",
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
        
        [ScriptMethod(name:"门神 幻狼召唤 (第一轮指路)",
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
            
            if(stratOfPhase1==StratsOfPhase1.MMW攻略组与苏帕酱噗) {
                
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

                        prompt="分摊";

                    }

                    else {

                        myPosition=myIndex switch {
                            
                            0 => leftMeleePosition,
                            1 => rightMeleePosition,
                            2 => leftRangePosition,
                            3 => rightRangePosition,
                            _ => ARENA_CENTER_OF_PHASE_1
                            
                        };
                        
                        prompt="分散";

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
                        
                        prompt="分散";

                    }

                    else {
                        
                        myPosition=stackPosition;
                        
                        prompt="分摊";
                        
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
        
        [ScriptMethod(name:"门神 幻狼召唤 (第二轮指路)",
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
            
            if(stratOfPhase1==StratsOfPhase1.MMW攻略组与苏帕酱噗) {
                
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

                        prompt="分散";

                    }

                    else {
                        
                        myPosition=stackPosition;

                        prompt="分摊";

                    }
                    
                }

                else {
                    
                    if(isDps(myIndex)) {
                        
                        myPosition=stackPosition;
                        
                        prompt="分摊";

                    }

                    else {
                        
                        myPosition=myIndex switch {
                            
                            0 => leftMeleePosition,
                            1 => rightMeleePosition,
                            2 => leftRangePosition,
                            3 => rightRangePosition,
                            _ => ARENA_CENTER_OF_PHASE_1
                            
                        };
                        
                        prompt="分散";
                        
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
        
        [ScriptMethod(name:"门神 幻狼召唤 (直线)",
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
        
        [ScriptMethod(name:"门神 幻狼召唤 (子阶段8控制)",
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
        
        [ScriptMethod(name:"门神 过场动画 (阶段1控制)",
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
        
        [ScriptMethod(name:"本体 Boss实体ID获取",
            eventType:EventTypeEnum.Targetable,
            userControl:false)]

        public void 本体_Boss实体ID获取(Event @event, ScriptAccessory accessory) {

            if(!string.IsNullOrWhiteSpace(phase2BossId)) {

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

            phase2BossId=@event["SourceId"];

        }
        
        [ScriptMethod(name:"本体 Boss中轴线及其附属箭头",
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
            currentProperties.Owner=sourceId;
            currentProperties.Color=colourOfTheBossAxis.V4.WithW(1);
            currentProperties.DestoryAt=420000;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Straight,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_2_NorthSouth_Axis_And_Arrows_2";
            currentProperties.Scale=new(2,9);
            currentProperties.Owner=sourceId;
            currentProperties.Color=colourOfTheBossAxis.V4.WithW(1);
            currentProperties.DestoryAt=420000;
            currentProperties.Offset=new Vector3(5.562f,0,4.5f);
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Arrow,currentProperties);
            
            currentProperties=accessory.Data.GetDefaultDrawProperties();

            currentProperties.Name="Phase_2_NorthSouth_Axis_And_Arrows_3";
            currentProperties.Scale=new(2,9);
            currentProperties.Owner=sourceId;
            currentProperties.Color=colourOfTheBossAxis.V4.WithW(1);
            currentProperties.DestoryAt=420000;
            currentProperties.Offset=new Vector3(-5.562f,0,4.5f);
        
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
        
        [ScriptMethod(name:"本体 爆震 (轮次控制)",
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
        
        [ScriptMethod(name:"本体 爆震 (指路)",
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

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {
                
                if(!convertObjectId(phase2BossId, out var bossId)) {
            
                    return;
            
                }
                
                var bossObject=accessory.Data.Objects.SearchById(bossId);

                if(bossObject==null) {

                    return;

                }

                double bossRotation=(convertRotation(bossObject.Rotation)-Math.PI+2*Math.PI)%(2*Math.PI);

                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                if(isInGroup1(myIndex)) {

                    // The correct platform:
                        
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                    // The another group:
                        
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=5125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                }

                if(isInGroup2(myIndex)) {
                    
                    // The correct platform:
                        
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=5125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                    // The another group:
                        
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=5125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }
                
            }
        
        }
        
        [ScriptMethod(name:"本体 闪光炮",
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
        
        [ScriptMethod(name:"本体 魔光 (获取)",
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
        
        [ScriptMethod(name:"本体 魔光 (指路)",
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

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {
                
                if(!convertObjectId(phase2BossId, out var bossId)) {
            
                    return;
            
                }
                
                var bossObject=accessory.Data.Objects.SearchById(bossId);

                if(bossObject==null) {

                    return;

                }

                double bossRotation=(convertRotation(bossObject.Rotation)-Math.PI+2*Math.PI)%(2*Math.PI);
                
                Vector3 myPosition=ARENA_CENTER_OF_PHASE_2;
                string prompt=string.Empty;
                
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

                    prompt="留在当前平台";

                }

                else {
                        
                    List<int> marksOnTheLeft=[],marksOnTheRight=[];

                    if(playerWasMarkedByAUltraviolentRay[0])marksOnTheLeft.Add(0);
                    if(playerWasMarkedByAUltraviolentRay[1])marksOnTheRight.Add(1);
                    
                    if(playerWasMarkedByAUltraviolentRay[4])marksOnTheLeft.Add(4);
                    if(playerWasMarkedByAUltraviolentRay[5])marksOnTheRight.Add(5);
                        
                    if(playerWasMarkedByAUltraviolentRay[6])marksOnTheLeft.Add(6);
                    if(playerWasMarkedByAUltraviolentRay[7])marksOnTheRight.Add(7);
                    
                    if(playerWasMarkedByAUltraviolentRay[2])marksOnTheLeft.Add(2);
                    if(playerWasMarkedByAUltraviolentRay[3])marksOnTheRight.Add(3);

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
                                prompt="去上";

                                break;

                            }
                                
                            case 2: {
                                    
                                myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);
                                prompt="留在当前平台";

                                break;

                            }
                                
                            case 3: {
                                    
                                myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTH);
                                prompt="去下";

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
                                prompt="去上";

                                break;

                            }
                                
                            case 2: {
                                    
                                myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);
                                prompt="留在当前平台";

                                break;

                            }
                                
                            case 3: {
                                    
                                myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTH);
                                prompt="去下";

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
                
                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=rotatePosition(myPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
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
        
        [ScriptMethod(name:"本体 魔光 (销毁)",
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
        
        [ScriptMethod(name:"本体 双牙击 (轮次控制)",
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
        
        [ScriptMethod(name:"本体 双牙击 (指路)",
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

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {
                
                if(!convertObjectId(phase2BossId, out var bossId)) {
            
                    return;
            
                }
                
                var bossObject=accessory.Data.Objects.SearchById(bossId);

                if(bossObject==null) {

                    return;

                }

                double bossRotation=(convertRotation(bossObject.Rotation)-Math.PI+2*Math.PI)%(2*Math.PI);

                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                if(isTank(myIndex)) {

                    if(myIndex==0) {
                        
                        // The correct platform:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=7125;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=7125;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                        // The another tank:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.DestoryAt=7125;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                    }

                    if(myIndex==1) {
                        
                        // The correct platform:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=7125;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=7125;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                        // The another tank:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.DestoryAt=7125;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                    }
                    
                    // Others:
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTH),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=7125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

                else {
                    
                    // The safe platform:
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(2);
                    currentProperties.Owner=accessory.Data.Me;
                    currentProperties.TargetPosition=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTH),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.ScaleMode|=ScaleMode.YByDistance;
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=7125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTH),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.Color=accessory.Data.DefaultSafeColor;
                    currentProperties.DestoryAt=7125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                    // Dangerous platforms:
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=7125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=7125;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                }
                
            }
        
        }
        
        [ScriptMethod(name:"本体 铠袖一触 (左右刀)",
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
        
        [ScriptMethod(name:"本体 铠袖一触 (钢铁月环)",
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

                prompt="就位后远离";

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
                
                prompt="就位后靠近";
                        
            }
            
            if(!string.IsNullOrWhiteSpace(prompt)) {

                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,6875);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
            }
        
        }
        
        [ScriptMethod(name:"本体 铠袖一触 (轮次控制)",
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
        
        [ScriptMethod(name:"本体 铠袖一触 (指路)",
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
        
        [ScriptMethod(name:"本体 刚刃一闪 (预站位指路)",
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

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {
                
                if(!convertObjectId(phase2BossId, out var bossId)) {
            
                    return;
            
                }
                
                var bossObject=accessory.Data.Objects.SearchById(bossId);

                if(bossObject==null) {

                    return;

                }

                double bossRotation=(convertRotation(bossObject.Rotation)-Math.PI+2*Math.PI)%(2*Math.PI);
                
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
                currentProperties.TargetPosition=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTH),ARENA_CENTER_OF_PHASE_2,bossRotation);
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=8250;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(8);
                currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTH),ARENA_CENTER_OF_PHASE_2,bossRotation);
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=8250;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                
                prompt="去Boss面前引导刚刃一闪";
                        
                if(enablePrompts) {
                    
                    accessory.Method.TextInfo(prompt,5750);
                    
                }
                    
                accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);
                
                System.Threading.Thread.MemoryBarrier();

                firstMooncleaverSemaphore.WaitOne();
                
                System.Threading.Thread.MemoryBarrier();
                
                // From 8.25s:

                if(bossRotationDuringPurgeAndTempest==null) {

                    return;

                }

                bossRotation=((double)bossRotationDuringPurgeAndTempest);
                prompt=string.Empty;
                
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(8);
                currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTH),ARENA_CENTER_OF_PHASE_2,bossRotation);
                currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                currentProperties.DestoryAt=4875;
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                if(!isTank(myIndex)) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=4875;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.Color=accessory.Data.DefaultDangerColor;
                    currentProperties.DestoryAt=4875;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);

                    prompt="去上方平台待机";

                }

                else {

                    if(myIndex==0) {
                        
                        // The correct platform:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=4875;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=4875;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                        prompt="去左侧靠下平台待机";
                        
                        // The another tank:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.DestoryAt=4875;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                    }

                    if(myIndex==1) {
                        
                        // The correct platform:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=4875;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=4875;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                        prompt="去右侧靠下平台待机";
                        
                        // The another tank:
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(8);
                        currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.Color=accessory.Data.DefaultDangerColor;
                        currentProperties.DestoryAt=4875;
        
                        accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                        
                    }
                    
                }
                
                if(!string.IsNullOrWhiteSpace(prompt)) {
                    
                    if(enablePrompts) {
                    
                        accessory.Method.TextInfo(prompt,2375);
                    
                    }
                    
                    accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);

                }
                
            }
        
        }
        
        [ScriptMethod(name:"本体 刚刃一闪 (Boss面向获取)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42085"],
            userControl:false)]

        public void 本体_刚刃一闪_Boss面向获取(Event @event,ScriptAccessory accessory) {

            if(bossRotationDuringPurgeAndTempest!=null) {
                
                return;
                
            }
            
            if(currentPhase!=2) {

                return;

            }

            if(currentSubPhase!=1) {

                return;

            }
            
            double sourceRotation=0;

            try {

                sourceRotation=JsonConvert.DeserializeObject<double>(@event["SourceRotation"]);

            } catch(Exception e) {
                
                accessory.Log.Error("SourceRotation deserialization failed.");

                return;

            }

            bossRotationDuringPurgeAndTempest=(convertRotation(sourceRotation)-Math.PI+2*Math.PI)%(2*Math.PI);
            
            System.Threading.Thread.MemoryBarrier();

            firstMooncleaverSemaphore.Set();

        }
        
        [ScriptMethod(name:"本体 风之残响",
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
        
        [ScriptMethod(name:"本体 土之残响",
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
        
        [ScriptMethod(name:"本体 风震魔印 (获取)",
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
        
        [ScriptMethod(name:"本体 风震魔印 (指路)",
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

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {

                if(bossRotationDuringPurgeAndTempest==null) {

                    return;

                }

                double bossRotation=((double)bossRotationDuringPurgeAndTempest);
                
                string prompt=string.Empty;
                bool isWarning=false;
                
                if(isTank(myIndex)) {

                    if(myIndex==0) {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=10000;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

                        if(mtWasMarkerByPatienceOfWind) {
                            
                            prompt="风之残响点名,退避另一个T";
                            isWarning=false;

                        }

                        else {

                            prompt="挑衅!";
                            isWarning=true;

                        }
                    
                    }
                
                    if(myIndex==1) {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=10000;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);

                        if(!mtWasMarkerByPatienceOfWind) {
                            
                            prompt="风之残响点名,退避另一个T";
                            isWarning=false;

                        }

                        else {

                            prompt="挑衅!";
                            isWarning=true;

                        }
                    
                    }
                
                }

                else {

                    if(mtWasMarkerByPatienceOfWind) {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=rotatePosition(getPlatformCenter(PlatformsOfPhase2.NORTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=10000;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                        
                        prompt="和MT同侧";
                        
                    }

                    else {
                        
                        currentProperties=accessory.Data.GetDefaultDrawProperties();

                        currentProperties.Scale=new(2);
                        currentProperties.Owner=accessory.Data.Me;
                        currentProperties.TargetPosition=rotatePosition(getPlatformCenter(PlatformsOfPhase2.NORTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                        currentProperties.ScaleMode|=ScaleMode.YByDistance;
                        currentProperties.Color=accessory.Data.DefaultSafeColor;
                        currentProperties.DestoryAt=10000;
        
                        accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
                        
                        prompt="和ST同侧";
                        
                    }
                
                }

                if(mtWasMarkerByPatienceOfWind) {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=10000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.NORTHEAST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=10000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                }

                else {
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
                    currentProperties.Color=colourOfHighlyDangerousAttacks.V4.WithW(1);
                    currentProperties.DestoryAt=10000;
        
                    accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Circle,currentProperties);
                    
                    currentProperties=accessory.Data.GetDefaultDrawProperties();

                    currentProperties.Scale=new(8);
                    currentProperties.Position=rotatePosition(getPlatformCenter(PlatformsOfPhase2.NORTHWEST),ARENA_CENTER_OF_PHASE_2,bossRotation);
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
        
        [ScriptMethod(name:"本体 风震魔印 (子阶段1控制)",
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

            firstMooncleaverSemaphore.Reset();
            elementalPurgeSemaphore.Reset();
            
            accessory.Log.Debug("Now moving to Phase 2 Sub-phase 2.");
        
        }
        
        [ScriptMethod(name:"本体 风狼阵 (预站位指路)",
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

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {

                if(bossRotationDuringPurgeAndTempest==null) {

                    return;

                }
                
                double bossRotation=((double)bossRotationDuringPurgeAndTempest);
                
                Vector3 myPosition=ARENA_CENTER_OF_PHASE_2;

                myPosition=myIndex switch {
                
                    0 => getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),
                    1 => getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),
                    2 => getPlatformCenter(PlatformsOfPhase2.SOUTHWEST),
                    3 => getPlatformCenter(PlatformsOfPhase2.SOUTHEAST),
                    4 => getPlatformCenter(PlatformsOfPhase2.NORTHWEST),
                    5 => getPlatformCenter(PlatformsOfPhase2.NORTHEAST),
                    6 => getPlatformCenter(PlatformsOfPhase2.NORTHWEST),
                    7 => getPlatformCenter(PlatformsOfPhase2.NORTHEAST),
                    _ => ARENA_CENTER_OF_PHASE_2
                
                };

                if(myPosition.Equals(ARENA_CENTER_OF_PHASE_2)) {

                    return;

                }
            
                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=rotatePosition(myPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=14000;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Position=rotatePosition(myPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=14000;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Circle,currentProperties);
                
            }
        
        }
        
        [ScriptMethod(name:"本体 飓风之相 (预站位指路)",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:42095"],
            suppress:2500)]
    
        public void 本体_飓风之相_预站位指路(Event @event,ScriptAccessory accessory) {

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
            
            if(bossRotationDuringPurgeAndTempest==null) {

                return;

            }

            double bossRotation=((double)bossRotationDuringPurgeAndTempest);

            Vector3 mtPosition=rotatePosition(new Vector3(86.80f,-150,99.52f),ARENA_CENTER_OF_PHASE_2,bossRotation);
            Vector3 group1Position=rotatePosition(new Vector3(82.53f,-150,99.42f),ARENA_CENTER_OF_PHASE_2,bossRotation);
            Vector3 stPosition=rotatePosition(new Vector3(113.29f,-150,99.33f),ARENA_CENTER_OF_PHASE_2,bossRotation);
            Vector3 group2Position=rotatePosition(new Vector3(117.34f,-150,99.34f),ARENA_CENTER_OF_PHASE_2,bossRotation);
            // 没作图,用鼠标指向凑合一下,我懒了。

            Vector3 myPosition=ARENA_CENTER_OF_PHASE_2;
            string prompt=string.Empty;

            if(isTank(myIndex)) {

                if(myIndex==0) {
                    
                    myPosition=mtPosition;
                    
                }
                
                if(myIndex==1) {
                    
                    myPosition=stPosition;
                    
                }

                prompt="引导连线";

            }

            else {
                
                if(isInGroup1(myIndex)) {
                    
                    myPosition=group1Position;
                    
                }
                
                if(isInGroup2(myIndex)) {
                    
                    myPosition=group2Position;
                    
                }
                
                prompt="站在T身后";
                
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
            currentProperties.DestoryAt=4500;
        
            accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
            if(enablePrompts) {
                    
                accessory.Method.TextInfo(prompt,2000);
                    
            }
                    
            accessory.tts(prompt,enableVanillaTts,enableDailyRoutinesTts);

        }
        
        [ScriptMethod(name:"本体 飓风之相 (直线)",
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

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {
                
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
                   (myIndex==5||myIndex==7)) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;

                    promptTime=currentProperties.Delay;

                }
                
                if((!tetherBeginsFromTheWest)
                   &&
                   (myIndex==4||myIndex==6)) {
                    
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
                   (myIndex==1||myIndex==3)) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;

                    promptTime=currentProperties.Delay;

                }
                
                if((!tetherBeginsFromTheWest)
                   &&
                   (myIndex==0||myIndex==2)) {
                    
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
                   (myIndex==0||myIndex==2)) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;

                    promptTime=currentProperties.Delay;

                }
                
                if((!tetherBeginsFromTheWest)
                   &&
                   (myIndex==1||myIndex==3)) {
                    
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
                   (myIndex==4||myIndex==6)) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;

                    promptTime=currentProperties.Delay;

                }
                
                if((!tetherBeginsFromTheWest)
                   &&
                   (myIndex==5||myIndex==7)) {
                    
                    currentProperties.Color=accessory.Data.DefaultSafeColor;

                    promptTime=currentProperties.Delay;

                }
        
                accessory.Method.SendDraw(DrawModeEnum.Default,DrawTypeEnum.Rect,currentProperties);

                string prompt="引导直线";
                
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
        
        [ScriptMethod(name:"本体 飓风之相 (初始化与监控)",
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
                        2 => PlatformsOfPhase2.SOUTHWEST,
                        3 => PlatformsOfPhase2.SOUTHEAST,
                        4 => PlatformsOfPhase2.NORTHWEST,
                        5 => PlatformsOfPhase2.NORTHEAST,
                        6 => PlatformsOfPhase2.NORTHWEST,
                        7 => PlatformsOfPhase2.NORTHEAST,
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
        
        [ScriptMethod(name:"本体 飓风之相 (轮次控制)",
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

        private void drawGuidanceForRanged(int partyIndex,Vector3 targetPosition,int round,ScriptAccessory accessory) {

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
        
        [ScriptMethod(name:"本体 飓风之相 (远程指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42097"])]
    
        public void Phase_2_Twofold_Tempest_Ranged_Guidance(Event @event,ScriptAccessory accessory) {

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

            if(!isRanged(myIndex)) {

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            tempestGuidanceSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {

                if(bossRotationDuringPurgeAndTempest==null) {

                    return;

                }

                double bossRotation=((double)bossRotationDuringPurgeAndTempest);
                
                int myPlatform=(int)(myIndex switch {
                    
                    2 => PlatformsOfPhase2.SOUTHWEST,
                    3 => PlatformsOfPhase2.SOUTHEAST,
                    6 => PlatformsOfPhase2.NORTHWEST,
                    7 => PlatformsOfPhase2.NORTHEAST,
                    _ => PlatformsOfPhase2.SOUTH
                    
                });

                if(myPlatform==((int)PlatformsOfPhase2.SOUTH)) {

                    return;

                }
                
                Vector3 myStackPosition=rotatePosition(new Vector3(100,-150,75.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myLinePosition=rotatePosition(new Vector3(100,-150,89.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myStandbyPosition=getPlatformCenter((PlatformsOfPhase2)myPlatform);
                
                myStackPosition=rotatePosition(myStackPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                myLinePosition=rotatePosition(myLinePosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                myStandbyPosition=rotatePosition(myStandbyPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);

                if(myIndex==2) {

                    if(tetherBeginsFromTheWest) {
                        
                        drawGuidanceForRanged(myIndex,myStackPosition,1,accessory);
                        drawGuidanceForRanged(myIndex,myStandbyPosition,2,accessory);
                        drawGuidanceForRanged(myIndex,myLinePosition,3,accessory);
                        drawGuidanceForRanged(myIndex,myStandbyPosition,4,accessory);
                        
                    }

                    else {
                        
                        drawGuidanceForRanged(myIndex,myStandbyPosition,1,accessory);
                        drawGuidanceForRanged(myIndex,myLinePosition,2,accessory);
                        drawGuidanceForRanged(myIndex,myStandbyPosition,3,accessory);
                        drawGuidanceForRanged(myIndex,myStackPosition,4,accessory);
                        
                    }
                    
                }
                
                if(myIndex==3) {

                    if(tetherBeginsFromTheWest) {
                        
                        drawGuidanceForRanged(myIndex,myStandbyPosition,1,accessory);
                        drawGuidanceForRanged(myIndex,myLinePosition,2,accessory);
                        drawGuidanceForRanged(myIndex,myStandbyPosition,3,accessory);
                        drawGuidanceForRanged(myIndex,myStackPosition,4,accessory);
                        
                    }

                    else {
                        
                        drawGuidanceForRanged(myIndex,myStackPosition,1,accessory);
                        drawGuidanceForRanged(myIndex,myStandbyPosition,2,accessory);
                        drawGuidanceForRanged(myIndex,myLinePosition,3,accessory);
                        drawGuidanceForRanged(myIndex,myStandbyPosition,4,accessory);
                        
                    }
                    
                }
                
                if(myIndex==6) {

                    if(tetherBeginsFromTheWest) {
                        
                        drawGuidanceForRanged(myIndex,myStandbyPosition,1,accessory);
                        drawGuidanceForRanged(myIndex,myStackPosition,2,accessory);
                        drawGuidanceForRanged(myIndex,myStandbyPosition,3,accessory);
                        drawGuidanceForRanged(myIndex,myLinePosition,4,accessory);
                        
                    }

                    else {
                        
                        drawGuidanceForRanged(myIndex,myLinePosition,1,accessory);
                        drawGuidanceForRanged(myIndex,myStandbyPosition,2,accessory);
                        drawGuidanceForRanged(myIndex,myStackPosition,3,accessory);
                        drawGuidanceForRanged(myIndex,myStandbyPosition,4,accessory);
                        
                    }
                    
                }
                
                if(myIndex==7) {

                    if(tetherBeginsFromTheWest) {
                        
                        drawGuidanceForRanged(myIndex,myLinePosition,1,accessory);
                        drawGuidanceForRanged(myIndex,myStandbyPosition,2,accessory);
                        drawGuidanceForRanged(myIndex,myStackPosition,3,accessory);
                        drawGuidanceForRanged(myIndex,myStandbyPosition,4,accessory);
                        
                    }

                    else {
                        
                        drawGuidanceForRanged(myIndex,myStandbyPosition,1,accessory);
                        drawGuidanceForRanged(myIndex,myStackPosition,2,accessory);
                        drawGuidanceForRanged(myIndex,myStandbyPosition,3,accessory);
                        drawGuidanceForRanged(myIndex,myLinePosition,4,accessory);
                        
                    }
                    
                }

            }

        }
        
        private void drawPositionGuidanceForMelee(int partyIndex,Vector3 targetPosition,ScriptAccessory accessory) {

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
        
        private void drawObjectGuidanceForMelee(int sourceIndex,int targetIndex,ScriptAccessory accessory) {

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
        
        [ScriptMethod(name:"本体 飓风之相 (MT指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42097"])]
    
        public void Phase_2_Twofold_Tempest_MT_Guidance(Event @event,ScriptAccessory accessory) {

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

            if(myIndex!=0) { // Subject to change.

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            tempestGuidanceSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {
                
                if(bossRotationDuringPurgeAndTempest==null) {

                    return;

                }

                double bossRotation=((double)bossRotationDuringPurgeAndTempest);
                
                int myPlatform=(int)(myIndex switch {
                    
                    0 => PlatformsOfPhase2.SOUTHWEST,
                    1 => PlatformsOfPhase2.SOUTHEAST,
                    4 => PlatformsOfPhase2.NORTHWEST,
                    5 => PlatformsOfPhase2.NORTHEAST,
                    _ => PlatformsOfPhase2.SOUTH
                    
                });

                if(myPlatform==((int)PlatformsOfPhase2.SOUTH)) {

                    return;

                }
                
                int carrierOnMyLeft=myIndex switch {
                    
                    0 => 4,
                    1 => -1,
                    4 => 5,
                    5 => 1,
                    _ => -1
                    
                };
                
                int carrierOnMyRight=myIndex switch {
                    
                    0 => -1,
                    1 => 5,
                    4 => 0,
                    5 => 4,
                    _ => -1
                    
                };
                
                Vector3 myStackPosition=rotatePosition(new Vector3(100,-150,75.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myLinePosition=rotatePosition(new Vector3(100,-150,89.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myStandbyPosition=getPlatformCenter((PlatformsOfPhase2)myPlatform);
                Vector3 myInterceptionPosition=new Vector3(86.82f,-150,99.72f); // Subject to change.
                // Sorry for measuring the position by the mouse pointer, I don't know how to calculate it accurately with geometric.
                // But anyway, I guess it doesn't need to be super accurate and geometrically reproducible.
                
                myStackPosition=rotatePosition(myStackPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                myLinePosition=rotatePosition(myLinePosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                myStandbyPosition=rotatePosition(myStandbyPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                myInterceptionPosition=rotatePosition(myInterceptionPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                
                // ----- Round 1 -----

                if(tetherBeginsFromTheWest) { // Subject to change.

                    if(currentTempestStackTarget!=accessory.Data.Me) {

                        int currentTargetIndex=accessory.Data.PartyList.IndexOf((uint)currentTempestStackTarget);
                        
                        if(!isLegalIndex(currentTargetIndex)) {

                            return;

                        }
                        
                        drawObjectGuidanceForMelee(myIndex,currentTargetIndex,accessory);
                        
                        System.Threading.Thread.MemoryBarrier();

                        tetherCapturingSemaphore.WaitOne();
            
                        System.Threading.Thread.MemoryBarrier();
                        
                        accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                        System.Threading.Thread.MemoryBarrier();

                        ++numberOfSteps;
                        
                        System.Threading.Thread.MemoryBarrier();

                    }
                    
                    drawPositionGuidanceForMelee(myIndex,myStackPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
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

                    drawObjectGuidanceForMelee(myIndex,carrierOnMyLeft,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherLeavingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForMelee(myIndex,myLinePosition,accessory);
                    
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

                    drawPositionGuidanceForMelee(myIndex,myLinePosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
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

                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForMelee(myIndex,myInterceptionPosition,accessory);
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForMelee(myIndex,myStackPosition,accessory);
                    
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
        
        [ScriptMethod(name:"本体 飓风之相 (ST指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42097"])]
    
        public void Phase_2_Twofold_Tempest_ST_Guidance(Event @event,ScriptAccessory accessory) {

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

            if(myIndex!=1) { // Subject to change.

                return;

            }
            
            System.Threading.Thread.MemoryBarrier();

            tempestGuidanceSemaphore.WaitOne();
            
            System.Threading.Thread.MemoryBarrier();
            
            var currentProperties=accessory.Data.GetDefaultDrawProperties();

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {
                
                if(bossRotationDuringPurgeAndTempest==null) {

                    return;

                }

                double bossRotation=((double)bossRotationDuringPurgeAndTempest);
                
                int myPlatform=(int)(myIndex switch {
                    
                    0 => PlatformsOfPhase2.SOUTHWEST,
                    1 => PlatformsOfPhase2.SOUTHEAST,
                    4 => PlatformsOfPhase2.NORTHWEST,
                    5 => PlatformsOfPhase2.NORTHEAST,
                    _ => PlatformsOfPhase2.SOUTH
                    
                });

                if(myPlatform==((int)PlatformsOfPhase2.SOUTH)) {

                    return;

                }
                
                int carrierOnMyLeft=myIndex switch {
                    
                    0 => 4,
                    1 => -1,
                    4 => 5,
                    5 => 1,
                    _ => -1
                    
                };
                
                int carrierOnMyRight=myIndex switch {
                    
                    0 => -1,
                    1 => 5,
                    4 => 0,
                    5 => 4,
                    _ => -1
                    
                };
                
                Vector3 myStackPosition=rotatePosition(new Vector3(100,-150,75.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myLinePosition=rotatePosition(new Vector3(100,-150,89.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myStandbyPosition=getPlatformCenter((PlatformsOfPhase2)myPlatform);
                Vector3 myInterceptionPosition=new Vector3(113.20f,-150,99.93f); // Subject to change.
                // Sorry for measuring the position by the mouse pointer, I don't know how to calculate it accurately with geometric.
                // But anyway, I guess it doesn't need to be super accurate and geometrically reproducible.
                
                myStackPosition=rotatePosition(myStackPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                myLinePosition=rotatePosition(myLinePosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                myStandbyPosition=rotatePosition(myStandbyPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                myInterceptionPosition=rotatePosition(myInterceptionPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                
                // ----- Round 1 -----

                if(!tetherBeginsFromTheWest) { // Subject to change.

                    if(currentTempestStackTarget!=accessory.Data.Me) {
                        
                        int currentTargetIndex=accessory.Data.PartyList.IndexOf((uint)currentTempestStackTarget);
                        
                        if(!isLegalIndex(currentTargetIndex)) {

                            return;

                        }
                        
                        drawObjectGuidanceForMelee(myIndex,currentTargetIndex,accessory);
                        
                        System.Threading.Thread.MemoryBarrier();

                        tetherCapturingSemaphore.WaitOne();
            
                        System.Threading.Thread.MemoryBarrier();
                        
                        accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                        System.Threading.Thread.MemoryBarrier();

                        ++numberOfSteps;
                        
                        System.Threading.Thread.MemoryBarrier();
                        
                    }
                    
                    drawPositionGuidanceForMelee(myIndex,myStackPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
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

                    drawObjectGuidanceForMelee(myIndex,carrierOnMyRight,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherLeavingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForMelee(myIndex,myLinePosition,accessory);
                    
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

                    drawPositionGuidanceForMelee(myIndex,myLinePosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
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

                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForMelee(myIndex,myInterceptionPosition,accessory);
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForMelee(myIndex,myStackPosition,accessory);
                    
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
        
        [ScriptMethod(name:"本体 飓风之相 (D1指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42097"])]
    
        public void Phase_2_Twofold_Tempest_D1_Guidance(Event @event,ScriptAccessory accessory) {

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

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {
                
                if(bossRotationDuringPurgeAndTempest==null) {

                    return;

                }

                double bossRotation=((double)bossRotationDuringPurgeAndTempest);
                
                int myPlatform=(int)(myIndex switch {
                    
                    0 => PlatformsOfPhase2.SOUTHWEST,
                    1 => PlatformsOfPhase2.SOUTHEAST,
                    4 => PlatformsOfPhase2.NORTHWEST,
                    5 => PlatformsOfPhase2.NORTHEAST,
                    _ => PlatformsOfPhase2.SOUTH
                    
                });

                if(myPlatform==((int)PlatformsOfPhase2.SOUTH)) {

                    return;

                }
                
                int carrierOnMyLeft=myIndex switch {
                    
                    0 => 4,
                    1 => -1,
                    4 => 5,
                    5 => 1,
                    _ => -1
                    
                };
                
                int carrierOnMyRight=myIndex switch {
                    
                    0 => -1,
                    1 => 5,
                    4 => 0,
                    5 => 4,
                    _ => -1
                    
                };
                
                Vector3 myStackPosition=rotatePosition(new Vector3(100,-150,75.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myLinePosition=rotatePosition(new Vector3(100,-150,89.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myStandbyPosition=getPlatformCenter((PlatformsOfPhase2)myPlatform);
                Vector3 interceptionPositionOnMyLeft=new Vector3(96.54f,-150,87.11f); // Subject to change.
                Vector3 interceptionPositionOnMyRight=new Vector3(89.05f,-150,92.51f); // Subject to change.
                // Sorry for measuring these three positions by the mouse pointer, I don't know how to calculate it accurately with geometric.
                // But anyway, I guess it doesn't need to be super accurate and geometrically reproducible.
                
                myStackPosition=rotatePosition(myStackPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                myLinePosition=rotatePosition(myLinePosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                myStandbyPosition=rotatePosition(myStandbyPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                interceptionPositionOnMyLeft=rotatePosition(interceptionPositionOnMyLeft,ARENA_CENTER_OF_PHASE_2,bossRotation);
                interceptionPositionOnMyRight=rotatePosition(interceptionPositionOnMyRight,ARENA_CENTER_OF_PHASE_2,bossRotation);
                
                // ----- Round 1 -----

                if(tetherBeginsFromTheWest) { // Subject to change.
                    
                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
                }
                
                else {
                    
                    drawPositionGuidanceForMelee(myIndex,myLinePosition,accessory);
                    
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
                    
                    drawPositionGuidanceForMelee(myIndex,interceptionPositionOnMyRight,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForMelee(myIndex,myStackPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
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

                    drawObjectGuidanceForMelee(myIndex,carrierOnMyLeft,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherLeavingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForMelee(myIndex,interceptionPositionOnMyLeft,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForMelee(myIndex,myStackPosition,accessory);
                    
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

                    drawPositionGuidanceForMelee(myIndex,myLinePosition,accessory);
                    
                }

                else {
                    
                    drawObjectGuidanceForMelee(myIndex,carrierOnMyRight,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherLeavingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
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
        
        [ScriptMethod(name:"本体 飓风之相 (D2指路)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42097"])]
    
        public void Phase_2_Twofold_Tempest_D2_Guidance(Event @event,ScriptAccessory accessory) {

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

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {
                
                if(bossRotationDuringPurgeAndTempest==null) {

                    return;

                }

                double bossRotation=((double)bossRotationDuringPurgeAndTempest);
                
                int myPlatform=(int)(myIndex switch {
                    
                    0 => PlatformsOfPhase2.SOUTHWEST,
                    1 => PlatformsOfPhase2.SOUTHEAST,
                    4 => PlatformsOfPhase2.NORTHWEST,
                    5 => PlatformsOfPhase2.NORTHEAST,
                    _ => PlatformsOfPhase2.SOUTH
                    
                });

                if(myPlatform==((int)PlatformsOfPhase2.SOUTH)) {

                    return;

                }
                
                int carrierOnMyLeft=myIndex switch {
                    
                    0 => 4,
                    1 => -1,
                    4 => 5,
                    5 => 1,
                    _ => -1
                    
                };
                
                int carrierOnMyRight=myIndex switch {
                    
                    0 => -1,
                    1 => 5,
                    4 => 0,
                    5 => 4,
                    _ => -1
                    
                };
                
                Vector3 myStackPosition=rotatePosition(new Vector3(100,-150,75.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myLinePosition=rotatePosition(new Vector3(100,-150,89.5f),ARENA_CENTER_OF_PHASE_2,Math.PI/5+(Math.PI*2/5*myPlatform));
                Vector3 myStandbyPosition=getPlatformCenter((PlatformsOfPhase2)myPlatform);
                Vector3 interceptionPositionOnMyLeft=new Vector3(111.05f,-150,92.84f); // Subject to change.
                Vector3 interceptionPositionOnMyRight=new Vector3(103.89f,-150,87.60f); // Subject to change.
                // Sorry for measuring these three positions by the mouse pointer, I don't know how to calculate it accurately with geometric.
                // But anyway, I guess it doesn't need to be super accurate and geometrically reproducible.
                
                myStackPosition=rotatePosition(myStackPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                myLinePosition=rotatePosition(myLinePosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                myStandbyPosition=rotatePosition(myStandbyPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                interceptionPositionOnMyLeft=rotatePosition(interceptionPositionOnMyLeft,ARENA_CENTER_OF_PHASE_2,bossRotation);
                
                // ----- Round 1 -----

                if(!tetherBeginsFromTheWest) { // Subject to change.
                    
                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForMelee(myIndex,myLinePosition,accessory);
                    
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
                    
                    drawPositionGuidanceForMelee(myIndex,interceptionPositionOnMyLeft,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForMelee(myIndex,myStackPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
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

                    drawObjectGuidanceForMelee(myIndex,carrierOnMyRight,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherLeavingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
                }

                else {
                    
                    drawPositionGuidanceForMelee(myIndex,interceptionPositionOnMyRight,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherCapturingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForMelee(myIndex,myStackPosition,accessory);
                    
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

                    drawPositionGuidanceForMelee(myIndex,myLinePosition,accessory);
                    
                }

                else {
                    
                    drawObjectGuidanceForMelee(myIndex,carrierOnMyLeft,accessory); // Subject to change.
                        
                    System.Threading.Thread.MemoryBarrier();

                    tetherLeavingSemaphore.WaitOne();
            
                    System.Threading.Thread.MemoryBarrier();
                        
                    accessory.Method.RemoveDraw(numberOfSteps.ToString());
                        
                    System.Threading.Thread.MemoryBarrier();

                    ++numberOfSteps;
                        
                    System.Threading.Thread.MemoryBarrier();
                    
                    drawPositionGuidanceForMelee(myIndex,myStandbyPosition,accessory);
                    
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
        
        [ScriptMethod(name:"本体 飓风之相 (子阶段2控制)",
            eventType:EventTypeEnum.StartCasting,
            eventCondition:["ActionId:42101"],
            userControl:false)]
    
        public void Phase_2_Twofold_Tempest_SubPhase_2_Control(Event @event,ScriptAccessory accessory) {

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
        
        [ScriptMethod(name:"本体 连击闪光炮",
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

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {

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

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {

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
        
        [ScriptMethod(name:"本体 第四次魔光 (预站位指路)",
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

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {
                
                if(!convertObjectId(phase2BossId, out var bossId)) {
            
                    return;
            
                }
                
                var bossObject=accessory.Data.Objects.SearchById(bossId);

                if(bossObject==null) {

                    return;

                }

                double bossRotation=(convertRotation(bossObject.Rotation)-Math.PI+2*Math.PI)%(2*Math.PI);

                Vector3 myPosition=ARENA_CENTER_OF_PHASE_2;

                if(isInGroup1(myIndex)) {

                    myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHWEST);

                }
                    
                if(isInGroup2(myIndex)) {

                    myPosition=getPlatformCenter(PlatformsOfPhase2.SOUTHEAST);

                }

                if(myPosition.Equals(ARENA_CENTER_OF_PHASE_2)) {

                    return;

                }
            
                var currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(2);
                currentProperties.Owner=accessory.Data.Me;
                currentProperties.TargetPosition=rotatePosition(myPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
                currentProperties.ScaleMode|=ScaleMode.YByDistance;
                currentProperties.Color=accessory.Data.DefaultSafeColor;
                currentProperties.DestoryAt=6000;
        
                accessory.Method.SendDraw(DrawModeEnum.Imgui,DrawTypeEnum.Displacement,currentProperties);
            
                currentProperties=accessory.Data.GetDefaultDrawProperties();

                currentProperties.Scale=new(8);
                currentProperties.Position=rotatePosition(myPosition,ARENA_CENTER_OF_PHASE_2,bossRotation);
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

            if(stratOfPhase2==StratsOfPhase2.MMW攻略组与苏帕酱噗) {
            
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