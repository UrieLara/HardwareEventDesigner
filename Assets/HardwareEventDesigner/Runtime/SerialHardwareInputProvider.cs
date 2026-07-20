using UnityEngine;
using System.IO.Ports;
using System.Collections.Generic;

namespace HardwareEventDesigner.Runtime
{
    public class SerialHardwareInputProvider : MonoBehaviour, IHardwareInputProvider
    {
        [Header("Serial Settings")]
        public string portName = "COM3";
        public int baudRate = 115200;

        private SerialPort serial;
        private Dictionary<string, float> values = new Dictionary<string, float>();

        private void Start()
        {
            serial = new SerialPort(portName, baudRate);
            serial.Open();
            serial.ReadTimeout = 1;
        }

        private void Update()
        {
            if (serial == null || !serial.IsOpen) return;

            try
            {
                string line = serial.ReadLine(); 
                ParseLine(line);
            }
            catch { }
        }

        private void ParseLine(string line)
        {
            if (!line.Contains(":")) return;

            string[] parts = line.Split(':');
            string channelId = parts[0];
            float value = float.Parse(parts[1]);

            values[channelId] = value;
        }

        public float GetValue(string channelId)
        {
            return values.ContainsKey(channelId) ? values[channelId] : 0f;
        }
    }
}
