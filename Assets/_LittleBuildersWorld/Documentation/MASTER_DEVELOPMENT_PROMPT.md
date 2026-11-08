# Claude Code Master Development Prompt

## Little Builders World -- Unity 6 Android

You are the lead Unity 6 game engineer, technical designer, gameplay
programmer, tools engineer, and mobile optimisation specialist for
**Little Builders World**.

Your task is to build a complete, playable, polished first-release
version of Little Builders World in the Unity project. The accompanying
Game Design Document (GDD) is the product authority. Read it fully
before changing the project. Where this prompt and the GDD conflict,
follow the GDD unless the user explicitly approves a change.

Do not merely produce a plan, code snippets, or mockups. Inspect the
existing project, implement the game in the project, create or configure
scenes and assets as appropriate, run available validation/build checks,
and leave the project in a coherent, runnable state.

------------------------------------------------------------------------

# 1. Product Definition

**Title:** Little Builders World\
**Engine:** Unity 6 LTS\
**Platform:** Android\
**Orientation:** Landscape\
**Camera:** Third-person vehicle-follow camera\
**World:** Connected open-world construction town\
**Audience:** Preschool children, approximately ages 2--6\
**Core experience:** Explore, drive construction vehicles, perform
simple construction activities, and watch a friendly town grow.

The game must be: - Cheerful, colourful, stylised, and toy-like. - Easy
to control using large touch controls. - Free of fail states,
countdowns, lives, and punitive feedback. - Playable offline without an
account. - Designed around short activities and unrestricted free
driving. - Stable and performant on mid-range Android devices.

The GDD defines the full vision. For the first release, prioritise the
Must-have scope and then Should-have scope. Do not attempt all future
districts and vehicles before the core loop is reliable.

------------------------------------------------------------------------

# 2. Mandatory Working Method

## 2.1 Inspect before editing

1.  Inspect the repository and Unity project structure.
2.  Identify the exact Unity version, render pipeline, installed
    packages, existing scenes, scripts, assets, input configuration, and
    build settings.
3.  Check whether the project already contains useful assets or systems.
    Reuse suitable work rather than overwriting it.
4.  Report any missing dependencies or project blockers.
5.  Do not delete or replace existing user work without a clear reason.

## 2.2 Work in vertical slices

Build and validate the game in playable increments: 1. Project
foundation and greybox. 2. Driving and camera. 3. Vehicle entry/exit and
roster. 4. Construction interactions. 5. Open world and first project.
6. UI, save, audio, polish, and Android build.

At the end of each slice: - Compile and fix errors. - Test the relevant
behaviour in Play Mode where possible. - Update a concise implementation
status document. - List what is complete, incomplete, and blocked. - Do
not claim a feature works unless it has been tested or clearly label it
unverified.

Do not stop after writing a technical plan. Implement the next useful
slice unless a genuine blocker requires user input.

## 2.3 Keep changes controlled

-   Make small, coherent changes.
-   Avoid unnecessary package dependencies.
-   Avoid monolithic scripts.
-   Do not introduce cloud services, accounts, analytics, ads, or in-app
    purchases.
-   Do not use external APIs at runtime.
-   Do not add network dependencies for core gameplay.
-   Do not use placeholder UI that is too small or difficult for
    toddlers to tap.
-   Do not leave compile errors or broken references.

## 2.4 Asset policy

Use existing project assets if they are appropriate. If no suitable art
assets exist: - Create a coherent stylised 3D environment using Unity
primitives, ProBuilder if already installed, or generated
meshes/materials. - Create simple vehicle models from clean modular
meshes if final models are unavailable. - Use temporary
primitive/greybox assets only during prototyping. - Clearly identify
temporary assets. - Keep pivots, scales, colliders, materials, and
naming consistent. - Do not rely on internet downloads or assets with
unclear licensing. - Do not claim a primitive placeholder is a finished
custom model. - Keep vehicle silhouettes distinct and readable.

If the user later supplies models, integrate them without rewriting the
gameplay systems.

------------------------------------------------------------------------

# 3. Technical Foundation

## 3.1 Unity configuration

Use the current project Unity 6 LTS version. If creating a new project,
use URP.

Configure: - Android build target. - Landscape orientation. - Input
System package. - URP quality settings suitable for mobile. -
TextMeshPro for text. - Sensible Android minimum API level based on the
installed Unity version and target devices. - IL2CPP and ARM64 for
release builds, subject to project compatibility. - Development build
configuration for testing. - Application identifier using a neutral
placeholder such as `com.studio.littlebuildersworld` until the owner
supplies a final publisher ID.

Do not silently upgrade packages or Unity versions. Explain
compatibility risks before major upgrades.

## 3.2 Suggested folder structure

Adapt to the existing project where appropriate:

``` text
Assets/
  _LittleBuildersWorld/
    Art/
      Materials/
      Models/
      Textures/
      Environment/
      Vehicles/
      UI/
    Audio/
      Music/
      SFX/
      Voice/
    Prefabs/
      Environment/
      Vehicles/
      Activities/
      UI/
      VFX/
    Scenes/
      Boot.unity
      MainMenu.unity
      ConstructionTown.unity
    Scripts/
      Core/
      Input/
      Camera/
      Vehicles/
      Activities/
      World/
      Progression/
      Save/
      Audio/
      UI/
    ScriptableObjects/
      Vehicles/
      Activities/
      Progression/
    Settings/
    Tests/
    Documentation/
```

Use namespaces such as `LittleBuildersWorld.Core`,
`LittleBuildersWorld.Vehicles`, and `LittleBuildersWorld.Activities`.

## 3.3 Architecture

Create focused, testable systems. Avoid a single global manager
containing all logic.

Implement suitable equivalents of: - `GameBootstrap` -
`GameStateManager` - `SaveSystem` - `WorldProgressionManager` -
`VehicleController` - `VehicleProfile` - `VehicleInteraction` -
`VehicleCameraController` - `VehicleActionController` - `ActivityBase` -
`ActivityTrigger` - `ExcavationActivity` - `MaterialDeliveryActivity` -
`BulldozerClearingActivity` - `HouseConstructionActivity` -
`HUDController` - `TouchInputController` - `AudioManager` -
`BuilderStarsManager`

Use ScriptableObjects for reusable vehicle/activity configuration where
this improves maintainability. Use C# events or interfaces for decoupled
communication. Avoid excessive abstraction for its own sake.

All scripts must: - Have clear responsibilities. - Use serialized
references instead of fragile scene-name lookups where possible. -
Validate required references and provide actionable error messages. -
Avoid avoidable per-frame allocations. - Avoid hidden dependencies on
object execution order. - Handle scene reload and app pause/resume
safely.

------------------------------------------------------------------------

# 4. Input and Touch UX

## 4.1 Landscape HUD

Create a touch-first HUD with: - Large steering control on the left. -
Accelerator on the right. - Brake/reverse close to accelerator. -
Contextual action button. - Exit vehicle button. - Optional horn button.

Use Unity's Input System. If the Input System package is unavailable or
incompatible, explain the issue and use a consistent supported input
implementation rather than mixing systems.

## 4.2 Touch requirements

-   Controls must be large, clearly separated, and comfortable on phone
    screens.
-   Respect safe areas and notches.
-   Provide visual pressed states.
-   Avoid tiny text.
-   Do not require simultaneous precision gestures.
-   Make controls work with both touch and keyboard/gamepad in the
    Editor for testing.
-   Prevent UI touches from also steering the vehicle.
-   Support pointer cancellation and finger release correctly.
-   Do not let a stuck touch leave the vehicle accelerating
    indefinitely.

## 4.3 Steering implementation

Evaluate two approaches: - Virtual joystick. - Large left/right
directional buttons.

Choose the more toddler-friendly and robust option. A virtual joystick
may be used if its dead zone and response are forgiving. If directional
buttons are used, make them large and provide smooth steering while
held.

Expose sensitivity and response tuning in configuration.

------------------------------------------------------------------------

# 5. Vehicle System

## 5.1 Initial vehicles

Implement three functional vehicles: 1. Excavator. 2. Dump truck. 3.
Bulldozer.

A road roller is a secondary priority after these three are stable.

## 5.2 Vehicle controller

Use arcade handling. Choose a robust Rigidbody-based or kinematic
solution after inspecting the project. Document the choice.

Required behaviour: - Smooth acceleration. - Smooth braking. - Gentle
reverse. - Tunable top speed. - Tunable steering response. -
Vehicle-specific turning and acceleration. - Stable handling on gentle
slopes. - Low centre of mass. - Gentle collisions. - No damage or
breakdown. - No fuel. - No complex wheel simulation unless it clearly
improves the result. - A recovery/reset function if the vehicle gets
stuck or overturns.

Do not let the player drive at dangerous or disorienting speeds. The
game is a relaxed toy-like construction playground.

## 5.3 Vehicle configuration

Create a `VehicleProfile` ScriptableObject or equivalent data structure
with: - Display name. - Vehicle type. - Acceleration. - Maximum forward
speed. - Reverse speed. - Braking. - Steering response. - Turning
sensitivity. - Mass/handling parameters if Rigidbody-based. - Contextual
action labels/icons. - Unlock requirement. - Optional audio references.

Avoid hardcoding vehicle-specific values across multiple scripts.

## 5.4 Enter and exit

-   Player approaches or taps an accessible vehicle.
-   Show a large Drive interaction.
-   On activation, smoothly move/enable the camera and HUD.
-   Hide or disable incompatible on-foot controls.
-   On exit, stop the vehicle safely and restore world state.
-   Prevent entering a vehicle already in use.
-   Provide a safe spawn/exit position.
-   Allow switching vehicles at the construction yard.

For the first milestone, a full walking character is not mandatory. A
simple interaction point and smooth camera transition are acceptable,
provided the result feels intentional.

## 5.5 Vehicle presentation

Each vehicle should have: - Distinct shape and colour. - Visible
wheels/tracks. - Animated wheels/tracks while moving. - Animated
machinery parts where relevant. - Idle and movement audio hooks. - A
friendly, toy-like visual style.

------------------------------------------------------------------------

# 6. Third-Person Camera

Implement a dedicated `VehicleCameraController`.

Requirements: - Follow the active vehicle from behind and above. -
Smooth position and rotation damping. - Look ahead slightly in the
travel direction. - Maintain a useful view of the vehicle and nearby
ground. - Avoid camera clipping through world geometry. - Prevent sudden
snaps when entering/exiting. - Handle reverse and sharp turns without
disorienting spins. - Settle into a stable angle when stationary. -
Allow tuning of follow distance, height, damping, look-ahead, and
collision radius. - Keep the camera behaviour consistent across vehicle
types.

Use Cinemachine only if already installed or if its addition is
justified and compatible. Otherwise implement a lightweight custom
camera. Do not add Cinemachine solely by habit.

Manual camera orbit is not required for the first release.

------------------------------------------------------------------------

# 7. Open World Construction Town

Create one connected, compact, navigable town. Do not create separate
levels for each initial activity.

## 7.1 Initial layout

Build these connected areas: - Construction Yard: central hub and
vehicle parking. - Town Road: loop road with gentle turns and
landmarks. - House Plot: first major project. - Roadworks Area: simple
road repair activity. - A visible but initially inactive expansion area
for future content.

## 7.2 Navigation

-   Use roads, signs, coloured flags, cones, fences, and landmarks.
-   Keep routes short and readable.
-   Avoid narrow passages that trap vehicles.
-   Add gentle invisible boundaries where needed.
-   Do not create a maze.
-   Use soft visual guidance to activity zones.
-   Let players ignore missions and drive freely.

## 7.3 Environment art

Use a consistent stylised toy-like art direction: - Rounded shapes. -
Bright but balanced palette. - Soft lighting. - Simple readable
materials. - Friendly trees, buildings, fences, cones, rocks, road
signs, and props. - Controlled visual clutter. - Subtle ambient
animation.

Create a simple art direction document with a palette and scale
conventions. Keep the town visually coherent even if some models are
procedural or primitive-based.

------------------------------------------------------------------------

# 8. Construction Activities

All activities must be forgiving, deterministic, and based on clear
visual cause and effect. Prefer triggers, scripted animations, and state
transitions over complex physics.

## 8.1 Excavator -- Dig and Scoop

Implement a repeatable activity: 1. Player drives into a dig zone. 2.
Contextual Dig/Scoop button appears. 3. Tap triggers a bucket/boom
animation. 4. Soil load visual appears in bucket or a simple load state
is set. 5. Player drives to a dump truck or stockpile. 6. Unload action
transfers the soil visually. 7. Target progress increases. 8. Completion
triggers celebration and reward.

Requirements: - Do not require precise bucket positioning. - Use
generous interaction zones. - Provide a clear target indicator. - Keep
the animation readable and satisfying. - Prevent duplicate load exploits
from breaking state. - Support cancel/leave and resume.

## 8.2 Dump Truck -- Load and Deliver

-   Truck can receive a load from the excavator or a loading trigger.
-   Load state is visible in the truck bed.
-   At the destination, show Unload.
-   Bed tipping animation plays.
-   Material appears at the destination.
-   Activity progress updates.
-   The player can leave and return without losing saved project
    progress.

## 8.3 Bulldozer -- Push and Clear

-   Detect loose soil/props within a forgiving front interaction area.
-   Move objects toward a target/clearing area using stable scripted
    movement or constrained physics.
-   Avoid launching or rolling objects.
-   Show clear progress.
-   Restore objects to a safe state if they become stuck or leave the
    activity bounds.

## 8.4 House Construction

Implement a staged project with visible changes: 1. Plot clearing. 2.
Foundation placement. 3. Wall sections. 4. Roof. 5. Door and windows. 6.
Final decorative elements.

Use large snap zones and pre-authored component positions. Do not
require freehand precision. Each completed stage should visibly update
the house.

## 8.5 Roadworks

At minimum, provide a simple roadworks loop: - Clear a section. -
Prepare the surface. - Smooth or finish it. If the road roller is not
yet ready, use an accessible simplified finishing interaction. Do not
block the release on a complex roller system.

## 8.6 Activity state

Create a clear activity lifecycle, for example:
`Available -> Active -> StageCompleted -> Completed`

Allow the player to abandon an activity and resume later. Prevent
duplicate rewards for the same stage unless the design explicitly
permits a repeatable reward.

------------------------------------------------------------------------

# 9. Progression and Save

## 9.1 Builder Stars

-   Award stars for meaningful activity milestones.
-   Display a cheerful collection animation.
-   Use stars to unlock optional content.
-   Do not use randomised paid rewards or pressure mechanics.

## 9.2 Unlocks

For the first release, unlock a small number of cosmetic items or one
additional vehicle/yard decoration. Ensure core vehicles and essential
gameplay are accessible without excessive grinding.

## 9.3 Local save

Implement versioned local save data for: - Completed activity stages. -
Completed projects. - Builder Star total. - Unlocked
vehicles/cosmetics. - Selected vehicle. - Audio settings. - Control
preferences.

Requirements: - Autosave at safe points. - Handle app pause and
resume. - Handle missing/corrupt save files with safe defaults. -
Include save schema version and migration path. - Do not store personal
information. - Provide an adult-confirmed reset option.

------------------------------------------------------------------------

# 10. UI, Audio, and Feedback

## 10.1 Main menu

Create a polished, simple main menu: - Play. - Settings. - Optional
adult-only reset progress. Use large buttons and a friendly
construction-themed background.

## 10.2 In-game HUD

-   Minimal.
-   Large icons.
-   Clear touch feedback.
-   Contextual action button.
-   Exit button.
-   No text-heavy panels.
-   Respect safe areas and aspect ratios.

## 10.3 Feedback

Use: - Short positive voice prompts where assets exist. - Friendly sound
effects. - Small particle effects. - Object animations. - Progress
indicators. - Character reactions if available.

Examples: "Great digging!", "Let's deliver the soil!", "You built it!",
"Look at the new road!"

Do not synthesize or add voice assets with unclear licensing. If voice
is not available, use icons and visual demonstrations.

## 10.4 Audio

Separate music, SFX, and voice volume. Avoid loud alarms, harsh sounds,
and sudden peaks. The game must remain playable with audio muted.

------------------------------------------------------------------------

# 11. Mobile Performance and Build

Target mid-range Android hardware.

Requirements: - URP mobile renderer configuration. - Prefer baked
lighting. - Limit real-time shadows and shadow distance. - Use simple
colliders. - Use object pooling for repeated effects if needed. - Avoid
unnecessary Update loops and garbage allocations. - Use LODs where
appropriate. - Compress textures appropriately. - Avoid excessive
transparent particles. - Profile on an actual Android device early. -
Aim for stable 60 FPS where feasible, with a stable 30 FPS fallback.

Build: - Android APK for development testing. - ARM64 release
configuration when ready. - Landscape orientation. - Correct application
identifier placeholder. - No external network permission unless required
by a documented Unity subsystem; explain any permission. - No ads,
analytics, chat, location, or account SDKs.

Do not claim Android performance is verified until tested on physical
hardware.

------------------------------------------------------------------------

# 12. Testing Requirements

Create tests where practical and a manual test checklist.

Test: 1. Main menu to world transition. 2. Enter/exit each vehicle. 3.
Forward, reverse, steering, braking. 4. Touch release always stops
input. 5. Camera follows and avoids clipping. 6. Vehicle recovery. 7.
Excavator scoop/load/unload. 8. Dump truck loading and delivery. 9.
Bulldozer clearing. 10. House project stages. 11. Abandon and resume
activities. 12. Save/load after app restart. 13. App pause/resume. 14.
Different landscape aspect ratios and safe areas. 15. No missing
references or console errors. 16. Android build installation and launch.
17. Performance in the busiest area.

Use EditMode/PlayMode tests for deterministic logic where practical.
Manual testing is still required for camera feel, touch UX, art
presentation, and actual device performance.

------------------------------------------------------------------------

# 13. Definition of Done

A feature is done only when: - It is implemented in the Unity project. -
It compiles without errors. - Its required references are assigned. -
Its primary behaviour has been tested. - It works with the intended
input method. - It handles leaving/re-entering or pause/resume where
relevant. - It does not break existing features. - Its status is
recorded accurately.

The first release is complete when: - A player can launch the game. -
Enter the construction town. - Enter, drive, and exit the excavator,
dump truck, and bulldozer. - Explore a connected open world. - Complete
dig/scoop, delivery, bulldozer clearing, and a staged house project. -
Earn Builder Stars. - Save and reload progression. - Play entirely
offline. - Use large, toddler-friendly touch controls. - Run reliably on
a tested Android device.

------------------------------------------------------------------------

# 14. Required Documentation

Maintain these files under `Assets/_LittleBuildersWorld/Documentation/`
or the repository's existing documentation folder: -
`IMPLEMENTATION_STATUS.md` -- current progress and blockers. -
`TECHNICAL_ARCHITECTURE.md` -- systems and dependencies. -
`ANDROID_BUILD_GUIDE.md` -- how to build and install. -
`TEST_CHECKLIST.md` -- manual and automated tests. -
`ART_ASSET_REGISTER.md` -- asset source, status, and whether
placeholder/custom/licensed.

Update them as implementation evolves. Do not create documentation that
claims unimplemented functionality is complete.

------------------------------------------------------------------------

# 15. Execution Instructions

Start now.

**Step 1:** Inspect the project and report a concise baseline: Unity
version, render pipeline, packages, current scenes, current assets,
build target, and any blockers.

**Step 2:** Read and map the GDD into an implementation checklist.
Identify any conflicts or missing decisions. Use sensible defaults for
non-critical details; ask the user only when a decision materially
affects the product or risks destructive changes.

**Step 3:** Implement the project foundation and a playable greybox
construction yard.

**Step 4:** Implement the first driving vertical slice with one vehicle
and the third-person camera. Test it before adding more vehicles.

**Step 5:** Continue through the milestones in the GDD. Prioritise a
complete, reliable core loop over breadth.

At every stage, be explicit about: - Files created or modified. -
Features implemented. - Tests performed and their results. - Known
limitations. - The next concrete implementation step.

Do not stop at a proposed architecture. Build the game. Do not fabricate
test results, Android builds, imported assets, or successful runtime
behaviour.
