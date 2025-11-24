using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sandbox.Game.GameSystems;
using VRage;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Profiler;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region Fields and Properties

        public static class ConfigsSections
        {
            public const string GlobalConfig = "CBM:GC";
            public const string CustomItems = "CBM:CI";
            public const string DisplayConfig = "CBM:DC";
            public const string InventoryManager = "CBM:IM";
            public const string SpecialContainer = "CBM:SC";
            public const string ItemsAssembling = "CBM:IA";
            public const string ItemsDisassembling = "CBM:ID";
            public const string RefineryManager = "CBM:RM";
            public const string ItemsCollecting = "CBM:IC";
            public const string StopDrones = "CBM:SD";
            public const string DisplayStatus = "CBM:DS";
            public const string DisplayItems = "CBM:DI";
            public const string DisplayLimits = "CBM:DL";
            public const string DisplayVolumes = "CBM:DV";
            public const string DisplayVolumesRemained = "CBM:DVR";

            public static readonly List<string> Displays = new List<string>
                { DisplayConfig, DisplayStatus, DisplayItems, DisplayLimits, DisplayVolumes, DisplayVolumesRemained };
        }

        private const string ScanPrefix = "CBM-SCAN";
        private const string PrintPrefix = "CBM-PRINT";

        private readonly TasksManager _tasks;
        private BlocksManager _blocks;
        private ItemsManager _items;
        private Dictionary<string, DisplayObject> _displays;
        private Dictionary<long, VolumeObject> _volumes;
        private Dictionary<string, List<string>> _messages;
        private ConfigObject _globalConfig;
        private double _time;

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
            _tasks.Add("Assemblers Cleanup", TaskAssemblersCleanup, 20);
            _tasks.Add("Refineries Manager", TaskRefineriesManager, 9);
            _tasks.Add("Stop Drones", TaskStopDrones, 6);
            _tasks.Add("Items Collecting", TaskItemsCollecting, 7);
            _tasks.Add("Displays Manager", TaskDisplaysManager, 1, true, true);
            _tasks.Add("Display Status", TaskDisplayStatus, 1, false);

            // Run Initialization
            TaskInitialization();

            // Set update rate
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (string.IsNullOrEmpty(argument))
            {
                _tasks.Run();
                _time += Runtime.TimeSinceLastRun.TotalSeconds;
                return;
            }

            argument = argument.ToLower().Trim();
            if (argument == "restart")
            {
                TaskInitialization();
                _tasks.Restart();
            }
            else if (argument.StartsWith("clear_queue"))
            {
                ActionClearQueue(argument);
            }
            else if (argument.StartsWith("clear_inventories"))
            {
                ActionClearInventories(argument);
            }
            else if (argument.StartsWith("scan"))
            {
                ActionScanItems();
            }
            else if (argument.StartsWith("print"))
            {
                ActionPrintItems();
            }
        }

        #endregion

        #region Tasks

        private void TaskInitialization()
        {
            if (_messages == null)
            {
                _messages = new Dictionary<string, List<string>>();
            }
            else
            {
                _messages.Clear();
            }

            ConfigureMeDisplay();
            CheckGlobalConfig();
            var ignoreTypes = new List<BlocksManager.BlockType>
            {
                BlocksManager.BlockType.AirVent,
                BlocksManager.BlockType.Battery,
                BlocksManager.BlockType.GasPowerProducer,
                BlocksManager.BlockType.GravityGenerator,
                BlocksManager.BlockType.Piston,
                BlocksManager.BlockType.Projector,
                BlocksManager.BlockType.Rotor,
                BlocksManager.BlockType.Sensor
            };
            _blocks = new BlocksManager(GridTerminalSystem, _globalConfig.Get("Tag"), _globalConfig.Get("Ignore"),
                null, ignoreTypes);

            if (_items == null)
            {
                _items = new ItemsManager(ItemsManager.GetCustomItemsFromString(Me.CustomData));
            }
            else
            {
                _items.ClearInventories();
            }

            if (_displays == null)
            {
                _displays = new Dictionary<string, DisplayObject>();
            }

            if (_volumes == null)
            {
                _volumes = new Dictionary<long, VolumeObject>();
            }

            // Add inventories to items
            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.GasTank))
            {
                if (!terminalBlock.CustomData.Contains(ConfigsSections.InventoryManager))
                    continue;

                var config = ConfigObject.Parse(ConfigsSections.InventoryManager, terminalBlock.CustomData);
                foreach (var key in config.Data.Keys.ToList())
                {
                    var item = _items.GetItem(key);
                    if (item == null)
                        continue;

                    item.Inventories.Add(terminalBlock.GetInventory(0));
                    _items.UpdateItem(item);
                }
            }

            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.Container))
            {
                if (!terminalBlock.CustomData.Contains(ConfigsSections.InventoryManager))
                    continue;

                var config = ConfigObject.Parse(ConfigsSections.InventoryManager, terminalBlock.CustomData);
                foreach (var key in config.Data.Keys.ToList())
                {
                    var item = _items.GetItem(key);
                    if (item == null)
                        continue;

                    item.Inventories.Add(terminalBlock.GetInventory(0));
                    _items.UpdateItem(item);
                }
            }

            // Update displays
            var displays = new Dictionary<string, DisplayObject>();
            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.TextSurfaceProvider))
            {
                if (!ConfigsSections.Displays.Any(key => terminalBlock.CustomData.Contains(key)))
                    continue;

                var provider = terminalBlock as IMyTextSurfaceProvider;
                if (provider == null)
                {
                    continue;
                }

                var count = provider.SurfaceCount;
                if (count <= 0)
                {
                    continue;
                }

                for (int index = 0; index < count; index++)
                {
                    var config = GetDisplayConfig(terminalBlock, index);
                    var selector = terminalBlock.GetId() + ":" + index;
                    var delay = 5;
                    if (GetDisplayConfigfSection(terminalBlock, ConfigsSections.DisplayConfig, index) != null)
                    {
                        delay = 1;
                    }
                    else if (GetDisplayConfigfSection(terminalBlock, ConfigsSections.DisplayVolumesRemained, index) !=
                             null)
                    {
                        delay = 3;
                    }

                    var listingDelay = int.Parse(config.Get("listingDelay"));

                    DisplayObject display;
                    if (_displays.ContainsKey(selector))
                    {
                        display = _displays[selector];
                        display.UpdateDelay = delay;
                        display.ListingDelay = listingDelay;
                    }
                    else
                    {
                        display = new DisplayObject(selector, terminalBlock.GetId(), index, delay, listingDelay);
                    }

                    displays[selector] = display;
                }
            }

            _displays = displays;

            // Update volumes
            var volumes = new Dictionary<long, VolumeObject>();
            foreach (var terminalBlock in _blocks.GetBlocks())
            {
                VolumeObject.VolumeTypes volumeType;
                var blockType = BlocksManager.GetBlockTypes(terminalBlock);
                if (blockType.Contains(BlocksManager.BlockType.Battery))
                {
                    volumeType = VolumeObject.VolumeTypes.Battery;
                }
                else if (blockType.Contains(BlocksManager.BlockType.GasTank))
                {
                    volumeType = VolumeObject.VolumeTypes.Tank;
                }
                else if (blockType.Contains(BlocksManager.BlockType.Container))
                {
                    volumeType = VolumeObject.VolumeTypes.Container;
                }
                else if (blockType.Contains(BlocksManager.BlockType.Collector))
                {
                    volumeType = VolumeObject.VolumeTypes.Container;
                }
                else if (blockType.Contains(BlocksManager.BlockType.Connector))
                {
                    volumeType = VolumeObject.VolumeTypes.Container;
                }
                else if (blockType.Contains(BlocksManager.BlockType.Drill))
                {
                    volumeType = VolumeObject.VolumeTypes.Container;
                }
                else
                {
                    continue;
                }

                var selector = terminalBlock.GetId();
                VolumeObject volume;
                if (_volumes.ContainsKey(selector))
                {
                    volume = _volumes[selector];
                    volume.BlockName = terminalBlock.CustomName;
                }
                else
                {
                    volume = new VolumeObject(selector, volumeType, terminalBlock.CustomName);
                }

                volumes[selector] = volume;
            }

            _volumes = volumes;
        }

        private void TaskCalculation()
        {
            // Calculate Items Amounts
            _items.ClearAmounts();
            foreach (var terminalBlock in _blocks.GetBlocks())
            {
                if (!terminalBlock.IsFunctional)
                    continue;

                if (terminalBlock.InventoryCount > 0)
                {
                    for (var i = 0; i < terminalBlock.InventoryCount; i++)
                    {
                        var inventory = terminalBlock.GetInventory(i);
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

                var assembler = terminalBlock as IMyAssembler;
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

            // Calculate volumes
            foreach (var volumeObject in _volumes.Values)
            {
                var terminalBlock = _blocks.GetBlock(volumeObject.Selector);
                if (volumeObject.VolumeType == VolumeObject.VolumeTypes.Battery)
                {
                    var block = terminalBlock as IMyBatteryBlock;
                    volumeObject.SetValue(block.CurrentStoredPower, block.MaxStoredPower, _time);
                }
                else if (volumeObject.VolumeType == VolumeObject.VolumeTypes.Tank)
                {
                    var block = terminalBlock as IMyGasTank;
                    volumeObject.SetValue(block.FilledRatio, (float)1, _time);
                }
                else if (volumeObject.VolumeType == VolumeObject.VolumeTypes.Container)
                {
                    var inventory = terminalBlock.GetInventory(0);
                    if (inventory == null)
                    {
                        continue;
                    }

                    volumeObject.SetValue(inventory.CurrentVolume.ToIntSafe(), inventory.MaxVolume.ToIntSafe(), _time);
                }
            }
        }

        private void TaskInventoryManager()
        {
            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.Container))
            {
                if (!terminalBlock.IsFunctional || !terminalBlock.CustomData.Contains(ConfigsSections.SpecialContainer))
                    continue;

                var container = terminalBlock as IMyCargoContainer;
                if (container == null)
                    continue;

                var config = ConfigObject.Parse(ConfigsSections.SpecialContainer, container.CustomData);
                if (config == null)
                    continue;

                var inventory = container.GetInventory(0);
                _items.TransferFromInventory(inventory);

                foreach (var entry in config.Data)
                {
                    var item = _items.GetItem(entry.Key);
                    if (item == null)
                        continue;

                    var needle = MyFixedPoint.DeserializeString(entry.Value);
                    if (needle == 0)
                        continue;

                    var current = inventory.GetItemAmount(item.Type);
                    if (current >= needle)
                        continue;

                    var quantity = needle - current;
                    InventoryHelper.TransferFromBlocks(item.Type, _blocks.GetBlocks(), inventory, quantity);
                }
            }

            var inventories = new List<IMyInventory>();
            var ignoreBlockTypes = new List<BlocksManager.BlockType>
            {
                BlocksManager.BlockType.Turret,
                BlocksManager.BlockType.Reactor,
                BlocksManager.BlockType.SafeZone
            };
            foreach (var terminalBlock in _blocks.GetBlocks())
            {
                if (!terminalBlock.IsFunctional || terminalBlock.CustomData.Contains(ConfigsSections.SpecialContainer))
                    continue;

                var blockType = BlocksManager.GetBlockTypes(terminalBlock);
                if (blockType.Intersect(ignoreBlockTypes).Any())
                    continue;

                if (blockType.Contains(BlocksManager.BlockType.GasGenerator))
                {
                    var inventory = terminalBlock.GetInventory(0);
                    var items = new List<MyInventoryItem>();
                    inventory.GetItems(items);
                    foreach (var item in items)
                    {
                        var find = _items.GetItem(item.Type.ToString());
                        if (find != null && find.Selector != "Ore/Ice")
                        {
                            find.Transfer(item, inventory);
                        }
                    }
                }
                else if (terminalBlock is IMyAssembler)
                {
                    inventories.Add(terminalBlock.GetInventory(1));
                }
                else if (terminalBlock is IMyRefinery)
                {
                    inventories.Add(terminalBlock.GetInventory(1));
                }
                else
                {
                    for (var i = 0; i < terminalBlock.InventoryCount; i++)
                    {
                        inventories.Add(terminalBlock.GetInventory(i));
                    }
                }
            }

            _items.TransferFromInventories(inventories);
        }

        private void TaskAssemblersManager()
        {
            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.Assembler))
            {
                if (!terminalBlock.IsWorking)
                    continue;

                var assembler = (IMyAssembler)terminalBlock;

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

                // // Move items for disassembly
                if (assembler.Mode == MyAssemblerMode.Disassembly)
                {
                    var queue = new List<MyProductionItem>();
                    assembler.GetQueue(queue);
                    var max = 5;
                    var current = 0;
                    foreach (var queueItem in queue)
                    {
                        var item = _items.GetItem(queueItem.BlueprintId.ToString());
                        if (item == null || item.Blueprints.Count <= 0)
                            continue;

                        foreach (var blueprint in item.Blueprints)
                        {
                            if (!assembler.CanUseBlueprint(blueprint.Key))
                                continue;

                            MyFixedPoint quantity = queueItem.Amount * blueprint.Value;
                            var transfer = InventoryHelper.TransferFromBlocks(item.Type, _blocks.GetBlocks(),
                                assembler.GetInventory(1), quantity);
                            if (transfer != null && transfer > 0)
                            {
                                current++;
                            }

                            break;
                        }

                        if (current >= max)
                            break;
                    }
                }
            }
        }

        private void TaskAssemblersCleanup()
        {
            var inventories = new List<IMyInventory>();
            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.Assembler))
            {
                if (!terminalBlock.IsWorking)
                    continue;

                inventories.Add(terminalBlock.GetInventory(0));
            }

            _items.TransferFromInventories(inventories);
        }

        private void TaskRefineriesManager()
        {
            var destinations = new List<IMyRefinery>();
            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.Refinery))
            {
                if (!terminalBlock.IsWorking || !terminalBlock.CustomData.Contains(ConfigsSections.RefineryManager))
                    continue;
                var refinery = terminalBlock as IMyRefinery;
                if (refinery == null)
                    continue;

                destinations.Add(refinery);
            }

            if (destinations.Count == 0)
            {
                return;
            }

            var sources = new List<IMyTerminalBlock>();
            foreach (var terminalBlock in _blocks.GetBlocks())
            {
                if (terminalBlock.CustomData.Contains(ConfigsSections.RefineryManager))
                    continue;
                sources.Add(terminalBlock);
            }

            foreach (var refinery in destinations)
            {
                var config = ConfigObject.Parse(ConfigsSections.RefineryManager, refinery.CustomData);
                if (config == null || config.Data.Keys.Count == 0)
                    continue;

                var destinationInventory = refinery.GetInventory(0);
                _items.TransferFromInventory(destinationInventory);
                foreach (var key in config.Data.Keys)
                {
                    var item = _items.GetItem(key);
                    if (item == null)
                        continue;

                    InventoryHelper.TransferFromBlocks(item.Type, sources, destinationInventory);
                    if (destinationInventory.IsFull)
                        break;
                }
            }
        }

        private void TaskStopDrones()
        {
            if (!_messages.ContainsKey("Stop Drones"))
            {
                _messages["Stop Drones"] = new List<string>();
            }
            else
            {
                _messages["Stop Drones"].Clear();
            }

            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.Connector))
            {
                if (!terminalBlock.IsWorking || !terminalBlock.CustomData.Contains(ConfigsSections.StopDrones))
                    continue;

                var connector = (IMyShipConnector)terminalBlock;
                if (connector.Status != MyShipConnectorStatus.Connected)
                    continue;

                var config = ConfigObject.Parse(ConfigsSections.StopDrones, connector.CustomData);
                if (config == null)
                    continue;

                var droneBlocksName = config.Get("DroneBlocksName");
                var baseContainersName = config.Get("BaseContainersName");

                string error = null;
                if (string.IsNullOrEmpty(droneBlocksName))
                    error = "Empty DroneBlocksName";
                if (string.IsNullOrEmpty(baseContainersName))
                    error = "Empty BaseContainersName";

                if (!string.IsNullOrEmpty(error))
                {
                    _messages["Stop Drones"].Add(connector.CustomName + ":");
                    _messages["Stop Drones"].Add(error);
                    continue;
                }

                var connectedBlocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(connectedBlocks,
                    block => block.CubeGrid == connector.OtherConnector.CubeGrid
                             && droneBlocksName != null && block.CustomName.Contains(droneBlocksName));
                if (connectedBlocks.Count == 0)
                {
                    _messages["Stop Drones"].Add(connector.CustomName + ":");
                    _messages["Stop Drones"].Add("Can't find drones blocks `" + droneBlocksName + "`");
                    continue;
                }

                var blockActive = true;
                float current = 0;
                float total = 0;
                foreach (var container in _blocks.GetBlocks(BlocksManager.BlockType.Container))
                {
                    if (baseContainersName != null && !container.CustomName.Contains(baseContainersName))
                        continue;
                    var inventory = container.GetInventory(0);

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

                foreach (var block in connectedBlocks)
                {
                    block.ApplyAction(blockActive ? "OnOff_On" : "OnOff_Off");
                }
            }
        }

        private void TaskItemsCollecting()
        {
            var itemsCollectingBlockTypes = new List<BlocksManager.BlockType>
            {
                BlocksManager.BlockType.Cockpit,
                BlocksManager.BlockType.Collector,
                BlocksManager.BlockType.Connector,
                BlocksManager.BlockType.Container,
                BlocksManager.BlockType.CryoChamber,
                BlocksManager.BlockType.Drill,
                BlocksManager.BlockType.Grinder,
                BlocksManager.BlockType.Sorter,
                BlocksManager.BlockType.Welder
            };

            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.Connector))
            {
                if (!terminalBlock.IsWorking || !terminalBlock.CustomData.Contains(ConfigsSections.ItemsCollecting))
                    continue;

                var connector = (IMyShipConnector)terminalBlock;
                if (connector.Status != MyShipConnectorStatus.Connected)
                    continue;

                var connectedConnector = connector.OtherConnector;
                var connectedBlocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(connectedBlocks,
                    block => block.CubeGrid == connectedConnector.CubeGrid);

                var destinationInventory = connector.GetInventory(0);
                _items.TransferFromInventory(destinationInventory);

                var config = ConfigObject.Parse(ConfigsSections.ItemsCollecting, connector.CustomData);
                var collectTypes = new List<MyItemType>();
                var ignoreTypes = new List<MyItemType>();
                if (config != null && config.Data.Keys.Count > 0)
                {
                    foreach (var key in config.Data.Keys)
                    {
                        var ignore = key.StartsWith("!");
                        var itemKey = ignore ? key.TrimStart('!') : key;
                        var item = _items.GetItem(itemKey);
                        if (item == null)
                            continue;

                        if (ignore)
                            ignoreTypes.Add(item.Type);
                        else
                            collectTypes.Add(item.Type);
                    }
                }

                foreach (var block in connectedBlocks)
                {
                    if (block.CustomName.Contains("!" + ConfigsSections.ItemsCollecting)
                        || block.CustomData.Contains("!" + ConfigsSections.ItemsCollecting)
                        || !BlocksManager.GetBlockTypes(block).Intersect(itemsCollectingBlockTypes).Any())
                        continue;

                    for (var i = 0; i < block.InventoryCount; i++)
                    {
                        var sourceInventory = block.GetInventory(i);
                        var sourceItems = new List<MyInventoryItem>();
                        sourceInventory.GetItems(sourceItems);
                        foreach (var item in sourceItems)
                        {
                            if (collectTypes.Count != 0 && !collectTypes.Contains(item.Type))
                                continue;

                            if (ignoreTypes.Count > 0 && ignoreTypes.Contains(item.Type))
                                continue;

                            InventoryHelper.TransferItem(item, sourceInventory, destinationInventory);
                            _items.TransferFromInventory(destinationInventory);
                        }
                    }
                }
            }
        }

        private void TaskDisplaysManager()
        {
            foreach (var displayObject in _displays.Values.ToList())
            {
                var terminalBlock = _blocks.GetBlock(displayObject.BlockSelector,
                    BlocksManager.BlockType.TextSurfaceProvider);

                if (terminalBlock == null || !terminalBlock.IsWorking ||
                    !ConfigsSections.Displays.Any(key => terminalBlock.CustomData.Contains(key)))
                    continue;

                var config = GetDisplayConfig(terminalBlock, displayObject.SurfaceIndex);
                var language = config.Get("language");

                displayObject.Tick();

                if (displayObject.NeedUpdate())
                {
                    displayObject.TickReset();
                    displayObject.ClearLines();

                    var isDisplayStatus = false;
                    if (displayObject.SurfaceIndex == 0 &&
                        terminalBlock.CustomData.Contains(ConfigsSections.DisplayStatus + "]"))
                    {
                        isDisplayStatus = true;
                    }
                    else if (terminalBlock.CustomData.Contains(
                                 ConfigsSections.DisplayStatus + ":" + displayObject.SurfaceIndex))
                    {
                        isDisplayStatus = true;
                    }

                    if (isDisplayStatus)
                    {
                        displayObject.AddTextLine("Runtime: " + SecondsToString(_time));
                        foreach (var line in _tasks.GetStatusText())
                        {
                            displayObject.AddTextLine(line);
                        }

                        foreach (var messageSection in _messages)
                        {
                            if (messageSection.Value.Count == 0)
                                continue;

                            displayObject.AddBlankLine();
                            displayObject.AddTextLine(messageSection.Key + ":");

                            foreach (var message in messageSection.Value.ToList())
                            {
                                displayObject.AddTextLine(message);
                            }
                        }
                    }
                    else
                    {
                        var configs = ConfigsHelper.GetSections(terminalBlock.CustomData, false);
                        foreach (var configSection in configs)
                        {
                            var configKey = ConfigsHelper.RemoveSectionIndex(configSection.Key);
                            if (!ConfigsSections.Displays.Any(key =>
                                    key != ConfigsSections.DisplayConfig && configKey.Contains(key))
                                || configSection.Value.Count == 0)
                            {
                                continue;
                            }

                            var configIndexPosition = configKey.LastIndexOf(":", StringComparison.Ordinal);
                            var configIndexString = configKey.Substring(configIndexPosition + 1);
                            if (!configIndexString.All(char.IsDigit))
                            {
                                if (displayObject.SurfaceIndex == 0)
                                {
                                    configIndexString = "0";
                                }
                                else
                                    continue;
                            }
                            else
                            {
                                configKey = configKey.Substring(0, configIndexPosition);
                            }

                            var configIndex = int.Parse(configIndexString);
                            if (configIndex != displayObject.SurfaceIndex)
                            {
                                continue;
                            }

                            foreach (var line in configSection.Value.ToList())
                            {
                                var clear = line.Trim();
                                if (string.IsNullOrEmpty(clear))
                                {
                                    displayObject.AddBlankLine();
                                    continue;
                                }

                                if (configKey == ConfigsSections.DisplayItems)
                                {
                                    var item = _items.GetItem(clear);
                                    if (item != null)
                                    {
                                        var label = item.Title(language);
                                        var text = item.Amounts.ToString();
                                        displayObject.AddLine(
                                            label + ": " + text,
                                            DisplayObject.TextSprite(label),
                                            DisplayObject.TextSprite(text, TextAlignment.RIGHT)
                                        );
                                        continue;
                                    }
                                }

                                if (configKey == ConfigsSections.DisplayLimits && line.Contains("="))
                                {
                                    var content = ConfigsHelper.ParseLine(line);
                                    if (!string.IsNullOrEmpty(content.Value))
                                    {
                                        var label = content.Key;
                                        var exist = MyFixedPoint.DeserializeString("-1");
                                        var needle = MyFixedPoint.DeserializeString(content.Value);

                                        var item = _items.GetItem(content.Key);
                                        if (item != null)
                                        {
                                            label = item.Title(language);
                                            exist = item.Amounts.Exist;
                                        }

                                        var text = ItemAmountsObject.ValueToString(exist) + "/" +
                                                   ItemAmountsObject.ValueToString(needle);
                                        Color? color = null;
                                        if (exist > needle)
                                        {
                                            color = Color.Green;
                                        }
                                        else if (exist < needle)
                                        {
                                            color = Color.Red;
                                        }

                                        displayObject.AddLine(
                                            label + ": " + text,
                                            DisplayObject.TextSprite(label),
                                            DisplayObject.TextSprite(text, TextAlignment.RIGHT, color)
                                        );

                                        continue;
                                    }
                                }

                                if ((configKey == ConfigsSections.DisplayVolumes
                                     || configKey == ConfigsSections.DisplayVolumesRemained) && line.Contains("="))
                                {
                                    var content = ConfigsHelper.ParseLine(line);
                                    if (!string.IsNullOrEmpty(content.Key))
                                    {
                                        var label = content.Key;
                                        var blockName = content.Value.ToLower();

                                        var objects = new List<VolumeObject>();
                                        foreach (var volumeObject in _volumes.Values)
                                        {
                                            if (!volumeObject.BlockName.ToLower().Contains(blockName))
                                            {
                                                continue;
                                            }

                                            objects.Add(volumeObject);
                                        }

                                        if (objects.Count > 0)
                                        {
                                            var sumObject = new VolumeObject(objects);
                                            if (!sumObject.IsValid)
                                            {
                                                continue;
                                            }

                                            var text = sumObject.CurrentPercent + "%";
                                            Color? color = null;
                                            if (configKey == ConfigsSections.DisplayVolumesRemained &&
                                                sumObject.RemainedVector != VolumeObject.RemainedVectors.None)
                                            {
                                                if (sumObject.RemainedVector ==
                                                    VolumeObject.RemainedVectors.Plus)
                                                {
                                                    color = Color.Green;
                                                }
                                                else if (sumObject.RemainedVector ==
                                                         VolumeObject.RemainedVectors.Minus)
                                                {
                                                    color = Color.Red;
                                                }

                                                if (sumObject.Remained > 2)
                                                {
                                                    var time = SecondsToString(sumObject.Remained);
                                                    text += " (";

                                                    if (sumObject.RemainedVector == VolumeObject.RemainedVectors.Plus)
                                                    {
                                                        text += "+";
                                                    }
                                                    else if (sumObject.RemainedVector ==
                                                             VolumeObject.RemainedVectors.Minus)
                                                    {
                                                        text += "-";
                                                    }

                                                    text += time + ")";
                                                }
                                            }

                                            displayObject.AddLine(label + ": " + text,
                                                DisplayObject.TextSprite(label),
                                                DisplayObject.TextSprite(text, TextAlignment.RIGHT, color)
                                            );

                                            continue;
                                        }
                                    }
                                }

                                displayObject.AddCustomTextLine(line);
                            }
                        }
                    }
                }

                _displays[displayObject.Selector] = displayObject;

                var provider = terminalBlock as IMyTextSurfaceProvider;
                var display = provider.GetSurface(displayObject.SurfaceIndex);
                if (display == null)
                {
                    continue;
                }

                if (displayObject.GetLines().Count == 0 && string.IsNullOrEmpty(config.Get("title")))
                {
                    continue;
                }

                var frame = display.DrawFrame();
                var viewport = DisplayObject.GetViewport(display);
                var padding = float.Parse(config.Get("padding"));
                var font = config.Get("font");
                var fontSize = float.Parse(config.Get("fontSize"));
                var lineHeight = float.Parse(config.Get("lineHeight"));
                var border = float.Parse(config.Get("border"));
                var positionLeft = viewport.X + padding;
                var positionRight = viewport.X + viewport.Width - padding;
                var positionCenter = viewport.X + viewport.Width / 2;
                var positionTop = viewport.Y + padding;
                var positionBottom = viewport.Y + viewport.Height - padding;

                // Main border
                if (border > 0)
                {
                    foreach (var sprite in DisplayObject.GetSurfaceBorder(display, border, padding))
                        frame.Add(sprite);

                    positionLeft = viewport.X + border + padding * 2;
                    positionRight = viewport.X + viewport.Width - border - padding * 2;
                    positionTop = viewport.Y + border + padding * 2;
                    positionBottom = viewport.Y + viewport.Height - border - padding * 2;
                }

                // Title
                var title = config.Get("title");
                if (!string.IsNullOrEmpty(title))
                {
                    foreach (var sprite in DisplayObject.GetSurfaceTitle(display, title, font, fontSize, lineHeight,
                                 border, padding))
                    {
                        frame.Add(sprite);
                    }

                    positionTop = viewport.Y + lineHeight + padding * 2;
                }

                // Content
                var limit = (int)Math.Round((positionBottom - positionTop) / lineHeight);
                displayObject.ListingTick();
                if (displayObject.NeedListing())
                {
                    displayObject.Listing(limit);
                }

                foreach (var line in displayObject.GetLines(limit))
                {
                    if (line.Count == 0)
                    {
                        positionTop += lineHeight;
                        continue;
                    }

                    foreach (var sprite in line)
                    {
                        var textSprite = sprite;
                        textSprite.Size = new Vector2(positionLeft, positionTop);
                        textSprite.FontId = font;
                        textSprite.RotationOrScale = fontSize;
                        if (textSprite.Color == null)
                        {
                            textSprite.Color = display.ScriptForegroundColor;
                        }

                        textSprite.Position = new Vector2(positionLeft, positionTop);
                        if (textSprite.Alignment == TextAlignment.RIGHT)
                        {
                            textSprite.Position = new Vector2(positionRight, positionTop);
                        }
                        else if (textSprite.Alignment == TextAlignment.CENTER)
                        {
                            textSprite.Position = new Vector2(positionCenter, positionTop);
                        }

                        frame.Add(textSprite);
                    }

                    positionTop += lineHeight;
                }

                // Draw
                display.ContentType = ContentType.SCRIPT;
                display.Script = "";
                display.WriteText(displayObject.LinesToString());

                frame.Dispose();
            }
        }

        private void TaskDisplayStatus()
        {
            var displayData = new List<string>()
            {
                { "= Crowigor's Base Manager =" },
            };
            displayData.Add("Runtime: " + SecondsToString(_time));
            displayData.AddRange(_tasks.GetStatusText());

            if (_messages.Count > 0)
            {
                displayData.Add("");
                foreach (var entry in _messages)
                {
                    if (entry.Value.Count == 0)
                    {
                        continue;
                    }

                    displayData.Add(entry.Key + ": ");
                    displayData.AddRange(entry.Value.Select(message => "   " + message));
                }
            }

            Echo(string.Join("\n", displayData.ToArray()));
        }

        #endregion

        #region Actions

        private void ActionClearQueue(string argument = "")
        {
            var blockName = argument.ToLower().Replace("clear_queue", "").Trim();

            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.Assembler))
            {
                if (!string.IsNullOrEmpty(argument))
                {
                    var customName = terminalBlock.CustomName.ToLower();
                    if (!customName.Contains(blockName))
                        continue;
                }

                var assembler = terminalBlock as IMyAssembler;
                assembler?.ClearQueue();
            }
        }

        private void ActionClearInventories(string argument = "")
        {
            var blockName = argument.ToLower().Replace("clear_inventories", "").Trim();
            foreach (var terminalBlock in _blocks.GetBlocks())
            {
                if (!string.IsNullOrEmpty(argument))
                {
                    var customName = terminalBlock.CustomName.ToLower();
                    if (!customName.Contains(blockName))
                        continue;
                }

                for (var i = 0; i < terminalBlock.InventoryCount; i++)
                {
                    var inventory = terminalBlock.GetInventory(i);
                    _items.TransferFromInventory(inventory);
                }
            }
        }

        private void ActionScanItems()
        {
            const string title = "[" + ConfigsSections.CustomItems + "]";
            var result = ItemsManager.ScanCustomItems(GridTerminalSystem, ScanPrefix, title);

            if (_messages == null)
            {
                _messages = new Dictionary<string, List<string>>();
            }

            _messages["Scan Result"] = result;
        }

        private void ActionPrintItems()
        {
            const string title = "[" + ConfigsSections.CustomItems + "]";
         
            var result = ItemsManager.PrintItems(_items ?? new ItemsManager(), GridTerminalSystem, PrintPrefix, title);

            if (_messages == null)
            {
                _messages = new Dictionary<string, List<string>>();
            }
            _messages["Print Result"] = result;
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

        private ConfigObject GetDisplayConfig(IMyTerminalBlock block, int index = 0)
        {
            var global = new ConfigObject(ConfigsSections.DisplayConfig, new Dictionary<string, string>
            {
                { "title", "" },
                { "font", _globalConfig.Get("DC:font", "Debug") },
                { "fontSize", _globalConfig.Get("DC:fontSize", "0.8") },
                { "lineHeight", _globalConfig.Get("DC:lineHeight", "32") },
                { "padding", _globalConfig.Get("DC:padding", "10") },
                { "border", _globalConfig.Get("DC:border", "1") },
                { "listingDelay", _globalConfig.Get("DC:listingDelay", "10") },
                { "language", _globalConfig.Get("DC:language", "source") },
            });

            var section = GetDisplayConfigfSection(block, ConfigsSections.DisplayConfig, index);
            if (section == null)
            {
                return global;
            }

            var config = ConfigObject.Parse(section, block.CustomData);
            if (config == null)
            {
                return global;
            }

            return ConfigsHelper.Merge(ConfigsSections.DisplayConfig,
                new List<ConfigObject> { global, config });
        }

        private string GetDisplayConfigfSection(IMyTerminalBlock block, string section, int index = 0)
        {
            if (index == 0 && block.CustomData.Contains(section + "]"))
            {
                return section;
            }

            if (block.CustomData.Contains(section + ":" + index))
            {
                return section + ":" + index;
            }

            return null;
        }

        private void CheckGlobalConfig()
        {
            var configDefault = new ConfigObject(ConfigsSections.GlobalConfig, new Dictionary<string, string>
            {
                { "Tag", "[Base]" },
                { "Ignore", "[!CBM]" },
                { "SD:BaseContainersMaxVolume", "90%" },
                { "DC:font", "Debug" },
                { "DC:fontSize", "0.8" },
                { "DC:lineHeight", "32" },
                { "DC:padding", "10" },
                { "DC:border", "1" },
                { "DC:listingDelay", "5" },
                { "DC:language", "source" }
            });
            var configCurrent = ConfigObject.Parse(ConfigsSections.GlobalConfig, Me.CustomData);
            var configNew = ConfigsHelper.Merge(ConfigsSections.GlobalConfig,
                new List<ConfigObject> { configDefault, configCurrent });

            _globalConfig = configNew;

            var customData = new List<string> { ConfigsHelper.ToCustomData(configNew) };

            var customItems = ConfigObject.Parse(ConfigsSections.CustomItems, Me.CustomData);
            if (customItems != null)
            {
                customData.Add(ConfigsHelper.ToCustomData(customItems));
            }

            Me.CustomData = string.Join("\n\n", customData.ToArray());
        }

        private static string SecondsToString(double seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
        }

        private void AddDebug(string message)
        {
            if (_messages == null)
            {
                _messages = new Dictionary<string, List<string>>();
            }

            if (!_messages.ContainsKey("Debug"))
            {
                _messages["Debug"] = new List<string>();
            }

            _messages["Debug"].Add(message);
        }

        #endregion;
    }
}