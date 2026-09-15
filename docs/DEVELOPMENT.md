# Development

## Repository map

- `unity/Assets/Dive/Scripts`: game rules, bots, controls, characters, audio, and splash scene.
- `unity/Assets/Dive/Shaders`: pool, water, bubbles, and team appearance.
- `unity/Assets/Dive/Resources`: game-ready models, textures, and audio.
- `web`: browser wrapper, transaction panel, and compiled Unity player.
- `api` and `scripts/hosted-api.mjs`: stateless Vercel Devnet endpoints.

## Build and checks

Use Node.js 24. Run `npm ci`, `npm test`, then `npm run build`. Output is `dist/`. Rebuilding Unity requires Unity 6000.6.0f1 with Web Build Support. CI checks the committed player and backend; it does not compile Unity.

## Production backend

Vercel uses the build command and output directory in `vercel.json`. Configure these server-only production secrets:

- `DIVE_SPONSOR_SEED`: base64-encoded 32-byte private seed for a dedicated Devnet sponsor.
- `DIVE_SESSION_SECRET`: cryptographically random signing secret of at least 32 characters.
- `DIVE_ORIGIN`: allowed browser origin, if different from `https://dive-unity.vercel.app`.

Never commit secrets or expose them in browser configuration. Gameplay runs in Unity; the backend submits V1 Memo receipts, not authoritative on-chain game rules.

The browser keeps five recent receipts in memory. The server prepares a transaction without broadcasting, then submits a signed ticket. Retries reuse identical signed bytes. Confirmation comes from Solana, with no receipt database.

## Verification

The backend tests cover invalid requests, ticket tampering, expired sessions, cold starts, duplicate submissions, RPC interruptions, and late confirmations. GitHub Actions runs them and builds the web output on pushes and pull requests.

`node scripts/smoke-hosted.mjs https://your-game.example` explicitly creates three Devnet test receipts and checks confirmations. It requires a funded backend and is not run automatically by CI.

Manual browser checks cover the splash, controls, match start, restart possession, scoring, replay, and transaction panel. Automated Unity gameplay tests are not yet included. The sponsored public endpoint is a Devnet demonstration, not an authenticated competitive game service.
