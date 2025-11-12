using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class DisplayObject
        {
            public string Selector;
            public long BlockSelector;
            public int SurfaceIndex;
            public int UpdateDelay;
            private int _updateCurrentTick;
            public int ListingDelay;
            private int _listingCurrentTick;
            private readonly List<List<MySprite>> _lines;
            private int _currentLine;

            public DisplayObject(string selector, long blockSelector, int surfaceIndex, int updateDelay = 5,
                int listingDelay = 5)
            {
                Selector = selector;
                BlockSelector = blockSelector;
                SurfaceIndex = surfaceIndex;
                UpdateDelay = updateDelay;
                _updateCurrentTick = updateDelay;
                ListingDelay = listingDelay;
                _listingCurrentTick = 0;
                _lines = new List<List<MySprite>>();
                _currentLine = 0;
            }

            public void Tick()
            {
                _updateCurrentTick++;
            }

            public void TickReset()
            {
                _updateCurrentTick = 0;
            }

            public bool NeedUpdate()
            {
                return _updateCurrentTick >= UpdateDelay;
            }

            public void ListingTick()
            {
                _listingCurrentTick++;
            }

            public void ListingReset()
            {
                _listingCurrentTick = 0;
            }

            public bool NeedListing()
            {
                return _listingCurrentTick >= ListingDelay;
            }

            public void Listing(int limit)
            {
                ListingReset();
                var current = _currentLine;
                var result = current + limit;
                if (_lines.Count - 1 < result)
                    result = 0;

                _currentLine = result;
            }

            public void AddLine(params MySprite[] sprites)
            {
                _lines.Add(sprites.ToList());
            }

            public void AddBlankLine()
            {
                AddLine(BlankSprite());
            }

            public void AddTextLine(string text, TextAlignment alignment = TextAlignment.LEFT, Color? color = null)
            {
                AddLine(TextSprite(text, alignment, color));
            }

            public void AddCustomTextLine(string text)
            {
                AddLine(ParseTextSprite(text));
            }

            public List<List<MySprite>> GetLines(int limit = 0)
            {
                if (limit <= 0)
                    return _lines;

                var start = _currentLine;
                var count = Math.Min(limit, _lines.Count - start);

                return _lines.GetRange(start, count);
            }

            public string LinesToString()
            {
                var result = new List<string>();
                foreach (var row in _lines)
                {
                    if (row.Count == 0)
                    {
                        result.Add("");
                        continue;
                    }

                    var line = new List<string>();
                    foreach (var sprite in row)
                        line.Add(sprite.Data);

                    result.Add(string.Join(" || ", line.ToArray()));
                }

                return string.Join("\n", result.ToArray());
            }

            public void ClearLines()
            {
                _lines.Clear();
            }

            public static MySprite BlankSprite()
            {
                return TextSprite("");
            }

            public static MySprite TextSprite(string text, TextAlignment alignment = TextAlignment.LEFT,
                Color? color = null)
            {
                return new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = text,
                    Alignment = alignment,
                    Color = color
                };
            }

            public static MySprite ParseTextSprite(string text = null)
            {
                if (string.IsNullOrEmpty(text))
                    return BlankSprite();

                var alignment = TextAlignment.LEFT;
                var pattern = @"^(center|right|left)\s*";
                var match = System.Text.RegularExpressions.Regex.Match(text, pattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    switch (match.Value.Trim().ToLower())
                    {
                        case "center":
                            alignment = TextAlignment.CENTER;
                            break;
                        case "right":
                            alignment = TextAlignment.RIGHT;
                            break;
                        case "left":
                            alignment = TextAlignment.LEFT;
                            break;
                    }

                    text = System.Text.RegularExpressions.Regex.Replace(text, pattern, "",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                        .Trim();
                }

                return TextSprite(text, alignment);
            }

            public static RectangleF GetViewport(IMyTextSurface surface)
            {
                return new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);
            }

            public static List<MySprite> GetSurfaceBorder(IMyTextSurface surface, float border = 1f,
                float padding = 10f)
            {
                var result = new List<MySprite>();
                if (border <= 0)
                    return result;

                var viewport = GetViewport(surface);
                var outerRectSize = new Vector2(viewport.Width - 2 * padding, viewport.Height - 2 * padding);
                var rectPosition = new Vector2(viewport.X + viewport.Width / 2, viewport.Y + viewport.Height / 2);
                result.Add(new MySprite
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = rectPosition,
                    Size = outerRectSize,
                    Color = surface.ScriptForegroundColor,
                    Alignment = TextAlignment.CENTER
                });

                var innerRectSize = new Vector2(outerRectSize.X - 2 * border, outerRectSize.Y - 2 * border);
                result.Add(new MySprite

                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = rectPosition,
                    Size = innerRectSize,
                    Color = surface.ScriptBackgroundColor,
                    Alignment = TextAlignment.CENTER
                });

                return result;
            }

            public static List<MySprite> GetSurfaceTitle(IMyTextSurface surface, string title = "",
                string font = "Debug", float fontSize = 0.8f, float lineHeight = 32f,
                float border = 1f, float padding = 10f)
            {
                var result = new List<MySprite>();
                if (string.IsNullOrEmpty(title))
                    return result;

                var viewport = GetViewport(surface);
                var size = new Vector2(title.Length * 15 * fontSize, lineHeight);
                var position = new Vector2(viewport.X + viewport.Width / 2, viewport.Y + lineHeight / 2 * fontSize);

                if (border > 0)
                {
                    var rectPosition = new Vector2(position.X, viewport.Y + padding + lineHeight / 2);
                    var outerRectSize = new Vector2(size.X + 2 * padding, lineHeight);
                    var innerRectSize = new Vector2(outerRectSize.X - 2 * border, lineHeight - 2 * border);
                    result.Add(new MySprite
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = rectPosition,
                        Size = outerRectSize,
                        Color = surface.ScriptForegroundColor,
                        Alignment = TextAlignment.CENTER
                    });

                    result.Add(new MySprite
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = rectPosition,
                        Size = innerRectSize,
                        Color = surface.ScriptBackgroundColor,
                        Alignment = TextAlignment.CENTER
                    });
                }

                result.Add(new MySprite
                {
                    Type = SpriteType.TEXT,
                    Data = title,
                    Position = position,
                    RotationOrScale = fontSize,
                    Color = surface.ScriptForegroundColor,
                    FontId = font,
                    Alignment = TextAlignment.CENTER
                });

                return result;
            }
        }
    }
}