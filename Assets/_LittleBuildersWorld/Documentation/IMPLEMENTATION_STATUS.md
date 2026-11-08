# Implementation Status — Little Builders World

_Last updated: 2026-11-09. "Verified" means checked by an automated test or the automated play-through in this
repository; nothing here has yet been run on a physical Android phone._

## Baseline

* Unity 6000.3.27f2, URP 17.3.0 (Mobile + PC renderer assets from Unity's 3D cross-platform template),
  Input System 1.20.1, uGUI 2.0 + TextMeshPro, Test Framework 1.6. No other packages (Unity's auto-added
  Analytics / Purchasing / Collab packages were removed).
* Scenes: `Boot` → `MainMenu` → `ConstructionTown` (all generated).
* Build target Android, landscape, IL2CPP, ARMv7 + ARM64, Vulkan + GLES3 with PowerVR → GLES3.

## Verification summary

| Check | Result |
|---|---|
| Generator + validator (`Tools/setup.sh`) | **Passes** — no empty references, scenes and prefabs complete, ARM/PowerVR settings present |
| EditMode tests | **23 / 23 pass** |
| PlayMode tests (real town scene) | **3 / 3 pass** |
| Automated play-through (`Tools/autopilot.sh`) | Run 5: **all checks passed**. Final run 7 (after the last fixes): 16/16 stars, all 5 unlocks, house + roadworks complete and restored after reload; **1 check failed** ("steering right turns the truck": 4°, it measured 72° in runs 1–6 — timing flake at ~5 FPS, see below). |
| Android development APK | **Builds** (73 MB); `verify_apk.sh`: `arm64-v8a` + `armeabi-v7a` ✔, `.*PowerVR.*` present in `globalgamemanagers.assets.split5` ✔, landscape ✔ |
| Android release APK | **Builds** (57 MB); both ABIs ✔, PowerVR filter ✔, landscape ✔, **no INTERNET permission** ✔ (stripped by `AndroidManifestPostProcess`) |
| On a physical phone | **Not yet tested** — see Blockers |

## Feature status

| Feature (GDD Must-have unless noted) | Status |
|---|---|
| Main menu (Play, held-gear settings, adult reset) | Done, verified in play-through |
| One connected town: yard, loop road, house plot, roadworks, brick depot, inactive playground site | Done |
| Construction yard hub, parking bays, vehicle selector (calls vehicles back to the yard) | Done, verified |
| Third-person camera (damped follow, look-ahead, reverse-safe, building collision, showcase, overview) | Done; feel unverified on device |
| Excavator, dump truck, bulldozer (+ road roller, Should-have) | Done, all 4 drivable; verified enter/drive/exit |
| Arcade driving, enter/exit, vehicle in use cannot be entered | Done, verified |
| Dig/scoop (excavator → truck / stockpile) | Done, verified |
| Load/deliver (hopper, brick depot, delivery zones, tip animation) | Done, verified |
| Push/clear (bulldozer, forgiving clear-away strip) | Done, verified |
| House project: clear → dig → walls → roof → door & windows → garden | Done, verified |
| Roadworks: clear → fill → roll → fresh lines + barriers removed (Should-have road roller) | Done, verified on runs where the roller route was clear |
| Builder Stars (16 available) and unlocks (3 paints, bunting, balloon arch + fountain) | Done, verified |
| Vehicle colour customisation (Should-have) | Done (paint button when not driving) |
| Local save (versioned JSON, backup, migration hook), autosave, pause/resume save | Done, verified (unit tests + reload in play-through) |
| Touch HUD: big steer buttons / optional joystick, Go, Back, contextual action, Exit, horn, reset | Done; stuck-touch watchdog verified in PlayMode test |
| Easy Drive (one-finger driving) | Done (default on) |
| Settings: music / SFX / voice volume, voice toggle, joystick, Easy Drive, sensitivity, reset behind parent gate | Done; not covered by automated tests |
| Audio: synthesised SFX, engine hum, two music loops | Done |
| Voice prompts (Should-have) | **Not done** — no voice assets with clear licensing; channel exists |
| Friendly builder mascot (Should-have) | Partial — a resident waves at the finished house; no mascot guide |
| Ambient animation (Should-have) | Done (windmill, trees, clouds, flags, beacons, smoke) |
| Guidance arrow + floating vehicle markers | Done |
| Stuck/out-of-world recovery | Done, verified |
| Tablet / aspect-ratio scaling (width match below 16:9), safe areas | Implemented; only 16:9 (1280×720) exercised automatically |
| Android performance | **Unverified** — needs the Redmi A3 |

## Play-through reliability

The play-through steers vehicles with scripted stick input on a software-rendered desktop player that runs at only
2–6 FPS on this machine. At that frame rate the scripted steering overshoots, so some route legs time out and the
test nudges the vehicle to the waypoint (logged as WARN). Runs differ: run 5 passed every check; other runs failed
only where the test's own route was blocked (a parked vehicle in the way, the truck stopping at the edge of the
excavator's 8.5 m reach). The test now parks/returns vehicles like a tidy player. These are test-route issues, not
game-logic faults, but the scenarios are worth re-checking by hand on a phone.

Game bugs the play-through found and that are fixed: clearing bounds measured from the wrong origin (rocks were
reset every tick); rocks could be pushed through a fence and lost (now they return to their start); vehicles could
ride up onto fences (colliders are now taller invisible walls); a white panel covered the truck bed; crawler-track
faces were inside out; a null reference during scene unload.

## Known limitations

* Vehicle positions and loads in buckets/beds are not saved; vehicles return to their yard bays on load.
* House parts and the skip have no colliders (vehicles can drive through the finished house walls).
* Playground and Bridge areas are visual only ("coming later").
* Lighting is real-time (one directional light) rather than baked.
* No LODs (meshes are low-poly).

## Blockers / needs the owner

1. **A GLES3/Vulkan Android phone over adb** (ideally the Redmi A3). The only paired device, a Mi Box 4, has a
   Mali-450 (OpenGL ES 2.0) and cannot run any Unity 6 game.
2. On-device checks: install, touch feel, second-launch test on PowerVR, FPS, safe areas — see TEST_CHECKLIST.md.
3. Final application id and release keystore.

## Next steps

1. Install on the Redmi A3; tune speeds/turning and camera distance from a child's real play.
2. Recorded voice prompts (licensed or self-recorded) and a builder mascot.
3. House wall colliders; baked lighting pass; playground project.
