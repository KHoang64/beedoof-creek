# Beaver Creek on the web

The exported app is Unity WebGL, preserving the existing environment, duck animations, weather and collision. A Three.js conversion was not necessary.

## Build

In Unity 6000.5.5f1:

1. Tools → Beaver Creek → Prepare menu and web controls (only needed when regenerating the original scenes).
2. Switch the active build platform to Web.
3. Tools → Beaver Creek → Build WebGL for Vercel.

The build method includes only Menu, FirstPerson and Cutscene, in that order. Existing unrelated Build Settings entries are retained but excluded from this export. Output: `Builds/BeaverCreekWeb/`.

`Assets/WebGLTemplates/BeaverCreek/index.html` contains the responsive loading page. Gzip hosting rules are copied from `WebSupport/vercel.json` into the export by the build method. Publish only the export directory; it contains no Unity source, credentials, or backups.

## Preview and publish

Run `python3 WebSupport/serve.py` and open http://127.0.0.1:8765. The server supplies the gzip and WebAssembly MIME headers required by the build.

From the export directory, use `vercel deploy --prod` after signing in. `vercel.json` is already configured for static hosting.

## Controls

- Menu: Explore in first person / Watch the cutscene.
- Desktop exploration: WASD or arrow keys, mouse look, Shift for faster walking, R to return to the start, Escape for menu. Click “Click to explore” if the browser releases mouse capture.
- Touch exploration: left thumbstick to walk and swipe the right side to look, simultaneously. Menu and Reset are at the top. Touch controls are detected automatically and can also be toggled manually.
- Cutscene: Pause/Play and Replay. Space and R also work on desktop.
- Layout updates for portrait, landscape, viewport changes and device safe areas. Touch state clears when focus, scene or layout changes.

## Validation

`BeaverCreekWebCheck.Start()` exercises menu navigation, simultaneous movement/look pointers, pointer ownership, grounding, input cancellation, pause/replay and repeated scene transitions in Play mode. It restores the original scene setup. See `Docs/QA/WebExperience/Runtime.txt` and screenshots.

Project originals before this change are in `Backups/WebExperience/`. That folder is local only and is not deployed.
