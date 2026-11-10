# Little Builders World — Claude Code Project Rules

- Product authority: `Assets/_LittleBuildersWorld/Documentation/GAME_DESIGN_DOCUMENT.md`; engineering brief: `MASTER_DEVELOPMENT_PROMPT.md`. Read both before feature work.
- Unity **6000.3.27f2**, URP 17.3, Input System, uGUI + TextMeshPro. Android landscape, touch-first, offline. Project at repo root.
- Runtime code: `Assets/_LittleBuildersWorld/Scripts` (asmdef `LittleBuildersWorld.Runtime`); generator/build tools: `Scripts/Editor`.
- **Art, audio, prefabs, ScriptableObjects, Resources and scenes are generated** by `Tools/setup.sh` (menu: Little Builders World ▸ Setup Project). Change the generator, never hand-edit generated assets.
- Budget-phone rules (Redmi A3, 32-bit, PowerVR): every Android build must be IL2CPP **ARMv7 + ARM64** and keep the Vulkan deny filter `.*PowerVR.*` (`ProjectConfigurator`). Verify each APK with `Tools/verify_apk.sh`. On-device test must include close-fully-and-relaunch.
- Child-facing UI: no reading, no timers, no fail states, big targets (≥150 px at 1920×1080), one contextual action at a time. Grown-up screens behind a held gear; reset behind the parent gate.
- No ads, analytics, accounts, network, IAP, chat, or personal data. Packages are deliberately minimal — Unity re-adds analytics/purchasing to a fresh project; keep them out of `Packages/manifest.json`.
- Commands: `Tools/setup.sh`, `Tools/run_tests.sh [EditMode|PlayMode|all]`, `Tools/autopilot.sh`, `Tools/showcase.sh` (art review renders + vertex counts), `Tools/blender/build_models.sh [DumpTruck] [--preview]` (smooth Blender vehicle models → mesh pack + baked texture; run before setup), `Tools/build_android.sh [release]`. Only one Unity process may open the project at a time (the Linux player run is fine in parallel).
- Never claim a feature/build/test works unless verified; keep `Documentation/IMPLEMENTATION_STATUS.md` honest and current.
- Use only original or clearly licensed assets; record them in `ART_ASSET_REGISTER.md`.
- Helper words (speech bubble + voice) have one source: `Tools/voice/voice_lines.json` (English + Thai). After editing it run `Tools/voice/generate_voice.sh` (offline TTS: Kokoro EN, VachanaTTS TH; no recordings) and `Tools/setup.sh`. Every activity's `m_HintKey` and every `<activityId>_done` line must exist there (the validator checks).
