<img width="2549" height="1352" alt="image" src="https://github.com/user-attachments/assets/401d7168-5ebc-4e26-8c6c-a6d7d868ce42" /># MR Running Companion

Final Year Project — Unity MR application for Meta Quest 3.

A mixed reality running companion system that overlays a virtual dog companion onto the real outdoor environment via Meta Quest 3 passthrough. Conducted as an A/B user study comparing a virtual companion (Mode B) against a data panel (Mode A) on companionship and motivation.

Demo video: https://vimeo.com/1174433829

## Environment
- Unity 2022.3.62f2c1
- Meta Quest 3 (MR passthrough mode)
- Meta XR SDK (via Unity Package Manager)
- TextMesh Pro
- BLE heart rate monitor (for full functionality)

## Notes on Reproduction
Full reproduction requires a Meta Quest 3 headset and a BLE-compatible heart rate monitor. The codebase includes a simulation mode for heart rate input, which allows partial testing without hardware.
To connect your own heart rate monitor, open the `Init` scene, select `PersistentRoot` in the Hierarchy, and set the **Device Name Filter** field on the **Heart Rate Ble Reader** component to your device's Bluetooth name (or leave empty to connect to the first available device).

## Repository Structure
- `Assets/Script/` — C# source code
- `Assets/Scenes/` — Unity scenes (Init, Menu, TestA, TestB)
- `Packages/` — Package dependencies
- `ProjectSettings/` — Unity project settings
