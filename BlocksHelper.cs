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

namespace IngameScript
{
    partial class Program
    {
        public class BlocksHelper
        {
            Dictionary<string, List<IMyTerminalBlock>> Storage { get; set; }
            public DebugHelper Debug { get; set; }

            public BlocksHelper(IMyGridTerminalSystem grid, string tag, string ignore)
            {
                Storage = new Dictionary<string, List<IMyTerminalBlock>>();
                Debug = new DebugHelper();

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                grid.SearchBlocksOfName(tag, blocks);
                blocks.Sort((a, b) => string.Compare(a.CustomName, b.CustomName));

                foreach (IMyTerminalBlock block in blocks)
                {
                    if (!block.IsFunctional || block.CustomName.Contains(ignore) || block.CustomData.Contains(ignore))
                    {
                        continue;
                    }

                    AddBlock("All", block);

                    if (block is IMyEntity)
                    {
                        AddBlock("Entitries", block);
                    }

                    if (block is IMyCargoContainer)
                    {
                        AddBlock("Containers", block);
                    }

                    if (block is IMyRefinery)
                    {
                        AddBlock("Refineries", block);
                    }

                    if (block is IMyAssembler)
                    {
                        AddBlock("Assemblers", block);
                    }

                    if (block is IMyConveyorSorter)
                    {
                        AddBlock("Sorters", block);
                    }

                    if (block is IMyShipConnector)
                    {
                        AddBlock("Connectors", block);
                    }

                    if (block is IMyGasGenerator)
                    {
                        AddBlock("Gas Generators", block);
                    }

                    if (block is IMyGasTank)
                    {
                        AddBlock("Gas Tanks", block);
                    }

                    if (block is IMyCryoChamber)
                    {
                        AddBlock("Cryo Chambers", block);
                    }

                    if (block is IMyTextPanel)
                    {
                        AddBlock("Displays", block);
                    }

                    if (block is IMyShipDrill)
                    {
                        AddBlock("Drills", block);
                    }

                    if (block is IMyShipGrinder)
                    {
                        AddBlock("Grinders", block);
                    }

                    if (block is IMyShipWelder)
                    {
                        string blocksType = "Welders";
                        List<ITerminalProperty> properties = new List<ITerminalProperty>();
                        block.GetProperties(properties);
                        foreach (ITerminalProperty property in properties)
                        {
                            if (property.Id.Contains("BuildAndRepair"))
                            {
                                blocksType = "BuildAndRepair";
                                break;
                            }
                        }

                        AddBlock(blocksType, block);
                    }

                    if (block is IMyPistonBase)
                    {
                        AddBlock("Pistons", block);
                    }


                    if (block is IMyLargeConveyorTurretBase)
                    {
                        AddBlock("Turrets", block);
                    }
                }
            }

            public void AddBlock(string type, IMyTerminalBlock block)
            {
                if (!Storage.ContainsKey(type))
                {
                    Storage[type] = new List<IMyTerminalBlock>();
                }
                Storage[type].Add(block);
            }

            public List<IMyTerminalBlock> GetBlocks(string type)
            {
                return (Storage.ContainsKey(type)) ? Storage[type] : new List<IMyTerminalBlock>();
            }
        }
    }
}
