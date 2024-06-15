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
            public const string CalculationTaskName = "Calculation";

            public TasksManager()
            {
                m_Storage = new Dictionary<string, TaskObject>();
            }
            public void Add(string name, Action method, int delay = 0, bool needInitialization = true, bool needCalculation = false)
            {
                if (delay < 1)
                {
                    delay = 1;
                }
                TaskObject task = new TaskObject(name, method, delay, needInitialization, needCalculation);
                m_Storage[task.Name] = task;
            }

            public void Clear()
            {
                m_Storage.Clear();
            }

            public void Run()
            {
                foreach (string key in new List<string>(m_Storage.Keys))
                {
                    TaskObject task = m_Storage[key];

                    if (task.Status == TaskObject.Statuses.Progress)
                    {
                        continue;
                    }
                    else if (task.Status == TaskObject.Statuses.Success || task.Status == TaskObject.Statuses.Error)
                    {
                        task.CurrentTick = 2;
                        task.LastStatus = task.Status;
                        task.Status = TaskObject.Statuses.Wait;

                        UpdateTask(task);
                    }
                    else if (task.Status == TaskObject.Statuses.Wait || task.Status == TaskObject.Statuses.Skip)
                    {
                        if (task.CurrentTick >= task.Delay)
                        {
                            task.CurrentTick = 1;
                            task.Error = null;
                            task.LastStatus = TaskObject.Statuses.Null;

                            bool skipTask = false;
                            if (task.NeedInitialization && !CheckInitialization())
                                skipTask = true;
                            if (task.NeedCalculation && !CheckCalculation())
                                skipTask = true;

                            if (!skipTask)
                            {
                                task.Status = TaskObject.Statuses.Progress;
                                UpdateTask(task);

                                if (task.Method == null)
                                {
                                    task.Error = "Method not found";
                                    task.Status = TaskObject.Statuses.Error;
                                    UpdateTask(task);
                                }
                                else
                                {
                                    try
                                    {
                                        task.Method();
                                        task.Status = (task.Delay == 1) ? TaskObject.Statuses.Wait : TaskObject.Statuses.Success;
                                        UpdateTask(task);
                                        if (task.Name == InitializationTaskName && m_Storage.ContainsKey(CalculationTaskName))
                                        {
                                            m_Storage[CalculationTaskName].LastStatus = TaskObject.Statuses.Null;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        task.Error = e.Message;
                                        task.Status = TaskObject.Statuses.Error;
                                        UpdateTask(task);
                                    }
                                }
                            }
                            else
                            {
                                task.CurrentTick = task.Delay;
                                task.Status = TaskObject.Statuses.Skip;
                                UpdateTask(task);
                            }
                        }
                        else
                        {
                            task.CurrentTick++;
                            UpdateTask(task);
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
                        text += (task.Status == TaskObject.Statuses.Wait) ? task.LastStatus : task.Status;
                        if (task.Status != TaskObject.Statuses.Skip)
                            text += " (" + task.CurrentTick + "/" + task.Delay + ")";
                    }
                    else
                        text += (task.Status != TaskObject.Statuses.Error) ? TaskObject.Statuses.Success : task.Status;

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
                if (task.Status == TaskObject.Statuses.Success)
                {
                    result = true;
                }
                else if (task.Status == TaskObject.Statuses.Wait)
                {
                    result = (task.LastStatus == TaskObject.Statuses.Success);
                }

                return result;
            }

            private bool CheckCalculation()
            {
                if (!m_Storage.ContainsKey(CalculationTaskName))
                {
                    return false;
                }

                bool result = false;
                TaskObject task = m_Storage[CalculationTaskName];
                if (task.Status == TaskObject.Statuses.Success)
                {
                    result = true;
                }
                else if (task.Status == TaskObject.Statuses.Wait)
                {
                    result = (task.LastStatus == TaskObject.Statuses.Success);
                }

                return result;
            }

            private void UpdateTask(TaskObject task)
            {
                m_Storage[task.Name] = task;
            }

        }

        public class TaskObject
        {
            public string Name { get; set; }
            public Statuses Status { get; set; }
            public Statuses LastStatus { get; set; }
            public string Error { get; set; }
            public int Delay { get; set; }
            public int CurrentTick { get; set; }
            public bool NeedInitialization { get; set; }
            public bool NeedCalculation { get; set; }
            public Action Method { get; set; }
            public enum Statuses { Wait, Progress, Error, Success, Skip, Null }

            public TaskObject(string name, Action method, int delay = 0, bool needInitialization = true, bool needCalculation = false)
            {
                Name = name;
                Status = Statuses.Wait;
                LastStatus = Statuses.Null;
                Error = null;
                Delay = delay;
                CurrentTick = delay;
                NeedInitialization = needInitialization;
                NeedCalculation = needCalculation;
                Method = method;
            }
        }
    }
}
