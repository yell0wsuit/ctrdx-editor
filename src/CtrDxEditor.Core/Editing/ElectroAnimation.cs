using System;
using System.Globalization;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Game-accurate electro on/off timing and atlas frame selection.</summary>
    public static class ElectroAnimation
    {
        private const double FrameDelaySeconds = 0.05;
        private const int OnFirstFrame = 1;
        private const int OnLastFrame = 4;

        /// <summary>Whether <paramref name="element"/> is the electro object.</summary>
        public static bool IsElectro(string element)
        {
            return element == "electro";
        }

        /// <summary>Whether the object has a positive electro on duration that can be previewed.</summary>
        public static bool HasActiveTiming(LevelObject obj)
        {
            return IsElectro(obj.Type) && ReadSeconds(obj, "onTime") > 0;
        }

        /// <summary>Whether the electro spark is active at <paramref name="elapsedSeconds"/>.</summary>
        public static bool IsOn(LevelObject obj, double elapsedSeconds)
        {
            return OnElapsedSeconds(obj, elapsedSeconds) is not null;
        }

        /// <summary>The visual descriptor key for electro at the given playback time.</summary>
        public static string SpriteKey(LevelObject obj, double? elapsedSeconds)
        {
            if (elapsedSeconds is not double seconds)
            {
                return "electro";
            }

            double? onElapsed = OnElapsedSeconds(obj, seconds);
            if (onElapsed is null)
            {
                return "electro_off";
            }

            int frameCount = OnLastFrame - OnFirstFrame + 1;
            int frame = OnFirstFrame + ((int)Math.Floor((onElapsed.Value / FrameDelaySeconds) + 1e-9) % frameCount);
            return $"electro_on_{frame}";
        }

        private static double? OnElapsedSeconds(LevelObject obj, double elapsedSeconds)
        {
            if (elapsedSeconds <= 0)
            {
                return null;
            }

            double offTime = ReadSeconds(obj, "offTime");
            double onTime = ReadSeconds(obj, "onTime");
            double initialDelay = ReadSeconds(obj, "initialDelay");
            if (!HasActiveTiming(obj))
            {
                return null;
            }

            double firstOffDuration = Math.Abs(offTime + initialDelay);
            if (elapsedSeconds < firstOffDuration)
            {
                return null;
            }

            double elapsedAfterFirstOn = elapsedSeconds - firstOffDuration;
            if (offTime <= 0)
            {
                return elapsedAfterFirstOn;
            }

            double cycle = onTime + offTime;
            if (cycle <= 0)
            {
                return null;
            }

            double phase = elapsedAfterFirstOn % cycle;
            return phase < onTime ? phase : null;
        }

        private static double ReadSeconds(LevelObject obj, string attribute)
        {
            return double.TryParse(obj.GetAttr(attribute), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? value
                : 0;
        }
    }
}
