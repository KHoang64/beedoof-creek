# After-rain revision — validation

## Overcast and brown-duck update

On 2026-09-20, both scenes were updated and recaptured with continuous gray cloud cover, no sun disc/direct-sun shadows, dim diffuse daylight, refreshed canopy reflections and brown body/feather materials on all seven ducks. Both serialized scene checks passed again (zero missing scripts and unsupported/null renderer materials). The first-person view and duck close-up were visually inspected. No C# or shader compilation errors were found. Animation controllers and behavior components were preserved; the full Play-mode tests below predate this appearance-only update.

Validated in Unity 6000.5.5f1 / URP on 2026-09-20.

- Both serialized scenes: one active camera, seven ducks, zero missing scripts, zero unsupported or null renderer materials.
- Cutscene ran for 64 seconds: all four shots visited, including the loop restart.
- Supplied animations observed in Play mode: Idle, Swim, Flap, PreenFront, PreenBack. The check observed 1,112 distinct flap wing poses.
- Drizzle and pond ripple effects played, with a measured peak of 222 particles against a combined ceiling of 420.
- First-person keyboard input moved the controller 9.35 metres in the movement test, and it remained grounded.
- Zero runtime errors during both final test runs. No C# compilation or shader compilation errors found in the Editor log.
- Inspected actual camera output for both scene variants, the duck close-up, water reflections, moss coverage, wet trail, and subtle rain/rings.

The initial movement check failed because the automated run had not focused the Game view/locked the cursor. A focused rerun passed; no movement-speed workaround was introduced.

The original open scene was restored after testing. Both new scenes are enabled in Build Settings after the pre-existing SampleScene, whose entry/order was preserved.

## Limits

No standalone player build, performance benchmark, or target-device test was run. Environment reflections are baked rather than planar reflections of the moving ducks. Drizzle is a lightweight visual effect, without physical per-drop surface collisions. The scene is a stylized interpretation rather than a surveyed reconstruction.
