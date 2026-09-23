# BeeDoofCreek project context

Unity 6000.5.5f1, Universal Render Pipeline 17.5.0. The PC quality level uses `Assets/Settings/PC_RPAsset.asset`. New Input System only; installed Input System 1.19.0. Existing vendor camera examples use legacy input and are not used by Beaver Creek.

## Beaver Creek implementation

- `Assets/BeaverCreek/Scenes/BeaverCreek_FirstPerson.unity`: CharacterController shoreline exploration, WASD/mouse/Shift/Escape/R.
- `Assets/BeaverCreek/Scenes/BeaverCreek_Cutscene.unity`: four-shot, 52-second looping camera sequence. Space pauses; R restarts.
- Both instantiate `Assets/BeaverCreek/Prefabs/BeaverCreekEnvironment.prefab`. Edit this prefab for changes shared by both scenes.
- BK Fantasy Forest supplies Water, WaterMoss, terrain textures, broadleaf trees, bushes, ferns, grass, rocks, roots and leaf litter.
- SoStylized supplies three non-snow pine variants across the pond.
- Patchmesh Country Critters supplies seven ducks and all duck animation clips. Five rest on the log; two swim. Per-bird deterministic random timers alternate wing flapping, front preening and back preening.
- Scene-specific materials are copies under `Assets/BeaverCreek/Materials`; vendor demo scenes and materials are preserved.
- `CreekFirstPerson`, `CreekDuck`, `CreekCinematic`, `CreekWeather`, and `CreekWind` are runtime-only components. All authoring/validation code is in the Editor folder.
- The after-rain revision uses BK Rain with a local URP particle shader, bounded camera-following drizzle, horizontal pond ripple particles, reflective trail hollows, wet material copies and brighter foliage. No SoStylized rain asset was present in the installed pack.
- The overcast/brown-duck revision uses `BeaverCreek/Fully overcast sky`, continuous opaque gray cloud cover, no sun disc, subdued diffuse daylight without direct shadows, and brown materials on all seven ducks. The `OvercastBrown` editor command applies this as a targeted change without rebuilding the environment layout.

## Validation and authoring

Use Tools → Beaver Creek for rebuilding or validation. Rebuilding regenerates the authored environment, scene files and generated assets; do not rebuild after manual scene edits without first backing them up. The deterministic seed is 1957.

The editor bridge consumes only an explicit command from `Temp/BeaverCreek.request`; it does not execute arbitrary code. Available commands are `inventory`, `Build`, `Validate`, `Smoke`, and `SmokeFirstPerson`. The bridge writes status to `Temp/BeaverCreek.result`. Rebuild and validation temporarily close only clean generated scenes and restore them afterward; they refuse to discard unsaved Beaver Creek scene edits.

Validation captures the actual URP camera output, checks serialized missing references and material support, and runs a separate Play-mode check for animation, shot progression, keyboard movement and grounding. Reports and screenshots are in `Assets/BeaverCreek/Validation`.

No Unity MCP provider is connected. Scene and prefab authoring uses Unity Editor APIs through local scripts, with native Editor refresh through the existing app. No packages were added.

## Known boundaries

This is a stylized composition study of the supplied Beaver Pond photos/videos, not a geographically exact scan. The water uses a baked canopy reflection, not screen-space/planar reflections of animated ducks. Lighting is realtime with a separately captured reflection probe; no full lightmap bake was performed. Standalone builds need their own target-platform validation.

## Web experience revision — 2026-09-21

- Added `BeaverCreek_Menu.unity` as the first Build Settings scene. The web exporter explicitly includes only Menu, FirstPerson and Cutscene.
- `CreekExperience` builds the mode menu, safe-area-aware HUD and playback controls with uGUI and Input System UI events. Each scene owns its UI and EventSystem; nothing persists across scene loads.
- `CreekTouchPad` owns one pointer per movement/look pad and clears input on release, disable, focus loss and resolution changes. `CreekFirstPerson` accepts touch and keyboard input without requiring a mouse on mobile.
- `BeaverCreekWebBuild` prepares scene hosts and builds the Unity WebGL player to `Builds/BeaverCreekWeb`. Custom template: `Assets/WebGLTemplates/BeaverCreek`. Gzip/MIME hosting headers and local server: `WebSupport`.
- New bridge commands: `WebPrepare`, `WebSwitch`, `WebBuild`, `WebSmoke`. Write requests atomically (write a temporary file then rename); a partially written mailbox can be consumed as an empty command.
- Automated interaction report and captures: `Docs/QA/WebExperience`. Pre-change files and scenes: `Backups/WebExperience` (no Git repository is present).
- Web target uses the existing mobile URP quality setting. The scene camera requests depth/opaque textures on WebGL for the creek water. Pixel ratio is capped by the browser template. Physical-device performance is not yet verified.
