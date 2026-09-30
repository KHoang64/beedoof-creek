# BeeDoofCreek — Beaver Creek

The complete Unity source project for a woodland creek experience with animated ducks, rainy scenery, first-person exploration, and a cinematic viewing mode.

**[Play the Three.js remake on Vercel](https://beedoof-creak.vercel.app)**

## Open the project

1. Install Unity **6000.5.5f1** through Unity Hub, including Web build support if you want to export the browser version.
2. Install Git LFS, then clone the repository:

   ```sh
   git lfs install
   git clone https://github.com/KHoang64/beaver-creek-web.git
   cd beaver-creek-web
   git lfs pull
   ```

3. Add the cloned folder to Unity Hub and open it. Unity restores packages and regenerates its local Library folder.
4. Open `Assets/BeaverCreek/Scenes/BeaverCreek_Menu.unity` to start the experience.

Use a Git clone with Git LFS to obtain the complete binary assets.

## Repository contents

- `Assets/`: all Unity assets, scripts, scenes, prefabs, and their `.meta` files.
- `Packages/` and `ProjectSettings/`: package versions and Unity project configuration.
- `Blender Export/`: supporting model source/export files.
- `Docs/`, `Data/`, and `WebSupport/`: project notes, validation evidence, supporting data, and web hosting utilities.

Textures, models, binary terrain data, and other binary assets use Git LFS. Text-based Unity scenes, prefabs, settings, scripts, and metadata remain ordinary Git files. Generated caches, local backups, IDE files, and build output are excluded.

## Controls

- **Desktop:** WASD or arrow keys to move, mouse to look, Shift to move faster, R to reset, and Escape for the menu.
- **Touch:** left thumbstick to move, swipe on the right to look, and use the Menu and Reset buttons at the top.
- **Cutscene:** Pause/Play and Replay, or Space and R on desktop.

## Web hosting

The Three.js/WebGPU remake is hosted at **https://beedoof-creak.vercel.app**. Its source and build instructions are in `WebSupport/ThreeCreek/`. The original Unity scenes and WebGL exporter remain in the project.

Run `npm ci` and `npm run build` in `WebSupport/ThreeCreek`, then deploy `Builds/BeaverCreekThree` to the existing Vercel project. The Unity WebGL exporter remains available for a separate legacy build.

See the [Three.js build and controls documentation](WebSupport/ThreeCreek/README.md), [legacy WebGL notes](WebSupport/README.md), and [project context](Docs/AI/UnityProjectContext.md) for more detail.

Third-party assets remain subject to their original licenses.
