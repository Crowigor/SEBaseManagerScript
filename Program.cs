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
using static VRageMath.Base6Directions;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        readonly Dictionary<string, string> ConfigsSections = new Dictionary<string, string>()
        {
            { "Global Config", "CBM:GC"},
            { "Inventory Manager", "CBM:IM"},
            { "Items Assembling", "CBM:IA"},
            { "Items Disassembling", "CBM:ID"},
            { "Items Ejecting", "CBM:IE"},
            { "Stop Drones", "CBM:SD"},
            { "Debug Display", "CBM:DD"},
        };

        TasksList Tasks;
        ItemsList Items;
        BlocksHelper Blocks;
        DebugHelper Debug;
        Config GlobalConfig;

        public Program()
        {
            // Create Tasks
            Tasks = new TasksList();
            Tasks.CreateTask("Initialization", TaskInitialization, 20, false);
            Tasks.CreateTask("Inventory Manager", TaskInventoryManager, 10);
            Tasks.CreateTask("Assemblers Cleanup", TaskAssemblersCleanup, 10);
            Tasks.CreateTask("Items Assembling", TaskItemsAssembling, 10);
            Tasks.CreateTask("Items Disassembling", TaskItemsDisassembling, 10);
            Tasks.CreateTask("Stop Drones", TaskStopDrones, 10);
            Tasks.CreateTask("Reset Items Amout", TaskResetItemsAmout, 5);

            // Create debug
            Debug = new DebugHelper();

            // Create items
            Items = new ItemsList();

            TaskInitialization();
            ConfigureMeDisplay();

            // Set update rate
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == null || argument == "")
            {
                Tasks.RunTasks();
            }

            DisplayStatus();
        }

        private void ConfigureMeDisplay()
        {
            // Set display
            IMyTextSurface display = Me.GetSurface(0);
            display.ContentType = ContentType.TEXT_AND_IMAGE;
            display.FontColor = new Color(255, 180, 0);
            display.FontSize = (float)0.9;
            display.TextPadding = (float)12.5;
            display.Alignment = TextAlignment.CENTER;
            display.WriteText("Crowigor's Base Manager");
            display.ClearImagesFromSelection();
            display.AddImageToSelection("LCD_Economy_Graph_2");
        }

        private void DisplayStatus()
        {
            List<string> displayData = new List<string>()
            {
                {"= Crowigor's Base Manager ="},
            };
            displayData.AddRange(Tasks.GetStatus());

            if (Debug.Info.Count > 0)
            {
                displayData.Add("\n == Debug Info ==");
                displayData.AddRange(Debug.Info);
            }

            if (Debug.Warning.Count > 0)
            {
                displayData.Add("\n == Warnings ==");
                displayData.AddRange(Debug.Warning);
            }

            string print = String.Join("\n", displayData.ToArray());
            Echo(print);

            foreach (IMyTerminalBlock block in Blocks.GetBlocks("Debug Display"))
            {
                IMyTextPanel display = (IMyTextPanel)block;
                display.ContentType = ContentType.TEXT_AND_IMAGE;
                display.FontColor = new Color(255, 180, 0);
                display.WriteText(print);
            }
        }

        private void TaskInitialization()
        {
            CheckGlobalConfig();
            Debug.Clear();

            Blocks = new BlocksHelper(GridTerminalSystem, GlobalConfig.Get("Tag"), GlobalConfig.Get("Ignore"));
            Debug.Merge(Blocks.Debug);

            Items.ResetData();
            Items.Blocks = Blocks;
            Debug.Merge(Items.Debug);

            foreach (IMyTerminalBlock block in Blocks.GetBlocks("Gas Tanks"))
            {
                string configSection = ConfigsSections["Inventory Manager"];
                if (block.CustomData.Contains(configSection))
                {
                    Config config = Config.Parse(configSection, block.CustomData);
                    foreach (KeyValuePair<string, string> entry in config.Data)
                    {
                        Item item = Items.GetItem(entry.Key);
                        if (item != null)
                        {
                            item.Inventories.Add(block.GetInventory(0));
                        }
                    }
                }
            }

            foreach (IMyTerminalBlock block in Blocks.GetBlocks("Containers"))
            {
                string configSection = ConfigsSections["Inventory Manager"];
                if (block.CustomData.Contains(configSection))
                {
                    Config config = Config.Parse(configSection, block.CustomData);
                    foreach (KeyValuePair<string, string> entry in config.Data)
                    {
                        Item item = Items.GetItem(entry.Key);
                        if (item != null)
                        {
                            item.Inventories.Add(block.GetInventory(0));
                        }
                    }
                }
            }

            foreach (IMyTerminalBlock block in Blocks.GetBlocks("Assemblers"))
            {
                if (block.CustomData.Contains(ConfigsSections["Items Assembling"]))
                {
                    Blocks.AddBlock("Items Assembling", block);
                }
                else if (block.CustomData.Contains(ConfigsSections["Items Disassembling"]))
                {
                    Blocks.AddBlock("Items Disassembling", block);
                }
            }

            foreach (IMyTerminalBlock block in Blocks.GetBlocks("Connectors"))
            {
                if (block.CustomData.Contains(ConfigsSections["Stop Drones"]))
                {
                    Blocks.AddBlock("Stop Drones", block);
                }
            }

            foreach (IMyTerminalBlock block in Blocks.GetBlocks("Displays"))
            {
                if (block.CustomData.Contains(ConfigsSections["Debug Display"]))
                {
                    Blocks.AddBlock("Debug Display", block);
                }
            }
        }

        private void TaskInventoryManager()
        {
            List<IMyInventory> inventories = new List<IMyInventory>();

            foreach (IMyTerminalBlock block in Blocks.GetBlocks("Entitries"))
            {
                if (!block.IsFunctional)
                {
                    continue;
                }
                else if (block is IMyLargeConveyorTurretBase)
                {
                    continue;
                }
                else if (block is IMyGasGenerator)
                {
                    IMyInventory inventory = block.GetInventory(0);
                    List<MyInventoryItem> items = new List<MyInventoryItem>();
                    inventory.GetItems(items);
                    foreach (MyInventoryItem item in items)
                    {
                        Item find = Items.GetItem(item.Type.ToString());
                        if (find != null && find.Selector != "Ore/Ice")
                        {
                            find.Transfer(item, inventory);
                        }
                    }
                }
                else if (block is IMyAssembler)
                {
                    inventories.Add(block.GetInventory(1));
                }
                else if (block is IMyRefinery)
                {
                    inventories.Add(block.GetInventory(1));
                }
                else if (block is IMyEntity)
                {
                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        inventories.Add(block.GetInventory(i));
                    }
                }
            }
            Items.TrasferFromInventories(inventories);
        }

        private void TaskAssemblersCleanup()
        {
            foreach (IMyAssembler assembler in Blocks.GetBlocks("Assemblers"))
            {
                if (!assembler.IsFunctional)
                {
                    continue;
                }
                Items.TrasferFromInventory(assembler.GetInventory(0));
            }
        }

        private void TaskItemsAssembling()
        {
            foreach (IMyAssembler assembler in Blocks.GetBlocks("Items Assembling"))
            {
                if (!assembler.IsWorking || assembler.Mode != MyAssemblerMode.Assembly || assembler.CooperativeMode)
                {
                    continue;
                }
                Config config = Config.Parse(ConfigsSections["Items Assembling"], assembler.CustomData);
                foreach (KeyValuePair<string, string> entry in config.Data)
                {
                    Item item = Items.GetItem(entry.Key, true);
                    if (item != null && item.Blueprints.Count > 0)
                    {
                        foreach (KeyValuePair<MyDefinitionId, MyFixedPoint> blueprint in item.Blueprints)
                        {
                            if (!assembler.CanUseBlueprint(blueprint.Key))
                            {
                                continue;
                            }

                            MyFixedPoint need = MyFixedPoint.DeserializeString(entry.Value);
                            MyFixedPoint total = item.Amout["exist"] + item.Amout["assembling"];
                            if (total < need)
                            {
                                MyFixedPoint add = need - total;
                                int calc = (int)Math.Ceiling((float)add / (float)blueprint.Value);
                                MyFixedPoint queue = MyFixedPoint.DeserializeString(calc.ToString());

                                assembler.Repeating = false;
                                assembler.AddQueueItem(blueprint.Key, queue);

                                item.Amout["assembling"] += queue * blueprint.Value;
                            }

                            break;
                        }
                    }
                }
            }
        }

        private void TaskItemsDisassembling()
        {
            foreach (IMyAssembler assembler in Blocks.GetBlocks("Items Disassembling"))
            {
                if (!assembler.IsWorking || assembler.Mode != MyAssemblerMode.Disassembly || assembler.CooperativeMode)
                {
                    continue;
                }

                Config config = Config.Parse(ConfigsSections["Items Disassembling"], assembler.CustomData);
                foreach (KeyValuePair<string, string> entry in config.Data)
                {
                    Item item = Items.GetItem(entry.Key, true);
                    if (item != null && item.Blueprints.Count > 0)
                    {
                        foreach (KeyValuePair<MyDefinitionId, MyFixedPoint> blueprint in item.Blueprints)
                        {
                            if (!assembler.CanUseBlueprint(blueprint.Key))
                            {
                                continue;
                            }

                            string value = entry.Value;
                            if (value == null)
                            {
                                value = "0";
                            }
                            MyFixedPoint need = MyFixedPoint.DeserializeString(value);
                            MyFixedPoint total = item.Amout["exist"] - item.Amout["disassembling"];
                            if (total > need)
                            {
                                MyFixedPoint add = total - need;
                                int calc = (int)Math.Round((float)add / (float)blueprint.Value, 0);
                                MyFixedPoint queue = MyFixedPoint.DeserializeString(calc.ToString());

                                assembler.Repeating = false;
                                assembler.AddQueueItem(blueprint.Key, queue);

                                item.Amout["disassembling"] += queue * blueprint.Value;
                            }

                            break;
                        }
                    }
                }

                if (!assembler.IsQueueEmpty)
                {
                    List<MyProductionItem> queue = new List<MyProductionItem>();
                    assembler.GetQueue(queue);
                    foreach (MyProductionItem queueItem in queue)
                    {
                        Item item = Items.GetItem(queueItem.BlueprintId.ToString());
                        if (item != null && item.Blueprints.Count > 0)
                        {
                            foreach (KeyValuePair<MyDefinitionId, MyFixedPoint> blueprint in item.Blueprints)
                            {
                                if (!assembler.CanUseBlueprint(blueprint.Key))
                                {
                                    continue;
                                }
                                MyFixedPoint quantity = queueItem.Amount * blueprint.Value;

                                TransferHelper.TransferFromBlocks(item.Type, Blocks.GetBlocks("Entitries"), assembler.GetInventory(1), quantity);
                            }
                        }
                    }
                }
            }
        }

        private void TaskStopDrones()
        {
            // Stop Drones
            foreach (IMyShipConnector connector in Blocks.GetBlocks("Stop Drones"))
            {
                Config config = Config.Parse(ConfigsSections["Stop Drones"], connector.CustomData);

                string dronBlocksName = config.Get("DronBlocksName");
                string baseContainersName = config.Get("BaseContainersName");

                List<string> errors = new List<string>();
                bool hasError = false;

                if (dronBlocksName == null)
                {
                    errors.Add("Empty DronBlocksName");
                    hasError = true; ;
                }
                if (baseContainersName == null)
                {
                    errors.Add("Empty BaseContainersName");
                    hasError = true;
                }

                if (hasError)
                {
                    string errorMessage = "Task: Stop Drones\n" + "Conector: " + connector.CustomName + "\n" + "Message:";
                    foreach (string text in errors)
                    {
                        errorMessage += text + "\n";
                    }
                    Debug.AddWarning(errorMessage);

                    continue;
                }

                if (connector.Status != MyShipConnectorStatus.Connected)
                {
                    continue;
                }

                IMyShipConnector otherConnector = connector.OtherConnector;
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, block => block.CubeGrid == otherConnector.CubeGrid && block.CustomName.Contains(dronBlocksName));
                if (blocks.Count == 0)
                {
                    Debug.AddInfo("Stop Drones: Can't find drons blocks " + dronBlocksName);
                    continue;
                }

                bool blockActive = true;
                if (baseContainersName != null)
                {
                    float current = 0;
                    float total = 0;
                    foreach (IMyCargoContainer container in Blocks.GetBlocks("Containers"))
                    {
                        if (container.CustomName.Contains(baseContainersName))
                        {
                            IMyInventory inventory = container.GetInventory(0);
                            current += (float)inventory.CurrentVolume;
                            total += (float)inventory.MaxVolume;
                        }
                    }

                    string maxConfig = config.Get("BaseContainersMaxVolume", GlobalConfig.Get("SD:BaseContainersMaxVolume", "90%"));
                    bool percent = maxConfig.Contains("%");
                    float max = (float)MyFixedPoint.DeserializeString(maxConfig.Replace("%", ""));
                    if (percent)
                    {
                        float calc = current / total * 100;
                        if (calc >= max)
                        {
                            blockActive = false;
                        }
                    }
                    else if (current >= total)
                    {
                        blockActive = false;
                    }
                }

                foreach (IMyTerminalBlock block in blocks)
                {
                    if (blockActive)
                    {
                        block.ApplyAction("OnOff_On");
                    }
                    else
                    {
                        block.ApplyAction("OnOff_Off");
                    }
                }
            }
        }

        private void TaskResetItemsAmout()
        {
            Items.ClearAmouts();
        }

        private void CheckGlobalConfig()
        {
            string section = ConfigsSections["Global Config"];
            Config configDefault = new Config(section, new Dictionary<string, string>()
            {
                {"Tag", "[Base]" },
                {"Ignore", "[!CBM]" },
                {"SD:BaseContainersMaxVolume", "90%" },
            });
            Config configCurrent = Config.Parse(section, Me.CustomData);
            Config configNew = Config.Merge(section, new List<Config> { configDefault, configCurrent });

            List<string> customData = new List<string>()
            {
                ";Crowigor's Base Manager",
                 "[" + ConfigsSections["Global Config"] + "]",
            };

            Debug.Merge(configNew.Debug);
            customData.AddRange(configNew.ToList());

            GlobalConfig = configNew;
            Me.CustomData = String.Join("\n", customData.ToArray());
        }
    }
}
