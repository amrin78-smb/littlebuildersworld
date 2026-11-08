# Technical Architecture — Little Builders World

Unity **6000.3.27f2**, URP 17.3, Input System 1.20, uGUI 2.0 + TextMeshPro. Android (landscape) first; a Linux
desktop player is built only for automated play-testing.

## Project layout

```
Assets/_LittleBuildersWorld/
  Scripts/                 runtime assembly LittleBuildersWorld.Runtime
    Core/        GameServices, GameStateManager, SceneFlow, GameBootstrap, GameConfig, PerformanceGovernor,
                 Tween, MeshFactory, AutoPilot (test-only, flag-gated)
    Input/       TouchInputController, PointerWatchdog
    Camera/      VehicleCameraController
    Vehicles/    Vehicle, VehicleProfile, VehicleDriveModel, VehicleController, VehicleVisuals, VehicleRecovery,
                 VehicleAudio, VehiclePaint, VehicleInteraction, VehicleActionController, IActionProvider,
                 ExcavatorArm, DumpTruckBed, DebrisPusher, LoadKind
    Activities/  ActivityProgress (pure), ActivityBase, ProjectActivity, HouseConstructionActivity,
                 RoadworksActivity, ExcavationActivity + DigZone, MaterialDeliveryActivity + DeliveryZone,
                 LoadingBay, BulldozerClearingActivity, RoadRollerActivity + RoughPatch, ActivityMarker,
                 ActivityEvents / GuidanceRegistry
    World/       ActivityZone, PushableDebris, RecoveryPoint, SoilPilePool, GuidanceArrow, CelebrationFX,
                 AmbientMotion, TownSession, MenuCameraOrbit
    Progression/ StarLedger (pure), BuilderStarsManager, WorldProgressionManager
    Save/        SaveData, SaveSerializer (pure), SaveSystem
    Audio/       AudioManager, AudioLibrary
    UI/          HUDController, HoldButton, TapButton, HoldToConfirmButton, VirtualJoystick, DriveBubbles,
                 WorldTapCatcher, VehicleSelectorUI, StarCelebrationUI, PauseMenu, SettingsPanel, ParentGate,
                 MainMenuUI, AspectRatioScaler, SafeAreaFitter, Pulse
    Editor/      generator + build tools (assembly LittleBuildersWorld.Editor)
  Tests/EditMode, Tests/PlayMode
  Scenes/ Boot, MainMenu, ConstructionTown      (generated)
  Prefabs/ Vehicles, Environment, Activities, VFX   (generated)
  ScriptableObjects/ Vehicles (VehicleProfile ×4), Audio (AudioLibrary)
  Resources/LBW_GameConfig.asset              (vehicles, unlocks, paints, audio, HUD icons)
  Art/, Audio/                                 (generated, original)
```

**Everything under Art, Audio, Prefabs, ScriptableObjects, Resources and Scenes is generated** by
`Little Builders World ▸ Setup Project` (batch: `Tools/setup.sh`). Change the generator (Scripts/Editor), not the
generated asset. GUIDs are preserved on re-runs.

## Service lifetime

`GameServices` is created by `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, so every scene (including one
opened directly in the Editor) has it. It owns `SaveSystem`, `AudioManager`, `BuilderStarsManager`,
`WorldProgressionManager`, `GameStateManager`, `SceneFlow` and `PerformanceGovernor`. Scene objects reference
each other through serialized fields set by the generator; they reach the services through
`GameServices.Instance` only (no `Find` by name). `Boot.unity` just forwards to the menu.

## Driving

* `VehicleDriveModel` (pure C#, unit-tested): signed speed + yaw rate from `DriveInput`, tuned by `DriveTuning`
  (copied from the vehicle's `VehicleProfile`). Smooth acceleration, braking that turns into gentle reverse,
  automatic braking when nothing is pressed, smoothed steering, turn-in-place for tracked vehicles.
* `VehicleController`: **Rigidbody, pitch/roll frozen, frictionless colliders**. Each FixedUpdate sets the
  velocity along the ground slope and rotates about Y. Chosen over a kinematic controller because collisions with
  the world and other vehicles resolve for free while the model stays deterministic. Frozen pitch/roll means a
  vehicle cannot overturn; `VehicleVisuals` leans the body to the slope instead. Parked vehicles are kinematic so
  they cannot be shoved. When blocked, the model is reconciled to the measured speed (no hidden speed build-up).
* `VehicleRecovery`: stuck detection (pedal held, no motion for 4 s → the reset button pulses), automatic reset
  when below the world or past the edge, manual reset to the nearest `RecoveryPoint`.

## Input

`TouchInputController` merges HUD `HoldButton`s (or the optional `VirtualJoystick`), keyboard (WASD/arrows, Space
action, E enter/exit, H horn, R reset), gamepad, and a scripted override (tests, AutoPilot) into one `DriveInput`.
Steering can only come from the steering controls, so other UI cannot steer. **Easy Drive** (default on): holding
a steering button alone also rolls forward slowly — one finger is enough.
Stuck-input safety: hold controls release on pointer-up, disable, app pause/focus loss, and through
`PointerWatchdog` whenever no finger/mouse/pen is down anywhere.

## Contextual action

One button, one action at a time. `VehicleActionController` asks the driven vehicle's `IVehicleTool` and every
registered world `IActionProvider` (dig piles, loading bays, delivery zones) for an `ActionOffer`, and shows the
highest priority. `Perform()` re-evaluates before executing, so a stale press never does the wrong thing.
Zones are polled with `ActivityZone.Contains` (circle/box on the ground plane) rather than physics triggers:
deterministic, survives teleports and parked vehicles.

## Activities

`ActivityProgress` (pure, unit-tested) implements `Available → Active → StageCompleted → Completed` with clamped
restore and ignore-after-complete. `ActivityBase` persists it after every unit of work through
`WorldProgressionManager` (autosave), pays rewards once per key (`<id>.stage<n>`, `<id>.complete`), raises
celebrations and drives an `ActivityMarker`. `ProjectActivity` opens its child stages one at a time and reveals
pre-authored parts per stage (bouncy pop-in); it pays the stage stars and the project bonus.

| Activity | Vehicle | Mechanism |
|---|---|---|
| Yard dig pit (`yard_dig`, repeatable) | Excavator | `DigZone` offers Scoop; `ExcavatorArm` Dump goes to the nearest truck with room, else the stockpile, else the ground |
| House 0 clear (`house_clear`) | Bulldozer | `DebrisPusher` carries `PushableDebris` in front of the blade (scripted, no rigidbody); debris in the skip zone is tidied; debris leaving the area returns home |
| House 1 dig (`house_dig`) | Excavator | as above; reveals the foundation slab |
| House 2–5 (`house_walls/roof/doors/garden`) | Dump truck | `LoadingBay` (Load) at the brick depot or yard hopper; `DeliveryZone` (Unload) tips the bed |
| Roadworks 0 clear / 1 fill / 2 roll | Bulldozer / Dump truck / Road roller | as above; `RoadRollerActivity` smooths `RoughPatch`es the roller drives over |

Any load is accepted anywhere (no wrong answers). Unloading outside a zone leaves a small pooled heap
(`SoilPilePool`, capped at 8).

## Camera

`VehicleCameraController` (custom; Cinemachine not installed and not needed): damped follow behind/above, yaw
follows the vehicle (slowly when reversing; frozen during machine animations), look-ahead, sphere-cast pull-in
against the `Building` layer, 3/4 showcase drift when resting, and an overview mode for choosing vehicles. All
transitions are damped (no snaps).

## Save

`SaveData` (versioned JSON, `version = 1`) → `Application.persistentDataPath/littlebuilders_save.json`. Writes go
to `.tmp` then replace the file, keeping the previous file as `.bak`; a corrupt main file falls back to the backup,
then to defaults. Saves are debounced and forced on pause, focus loss and quit. Migration hook:
`SaveSerializer.Migrate`. Only progress and settings are stored — no personal data.

## Rendering / performance

URP Mobile asset for Android: one directional light, hard shadows (1 cascade, 40 m, 1024 map), no HDR, no
post-processing, 2× MSAA. Static props use static batching; shared palette materials (SRP batcher). Ambient
motion runs in one `AmbientMotion` update. `PerformanceGovernor` (device only) lowers render scale to 0.8, then
turns off shadows, if the average frame rate stays below 40 FPS. Target 60 FPS requested; budget phones are
expected to run 30–45 (see ANDROID_BUILD_GUIDE.md).

## Layers

`8 Vehicle`, `11 Building` (camera collision). Ground and everything else are `Default`.
