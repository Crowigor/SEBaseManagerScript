using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using static IngameScript.Program;

namespace IngameScript
{
    partial class Program
    {
        public class Item
        {
            public string Selector { get; set; }
            public string Name { get; set; }
            public string Localization { get; set; }
            public MyItemType Type { get; set; }
            public Dictionary<MyDefinitionId, MyFixedPoint> Blueprints { get; set; }
            public List<IMyInventory> Inventories { get; set; }
            public Dictionary<string, MyFixedPoint> Amout { get; set; }
            public bool IsNewAmout { get; set; }
            public DebugHelper Debug { get; set; }

            public Item(string name, string localization, string type, Dictionary<string, string> blueprints = null)
            {
                Selector = type.Replace("MyObjectBuilder_", ""); ;
                Name = name;
                Localization = localization;
                Type = MyItemType.Parse(type);


                Blueprints = new Dictionary<MyDefinitionId, MyFixedPoint>();
                if (blueprints != null)
                {
                    foreach (KeyValuePair<string, string> entry in blueprints)
                    {
                        Blueprints[MyDefinitionId.Parse(entry.Key)] = MyFixedPoint.DeserializeString(entry.Value);

                    }
                }

                Debug = new DebugHelper();
                ResetData();
            }

            public bool IsCrafteble()
            {
                return (Blueprints.Count > 0);
            }

            public void ResetData()
            {
                ClearAmout();
                ClearInventories();
                Debug.Clear();
            }

            public void ClearAmout()
            {
                IsNewAmout = true;
                Amout = new Dictionary<string, MyFixedPoint>()
                {
                    {"exist", MyFixedPoint.Zero},
                    {"assembling", MyFixedPoint.Zero},
                    {"disassembling", MyFixedPoint.Zero},
                };
            }

            public void ClearInventories()
            {
                Inventories = new List<IMyInventory>();
            }

            public MyFixedPoint Transfer(MyInventoryItem inventoryItem, IMyInventory sourceInventory, MyFixedPoint amount = new MyFixedPoint())
            {
                if (Inventories.Count == 0)
                {
                    return amount;
                }

                MyFixedPoint zero = MyFixedPoint.Zero;

                return TransferHelper.TransferToInventories(inventoryItem, sourceInventory, Inventories, amount);
            }


            public void CalculateAmout(BlocksHelper blocks)
            {
                MyFixedPoint exist = MyFixedPoint.Zero;
                MyFixedPoint assembling = MyFixedPoint.Zero;
                MyFixedPoint disassembling = MyFixedPoint.Zero;
                foreach (IMyCargoContainer block in blocks.GetBlocks("Containers"))
                {
                    if (!block.IsFunctional)
                    {
                        continue;
                    }
                    exist += block.GetInventory(0).GetItemAmount(Type);
                }
                foreach (IMyRefinery block in blocks.GetBlocks("Refineries"))
                {
                    if (!block.IsFunctional)
                    {
                        continue;
                    }
                    exist += block.GetInventory(0).GetItemAmount(Type);
                    exist += block.GetInventory(1).GetItemAmount(Type);
                }
                foreach (IMyAssembler block in blocks.GetBlocks("Assemblers"))
                {
                    if (!block.IsFunctional)
                    {
                        continue;
                    }
                    exist += block.GetInventory(0).GetItemAmount(Type);
                    exist += block.GetInventory(1).GetItemAmount(Type);
                    if (Blueprints.Count > 0)
                    {
                        IMyAssembler assembler = (IMyAssembler)block;
                        foreach (KeyValuePair<MyDefinitionId, MyFixedPoint> entry in Blueprints)
                        {
                            MyDefinitionId blueprint = entry.Key;
                            MyFixedPoint assemblingRate = entry.Value;
                            if (!assembler.IsQueueEmpty && assembler.CanUseBlueprint(blueprint))
                            {
                                List<MyProductionItem> queue = new List<MyProductionItem>();
                                assembler.GetQueue(queue);
                                foreach (MyProductionItem item in queue)
                                {
                                    if (item.BlueprintId == blueprint)
                                    {
                                        MyFixedPoint add = item.Amount * assemblingRate;
                                        if (assembler.Mode == MyAssemblerMode.Assembly)
                                        {
                                            assembling += add;
                                        }
                                        else if (assembler.Mode == MyAssemblerMode.Disassembly)
                                        {
                                            disassembling += add;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                foreach (IMyGasGenerator block in blocks.GetBlocks("Gas Generators"))
                {
                    if (!block.IsFunctional)
                    {
                        continue;
                    }
                    exist += block.GetInventory(0).GetItemAmount(Type);
                }
                foreach (IMyShipConnector block in blocks.GetBlocks("Connectors"))
                {
                    if (!block.IsFunctional)
                    {
                        continue;
                    }
                    exist += block.GetInventory(0).GetItemAmount(Type);
                }
                foreach (IMyGasTank block in blocks.GetBlocks("Gas Tanks"))
                {
                    if (!block.IsFunctional)
                    {
                        continue;
                    }
                    exist += block.GetInventory(0).GetItemAmount(Type);
                }
                foreach (IMyLargeConveyorTurretBase block in blocks.GetBlocks("Turrets"))
                {
                    if (!block.IsFunctional)
                    {
                        continue;
                    }
                    exist += block.GetInventory(0).GetItemAmount(Type);
                }

                IsNewAmout = false;
                Amout = new Dictionary<string, MyFixedPoint>()
                {
                    {"exist", exist},
                    {"assembling", assembling},
                    {"disassembling", disassembling},
                };
            }
        }
    }
}
