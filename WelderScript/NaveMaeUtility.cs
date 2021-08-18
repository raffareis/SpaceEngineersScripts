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
	partial class Program {
		public void AjustaPainel(IMyTextSurface textSurface, int caracteresDesejados) {
			textSurface.Font = "Monospace";
			textSurface.FontColor = new VRageMath.Color(127, 127, 127);
			textSurface.FontSize = 1;
			textSurface.TextPadding = 0;
			var larguraPanel = textSurface.SurfaceSize.X;
			//var fontInicial = textSurface.FontSize;
			var tamanhoDesejado = (larguraPanel / (caracteresDesejados));
			var sb = new StringBuilder();
			sb.Append("─");
			var tamanhoTexto = textSurface.MeasureStringInPixels(sb, "Monospace", textSurface.FontSize);
			var tamanhoCaracter = tamanhoTexto.X;
			var tamanhoPorFontsize = (float)Math.Ceiling(tamanhoCaracter / textSurface.FontSize);
			var fontSizeNova = tamanhoDesejado / tamanhoPorFontsize;
			textSurface.FontSize = fontSizeNova;

			//WriteLog($"Ajustando Painel: L{larguraPanel:N1};Cols:{colunas};FSI:{fontInicial:N1};TD:{tamanhoDesejado:N1};TC:{tamanhoCaracter:N1};TFS:{tamanhoPorFontsize:N1};FSN:{fontSizeNova:N1}");

			//var caracteresW = (int)(Math.Floor(larguraPanel / tamanhoTexto.X));
		}
		private string MontaIndicator(int min, int max, int width, int valor) {
			var widthInterno = width - 2;
			if (valor > max)
				valor = max;
			char indicatorR = '▓';
			char indicatorL = '░';

			var valorBloco = Convert.ToInt32((max - min) / (float)widthInterno);
			var blocos = (int)((float)valor / (float)valorBloco);
			var blocosL = blocos < 0 ? -blocos : 0;
			var blocosR = blocos > 0 ? blocos : 0;
			var tamanhoL = (int)(min < 0 ? -min / (float)valorBloco : 0);
			var tamanhoR = (int)(max / (float)valorBloco);
			var strL = new string(' ', Math.Max(0, tamanhoL - blocosL)) + new string(indicatorL, Math.Max(0,blocosL));
			var strR = new string(indicatorR, Math.Max(0,blocosR)) + new string(' ', Math.Max(0,tamanhoR - blocosR));

			var final = "[" + strL +( min < 0 ? "|" : "") + strR + "]";
			return final;
		}

		private const string MULTIPLIERS = ".kMGTPEZY";

		
			private string getExtraField(IMyTerminalBlock block, string regexString) {
			System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(regexString, System.Text.RegularExpressions.RegexOptions.Singleline);
			string result = "";
			System.Text.RegularExpressions.Match match = regex.Match(block.DetailedInfo);
			if (match.Success) {
				result = match.Groups[1].Value;
			}
			return result;
		}

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

		private string amountFormatter(float amt, String typeId = "") {
			if (typeId.EndsWith("_Ore") || typeId.EndsWith("_Ingot")) {
				if (amt > 1000.0f) {
					return "" + Math.Round((float)amt / 1000, 2) + "K";
				} else {
					return "" + Math.Round((float)amt, 2);
				}
			}

			var newAmnt = amt;
			var units = new[] { "", "k", "M", "G", "T" };
			var curUnit = 0;
			while (newAmnt > 1000.0f) {
				curUnit++;
				newAmnt /= 1000.0f;
			}
			if (curUnit > units.Length - 1)
				return $"{amt:N0}";
			return $"{newAmnt:N1}{units[curUnit]}";
		}

		private bool filterThis(IMyTerminalBlock block) {
			return block.CubeGrid == Me.CubeGrid;
		}

		private bool filterNotThis(IMyTerminalBlock block) {
			return block.CubeGrid != Me.CubeGrid;
		}
		}
	}
