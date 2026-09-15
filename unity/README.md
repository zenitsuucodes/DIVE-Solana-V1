# DIVE Unity game

Open this directory with Unity 6000.6.0f1 and Web Build Support. Open Assets/Dive/Scenes/FirstPerson.unity to play in the editor. DIVE > Build browser preview creates Builds/Web; from the repository root, run node scripts/serve-unity.mjs and visit http://127.0.0.1:5186/.

## Gameplay

Single-player 3v3: you and two blue bots against three coral bots, first to three goals. WASD swims, mouse looks, Space rises, Ctrl dives, left click shoots or steals, E passes, Escape pauses. When browser mouse capture is unavailable, hold right mouse to look. Bots pursue the puck, support, defend and pass forward.

## Visuals

Original Blender divers use 15 bones and Idle, Swim and Shoot animations. First-person gloves and a hockey stick are animated separately. FBX exports use FBX_SCALE_ALL to preserve consistent mesh and animation units. The built-in render pipeline uses refractive water, a 512px planar reflection, pool caustics, bubbles, ladders and goal nets. Camera color grading is disabled after a rendering issue.

## Verification — September 15, 2026

Unity Web build succeeded: 21,354,455 bytes. Browser startup, a complete bot-driven match to three goals, replay and a brief movement input were exercised. Browser console returned no warnings or errors after the match. Broad hardware testing, gameplay balance and sustained mouse-look testing remain outstanding.

## Limits

Local practice only: no wallet connection or Solana Devnet transactions. The top-right status says Devnet not connected. No Unity MCP connector is installed. Legacy top-down scripts are retained as reference and are not the active game. See ../ASSETS.md for provenance.

## Audio and kickoff update

The game now bundles 18 clips, including four ElevenLabs water/action effects, six announcer lines, one instrumental and seven original synthesized cues. All clips preload; match entry waits until they are decoded. Music crossfades at its loop boundary and ducks under announcements. M or the on-screen Sound button toggles a saved mute preference.

A randomly selected team receives exclusive starting possession during a four-second frozen countdown. After goals, the conceding team receives the restart. Blue restarts give the human player the puck. Match-end releases mouse capture.


Final browser build: 23,216,206 bytes. Verified all 18 clips decoded before entry and the browser audio context resumed after clicking Play. No unloaded-audio messages occurred during active gameplay after the fix. Verified Coral opening possession, four-second restart countdown after a Blue goal, a complete first-to-three match and replay. M toggles the label and persists through reload. The final Web output was scanned for the configured API key with zero matches. Listening balance across devices and both teams' starting randomness have not been exhaustively tested.

