using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {

	public enum EnumTipoLog {
		Debug,
		Info,
		Error,
		Warning
	}

	public class ItemLog {
		public int Id { get; set; }
		public EnumTipoLog Tipo { get; set; }
		public DateTime DataHora { get; set; }
		public string Texto { get; set; }
	}

	public class MeuPainel {

		public MeuPainel(IMyTextSurface surface) {
			TextSurface = surface;
		}

		public void AjustaPainel() {
			TextSurface.Font = "Monospace";
			TextSurface.FontColor = new VRageMath.Color(127, 127, 127);
			TextSurface.FontSize = 1;
			TextSurface.TextPadding = 0;
			var larguraPanel = TextSurface.SurfaceSize.X;
			var fontInicial = TextSurface.FontSize;
			var tamanhoDesejado = (larguraPanel / (CaracteresDesejados));
			var sb = new StringBuilder();
			sb.Append("─");
			var tamanhoTexto = TextSurface.MeasureStringInPixels(sb, "Monospace", TextSurface.FontSize);
			var tamanhoCaracter = tamanhoTexto.X;
			var tamanhoPorFontsize = (float)Math.Ceiling(tamanhoCaracter / TextSurface.FontSize);
			var fontSizeNova = tamanhoDesejado / tamanhoPorFontsize;
			TextSurface.FontSize = fontSizeNova;

			//WriteLog($"Ajustando Painel: L{larguraPanel:N1};Cols:{colunas};FSI:{fontInicial:N1};TD:{tamanhoDesejado:N1};TC:{tamanhoCaracter:N1};TFS:{tamanhoPorFontsize:N1};FSN:{fontSizeNova:N1}");

			//var caracteresW = (int)(Math.Floor(larguraPanel / tamanhoTexto.X));
		}

		public void WriteText(string sb, int paddingW = 1, int paddingH = 1) {
			var linhas = sb.Split('\n').ToList();

			if (paddingH > 0) {
				for (var i = 0; i < paddingH; i++) {
					linhas.Insert(0, " ");
				}
			}

			for (var i = 0; i < linhas.Count; i++) {
				linhas[i] = "".PadLeft(paddingW) + linhas[i] + "".PadRight(paddingW);
			}
			for (var i = 0; i < paddingH; i++) {
				linhas.Add(" ");
			}

			TextSurface.WriteText(string.Join("\n", linhas));
		}

		public void PrintTest() {
			var sb = new StringBuilder();
			sb.Append("─");
			var tamanhoTexto = TextSurface.MeasureStringInPixels(sb, "Monospace", FontSize);
			var caracteresW = CaracteresDesejados; // (int)(Math.Floor(LarguraPixels / tamanhoTexto.X));
			var caracteresH = (int)(Math.Floor(AlturaPixels / tamanhoTexto.Y));

			var texto = new StringBuilder();
			for (var j = 1; j <= caracteresH; j++) {
				if (j == 1)
					texto.AppendLine("┌" + "".PadLeft(caracteresW - 3, '─') + "┐");
				else if (j == caracteresH)
					texto.AppendLine("└" + "".PadLeft(caracteresW - 3, '─') + "┘");
				else
					texto.AppendLine("│" + "".PadLeft(caracteresW - 3, 'X') + "│");
			}
			//WriteText("");
			WriteText(texto.ToString());
		}

		public string CustomData => ((IMyTerminalBlock)TextSurface)?.CustomData ?? "";

		public float FontSize {
			get { return TextSurface.FontSize; }
			set { TextSurface.FontSize = value; }
		}

		public string TipoPainel {
			get {
				if (string.IsNullOrWhiteSpace(CustomData))
					return "Não Definido";

				var tipoPainel = CustomData.Split(',')[0];
				return tipoPainel;
			}
		}

		public int NumeroDeColunas {
			get {
				if (string.IsNullOrWhiteSpace(CustomData))
					return 1;
				var campos = CustomData.Split(',').Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim()).ToArray();
				if (!string.IsNullOrWhiteSpace(CustomData) && campos.Length > 2) {
					var strColunas = campos[2];
					int colunas;
					if (int.TryParse(strColunas, out colunas))
						return colunas;
				}
				return 1;
			}
		}

		public int CaracteresDesejados {
			get {
				if (string.IsNullOrWhiteSpace(CustomData))
					return 60;
				var campos = CustomData.Split(',').Where(s => !string.IsNullOrEmpty(s)).Select(s => s.Trim()).ToArray();
				if (!string.IsNullOrWhiteSpace(CustomData) && campos.Length > 1) {
					var strColunas = campos[1];
					int colunas;
					if (int.TryParse(strColunas, out colunas))
						return colunas;
				}
				return 60;
			}
		}

		public IMyTextSurface TextSurface { get; private set; }

		public float LarguraPixels => TextSurface.SurfaceSize.X;
		public float AlturaPixels => TextSurface.SurfaceSize.Y;
	}

	partial class Program : MyGridProgram {

		// VARIAVEIS
		private List<MeuPainel> TodosPaineis;

		private List<MeuPainel> PanelsAssembler;
		private List<MeuPainel> PanelsRefinery;
		private List<MeuPainel> PanelsHydrogen;
		private List<MeuPainel> PanelsPower;
		private List<MeuPainel> PanelsJump;
		private List<MeuPainel> PanelsDebug;
		private List<MeuPainel> PanelsStructural;
		private List<MeuPainel> PanelsWar;
		private List<IMyTerminalBlock> blocksThrust;
		private List<IMyTerminalBlock> blocksContainers;
		private List<IMyBatteryBlock> blocksBatteries;
		private List<ItemLog> _log = new List<ItemLog>();
		private List<IMyAssembler> assemblers = null;
		private List<IMyRefinery> refineries = null;
		private IMyJumpDrive jumpDrive = null;
		private IMyCockpit cockpit = null;
		private string lastCustomData = "~";
		private long ticks = 0;

		public Program() {
			Runtime.UpdateFrequency = UpdateFrequency.Update10;

			//TodosPaineis.Add(new MeuPainel(Me.GetSurface(0)));
			//PanelsComponent = myTextSurfacesTerminal.Where(t => t.CustomData.Contains("Component")).Cast<IMyTextSurface>().ToList();
			//PanelsAssembler = myTextSurfacesTerminal.Where(t => t.CustomData.Contains("Assembler")).Cast<IMyTextSurface>().ToList();
			//PanelsRefinery = myTextSurfacesTerminal.Where(t => t.CustomData.Contains("Refinery")).Cast<IMyTextSurface>().ToList();
			//PanelsHydrogen = myTextSurfacesTerminal.Where(t => t.CustomData.Contains("Hydrogen")).Cast<IMyTextSurface>().ToList();
			//PanelsPower = myTextSurfacesTerminal.Where(t => t.CustomData.Contains("Power")).Cast<IMyTextSurface>().ToList();
			//PanelsJump = myTextSurfacesTerminal.Where(t => t.CustomData.Contains("Jump")).Cast<IMyTextSurface>().ToList();
			//PanelDebug = new MeuPainel(Me.GetSurface(0));

			Inicializa();
		}

		private void Inicializa() {
			

			List<IMyTerminalBlock> myTextSurfacesTerminal = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyTextSurface>(myTextSurfacesTerminal, filterThis);

			TodosPaineis = myTextSurfacesTerminal
				.Where(t => !string.IsNullOrEmpty(t.CustomData))
				.Cast<IMyTextSurface>()
				.Select(t => new MeuPainel(t))
				.ToList();

			PanelsAssembler = TodosPaineis.Where(p => p.TipoPainel == "Assembler").ToList();
			PanelsRefinery = TodosPaineis.Where(p => p.TipoPainel == "Refinery").ToList();
			PanelsHydrogen = TodosPaineis.Where(p => p.TipoPainel == "Hydrogen").ToList();
			PanelsPower = TodosPaineis.Where(p => p.TipoPainel == "Power").ToList();
			PanelsJump = TodosPaineis.Where(p => p.TipoPainel == "Jump").ToList();
			PanelsDebug = TodosPaineis.Where(p => p.TipoPainel == "Debug").ToList();
			PanelsStructural = TodosPaineis.Where(p => p.TipoPainel == "Structural").ToList();
			PanelsWar = TodosPaineis.Where(p => p.TipoPainel == "War").ToList();
			WriteLog("Inicializando...");

			blocksThrust = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyThrust>(blocksThrust, filterThis);

			blocksContainers = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocksContainers, a=> { return filterThis(a) && a.HasInventory; });

			blocksBatteries = new List<IMyBatteryBlock>();

			GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(blocksBatteries, filterThis);
			WriteLog("Paineis Assembler: " + PanelsAssembler.Count);
			WriteLog("Paineis Refinery: " + PanelsRefinery.Count);
			WriteLog("Paineis Hydrogen: " + PanelsHydrogen.Count);
			WriteLog("Paineis Power: " + PanelsPower.Count);
			WriteLog("Paineis Jump: " + PanelsJump.Count);
			WriteLog("Paineis Debug: " + PanelsDebug.Count);
			WriteLog("Containers: " + blocksContainers.Count);
			WriteLog("Thrusters: " + blocksThrust.Count);
			if (refineries == null) {
				refineries = new List<IMyRefinery>();
				GridTerminalSystem.GetBlocksOfType<IMyRefinery>(refineries);
				WriteLog("Refineries Localizadas: " + refineries.Count);
			}
			if (assemblers == null) {
				assemblers = new List<IMyAssembler>();
				GridTerminalSystem.GetBlocksOfType(assemblers, r => r.CubeGrid == Me.CubeGrid);
				WriteLog("Assemblers Localizados: " + assemblers.Count);
			}
			if (jumpDrive == null) {
				var lstJumps = new List<IMyTerminalBlock>();
				GridTerminalSystem.GetBlocksOfType<IMyJumpDrive>(lstJumps, filterThis);
				if (lstJumps.Any())
					jumpDrive = lstJumps.First() as IMyJumpDrive;
			}
			if (cockpit == null) {
				var lstCock = new List<IMyTerminalBlock>();
				GridTerminalSystem.GetBlocksOfType<IMyCockpit>(lstCock, filterThis);
				var newlst = lstCock.Cast<IMyCockpit>();
				var mainCock = newlst.SingleOrDefault(c => c.IsMainCockpit);
				if (mainCock != null)
					cockpit = mainCock;
			}
			
		}

		

		private IEnumerable<IMyTerminalBlock> GetIncompleteBlocks(bool markOnHud = true)
        {
			var lst = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocks(lst);
			var minVec = Me.CubeGrid.Min;
			var maxVec = Me.CubeGrid.Max;
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();

			for (var i = minVec.X; i <= maxVec.X; i++)
				for (var j = minVec.Y; j <= maxVec.Y; j++)
					for (var k = minVec.Y; k <= maxVec.Y; k++) {
						var vec = new Vector3I(i, j, k);
						if (Me.CubeGrid.CubeExists(vec))
						{
							var cubo = Me.CubeGrid.GetCubeBlock(vec);
							
							if(cubo!=null)
								blocks.Add(cubo);
						}
					}
			
			foreach(var cubo in blocks)      {
				if(cubo.HasDeformation || !cubo.IsFullIntegrity || cubo.BuildLevelRatio < 1 || cubo.CurrentDamage > 0 || cubo.BuildIntegrity < 1 || cubo.IsDestroyed)
                {
					//GPS:[Asteroid] Ag4: -241699.94:-123577.88:-154025.73:#FFF18975:
					var posicaoMundo = Me.CubeGrid.GridIntegerToWorld(cubo.Position);
					Echo($"Cubo deformado: x:{posicaoMundo.X} y:{posicaoMundo.Y} z:{posicaoMundo.Z}");					
					WriteLog($"GPS:[Cubo deformado]:{posicaoMundo.X}:{posicaoMundo.Y}:{posicaoMundo.Z}:#FF00FF:");
				}
            }			
			//lst.Where(i=>i.ShowOnHUD).ToList().ForEach(i => i.ShowOnHUD = false);
			var faulty = lst.Where(i => !i.IsFunctional).ToList();
			if (markOnHud)
				faulty.ForEach(i => i.ShowOnHUD = true);
			return faulty;
        }
		private void Main() {
			ticks++;
			if (cockpit != null)
				UpdateCockpit();




			if (ticks % 20 == 0) { //Os que atualiza 20x menos
				if (Me.CustomData != lastCustomData) {
					Inicializa();
					TodosPaineis.ForEach(a => a.AjustaPainel());
					lastCustomData = Me.CustomData;
				}
				if (Me.CustomData == "DebugPanels") {
					TodosPaineis.ForEach(p => p.PrintTest());
					return;
				}


				if (PanelsPower.Any())
					UpdatePowerPanel();
				if (PanelsJump.Any())
					UpdateJumpPanel();
				if (PanelsAssembler.Any())
					UpdateProducao();
				if (PanelsRefinery.Any())
					UpdateInventory();

				
				

			}
			if (ticks % 40 == 0) {
				//UpdateFaults();
				if (PanelsHydrogen.Any())
					UpdateHydrogen();
				//Os que atualiza 40x menos
				//UpdateBatteryPanel();
				//IMyTextPanel panelPower =  GridTerminalSystem.GetBlockWithName(PANEL_POWER);
				//UpdateBatteryPanel();

			}
			if(ticks % 80 == 0) {
				
			}

			//UpdateInventory();
			//UpdateProducao();
		}
		private void UpdateFaults()
        {
			var faulty = GetIncompleteBlocks(true);
			foreach (var i in faulty)
				WriteLog("Faulty: " + i.CustomName);

		}
		private void UpdateCockpit() {
			/* Motion */
			var panelMove = new MeuPainel(cockpit.GetSurface(2));
			var textoMove = $"\nMov: {cockpit.MoveIndicator}";
			textoMove += $"\nRot: {cockpit.RotationIndicator}";
			textoMove += $"\nRol: {cockpit.RollIndicator:N2}";
			panelMove.WriteText(textoMove);
			
			/* Thrust */
			var panelThrust = new MeuPainel(cockpit.GetSurface(1));

			var thrusts = blocksThrust.Cast<IMyThrust>();
			var tTotal = thrusts.Count();
			var tWorking = thrusts.Count(t => t.IsWorking && t.IsFunctional);

			var textoThrust = $"Thr: {tWorking} / {tTotal}\n";

			//var grp = thrusts.GroupBy(g => Vector3I.GetDominantDirection(g.GridThrustDirection));

			//var L = grp.Where(g => g.Key == CubeFace.Left).Select(g => g.Sum(a => a.CurrentThrust) / g.Sum(a => a.MaxThrust)).SingleOrDefault();
			//var R = grp.Where(g => g.Key == CubeFace.Right).Select(g => g.Sum(a => a.CurrentThrust) / g.Sum(a => a.MaxThrust)).SingleOrDefault();
			//var U = grp.Where(g => g.Key == CubeFace.Up).Select(g => g.Sum(a => a.CurrentThrust) / g.Sum(a => a.MaxThrust)).SingleOrDefault();
			//var D = grp.Where(g => g.Key == CubeFace.Down).Select(g => g.Sum(a => a.CurrentThrust) / g.Sum(a => a.MaxThrust)).SingleOrDefault();
			//var F = grp.Where(g => g.Key == CubeFace.Forward).Select(g => g.Sum(a => a.CurrentThrust) / g.Sum(a => a.MaxThrust)).SingleOrDefault();
			//var B = grp.Where(g => g.Key == CubeFace.Backward).Select(g => g.Sum(a => a.CurrentThrust) / g.Sum(a => a.MaxThrust)).SingleOrDefault();

			//var LR = L - R;
			//var BF = B - F;
			//var DU = D - U;

			//var maxWidthIndicator = 30;

			//textoThrust += $"\nLR:{LR.ToString("P0").PadLeft(6)}";
			//textoThrust += $"\n{MontaIndicator(-100, 100, maxWidthIndicator, (int)(LR * 100))}";
			//textoThrust += $"\n{MontaIndicator(-100, 100, maxWidthIndicator, (int)(LR * 100))}";
			//textoThrust += $"\n\nBF:{BF.ToString("P0").PadLeft(6)}";

			//textoThrust += $"\n{MontaIndicator(-100, 100, maxWidthIndicator, (int)(BF * 100))}";
			//textoThrust += $"\n{MontaIndicator(-100, 100, maxWidthIndicator, (int)(BF * 100))}";

			//textoThrust += $"\n\nDU:{DU.ToString("P0").PadLeft(6)}";
			//textoThrust += $"\n{MontaIndicator(-100, 100, maxWidthIndicator, (int)(DU * 100))}";
			//textoThrust += $"\n{MontaIndicator(-100, 100, maxWidthIndicator, (int)(DU * 100))}";

			//foreach(var g in grp.OrderBy(a=>a.Key)) {
			//    var curT = g.Sum(a => a.CurrentThrust);
			//    var maxT = g.Sum(a => a.MaxThrust);
			//    var dir = g.Key;
			//    textoThrust += $"\n{dir}: {(curT / maxT):P1}";
			//}

			panelThrust.WriteText(textoThrust);
		}

		
		Dictionary<string, float> todosComponentesPrev = new Dictionary<string, float>();
		Dictionary<string, float> todosOres = new Dictionary<string, float>();
			Dictionary<string, int> todosOresRefinarias = new Dictionary<string, int>();
		Dictionary<string, float> todosComponentes = new Dictionary<string, float>();
			Dictionary<string, int> todosComponentesAssembler = new Dictionary<string, int>();
		void UpdateProducao() {
			var x = 0;
			todosOres.Clear();
			todosOresRefinarias.Clear();
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
					todosOresRefinarias[firstItem?.Type.SubtypeId]++;
				}

			}
			string txtFinal = $"--- Refinarias ({x} / {refineries.Count}) ---\n";

			foreach (var ore in todosOres) {
				txtFinal += $"{ore.Value.ToString("N1").PadLeft(8, ' ')} {ore.Key} [{todosOresRefinarias[ore.Key]}]\n";
			}
			x = 0;
			todosComponentes.Clear();
			todosComponentesAssembler.Clear();
			foreach (var assembler in assemblers) {
				//List<VRage.Game.ModAPI.Ingame.MyInventoryItem> oresRefinaria = new List<MyInventoryItem>();
				Echo("Assembler: " + assembler.Name);
				var itensProd = new List<MyProductionItem>();
				assembler.GetQueue(itensProd);// .GetItems(oresRefinaria);
				
				if (!itensProd.Any()) {
					continue;
				}
				
				//var firstItem = itensProd.FirstOrDefault();
				foreach (var i in itensProd) {
					if (!todosComponentes.ContainsKey(i.BlueprintId.SubtypeName)) {
						todosComponentes.Add(i.BlueprintId.SubtypeName, (float)i.Amount);
						todosComponentesAssembler.Add(i.BlueprintId.SubtypeName, 1);
					} else {

						todosComponentes[i.BlueprintId.SubtypeName] += (float)i.Amount;
						todosComponentesAssembler[i.BlueprintId.SubtypeName]++;
					}
				}
				x++;

				

			}
			//string txtFinal = $"--- Refinarias ({x} / {refineries.Count}) ---\n";



			txtFinal += $"\n\n--- Assemblers ({x} / {assemblers.Count}) ---\n";
			foreach (var ore in todosComponentes.Where(c=>c.Value > 1)) {
				var estaProduzindo = todosComponentesPrev.ContainsKey(ore.Key) && todosComponentesPrev[ore.Key] > ore.Value ? (Convert.ToInt32(todosComponentes[ore.Key] - todosComponentesPrev[ore.Key])).ToString().PadLeft(3) : "   " ;
				txtFinal += $"{estaProduzindo}{Convert.ToInt32(ore.Value).ToString().PadLeft(8, ' ')} {ore.Key} [{todosComponentesAssembler[ore.Key]}]\n";
			}
			//if (ticks % 40 == 0) { //Hack pra dar tempo de contabilizar
				todosComponentesPrev.Clear();
				foreach (var c in todosComponentes)
					todosComponentesPrev.Add(c.Key, c.Value);
			//}/
			//WriteLog(txtFinal);
			WriteToPanels(PanelsAssembler, txtFinal);
		}


		class InventoryInfo {
			public int Id { get; set; }
			public int CurrentVolume { get; set; }
			public int MaxVolume { get; set; }

		}


		private DateTime lastHydrogenTime = DateTime.Now;
		private int lastIceQt = 0;
		private int lastUraniumQt = 0;
		private float lastHydroPercent = 0;
		private void UpdateHydrogen() {
			var generators = new List<IMyGasGenerator>();
			GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(generators, filterThis);
			var tanks = new List<IMyGasTank>();
			GridTerminalSystem.GetBlocksOfType<IMyGasTank>(tanks, filterThis);

			var inventories = blocksContainers.SelectMany(b => {
				List<IMyInventory> lst = new List<IMyInventory>();
				for (var i = 0; i < b.InventoryCount; i++) {
					lst.Add(b.GetInventory(i));

				};
				return lst;
			});
			var gelos = inventories.Sum(i => i.GetItemAmount(MyItemType.MakeOre("Ice")).ToIntSafe()  );
			var uraniums = inventories.Sum(i => i.GetItemAmount(MyItemType.MakeIngot("Uranium")).ToIntSafe());
			

			var totalCur = 0f;
			var totalMax = 0f;
			foreach(var t in tanks) {
				totalMax += t.Capacity;
				totalCur += Convert.ToSingle(t.Capacity * t.FilledRatio);
            }

			var timeElapsed = DateTime.Now - lastHydrogenTime;
			var deltaCur = (totalCur/totalMax) - lastHydroPercent;
			var deltaGelo = gelos - lastIceQt;
			var deltaUranium = uraniums - lastUraniumQt;

			lastHydrogenTime = DateTime.Now;
			lastHydroPercent = totalCur / totalMax;
			lastIceQt = gelos;
			lastUraniumQt = uraniums;
			var geloPorMinuto = deltaGelo / timeElapsed.TotalMinutes;
			var uraniumPorMinuto = deltaUranium / timeElapsed.TotalMinutes;
			var tempoRestanteGelo = gelos / geloPorMinuto;
			var tempoRestanteUranium = uraniums / uraniumPorMinuto;

			
			var strHydroP = MontaIndicator(0, 100, 40, Convert.ToInt32((totalCur / totalMax) * 100));
			var tempoHydro = (1 - (totalCur / totalMax)) / (deltaCur / timeElapsed.TotalMinutes);
			var strConsumoGelo = $"{-geloPorMinuto:N1} Ice/Min  ({-tempoRestanteGelo:N1} min)";
			var strConsumoUranium = $"{-uraniumPorMinuto:N1} U/Min  ({-tempoRestanteUranium:N1} min)";

			var sb = new StringBuilder();
			sb.AppendLine($"Hydrogen   {totalCur/totalMax:P2}   {tempoHydro:N1}min  :");
			sb.AppendLine(strHydroP);
			
			sb.AppendLine($"\nIce ({gelos/1000:N1}k): ");
			sb.AppendLine(strConsumoGelo);
			sb.AppendLine($"\nU ({uraniums}): ");
			sb.AppendLine(strConsumoUranium);
			WriteToPanels(PanelsHydrogen, sb.ToString());

        }

		private void UpdateInventory() {
			


			var dictItens = new Dictionary<string, int>();
			var dictIngots = new Dictionary<string, float>();
			var dictOres = new Dictionary<string, float>();
			var dictInv = new Dictionary<string, List<InventoryInfo>>();
			
			var sb = new StringBuilder();
			foreach (var c in blocksContainers) {
				var lstInv = new List<InventoryInfo>();
				for (var i = 0; i < c.InventoryCount; i++) {
					
					var inv = c.GetInventory(i);
					lstInv.Add(new InventoryInfo { Id = i, CurrentVolume = inv.CurrentVolume.ToIntSafe(), MaxVolume = inv.MaxVolume.ToIntSafe() });
					var items = new List<MyInventoryItem>();
					inv.GetItems(items);
					foreach (var item in items) {
						var typeId = item.Type.TypeId.ToString();
						var id = decodeItemName(item.Type.SubtypeId.ToString(), typeId);
						if (typeId.EndsWith("_Ingot")) {
							if (!dictIngots.ContainsKey(id)) {
								dictIngots.Add(id, 0);
							}
							dictIngots[id] += (float)item.Amount;
						} else if (typeId.EndsWith("_Ore") ) {
							if (!dictOres.ContainsKey(id)) {
								dictOres.Add(id, 0);
							}
							dictOres[id] += (float)item.Amount;
						}						
						else {
							if (!dictItens.ContainsKey(id)) {
								dictItens.Add(id, 0);
							}
							dictItens[id] += item.Amount.ToIntSafe();
						}
					}
				}
				if(c is IMyCargoContainer)
					dictInv.Add(c.CustomName, lstInv);



			}

			sb.AppendLine("         --- COMPONENTS ---");
			var listItens = dictItens.Keys.OrderBy(n => n);
			foreach (var item in listItens) {
				sb.AppendLine(dictItens[item].ToString().PadLeft(8, ' ') + " " + item);
			}

			sb.AppendLine("\n            --- INGOTS ---");
			var listIngots = dictIngots.Keys.OrderBy(n => n);
			foreach (var item in listIngots) {
				sb.AppendLine("  " + dictIngots[item].ToString("N1").PadLeft(8, ' ') + " " + item);
			}

			sb.AppendLine("\n            --- ORES ---");
			var listOres = dictOres.Keys.OrderBy(n => n);
			foreach (var item in listOres) {
				sb.AppendLine("  " + dictOres[item].ToString("N1").PadLeft(8, ' ') + " " + item);
			}
			if (false) { //Desativado

				sb.AppendLine("\n            --- CARGO ---");
				var listInv = dictInv.Where(a => a.Value.Max(s => s.MaxVolume) > 15).OrderByDescending(n => n.Value.Select(a => a.MaxVolume).Max()).ThenByDescending(n => n.Value.Sum(a => a.CurrentVolume));
				var v = 0;
				foreach (var item in listInv) {
					v++;
					var curVolTot = item.Value.Sum(i => i.CurrentVolume);
					var curVolMax = item.Value.Sum(i => i.MaxVolume);
					//sb.AppendLine(v.ToString());
					sb.AppendLine($"  {v.ToString().PadLeft(2)} - {(float)curVolTot / curVolMax:P0} ({item.Key})");
					var iid = 0;
					foreach (var ii in item.Value) {
						sb.AppendLine($"    - {ii.CurrentVolume.ToString().PadLeft(4)}/{ii.MaxVolume.ToString().PadLeft(4)} ({(float)ii.CurrentVolume / ii.MaxVolume:P0})");

					}
					//sb.AppendLine("  " +  dictInv[item].ToString("N1").PadLeft(8, ' ') + " " + item);
				}

			}
			

			WriteToPanels(PanelsRefinery, sb.ToString());
		}

		private void UpdateJumpPanel() {
			var maxInput = getExtraFieldFloat(jumpDrive, "Max Required Input: (\\d+\\.?\\d*) (\\w?)W");
			var maxStored = getExtraFieldFloat(jumpDrive, "Max Stored Power: (\\d+\\.?\\d*) (\\w?)Wh");
			var curInput = getExtraFieldFloat(jumpDrive, "Current Input: (\\d+\\.?\\d*) (\\w?)W");
			var storedPower = getExtraFieldFloat(jumpDrive, "Stored power: (\\d+\\.?\\d*) (\\w?)Wh");
			var maxDist = getExtraFieldFloat(jumpDrive, "Max jump distance:(\\d+\\.?\\d*) (\\w?)km");
			var curJump = getExtraFieldFloat(jumpDrive, "Current jump:(\\d+\\.?\\d*)(\\w?)%");
			var curDistProp = jumpDrive.GetProperty("JumpDistance");// as MyTerminalControlSlider<MyJumpDrive>;// .AsFloat();// .AsFloat();
			var curDistPercent = curDistProp.AsFloat().GetValue(jumpDrive);
			var curDistKm = (int)Math.Round(((maxDist * curDistPercent) / 100d) + 5);
			var status = jumpDrive.Status;
			var texto = $"Input: {amountFormatter(curInput)}W / {amountFormatter(maxInput)}W ({curInput / maxInput:P1})\nStore: {amountFormatter(storedPower)}Wh / {amountFormatter(maxStored)}Wh ({storedPower / maxStored:P1})\n\nCur Dist: {curDistKm}km ({curDistPercent:N0}%)\nMax Dist: {maxDist}km\n\nCur Jump: {curJump}%\nStatus: {status}";
			//List<ITerminalProperty> properties = new List<ITerminalProperty>();
			//jumpDrive.GetProperties(properties);
			//         List<Sandbox.ModAPI.Interfaces.ITerminalAction> actions = new List<Sandbox.ModAPI.Interfaces.ITerminalAction>();
			//         jumpDrive.GetActions(actions);
			//         texto += "\n";
			//         foreach(var p in actions) {
			//             texto += $"\n{p.Id} - {p.Name}";
			//}

			WriteToPanels(PanelsJump, texto);
		}

		private void UpdatePowerPanel() {
			//BATERIAS
			var totalCur = 0f;
			var totalMax = 0f;
			
			var blocks = blocksBatteries.OrderBy(b => b.CustomName).ToList();
			var txtFinal = "--- Baterias (" + blocks.Count + ") ---";

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
			}
			txtFinal += "\n--- TOTAL: " + String.Format("{0:N1}MW / {1:N1}MW ({2:P0})", totalCur / 1000000, totalMax / 1000000, totalCur / totalMax) + "\n\n";

			var curTotal = 0f;
			var maxTotal = 0f;

			var blocksSolar = new List<IMySolarPanel>();
			GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(blocksSolar);
			if (blocksSolar.Count > 0) {
				blocksSolar = blocksSolar.OrderBy(b => b.CustomName).ToList();

				txtFinal += "--- Solar (" + blocksSolar.Count + ") ---";

				for (int i = 0; i < blocksSolar.Count; i++) {
					var maxPow = getExtraFieldFloat(blocksSolar[i], "Max Output: (\\d+\\.?\\d*) (\\w?)W");
					var curPow = getExtraFieldFloat(blocksSolar[i], "Current Output: (\\d+\\.?\\d*) (\\w?)W");
					txtFinal += "\n" + (i + 1).ToString().PadLeft(2, ' ') + ": " + String.Format("{0:N0}kW / {1:N0}kW", curPow / 1000, maxPow / 1000);

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
					txtFinal += "\n" + (i + 1).ToString().PadLeft(2, ' ') + ": " + String.Format("{0:N0}kW / {1:N0}kW", curPow / 1000, maxPow / 1000);
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

					txtFinal += "\n" + (i + 1).ToString().PadLeft(2, ' ') + ": " + String.Format("{0:N0}kW / {1:N0}kW ({2})", curPow / 1000, maxPow / 1000, fill);
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
					var uranium = amountFormatter(blocksNuclear[i].GetInventory(0).CurrentMass.RawValue / 1000);
					txtFinal += "\n" + (i + 1).ToString().PadLeft(2, ' ') + ": " + String.Format("{0:N0}kW / {1:N0}kW", curPow / 1000, maxPow / 1000);
					txtFinal += $" (U: {uranium}g)";
					maxTotal += maxPow;
					curTotal += curPow;
				}
			}

			txtFinal += String.Format("\n--- TOTAL: {0:N0}kW / {1:N0}kW ---", curTotal / 1000, maxTotal / 1000);

			//var linhas = txtFinal.Split('\n');

			WriteToPanels(PanelsPower, txtFinal);
		}

		private void WriteLog(string texto, EnumTipoLog tipo = EnumTipoLog.Info) {
			var id = _log.Any() ? _log.Max(l => l.Id) + 1 : 1;
			_log.Add(new ItemLog { DataHora = DateTime.Now, Texto = texto, Tipo = tipo, Id = id });
			var stringTexto = string.Join("\n", _log.OrderByDescending(l => l.Id).Take(40).Select(t => $"{t.Id.ToString().PadLeft(4, '0')} {t.DataHora:HH:mm} [{t.Tipo}] {t.Texto}"));

			WriteToPanels(PanelsDebug, stringTexto);
		}

		private void WriteToPanels(List<MeuPainel> panels, string text) {
			foreach (var p in panels) {
				p.WriteText(text);
			}
		}

		
	}
}