/*using Sandbox.Game.EntityComponents;
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
using VRageMath;*/

using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame;
//using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    partial class Program : MyGridProgram {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        const string PANEL_BATERIAS = "TxtBaterias";
        const string PANEL_POWER = "TxtPower";
        const string PANEL_CARGO = "TxtInventory 1";
        const string PANEL_CARGO_RAW = "TxtInventory 2";
        const string PANEL_PRODUCAO = "TxtProducao";
        const int MAX_ITENS =22;
        const float WHEN_SHUTDOWN_HYDRO = .7f;
        const float WHEN_SHUTDOWN_NUCLEAR = .9f;

        
        void Main() {


            //IMyTextPanel panelPower =  GridTerminalSystem.GetBlockWithName(PANEL_POWER);      
            UpdateBatteryPanel();
            UpdatePowerPanel();
            UpdateInventory();
            UpdateProducao();

        }
        List<IMyAssembler> assemblers = null;
        List<IMyRefinery> refineries = null;
        void UpdateProducao() {
          
            var panel = GridTerminalSystem.GetBlockWithName(PANEL_PRODUCAO) as IMyTextPanel;
            Echo("Painel Localizado");
            if (refineries == null) {
                refineries = new List<IMyRefinery>();
                GridTerminalSystem.GetBlocksOfType<IMyRefinery>(refineries);
            }
              
            Echo("Refineries Localizado: "+refineries);

            if (assemblers == null) {
                assemblers = new List<IMyAssembler>();
                GridTerminalSystem.GetBlocksOfType(assemblers, r => r.CubeGrid == Me.CubeGrid);
            }
            Echo("Assemblers Localizado");
            var x = 0;
            Dictionary<string, float> todosOres = new Dictionary<string, float>();
            Dictionary<string, int> todosOresRefinarias = new Dictionary<string, int>();
            foreach (var refinaria in refineries) {
                //List<VRage.Game.ModAPI.Ingame.MyInventoryItem> oresRefinaria = new List<MyInventoryItem>();
                Echo("Refinaria: " + refinaria.Name);
                var firstItem = refinaria.InputInventory.GetItemAt(0);// .GetItems(oresRefinaria);
                if (firstItem == null) {
                    continue;
                }
                x++;

                if (!todosOres.ContainsKey(firstItem?.Type.SubtypeId)) {
                    todosOres.Add(firstItem?.Type.SubtypeId, (float)firstItem?.Amount);
                    todosOresRefinarias.Add(firstItem?.Type.SubtypeId, 1);
                } else {
                    todosOres[firstItem?.Type.SubtypeId] += (float)firstItem?.Amount;
                    todosOresRefinarias[firstItem?.Type.SubtypeId] ++;
                }

            }
            string txtFinal = $"--- Refinarias ({x} / {refineries.Count}) ---\n";

            foreach (var ore in todosOres) {
                txtFinal += $"{ore.Value.ToString("N1").PadLeft(8, ' ')} {ore.Key} [{todosOresRefinarias[ore.Key]}]\n";
            }
            x = 0;
            Dictionary<string, float> todosComponentes = new Dictionary<string, float>();
            Dictionary<string, int> todosComponentesAssembler = new Dictionary<string, int>();
            foreach (var assembler in assemblers) {
                //List<VRage.Game.ModAPI.Ingame.MyInventoryItem> oresRefinaria = new List<MyInventoryItem>();
                Echo("Assembler: " + assembler.Name);
                var itensProd = new List<MyProductionItem>();
                assembler.GetQueue(itensProd) ;// .GetItems(oresRefinaria);
                if (!itensProd.Any()) {
                    continue;
                }
                var firstItem = itensProd.FirstOrDefault();
               
                x++;

                if (!todosComponentes.ContainsKey(firstItem.BlueprintId.SubtypeName)) {
                    todosComponentes.Add(firstItem.BlueprintId.SubtypeName, (float)firstItem.Amount);
                    todosComponentesAssembler.Add(firstItem.BlueprintId.SubtypeName, 1);
                } else {
                    todosComponentes[firstItem.BlueprintId.SubtypeName] += (float)firstItem.Amount;
                    todosComponentesAssembler[firstItem.BlueprintId.SubtypeName]++;
                }

            }
            //string txtFinal = $"--- Refinarias ({x} / {refineries.Count}) ---\n";


            txtFinal += $"\n\n--- Assemblers ({x} / {assemblers.Count}) ---\n";
            foreach (var ore in todosComponentes) {
                txtFinal += $"{ore.Value.ToString("N1").PadLeft(8, ' ')} {ore.Key} [{todosComponentesAssembler[ore.Key]}]\n";
            }
            panel.WriteText(txtFinal);
        }
        void UpdateInventory() {

            var dictItens = new Dictionary<string, int>();
            var dictRaw = new Dictionary<string, float>();
            var containers = new List<IMyInventoryOwner>();
            GridTerminalSystem.GetBlocksOfType<IMyInventoryOwner>(containers);
            var txtFinal = "";
            foreach (var c in containers) {
                for (var i = 0; i < c.InventoryCount; i++) {
                    var inv = c.GetInventory(i);
                    var items = new List<MyInventoryItem>();
                    inv.GetItems(items);
                    foreach (var item in items) {
                        var typeId = item.Type.TypeId.ToString();
                        var id = decodeItemName(item.Type.SubtypeId.ToString(), typeId);
                        if (typeId.EndsWith("_Ore") || typeId.EndsWith("_Ingot")) {
                            if (!dictRaw.ContainsKey(id)) {
                                dictRaw.Add(id, 0);
                            }
                            dictRaw[id] += (float)item.Amount;
                        } else {
                            if (!dictItens.ContainsKey(id)) {
                                dictItens.Add(id, 0);
                            }
                            dictItens[id] += item.Amount.ToIntSafe();
                        }
                    }
                }
            }


            txtFinal += "         --- COMPONENTS ---";
            var listItens = dictItens.Keys.OrderBy(n => n);
            foreach (var item in listItens) {
                txtFinal += "\n  " + dictItens[item].ToString().PadLeft(8, ' ') + " " + item;
            }


            var txtFinal2 = "            --- RAW ---";
            var listRaw = dictRaw.Keys.OrderBy(n => n);
            foreach (var item in listRaw) {
                txtFinal2 += "\n  " + dictRaw[item].ToString("N1").PadLeft(8, ' ') + " " + item;
            }



            var panel = GridTerminalSystem.GetBlockWithName(PANEL_CARGO) as IMyTextPanel;
            panel.WritePublicText(txtFinal);
            var panel2 = GridTerminalSystem.GetBlockWithName(PANEL_CARGO_RAW) as IMyTextPanel;
            panel2.WritePublicText(txtFinal2);




        }

        void UpdatePowerPanel() {
            var curTotal = 0f;
            var maxTotal = 0f;
            var panel1 = GridTerminalSystem.GetBlockWithName(PANEL_POWER + "1") as IMyTextPanel;
            var panel2 = GridTerminalSystem.GetBlockWithName(PANEL_POWER + "2") as IMyTextPanel;
            var txtFinal = "";
            var blocks = new List<IMySolarPanel>();
            GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(blocks);
            if (blocks.Count > 0) {
                blocks = blocks.OrderBy(b => b.CustomName).ToList();

                txtFinal += "--- Solar (" + blocks.Count + ") ---";

                for (int i = 0; i < blocks.Count; i++) {
                    var maxPow = getExtraFieldFloat(blocks[i], "Max Output: (\\d+\\.?\\d*) (\\w?)W");
                    var curPow = getExtraFieldFloat(blocks[i], "Current Output: (\\d+\\.?\\d*) (\\w?)W");
                    txtFinal += "\n" + blocks[i].CustomName + ": ";
                    txtFinal += String.Format("{0:N0}kW / {1:N0}kW", curPow / 1000, maxPow / 1000);

                    maxTotal += maxPow;
                    curTotal += curPow;
                }
            }
            var blocksWind = new List<IMyTerminalBlock>();

            GridTerminalSystem.SearchBlocksOfName("Wind Turbine", blocksWind);
            if (blocksWind.Count > 0) {
                blocksWind = blocksWind.OrderBy(b => b.CustomName).ToList();


                txtFinal += "\n--- Wind (" + blocksWind.Count + ") ---";

                for (int i = 0; i < blocksWind.Count; i++) {
                    var maxPow = getExtraFieldFloat(blocksWind[i], "Max Output: (\\d+\\.?\\d*) (\\w?)W");
                    var curPow = getExtraFieldFloat(blocksWind[i], "Current Output: (\\d+\\.?\\d*) (\\w?)W");
                    txtFinal += "\n" + blocksWind[i].CustomName + ": ";
                    txtFinal += String.Format("{0:N0}kW / {1:N0}kW", curPow / 1000, maxPow / 1000);
                    maxTotal += maxPow;
                    curTotal += curPow;
                }
            }
            var blocksHydro = new List<IMyTerminalBlock>();
                GridTerminalSystem.SearchBlocksOfName("HydrogenEngine", blocksHydro);
            if (blocksHydro.Count > 0) {
                blocksHydro = blocksHydro.OrderBy(b => b.CustomName).ToList();


                txtFinal += "\n--- Hydro (" + blocksHydro.Count + ") ---";

                for (int i = 0; i < blocksHydro.Count; i++) {
                    var maxPow = getExtraFieldFloat(blocksHydro[i], "Max Output: (\\d+\\.?\\d*) (\\w?)W");
                    var curPow = getExtraFieldFloat(blocksHydro[i], "Current Output: (\\d+\\.?\\d*) (\\w?)W");
                    var fill = getExtraField(blocksHydro[i], "Filled: (\\d+\\.?\\d*) (\\w?)W");
                    txtFinal += "\n" + blocksHydro[i].CustomName + ": ";
                    txtFinal += String.Format("{0:N0}kW / {1:N0}kW ({2})", curPow / 1000, maxPow / 1000, fill);
                    maxTotal += maxPow;
                    curTotal += curPow;
                }
            }
            var blocksNuclear = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName("Reactor", blocksNuclear);
            if (blocksNuclear.Count > 0) {
                blocksNuclear = blocksNuclear.OrderBy(b => b.CustomName).ToList();


                txtFinal += "\n--- Nuclear (" + blocksNuclear.Count + ") ---";

                for (int i = 0; i < blocksNuclear.Count; i++) {
                    var maxPow = getExtraFieldFloat(blocksNuclear[i], "Max Output: (\\d+\\.?\\d*) (\\w?)W");
                    var curPow = getExtraFieldFloat(blocksNuclear[i], "Current Output: (\\d+\\.?\\d*) (\\w?)W");
                    txtFinal += "\n" + blocksNuclear[i].CustomName + ": ";
                    txtFinal += String.Format("{0:N0}kW / {1:N0}kW", curPow / 1000, maxPow / 1000);
                    maxTotal += maxPow;
                    curTotal += curPow;
                }
            }



            txtFinal += String.Format("\n--- TOTAL: {0:N0}kW / {1:N0}kW ---", curTotal / 1000, maxTotal / 1000);

			var linhas = txtFinal.Split('\n');
			panel1.WriteText(string.Join("\n", linhas.Take(MAX_ITENS)));
			if (linhas.Length > MAX_ITENS) {
				panel2.WriteText(string.Join("\n", linhas.Skip(MAX_ITENS)));
			}
			//panel1.WriteText(txtFinal);
            

        }
        void UpdateBatteryPanel() {
            var totalCur = 0f;
            var totalMax = 0f;
            var panelBaterias = GridTerminalSystem.GetBlockWithName(PANEL_BATERIAS + "1") as IMyTextPanel;
            var panelBaterias2 = GridTerminalSystem.GetBlockWithName(PANEL_BATERIAS + "2") as IMyTextPanel;
            var blocks = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(blocks);
            blocks = blocks.OrderBy(b => b.CustomName).ToList();
            var txtFinal = "Baterias (" + blocks.Count + "):";

            for (int i = 0; i < blocks.Count; i++) {
                var maxPow = getExtraFieldFloat(blocks[i], "Max Stored Power: (\\d+\\.?\\d*) (\\w?)Wh");
                var curPow = getExtraFieldFloat(blocks[i], "Max Stored Power:.*Stored power: (\\d+\\.?\\d*) (\\w?)Wh");
                var curInput = getExtraFieldFloat(blocks[i], "Current Input: (\\d+\\.?\\d*) (\\w?)W");
                var curOutput = getExtraFieldFloat(blocks[i], "Current Output: (\\d+\\.?\\d*) (\\w?)W");
                var rechargeTime = getExtraFieldFloat(blocks[i], "Fully recharged in: (\\d+\\.?\\d*) (\\w?)min");
                var depleteTime = getExtraFieldFloat(blocks[i], "Fully depleted in: (\\d+\\.?\\d*) (\\w?)min");


                var rechargeTxt = curInput > curOutput ? String.Format(" ({0:N0}min)", rechargeTime) : "";
                var depleteTxt = curInput < curOutput ? String.Format(" ({0:N0}min)", depleteTime) : "";
                totalCur += curPow;
                totalMax += maxPow;

                txtFinal += "\n" + (i + 1).ToString().PadLeft(2, ' ') + ": " + String.Format("{0:N0}%", (curPow / maxPow) * 100).PadLeft(5, ' ') + String.Format(" {0:N1}MW", (curInput - curOutput) / 1000000).PadLeft(4, ' ');
                txtFinal += "  " + blocks[i].CustomName + rechargeTxt + depleteTxt;
                if (i == MAX_ITENS) {
                    panelBaterias.WritePublicText(txtFinal);
                    txtFinal = "--- Cont ---";
                }

            }
            //var engine1 = GridTerminalSystem.GetBlockWithName("HydrogenEngine1");
            //var engine2 = GridTerminalSystem.GetBlockWithName("SmallReactor1");
            //if (totalCur / totalMax > WHEN_SHUTDOWN_HYDRO) {

            //    engine1.ApplyAction("OnOff_Off");
            //} else {
            //    engine1.ApplyAction("OnOff_On");
            //}
            //if (totalCur / totalMax > WHEN_SHUTDOWN_NUCLEAR) {
            //    engine2.ApplyAction("OnOff_Off");
            //} else {

            //    engine2.ApplyAction("OnOff_On");
            //}

            txtFinal += "\n--- Total: " + String.Format("{0:N1} / {1:N1}MW ({2:P0})", totalCur / 1000000, totalMax / 1000000, totalCur / totalMax);

            panelBaterias2.WritePublicText(txtFinal);
        }


        string getExtraField(IMyTerminalBlock block, string regexString) {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(regexString, System.Text.RegularExpressions.RegexOptions.Singleline);
            string result = "";
            System.Text.RegularExpressions.Match match = regex.Match(block.DetailedInfo);
            if (match.Success) {
                result = match.Groups[1].Value;
            }
            return result;
        }

        const string MULTIPLIERS = ".kMGTPEZY";

        float getExtraFieldFloat(IMyTerminalBlock block, string regexString) {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(regexString, System.Text.RegularExpressions.RegexOptions.Singleline);
            float result = 0.0f;
            double parsedDouble;
            System.Text.RegularExpressions.Match match = regex.Match(block.DetailedInfo);
            if (match.Success) {
                if (Double.TryParse(match.Groups[1].Value, out parsedDouble)) {
                    result = (float)parsedDouble;
                }
                if (MULTIPLIERS.IndexOf(match.Groups[2].Value) > -1) {
                    result = result * (float)Math.Pow(1000.0, MULTIPLIERS.IndexOf(match.Groups[2].Value));
                }
            }
            return result;
        }

        String decodeItemName(String name, String typeId) {
            if (name.Equals("Construction")) { return "Construction Component"; }
            if (name.Equals("MetalGrid")) { return "Metal Grid"; }
            if (name.Equals("InteriorPlate")) { return "Interior Plate"; }
            if (name.Equals("SteelPlate")) { return "Steel Plate"; }
            if (name.Equals("SmallTube")) { return "Small Steel Tube"; }
            if (name.Equals("LargeTube")) { return "Large Steel Tube"; }
            if (name.Equals("BulletproofGlass")) { return "Bulletproof Glass"; }
            if (name.Equals("Reactor")) { return "Reactor Component"; }
            if (name.Equals("Thrust")) { return "Thruster Component"; }
            if (name.Equals("GravityGenerator")) { return "GravGen Component"; }
            if (name.Equals("Medical")) { return "Medical Component"; }
            if (name.Equals("RadioCommunication")) { return "Radio Component"; }
            if (name.Equals("Detector")) { return "Detector Component"; }
            if (name.Equals("SolarCell")) { return "Solar Cell"; }
            if (name.Equals("PowerCell")) { return "Power Cell"; }
            if (name.Equals("AutomaticRifleItem")) { return "Rifle"; }
            if (name.Equals("AutomaticRocketLauncher")) { return "Rocket Launcher"; }
            if (name.Equals("WelderItem")) { return "Welder"; }
            if (name.Equals("AngleGrinderItem")) { return "Grinder"; }
            if (name.Equals("HandDrillItem")) { return "Hand Drill"; }
            if (typeId.EndsWith("_Ore")) {
                if (name.Equals("Stone")) {
                    return name;
                }
                return name + " Ore";
            }
            if (typeId.EndsWith("_Ingot")) {
                if (name.Equals("Stone")) {
                    return "Gravel";
                }
                if (name.Equals("Magnesium")) {
                    return name + " Powder";
                }
                if (name.Equals("Silicon")) {
                    return name + " Wafer";
                }
                return name + " Ingot";
            }
            return name;
        }

        String amountFormatter(float amt, String typeId) {
            if (typeId.EndsWith("_Ore") || typeId.EndsWith("_Ingot")) {
                if (amt > 1000.0f) {
                    return "" + Math.Round((float)amt / 1000, 2) + "K";
                } else {
                    return "" + Math.Round((float)amt, 2);
                }
            }
            return "" + Math.Round((float)amt, 0);
        }

    }
}
