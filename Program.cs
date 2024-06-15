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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region Fields and Properties
        public static class ConfigsSections
        {
            public const string GlobalConfig = "CBM:GC";
            public const string DisplayConfig = "CBM:DC";
            public const string InventoryManager = "CBM:IM";
            public const string ItemsAssembling = "CBM:IA";
            public const string ItemsDisassembling = "CBM:ID";
        }

        TasksManager Tasks;
        BlocksManager Blocks;
        ItemsManager Items;
        ConfigObject GlobalConfig;

        #endregion;

        #region Ingame methods
        public Program()
        {
            // Add Tasks
            Tasks = new TasksManager();
            Tasks.Add(TasksManager.InitializationTaskName, TaskInitialization, 20, false);
            Tasks.Add(TasksManager.CalculationTaskName, TaskCalculation, 3);
            Tasks.Add("Inventory Manager", TaskInventoryManager, 10);
            Tasks.Add("Assemblers Manager", TaskAssemblersManager, 5, true, true);
            Tasks.Add("Display Status", TaskDisplayStatus, 1, false);

            // Run Initialization
            TaskInitialization();

            // Set update rate
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == null || argument == "")
                Tasks.Run();

        }

        #endregion

        #region Tasks
        private void TaskInitialization()
        {
            ConfigureMeDisplay();
            CheckGlobalConfig();
            Blocks = new BlocksManager(GridTerminalSystem, GlobalConfig.Get("Tag"), GlobalConfig.Get("Ignore"));

            if (Items == null)
                Items = new ItemsManager();
            else
                Items.ClearInventories();

            // Add invetories to items
            foreach (IMyTerminalBlock block in Blocks.GetBlocks(BlocksManager.BlockType.GasTanks))
            {
                string configSection = ConfigsSections.InventoryManager;
                if (block.CustomData.Contains(configSection))
                {
                    ConfigObject config = ConfigObject.Parse(configSection, block.CustomData);
                    foreach (KeyValuePair<string, string> entry in config.Data)
                    {
                        ItemObject item = Items.GetItem(entry.Key);
                        if (item != null)
                        {
                            item.Inventories.Add(block.GetInventory(0));
                        }
                    }
                }
            }
            foreach (IMyTerminalBlock block in Blocks.GetBlocks(BlocksManager.BlockType.Containers))
            {
                string configSection = ConfigsSections.InventoryManager;
                if (block.CustomData.Contains(configSection))
                {
                    ConfigObject config = ConfigObject.Parse(configSection, block.CustomData);
                    foreach (KeyValuePair<string, string> entry in config.Data)
                    {
                        ItemObject item = Items.GetItem(entry.Key);
                        if (item != null)
                        {
                            item.Inventories.Add(block.GetInventory(0));
                        }
                    }
                }
            }
        }

        private void TaskCalculation()
        {
            // Calcualte Itmes Amounts
            Items.ClearAmounts();
            foreach (IMyTerminalBlock block in Blocks.GetBlocks())
            {
                if (!block.IsFunctional)
                {
                    continue;
                }
                if (block.InventoryCount > 0)
                {
                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        IMyInventory inventory = block.GetInventory(i);
                        if (inventory == null)
                            continue;

                        List<MyInventoryItem> items = new List<MyInventoryItem>();
                        inventory.GetItems(items);
                        if (items.Count > 0)
                        {
                            foreach (MyInventoryItem item in items)
                            {
                                ItemObject find = Items.GetItem(item.Type.ToString());
                                if (find != null)
                                {
                                    find.Amounts.Exist += item.Amount;
                                    Items.UpdateItem(find);
                                }
                            }
                        }
                    }
                }

                if (block is IMyAssembler)
                {
                    IMyAssembler assembler = (IMyAssembler)block;

                    if (assembler.IsWorking)
                    {

                        if (!assembler.IsQueueEmpty)
                        {
                            List<MyProductionItem> queue = new List<MyProductionItem>();
                            assembler.GetQueue(queue);
                            foreach (MyProductionItem item in queue)
                            {
                                ItemObject find = Items.GetItem(item.BlueprintId.ToString());
                                if (find != null)
                                {
                                    MyFixedPoint amount = item.Amount * find.Blueprints[item.BlueprintId];
                                    if (assembler.Mode == MyAssemblerMode.Assembly)
                                        find.Amounts.Assembling += amount;
                                    else if (assembler.Mode == MyAssemblerMode.Disassembly)
                                        find.Amounts.Disassembling += amount;
                                }
                            }
                        }
                        if (!assembler.CooperativeMode)
                        {
                            if (assembler.Mode == MyAssemblerMode.Assembly && block.CustomData.Contains(ConfigsSections.ItemsAssembling))
                            {
                                ConfigObject config = ConfigObject.Parse(ConfigsSections.ItemsAssembling, block.CustomData);
                                if (config != null)
                                {
                                    foreach (KeyValuePair<string, string> entry in config.Data)
                                    {
                                        ItemObject find = Items.GetItem(entry.Key);
                                        if (find != null)
                                        {
                                            if (find.Amounts.AssemblingQuota < 0)
                                            {
                                                find.Amounts.AssemblingQuota = 0;
                                            }
                                            find.Amounts.AssemblingQuota += MyFixedPoint.DeserializeString(entry.Value);
                                        }
                                    }
                                }
                            }
                            else if (assembler.Mode == MyAssemblerMode.Disassembly && block.CustomData.Contains(ConfigsSections.ItemsDisassembling))
                            {
                                ConfigObject config = ConfigObject.Parse(ConfigsSections.ItemsDisassembling, block.CustomData);
                                if (config != null)
                                {
                                    foreach (KeyValuePair<string, string> entry in config.Data)
                                    {
                                        ItemObject find = Items.GetItem(entry.Key);
                                        if (find != null)
                                        {
                                            if (find.Amounts.DisassemblingQuota < 0)
                                            {
                                                find.Amounts.DisassemblingQuota = 0;
                                            }
                                            find.Amounts.DisassemblingQuota += MyFixedPoint.DeserializeString(entry.Value);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (ItemObject item in Items.GetList())
            {
                item.Amounts.IsNew = false;
                Items.UpdateItem(item);
            }
        }

        private void TaskInventoryManager()
        {
            List<IMyInventory> inventories = new List<IMyInventory>();

            foreach (IMyTerminalBlock block in Blocks.GetBlocks())
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
                        ItemObject find = Items.GetItem(item.Type.ToString());
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

        private void TaskAssemblersManager()
        {
            foreach (IMyAssembler assembler in Blocks.GetBlocks(BlocksManager.BlockType.Assemblers))
            {
                if (!assembler.IsFunctional)
                    continue;

                // Cleanup source invetory
                Items.TrasferFromInventory(assembler.GetInventory(0));

                if (assembler.IsWorking)
                {
                    // Add items to quota
                    if (!assembler.CooperativeMode)
                    {
                        if (assembler.Mode == MyAssemblerMode.Assembly)
                        {
                            // Items Assembling
                            ConfigObject config = ConfigObject.Parse(ConfigsSections.ItemsAssembling, assembler.CustomData);
                            if (config != null)
                            {
                                foreach (KeyValuePair<string, string> entry in config.Data)
                                {
                                    ItemObject item = Items.GetItem(entry.Key);
                                    if (item != null && item.Blueprints.Count > 0)
                                    {
                                        foreach (KeyValuePair<MyDefinitionId, MyFixedPoint> blueprint in item.Blueprints)
                                        {
                                            if (!assembler.CanUseBlueprint(blueprint.Key))
                                                continue;

                                            MyFixedPoint need = MyFixedPoint.DeserializeString(entry.Value);
                                            MyFixedPoint total = item.Amounts.Exist + item.Amounts.Assembling;
                                            if (total < need)
                                            {
                                                MyFixedPoint add = need - total;
                                                int calc = (int)Math.Ceiling((float)add / (float)blueprint.Value);
                                                MyFixedPoint queue = MyFixedPoint.DeserializeString(calc.ToString());

                                                assembler.Repeating = false;
                                                assembler.AddQueueItem(blueprint.Key, queue);

                                                item.Amounts.Assembling += queue * blueprint.Value;
                                                Items.UpdateItem(item);
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else if (assembler.Mode == MyAssemblerMode.Disassembly)
                        {
                            // Items Disassembling
                            ConfigObject config = ConfigObject.Parse(ConfigsSections.ItemsDisassembling, assembler.CustomData);
                            if (config != null)
                            {
                                foreach (KeyValuePair<string, string> entry in config.Data)
                                {
                                    ItemObject item = Items.GetItem(entry.Key);
                                    if (item != null && item.Blueprints.Count > 0)
                                    {
                                        foreach (KeyValuePair<MyDefinitionId, MyFixedPoint> blueprint in item.Blueprints)
                                        {
                                            if (!assembler.CanUseBlueprint(blueprint.Key))
                                                continue;

                                            string value = entry.Value;
                                            if (value == null)
                                                value = "0";

                                            MyFixedPoint need = MyFixedPoint.DeserializeString(value);
                                            MyFixedPoint total = item.Amounts.Exist - item.Amounts.Disassembling;
                                            if (total > need)
                                            {
                                                MyFixedPoint add = total - need;
                                                int calc = (int)Math.Round((float)add / (float)blueprint.Value, 0);
                                                MyFixedPoint queue = MyFixedPoint.DeserializeString(calc.ToString());

                                                assembler.Repeating = false;
                                                assembler.AddQueueItem(blueprint.Key, queue);

                                                item.Amounts.Disassembling += queue * blueprint.Value;
                                                Items.UpdateItem(item);
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Move items for disassembly
                    if (assembler.Mode == MyAssemblerMode.Disassembly)
                    {
                        List<MyProductionItem> queue = new List<MyProductionItem>();
                        assembler.GetQueue(queue);
                        foreach (MyProductionItem queueItem in queue)
                        {
                            ItemObject item = Items.GetItem(queueItem.BlueprintId.ToString());
                            if (item != null && item.Blueprints.Count > 0)
                            {
                                foreach (KeyValuePair<MyDefinitionId, MyFixedPoint> blueprint in item.Blueprints)
                                {
                                    if (!assembler.CanUseBlueprint(blueprint.Key))
                                        continue;

                                    MyFixedPoint quantity = queueItem.Amount * blueprint.Value;
                                    InventoryHelper.TransferFromBlocks(item.Type, Blocks.GetBlocks(), assembler.GetInventory(1), quantity);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void TaskDisplayStatus()
        {
            List<string> displayData = new List<string>()
            {
                {"= Crowigor's Base Manager ="},
            };
            displayData.AddRange(Tasks.GetStatusText());

            Echo(String.Join("\n", displayData.ToArray()));
        }
        #endregion

        #region Script Methods
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

        private void CheckGlobalConfig()
        {
            string section = ConfigsSections.GlobalConfig;
            ConfigObject configDefault = new ConfigObject(section, new Dictionary<string, string>()
            {
                {"Tag", "[Base]" },
                {"Ignore", "[!CBM]" },
                {"SD:BaseContainersMaxVolume", "90%" },
            });
            ConfigObject configCurrent = ConfigObject.Parse(section, Me.CustomData);
            ConfigObject configNew = ConfigObject.Merge(section, new List<ConfigObject> { configDefault, configCurrent });

            List<string> customData = new List<string>()
            {
                ";Crowigor's Base Manager",
                 "[" + section + "]",
            };

            customData.AddRange(configNew.ToList());

            GlobalConfig = configNew;
            Me.CustomData = String.Join("\n", customData.ToArray());
        }
        #endregion;
    }
}