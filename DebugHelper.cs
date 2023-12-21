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
    partial class Program
    {
        public class DebugHelper
        {
            public List<string> Info { get; set; }
            public List<string> Warning { get; set; }

            public DebugHelper()
            {
                Info = new List<string>();
                Warning = new List<string>();
            }

            public void AddInfo(string message, bool unique = true)
            {
                AddMessage(Info, message, unique);
            }

            public void AddWarning(string message, bool unique = true)
            {
                AddMessage(Warning, message, unique);
            }

            public void Clear()
            {
                Info = new List<string>();
                Warning = new List<string>();
            }

            public void Merge(DebugHelper debug)
            {

                Info.AddRange(debug.Info);
                Warning.AddRange(debug.Warning);
            }

            protected void AddMessage(List<string> storage, string message, bool unique = true)
            {
                if (unique && !storage.Contains(message))
                {
                    storage.Add(message);
                }
                else if (!unique)
                {
                    storage.Add(message);
                }
            }
        }
    }
}
