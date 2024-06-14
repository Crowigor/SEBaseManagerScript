using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        public class InventoryHelper
        {
            public static MyFixedPoint TransferToInventories(MyInventoryItem inventoryItem, IMyInventory sourceInventory, List<IMyInventory> destanationInventories, MyFixedPoint amount = new MyFixedPoint())
            {
                MyFixedPoint zero = MyFixedPoint.Zero;
                MyFixedPoint result = inventoryItem.Amount;

                foreach (IMyInventory destinationInventory in destanationInventories)
                {
                    if (destinationInventory == sourceInventory)
                    {
                        if (amount == zero)
                            return zero;

                        MyFixedPoint exist = destinationInventory.GetItemAmount(inventoryItem.Type);
                        if (amount <= exist)
                            return zero;

                        result -= exist;

                        continue;
                    }

                    MyFixedPoint? transfer = TransferItem(inventoryItem, sourceInventory, destinationInventory, amount);
                    if (transfer != null)
                    {
                        if (transfer == zero)
                            return zero;

                        result = (MyFixedPoint)transfer;
                    }
                }

                return result;
            }

            public static MyFixedPoint? TransferItem(MyInventoryItem item, IMyInventory sourceInventory, IMyInventory destinationInventory, MyFixedPoint amount = new MyFixedPoint())
            {
                MyFixedPoint before = GetItemAmout(item, sourceInventory);
                MyFixedPoint zero = MyFixedPoint.Zero;
                if (sourceInventory.CanTransferItemTo(destinationInventory, item.Type) && !destinationInventory.IsFull)
                {
                    bool transfer = (amount == zero || before <= amount) ? sourceInventory.TransferItemTo(destinationInventory, item) : sourceInventory.TransferItemTo(destinationInventory, item, amount);
                    if (transfer)
                    {
                        MyFixedPoint after = GetItemAmout(item, sourceInventory);
                        if (amount == zero || amount == before)
                        {
                            return after;
                        }

                        if (amount > before)
                        {
                            return after + (amount - before);
                        }

                        MyFixedPoint result = (after > 0) ? before - after : before;

                        return amount - result;
                    }
                }

                return null;
            }

            public static MyFixedPoint GetItemAmout(MyInventoryItem item, IMyInventory inventory)
            {
                var findItem = inventory.GetItemByID(item.ItemId);

                return (findItem.HasValue) ? findItem.Value.Amount : MyFixedPoint.Zero;
            }

            public static MyFixedPoint? TransferFromBlocks(MyItemType type, List<IMyTerminalBlock> blocks, IMyInventory destanationInventory, MyFixedPoint amount = new MyFixedPoint())
            {
                MyFixedPoint zero = new MyFixedPoint();
                if (amount == zero || blocks.Count == 0)
                    return null;

                MyFixedPoint current = destanationInventory.GetItemAmount(type);
                amount -= current;

                if (amount <= zero)
                    return amount;

                foreach (IMyTerminalBlock block in blocks)
                {
                    if (block is IMyEntity)
                    {
                        int invertoryCounts = block.InventoryCount;
                        if (invertoryCounts > 0)
                        {
                            for (int i = 0; i < invertoryCounts; i++)
                            {
                                IMyInventory sourceInventory = block.GetInventory(i);
                                List<MyInventoryItem> sourceInvertoryItems = new List<MyInventoryItem>();
                                sourceInventory.GetItems(sourceInvertoryItems, b => b.Type == type);

                                foreach (MyInventoryItem? inventoryItem in sourceInvertoryItems)
                                {
                                    if (inventoryItem != null)
                                    {
                                        MyFixedPoint? transfer = InventoryHelper.TransferItem((MyInventoryItem)inventoryItem, (IMyInventory)sourceInventory, destanationInventory, amount);
                                        if (transfer != null)
                                            amount = (MyFixedPoint)transfer;

                                        if (amount <= zero)
                                            return amount;
                                    }
                                }
                            }
                        }
                    }
                }
                return amount;
            }
        }
    }
}