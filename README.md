# Little Builders World

A toddler-friendly (ages 2–6) 3D open-world construction playground for Android, made with **Unity 6000.3.27f2 + URP**.
Drive an excavator, dump truck, bulldozer and road roller around a little town, dig, deliver, push and roll —
and watch a house, a road and a playground get built. A friendly foreman in the corner says what to do next with
a speech bubble and a voice (English or Thai, generated offline). No timers, no fail states, no ads, no purchases,
fully offline.

## Quick start
1. Open this folder in Unity Hub (Unity **6000.3.27f2** with Android Build Support).
2. **Little Builders World ▸ Setup Project (generate everything)** (or `Tools/setup.sh`).
3. Open `Assets/_LittleBuildersWorld/Scenes/Boot.unity` → Play. Keyboard: WASD/arrows drive, Space = action, E = enter/exit, H = horn, R = reset.
4. Android: `Tools/build_android.sh` (dev) or `Tools/build_android.sh release` → `Builds/Android/`. Builds are ARMv7 + ARM64 and PowerVR GPUs use OpenGL ES 3 (Redmi A3).

## Tools
| Script | Does |
|---|---|
| `Tools/setup.sh` | Regenerate all art, audio, data, prefabs, scenes; validate |
| `Tools/run_tests.sh [EditMode\|PlayMode\|all]` | Unit + scene tests |
| `Tools/autopilot.sh` | Linux test player + automated full play-through (screenshots/report in `Logs/AutoPilot/`) |
| `Tools/showcase.sh` | Art review: renders vehicles, props and town views to `Logs/Showcase/` and logs scene vertex counts |
| `Tools/build_android.sh [release]` | APK + `Tools/verify_apk.sh` (ABIs, PowerVR filter, orientation) |
| `Tools/voice/generate_voice.sh [key…]` | Regenerate the helper's voice clips offline from `Tools/voice/voice_lines.json` (Kokoro EN, VachanaTTS TH), then run `Tools/setup.sh` |

## Documentation
`Assets/_LittleBuildersWorld/Documentation/`: `IMPLEMENTATION_STATUS.md`, `TECHNICAL_ARCHITECTURE.md`,
`ANDROID_BUILD_GUIDE.md`, `TEST_CHECKLIST.md`, `ART_ASSET_REGISTER.md`, `ART_DIRECTION.md`, plus the source
`GAME_DESIGN_DOCUMENT.md` and `MASTER_DEVELOPMENT_PROMPT.md`.
