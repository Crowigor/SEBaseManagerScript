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
        }

        TasksManager Tasks;
        BlocksManager Blocks;
        ItemsManager Items;
        ConfigObject GlobalConfig;

        #endregion;

        #region Ingame methods
        public Program()
        {
            // Add Tasks
            Tasks = new TasksManager();
            Tasks.Add(TasksManager.InitializationTaskName, TaskInitialization, 20, false);
            Tasks.Add("Inventory Manager", TaskInventoryManager, 3);
            Tasks.Add("Display Status", TaskDisplayStatus, 1, false);

            // Run Initialization
            TaskInitialization();

            // Set update rate
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == null || argument == "")
                Tasks.Run();

        }

        #endregion

        #region Tasks
        private void TaskInitialization()
        {
            ConfigureMeDisplay();
            CheckGlobalConfig();
            Blocks = new BlocksManager(GridTerminalSystem, GlobalConfig.Get("Tag"), GlobalConfig.Get("Ignore"));

            if (Items == null)
            {
                Items = new ItemsManager();
            }
            else
            {
                Items.ResetData();
            }

            // Add invetories to items
            foreach (IMyTerminalBlock block in Blocks.GetBlocks(BlocksManager.BlockType.GasTanks))
            {
                string configSection = ConfigsSections.InventoryManager;
                if (block.CustomData.Contains(configSection))
                {
                    ConfigObject config = ConfigObject.Parse(configSection, block.CustomData);
                    foreach (KeyValuePair<string, string> entry in config.Data)
                    {
                        ItemObject item = Items.GetItem(entry.Key);
                        if (item != null)
                        {
                            item.Inventories.Add(block.GetInventory(0));
                        }
                    }
                }
            }
            foreach (IMyTerminalBlock block in Blocks.GetBlocks(BlocksManager.BlockType.Containers))
            {
                string configSection = ConfigsSections.InventoryManager;
                if (block.CustomData.Contains(configSection))
                {
                    ConfigObject config = ConfigObject.Parse(configSection, block.CustomData);
                    foreach (KeyValuePair<string, string> entry in config.Data)
                    {
                        ItemObject item = Items.GetItem(entry.Key);
                        if (item != null)
                        {
                            item.Inventories.Add(block.GetInventory(0));
                        }
                    }
                }
            }
        }

        private void TaskInventoryManager()
        {
            List<IMyInventory> inventories = new List<IMyInventory>();

            foreach (IMyTerminalBlock block in Blocks.GetBlocks())
            {
                if (!block.IsFunctional)
                {
                    continue;
                }
                else if (block is IMyLargeConveyorTurretBase)
                {
                    continue;
                }
                else if (block is IMyGasGenerator)
                {
                    IMyInventory inventory = block.GetInventory(0);
                    List<MyInventoryItem> items = new List<MyInventoryItem>();
                    inventory.GetItems(items);
                    foreach (MyInventoryItem item in items)
                    {
                        ItemObject find = Items.GetItem(item.Type.ToString());
                        if (find != null && find.Selector != "Ore/Ice")
                        {
                            find.Transfer(item, inventory);
                        }
                    }
                }
                else if (block is IMyAssembler)
                {
                    inventories.Add(block.GetInventory(1));
                }
                else if (block is IMyRefinery)
                {
                    inventories.Add(block.GetInventory(1));
                }
                else if (block is IMyEntity)
                {
                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        inventories.Add(block.GetInventory(i));
                    }
                }
            }
            Items.TrasferFromInventories(inventories);
        }

        private void TaskDisplayStatus()
        {
            List<string> displayData = new List<string>()
            {
                {"= Crowigor's Base Manager ="},
            };
            displayData.AddRange(Tasks.GetStatusText());

            Echo(String.Join("\n", displayData.ToArray()));
        }
        #endregion

        #region Script Methods
        private void ConfigureMeDisplay()
        {
            // Set display
            IMyTextSurface display = Me.GetSurface(0);
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
            string section = ConfigsSections.GlobalConfig;
            ConfigObject configDefault = new ConfigObject(section, new Dictionary<string, string>()
            {
                {"Tag", "[Base]" },
                {"Ignore", "[!CBM]" },
                {"SD:BaseContainersMaxVolume", "90%" },
            });
            ConfigObject configCurrent = ConfigObject.Parse(section, Me.CustomData);
            ConfigObject configNew = ConfigObject.Merge(section, new List<ConfigObject> { configDefault, configCurrent });

            List<string> customData = new List<string>()
            {
                ";Crowigor's Base Manager",
                 "[" + section + "]",
            };

            customData.AddRange(configNew.ToList());

            GlobalConfig = configNew;
            Me.CustomData = String.Join("\n", customData.ToArray());
        }
        #endregion;
    }
}