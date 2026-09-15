# DIVE — Unity browser game

The current first-person underwater hockey game: human divers, blue and coral teams, poolside splash screen, reflective blue water, audio, and 3v3 gameplay. This repository contains the compiled Unity browser player and source. The old top-down prototype is excluded.

## Deploy the game

Import this repository into Vercel. The included `vercel.json` explicitly uses **npm run build**, output **dist**, and no framework preset. This copies the current Unity player; it does not run Vite or build the former prototype. Only `/` is an intended public page.

The compiled player in `web/player/Build` is committed so deployment does not require Unity on the build server. To publish future Unity changes, rebuild with **DIVE > Build browser preview**, copy the resulting Build folder into `web/player/Build`, then run `npm run build` and push.

## Run the complete game locally

Run `npm ci`, `npm run build`, then `npm start`. Open http://127.0.0.1:5186/.

The local Node server sponsors real Solana Devnet V1 event receipts. A static Vercel deployment serves the Unity game, but does **not** run the local sponsor server. Hosted receipts need a separately configured backend; the wallet seed and API keys are deliberately excluded. Do not put them in frontend files.

WASD swims, mouse looks, Space rises, Ctrl dives, Shift sprints, hold/release click shoots, E passes, Escape pauses, and M mutes. One team starts with possession; the conceding team restarts after goals.

`npm test` checks Devnet ordering, retries, duplicate prevention and confirmation recovery. Open `unity` with Unity 6000.6.0f1 and Web Build Support to edit the game. The Unity MCP companion package is included via a relative path in `vendor`.

## Credits

See ASSETS.md for Meshy, Blender and ElevenLabs asset provenance. Made with AnkleBreaker MCP.

![AnkleBreaker MCP](vendor/unity-mcp-plugin/icon.png)
