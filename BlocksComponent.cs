using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using SpaceEngineers.Game.Entities.Blocks;

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
                AirVent,
                Assembler,
                Battery,
                Cockpit,
                Collector,
                Connector,
                Container,
                CryoChamber,
                Display,
                Drill,
                GasGenerator,
                GasPowerProducer,
                GasTank,
                GravityGenerator,
                Grinder,
                Piston,
                Projector,
                Reactor,
                Refinery,
                Rotor,
                Sorter,
                TerminalBlock,
                Turret,
                Welder
            }

            public BlocksManager(IMyGridTerminalSystem grid, string tag, string ignoreTag,
                List<BlockType> types = null, List<BlockType> ignoreTypes = null)
            {
                _storage = new Dictionary<long, IMyTerminalBlock>();
                _storageByTypes = new Dictionary<BlockType, List<long>>();

                foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
                {
                    if (type == BlockType.TerminalBlock)
                        continue;

                    if (types != null && types.Count > 0 && !types.Contains(type))
                        continue;

                    if (ignoreTypes != null && ignoreTypes.Count > 0 && ignoreTypes.Contains(type))
                        continue;

                    _storageByTypes[type] = new List<long>();
                }

                var blocks = new List<IMyTerminalBlock>();
                grid.SearchBlocksOfName(tag, blocks);
                blocks.Sort((a, b) => string.Compare(a.CustomName, b.CustomName));

                foreach (var block in blocks)
                {
                    if (!block.IsFunctional || block.CustomName.Contains(ignoreTag) ||
                        block.CustomData.Contains(ignoreTag))
                        continue;

                    var addToStorage = false;
                    foreach (var blockType in GetBlockTypes(block))
                    {
                        if (!_storageByTypes.ContainsKey(blockType))
                            continue;
                        addToStorage = true;
                        _storageByTypes[blockType].Add(block.EntityId);
                    }

                    if (addToStorage)
                    {
                        _storage[block.EntityId] = block;
                    }
                }
            }

            public IMyTerminalBlock GetBlock(long selector, BlockType blockType = BlockType.TerminalBlock)
            {
                if (!_storage.ContainsKey(selector))
                    return null;

                if (blockType != BlockType.TerminalBlock && !_storageByTypes[blockType].Contains(selector))
                    return null;

                return _storage[selector];
            }

            public List<IMyTerminalBlock> GetBlocks(BlockType blockType = BlockType.TerminalBlock)
            {
                if (blockType == BlockType.TerminalBlock)
                    return _storage.Values.ToList();

                var result = new List<IMyTerminalBlock>();
                if (!_storageByTypes.ContainsKey(blockType) || _storageByTypes[blockType].Count == 0)
                    return result;

                foreach (var EntityId in _storageByTypes[blockType])
                {
                    if (_storage.ContainsKey(EntityId))
                    {
                        result.Add(_storage[EntityId]);
                    }
                }

                return result;
            }

            public static List<BlockType> GetBlockTypes(IMyTerminalBlock block)
            {
                var result = new List<BlockType>();

                if (block is IMyAirVent)
                    result.Add(BlockType.AirVent);
                if (block is IMyAssembler)
                    result.Add(BlockType.Assembler);
                if (block is IMyBatteryBlock)
                    result.Add(BlockType.Battery);
                if(block is IMyCockpit)
                    result.Add(BlockType.Cockpit);
                if (block is IMyCollector)
                    result.Add(BlockType.Collector);
                if (block is IMyShipConnector)
                    result.Add(BlockType.Connector);
                if (block is IMyCargoContainer)
                    result.Add(BlockType.Container);
                if (block is IMyCryoChamber)
                    result.Add(BlockType.CryoChamber);
                if (block is IMyTextPanel)
                    result.Add(BlockType.Display);
                if (block is IMyShipDrill)
                    result.Add(BlockType.Drill);
                if (block is IMyGasGenerator)
                    result.Add(BlockType.GasGenerator);
                if (block is IMyGasTank)
                    result.Add(BlockType.GasTank);
                if (block is IMyGravityGenerator)
                    result.Add(BlockType.GravityGenerator);
                if (block is IMyShipGrinder)
                    result.Add(BlockType.Grinder);
                if (block is IMyPowerProducer && block is IMyGasTank)
                    result.Add(BlockType.GasPowerProducer);
                if (block is IMyPistonBase)
                    result.Add(BlockType.Piston);
                if (block is IMyProjector)
                    result.Add(BlockType.Projector);
                if (block is IMyReactor)
                    result.Add(BlockType.Reactor);
                if (block is IMyRefinery)
                    result.Add(BlockType.Refinery);
                if (block is IMyMotorStator)
                    result.Add(BlockType.Rotor);
                if (block is IMyConveyorSorter)
                    result.Add(BlockType.Sorter);
                if (block is IMyLargeConveyorTurretBase)
                    result.Add(BlockType.Turret);
                if (block is IMyShipWelder)
                    result.Add(BlockType.Welder);

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