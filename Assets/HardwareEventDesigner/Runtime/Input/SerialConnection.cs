using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace HardwareEventDesigner.Runtime
{
    public class SerialConnection : IDisposable
    {
        private readonly SerialPort _port;
        private Thread _readThread;
        private volatile bool _keepReading;
        private readonly ConcurrentQueue<string> _incomingLines = new ConcurrentQueue<string>();
        private readonly StringBuilder _lineBuffer = new StringBuilder();

        public bool IsOpen => _port != null && _port.IsOpen;

        public SerialConnection(string portName, int baudRate, int readTimeoutMs = 100)
        {
            _port = new SerialPort(portName, baudRate)
            {
                ReadTimeout = readTimeoutMs
            };
        }

        public void Open()
        {
            _port.Open();
            _port.DtrEnable = false;
            _port.RtsEnable = false;

            _keepReading = true;
            _readThread = new Thread(ReadLoop) { IsBackground = true };
            _readThread.Start();
        }

        private void ReadLoop()
        {
            while (_keepReading)
            {
                try
                {
                    if (_port.BytesToRead > 0)
                    {
                        string chunk = _port.ReadExisting();
                        ProcessChunk(chunk);
                    }
                    else
                    {
                        Thread.Sleep(5);
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[SerialConnection] ReadLoop detenido: {e.GetType().Name} - {e.Message}");
                    _keepReading = false;
                }
            }
        }

        private void ProcessChunk(string chunk)
        {
            foreach (char c in chunk)
            {
                if (c == '\n')
                {
                    string completedLine = _lineBuffer.ToString();
                    _lineBuffer.Clear();
                    if (!string.IsNullOrEmpty(completedLine))
                        _incomingLines.Enqueue(completedLine);
                }
                else if (c != '\r')
                {
                    _lineBuffer.Append(c);
                }
            }
        }

        public bool TryDequeueLine(out string line)
        {
            return _incomingLines.TryDequeue(out line);
        }

        public void Dispose()
        {
            _keepReading = false;
            _readThread?.Join(200);
            if (_port != null && _port.IsOpen)
                _port.Close();
        }
    }
}