using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using SpaceEngineers.Game.Entities.Blocks;

namespace IngameScript
{
    partial class Program
    {
        public class VolumeObject
        {
            public enum VolumeTypes
            {
                None,
                Battery,
                Container,
                Tank,
            }

            public enum RemainedVectors
            {
                None,
                Plus,
                Minus,
            }

            public long Selector;
            public VolumeTypes VolumeType;
            public string BlockName;
            public double CurrentVolume;
            public double LastVolume;
            public double MaxVolume;
            public int CurrentPercent;
            public double CurrentTime;
            public double LastTime;
            public double Remained;
            public RemainedVectors RemainedVector;
            public bool IsValid { get; private set; }

            public VolumeObject(long selector, VolumeTypes volumeType, string blockName)
            {
                Selector = selector;
                VolumeType = volumeType;
                BlockName = blockName;
                RemainedVector = RemainedVectors.None;
                IsValid = true;
            }

            public VolumeObject(List<VolumeObject> objects, VolumeTypes volumeType)
            {
                int count = 0;

                double sumCurrentTime = 0;
                double sumLastTime = 0;

                double currentVolume = 0;
                double lastVolume = 0;
                double maxVolume = 0;

                foreach (var volumeObject in objects)
                {
                    if (volumeObject.VolumeType != volumeType)
                    {
                        continue;
                    }

                    count++;


                    sumCurrentTime += volumeObject.CurrentTime;
                    sumLastTime += volumeObject.LastTime;

                    currentVolume += volumeObject.CurrentVolume;
                    lastVolume += volumeObject.LastVolume;
                    maxVolume += volumeObject.MaxVolume;
                }

                if (count == 0)
                {
                    IsValid = false;
                    return;
                }

                IsValid = true;

                CurrentVolume = lastVolume;
                CurrentTime = count > 0 ? sumLastTime / count : 0;

                var currentTime = count > 0 ? sumCurrentTime / count : 0;
                SetValue(currentVolume, maxVolume, currentTime);
            }

            public void SetValue(double current, double max, double currentTime)
            {
                LastVolume = CurrentVolume;
                LastTime = CurrentTime;

                CurrentVolume = current;
                MaxVolume = max;
                CurrentTime = currentTime;

                if (MaxVolume > 0)
                {
                    var raw = CurrentVolume / MaxVolume * 100.0;
                    var rounded = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
                    if (rounded < 0)
                    {
                        rounded = 0;
                    }
                    else if (rounded > 100)
                    {
                        rounded = 100;
                    }

                    CurrentPercent = rounded;
                }
                else
                {
                    CurrentPercent = 0;
                }


                if (CurrentVolume > LastVolume)
                {
                    RemainedVector = RemainedVectors.Plus;
                }
                else if (CurrentVolume < LastVolume)
                {
                    RemainedVector = RemainedVectors.Minus;
                }
                else
                {
                    RemainedVector = RemainedVectors.None;
                }

                if (RemainedVector != RemainedVectors.None)
                {
                    var time = CurrentTime - LastTime;
                    if (time <= 0)
                    {
                        Remained = 0;
                        return;
                    }

                    var volume = CurrentVolume - LastVolume;
                    var rate = volume / time;
                    const double eps = 1e-9;
                    if (Math.Abs(rate) < eps)
                    {
                        Remained = 0;

                        return;
                    }

                    if (RemainedVector == RemainedVectors.Plus)
                    {
                        if (MaxVolume > 0 && CurrentVolume < MaxVolume)
                        {
                            var left = MaxVolume - CurrentVolume;
                            Remained = left > 0 ? Math.Max(0, left / rate) : 0;
                        }
                        else
                        {
                            Remained = 0;
                        }
                    }
                    else
                    {
                        if (CurrentVolume > 0)
                        {
                            Remained = Math.Max(0, CurrentVolume / Math.Abs(rate));
                        }
                        else
                        {
                            Remained = 0;
                        }
                    }
                }
            }
        }
    }
}