# DIVE Unity project

Open this folder with Unity 6000.6.0f1 and Web Build Support. Open `Assets/Dive/Scenes/FirstPerson.unity` and press Play. No third-party editor connector is required.

## Gameplay

Single-player 3v3 underwater hockey: you and two blue bots face three coral bots. First to three goals wins. One team starts with possession; after a goal, the conceding team restarts.

WASD swims, mouse looks, Space rises, Ctrl dives, left click shoots or steals, E passes, and Escape releases mouse capture. Hold right mouse to look when pointer lock is unavailable.

## Build

Use **DIVE > Build browser preview**. Copy the four generated `Builds/Web/Build/Web.*` files into `web/player/Build/` at the repository root, then run `npm run build` there. The production output is `dist/`.

The deployed browser wrapper connects match events to the Vercel Devnet API. Unity contains no sponsor private key. The panel shows five recent receipts with links to Solscan.

See [Development](../docs/DEVELOPMENT.md) and [Credits](../docs/credits/README.md).
