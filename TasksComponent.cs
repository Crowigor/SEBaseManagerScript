using System;
using System.Collections.Generic;

namespace IngameScript
{
    partial class Program
    {
        public class TasksManager
        {
            private Dictionary<string, TaskObject> m_Storage;
            public const string InitializationTaskName = "Initialization";
            public enum TaskStatus { Wait, Progress, Error, Success, Initialization, Null }

            public TasksManager()
            {
                m_Storage = new Dictionary<string, TaskObject>();
            }
            public void Add(string name, Action method, int delay = 0, bool needInitialization = true)
            {
                if (delay < 1)
                {
                    delay = 1;
                }
                TaskObject task = new TaskObject(name, method, delay, needInitialization);
                m_Storage[task.Name] = task;
            }

            public void Clear()
            {
                m_Storage.Clear();
            }

            public void Run()
            {
                bool initializationSuccess = CheckInitialization();
                foreach (string key in new List<string>(m_Storage.Keys))
                {
                    TaskObject task = m_Storage[key];

                    UpdateStorageItem(task);
                    if (task.Status == TaskStatus.Progress)
                    {
                        continue;
                    }
                    else if (task.Status == TaskStatus.Success || task.Status == TaskStatus.Error)
                    {
                        task.CurrentTick = 2;
                        task.LastStatus = task.Status;
                        task.Status = TaskStatus.Wait;

                        UpdateStorageItem(task);
                    }
                    else if (task.Status == TaskStatus.Wait || task.Status == TaskStatus.Initialization)
                    {
                        if (task.CurrentTick >= task.Delay)
                        {
                            task.CurrentTick = 1;
                            task.Error = null;
                            task.LastStatus = TaskStatus.Null;

                            if (!task.NeedInitialization || initializationSuccess)
                            {
                                task.Status = TaskStatus.Progress;
                                UpdateStorageItem(task);

                                if (task.Method == null)
                                {
                                    task.Error = "Method not found";
                                    task.Status = TaskStatus.Error;
                                    UpdateStorageItem(task);
                                }

                                else
                                {
                                    try
                                    {
                                        task.Method();
                                        task.Status = (task.Delay == 1) ? TaskStatus.Wait : TaskStatus.Success;
                                        UpdateStorageItem(task);
                                    }
                                    catch (Exception e)
                                    {
                                        task.Error = e.Message;
                                        task.Status = TaskStatus.Error;
                                        UpdateStorageItem(task);
                                    }
                                }
                            }
                            else
                            {
                                task.CurrentTick = task.Delay;
                                task.Status = TaskStatus.Initialization;
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

            public List<string> GetStatusText()
            {
                List<string> result = new List<string>();
                foreach (KeyValuePair<string, TaskObject> entry in m_Storage)
                {
                    TaskObject task = entry.Value;

                    string text = task.Name + ": ";

                    if (task.Delay > 1)
                    {
                        text += (task.Status == TaskStatus.Wait) ? task.LastStatus : task.Status;
                        if (task.Status != TaskStatus.Initialization)
                            text += " (" + task.CurrentTick + "/" + task.Delay + ")";
                    }
                    else
                        text += (task.Status != TaskStatus.Error) ? TaskStatus.Success : task.Status;

                    result.Add(text);

                    if (task.Error != null)
                    {

                        result.Add(task.Error + "\n");
                    }
                }

                return result;
            }

            private bool CheckInitialization()
            {
                if (!m_Storage.ContainsKey(InitializationTaskName))
                {
                    return false;
                }

                bool result = false;
                TaskObject task = m_Storage[InitializationTaskName];
                if (task.Status == TaskStatus.Success)
                {
                    result = true;
                }
                else if (task.Status == TaskStatus.Wait)
                {
                    result = (task.LastStatus == TaskStatus.Success);
                }

                return result;
            }

            private void UpdateStorageItem(TaskObject task)
            {
                m_Storage[task.Name] = task;
            }

        }

        public class TaskObject
        {
            public string Name { get; set; }
            public TasksManager.TaskStatus Status { get; set; }
            public TasksManager.TaskStatus LastStatus { get; set; }
            public string Error { get; set; }
            public int Delay { get; set; }
            public int CurrentTick { get; set; }
            public bool NeedInitialization { get; set; }
            public Action Method { get; set; }

            public TaskObject(string name, Action method, int delay = 0, bool needInitialization = true)
            {
                Name = name;
                Status = TasksManager.TaskStatus.Wait;
                LastStatus = TasksManager.TaskStatus.Null;
                Error = null;
                Delay = delay;
                CurrentTick = delay;
                NeedInitialization = needInitialization;
                Method = method;
            }
        }
    }
}
