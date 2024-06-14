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
            private Dictionary<long, IMyTerminalBlock> m_Storage;
            private Dictionary<BlockType, List<long>> m_StorageByTypes;
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
                m_Storage = new Dictionary<long, IMyTerminalBlock>();
                m_StorageByTypes = new Dictionary<BlockType, List<long>>();

                foreach (BlockType type in Enum.GetValues(typeof(BlockType)))
                {
                    m_StorageByTypes[type] = new List<long>();
                }

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                grid.SearchBlocksOfName(tag, blocks);
                blocks.Sort((a, b) => string.Compare(a.CustomName, b.CustomName));

                foreach (IMyTerminalBlock block in blocks)
                {
                    if (!block.IsFunctional || block.CustomName.Contains(ignore) || block.CustomData.Contains(ignore))
                        continue;

                    m_Storage[block.EntityId] = block;

                    if (block is IMyAssembler)
                        m_StorageByTypes[BlockType.Assemblers].Add(block.EntityId);

                    if (block is IMyCollector)
                        m_StorageByTypes[BlockType.Collectors].Add(block.EntityId);

                    if (block is IMyShipConnector)
                        m_StorageByTypes[BlockType.Connectors].Add(block.EntityId);

                    if (block is IMyCargoContainer)
                        m_StorageByTypes[BlockType.Containers].Add(block.EntityId);

                    if (block is IMyCryoChamber)
                        m_StorageByTypes[BlockType.CryoChambers].Add(block.EntityId);

                    if (block is IMyTextPanel)
                        m_StorageByTypes[BlockType.Displays].Add(block.EntityId);

                    if (block is IMyShipDrill)
                        m_StorageByTypes[BlockType.Drills].Add(block.EntityId);

                    if (block is IMyGasGenerator)
                        m_StorageByTypes[BlockType.GasGenerators].Add(block.EntityId);

                    if (block is IMyGasTank)
                        m_StorageByTypes[BlockType.GasTanks].Add(block.EntityId);

                    if (block is IMyShipGrinder)
                        m_StorageByTypes[BlockType.Grinders].Add(block.EntityId);

                    if (block is IMyPistonBase)
                        m_StorageByTypes[BlockType.Pistons].Add(block.EntityId);

                    if (block is IMyRefinery)
                        m_StorageByTypes[BlockType.Refineries].Add(block.EntityId);

                    if (block is IMyConveyorSorter)
                        m_StorageByTypes[BlockType.Sorters].Add(block.EntityId);

                    if (block is IMyLargeConveyorTurretBase)
                        m_StorageByTypes[BlockType.Turrets].Add(block.EntityId);

                    if (block is IMyShipWelder)
                    {
                        List<ITerminalProperty> properties = new List<ITerminalProperty>();
                        block.GetProperties(properties);
                        bool isBuildAndRepair = properties.Any(p => p.Id.Contains("BuildAndRepair"));
                        if (isBuildAndRepair)
                        {
                            m_StorageByTypes[BlockType.BuildAndRepair].Add(block.EntityId);
                        }
                        else
                        {
                            m_StorageByTypes[BlockType.Welders].Add(block.EntityId);
                        }
                    }
                }
            }

            public List<IMyTerminalBlock> GetBlocks(BlockType blockType = BlockType.All)
            {
                if (blockType == BlockType.All)
                    return m_Storage.Values.ToList();

                List<IMyTerminalBlock> result = new List<IMyTerminalBlock>();
                if (!m_StorageByTypes.ContainsKey(blockType) || m_StorageByTypes[blockType].Count == 0)
                    return result;

                foreach (long EntityId in m_StorageByTypes[blockType])
                {
                    if (m_Storage.ContainsKey(EntityId))
                    {
                        result.Add(m_Storage[EntityId]);
                    }
                }

                return result;

            }

            public void Clear()
            {
                m_Storage.Clear();
                m_StorageByTypes.Clear();
            }
        }

    }
}
