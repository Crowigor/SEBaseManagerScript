using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    partial class Program
    {
        public class BlocksManager
        {
            private Dictionary<long, IMyTerminalBlock> _storage;
            private Dictionary<BlockType, List<long>> _storageByTypes;

            public enum BlockType
            {
                All,
                Assemblers,
                Collectors,
                Connectors,
                Containers,
                CryoChambers,
                Displays,
                Drills,
                GasGenerators,
                GasTanks,
                Grinders,
                Pistons,
                Refineries,
                Sorters,
                Turrets,
                Welders,
                BuildAndRepair,
            }

            public BlocksManager(IMyGridTerminalSystem grid, string tag, string ignore)
            {
                _storage = new Dictionary<long, IMyTerminalBlock>();
                _storageByTypes = new Dictionary<BlockType, List<long>>();

                foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
                {
                    _storageByTypes[type] = new List<long>();
                }

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                grid.SearchBlocksOfName(tag, blocks);
                blocks.Sort((a, b) => string.Compare(a.CustomName, b.CustomName));

                foreach (IMyTerminalBlock block in blocks)
                {
                    if (!block.IsFunctional || block.CustomName.Contains(ignore) || block.CustomData.Contains(ignore))
                        continue;

                    _storage[block.EntityId] = block;

                    if (block is IMyAssembler)
                        _storageByTypes[BlockType.Assemblers].Add(block.EntityId);

                    if (block is IMyCollector)
                        _storageByTypes[BlockType.Collectors].Add(block.EntityId);

                    if (block is IMyShipConnector)
                        _storageByTypes[BlockType.Connectors].Add(block.EntityId);

                    if (block is IMyCargoContainer)
                        _storageByTypes[BlockType.Containers].Add(block.EntityId);

                    if (block is IMyCryoChamber)
                        _storageByTypes[BlockType.CryoChambers].Add(block.EntityId);

                    if (block is IMyTextPanel)
                        _storageByTypes[BlockType.Displays].Add(block.EntityId);

                    if (block is IMyShipDrill)
                        _storageByTypes[BlockType.Drills].Add(block.EntityId);

                    if (block is IMyGasGenerator)
                        _storageByTypes[BlockType.GasGenerators].Add(block.EntityId);

                    if (block is IMyGasTank)
                        _storageByTypes[BlockType.GasTanks].Add(block.EntityId);

                    if (block is IMyShipGrinder)
                        _storageByTypes[BlockType.Grinders].Add(block.EntityId);

                    if (block is IMyPistonBase)
                        _storageByTypes[BlockType.Pistons].Add(block.EntityId);

                    if (block is IMyRefinery)
                        _storageByTypes[BlockType.Refineries].Add(block.EntityId);

                    if (block is IMyConveyorSorter)
                        _storageByTypes[BlockType.Sorters].Add(block.EntityId);

                    if (block is IMyLargeConveyorTurretBase)
                        _storageByTypes[BlockType.Turrets].Add(block.EntityId);

                    if (block is IMyShipWelder)
                    {
                        List<ITerminalProperty> properties = new List<ITerminalProperty>();
                        block.GetProperties(properties);
                        bool isBuildAndRepair = properties.Any(p => p.Id.Contains("BuildAndRepair"));
                        if (isBuildAndRepair)
                        {
                            _storageByTypes[BlockType.BuildAndRepair].Add(block.EntityId);
                        }
                        else
                        {
                            _storageByTypes[BlockType.Welders].Add(block.EntityId);
                        }
                    }
                }
            }

            public IMyTerminalBlock GetBlock(long selector, BlockType blockType = BlockType.All)
            {
                if (!_storage.ContainsKey(selector))
                    return null;

                if (blockType != BlockType.All && !_storageByTypes[blockType].Contains(selector))
                    return null;

                return _storage[selector];
            }

            public List<IMyTerminalBlock> GetBlocks(BlockType blockType = BlockType.All)
            {
                if (blockType == BlockType.All)
                    return _storage.Values.ToList();

                List<IMyTerminalBlock> result = new List<IMyTerminalBlock>();
                if (!_storageByTypes.ContainsKey(blockType) || _storageByTypes[blockType].Count == 0)
                    return result;

                foreach (long EntityId in _storageByTypes[blockType])
                {
                    if (_storage.ContainsKey(EntityId))
                    {
                        result.Add(_storage[EntityId]);
                    }
                }

                return result;
            }

            public void Clear()
            {
                _storage.Clear();
                _storageByTypes.Clear();
            }
        }
    }
}