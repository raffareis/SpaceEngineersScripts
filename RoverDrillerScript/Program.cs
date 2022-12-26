using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {


        IMyTextSurface PANEL_BATTERY = null;
        IMyTextSurface PANEL_CARGO = null;

        IMyTextSurface PANEL_FL = null;
        IMyTextSurface PANEL_FR = null;
        private List<IMyBatteryBlock> batteryBlocks;
        private List<IMyTerminalBlock> blocksThrust;
        private List<IMyTerminalBlock> containers;
        IMyCockpit myCockpit;


        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            var lst = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(lst, filterThis);
            myCockpit = lst.FirstOrDefault();
            if (myCockpit == null) {
                Echo("Cockpit não encontrado");
                return;
            }
            PANEL_CARGO = myCockpit.GetSurface(0);
            PANEL_BATTERY = myCockpit.GetSurface(3);
            PANEL_FL = myCockpit.GetSurface(1);
            PANEL_FR = myCockpit.GetSurface(2);

            blocksThrust = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(blocksThrust, filterThis);

            containers = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(containers, i => filterThis(i) && i.HasInventory);
            batteryBlocks = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteryBlocks, filterThis);
            
            
            //var filterStone = new List<MyInventoryItemFilter>();
            //filterStone.Add(new MyInventoryItemFilter("MyObjectBuilder_Ore/Stone"));
            //var sorters = new List<IMyConveyorSorter>() ;

            //GridTerminalSystem.GetBlocksOfType<IMyConveyorSorter>(sorters, filterThis);
            //sorters.ForEach(r => {
            //    var currentFilters = new List<MyInventoryItemFilter>();

            //    r.GetFilterList(currentFilters);
            //    //currentFilters.ForEach(f => {
            //    //    Echo($"ItemID: {f.ItemId.ToString()}");
            //    //    Echo($"SubTypeName: {f.ItemId.SubtypeName}");
            //    //});
            //    r.DrainAll = true;
            //    r.SetFilter(MyConveyorSorterMode.Whitelist, filterStone);
            //});
        }
        long ticks = 0;
        void Main(string argument, UpdateType updateSource) {
            ticks++;
            if (ticks % 5 == 0)
                UpdateInventory();

            if (ticks % 10 == 0) {
                UpdateInventoryTop();
            }
            if (ticks % 100 == 0) {
                UpdateStructural();
                UpdateBatteryPanel();

            }
        }
        private void UpdateStructural() {
            var falt = GetIncompleteBlocks();
            var txt = "BLOCOS DANIFICADOS: \n";
            foreach(var f in falt) {
                txt += f.CustomName + "\n";
            }
            PANEL_FR.WriteText(txt);

        }
        private IEnumerable<IMyTerminalBlock> GetIncompleteBlocks(bool markOnHud = true) {
            var lst = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(lst);
            lst = lst.Where(filterThis).ToList();
            var minVec = Me.CubeGrid.Min;
            var maxVec = Me.CubeGrid.Max;
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();

            for (var i = minVec.X; i <= maxVec.X; i++)
                for (var j = minVec.Y; j <= maxVec.Y; j++)
                    for (var k = minVec.Y; k <= maxVec.Y; k++) {
                        var vec = new Vector3I(i, j, k);
                        if (Me.CubeGrid.CubeExists(vec)) {
                            var cubo = Me.CubeGrid.GetCubeBlock(vec);

                            if (cubo != null)
                                blocks.Add(cubo);
                        }
                    }

            foreach (var cubo in blocks) {
                if (cubo.HasDeformation || !cubo.IsFullIntegrity || cubo.BuildLevelRatio < 1 || cubo.CurrentDamage > 0 || cubo.BuildIntegrity < 1 || cubo.IsDestroyed) {
                    //GPS:[Asteroid] Ag4: -241699.94:-123577.88:-154025.73:#FFF18975:
                    var posicaoMundo = Me.CubeGrid.GridIntegerToWorld(cubo.Position);
                    Echo($"Cubo deformado: x:{posicaoMundo.X} y:{posicaoMundo.Y} z:{posicaoMundo.Z}");                    
                }
            }
            lst.Where(i=>i.ShowOnHUD && i.IsFunctional).ToList().ForEach(i => i.ShowOnHUD = false);
            var faulty = lst.Where(i => !i.IsFunctional).ToList();
            if (markOnHud)
                faulty.ForEach(i => i.ShowOnHUD = true);
            return faulty;
        }
        void UpdateInventoryTop() {
            //var listItens = CurrentItems.Keys.OrderBy(n => n);
            //var txtFinal = "";
            //foreach (var item in listItens) {
            //    txtFinal += "\n " + CurrentItems[item].ToString().PadLeft(5, ' ') + " " + item;
            //}
            //txtFinal += String.Format("\n\nTOTAL: {0:N2}/{1:N2} ({2:P0})", CurrentVolume, MaxVolume, CurrentVolume / MaxVolume);

            //var panel = GridTerminalSystem.GetBlockWithName(PANEL_CARGO) as IMyTextPanel;
            //var lst = new List<IMyTerminalBlock>();
            //GridTerminalSystem.GetBlocksOfType<IMyInventoryOwner>(lst, filterThis);
            var txtFinal = " --- CARGO SPACE ---\n";
            long currentTotal = 0;
            long maxTotal = 0;
            var idx = 1;
            foreach (var i in containers.OrderByDescending(i => i.GetInventory(0).MaxVolume.RawValue)) {
                if (i.GetInventory(0).MaxVolume.RawValue > 4000000) {
                    currentTotal += i.GetInventory(0).CurrentVolume.RawValue;
                    maxTotal += i.GetInventory(0).MaxVolume.RawValue;

                    txtFinal += $"{idx++.ToString().PadLeft(2)} {MontaIndicator(0, 1000, 18, Convert.ToInt32(i.GetInventory(0).CurrentVolume.RawValue * 1000 / i.GetInventory(0).MaxVolume.RawValue))} {Convert.ToInt32(i.GetInventory(0).CurrentVolume.RawValue / 1000000m)}/{Convert.ToInt32(i.GetInventory(0).MaxVolume.RawValue / 1000000m)}\n";



                }
            }

            txtFinal += $"\n\n-- Total: {currentTotal / 1000000m:N1} / {maxTotal / 1000000m:N1} ({(Convert.ToDouble(currentTotal) * 100 / maxTotal):N0}%)\n{MontaIndicator(0, 1000, 28, Convert.ToInt32(currentTotal * 1000 / maxTotal))}";

            PANEL_FL.WriteText(txtFinal);


        }
        Dictionary<string,int> lastDict = new Dictionary<string, int>() ;
        void UpdateInventory() {
            var totalMaxVolume = 0f;
            var totalCurVolume = 0f;
            var dictItens = new Dictionary<string, int>();
            //var dictRaw = new Dictionary<string, int>();
            //var dictContainer = new Dictionary<string, float>();            
            var txtFinal = "";

            foreach (var c in containers.OrderByDescending(c => c.GetInventory(0).MaxVolume.RawValue)) {


                var thisCur = 0f;
                var thisMax = 0f;
                for (var i = 0; i < c.InventoryCount; i++) {
                    var inv = c.GetInventory(i);

                    thisMax += (float)inv.MaxVolume;
                    thisCur += (float)inv.CurrentVolume;

                    totalMaxVolume += (float)inv.MaxVolume;
                    totalCurVolume += (float)inv.CurrentVolume;

                    var items = new List<MyInventoryItem>();
                    inv.GetItems(items);
                    foreach (var item in items) {
                        var typeId = item.Type.TypeId.ToString();
                        var id = decodeItemName(item.Type.SubtypeId.ToString(), typeId);

                        if (!dictItens.ContainsKey(id)) {
                            dictItens.Add(id, 0);
                        }
                        dictItens[id] += item.Amount.ToIntSafe();
                    }
                }
                //if (c is IMyCargoContainer)
                //    dictContainer.Add(c.CustomName, thisCur / thisMax);

            }

            

            var listItens = dictItens.Keys.OrderBy(n => n);
            foreach (var item in listItens) {
                var nomeItem = item;
                var valorAtual = dictItens[item];
                var valorAnterior = lastDict.ContainsKey(item) ? lastDict[item] : 0;
                var delta = valorAtual - valorAnterior;


                txtFinal += "\n  " + amountFormatter(dictItens[item]).PadLeft(6, ' ') + " " + item.PadRight(6,' ') + (delta!=0 ? $" ({delta})" : "");
            }
            lastDict = new Dictionary<string, int>(dictItens);
            //txtFinal += String.Format("\n\nTOT: {0:N2}/{1:N2} ({2:P0})", totalCurVolume, totalMaxVolume, totalCurVolume / totalMaxVolume);
            //foreach (var t in dictContainer) {
            //    txtFinal += String.Format("\n {0:P0}", t.Value).PadLeft(5, ' ') + " " + t.Key;


            //}

            PANEL_CARGO.WriteText(txtFinal);
        }
        //void UpdateBatteryPanel() {
        //    var totalCur = 0f;
        //    var totalMax = 0f;


        //    var blocks = new List<IMyBatteryBlock>();
        //    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(blocks, filterThis);
        //    blocks = blocks.OrderBy(b => b.CustomName).ToList();
        //    var txtFinal = "Baterias (" + blocks.Count + "):";

        //    for (int i = 0; i < blocks.Count; i++) {
        //        var maxPow = getExtraFieldFloat(blocks[i], "Max Stored Power: (\\d+\\.?\\d*) (\\w?)Wh");
        //        var curPow = getExtraFieldFloat(blocks[i], "Max Stored Power:.*Stored power: (\\d+\\.?\\d*) (\\w?)Wh");
        //        var curInput = getExtraFieldFloat(blocks[i], "Current Input: (\\d+\\.?\\d*) (\\w?)W");
        //        var curOutput = getExtraFieldFloat(blocks[i], "Current Output: (\\d+\\.?\\d*) (\\w?)W");
        //        var rechargeTime = getExtraFieldFloat(blocks[i], "Fully recharged in: (\\d+\\.?\\d*) (\\w?)min");
        //        var depleteTime = getExtraFieldFloat(blocks[i], "Fully depleted in: (\\d+\\.?\\d*) (\\w?)min");

        //        var rechargeTxt = curInput > curOutput ? String.Format(" ({0:N0}min)", rechargeTime) : "";
        //        var depleteTxt = curInput < curOutput ? String.Format(" ({0:N0}min)", depleteTime) : "";
        //        totalCur += curPow;
        //        totalMax += maxPow;

        //        txtFinal += "\n" + (i + 1) + ": " + String.Format("{0:N0}%", curPow * 100 / maxPow).PadLeft(4, ' ') + String.Format(" {0:N1}MW", (curInput - curOutput) / 1000000).PadLeft(4, ' ');
        //        txtFinal += " " + rechargeTxt + depleteTxt;

        //    }

        //    txtFinal += "\n-- Total: " + String.Format("{0:N1}/{1:N1} ({2:P0})", totalCur / 1000000, totalMax / 1000000, totalCur / totalMax);

        //    PANEL_FL.WriteText(txtFinal);
        //}
        void UpdateBatteryPanel() {
            var totalCur = 0f;
            var totalMax = 0f;



            batteryBlocks = batteryBlocks.Where(b => !b.CustomName.Contains("Small")).OrderBy(b => b.CustomName).ToList();
            var txtFinal = "Baterias (" + batteryBlocks.Count + "):";

            for (int i = 0; i < batteryBlocks.Count; i++) {
                var maxPow = getExtraFieldFloat(batteryBlocks[i], "Max Stored Power: (\\d+\\.?\\d*) (\\w?)Wh");
                var curPow = getExtraFieldFloat(batteryBlocks[i], "Max Stored Power:.*Stored power: (\\d+\\.?\\d*) (\\w?)Wh");
                var curInput = getExtraFieldFloat(batteryBlocks[i], "Current Input: (\\d+\\.?\\d*) (\\w?)W");
                var curOutput = getExtraFieldFloat(batteryBlocks[i], "Current Output: (\\d+\\.?\\d*) (\\w?)W");
                var rechargeTime = getExtraFieldFloat(batteryBlocks[i], "Fully recharged in: (\\d+\\.?\\d*) (\\w?)min");
                var depleteTime = getExtraFieldFloat(batteryBlocks[i], "Fully depleted in: (\\d+\\.?\\d*) (\\w?)min");

                var rechargeTxt = curInput > curOutput ? String.Format(" ({0:N1}min)", rechargeTime) : "";
                var depleteTxt = curInput < curOutput ? String.Format(" ({0:N1}min)", depleteTime) : "";
                totalCur += curPow;
                totalMax += maxPow;

                txtFinal += "\n" + (i + 1) + ": " + String.Format("{0:N0}%", curPow * 100 / maxPow).PadLeft(4, ' ') + String.Format("{0}W", amountFormatter((curInput - curOutput) / 1000000)).PadLeft(8, ' ');
                txtFinal += "  " /*+ blocks[i].CustomName*/ + rechargeTxt + depleteTxt;

            }

            txtFinal += "\n-- Total: " + String.Format("{0:N1}/{1:N1} ({2:P0})", totalCur / 1000000, totalMax / 1000000, totalCur / totalMax) + "\n" + MontaIndicator(0, 100, 25, Convert.ToInt32(totalCur * 100 / totalMax));

            PANEL_BATTERY.WriteText(txtFinal);
        }


    }
}
