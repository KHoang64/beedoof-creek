# Web deployment — 2026-09-24

Live: https://beaver-creek-web.vercel.app/

Vercel project: khoang99s-projects/beedoof-creek.
Production deployment: dpl_6Z7g9d2TRA8gu6UEaGyJpTR31MEv (Ready).
The original live alias was explicitly assigned to this deployment.

Unity 6000.5.1f1, WebGL release, three scenes: Menu, FirstPerson, Cutscene.
Final build succeeded in 46.765 seconds with exit code 0 and no build errors.
Build log: Logs/WebGLFinal.log. Export: Builds/BeaverCreekWeb.
Clean upload directory: Builds/BeaverCreekDeploy (HTML, hosting config and
four player files only). About 68.8 MiB uploaded.

Web-only shader/material copies fix unsupported BK shader targets and water
tessellation. Native scene and vendor materials are preserved. Mobile and desktop
target 60 FPS; physical-device performance is not yet measured.

Validated local desktop and portrait rendering, mode navigation, gzip integrity,
WASM signature, and the live cutscene at the original production URL. No browser
errors were captured. Existing URP/vendor deprecation and unused FSR warnings
remain. Details: WebGLShaderFix.md.

Rebuild using Tools → Beaver Creek → Build WebGL for Vercel, or the
BeaverCreek.Editor.BeaverCreekWebBuild.BuildWeb batch entry point. Restart Unity
following WebGL module installation. Hashed filenames are disabled because this
Unity version's batch exporter hit Bee's buildprogram retry limit; Vercel headers
revalidate HTML and build assets to prevent stale players.

The machine's global Vercel CLI 39 is too old for the upload endpoint. This
deployment used npx --yes vercel@latest (59.26.0).
