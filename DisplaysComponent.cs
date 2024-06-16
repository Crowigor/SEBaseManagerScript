using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;

namespace IngameScript
{
    partial class Program
    {
        public class DisplayObject
        {
            public long Selector;
            public int UpdateDataDelay;
            public int UpdateDataCurrentTick;
            public readonly Dictionary<int, List<MySprite>> Lines;

            public DisplayObject(long selector, int updateDataDelay)
            {
                Selector = selector;
                UpdateDataDelay = updateDataDelay;
                UpdateDataCurrentTick = 0;
                Lines = new Dictionary<int, List<MySprite>>();
            }

            public Dictionary<int, List<MySprite>> GetLines(int limit = 0)
            {
                if (limit <= 0)
                {
                    return Lines;
                }

                var count = 0;
                var result = new Dictionary<int, List<MySprite>>();
                foreach (var line in Lines)
                {
                    if (count == limit)
                        break;

                    result[line.Key] = line.Value;
                    count++;
                }

                return result;
            }
        }
    }
}