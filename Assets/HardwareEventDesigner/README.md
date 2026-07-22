# Hardware Event Designer

## Architecture Overview

```text
Assets/HardwareEventDesigner/
├── Runtime/
│   ├── HardwareEventDesigner.Runtime.asmdef
│   ├── Channels/
│   │   └── HardwareEventChannel.cs        # ScriptableObject: defines a data channel
│   ├── Listeners/
│   │   └── HardwareEventListener.cs       # Listens to a channel and triggers UnityEvents
│   └── Input/
│       ├── HardwareInputManager.cs        # Orchestrates providers and updates listeners
│       ├── IHardwareInputProvider.cs      # Contract: "something that provides values by channelId"
│       ├── SerialHardwareInputProvider.cs # Real input source: data received through serial port
│       ├── SerialConnection.cs            # Handles serial port communication on a separate thread
│       ├── SerialLineParser.cs            # Interprets the text-based communication protocol
│       └── MockHardwareInputProvider.cs   # Mock input source: manually assigned values
│
├── Editor/
│   ├── HardwareEventDesigner.Editor.asmdef
│   ├── Windows/
│   │   └── HardwareEventDesignerWindow.cs # Create/edit channels and simulate values
│   └── Overlays/
│       └── HardwareEventSceneOverlay.cs   # Live status panel in the Scene View
│
├── Examples/
│   ├── HardwareEventDesigner.Examples.asmdef
│   ├── Scenes/                            # Example scene using the Mock provider
│   └── Channels/                          # Test channels (Joystick, Button)
│
└── Documentation~/
    ├── README.md
    └── TROUBLESHOOTING.md
```

## Firmware

### ESP32 + PlatformIO

The ESP32 reads a two-axis analog joystick and a push button. The joystick axis values are normalized to a `0.0`–`1.0` range and transmitted over Serial every 50 ms as a single text line:

```text
JoystickX:0.523    JoystickY:0.812    PushButton:0
```

## Unity — Data Flow

```text
ESP32 (Serial)
   → SerialConnection (dedicated read thread, thread-safe queue)
   → SerialLineParser (text → Dictionary<channelId, value>)
   → SerialHardwareInputProvider (implements IHardwareInputProvider)
   → HardwareInputManager (updates listeners every frame)
   → HardwareEventListener (per channel, decides when to trigger events)
   → UnityEvent (onTriggered / onValueChanged)
```

## Project Status / Future Improvements

* **Text-based protocol → Binary protocol:** Consider switching to a binary protocol if the system needs to scale to a larger number of channels at higher frequencies.
* **Multiple devices:** `HardwareInputManager` currently assumes a single active manager instance. Supporting multiple simultaneous devices would require removing this static limitation.
* **Transport abstraction:** `SerialConnection` could be generalized behind an `IDataConnection` interface to support Bluetooth or other communication transports without modifying the rest of the system.
