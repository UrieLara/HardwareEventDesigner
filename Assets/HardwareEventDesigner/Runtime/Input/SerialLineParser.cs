using System.Collections.Generic;
using System.Globalization;

namespace HardwareEventDesigner.Runtime
{
    /// <summary>
    /// Convierte una línea cruda del firmware (ej: "JoystickX:0.523\tPushButton:1")
    /// en pares (channelId, valor). No sabe nada sobre puertos ni hilos.
    /// </summary>
    public static class SerialLineParser
    {
        public static void ParseInto(string line, Dictionary<string, float> destination)
        {
            if (string.IsNullOrEmpty(line)) return;

            string[] channels = line.Split('\t');
            foreach (string channelData in channels)
            {
                string[] parts = channelData.Split(':');
                if (parts.Length < 2) continue;

                string channelId = parts[0].Trim();
                if (float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    destination[channelId] = value;
                }
            }
        }
    }
}