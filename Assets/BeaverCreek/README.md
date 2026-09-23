# Beaver Creek

Two versions of the same stylized woodland pond, inspired by the supplied Beaver Pond footage.

Open either scene from `Scenes` and press Play:

- **BeaverCreek_FirstPerson** — WASD to walk, mouse to look, Shift for faster movement, Escape to release the cursor, click to resume, R to return to the starting point. The pond boundary keeps the player on the bank.
- **BeaverCreek_Cutscene** — a 52-second loop: trail-marker establishing shot, duck close-up, moss-covered pond panorama, and return beneath the canopy. Space pauses and R restarts.

Both scenes reference **Prefabs/BeaverCreekEnvironment**. Change this prefab to update the shared setting. Each scene has its own camera, lighting and color grading.

### Minutes after a shower

Both versions include brighter fresh greens and WaterMoss, damp bark and leaf litter, a muddy trail with reflective hollows, soft humid distance haze, and light lingering drizzle. The installed SoStylized pack has no rain prefab, so the effect adapts BK's Rain prefab with a project-local URP streak/ripple shader. `CreekWeather` follows the active camera with the drizzle emitter and maintains sparse expanding rings on the open pond. The combined particle ceiling is 420; no GameObjects are spawned per raindrop.

The sky is now completely covered by a gray cloud layer, with no blue openings or sun disc. Direct sunny shadows are disabled and only a weak cool diffuse fill remains. All seven ducks use the Patchmesh brown body and feather materials, retaining their original animation controllers and behavior.

### Assets and behavior

BK WaterMoss covers the far pond, with a ragged opening around the resting log. BK foliage, roots, rocks and terrain frame the shoreline. Three SoStylized pine variations form the layered background. Seven Patchmesh ducks use the supplied idle, wing-flap idle, swimming and front/back preening animation clips, with independent behavior timing.

### Authoring and checks

Tools → Beaver Creek contains deterministic scene authoring and validation commands. **Rebuild overwrites the generated scenes and prefab**, so preserve any manual edits first. It does not overwrite the vendor demo scenes/materials. Both scenes are appended to Build Settings, preserving existing entries.

The Validation folder contains camera previews and exact validation reports. `Runtime_AfterRain.png` and `Runtime_Ducks.png` are captured during actual Play mode. These are not an exported game. The environment reflection is baked; moving ducks are not reflected dynamically. Rain is a visual effect, not a gameplay/weather simulation.
