# Web deployment handoff

## Export

- Status: Unity WebGL build succeeded.
- Unity: 6000.5.5f1, WebGL, release build.
- Scenes: `BeaverCreek_Menu`, `BeaverCreek_FirstPerson`, `BeaverCreek_Cutscene`.
- Output: `Builds/BeaverCreekWeb/`.
- Build report: 71,898,872 bytes, 59 seconds, 0 errors, 4 warnings.
- Main compressed files: 57 MB data, 11 MB wasm, 76 KB framework, 28 KB loader.
- The WebGL-only texture override is capped at 1024 and originals are preserved in `Backups/WebTextureImport`.

## Vercel

The export includes `vercel.json` with the correct WebAssembly, JavaScript and data-file `Content-Encoding: gzip` headers. Deploy from the project root after the Vercel CLI is authenticated:

```sh
npx vercel deploy Builds/BeaverCreekWeb --prod --yes
```

The CLI session was authenticated during this task, but the Codex sandbox could not make the final outbound Vercel API request: its network escalation was rejected by the current usage-limit gate. No deployment URL was created or reported by the CLI. The command above is ready to run in a normal terminal and does not rebuild the project.
