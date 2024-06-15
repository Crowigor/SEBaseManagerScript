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
            public const string StopDrones = "CBM:SD";
            public const string DisplayStatus = "CBM:DS";
            public const string DisplayItems = "CBM:DI";
        }

        private readonly TasksManager _tasks;
        private BlocksManager _blocks;
        private ItemsManager _items;
        private Dictionary<string, List<string>> _messages;
        private ConfigObject _globalConfig;

        #endregion;

        #region Ingame methods

        public Program()
        {
            // Add Tasks
            _tasks = new TasksManager();
            _tasks.Add(TasksManager.InitializationTaskName, TaskInitialization, 20, false);
            _tasks.Add(TasksManager.CalculationTaskName, TaskCalculation, 3);
            _tasks.Add("Inventory Manager", TaskInventoryManager, 10);
            _tasks.Add("Assemblers Manager", TaskAssemblersManager, 5, true, true);
            _tasks.Add("Connectors Manager", TaskConnectorsManager, 6, true, true);
            _tasks.Add("Display Status", TaskDisplayStatus, 1, false);

            // Run Initialization
            TaskInitialization();

            // Set update rate
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (string.IsNullOrEmpty(argument))
                _tasks.Run();
        }

        #endregion

        #region Tasks

        private void TaskInitialization()
        {
            ConfigureMeDisplay();
            CheckGlobalConfig();
            _blocks = new BlocksManager(GridTerminalSystem, _globalConfig.Get("Tag"), _globalConfig.Get("Ignore"));

            if (_items == null)
                _items = new ItemsManager();
            else
                _items.ClearInventories();

            if (_messages != null)
                _messages.Clear();
            else
                _messages = new Dictionary<string, List<string>>();

            // Add inventories to items
            foreach (var block in _blocks.GetBlocks(BlocksManager.BlockType.GasTanks))
            {
                const string configSection = ConfigsSections.InventoryManager;
                if (!block.CustomData.Contains(configSection))
                    continue;

                var config = ConfigObject.Parse(configSection, block.CustomData);
                foreach (var item in config.Data.Select(entry => _items.GetItem(entry.Key)).Where(item => item != null))
                {
                    item.Inventories.Add(block.GetInventory(0));
                }
            }

            foreach (var block in _blocks.GetBlocks(BlocksManager.BlockType.Containers))
            {
                const string configSection = ConfigsSections.InventoryManager;
                if (!block.CustomData.Contains(configSection))
                    continue;

                var config = ConfigObject.Parse(configSection, block.CustomData);
                foreach (var item in config.Data.Select(entry => _items.GetItem(entry.Key)).Where(item => item != null))
                {
                    item.Inventories.Add(block.GetInventory(0));
                }
            }
        }

        private void TaskCalculation()
        {
            // Calculate Items Amounts
            _items.ClearAmounts();
            foreach (var block in _blocks.GetBlocks().Where(block => block.IsFunctional))
            {
                if (block.InventoryCount > 0)
                {
                    for (var i = 0; i < block.InventoryCount; i++)
                    {
                        var inventory = block.GetInventory(i);
                        if (inventory == null)
                            continue;

                        var items = new List<MyInventoryItem>();
                        inventory.GetItems(items);
                        if (items.Count <= 0)
                            continue;

                        foreach (var item in items)
                        {
                            var find = _items.GetItem(item.Type.ToString());
                            if (find == null)
                                continue;

                            find.Amounts.Exist += item.Amount;
                            _items.UpdateItem(find);
                        }
                    }
                }

                var assembler = block as IMyAssembler;
                if (assembler == null) continue;
                {
                    if (!assembler.IsWorking)
                        continue;

                    // Calculate  assembler queue
                    if (!assembler.IsQueueEmpty)
                    {
                        var queue = new List<MyProductionItem>();
                        assembler.GetQueue(queue);
                        foreach (var item in queue)
                        {
                            var find = _items.GetItem(item.BlueprintId.ToString());
                            if (find == null)
                                continue;

                            var amount = item.Amount * find.Blueprints[item.BlueprintId];
                            if (assembler.Mode == MyAssemblerMode.Assembly)
                                find.Amounts.Assembling += amount;
                            else if (assembler.Mode == MyAssemblerMode.Disassembly)
                                find.Amounts.Disassembling += amount;
                        }
                    }

                    // Calculate assembler quota
                    if (assembler.CooperativeMode) continue;
                    {
                        if (assembler.Mode == MyAssemblerMode.Assembly &&
                            assembler.CustomData.Contains(ConfigsSections.ItemsAssembling))
                        {
                            var config = ConfigObject.Parse(ConfigsSections.ItemsAssembling, assembler.CustomData);
                            if (config == null)
                                continue;

                            foreach (var entry in config.Data)
                            {
                                var find = _items.GetItem(entry.Key);
                                if (find == null) continue;
                                if (find.Amounts.AssemblingQuota < 0)
                                {
                                    find.Amounts.AssemblingQuota = 0;
                                }

                                find.Amounts.AssemblingQuota += MyFixedPoint.DeserializeString(entry.Value);
                            }
                        }
                        else if (assembler.Mode == MyAssemblerMode.Disassembly &&
                                 assembler.CustomData.Contains(ConfigsSections.ItemsDisassembling))
                        {
                            var config = ConfigObject.Parse(ConfigsSections.ItemsDisassembling,
                                assembler.CustomData);
                            if (config == null)
                                continue;

                            foreach (var entry in config.Data)
                            {
                                var find = _items.GetItem(entry.Key);
                                if (find == null)
                                    continue;

                                if (find.Amounts.DisassemblingQuota < 0)
                                    find.Amounts.DisassemblingQuota = 0;

                                find.Amounts.DisassemblingQuota += MyFixedPoint.DeserializeString(entry.Value);
                            }
                        }
                    }
                }
            }

            foreach (var item in _items.GetList())
            {
                item.Amounts.IsNew = false;
                _items.UpdateItem(item);
            }
        }

        private void TaskInventoryManager()
        {
            var inventories = new List<IMyInventory>();
            foreach (var block in _blocks.GetBlocks().Where(block => block.IsFunctional)
                         .Where(block => !(block is IMyLargeConveyorTurretBase)))
            {
                if (block is IMyGasGenerator)
                {
                    var inventory = block.GetInventory(0);
                    var items = new List<MyInventoryItem>();
                    inventory.GetItems(items);
                    foreach (var item in items)
                    {
                        var find = _items.GetItem(item.Type.ToString());
                        if (find != null && find.Selector != "Ore/Ice")
                            find.Transfer(item, inventory);
                    }
                }
                else if (block is IMyAssembler)
                    inventories.Add(block.GetInventory(1));
                else if (block is IMyRefinery)
                    inventories.Add(block.GetInventory(1));
                else
                {
                    for (var i = 0; i < block.InventoryCount; i++)
                    {
                        inventories.Add(block.GetInventory(i));
                    }
                }
            }

            _items.TransferFromInventories(inventories);
        }

        private void TaskAssemblersManager()
        {
            foreach (var assembler in _blocks.GetBlocks(BlocksManager.BlockType.Assemblers).Cast<IMyAssembler>()
                         .Where(assembler => assembler.IsFunctional))
            {
                // Cleanup source inventory
                _items.TransferFromInventory(assembler.GetInventory(0));

                // Add items to quota
                if (!assembler.IsWorking)
                    continue;
                if (!assembler.CooperativeMode)
                {
                    if (assembler.Mode == MyAssemblerMode.Assembly)
                    {
                        // Items Assembling
                        var config = ConfigObject.Parse(ConfigsSections.ItemsAssembling, assembler.CustomData);
                        if (config != null)
                        {
                            foreach (var entry in config.Data)
                            {
                                var item = _items.GetItem(entry.Key);
                                if (item == null || item.Blueprints.Count <= 0)
                                    continue;

                                foreach (var blueprint in item.Blueprints)
                                {
                                    if (!assembler.CanUseBlueprint(blueprint.Key))
                                        continue;

                                    var need = MyFixedPoint.DeserializeString(entry.Value);
                                    var total = item.Amounts.Exist + item.Amounts.Assembling;
                                    if (total < need)
                                    {
                                        var add = need - total;
                                        var calc = (int)Math.Ceiling((float)add / (float)blueprint.Value);
                                        var queue = MyFixedPoint.DeserializeString(calc.ToString());

                                        assembler.Repeating = false;
                                        assembler.AddQueueItem(blueprint.Key, queue);

                                        item.Amounts.Assembling += queue * blueprint.Value;
                                        _items.UpdateItem(item);
                                    }

                                    break;
                                }
                            }
                        }
                    }
                    else if (assembler.Mode == MyAssemblerMode.Disassembly)
                    {
                        // Items Disassembling
                        var config = ConfigObject.Parse(ConfigsSections.ItemsDisassembling, assembler.CustomData);
                        if (config != null)
                        {
                            foreach (var entry in config.Data)
                            {
                                var item = _items.GetItem(entry.Key);
                                if (item == null || item.Blueprints.Count <= 0)
                                    continue;

                                foreach (var blueprint in item.Blueprints)
                                {
                                    if (!assembler.CanUseBlueprint(blueprint.Key))
                                        continue;

                                    var value = entry.Value ?? "0";

                                    var need = MyFixedPoint.DeserializeString(value);
                                    var total = item.Amounts.Exist - item.Amounts.Disassembling;
                                    if (total > need)
                                    {
                                        var add = total - need;
                                        var calc = (int)Math.Round((float)add / (float)blueprint.Value, 0);
                                        var queue = MyFixedPoint.DeserializeString(calc.ToString());

                                        assembler.Repeating = false;
                                        assembler.AddQueueItem(blueprint.Key, queue);

                                        item.Amounts.Disassembling += queue * blueprint.Value;
                                        _items.UpdateItem(item);
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }

                // Move items for disassembly
                if (assembler.Mode != MyAssemblerMode.Disassembly) continue;
                {
                    var queue = new List<MyProductionItem>();
                    assembler.GetQueue(queue);
                    foreach (var queueItem in queue)
                    {
                        var item = _items.GetItem(queueItem.BlueprintId.ToString());
                        if (item == null || item.Blueprints.Count <= 0)
                            continue;

                        foreach (var quantity in from blueprint in item.Blueprints
                                 where assembler.CanUseBlueprint(blueprint.Key)
                                 select queueItem.Amount * blueprint.Value)
                        {
                            InventoryHelper.TransferFromBlocks(item.Type, _blocks.GetBlocks(),
                                assembler.GetInventory(1), quantity);
                        }
                    }
                }
            }
        }

        private void TaskConnectorsManager()
        {
            if (!_messages.ContainsKey("Stop Drones"))
                _messages["Stop Drones"] = new List<string>();
            else
                _messages["Stop Drones"].Clear();

            foreach (var connector in _blocks.GetBlocks(BlocksManager.BlockType.Connectors).Cast<IMyShipConnector>()
                         .Where(connector => connector.IsFunctional))
            {
                // Stop drones
                if (connector.CustomData.Contains(ConfigsSections.StopDrones))
                {
                    // Legacy replacer
                    connector.CustomData = connector.CustomData.Replace("DronBlocksName", "DroneBlocksName");

                    var config = ConfigObject.Parse(ConfigsSections.StopDrones, connector.CustomData);
                    var droneBlocksName = config.Get("DroneBlocksName");
                    var baseContainersName = config.Get("BaseContainersName");

                    string error = null;
                    if (string.IsNullOrEmpty(droneBlocksName))
                        error = "Empty DroneBlocksName";
                    if (string.IsNullOrEmpty(baseContainersName))
                        error = "Empty BaseContainersName";

                    if (!string.IsNullOrEmpty(error))
                    {
                        _messages["Stop Drones"].Add(connector.CustomName + ": " + error);
                        continue;
                    }

                    if (connector.Status != MyShipConnectorStatus.Connected)
                        continue;

                    var otherConnector = connector.OtherConnector;
                    var blocks = new List<IMyTerminalBlock>();
                    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks,
                        block => block.CubeGrid == otherConnector.CubeGrid &&
                                 block.CustomName.Contains(droneBlocksName));
                    if (blocks.Count == 0)
                    {
                        _messages["Stop Drones"].Add(connector.CustomName + ": " +
                                                     "Can't find drones blocks `" + droneBlocksName + "`");
                        continue;
                    }

                    var blockActive = true;
                    if (baseContainersName != null)
                    {
                        float current = 0;
                        float total = 0;

                        foreach (var inventory in from IMyCargoContainer container in
                                     _blocks.GetBlocks(BlocksManager.BlockType.Containers)
                                 where container.CustomName.Contains(baseContainersName)
                                 select container.GetInventory(0))
                        {
                            current += (float)inventory.CurrentVolume;
                            total += (float)inventory.MaxVolume;
                        }

                        var maxConfig = config.Get("BaseContainersMaxVolume",
                            _globalConfig.Get("SD:BaseContainersMaxVolume", "90%"));

                        var percent = maxConfig.Contains("%");
                        var max = (float)MyFixedPoint.DeserializeString(maxConfig.Replace("%", ""));
                        if (percent)
                        {
                            var calc = current / total * 100;
                            if (calc >= max)
                                blockActive = false;
                        }
                        else if (current >= total)
                            blockActive = false;
                    }

                    foreach (var block in blocks)
                    {
                        block.ApplyAction(blockActive ? "OnOff_On" : "OnOff_Off");
                    }
                }
            }
        }

        private void TaskDisplayStatus()
        {
            var displayData = new List<string>()
            {
                { "= Crowigor's Base Manager =" },
            };
            displayData.AddRange(_tasks.GetStatusText());

            if (_messages.Count > 0)
            {
                displayData.Add("");
                foreach (var entry in _messages.Where(entry => entry.Value.Count != 0))
                {
                    displayData.Add(entry.Key + ": ");
                    displayData.AddRange(entry.Value.Select(message => "   " + message));
                }
            }

            Echo(string.Join("\n", displayData.ToArray()));
        }

        #endregion

        #region Script Methods

        private void ConfigureMeDisplay()
        {
            // Set display
            var display = Me.GetSurface(0);
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
            const string section = ConfigsSections.GlobalConfig;
            var configDefault = new ConfigObject(section, new Dictionary<string, string>()
            {
                { "Tag", "[Base]" },
                { "Ignore", "[!CBM]" },
                { "SD:BaseContainersMaxVolume", "90%" },
            });
            var configCurrent = ConfigObject.Parse(section, Me.CustomData);
            var configNew = ConfigObject.Merge(section, new List<ConfigObject> { configDefault, configCurrent });

            var customData = new List<string>()
            {
                ";Crowigor's Base Manager",
                "[" + section + "]",
            };
            customData.AddRange(configNew.ToList());

            _globalConfig = configNew;
            Me.CustomData = string.Join("\n", customData.ToArray());
        }

        #endregion;
    }
}