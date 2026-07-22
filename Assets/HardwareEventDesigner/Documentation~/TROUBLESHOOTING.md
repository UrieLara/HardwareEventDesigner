# Troubleshooting

Issues found during development, how they were diagnosed, and how they were resolved. 
---

## `SerialPort.ReadLine()` hangs indefinitely with ESP32 (no data read after boot)

### Symptom

Opening the serial port from Unity, the console only ever showed the ESP32's boot message:

```
ets Jul 29 2019 12:21:46
rst:0x1 (POWERON_RESET),boot:0x13 (SPI_FAST_FLASH_BOOT)
...
```

...and no data from the firmware's `loop()` ever arrived (not even a fixed `Serial.println()` test line with no floats involved). The reader thread stayed alive — no exceptions, the port never closed — a line simply never completed.

One detail that seemed relevant at the time and wasn't: temporarily adding a `Serial.println("PING")` line at the start of `loop()` made data start arriving, sometimes. Removing that line made reading hang again. This led to suspecting (incorrectly) that the issue was **boot timing** or the **DTR/RTS control lines** of the serial port, which on ESP32 boards are wired to the reset pin and can trigger a reset or leave the board in bootloader mode when the port is opened from a PC.

### Discarded hypotheses (in order)

1. **Port occupied by another process** (PlatformIO's Serial Monitor open at the same time) — ruled out, the port opened without error.
2. **Bootloop from insufficient power** — ruled out: the boot message appeared once, not in a repeating loop.
3. **`%f` not supported in ESP32's `printf` build** (a known issue with `newlib-nano` lacking float support) — ruled out with an isolation test: the `Serial.printf` with floats was temporarily swapped for a `Serial.println("PING")` fixed-text line, with no floats at all. The symptom (stuck at boot) stayed exactly the same without floats involved, so this wasn't it — even though the `PING` line seemed to "fix" something at the time, it was later confirmed to be timing coincidence.
4. **DTR/RTS ambiguity causing entry into bootloader mode** — forcing `DtrEnable = false` and `RtsEnable = false` on open, plus a fixed delay (`Thread.Sleep`) before reading, was tested. It didn't resolve the issue consistently.

### Actual diagnosis

To isolate the variable, the entire read method was temporarily replaced, swapping `_port.ReadLine()` for a raw byte-based read:

```csharp
private void ReadLoop()
{
    while (_keepReading)
    {
        try
        {
            if (_port.BytesToRead > 0)
            {
                int bytes = _port.BytesToRead;
                UnityEngine.Debug.Log($"[SerialConnection] Bytes available: {bytes}");
                string data = _port.ReadExisting();
                UnityEngine.Debug.Log($"[SerialConnection] Data: '{data}'");
            }
            Thread.Sleep(10);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[SerialConnection] Error: {e.GetType().Name} - {e.Message}");
            _keepReading = false;
        }
    }
}
```

With this code, data **did** arrive, consistently, with no artificial delay and no DTR/RTS manipulation. This confirmed that:

- Bytes were reaching the OS-level port buffer completely normally.
- The problem was specifically in `SerialPort`'s `ReadLine()` method, not the hardware, the firmware, or boot timing.

### Root cause

`SerialPort.ReadLine()` in .NET has a documented problematic behavior, especially in combination with USB-to-serial adapters (CP2102, CH340, and similar, common on ESP32/Arduino boards) on Windows. The method waits to internally accumulate a full buffer until it finds the line terminator, but in certain combinations of driver and data-arrival pattern (short bursts of bytes instead of a continuous stream), that wait can fail to resolve consistently — hitting repeated timeouts without ever completing a read, even though data is available in the underlying buffer (verifiable via `BytesToRead`).

This isn't an issue with the firmware, the wiring, or the DTR/RTS lines — it's a known limitation of `ReadLine()` in this specific hardware scenario.

### Fix

`ReadLine()` was replaced with manual character accumulation, using `BytesToRead` + `ReadExisting()` as the foundation (the same mechanism used for diagnosis), building lines character by character until a `\n` is found:

```csharp
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
            UnityEngine.Debug.LogWarning($"[SerialConnection] ReadLoop stopped: {e.GetType().Name} - {e.Message}");
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
```

The rest of the architecture (`SerialLineParser`, `SerialHardwareInputProvider`, `HardwareInputManager`) required no changes at all — they kept consuming from the same `ConcurrentQueue<string>` as always. Having responsibilities separated from the start meant the bug was contained to a single class, and fixing it had no side effects on the rest of the system.

### Lesson / note for the future

If this project is ever ported to Mac/Linux, it's worth re-testing whether `ReadLine()` shows the same issue there — this limitation is more widely reported in combination with Windows and certain USB-serial drivers, and might not reproduce the same way on other operating systems.

---

## Reset or bootloader entry when opening the port from Unity

### Symptom

On first connecting, the ESP32 shows its boot message (`rst:0x1 (POWERON_RESET)...`) as soon as Unity opens the serial port.

### Cause

USB-to-serial adapters used on ESP32 development boards (CP2102, CH340, etc.) have a circuit connecting the serial port's **DTR** and **RTS** control lines to the chip's `EN` (reset) and `GPIO0` (boot mode select) pins. This is designed so that flashing tools (like `esptool`) can automatically reset the board and put it into programming mode, with no physical buttons involved.

The side effect: any program that opens the serial port from a PC — including .NET's `SerialPort` — can toggle those lines on connect, causing a physical reset of the board as an unintended side effect.

### Mitigation

This isn't fully a bug to "fix" — it's expected hardware behavior. Recommended approach:

- Force a known state on those lines when opening the port (`DtrEnable = false; RtsEnable = false;`), to avoid the ambiguous combination that can leave the board in bootloader mode instead of booting normally.
- Design the parser to safely ignore any boot-time line that doesn't match the expected protocol format (in this case, any line without a `:` is discarded with no error).
