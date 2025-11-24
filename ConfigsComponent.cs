using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    partial class Program
    {
        private const string RootConfigSectionName = "ROOT CONFIG SECTION";
        private const string SectionIndexSeparator = "::_";

        public class ConfigObject
        {
            public readonly string Section;
            public readonly Dictionary<string, string> Data;

            public ConfigObject(string section, Dictionary<string, string> data = null)
            {
                Section = section;
                Data = data ?? new Dictionary<string, string>();
            }

            public string Get(string key, string defaultValue = null)
            {
                string value;
                return (Data.TryGetValue(key, out value)) ? value : defaultValue;
            }

            public void Set(string key, string value = null)
            {
                Data[key] = value;
            }

            public List<string> ToStringList()
            {
                var result = new List<string>();
                foreach (var entry in Data)
                {
                    var line = entry.Key;
                    if (!string.IsNullOrEmpty(entry.Value))
                    {
                        line += "=" + entry.Value;
                    }

                    result.Add(line);
                }

                return result;
            }

            public string DataToString()
            {
                return string.Join("\n", ToStringList());
            }

            public static ConfigObject Parse(string section, string data = "")
            {
                if (string.IsNullOrEmpty(section) || !data.Contains(section))
                {
                    return null;
                }

                var sections = ConfigsHelper.GetSections(data);
                List<string> lines;
                if (!sections.TryGetValue(section, out lines))
                {
                    return null;
                }

                var result = new ConfigObject(section);
                foreach (var line in lines)
                {
                    var content = ConfigsHelper.ParseLine(line);
                    if (!string.IsNullOrEmpty(content.Key))
                    {
                        result.Set(content.Key, content.Value);
                    }
                }

                return result;
            }
        }

        public static class ConfigsHelper
        {
            public static Dictionary<string, List<string>> GetSections(string data, bool group = true)
            {
                var result = new Dictionary<string, List<string>>();

                var lines = data.Split('\n');
                if (lines.Length == 0)
                {
                    return result;
                }

                var section = RootConfigSectionName;
                var index = 1;
                result[section] = new List<string>();
                foreach (var line in data.Split('\n'))
                {
                    var sectionContent = line.Trim();
                    if (sectionContent.StartsWith("#") || sectionContent.StartsWith(";"))
                    {
                        continue;
                    }

                    if (sectionContent.StartsWith("[") && sectionContent.EndsWith("]"))
                    {
                        section = sectionContent.Substring(1, sectionContent.Length - 2).Trim();
                        if (!group)
                        {
                            section = AddSectionIndex(section, index);
                            index++;
                        }

                        if (!result.ContainsKey(section))
                        {
                            result[section] = new List<string>();
                        }

                        continue;
                    }

                    if (!string.IsNullOrEmpty(section))
                    {
                        result[section].Add(line);
                    }
                }

                foreach (var key in result.Keys.ToList())
                {
                    var list = result[key];

                    if (list.Count == 0 || !string.IsNullOrWhiteSpace(list[list.Count - 1].Trim()))
                    {
                        continue;
                    }

                    list.RemoveAt(list.Count - 1);
                    result[key] = list;
                }

                return result;
            }

            public static string AddSectionIndex(string section, int index)
            {
                return section + SectionIndexSeparator + index;
            }

            public static string RemoveSectionIndex(string section)
            {
                if (string.IsNullOrEmpty(section))
                {
                    return section;
                }

                var position = section.LastIndexOf(SectionIndexSeparator, StringComparison.Ordinal);
                if (position < 0)
                {
                    return section;
                }

                return section.Substring(0, position);
            }

            public static KeyValuePair<string, string> ParseLine(string line)
            {
                var result = new KeyValuePair<string, string>(null, null);
                var content = line.Trim();
                if (string.IsNullOrEmpty(content))
                {
                    return result;
                }

                var lineParts = content.Split(new[] { '=' }, 2);
                var key = lineParts[0].Trim();
                string value = null;
                if (lineParts.Length == 2)
                {
                    value = lineParts[1].Trim();
                }

                return new KeyValuePair<string, string>(key, value);
            }

            public static ConfigObject Merge(string section, List<ConfigObject> configs)
            {
                var result = new ConfigObject(section);
                foreach (var config in configs)
                {
                    if (config == null || config.Data.Count == 0)
                    {
                        continue;
                    }

                    foreach (var entry in config.Data)
                    {
                        result.Set(entry.Key, entry.Value);
                    }
                }

                return result;
            }

            public static string ToCustomData(ConfigObject config, string customData = "")
            {
                var result = new List<string>();
                var sections = GetSections(customData);

                if (config != null && !string.IsNullOrEmpty(config.Section))
                {
                    sections[config.Section] = config.ToStringList();
                }

                var firstSection = false;
                foreach (var section in sections)
                {
                    if (section.Key != RootConfigSectionName)
                    {
                        if (!firstSection)
                        {
                            firstSection = true;
                        }
                        else
                        {
                            result.Add("");
                        }

                        result.Add("[" + section.Key + "]");
                    }


                    result.AddRange(section.Value);
                }

                return string.Join("\n", result.ToArray());
            }
        }
    }
}