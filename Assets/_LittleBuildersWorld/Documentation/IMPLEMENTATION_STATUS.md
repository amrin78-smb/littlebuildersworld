# Implementation Status — Little Builders World

_Last updated: 2026-11-10. "Verified" means checked by an automated test or the automated play-through in this
repository. v0.2.1 was played by the owner on a Redmi A3, a Galaxy S25 Ultra and a TCL Tab 10 ("plays well",
"voice is ok"). The v0.3.0 art pass below is not yet tested on a phone._

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
| EditMode tests | **34 / 34 pass** (adds helper timing, language and Thai mark placement) |
| PlayMode tests (real town scene) | **6 / 6 pass** (steering turns and stays grounded/upright; bulldozer pushes the house crate onto the stripes and it counts; a piece wedged against the far fence goes home, a reachable one stays) |
| Automated play-through (`Tools/autopilot.sh`) | Run 19 (v0.3.0 art pass): **ALL 121 CHECKS PASSED** — 24/24 stars, all 8 unlocks, all projects restored after reload; 7 route legs needed a nudge (test-driver paths, warnings only). Run 18 (before the wedge fix) failed playground clearing: the test bulldozer shoved a rock against the west fence where it could not get behind it — fixed in the game (see Art pass). Earlier: run 17 (v0.2.1): **ALL 121 CHECKS PASSED** — 24/24 stars, all 8 unlocks; house, roadworks and playground complete and restored after reload; helper lines and Thai switch checked; steering check 27°. 3 route legs needed a nudge (low FPS), logged as warnings. (The stuck-test spawn was moved off the fence afterwards; not re-run.) |
| Android release APK v0.2.1 (code 3) | **Builds** (~60 MB); `verify_apk.sh`: `arm64-v8a` + `armeabi-v7a` ✔, `.*PowerVR.*` filter ✔, landscape ✔, **no INTERNET permission** ✔ |
| On a physical phone | v0.2.1 played by the owner on the Redmi A3 ("plays well so far"), Galaxy S25 Ultra and TCL Tab 10 tablet ("works fine", "voice is ok"). Close-and-relaunch, Thai text and frame-rate numbers not yet reported. v0.3.0 not yet on a phone |

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
| Voice prompts (Should-have) | **Done** — 47 helper lines in English and Thai, generated offline (Kokoro / VachanaTTS, like Little Burger World); verified the right line is chosen in the play-through; how it sounds is unverified on a phone |
| Text speech bubbles | **Done** — every voice line also shows in a speech bubble with a picture (vehicle portrait or the button to press); works muted; tap the foreman to repeat |
| Friendly builder mascot (Should-have) | **Done** — the foreman helper (portrait in the HUD, a waving figure in the yard) |
| Thai language | **Done** — grown-up setting, defaults to the device language; Mitr font fallback; tone marks placed by `ThaiText` (TMP has no Thai shaping) — check on a phone |
| Playground project (GDD "Later") | **Done, verified** — opens at 6 stars; bulldozer clear → excavator sandpit → truck slide (2 loads) → truck swings (2 loads, play-parts depot) → roller soft mats → climbing frame, roundabout, children |
| More unlocks | **Done** — 24 stars total; new: playground area (6), forest paint (19), rainbow arch (23) |
| Ambient animation (Should-have) | Done (windmill, trees, clouds, flags, beacons, smoke) |
| Guidance arrow + floating vehicle markers | Done |
| Stuck/out-of-world recovery | Done, verified |
| Vehicles staying on the ground | **Fixed** — the play-through's new float diagnostic found vehicles (mostly the truck) rising 0.4–1.8 m: the body could tilt despite the rotation lock, and an airborne vehicle kept any upward speed. The controller now rebuilds an upright rotation every step and never rises while airborne; 0 float events since |
| Tablet / aspect-ratio scaling (width match below 16:9), safe areas | Implemented; only 16:9 (1280×720) exercised automatically |
| Android performance | Owner reports v0.2.1 plays well on the Redmi A3; not profiled (no FPS numbers) |

## Play-through reliability

The play-through steers vehicles with scripted stick input on a software-rendered desktop player that runs at only
2–6 FPS on this machine. At that frame rate the scripted steering overshoots, so some route legs time out and the
test nudges the vehicle to the waypoint (logged as WARN). Runs differ: run 5 passed every check; other runs failed
only where the test's own route was blocked (a parked vehicle in the way, the truck stopping at the edge of the
excavator's 8.5 m reach). The test now parks/returns vehicles like a tidy player. These are test-route issues, not
game-logic faults, but the scenarios are worth re-checking by hand on a phone.

(Resolved in v0.2.1: the cause was a rotation-threshold bug — see the bug sweep.) Previously, since run 7, the very first steering check in the test player read 4° (it read 70–78° in runs 1–6), even when
counted in physics steps. The identical manoeuvre in the real town scene under the PlayMode test runner turns 69°,
and the player drives every later route normally, so this is treated as an artefact of the test player's start-up,
not a steering fault. Not root-caused.

Game bugs the play-through found and that are fixed: clearing bounds measured from the wrong origin (rocks were
reset every tick); rocks could be pushed through a fence and lost (now they return to their start); vehicles could
ride up onto fences (colliders are now taller invisible walls); a white panel covered the truck bed; crawler-track
faces were inside out; a null reference during scene unload.

## Playground, garden, people and ground (v0.6.0)

* `build_env.sh` gains three groups and the ground tiles: Play (sandpit, slide, swings, climbing frame, roundabout),
  Garden (fountain, duck pond, bench, meadow tuft and flowers), People (foreman, KidA–C, resident), Ground (seamless
  512² grass/dirt/sand). Same pivots as before (SwingPivot0/1, Spin, Flag, Duck0/1, WaveArm), same colliders.
  Bake time ~14 min for Play + Garden + People.
* The meadow (130 flowers, 160 tufts) is now one combined mesh on the Garden material.
* Town scene: 807 mesh renderers (v0.5.0: ~1016), ~526k vertices (v0.5.0: ~511k).
* EditMode 34/34, PlayMode 6/6, autopilot ALL PASSED (24 stars, 5 route warnings). Not yet measured on the Redmi A3.

## Blender environment (v0.5.0)

* `Tools/blender/build_env.sh` (Nature/Street ~5 min, Buildings ~35 min with its 2048² atlas) rebuilds trees, bush,
  debris, fence, lamp, barrier, sign post, potholes (3 stages), 4 town houses, shed, water tower and windmill as
  smooth baked models; `PropBuilder`/`TownBuilder` use them when present (same pivots: Sway, Blades; same colliders).
* Red/white kerbs along all roads. Showcase now hides the end-of-roadworks lines like the game does at start.
* Town scene: ~1016 mesh renderers (was 2555), ~511k vertices (was ~504k).
* Not yet measured on the Redmi A3.

## Blender vehicle models (v0.4.0)

* New offline pipeline: `Tools/blender/build_models.sh` (Blender 5.2, headless, ~10 min per vehicle) scripts a
  smooth model, bakes one texture per vehicle (colour × soft shadow, gloss, glow mask) and writes a mesh pack;
  `BlenderModels` (Setup) turns it into meshes under the same pivots, so wheels, steering, the excavator arm, the
  tipping bed, tracks (scrolling belt, spinning sprockets/road wheels), beacon and paint work unchanged.
* All four vehicles converted: bigger faces with brows and smiles, rounded cabs, curved bulldozer blade, chunky
  treaded tyres, chrome bumpers/rams, glowing lights. Town scene ~504k vertices (v0.3.0: ~490k).
* Checked with `Tools/showcase.sh` renders (now incl. front and face close-ups). EditMode 34/34, PlayMode 6/6.
* Not yet measured on the Redmi A3 (frame rate, memory with four 1024² vehicle textures).

## Art pass (v0.3.0)

All still generated in code (no third-party art). Checked with `Tools/showcase.sh` renders and in-game screenshots.

* Vehicles: readable backs (tail lights/indicators, plates, grilles, hazard chevrons, mud flaps, bulldozer ripper),
  treaded tyres with repaintable hub caps, mirrors, exhausts, track rollers, excavator rams, cheeks on every face.
* People: rounder figures with big heads, smiles, cheeks, ears, shoes; the resident uses the same figure.
* World: horizon hills + far ground (no visible world edge), duck pond (bobbing ducks), ~150 flowers and 200 grass
  tufts as a few combined meshes, framed windows/flower boxes, windmill sails, glowing lamps, tower braces,
  realistic potholes, dotted play mats, framed windows on the built house.
* Fixed: the playground roundabout spun like a wheel (new `AmbientMotion.Kind.Turn`).
* Budget: bevel/sphere detail scales with size and vehicle parts are merged per material, so the town scene went
  from ~513k to ~490k vertices despite the extra detail. Not yet measured on the Redmi A3.
* Gameplay fix found by the play-through: a piece pushed against the fence on the far side from the stripes (no
  room for the bulldozer behind it) now returns to its start after 4 s at rest (`BulldozerClearingActivity.Wedged`).

## Bug sweep (v0.2.1)

After the owner's v0.2.0 test ("keeps asking to push rocks but there are none"; "bulldozer tracks don't show
properly"), a 12-agent sweep (6 area finders + adversarial verifiers) confirmed and this release fixes:

* **Clearing soft-lock**: debris carried by the blade was placed straight through the 3 m fence colliders (debris
  has no collider), and beyond the house's north fence / playground's south fence it stayed inside the 14 m reset
  circle, so the stage could never finish. Pushes now stop at walls (`DebrisPusher` sphere-cast), each fenced plot
  has a play-area box (anything outside pops home), and the arrow points at the nearest remaining piece.
* The dig mound was visible during clearing and hid the crate's start spot: the mound now appears only after
  clearing (`ProjectActivity.ApplyState` is order-independent), and the crate/log start spots moved clear of it and
  of the north fence.
* **Crawler tracks**: side plates were wound inward (back-face culled from outside) and inset, leaving a slit.
* Loaded truck heard "tip out the load" instead of where to take it; a hint could be skipped without being heard;
  "tap the paint button" no longer plays while driving (button hidden).
* Gentle steering was dropped by a too-coarse angle threshold (regression from v0.2.0).
* Reset / return-home never lands a vehicle on a parked one; locked playground gate is solid.
* Repeatable dig pit no longer stays closed after a save in the 2.5 s before it restarts.
* Double-tap on Exit no longer also presses Home; parent gate locks for 30 s after a wrong answer; pause gear
  survives lost touches; steering sensitivity applies at once; reset keeps the language; performance governor
  ignores load/resume hitches; scene fade blocks taps; unlock card no longer covers the helper on 4:3.

Not fixed (low): a mid-stage restart restores cleared debris/rolled patches by count, not by which ones.

## Known limitations

* Vehicle positions and loads in buckets/beds are not saved; vehicles return to their yard bays on load.
* House parts and the skip have no colliders (vehicles can drive through the finished house walls).
* The Bridge area is not built. Playground props have no colliders (like the house), so machines can drive through them.
* Lighting is real-time (one directional light) rather than baked.
* No LODs (meshes are low-poly).

## Blockers / needs the owner

1. **A GLES3/Vulkan Android phone over adb** (ideally the Redmi A3). The only paired device, a Mi Box 4, has a
   Mali-450 (OpenGL ES 2.0) and cannot run any Unity 6 game.
2. On-device checks: install, touch feel, second-launch test on PowerVR, FPS, safe areas — see TEST_CHECKLIST.md.
3. Final application id and release keystore.

## Next steps

1. Install on the Redmi A3; tune speeds/turning and camera distance from a child's real play.
2. Tune helper voice volume / repetition from real play; consider a recorded voice.
3. House wall colliders; baked lighting pass; bridge or beach area; cement mixer / crane.
