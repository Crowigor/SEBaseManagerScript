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
        }
    }
}