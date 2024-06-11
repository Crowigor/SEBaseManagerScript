using Sandbox.Engine.Utils;
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
        public class ConfigObject
        {
            public string Section { get; set; }
            public Dictionary<string, string> Data { get; set; }
            public DebugHelper Debug { get; set; }

            public ConfigObject(string section, Dictionary<string, string> data = null)
            {
                Section = section;
                Data = (data != null) ? data : new Dictionary<string, string>();
                Debug = new DebugHelper();
            }

            public string Get(string key, string defaultValue = null)
            {
                return (Data.ContainsKey(key)) ? Data[key] : defaultValue;
            }

            public void Set(string key, string Value = null)
            {
                Data[key] = Value;
            }

            public List<string> ToList()
            {
                List<string> result = new List<string>();
                foreach (KeyValuePair<string, string> entry in Data)
                {
                    result.Add(entry.Key + "=" + entry.Value);
                }

                return result;
            }

            public static ConfigObject Parse(string section, string data)
            {
                Dictionary<string, string> resultData = new Dictionary<string, string>();

                string currentSection = "";
                string[] lines = data.Split('\n');
                foreach (string line in lines)
                {
                    string content = line.Trim();
                    if (content.Length == 0)
                    {
                        continue;
                    }
                    else if (content.StartsWith(";"))
                    {
                        continue;
                    }
                    else if (content.StartsWith("[") && content.EndsWith("]"))
                    {
                        currentSection = content.Substring(1, content.Length - 2).Trim();
                        continue;
                    }
                    else if (currentSection == section)
                    {
                        string[] lineParts = content.Split(new[] { '=' }, 2);
                        string key = lineParts[0].Trim();
                        string value = null;
                        if (lineParts.Length == 2)
                        {
                            value = lineParts[1].Trim();
                        }

                        resultData.Add(key, value);
                    }
                }

                return new ConfigObject(section, resultData);
            }

            public static ConfigObject Merge(string section, List<ConfigObject> configs)
            {
                ConfigObject result = new ConfigObject(section);

                foreach (ConfigObject config in configs)
                {
                    foreach (KeyValuePair<string, string> entry in config.Data)
                    {
                        result.Set(entry.Key, entry.Value);

                    }
                }

                return result;
            }
        }
    }
}
