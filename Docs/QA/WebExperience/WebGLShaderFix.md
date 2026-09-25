# WebGL pink-material investigation (2026-09-23)

Live reproduction: https://beaver-creek-web.vercel.app/ → Watch the cutscene.
Ground, broadleaf vegetation and pond are magenta; pine trees and ducks render.

The seven BK shaders require target 4.5 (OpenGL ES 3.1), while WebGL 2 uses
OpenGL ES 3.0. BK/Water additionally enables hardware tessellation in nine
passes. These are incompatible requirements, independently of hosting headers.

Changes:
- The exporter generates separate shader/material assets under
  `Assets/BeaverCreek/Generated/WebShaders` with unconditional target 3.5.
- Generated water shaders remove tessellation defines and stage pragmas, using
  the existing non-tessellated vertex path. Native vendor assets are unchanged.
- An `IProcessSceneWithReport` callback replaces materials only in WebGL build
  scenes. Missing prepared materials fail the build instead of shipping pink.
- The browser canvas is capped at 921,600 pixels on mobile and 2,073,600 on
  desktop, including resize/orientation changes. The existing DPR limits remain.
- Both PC and mobile target 60 FPS, per user request. This is a target, not a
  measured performance guarantee.
- Every web export configures gzip. HTML and build-file responses revalidate
  instead of retaining old content. Hashed filenames are disabled because this
  Unity version's batch exporter hit Bee's six-buildprogram-run limit with them.

Static checks passed: Vercel JSON parsing, canvas aspect/pixel-budget checks at
390×844, 844×390, 1920×1080 and 3840×2160, and scoped Git diff whitespace check.

The first build reported Succeeded but logged six errors and produced no player.
Four were missing HullFunction kernels: conditioning tessellation pragmas on
SHADER_API_GLES3 did not work. That approach was removed in favor of explicit
web copies. Final packaging also logged WebGL not supported after installing
the module into the running editor. WebGL Build Support 6000.5.1f1 installed
successfully. A normal editor shutdown and fresh batch session loaded the module.

The exporter now rejects nonzero build errors and missing index/wasm artifacts.
Static generation checks passed for all seven shaders: no target 4.5 or enabled
tessellation stage remains in the generated web source.

Final local validation (2026-09-24): Unity 6000.5.1f1 WebGL release batch build
succeeded in 46.765 seconds, with exit code 0 and no build errors. Log:
`Logs/WebGLFinal.log`. Existing vendor/URP deprecation warnings remain.
The generated water shader contains 104 GLES3 programs; the failed version had 0.

The local player was visually inspected at 1280×720 and 390×844: pond water,
ground, broadleaf vegetation, grass, ducks and pine trees render without magenta.
Cutscene playback, menu navigation and loading first-person exploration worked.
No browser errors were captured. URP's unused FSR upscaling warning remains.
Canvas sizing, referenced file existence, gzip integrity and WASM magic passed.
Compressed payload: data 60,402,844 bytes; wasm 11,592,293 bytes;
framework 74,510 bytes; loader 27,221 bytes.

Physical Android/iOS frame-time and sustained 60 FPS measurements remain unverified.

Deployment: Vercel production `dpl_6Z7g9d2TRA8gu6UEaGyJpTR31MEv` is Ready.
The existing https://beaver-creek-web.vercel.app alias was explicitly updated to
https://beedoof-creek-78obnz78n-khoang99s-projects.vercel.app on 2026-09-24.
Only index.html, vercel.json and the four player files were uploaded; debug
symbols and Unity source were excluded through a separate deployment directory.
The original production URL was reloaded and its live cutscene visually checked:
forest, pond and ducks render without pink, and no browser errors were captured.
Production wasm returned HTTP 200, application/wasm, gzip encoding and no-cache.

Manual device regression procedure:
1. Explore and watch all four cutscene shots: no magenta terrain, foliage or pond.
2. Check browser Console for shader compile/link errors.
3. Exercise movement/look together, menu/reset and cutscene pause/replay.
4. Rotate a phone and resize a desktop window; verify canvas aspect and input.
5. Measure sustained frame time on PC and physical Android/iOS devices.

The generated web copies are regenerated from the vendor sources on export;
review the transformation if the vendor changes shader properties or stages.
