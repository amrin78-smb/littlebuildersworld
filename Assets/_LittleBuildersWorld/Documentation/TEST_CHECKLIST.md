# Test Checklist — Little Builders World

## Automated

| Command | What it covers |
|---|---|
| `Tools/setup.sh` | Regenerates everything; `ProjectValidator` fails on any empty reference, missing scene/prefab script, duplicate activity id, wrong vehicle count, missing ARMv7/ARM64 or PowerVR filter |
| `Tools/run_tests.sh EditMode` | Drive model (acceleration cap, auto-brake on release, brake→reverse, turn-in-place, steering smoothing, reverse steering, no hidden speed when blocked, input clamping); save round-trip, missing/corrupt/unversioned/newer files, sanitising, no personal-data fields; star rewards pay once, unlocks never removed; activity lifecycle, restore clamping, abandon & resume |
| `Tools/run_tests.sh PlayMode` | Real town scene: enter/drive/release-stop/exit, cannot enter a vehicle in use, hold button released by the pointer watchdog, excavator scoop → one action at a time, leaving keeps the load |
| `Tools/autopilot.sh` | Linux test player, full scripted play-through with real driving along routes and the action button: menu → town; basic driving; yard dig pit loads the truck; house clear / dig / walls / roof / doors / garden; roadworks clear / fill / roll; stuck detection + reset; out-of-world recovery; pause/resume; save + reload restores stars and finished projects. Screenshots + `autopilot_report.txt` in `Logs/AutoPilot/` |
| `Tools/build_android.sh` | APK build + `verify_apk.sh` (both ABIs, PowerVR filter, landscape) |

## Manual (device) — must be done by a person

| # | Test | How | Result |
|---|---|---|---|
| 1 | Menu → world | Tap Play | |
| 2 | Enter/exit each vehicle | Drive bubble, vehicle tap, selector row; Exit button | |
| 3 | Forward / reverse / steer / brake feel | Hold Go, Back, arrows; Easy Drive with one finger | |
| 4 | Touch release always stops | Slide finger off a pedal; pull down the notification shade while holding Go; app switch while holding | |
| 5 | Camera follows, no clipping | Drive past the shed, water tower, houses; reverse; tight turns | |
| 6 | Recovery | Push into a fence 5 s → reset pulses → tap reset | |
| 7 | Excavator scoop/load/unload | Yard pit, truck parked beside | |
| 8 | Truck load & deliver | Hopper (gravel) and brick depot | |
| 9 | Bulldozer clearing | House plot debris into the skip | |
| 10 | House stages | All 6 stages visibly build | |
| 11 | Abandon & resume | Leave mid-stage, drive away, come back; quit the app mid-stage, reopen | |
| 12 | Save after restart | Kill the app from Recents, reopen, Continue | |
| 13 | Pause/resume | Hold gear 1.5 s; also home button + return | |
| 14 | Aspect ratios / safe areas | 16:9, 20:9 notch phone, 16:10 and 4:3 tablet | |
| 15 | No console errors | `adb logcat -s Unity` during a session | |
| 16 | Install & launch | Redmi A3 (32-bit, PowerVR) and a 64-bit phone | |
| 17 | **Second launch on PowerVR** | Install, play, swipe away, reopen, drive | |
| 18 | Performance | FPS in the yard and at the finished house with all vehicles there (Redmi A3) | |
| 19 | Parent gate | Toddler taps gear → nothing; adult hold → pause; Reset needs the sum | |
| 20 | Muted play | Mute all; the game is still understandable | |
