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
            public static MyFixedPoint TransferToInventories(MyInventoryItem inventoryItem,
                IMyInventory sourceInventory, List<IMyInventory> destinationInventories,
                MyFixedPoint amount = new MyFixedPoint())
            {
                var zero = MyFixedPoint.Zero;
                var result = inventoryItem.Amount;

                foreach (var destinationInventory in destinationInventories)
                {
                    if (destinationInventory == sourceInventory)
                    {
                        if (amount == zero)
                            return zero;

                        var exist = destinationInventory.GetItemAmount(inventoryItem.Type);
                        if (amount <= exist)
                            return zero;

                        result -= exist;
                        continue;
                    }

                    var transfer = TransferItem(inventoryItem, sourceInventory, destinationInventory, amount);
                    if (transfer == null)
                        continue;

                    if (transfer == zero)
                        return zero;

                    result = (MyFixedPoint)transfer;
                }

                return result;
            }

            public static MyFixedPoint? TransferItem(MyInventoryItem item, IMyInventory sourceInventory,
                IMyInventory destinationInventory, MyFixedPoint amount = new MyFixedPoint())
            {
                var before = GetItemAmount(item, sourceInventory);
                var zero = MyFixedPoint.Zero;

                if (!sourceInventory.CanTransferItemTo(destinationInventory, item.Type) ||
                    destinationInventory.IsFull)
                    return null;

                var transfer = (amount == zero || before <= amount)
                    ? sourceInventory.TransferItemTo(destinationInventory, item)
                    : sourceInventory.TransferItemTo(destinationInventory, item, amount);
                if (!transfer)
                    return null;

                var after = GetItemAmount(item, sourceInventory);
                if (amount == zero || amount == before)
                    return after;

                if (amount > before)
                    return after + (amount - before);

                var result = (after > 0) ? before - after : before;
                return amount - result;
            }

            public static MyFixedPoint GetItemAmount(MyInventoryItem item, IMyInventory inventory)
            {
                var findItem = inventory.GetItemByID(item.ItemId);

                return findItem?.Amount ?? MyFixedPoint.Zero;
            }

            public static MyFixedPoint? TransferFromInventories(MyItemType type, List<IMyInventory> inventories,
                IMyInventory destinationInventory, MyFixedPoint amount = new MyFixedPoint())
            {
                var zero = new MyFixedPoint();
                if (amount == zero || inventories.Count == 0)
                    return null;

                var current = destinationInventory.GetItemAmount(type);
                amount -= current;

                if (amount <= zero)
                    return amount;

                foreach (var sourceInventory in inventories)
                {
                    var sourceInventoryItems = new List<MyInventoryItem>();
                    sourceInventory.GetItems(sourceInventoryItems, b => b.Type == type);

                    foreach (var inventoryItem in sourceInventoryItems)
                    {
                        var transfer = TransferItem(inventoryItem, sourceInventory, destinationInventory, amount);
                        if (transfer != null)
                            amount = (MyFixedPoint)transfer;

                        if (amount <= zero)
                            return amount;
                    }
                }

                return amount;
            }

            public static MyFixedPoint? TransferFromBlocks(MyItemType type, List<IMyTerminalBlock> blocks,
                IMyInventory destinationInventory, MyFixedPoint amount = new MyFixedPoint())
            {
                var zero = new MyFixedPoint();
                if (amount == zero || blocks.Count == 0)
                    return null;

                var current = destinationInventory.GetItemAmount(type);
                amount -= current;

                if (amount <= zero)
                    return amount;

                foreach (var block in blocks)
                {
                    if (block == null)
                        continue;

                    var inventoryCounts = block.InventoryCount;
                    if (inventoryCounts <= 0)
                        continue;

                    for (var i = 0; i < inventoryCounts; i++)
                    {
                        var sourceInventory = block.GetInventory(i);
                        var sourceInventoryItems = new List<MyInventoryItem>();
                        sourceInventory.GetItems(sourceInventoryItems, b => b.Type == type);

                        foreach (var inventoryItem in sourceInventoryItems)
                        {
                            var transfer = TransferItem(inventoryItem, sourceInventory, destinationInventory, amount);
                            if (transfer != null)
                                amount = (MyFixedPoint)transfer;

                            if (amount <= zero)
                                return amount;
                        }
                    }
                }

                return amount;
            }
        }
    }
}