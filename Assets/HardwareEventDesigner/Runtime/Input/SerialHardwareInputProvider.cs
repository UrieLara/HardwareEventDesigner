using System.Collections.Generic;
using UnityEngine;

namespace HardwareEventDesigner.Runtime
{
    public class SerialHardwareInputProvider : MonoBehaviour, IHardwareInputProvider
    {
        [Header("Serial Settings")]
        [SerializeField] private string portName = "COM3";
        [SerializeField] private int baudRate = 115200;

        private SerialConnection _connection;
        private readonly Dictionary<string, float> _values = new Dictionary<string, float>();

        private void Start()
        {
            _connection = new SerialConnection(portName, baudRate);
            try
            {
                _connection.Open();
                Debug.Log($"SERIAL OPENED: {portName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"No se pudo abrir el puerto {portName}: {e.Message}");
            }
        }

        private void Update()
        {
            if (_connection == null || !_connection.IsOpen) return;

            while (_connection.TryDequeueLine(out string line))
            {
                Debug.Log($"[Serial] Línea recibida: {line}");
                SerialLineParser.ParseInto(line, _values);
            }
        }

        public float GetValue(string channelId) =>
            _values.TryGetValue(channelId, out float v) ? v : 0f;

        private void OnDestroy()
        {
            _connection?.Dispose();
        }
    }
}