﻿using Sandbox.Game;
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
        public class ItemsManager
        {
            private Dictionary<string, ItemObject> m_Storage;
            private Dictionary<string, string> m_Aliases;

            public ItemsManager()
            {
                m_Storage = new Dictionary<string, ItemObject>();
                m_Aliases = new Dictionary<string, string>();

                foreach (ItemObject item in ItemsDatabase.GetItems())
                {
                    m_Storage[item.Selector] = item;
                    m_Aliases[item.Name] = item.Selector;
                    m_Aliases[item.Localization] = item.Selector;
                    m_Aliases[item.Type.ToString()] = item.Selector;

                    if (item.Blueprints.Count > 0)
                    {
                        foreach (KeyValuePair<MyDefinitionId, MyFixedPoint> entry in item.Blueprints)
                        {
                            m_Aliases[entry.Key.ToString()] = item.Selector;
                        }
                    }
                }
            }

            public ItemObject GetItem(string key)
            {
                string selector = null;
                if (m_Storage.ContainsKey(key))
                    selector = key;
                else if (m_Aliases.ContainsKey(key))
                    selector = m_Aliases[key];

                if (selector == null)
                    return null;

                ItemObject item = m_Storage[selector];

                return item;
            }

            public List<ItemObject> GetList()
            {
                return m_Storage.Values.ToList();
            }
            public void UpdateItem(ItemObject item)
            {
                if (item == null)
                    return;

                m_Storage[item.Selector] = item;
            }

            public void ClearInventories()
            {
                foreach (ItemObject item in m_Storage.Values)
                {
                    item.ClearInventories();
                }
            }

            public void ClearAmounts()
            {
                foreach (ItemObject item in m_Storage.Values)
                {
                    item.ClearAmount();
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
                    ItemObject find = GetItem(item.Type.ToString());
                    if (find != null)
                        find.Transfer(item, inventory);
                }
            }
        }

        public class ItemObject
        {
            public string Selector { get; set; }
            public string Name { get; set; }
            public string Localization { get; set; }
            public MyItemType Type { get; set; }
            public List<string> Aliases { get; set; }
            public Dictionary<MyDefinitionId, MyFixedPoint> Blueprints { get; set; }
            public List<IMyInventory> Inventories { get; set; }
            public ItemAmountsObject Amounts { get; set; }

            public bool IsNewAmount { get; set; }

            public ItemObject(string name, string localization, string type, Dictionary<string, string> blueprints = null)
            {
                Selector = type.Replace("MyObjectBuilder_", ""); ;
                Name = name;
                Localization = localization;
                Type = MyItemType.Parse(type);
                Aliases = new List<string>() { Selector, Name, Localization, Type.ToString() };
                Blueprints = new Dictionary<MyDefinitionId, MyFixedPoint>();
                if (blueprints != null)
                {
                    foreach (KeyValuePair<string, string> entry in blueprints)
                    {
                        Blueprints[MyDefinitionId.Parse(entry.Key)] = MyFixedPoint.DeserializeString(entry.Value);
                    }
                }

                ClearInventories();
                ClearAmount();
            }

            public bool IsCrafteble()
            {
                return (Blueprints.Count > 0);
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

            public MyFixedPoint Transfer(MyInventoryItem inventoryItem, IMyInventory sourceInventory, MyFixedPoint amount = new MyFixedPoint())
            {
                if (Inventories.Count == 0)
                    return amount;

                MyFixedPoint zero = MyFixedPoint.Zero;

                return InventoryHelper.TransferToInventories(inventoryItem, sourceInventory, Inventories, amount);
            }


        }

        public class ItemAmountsObject
        {
            public bool IsNew { get; set; }
            public MyFixedPoint Exist { get; set; }
            public MyFixedPoint Assembling { get; set; }
            public MyFixedPoint AssemblingQuota { get; set; }
            public MyFixedPoint Disassembling { get; set; }
            public MyFixedPoint DisassemblingQuota { get; set; }

            public ItemAmountsObject()
            {
                Clear();
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
        }
    }
}
