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
        const string COCKPIT = "[Driller] Cockpit";
        bool filterThis(IMyTerminalBlock block) {
            return block.CubeGrid == Me.CubeGrid;
        }


        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        void Main() {


            UpdateInventory();

        }

        void UpdateInventory() {
            var totalMaxVolume = 0f;
            var totalCurVolume = 0f;
            var dictItens = new Dictionary<string, int>();
            var dictRaw = new Dictionary<string, int>();
            var dictContainer = new Dictionary<string, float>();
            var containers = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(containers, filterThis);
            var txtFinal = "";
            containers = containers.OrderBy(c => c.CustomName).ToList();
            foreach (var c in containers) {
                if (c is IMyInventoryOwner) {

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
                    if (c is IMyCargoContainer)
                        dictContainer.Add(c.CustomName, thisCur / thisMax);
                }
            }



            var listItens = dictItens.Keys.OrderBy(n => n);
            foreach (var item in listItens) {
                txtFinal += "\n  " + dictItens[item].ToString().PadRight(7, ' ') + " " + item;
            }
            txtFinal += String.Format("\n\nTOT: {0:N2}/{1:N2} ({2:P0})", totalCurVolume, totalMaxVolume, totalCurVolume / totalMaxVolume);
            foreach (var t in dictContainer) {
                txtFinal += String.Format("\n {0:P0}", t.Value).PadLeft(5, ' ') + " " + t.Key;


            }
            var cp = GridTerminalSystem.GetBlockWithName(COCKPIT) as IMyCockpit;
            var panel = cp.GetSurface(0);
            panel.WriteText(txtFinal);
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
    }
}
