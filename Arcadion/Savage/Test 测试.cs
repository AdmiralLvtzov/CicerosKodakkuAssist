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

namespace CicerosKodakkuAssist.Test
{

    [ScriptType(name:"Test 测试",
        territorys:[177],
        guid:"b05c80f8-7656-4a60-ae66-5f1cc91e8b81",
        version:"0.0.0.1",
        note:"Test 测试",
        author:"Cicero 灵视")]

    public class Test
    {
        
        [ScriptMethod(name:"Test 测试",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:24376"])]
    
        public void Test_测试(Event @event,ScriptAccessory accessory) {
            
            accessory.Method.SendChat($"""

                                                     北
                                       MTH1         STH2
                                       D1D3         D2D4
                                       v5
                                         MT  大圈1   ST
                                         D1  大圈2   D2
                                       """);
            
            System.Threading.Thread.Sleep(1500);
            
            accessory.Method.SendChat($"""

                                                         北
                                       H1    MT         D2    D4
                                                      Boss
                                       D3    D1          ST     H2
                                       
                                       """);
            
            System.Threading.Thread.Sleep(1500);
            
            accessory.Method.SendChat($"""

                                                MT
                                           D3     D4
                                       H1             H2
                                           D1     D2
                                                ST
                                       """);
            
        }
        
    }

}