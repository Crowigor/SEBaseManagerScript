using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    partial class Program
    {
        public class TasksManager
        {
            private readonly Dictionary<string, TaskObject> _storage = new Dictionary<string, TaskObject>();
            public const string InitializationTaskName = "Initialization";
            public const string CalculationTaskName = "Calculation";

            public void Add(string name, Action method, int delay = 1, bool needInitialization = true,
                bool needCalculation = false)
            {
                if (delay < 1)
                {
                    delay = 1;
                }

                var task = new TaskObject(name, method, delay, needInitialization, needCalculation);
                _storage[task.Name] = task;
            }

            public void Clear()
            {
                _storage.Clear();
            }

            public void Restart()
            {
                foreach (string key in new List<string>(_storage.Keys))
                {
                    var task = _storage[key];
                    task.Status = TaskObject.Statuses.Wait;
                    task.LastStatus = TaskObject.Statuses.Null;
                    task.CurrentTick = task.Delay;
                    UpdateTask(task);
                }

                Run();
            }

            public void Run()
            {
                foreach (string key in new List<string>(_storage.Keys))
                {
                    var task = _storage[key];
                    if (task.Status == TaskObject.Statuses.Progress)
                    {
                        continue;
                    }

                    if (task.Status == TaskObject.Statuses.Success || task.Status == TaskObject.Statuses.Error)
                    {
                        task.CurrentTick = 2;
                        task.LastStatus = task.Status;
                        task.Status = TaskObject.Statuses.Wait;

                        UpdateTask(task);
                    }

                    if (task.Status != TaskObject.Statuses.Wait && task.Status != TaskObject.Statuses.Skip)
                    {
                        continue;
                    }

                    if (task.CurrentTick < task.Delay)
                    {
                        task.CurrentTick++;
                        UpdateTask(task);
                        continue;
                    }

                    task.CurrentTick = 1;
                    task.Error = null;
                    task.LastStatus = TaskObject.Statuses.Null;

                    if ((task.NeedInitialization && !CheckInitialization()) ||
                        (task.NeedCalculation && !CheckCalculation()))
                    {
                        task.CurrentTick = task.Delay;
                        task.Status = TaskObject.Statuses.Skip;
                        UpdateTask(task);

                        continue;
                    }

                    task.Status = TaskObject.Statuses.Progress;
                    UpdateTask(task);

                    if (task.Method == null)
                    {
                        task.Error = "Method not found";
                        task.Status = TaskObject.Statuses.Error;
                        UpdateTask(task);

                        continue;
                    }

                    try
                    {
                        task.Method();
                        task.Status = (task.Delay == 1) ? TaskObject.Statuses.Wait : TaskObject.Statuses.Success;
                        UpdateTask(task);
                        TaskObject value;

                        if (task.Name == InitializationTaskName && _storage.TryGetValue(CalculationTaskName, out value))
                        {
                            value.LastStatus = TaskObject.Statuses.Skip;
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

            public List<string> GetStatusText()
            {
                var result = new List<string>();
                foreach (var entry in _storage)
                {
                    var task = entry.Value;
                    var text = task.Name + ": ";

                    if (task.Delay > 1)
                    {
                        text += (task.Status == TaskObject.Statuses.Wait) ? task.LastStatus : task.Status;
                        if (task.Status != TaskObject.Statuses.Skip)
                        {
                            text += " (" + task.CurrentTick + "/" + task.Delay + ")";
                        }
                    }
                    else
                    {
                        text += (task.Status != TaskObject.Statuses.Error) ? TaskObject.Statuses.Success : task.Status;
                    }

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
                TaskObject task;
                if (!_storage.TryGetValue(InitializationTaskName, out task))
                {
                    return false;
                }

                bool result;
                if (task.Status == TaskObject.Statuses.Success)
                {
                    result = true;
                }
                else if (task.Status == TaskObject.Statuses.Wait)
                {
                    result = (task.LastStatus == TaskObject.Statuses.Success);
                }
                else
                {
                    result = false;
                }

                return result;
            }

            private bool CheckCalculation()
            {
                TaskObject task;
                if (!_storage.TryGetValue(CalculationTaskName, out task))
                {
                    return false;
                }

                bool result;
                if (task.Status == TaskObject.Statuses.Success)
                {
                    result = true;
                }
                else if (task.Status == TaskObject.Statuses.Wait)
                {
                    result = (task.LastStatus == TaskObject.Statuses.Success);
                }
                else
                {
                    result = false;
                }

                return result;
            }

            private void UpdateTask(TaskObject task)
            {
                _storage[task.Name] = task;
            }
        }

        public class TaskObject
        {
            public readonly string Name;
            public Statuses Status;
            public Statuses LastStatus;
            public string Error;
            public readonly int Delay;
            public int CurrentTick;
            public readonly bool NeedInitialization;
            public readonly bool NeedCalculation;
            public readonly Action Method;

            public enum Statuses
            {
                Wait,
                Progress,
                Error,
                Success,
                Skip,
                Null
            }

            public TaskObject(string name, Action method, int delay = 0, bool needInitialization = true,
                bool needCalculation = false)
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