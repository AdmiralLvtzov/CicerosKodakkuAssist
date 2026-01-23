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
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
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
        
        private bool enableDebugLogging=true;
        
        [ScriptMethod(name:"Test 测试",
            eventType:EventTypeEnum.ActionEffect,
            eventCondition:["ActionId:24376"])]
    
        public void Test_测试(Event @event,ScriptAccessory accessory) {
            
            SendLindschratInformation(accessory,[0,2,4,6,1,3,5,7],[0,4],[1,5]);
            
            System.Threading.Thread.Sleep(1500);
            
            SendTowerInformation(accessory,[0,1,2,3,4,5,6,7]);
            
            System.Threading.Thread.Sleep(1500);
            
            SendTetherInformation(accessory,[0,1,2,3,4,5,6,7]);
            
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
        
        public static bool isLegalPartyIndex(int partyIndex) {

            return (0<=partyIndex&&partyIndex<=7);

        }
        
    }

}