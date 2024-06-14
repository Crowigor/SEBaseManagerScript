using System;
using System.Collections.Generic;

namespace IngameScript
{
    partial class Program
    {
        public class ConfigObject
        {
            public string Section { get; set; }
            public Dictionary<string, string> Data { get; set; }

            public ConfigObject(string section, Dictionary<string, string> data = null)
            {
                Section = section;
                Data = (data != null) ? data : new Dictionary<string, string>();
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
                    string line = entry.Key;
                    if (line.StartsWith("EMPTY_LINE_"))
                    {
                        line = " ";
                        result.Add(line);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(entry.Value))
                    {
                        line += "=" + entry.Value;
                    }
                    result.Add(line);
                }

                return result;
            }

            public static ConfigObject Parse(string section, string data, bool emptyLines = false)
            {
                Dictionary<string, string> resultData = new Dictionary<string, string>();

                string currentSection = "";
                string[] lines = data.Split('\n');
                int emptyLineCounter = 0;
                foreach (string line in lines)
                {
                    string content = line.Trim();
                    if (content.Length == 0 && !emptyLines)
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
                        if (emptyLines && String.IsNullOrEmpty(key))
                        {
                            key = "EMPTY_LINE_" + emptyLineCounter;
                            emptyLineCounter++;
                        }

                        string value = null;
                        if (lineParts.Length == 2)
                        {
                            value = lineParts[1].Trim();
                        }

                        resultData[key] = value;
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
