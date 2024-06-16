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
            public static List<string> Displays = new List<string> { DisplayConfig, DisplayStatus, DisplayItems };
        }

        private readonly TasksManager _tasks;
        private BlocksManager _blocks;
        private ItemsManager _items;
        private Dictionary<long, DisplayObject> _displays;
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
            _tasks.Add("Connectors Manager", TaskConnectorsManager, 6);
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

            if (_displays == null)
                _displays = new Dictionary<long, DisplayObject>();

            if (_messages == null)
                _messages = new Dictionary<string, List<string>>();
            else
                _messages.Clear();


            // Add inventories to items
            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.GasTanks))
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

            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.Containers))
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
            var displays = new Dictionary<long, DisplayObject>();
            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.Displays))
            {
                if (!ConfigsSections.Displays.Any(key => terminalBlock.CustomData.Contains(key)))
                    continue;

                var selector = terminalBlock.GetId();
                var delay = terminalBlock.CustomData.Contains(ConfigsSections.DisplayStatus) ? 1 : 5;

                DisplayObject display;
                if (_displays.ContainsKey(selector))
                {
                    display = _displays[selector];
                    display.UpdateDataDelay = delay;
                }
                else
                    display = new DisplayObject(selector, delay);

                displays[selector] = display;

                var configDefault = new ConfigObject(ConfigsSections.DisplayConfig, new Dictionary<string, string>
                {
                    { "title", "" },
                    { "font", "Debug" },
                    { "fontSize", "0.8" },
                    { "lineHeight", "32" },
                    { "padding", "10" },
                    { "border", "1" },
                });
                var configCurrent = ConfigObject.Parse(ConfigsSections.DisplayConfig, terminalBlock.CustomData);
                var config = ConfigsHelper.Merge(ConfigsSections.DisplayConfig,
                    new List<ConfigObject> { configDefault, configCurrent });
                terminalBlock.CustomData = ConfigsHelper.ToCustomData(config, terminalBlock.CustomData);
            }

            _displays = displays;
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
        }

        private void TaskInventoryManager()
        {
            var inventories = new List<IMyInventory>();
            foreach (var terminalBlock in _blocks.GetBlocks())
            {
                if (!terminalBlock.IsFunctional || terminalBlock is IMyLargeConveyorTurretBase)
                    continue;

                if (terminalBlock is IMyGasGenerator)
                {
                    var inventory = terminalBlock.GetInventory(0);
                    var items = new List<MyInventoryItem>();
                    inventory.GetItems(items);
                    foreach (var item in items)
                    {
                        var find = _items.GetItem(item.Type.ToString());
                        if (find != null && find.Selector != "Ore/Ice")
                            find.Transfer(item, inventory);
                    }
                }
                else if (terminalBlock is IMyAssembler)
                    inventories.Add(terminalBlock.GetInventory(1));
                else if (terminalBlock is IMyRefinery)
                    inventories.Add(terminalBlock.GetInventory(1));
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
            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.Assemblers))
            {
                if (!terminalBlock.IsWorking)
                    continue;

                var assembler = (IMyAssembler)terminalBlock;

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

            foreach (var terminalBlock in _blocks.GetBlocks(BlocksManager.BlockType.Connectors))
            {
                if (!terminalBlock.IsWorking)
                    return;

                var connector = (IMyShipConnector)terminalBlock;

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
                        _messages["Stop Drones"].Add(connector.CustomName + ":");
                        _messages["Stop Drones"].Add(error);
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
                        _messages["Stop Drones"].Add(connector.CustomName + ":");
                        _messages["Stop Drones"].Add("Can't find drones blocks `" + droneBlocksName + "`");
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

        private void TaskDisplaysManager()
        {
            foreach (var selector in _displays.Keys.ToList())
            {
                var terminalBlock = _blocks.GetBlock(selector, BlocksManager.BlockType.Displays);

                if (terminalBlock == null || !terminalBlock.IsWorking ||
                    !ConfigsSections.Displays.Any(key => terminalBlock.CustomData.Contains(key)))
                    continue;

                // Update display lines
                var displayObject = _displays[selector];
                displayObject.UpdateDataCurrentTick++;
                if (displayObject.UpdateDataCurrentTick >= displayObject.UpdateDataDelay)
                {
                    displayObject.Lines.Clear();
                    if (terminalBlock.CustomData.Contains(ConfigsSections.DisplayStatus))
                    {
                        var i = 0;
                        foreach (var line in _tasks.GetStatusText())
                        {
                            i++;
                            displayObject.Lines[i] = new List<MySprite> { new MySprite { Data = line } };
                        }

                        foreach (var messageSection in _messages)
                        {
                            if (messageSection.Value.Count == 0)
                                continue;

                            i++;
                            displayObject.Lines[i] = new List<MySprite>();

                            i++;
                            displayObject.Lines[i] = new List<MySprite>
                                { new MySprite { Data = messageSection.Key + ":" } };

                            foreach (var message in messageSection.Value.ToList())
                            {
                                i++;
                                displayObject.Lines[i] = new List<MySprite>
                                    { new MySprite { Data = message, Alignment = TextAlignment.LEFT, } };
                            }
                        }
                    }
                    else
                    {
                        var i = 0;
                        var configs = ConfigsHelper.GetSections(terminalBlock.CustomData);
                        foreach (var configSection in configs)
                        {
                            if (configSection.Value.Count == 0)
                                continue;

                            foreach (var line in configSection.Value.ToList())
                            {
                                var clear = line.Trim();
                                if (string.IsNullOrEmpty(clear))
                                {
                                    displayObject.Lines[i] = new List<MySprite>();
                                    i++;
                                    continue;
                                }

                                if (configSection.Key == ConfigsSections.DisplayItems)
                                {
                                    var item = _items.GetItem(clear);
                                    if (item != null)
                                    {
                                        displayObject.Lines[i] = new List<MySprite>();
                                        displayObject.Lines[i].Add(new MySprite { Data = clear });

                                        string amount = AmountToString(item.Amounts.Exist);
                                        if (item.Amounts.AssemblingQuota > 0 || item.Amounts.DisassemblingQuota > 0)
                                        {
                                            amount += "/";
                                            MyFixedPoint quota = -1;
                                            if (item.Amounts.AssemblingQuota >= 0)
                                            {
                                                quota = item.Amounts.AssemblingQuota;
                                            }

                                            if (item.Amounts.DisassemblingQuota >= 0)
                                            {
                                                if (quota >= 0 && item.Amounts.DisassemblingQuota < quota)
                                                    quota = item.Amounts.DisassemblingQuota;
                                                else if (quota < 0)
                                                    quota = item.Amounts.DisassemblingQuota;
                                            }

                                            amount += AmountToString(quota);
                                        }

                                        if (item.Amounts.Assembling > 0 || item.Amounts.Disassembling > 0)
                                        {
                                            amount += " (";
                                            if (item.Amounts.Assembling > 0)
                                                amount += "+" + AmountToString(item.Amounts.Assembling);

                                            if (item.Amounts.Disassembling > 0)
                                                amount += "0" + AmountToString(item.Amounts.Disassembling);

                                            amount += ")";
                                        }

                                        displayObject.Lines[i].Add(new MySprite
                                            { Data = amount, Alignment = TextAlignment.RIGHT });
                                    }

                                    i++;
                                }
                            }
                        }
                    }
                }

                _displays[selector] = displayObject;

                var config = ConfigObject.Parse(ConfigsSections.DisplayConfig, terminalBlock.CustomData);
                var display = (IMyTextPanel)terminalBlock;
                var viewport = new RectangleF((display.TextureSize - display.SurfaceSize) / 2f, display.SurfaceSize);
                var padding = float.Parse(config.Get("padding"));
                var font = config.Get("font");
                var fontSize = float.Parse(config.Get("fontSize"));
                var lineHeight = float.Parse(config.Get("lineHeight"));
                var border = float.Parse(config.Get("border"));
                var positionLeft = viewport.X + padding;
                var positionRight = viewport.X + viewport.Width - padding;
                var positionTop = viewport.Y + padding;
                var positionBottom = viewport.Y + viewport.Height - padding;

                var frame = display.DrawFrame();
                // Main border
                if (border > 0)
                {
                    var outerRectSize = new Vector2(viewport.Width - 2 * padding, viewport.Height - 2 * padding);
                    var innerRectSize =
                        new Vector2(outerRectSize.X - 2 * border, outerRectSize.Y - 2 * border);
                    var rectPosition = new Vector2(viewport.X + viewport.Width / 2, viewport.Y + viewport.Height / 2);
                    var outerRectSprite = new MySprite
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = rectPosition,
                        Size = outerRectSize,
                        Color = display.ScriptForegroundColor,
                        Alignment = TextAlignment.CENTER
                    };
                    var innerRectSprite = new MySprite
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = rectPosition,
                        Size = innerRectSize,
                        Color = display.ScriptBackgroundColor,
                        Alignment = TextAlignment.CENTER
                    };
                    frame.Add(outerRectSprite);
                    frame.Add(innerRectSprite);

                    positionLeft = viewport.X + border + padding * 2;
                    positionRight = viewport.X + viewport.Width - border - padding * 2;
                    positionTop = viewport.Y + border + padding * 2;
                    positionBottom = viewport.Y + viewport.Height - border - padding * 2;
                }

                // Title
                var title = config.Get("title");
                if (!string.IsNullOrEmpty(title))
                {
                    var titleWidth = title.Length * 15 * fontSize;
                    var titleSize = new Vector2(titleWidth, lineHeight);
                    var titlePosition = new Vector2(viewport.X + viewport.Width / 2,
                        viewport.Y + lineHeight / 2 * fontSize);
                    var titleSprite = new MySprite
                    {
                        Type = SpriteType.TEXT,
                        Data = title,
                        Position = titlePosition,
                        RotationOrScale = fontSize,
                        Color = display.ScriptForegroundColor,
                        FontId = font,
                        Alignment = TextAlignment.CENTER
                    };

                    if (border > 0)
                    {
                        var titleRectPosition = new Vector2(titlePosition.X, viewport.Y + padding + lineHeight / 2);
                        var titleOuterRectSize = new Vector2(titleSize.X + 2 * padding, lineHeight);
                        var titleInnerRectSize = new Vector2(titleOuterRectSize.X - 2 * border,
                            lineHeight - 2 * border);

                        var titleOuterRectSprite = new MySprite
                        {
                            Type = SpriteType.TEXTURE,
                            Data = "SquareSimple",
                            Position = titleRectPosition,
                            Size = titleOuterRectSize,
                            Color = display.ScriptForegroundColor,
                            Alignment = TextAlignment.CENTER
                        };
                        var titleInnerRectSprite = new MySprite
                        {
                            Type = SpriteType.TEXTURE,
                            Data = "SquareSimple",
                            Position = titleRectPosition,
                            Size = titleInnerRectSize,
                            Color = display.ScriptBackgroundColor,
                            Alignment = TextAlignment.CENTER
                        };
                        frame.Add(titleOuterRectSprite);
                        frame.Add(titleInnerRectSprite);
                    }

                    frame.Add(titleSprite);

                    positionTop = viewport.Y + lineHeight + padding * 2;
                }

                // Content
                var limit = (int)Math.Round((positionBottom - positionTop) / lineHeight);
                foreach (var line in displayObject.GetLines(limit))
                {
                    if (line.Value.Count == 0)
                    {
                        positionTop += lineHeight;
                        continue;
                    }

                    foreach (var sprite in line.Value)
                    {
                        var printSprite = sprite;
                        printSprite.Type = SpriteType.TEXT;
                        printSprite.Size = new Vector2(positionLeft, positionTop);
                        printSprite.FontId = font;
                        printSprite.RotationOrScale = fontSize;
                        printSprite.Color = display.ScriptForegroundColor;

                        printSprite.Position = new Vector2(positionLeft, positionTop);
                        if (printSprite.Alignment == TextAlignment.RIGHT)
                        {
                            printSprite.Position = new Vector2(positionRight, positionTop);
                        }

                        frame.Add(printSprite);
                    }

                    positionTop += lineHeight;
                }

                // Draw
                display.ContentType = ContentType.SCRIPT;
                display.Script = "";
                frame.Dispose();
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
                foreach (var entry in _messages)
                {
                    if (entry.Value.Count == 0)
                        continue;

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
            var configDefault = new ConfigObject(ConfigsSections.GlobalConfig, new Dictionary<string, string>
            {
                { "Tag", "[Base]" },
                { "Ignore", "[!CBM]" },
                { "SD:BaseContainersMaxVolume", "90%" },
            });
            var configCurrent = ConfigObject.Parse(ConfigsSections.GlobalConfig, Me.CustomData);
            var configNew = ConfigsHelper.Merge(ConfigsSections.GlobalConfig,
                new List<ConfigObject> { configDefault, configCurrent });

            _globalConfig = configNew;
            Me.CustomData = ConfigsHelper.ToCustomData(configNew, ";Crowigor's Base Manager");
        }

        private string AmountToString(MyFixedPoint amount)
        {
            double value = (double)amount;

            if (value >= 1000000)
                return (value / 1000000.0).ToString("0.#") + "M";

            if (value >= 1000)
                return (value / 1000.0).ToString("0.#") + "K";

            return value.ToString("0");
        }

        #endregion;
    }
}