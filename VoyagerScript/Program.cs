using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame;

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

		//string COCKPIT = "";

		private IMyTextSurface PANEL_BATTERY = null;
		private IMyTextSurface PANEL_CARGO = null;

		private IMyTextSurface PANEL_FL = null;
		private IMyTextSurface PANEL_FR = null;

		private IMyCockpit myCockpit;

		private bool filterThis(IMyTerminalBlock block) {
			return block.CubeGrid == Me.CubeGrid;
		}

		public Program() {
			Runtime.UpdateFrequency = UpdateFrequency.Update100;
		}

		private void Main(string argument, UpdateType updateSource) {
			var lst = new List<IMyCockpit>();
			GridTerminalSystem.GetBlocksOfType<IMyCockpit>(lst, filterThis);
			myCockpit = lst.FirstOrDefault();
			if (myCockpit == null) {
				Echo("Cockpit não encontrado");
				return;
			}

			PANEL_CARGO = myCockpit.GetSurface(1);
			PANEL_BATTERY = myCockpit.GetSurface(3);
			PANEL_FL = myCockpit.GetSurface(0);
			PANEL_FR = myCockpit.GetSurface(2);

			UpdateInventory();
			UpdateBatteryPanel();
			UpdateHydrogen();
		}

		// Define the names you want the script to look for. This should be all you need to change in the script.
		

		private string nameHydrogenTanks = "Hydrogen Tank";

		private void UpdateHydrogen() {
			// Find the text panel to display to.
			IMyTextSurface displayPanel = PANEL_FL;

			// Find the hydrogen tanks with the specified name.
			List<IMyTerminalBlock> listHydrogenTanks = new List<IMyTerminalBlock>();
			GridTerminalSystem.SearchBlocksOfName(nameHydrogenTanks, listHydrogenTanks,filterThis);

			// Initialize variables for measuring our hydrogen amounts.
			double storedHydrogen = 0;
			double percentHydrogen = 0;

			// For each hydrogen tank matching the specified name, find its fill % and add it to the total %.
			foreach (var tank in listHydrogenTanks) {
				string[] infoTank = tank.DetailedInfo.Split(':');
				int percentIndex = infoTank[3].IndexOf("%");
				storedHydrogen += Double.Parse(infoTank[3].Substring(0, percentIndex));
			}

			// Divide the total percentage by the number of tanks found.
			percentHydrogen = Math.Round(storedHydrogen / listHydrogenTanks.Count, 2);

			// Define how long the fuel display bar is based on fill percent.
			int indicatorlength = 50;
			int hydrogenindicator = (int)((percentHydrogen / 100) * indicatorlength);

			// Put together the actual text that will be displayed on the screen.
			string displayText =

			"______________________________________________" + "\n" +
			"                        FUEL STATUS" + "\n" +
			"¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯¯" + "\n" +
			"          [" + new string('!', hydrogenindicator) + new string('∙', indicatorlength - hydrogenindicator) + "]" + "\n" +
			"                  FUEL TANKS (" + percentHydrogen.ToString() + "%)" + "\n" +
			"";

			// Write the text to the screen.
			displayPanel.WriteText(displayText);
		}

		private void UpdateInventory() {
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
			txtFinal += String.Format("\n\nTOT: {0:N1}/{1:N1} ({2:N0}%)", totalCurVolume, totalMaxVolume, totalCurVolume *100 / totalMaxVolume);
			foreach (var t in dictContainer) {
				txtFinal += String.Format("\n {0:P0}", t.Value).PadLeft(5, ' ') + " " + t.Key;
			}

			PANEL_CARGO.WriteText(txtFinal);
		}

		private void UpdateBatteryPanel() {
			var totalCur = 0f;
			var totalMax = 0f;

			var blocks = new List<IMyBatteryBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(blocks, filterThis);
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

				txtFinal += "\n" + (i + 1) + ": " + String.Format("{0:N0}%", curPow * 100 / maxPow).PadLeft(4, ' ') + String.Format(" {0:N1}MW", (curInput - curOutput) / 1000000).PadLeft(4, ' ');
				txtFinal += " " + rechargeTxt + depleteTxt;
			}

			txtFinal += "\n-- Total: " + String.Format("{0:N1}/{1:N1} ({2:P0})", totalCur / 1000000, totalMax / 1000000, totalCur / totalMax);

			PANEL_BATTERY.WriteText(txtFinal);
		}

		private string getExtraField(IMyTerminalBlock block, string regexString) {
			System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(regexString, System.Text.RegularExpressions.RegexOptions.Singleline);
			string result = "";
			System.Text.RegularExpressions.Match match = regex.Match(block.DetailedInfo);
			if (match.Success) {
				result = match.Groups[1].Value;
			}
			return result;
		}

		private const string MULTIPLIERS = ".kMGTPEZY";

		private float getExtraFieldFloat(IMyTerminalBlock block, string regexString) {
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

		private String decodeItemName(String name, String typeId) {
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

		private String amountFormatter(float amt, String typeId) {
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