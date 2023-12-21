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
    partial class Program
    {
        public class ItemsList
        {
            Dictionary<string, Item> Storage { get; set; }
            Dictionary<string, string> Aliases;
            public BlocksHelper Blocks { get; set; }
            public DebugHelper Debug { get; set; }

            public ItemsList()
            {
                Storage = new Dictionary<string, Item>();
                Aliases = new Dictionary<string, string>();
                Blocks = null;
                Debug = new DebugHelper();

                foreach (Item item in ItemsDB.getList())
                {
                    Storage.Add(item.Selector, item);
                    Aliases[item.Name] = item.Selector;
                    Aliases[item.Localization] = item.Selector;
                    Aliases[item.Type.ToString()] = item.Selector;

                    if (item.Blueprints.Count > 0)
                    {
                        foreach (KeyValuePair<MyDefinitionId, MyFixedPoint> entry in item.Blueprints)
                        {
                            Aliases[entry.Key.ToString()] = item.Selector;
                        }
                    }
                }
            }

            public Item GetItem(string key, bool calculateAmout = false)
            {
                string selector = null;
                if (Storage.ContainsKey(key))
                {
                    selector = key;
                }
                else if (Aliases.ContainsKey(key))
                {
                    selector = Aliases[key];
                }

                if (selector == null)
                {
                    return null;
                }

                Item item = Storage[selector];
                if (Blocks != null && (calculateAmout || item.IsNewAmout))
                {
                    item.CalculateAmout(Blocks);
                    Storage[selector] = item;
                }

                return item;
            }

            public void ResetData()
            {
                List<string> itemsList = new List<string>(Storage.Keys);
                foreach (string key in itemsList)
                {
                    Storage[key].ResetData();
                }
                Blocks = null;
            }

            public void ClearAmouts()
            {
                List<string> itemsList = new List<string>(Storage.Keys);
                foreach (string key in itemsList)
                {
                    Storage[key].ClearAmout();
                }
            }

            public void TrasferFromInventories(List<IMyInventory> inventories)
            {
                foreach (IMyInventory inventory in inventories)
                {
                    TrasferFromInventory(inventory);
                }
            }

            public void TrasferFromInventory(IMyInventory inventory)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inventory.GetItems(items);
                foreach (MyInventoryItem item in items)
                {
                    Item find = GetItem(item.Type.ToString());
                    if (find != null)
                    {
                        find.Transfer(item, inventory);
                    }
                }
            }
        }
    }
}
