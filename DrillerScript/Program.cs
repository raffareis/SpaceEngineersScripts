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
            if(myCockpit == null) {
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
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(containers, i=> filterThis(i) && i.HasInventory); 
            batteryBlocks = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteryBlocks, filterThis);
        }
        long ticks = 0;
        void Main(string argument, UpdateType updateSource) {


			ticks++;
            
            UpdateThrusters();
            if (ticks % 10 == 0) {
                UpdateInventory();
                UpdateInventoryTop();
                UpdateBatteryPanel();
            }
        }
        void UpdateThrusters() {
           

			var thrusts = blocksThrust.Cast<IMyThrust>();
			var tTotal = thrusts.Count();
			var tWorking = thrusts.Count(t => t.IsWorking && t.IsFunctional);

			var textoThrust = $"Thr: {tWorking} / {tTotal}\n";

			var grp = thrusts.GroupBy(g => Vector3I.GetDominantDirection(g.GridThrustDirection));

			var L = grp.Where(g => g.Key == CubeFace.Left).Select(g => g.Sum(a => a.CurrentThrust) / g.Sum(a => a.MaxThrust)).SingleOrDefault();
			var R = grp.Where(g => g.Key == CubeFace.Right).Select(g => g.Sum(a => a.CurrentThrust) / g.Sum(a => a.MaxThrust)).SingleOrDefault();
			var U = grp.Where(g => g.Key == CubeFace.Up).Select(g => g.Sum(a => a.CurrentThrust) / g.Sum(a => a.MaxThrust)).SingleOrDefault();
			var D = grp.Where(g => g.Key == CubeFace.Down).Select(g => g.Sum(a => a.CurrentThrust) / g.Sum(a => a.MaxThrust)).SingleOrDefault();
			var F = grp.Where(g => g.Key == CubeFace.Forward).Select(g => g.Sum(a => a.CurrentThrust) / g.Sum(a => a.MaxThrust)).SingleOrDefault();
			var B = grp.Where(g => g.Key == CubeFace.Backward).Select(g => g.Sum(a => a.CurrentThrust) / g.Sum(a => a.MaxThrust)).SingleOrDefault();

			var LR = L - R;
			var BF = B - F;
			var DU = D - U;

			var maxWidthIndicator = 30;

			textoThrust += $"\nLR:{LR.ToString("P0").PadLeft(6)}";
			textoThrust += $"\n{MontaIndicator(-100, 100, maxWidthIndicator, (int)(LR * 100))}";
			textoThrust += $"\n{MontaIndicator(-100, 100, maxWidthIndicator, (int)(LR * 100))}";
			textoThrust += $"\n\nBF:{BF.ToString("P0").PadLeft(6)}";

			textoThrust += $"\n{MontaIndicator(-100, 100, maxWidthIndicator, (int)(BF * 100))}";
			textoThrust += $"\n{MontaIndicator(-100, 100, maxWidthIndicator, (int)(BF * 100))}";

			textoThrust += $"\n\nDU:{DU.ToString("P0").PadLeft(6)}";
			textoThrust += $"\n{MontaIndicator(-100, 100, maxWidthIndicator, (int)(DU * 100))}";
			textoThrust += $"\n{MontaIndicator(-100, 100, maxWidthIndicator, (int)(DU * 100))}";

			//foreach(var g in grp.OrderBy(a=>a.Key)) {
			//    var curT = g.Sum(a => a.CurrentThrust);
			//    var maxT = g.Sum(a => a.MaxThrust);
			//    var dir = g.Key;
			//    textoThrust += $"\n{dir}: {(curT / maxT):P1}";
			//}

			PANEL_FR.WriteText(textoThrust);
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
            foreach(var i in containers.OrderByDescending(i=>i.GetInventory(0).MaxVolume.RawValue)) {                
                if (i.GetInventory(0).MaxVolume.RawValue > 4000000) {
                    currentTotal += i.GetInventory(0).CurrentVolume.RawValue;
                    maxTotal += i.GetInventory(0).MaxVolume.RawValue;

                    txtFinal += $"{idx++.ToString().PadLeft(2)} {MontaIndicator(0,1000,18,Convert.ToInt32(i.GetInventory(0).CurrentVolume.RawValue * 1000 / i.GetInventory(0).MaxVolume.RawValue))} {Convert.ToInt32(i.GetInventory(0).CurrentVolume.RawValue / 1000000m)}/{Convert.ToInt32(i.GetInventory(0).MaxVolume.RawValue / 1000000m)}\n";



                }
            }
            
            txtFinal += $"\n\n-- Total: {currentTotal / 1000000m:N1} / {maxTotal / 1000000m:N1} ({(Convert.ToDouble(currentTotal) * 100 / maxTotal):N0}%)\n{MontaIndicator(0,1000,28,Convert.ToInt32(currentTotal*1000 / maxTotal))}";

            PANEL_FL.WriteText(txtFinal);


        }
        void UpdateInventory() {
            var totalMaxVolume = 0f;
            var totalCurVolume = 0f;
            var dictItens = new Dictionary<string, int>();
            //var dictRaw = new Dictionary<string, int>();
            //var dictContainer = new Dictionary<string, float>();            
            var txtFinal = "";
           
            foreach (var c in  containers.OrderByDescending(c => c.GetInventory(0).MaxVolume.RawValue )) {
               

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
                txtFinal += "\n  " + amountFormatter(dictItens[item]).PadLeft(6, ' ') + " " + item;
            }
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
           

           
            batteryBlocks = batteryBlocks.Where(b=>!b.CustomName.Contains("Small")).OrderBy(b => b.CustomName).ToList();
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

            txtFinal += "\n-- Total: " + String.Format("{0:N1}/{1:N1} ({2:P0})", totalCur / 1000000, totalMax / 1000000, totalCur / totalMax)+"\n"+MontaIndicator(0,100,25,Convert.ToInt32(totalCur*100/totalMax));

            PANEL_BATTERY.WriteText(txtFinal);
        }

      
    }
}
