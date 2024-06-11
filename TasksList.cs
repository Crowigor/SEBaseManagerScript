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
using static IngameScript.Program;

namespace IngameScript
{
    partial class Program
    {
        public class TasksList
        {
            Dictionary<string, TaskObject> Storage { get; set; }
            public DebugHelper Debug { get; set; }

            public TasksList()
            {
                Storage = new Dictionary<string, TaskObject>();
                Debug = new DebugHelper();
            }

            public void CreateTask(string name, Action method, int delay = 0, bool needInitialization = true)
            {
                TaskObject task = new TaskObject(name, method, delay, needInitialization);
                Storage[task.Name] = task;
            }

            public void RunTasks()
            {              
                bool initializationSuccess = CheckInitialization();
                foreach (string key in new List<string>(Storage.Keys))
                {
                    TaskObject task = Storage[key];

                    UpdateStorageItem(task);

                    if (task.Status == "progress")
                    {
                        continue;
                    }
                    else if (task.Status == "success" || task.Status == "error")
                    {
                        task.CurrentTick = 2;
                        task.LastStatus = task.Status;
                        task.Status = "wait";

                        UpdateStorageItem(task);
                    }
                    else if (task.Status == "wait" || task.Status == "wait initialization")
                    {
                        if (task.CurrentTick >= task.Delay)
                        {
                            task.CurrentTick = 1;
                            task.Error = null;
                            task.LastStatus = null;

                            if (!task.NeedInitialization || initializationSuccess)
                            {
                                task.Status = "progress";
                                UpdateStorageItem(task);

                                if (task.Method == null)
                                {
                                    task.Error = "Method not found";
                                    task.Status = "error";
                                    UpdateStorageItem(task);
                                }

                                else
                                {
                                    try
                                    {
                                        task.Method();
                                        task.Status = "success";
                                        UpdateStorageItem(task);
                                    }
                                    catch (Exception e)
                                    {
                                        task.Error = e.Message;
                                        task.Status = "error";
                                        UpdateStorageItem(task);
                                    }
                                }
                            }
                            else
                            {
                                task.CurrentTick = task.Delay;
                                task.Status = "wait initialization";
                                UpdateStorageItem(task);
                            }
                        }
                        else
                        {
                            task.CurrentTick++;
                            UpdateStorageItem(task);
                        }
                    }
                }
            }

            public List<string> GetStatus()
            {
                List<string> status = new List<string>();
                foreach (KeyValuePair<string, TaskObject> entry in Storage)
                {
                    TaskObject task = entry.Value;

                    string text = task.Name + ": ";

                    text += (task.Status == "wait") ? task.LastStatus : task.Status;
                    if (task.Status != "wait initialization")
                    {
                        text += " (" + task.CurrentTick + "/" + task.Delay + ")";
                    }
                    status.Add(text);

                    if (task.Error != null)
                    {

                        status.Add(task.Error + "\n");
                    }
                }

                return status;
            }

            private bool CheckInitialization()
            {
                if (!Storage.ContainsKey("Initialization"))
                {
                    return false;
                }

                bool result = false;
                TaskObject task = Storage["Initialization"];
                if (task.Status == "success")
                {
                    result = true;
                }
                else if (task.Status == "wait")
                {
                    result = (task.LastStatus == "success");
                }

                return result;
            }

            private void UpdateStorageItem(TaskObject task)
            {
                Storage[task.Name] = task;
            }
        }
    }
}
