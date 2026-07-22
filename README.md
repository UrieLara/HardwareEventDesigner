# Hardware Event Designer

A Unity Editor tool + ESP32 firmware pair that reads physical hardware signals (analog joysticks, buttons, potentiometers, etc.) and turns them into configurable gameplay events, straight from the Inspector, with no code required per new sensor.

Built as a generic bridge between electronics and Unity: a **channel** defines what a piece of data is and how it activates, a **source** provides the actual values (real hardware over Serial, or a mock for working without hardware connected), and **listeners** react to events from any `MonoBehaviour` via `UnityEvent`.

---

## Why this project exists

My professional focus is **Tool Engineering / Gameplay Programming / Hardware-Software Integration**. This project was built to demonstrate all three at once: real firmware reading physical sensors, a decoupled and testable Unity architecture, and an Editor tool that a designer (not a programmer) can use without touching code.

---

## Architecture overview

```
Assets/HardwareEventDesigner/
├── Runtime/
│   ├── HardwareEventDesigner.Runtime.asmdef
│   ├── Channels/
│   │   └── HardwareEventChannel.cs        # ScriptableObject: defines a data channel
│   ├── Listeners/
│   │   └── HardwareEventListener.cs       # Listens to a channel and fires UnityEvents
│   └── Input/
│       ├── HardwareInputManager.cs        # Orchestrates: reads providers, updates listeners
│       ├── IHardwareInputProvider.cs      # Contract: "something that gives values by channelId"
│       ├── SerialHardwareInputProvider.cs # Real source: data over serial port
│       ├── SerialConnection.cs            # Only opens/reads the port (background thread)
│       ├── SerialLineParser.cs            # Only parses the text protocol
│       └── MockHardwareInputProvider.cs   # Fake source: manually set values
│
├── Editor/
│   ├── HardwareEventDesigner.Editor.asmdef
│   ├── Windows/
│   │   └── HardwareEventDesignerWindow.cs # Create/edit channels, simulate values
│   └── Overlays/
│       └── HardwareEventSceneOverlay.cs   # Live status panel in the Scene View
│
├── Examples/
│   ├── HardwareEventDesigner.Examples.asmdef
│   ├── Scenes/                            # Example scene using the Mock
│   └── Channels/                          # Test channels (Joystick, Button)
│
└── Documentation~/
    ├── README.md
    └── TROUBLESHOOTING.md
```

### Key pieces and their single responsibility

| Component | Single responsibility |
|---|---|
| `HardwareEventChannel` | Defines a data channel (id, type, threshold, trigger mode) and decides whether a value activates it |
| `HardwareEventListener` | Listens to a channel, tracks its active/inactive state, fires `UnityEvent`s |
| `IHardwareInputProvider` | Minimal contract: "give me the current value for this `channelId`" |
| `HardwareInputManager` | Each frame, pulls values from a provider and pushes them to every registered listener |
| `SerialConnection` | Opens the port, reads raw bytes on a background thread. Knows nothing about the protocol |
| `SerialLineParser` | Converts a text line into `(channelId, value)` pairs. Knows nothing about threads or ports |
| `SerialHardwareInputProvider` | Wires `SerialConnection` + `SerialLineParser` together, implements `IHardwareInputProvider` |
| `MockHardwareInputProvider` | Same interface, but with manually set values — lets the tool be tested without any hardware connected |

**Why it's split this way:** each class has a single reason to change (Single Responsibility Principle). If the firmware protocol changes tomorrow (say, to JSON), only `SerialLineParser` needs to change. If another transport is added (Bluetooth instead of Serial), a new class with the same shape as `SerialConnection` can be written and the rest of the system never notices. This also makes each piece individually testable: `SerialLineParser` is a pure function — it can be handed a string and checked against the expected result, with no real serial port involved.

---

## Firmware (ESP32 + PlatformIO)

The ESP32 reads a 2-axis analog joystick and a pushbutton, normalizes the axis values to a `0.0`–`1.0` range, and sends them over Serial every 50ms as a single line of text:

```
JoystickX:0.523	JoystickY:0.812	PushButton:0
```

Format: `channelId:value`, channels separated by tab (`\t`), line terminated with `\n`.

### Firmware design decisions

- **`millis()` instead of `delay()`:** the loop never blocks the microcontroller while waiting for the send interval, leaving room to add more sensors or logic without introducing lag.
- **One line with all channels, not one line per channel:** guarantees that values from the same reading "instant" arrive atomically. Sending separate lines per channel could desync values if one of them is delayed or dropped.
- **Plain text protocol, not binary:** chosen for readability and easy debugging (inspectable with any serial monitor). For scenarios with many channels at high frequency, a binary protocol would be more bandwidth-efficient — intentionally left out of scope for this project.

---

## Unity — data flow

```
ESP32 (Serial) 
   → SerialConnection (reader thread, thread-safe queue)
   → SerialLineParser (text → Dictionary<channelId, value>)
   → SerialHardwareInputProvider (implements IHardwareInputProvider)
   → HardwareInputManager (distributes values every frame)
   → HardwareEventListener (per channel, decides whether to fire an event)
   → UnityEvent (onTriggered / onValueChanged)
```

### Listener registration

`HardwareInputManager` doesn't go looking for listeners in the scene — each `HardwareEventListener` registers itself in `OnEnable` and unregisters in `OnDisable`. This avoids the problem of runtime-instantiated listeners (e.g. a prefab that spawns mid-game) being left out of the system, and avoids dangling references to destroyed objects.

### Trigger modes (`HardwareTriggerMode`)

- **Edge:** fires `onTriggered` exactly once, at the moment the value crosses the threshold (inactive → active transition).
- **Hold:** fires `onTriggered` every frame while the value stays above the threshold. Meant for "while held" logic (charging something, accumulating an action), as opposed to Edge's single pulse.
- **Toggle:** every threshold crossing flips the active/inactive state, with no need to keep anything held down.

### Analog noise filtering (`onValueChanged`)

A physical joystick never returns the exact same value twice — there's natural ADC noise. `onValueChanged` doesn't compare for exact equality; it uses a configurable threshold (`changeThreshold`) to ignore microscopic variations and avoid the event firing dozens of times per second with no real, perceptible change.

---

## Hardware setup

### Components used

| Component | Notes |
|---|---|
| ESP32 dev board | Any standard dev board with analog-capable GPIOs |
| 2-axis analog joystick module (HW-504) | Uses two potentiometers (X/Y) + an integrated pushbutton |
| Voltage divider resistors (X/Y axes) | 2kΩ + 1kΩ in series on each axis output, stepping the HW-504's 5V signal down to a safe ~3.3V level for the ESP32's ADC input |
| Pushbutton | External 10kΩ pull-up to 3.3V |
| USB cable (data-capable, not charge-only) | Required for both flashing and reading Serial data |

### Pin mapping

| ESP32 Pin | Signal | Firmware constant |
|---|---|---|
| GPIO 34 | Joystick X axis (analog) | `pinX` |
| GPIO 35 | Joystick Y axis (analog) | `pinY` |
| GPIO 19 | Pushbutton (digital, `INPUT_PULLUP`) | `pinButton` |

### Circuit

<p align="center">
  <img src="./Assets/HardwareEventDesigner/Documentation~/images/scene-view-overlay.png" width="60%" alt="Unity Scene View showing live channel values" />
  <img src="./Assets/HardwareEventDesigner/Documentation~/images/physical-circuit.jpg" width="38%" alt="Physical ESP32 + joystick circuit" />
</p>

### Schematic

<p align="center">
  <img src="./Assets/HardwareEventDesigner/Documentation~/images/circuit-schematic.png" width="70%" alt="Circuit schematic" />
</p>

---

## Testing without hardware connected

The example scene includes both providers as separate GameObjects: `SerialHardwareInputProvider` and `MockHardwareInputProvider`. To test without any ESP32 connected, reassign the `_providerBehaviour` field on `HardwareInputManager` (in the Inspector) from the Serial provider to the Mock provider.

Once switched, values can be set by hand on `MockHardwareInputProvider`'s channel list in the Inspector — for example, changing the `DoorButton` virtual channel — and the change will be visible live in the Scene View overlay (`Tools > Hardware Event Designer` or the `HardwareEventSceneOverlay` panel), with no hardware required.

---

## Known issues and how they were solved

See [`TROUBLESHOOTING.md`](./Assets/HardwareEventDesigner/Documentation~/TROUBLESHOOTING.md) for the details of a particularly interesting bug related to `SerialPort.ReadLine()` in .NET and ESP32 boards.
