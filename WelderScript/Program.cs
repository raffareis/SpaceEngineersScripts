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

        IMyTextSurface PANEL_BATTERY = null;
        IMyTextSurface PANEL_CARGO = null;
        IMyTextSurface PANEL_LOG = null;        
        IMyTextSurface PANEL_FL = null;
        IMyTextSurface PANEL_FR = null;

        ///////////////////////
        ////// VARIABLES ////
        ///////////////////////
        Dictionary<string, int> DesiredItems {
            get {
                var cd = Me.CustomData;
                var dict = new Dictionary<string, int>();
                var linhas = cd.Split('\n');
                foreach(var l in linhas) {
                    var cp = l.Split(',');
                    dict.Add(cp[0], int.Parse(cp[1].Trim()));
                }
                return dict;

            }

        }
        Dictionary<string,string> ItemsToBluePrint = new Dictionary<string, string>();
        //Dictionary<string, int> MinimumItems = new Dictionary<string, int>();
        //Dictionary<string, float> Inventories = new Dictionary<string, float>();
        Dictionary<string, int> CurrentItems = new Dictionary<string, int>();
        float MaxVolume = 0f;
        float CurrentVolume = 0f;
        List<IMyTerminalBlock> containers = null;
        List<IMyBatteryBlock> batteryBlocks = null;
       

        IMyShipConnector Connector = null;
        ///////////////////////
        ////// METHODS//////
        ///////////////////////


        bool filterContainersBase(IMyTerminalBlock block) {
            if (block.CubeGrid == Me.CubeGrid) return false;
            if (!(block is IMyInventoryOwner)) return false;
            var ownerBlock = (IMyInventoryOwner)block;
            var invConnector = ((IMyInventoryOwner)Connector).GetInventory(0);
            
            for(var i = 0; i<ownerBlock.InventoryCount; i++) {
                if (ownerBlock.GetInventory(i).IsConnectedTo(invConnector)) return true;
			}

            return false;

        }
        bool filterMyContainers(IMyTerminalBlock block) {
            return block.CubeGrid == Me.CubeGrid
                    && block is IMyCargoContainer

                   // && (float)((IMyInventoryOwner)block).GetInventory(0).CurrentVolume / (float)((IMyInventoryOwner)block).GetInventory(0).MaxVolume < .9f
                    && ((IMyInventoryOwner)block).GetInventory(0).IsConnectedTo(((IMyInventoryOwner)Connector).GetInventory(0));

        }
        IMyInventory GetAvailableInventory() {
            var myContainers = new List<IMyCargoContainer>();
            GridTerminalSystem.GetBlocksOfType(myContainers, filterMyContainers);
            var owner = myContainers.Select(c => c.GetInventory()).Where(i => !i.IsFull).OrderBy(i => ((float)i.CurrentVolume.RawValue / i.MaxVolume.RawValue)).FirstOrDefault();
            //var owner = (IMyInventoryOwner)myContainers.Where(block=> ((IMyInventoryOwner)block).GetInventory(0).MaxVolume.RawValue > 1000000) .OrderBy(block => (float)((IMyInventoryOwner)block).GetInventory(0).CurrentVolume / (float)((IMyInventoryOwner)block).GetInventory(0).MaxVolume).FirstOrDefault();
            return owner;

        }
        void TransferRequiredItems() {
            var requiredItems = new Dictionary<string, int>();
            Dictionary<string, decimal> itemsToBuild = new Dictionary<string, decimal>();
            foreach (var m in DesiredItems) {
                if (!CurrentItems.ContainsKey(m.Key))
                    requiredItems.Add(m.Key, m.Value);
                else if (CurrentItems[m.Key] < m.Value) {
                    requiredItems.Add(m.Key, m.Value - CurrentItems[m.Key]);
                }
            }
            //WriteItemsOnPanel(requiredItems,PanelFl);
            //PanelFl.WritePublicText(requiredItems.Count.ToString());
            var baseContainers = new List<IMyTerminalBlock>();
            //GridTerminalSystem.GetBlocksOfType<IMyInventoryOwner>(baseContainers).Where(filterContainersBase) (baseContainers, filterContainersBase);

            GridTerminalSystem.GetBlocksOfType<IMyInventoryOwner>(baseContainers, filterContainersBase);
            var inventories = baseContainers.SelectMany(b => {
                var invs = new List<IMyInventory>();
                for (var i = 0; i < b.InventoryCount; i++)
                    invs.Add(b.GetInventory(i));
                return invs;
            });
            var txtDebug = "";
            foreach (var required in requiredItems) {
                var myInventory = GetAvailableInventory();
                //  var required = requiredItems.First();

                var amountLeft = required.Value;
                foreach (var inv in inventories) {
                    var items = new List<MyInventoryItem>();
                    inv.GetItems(items);
                    foreach (var i in items) {
                        if (i.Type.SubtypeId.ToString() != required.Key)
                            continue;
                        var amountToTransfer = Math.Min(amountLeft, i.Amount.ToIntSafe());
                        Transfer(inv, myInventory, required.Key, amountToTransfer);
                        amountLeft -= amountToTransfer;
                        txtDebug += required.Key + " " + i.Amount + "\n";
                    }
                }
                if (amountLeft > 0)
                    itemsToBuild.Add(required.Key, amountLeft);
            }

            if (itemsToBuild.Any())
                BuildRemainingItems(itemsToBuild);
            //PanelFl.WritePublicText(txtDebug);
        }

        private void BuildRemainingItems(Dictionary<string, decimal> itemsToBuild) {
            var assemblers = new List<IMyAssembler>();
            
            GridTerminalSystem.GetBlocksOfType(assemblers);
                    
            var assemblerCount = assemblers.Count;
            var assemblerRatio = 1;
            Echo($"Assemblers: {assemblerCount}");


            foreach (var item in itemsToBuild) {

                if (!ItemsToBluePrint.ContainsKey(item.Key)) { 
                    Echo($"Item Não encontrado no dicionário de blueprints: {item.Key} - {item.Value}");
                    continue;
                }

                MyDefinitionId mydef;                
                MyDefinitionId.TryParse(ItemsToBluePrint[item.Key], out mydef);
                if (assemblers.Any(a => {
                    var lst = new List<MyProductionItem>();
                    a.GetQueue(lst);
                    return lst.Any(i => i.BlueprintId == mydef && i.Amount >= 1);
                }))
                    continue;


                var assemblersToUse = assemblers.OrderByDescending(a => a.IsQueueEmpty).ThenBy(a => a.IsProducing).ThenBy(a => {
                    var lst = new List<MyProductionItem>();                    
                    a.GetQueue(lst);
                    return lst.Sum(i => i.Amount.RawValue);
                }).Take(assemblerCount / assemblerRatio );
                foreach (var assembler in assemblersToUse) {
                    if (assembler != null && item.Value > 0) {
                        Echo($"Construindo {item.Value * 1.5m / (assemblerCount / assemblerRatio)} {item.Key} em {assembler.CustomName}");
                        assembler.AddQueueItem(mydef, item.Value * 1.5m /(assemblerCount / assemblerRatio));
                    }
                }
                
            }

        }

        void UpdateCurrentItems() {
            MaxVolume = 0f;
            CurrentVolume = 0f;
            CurrentItems.Clear();
           
            var txtFinal = "";
            foreach (var c in containers) {
                if (c is IMyInventoryOwner)
                    for (var i = 0; i < c.InventoryCount; i++) {
                        var inv = c.GetInventory(i);
                        MaxVolume += (float)inv.MaxVolume;
                        CurrentVolume += (float)inv.CurrentVolume;
                        var items = new List<MyInventoryItem>();
                        inv.GetItems(items);
                        foreach (var item in items) {
                            var typeId = item.Type.TypeId.ToString();
                            var id = item.Type.SubtypeId.ToString();
                            if (!CurrentItems.ContainsKey(id)) {
                                CurrentItems.Add(id, 0);
                            }
                            CurrentItems[id] += item.Amount.ToIntSafe();
                        }
                    }
            }


        }


        ////////////////////////
        //////    MAIN  /////////
        ////////////////////////


        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            PopulaDictionaries();
            var lst = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(lst, filterThis);
            var cock = lst.FirstOrDefault();
            PANEL_CARGO = cock.GetSurface(0);
            PANEL_BATTERY = cock.GetSurface(3);
            PANEL_FL = cock.GetSurface(1);
            PANEL_FR = cock.GetSurface(2);
            var myConnectors = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(myConnectors, filterThis);
            Connector = myConnectors.FirstOrDefault() as IMyShipConnector;


            containers = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(containers, i=> filterThis(i) && i.HasInventory);
            
            batteryBlocks = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteryBlocks, filterThis);

        }



        void Main() {
           


            UpdateCurrentItems();
            UpdateBatteryPanel();
            UpdateInventoryTop();
            UpdateInventoryFront();
            // PanelFl.WritePublicText("Why YU");
            if (Connector.Status == MyShipConnectorStatus.Connected)
                TransferRequiredItems();

        }
        void WriteItemsOnPanel(Dictionary<string, int> items, IMyTextSurface panel, string titulo = "") {
            var txtFinal = titulo;
            if (titulo != "")
                txtFinal += "\n";
            var x = 0;


            foreach (var i in items.OrderBy(i=>i.Key)) {
                txtFinal += i.Value.ToString().PadLeft(4, ' ') + "-" + i.Key.Substring(0, Math.Min(6, i.Key.Length)).PadRight(6, ' ') + " " + ((x % 2 == 0) ? " " : "\n");
                x++;
            }
            panel.WriteText(txtFinal);

        }
        void UpdateInventoryFront() {
            var itensFaltando = new Dictionary<string, int>();
            var sb = new StringBuilder();
            foreach (var m in DesiredItems.OrderBy(d=> d.Key)) {
                var current = CurrentItems.ContainsKey(m.Key) ? CurrentItems[m.Key] : 0;
                sb.AppendLine($"{MontaIndicator(0, 100, 10, Convert.ToInt32((float)current * 100 / m.Value))} {current.ToString().PadLeft(4)}/{m.Value.ToString().PadLeft(4)} {m.Key.Substring(0,Math.Min(m.Key.Length,5)).PadRight(5)}");
                

                //if (!CurrentItems.ContainsKey(m.Key) || CurrentItems[m.Key] < m.Value) {
                //    itensFaltando.Add(m.Key, CurrentItems.ContainsKey(m.Key) ? m.Value - CurrentItems[m.Key] : m.Value);
                //}
            }

            var texto = sb.ToString();
            var linhas = texto.Split('\n');
            var altura = 12;
            PANEL_FL.WriteText(string.Join("\n", linhas.Take(altura)));
            PANEL_FR.WriteText(string.Join("\n", linhas.Skip(altura)));

            //WriteItemsOnPanel(CurrentItems, PANEL_FL,"Inventário");         
            //WriteItemsOnPanel(itensFaltando, PANEL_FR,"Faltando");
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
            var txtFinal = "";
            long currentTotal = 0;
            long maxTotal = 0;
            var idx = 1;
            foreach(var i in containers.OrderByDescending(i=>i.GetInventory(0).MaxVolume.RawValue)) {                
                if (i.GetInventory(0).MaxVolume.RawValue > 1000000) {
                    currentTotal += i.GetInventory(0).CurrentVolume.RawValue;
                    maxTotal += i.GetInventory(0).MaxVolume.RawValue;

                    txtFinal += $"{idx++.ToString().PadLeft(2)} {MontaIndicator(0,1000,18,Convert.ToInt32(i.GetInventory(0).CurrentVolume.RawValue * 1000 / i.GetInventory(0).MaxVolume.RawValue))} {Convert.ToInt32(i.GetInventory(0).CurrentVolume.RawValue / 1000000m)}/{Convert.ToInt32(i.GetInventory(0).MaxVolume.RawValue / 1000000m)}\n";



                }
            }
            
            txtFinal += $"\n\n-- Total: {currentTotal / 1000000m:N1} / {maxTotal / 1000000m:N1} ({(Convert.ToDouble(currentTotal) * 100 / maxTotal):N0}%)\n{MontaIndicator(0,1000,28,Convert.ToInt32(currentTotal*1000 / maxTotal))}";

            PANEL_CARGO.WriteText(txtFinal);


        }

      void UpdateBatteryPanel() {
            var totalCur = 0f;
            var totalMax = 0f;
           

           
            batteryBlocks = batteryBlocks.OrderBy(b => b.CustomName).ToList();
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


       

       

       


        void PopulaDictionaries() {
            //DesiredItems.Add("PowerCell", 120);
            //DesiredItems.Add("Computer", 300);
            //DesiredItems.Add("Display", 120);
            //DesiredItems.Add("Motor", 200);
            //DesiredItems.Add("Construction", 400);
            //DesiredItems.Add("MetalGrid", 80);
            //DesiredItems.Add("InteriorPlate", 500);
            //DesiredItems.Add("SteelPlate", 2000);
            //DesiredItems.Add("SmallTube", 250);
            //DesiredItems.Add("LargeTube", 140);
            //DesiredItems.Add("BulletproofGlass", 50);
            //DesiredItems.Add("Reactor", 60);
            //DesiredItems.Add("Thrust", 120);
            //DesiredItems.Add("RadioCommunication", 80);
            //DesiredItems.Add("Detector", 80);
            //DesiredItems.Add("SolarCell", 10);
            //DesiredItems.Add("Girder", 20);
            //DesiredItems.Add("SuperConductor", 50);

            ItemsToBluePrint.Add("PowerCell", "MyObjectBuilder_BlueprintDefinition/PowerCell");
            ItemsToBluePrint.Add("Computer", "MyObjectBuilder_BlueprintDefinition/ComputerComponent");
            ItemsToBluePrint.Add("Display", "MyObjectBuilder_BlueprintDefinition/Display");
            ItemsToBluePrint.Add("Motor", "MyObjectBuilder_BlueprintDefinition/MotorComponent");
            ItemsToBluePrint.Add("Construction", "MyObjectBuilder_BlueprintDefinition/ConstructionComponent");
            ItemsToBluePrint.Add("MetalGrid", "MyObjectBuilder_BlueprintDefinition/MetalGrid");
            ItemsToBluePrint.Add("InteriorPlate", "MyObjectBuilder_BlueprintDefinition/InteriorPlate");
            ItemsToBluePrint.Add("SteelPlate", "MyObjectBuilder_BlueprintDefinition/SteelPlate");
            ItemsToBluePrint.Add("SmallTube", "MyObjectBuilder_BlueprintDefinition/SmallTube");
            ItemsToBluePrint.Add("LargeTube", "MyObjectBuilder_BlueprintDefinition/LargeTube");
            ItemsToBluePrint.Add("BulletproofGlass", "MyObjectBuilder_BlueprintDefinition/BulletproofGlass");
            ItemsToBluePrint.Add("Reactor", "MyObjectBuilder_BlueprintDefinition/ReactorComponent");
            ItemsToBluePrint.Add("Thrust", "MyObjectBuilder_BlueprintDefinition/ThrustComponent");
            ItemsToBluePrint.Add("RadioCommunication", "MyObjectBuilder_BlueprintDefinition/RadioCommunicationComponent");
            ItemsToBluePrint.Add("Detector", "MyObjectBuilder_BlueprintDefinition/DetectorComponent");
            ItemsToBluePrint.Add("SolarCell", "MyObjectBuilder_BlueprintDefinition/SolarCell");
            ItemsToBluePrint.Add("SuperConductor", "MyObjectBuilder_BlueprintDefinition/Superconductor");
            ItemsToBluePrint.Add("Girder", "MyObjectBuilder_BlueprintDefinition/GirderComponent");


            //MinimumItems.Add("Computer", 20);
            //MinimumItems.Add("Motor", 20);
            //MinimumItems.Add("Display", 20);
            //MinimumItems.Add("Construction", 20);
            //MinimumItems.Add("MetalGrid", 5);
            //MinimumItems.Add("InteriorPlate", 20);
            //MinimumItems.Add("SteelPlate", 40);
            //MinimumItems.Add("SmallTube", 20);
            //MinimumItems.Add("LargeTube", 10);
            //MinimumItems.Add("BulletproofGlass", 3);
            //MinimumItems.Add("Reactor", 5);
            //MinimumItems.Add("Thrust", 5);
            //MinimumItems.Add("RadioCommunication", 0);
            //MinimumItems.Add("Detector", 1);
            //MinimumItems.Add("SolarCell", 1);
            //MinimumItems.Add("PowerCell", 1);
        }

        void Transfer(IMyInventory a, IMyInventory b, string sType, float amount) {
            var items = new List<MyInventoryItem>();
            a.GetItems(items);

            float left = amount;
            for (int i = items.Count - 1; i >= 0; i--) {
                if (left > 0 && items[i].Type.SubtypeId.ToString() == sType) {
                    if ((float)items[i].Amount > left) {
                        // transfer remaining and break
                        a.TransferItemTo(b, i, null, true, (VRage.MyFixedPoint)amount);
                        left = 0;
                        break;
                    } else {
                        left -= (float)items[i].Amount;
                        // transfer all
                        a.TransferItemTo(b, i, null, true, null);
                    }
                }
            }
        }

    }
}
