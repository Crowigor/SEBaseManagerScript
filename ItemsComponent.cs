using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        public class ItemsManager
        {
            public const string CustomItemsPrefix = "CM_CI";
            public const string UnknownCustomItem = "UNKNOWN_ITEM";

            private readonly Dictionary<string, ItemObject> _storage;
            private readonly Dictionary<string, string> _aliases;

            public ItemsManager(List<ItemObject> customItems = null)
            {
                List<ItemObject> items;
                if (customItems == null || customItems.Count == 0)
                {
                    items = ItemsDatabase.GetItems();
                }
                else
                {
                    var dictionary = new Dictionary<string, ItemObject>();
                    foreach (var item in ItemsDatabase.GetItems())
                    {
                        dictionary[item.Selector] = item;
                    }

                    foreach (var item in customItems)
                    {
                        if (!dictionary.ContainsKey(item.Selector))
                        {
                            dictionary[item.Selector] = item;

                            continue;
                        }

                        var update = dictionary[item.Selector];
                        var needUpdate = false;
                        if (item.Name != update.Name && item.Name != item.Type.SubtypeId)
                        {
                            update.Name = item.Name;
                            needUpdate = true;
                        }

                        if (item.Localization != update.Localization && item.Localization != item.Type.SubtypeId)
                        {
                            update.Localization = item.Localization;
                            needUpdate = true;
                        }

                        if (item.Blueprints.Count > 0)
                        {
                            needUpdate = true;
                            foreach (var keyPair in item.Blueprints)
                            {
                                update.Blueprints[keyPair.Key] = keyPair.Value;
                            }
                        }

                        if (!needUpdate)
                        {
                            continue;
                        }

                        update.UpdateAliases();

                        dictionary[update.Selector] = update;
                    }

                    items = dictionary.Values.ToList();
                }

                _storage = new Dictionary<string, ItemObject>();
                _aliases = new Dictionary<string, string>();

                foreach (var item in items)
                {
                    _storage[item.Selector] = item;

                    foreach (var alias in item.Aliases)
                    {
                        _aliases[alias] = item.Selector;
                    }
                }
            }

            public ItemObject GetItem(string key)
            {
                var keyLower = key.ToLower();
                string selector = null;
                if (_storage.ContainsKey(key))
                {
                    selector = key;
                }
                else if (_aliases.ContainsKey(keyLower))
                {
                    selector = _aliases[keyLower];
                }

                return selector == null ? null : _storage[selector];
            }

            public List<ItemObject> GetList()
            {
                return _storage.Values.ToList();
            }

            public void UpdateItem(ItemObject item)
            {
                if (item == null)
                {
                    return;
                }

                _storage[item.Selector] = item;
            }

            public void ClearInventories()
            {
                foreach (var item in _storage.Values)
                {
                    item.ClearInventories();
                }
            }

            public void ClearAmounts()
            {
                foreach (var item in _storage.Values)
                {
                    item.ClearAmount();
                }
            }

            public void TransferFromInventories(List<IMyInventory> inventories)
            {
                foreach (var inventory in inventories)
                {
                    TransferFromInventory(inventory);
                }
            }

            public void TransferFromInventory(IMyInventory inventory)
            {
                var items = new List<MyInventoryItem>();
                inventory.GetItems(items);
                foreach (var item in items)
                {
                    var find = GetItem(item.Type.ToString());
                    find?.Transfer(item, inventory);
                }
            }

            public static List<ItemObject> GetCustomItemsFromString(string input)
            {
                var result = new List<ItemObject>();
                if (string.IsNullOrEmpty(input) || !input.Contains(CustomItemsPrefix))
                {
                    return result;
                }

                var lines = input.Split('\n');
                foreach (var line in lines)
                {
                    if (!line.StartsWith(CustomItemsPrefix))
                    {
                        continue;
                    }

                    var parts = line.Split(new[] { ':' }, 6);
                    if (parts.Length == 1)
                    {
                        continue;
                    }

                    var type = parts[1].Trim();
                    if (string.IsNullOrEmpty(type) || type == UnknownCustomItem)
                    {
                        continue;
                    }

                    MyItemType itemType;
                    try
                    {
                        itemType = MyItemType.Parse(type);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    type = itemType.ToString();

                    var name = itemType.SubtypeId;
                    if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2].Trim()))
                    {
                        name = parts[2].Trim();
                    }

                    var localization = itemType.SubtypeId;
                    if (parts.Length > 3 && !string.IsNullOrEmpty(parts[3].Trim()))
                    {
                        localization = parts[3].Trim();
                    }

                    Dictionary<string, string> blueprints = null;
                    if (parts.Length > 4 && !string.IsNullOrEmpty(parts[4].Trim()))
                    {
                        var blueprintId = parts[4].Trim();
                        var blueprintRate = "1";
                        if (parts.Length > 5 && !string.IsNullOrEmpty(parts[5].Trim()))
                        {
                            blueprintRate = parts[5].Trim();
                        }

                        blueprints = new Dictionary<string, string>
                        {
                            [blueprintId] = blueprintRate
                        };
                    }

                    result.Add(new ItemObject(name, localization, type, blueprints));
                }

                return result;
            }

            public static List<string> ScanCustomItems(
                IMyGridTerminalSystem gridTerminalSystem, string blocksName = "ScanItems",
                string title = "[Custom Items]")
            {
                var messages = new List<string>
                {
                    "INFO - Starting Custom items scanner."
                };

                var blocks = new List<IMyTerminalBlock>();
                gridTerminalSystem.SearchBlocksOfName(blocksName, blocks);
                if (blocks.Count == 0)
                {
                    messages.Add("ERROR - Blocks with `" + blocksName + "` not found!");

                    return messages;
                }

                IMyTextPanel display = null;
                var manager = new ItemsManager();
                var itemsType = new Dictionary<string, MyItemType>();
                var blueprintsType = new Dictionary<string, MyDefinitionId>();
                foreach (var block in blocks)
                {
                    if (block is IMyCargoContainer)
                    {
                        var inventory = block.GetInventory(0);
                        if (inventory == null)
                        {
                            continue;
                        }

                        var inventoryItems = new List<MyInventoryItem>();
                        inventory.GetItems(inventoryItems);
                        foreach (var item in inventoryItems)
                        {
                            var key = item.Type.ToString();
                            if (manager.GetItem(key) == null)
                            {
                                itemsType[key] = item.Type;
                            }
                        }

                        continue;
                    }

                    var assembler = block as IMyProductionBlock;
                    if (assembler != null)
                    {
                        var queue = new List<MyProductionItem>();
                        assembler.GetQueue(queue);
                        foreach (var item in queue)
                        {
                            var key = item.BlueprintId.ToString();
                            if (manager.GetItem(key) == null)
                            {
                                blueprintsType[key] = item.BlueprintId;
                            }
                        }

                        continue;
                    }

                    var panel = block as IMyTextPanel;
                    if (panel != null)
                    {
                        display = panel;
                    }
                }

                if (display == null)
                {
                    messages.Add("ERROR - Display with `" + blocksName + "` not found");

                    return messages;
                }

                if (itemsType.Count == 0)
                {
                    messages.Add("WARNING - New Items not found");
                }
                else
                {
                    messages.Add("INFO - Find " + itemsType.Count + " new Item(s)");
                }

                if (blueprintsType.Count == 0)
                {
                    messages.Add("WARNING - New Blueprints not found");
                }
                else
                {
                    messages.Add("INFO - Find " + blueprintsType.Count + " new Blueprint(s)");
                }

                if (blueprintsType.Count == 0 && itemsType.Count == 0)
                {
                    return messages;
                }

                var lines = new List<string> { title };
                var blueprintsFind = new List<string>();

                if (itemsType.Count > 0)
                {
                    foreach (var type in itemsType.Values)
                    {
                        var subtype = type.SubtypeId;
                        var findBlueprints = false;

                        foreach (var blueprint in blueprintsType.Keys)
                        {
                            if (!blueprint.Contains(subtype))
                            {
                                continue;
                            }

                            findBlueprints = true;
                            blueprintsFind.Add(blueprint);
                            lines.Add(string.Join(":", CustomItemsPrefix, type, subtype, subtype, blueprint, "1"));
                        }

                        if (!findBlueprints)
                        {
                            lines.Add(string.Join(":", CustomItemsPrefix, type, subtype, subtype));
                        }
                    }
                }

                if (blueprintsType.Count > 0)
                {
                    foreach (var type in blueprintsType.Values)
                    {
                        var key = type.ToString();
                        var subtype = type.SubtypeId.ToString();
                        if (blueprintsFind.Contains(key))
                        {
                            continue;
                        }

                        lines.Add(string.Join(":", CustomItemsPrefix, UnknownCustomItem, subtype, subtype, key, "1"));
                    }
                }

                display.ContentType = ContentType.TEXT_AND_IMAGE;
                display.WriteText(string.Join("\n", lines.ToArray()));
                messages.Add("SUCCESS - Result add to " + display.CustomName);

                return messages;
            }

            public static List<string> PrintItems(ItemsManager manager,
                IMyGridTerminalSystem gridTerminalSystem, string blocksName = "PrintItems",
                string title = "[All Items]")
            {
                var messages = new List<string>
                {
                    "INFO - Starting print manager items."
                };

                var blocks = new List<IMyTerminalBlock>();
                gridTerminalSystem.SearchBlocksOfName(blocksName, blocks);
                if (blocks.Count == 0)
                {
                    messages.Add("ERROR - Blocks with `" + blocksName + "` not found!");

                    return messages;
                }

                IMyTextPanel display = null;
                foreach (var block in blocks)
                {
                    var panel = block as IMyTextPanel;
                    if (panel != null)
                    {
                        display = panel;
                    }
                }

                if (display == null)
                {
                    messages.Add("ERROR - Display with `" + blocksName + "` not found");

                    return messages;
                }

                var list = manager.GetList();
                messages.Add("INFO - Find " + list.Count + " items");

                var lines = new List<string> { title };
                foreach (var itemObject in list)
                {
                    lines.AddRange(itemObject.ToStringList());
                }

                display.ContentType = ContentType.TEXT_AND_IMAGE;
                display.WriteText(string.Join("\n", lines.ToArray()));
                messages.Add("SUCCESS - Result add to " + display.CustomName);

                return messages;
            }
        }

        public class ItemObject
        {
            public readonly string Selector;
            public string Name;
            public string Localization;
            public MyItemType Type;
            public List<string> Aliases;
            public readonly Dictionary<MyDefinitionId, MyFixedPoint> Blueprints;
            public List<IMyInventory> Inventories;
            public ItemAmountsObject Amounts;

            public ItemObject(string name, string localization, string type,
                Dictionary<string, string> blueprints = null)
            {
                Selector = type.Replace("MyObjectBuilder_", "");
                Name = name;
                Localization = localization;
                Type = MyItemType.Parse(type);
                Blueprints = new Dictionary<MyDefinitionId, MyFixedPoint>();
                if (blueprints != null)
                {
                    foreach (var entry in blueprints)
                    {
                        var blueprint = MyDefinitionId.Parse(entry.Key);
                        Blueprints[blueprint] = MyFixedPoint.DeserializeString(entry.Value);
                    }
                }

                UpdateAliases();
                ClearInventories();
                ClearAmount();
            }

            public void UpdateAliases()
            {
                Aliases = new List<string>
                {
                    Selector.ToLower(),
                    Name.ToLower(),
                    Localization.ToLower(),
                    Type.ToString().ToLower(),
                };

                if (Blueprints.Count <= 0)
                {
                    return;
                }

                foreach (var blueprint in Blueprints.Keys)
                {
                    Aliases.Add(blueprint.ToString().ToLower());
                }
            }

            public bool IsCraftable()
            {
                return (Blueprints.Count > 0);
            }

            public string Title(string language = "local")
            {
                if (language.ToLower() == "source")
                {
                    return Name;
                }

                return Localization;
            }

            public void ClearAmount()
            {
                if (Amounts != null)
                {
                    Amounts.Clear();
                }
                else
                {
                    Amounts = new ItemAmountsObject();
                }
            }

            public void ClearInventories()
            {
                if (Inventories != null)
                {
                    Inventories.Clear();
                }
                else
                {
                    Inventories = new List<IMyInventory>();
                }
            }

            public void Transfer(MyInventoryItem inventoryItem, IMyInventory sourceInventory,
                MyFixedPoint amount = new MyFixedPoint())
            {
                if (Inventories.Count == 0)
                {
                    return;
                }

                InventoryHelper.TransferToInventories(inventoryItem, sourceInventory, Inventories, amount);
            }

            public List<string> ToStringList()
            {
                var lines = new List<string>();
                if (Blueprints.Count == 0)
                {
                    lines.Add(string.Join(":", ItemsManager.CustomItemsPrefix, Type.ToString(), Name, Localization));

                    return lines;
                }

                foreach (var blueprint in Blueprints)
                {
                    lines.Add(string.Join(":", ItemsManager.CustomItemsPrefix, Type.ToString(), Name, Localization,
                        blueprint.Key.ToString(), blueprint.Value));
                }

                return lines;
            }
        }

        public class ItemAmountsObject
        {
            public bool IsNew;
            public MyFixedPoint Exist;
            public MyFixedPoint Assembling;
            public MyFixedPoint AssemblingQuota;
            public MyFixedPoint Disassembling;
            public MyFixedPoint DisassemblingQuota;

            public ItemAmountsObject()
            {
                Clear();
            }

            public override string ToString()
            {
                var result = ValueToString(Exist);
                if (AssemblingQuota > 0 || DisassemblingQuota > 0)
                {
                    result += "/";
                    MyFixedPoint quota = -1;
                    if (AssemblingQuota >= 0)
                    {
                        quota = AssemblingQuota;
                    }

                    if (DisassemblingQuota >= 0)
                    {
                        if (quota >= 0 && DisassemblingQuota < quota)
                        {
                            quota = DisassemblingQuota;
                        }
                        else if (quota < 0)
                        {
                            quota = DisassemblingQuota;
                        }
                    }

                    result += ValueToString(quota);
                }

                if (Assembling <= 0 && Disassembling <= 0)
                {
                    return result;
                }

                result += " (";
                if (Assembling > 0)
                {
                    result += "+" + ValueToString(Assembling);
                }

                if (Disassembling > 0)
                {
                    result += "0" + ValueToString(Disassembling);
                }

                result += ")";

                return result;
            }

            public void Clear()
            {
                IsNew = true;
                Exist = MyFixedPoint.Zero;
                Assembling = MyFixedPoint.Zero;
                AssemblingQuota = -1;
                Disassembling = MyFixedPoint.Zero;
                DisassemblingQuota = -1;
            }

            public static string ValueToString(MyFixedPoint value)
            {
                var result = (double)value;

                if (result >= 1000000)
                {
                    return (result / 1000000.0).ToString("0.#") + "M";
                }

                if (result >= 1000)
                {
                    return (result / 1000.0).ToString("0.#") + "K";
                }

                return result.ToString("0");
            }
        }
    }
}