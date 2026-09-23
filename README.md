# Beaver Creek

A playable Unity WebGL experience with first-person creek exploration, animated ducks, rainy woodland scenery, and a cinematic viewing mode.

## Play online

**[Launch Beaver Creek on Vercel](https://beaver-creek-web.vercel.app)**

## Controls

- **Explore on desktop:** WASD or arrow keys to move, mouse to look, Shift to move faster, R to reset, and Escape for the menu.
- **Explore on touch devices:** use the left thumbstick to move and swipe on the right to look. Menu and Reset are at the top.
- **Watch the cutscene:** use Pause/Play and Replay, or Space and R on desktop.

## About this repository

This repository contains the exported web player from the BeeDoofCreek Unity project, built with Unity 6000.5.5f1. The Unity source project is maintained separately.

## Hosting

Vercel serves the static files from the repository root. No build command is needed. The included `vercel.json` supplies the gzip encoding and MIME headers required by the Unity WebGL player.

To deploy with the Vercel CLI, run `vercel deploy --prod` from this directory after signing in.

The build files must be served over HTTP with the configured headers; opening `index.html` directly from disk is not supported.
