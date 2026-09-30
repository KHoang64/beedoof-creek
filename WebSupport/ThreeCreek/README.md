# BeeDoof Creek browser remake

`WebSupport/ThreeCreek` is the static Three.js/WebGPU version. It builds to `Builds/BeaverCreekThree`, which is the directory to deploy to the existing Vercel project `beedoof-creek` (`beedoof-creak.vercel.app`). The old Unity WebGL export remains in `Builds/BeaverCreekWeb` for reference.

## Build and preview

```powershell
cd WebSupport/ThreeCreek
npm ci
npm run build
npm run dev
```

Vite copies `public/vercel.json` and `public/assets` into the output. The browser renderer selects WebGPU when available and falls back to WebGL 2. Phones use a lower render resolution and fewer grass instances; render resolution adjusts if frame rate drops. Rain is a light 2D canvas overlay with an on/off button.

## Source assets and scene

The pond dimensions, bank path, duck positions, four film shots, and overcast palette follow `Assets/BeaverCreek/Editor/BeaverCreekBuilder.cs`. The browser loads the project's Patchmesh duck FBX and textures, SoStylized pine FBXs, and BK broadleaf, grass, fern, and shrub FBXs and textures. Repeated background vegetation is instanced procedural geometry to limit draw calls. Every loaded mesh receives a browser-compatible material.

The Route 209 file is copied from `Assets/route 209 day EXTENDED pokemon d p pt.wmv.mp3`. Music starts after a user gesture because browsers block unsolicited audio. The volume setting persists locally. Confirm redistribution rights for the music and third-party model packs before public distribution.

## Controls

- Desktop: click the scene for mouse look; WASD or arrows to walk, Shift to hurry, R to reset, Escape for the menu.
- Touch: left thumb pad to walk; right pad to look. The menu and reset controls work in portrait and landscape.
- Film: Pause/Play and Replay, or Space and R on a keyboard.
- Rain and music controls remain available while exploring and watching.

## Validation

`node tests/smoke.mjs` checks the scene, menu, film, rain, and music controls at desktop, portrait, and landscape sizes and writes screenshots to `Docs/QA/ThreeCreek`. Set `CREEK_URL` to test a different local origin. The checks run in desktop Chrome with mobile viewport emulation. A 60 fps claim still needs measurement on physical target phones; the adaptive renderer aims for that rate but cannot guarantee it on every device.
