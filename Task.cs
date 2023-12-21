using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
    partial class Program
    {
        public class Task
        {
            public string Name { get; set; }
            public string Status { get; set; }
            public string LastStatus { get; set; }
            public string Error { get; set; }
            public int Delay { get; set; }
            public int CurrentTick { get; set; }
            public bool NeedInitialization { get; set; }
            public Action Method { get; set; }
            public DebugHelper Debug { get; set; }

            public Task(string name, Action method, int delay = 0, bool needInitialization = true)
            {
                Name = name;
                Status = "wait";
                LastStatus = null;
                Error = null;
                Delay = delay;
                CurrentTick = delay;
                NeedInitialization = needInitialization;
                Method = method;
                Debug = new DebugHelper();
            }
        }
    }
}
