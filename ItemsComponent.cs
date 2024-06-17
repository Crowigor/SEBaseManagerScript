using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        public class ItemsManager
        {
            private readonly Dictionary<string, ItemObject> _storage;
            private readonly Dictionary<string, string> _aliases;

            public ItemsManager()
            {
                _storage = new Dictionary<string, ItemObject>();
                _aliases = new Dictionary<string, string>();

                foreach (var item in ItemsDatabase.GetItems())
                {
                    _storage[item.Selector] = item;

                    foreach (var alias in item.Aliases)
                        _aliases[alias] = item.Selector;
                }
            }

            public ItemObject GetItem(string key)
            {
                string selector = null;
                if (_storage.ContainsKey(key))
                    selector = key;
                else if (_aliases.ContainsKey(key))
                    selector = _aliases[key];

                return selector == null ? null : _storage[selector];
            }

            public List<ItemObject> GetList()
            {
                return _storage.Values.ToList();
            }

            public void UpdateItem(ItemObject item)
            {
                if (item == null)
                    return;

                _storage[item.Selector] = item;
            }

            public void ClearInventories()
            {
                foreach (var item in _storage.Values)
                    item.ClearInventories();
            }

            public void ClearAmounts()
            {
                foreach (var item in _storage.Values)
                    item.ClearAmount();
            }

            public void TransferFromInventories(List<IMyInventory> inventories)
            {
                foreach (var inventory in inventories)
                    TransferFromInventory(inventory);
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
        }

        public class ItemObject
        {
            public readonly string Selector;
            public readonly string Name;
            public readonly string Localization;
            public MyItemType Type;
            public readonly List<string> Aliases;
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
                Aliases = new List<string> { Selector, Name, Localization, Type.ToString() };
                Blueprints = new Dictionary<MyDefinitionId, MyFixedPoint>();
                if (blueprints != null)
                {
                    foreach (var entry in blueprints)
                    {
                        var blueprint = MyDefinitionId.Parse(entry.Key);
                        Blueprints[blueprint] = MyFixedPoint.DeserializeString(entry.Value);
                        Aliases.Add(blueprint.ToString());
                    }
                }

                ClearInventories();
                ClearAmount();
            }

            public bool IsCrafteble()
            {
                return (Blueprints.Count > 0);
            }

            public string Title(string language = "local")
            {
                if (language.ToLower() == "source")
                    return Name;

                return Localization;
            }

            public void ClearAmount()
            {
                if (Amounts != null)
                    Amounts.Clear();
                else
                    Amounts = new ItemAmountsObject();
            }

            public void ClearInventories()
            {
                if (Inventories != null)
                    Inventories.Clear();
                else
                    Inventories = new List<IMyInventory>();
            }

            public void Transfer(MyInventoryItem inventoryItem, IMyInventory sourceInventory,
                MyFixedPoint amount = new MyFixedPoint())
            {
                if (Inventories.Count == 0)
                    return;

                InventoryHelper.TransferToInventories(inventoryItem, sourceInventory, Inventories, amount);
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
                            quota = DisassemblingQuota;
                        else if (quota < 0)
                            quota = DisassemblingQuota;
                    }

                    result += ValueToString(quota);
                }

                if (Assembling > 0 || Disassembling > 0)
                {
                    result += " (";
                    if (Assembling > 0)
                        result += "+" + ValueToString(Assembling);

                    if (Disassembling > 0)
                        result += "0" + ValueToString(Disassembling);

                    result += ")";
                }

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
                    return (result / 1000000.0).ToString("0.#") + "M";

                if (result >= 1000)
                    return (result / 1000.0).ToString("0.#") + "K";

                return result.ToString("0");
            }
        }
    }
}