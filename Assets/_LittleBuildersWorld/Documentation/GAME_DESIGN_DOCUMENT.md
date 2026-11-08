# Little Builders World

## Game Design Document (GDD)

**Version:** 1.0\
**Target engine:** Unity 6 LTS\
**Platform:** Android (touch-first)\
**Camera:** Third-person, vehicle-follow\
**World structure:** Seamless open-world construction town\
**Target audience:** Preschool children, approximately ages 2--6\
**Design pillars:** Drive, discover, build, celebrate

------------------------------------------------------------------------

# 1. High Concept

**Little Builders World** is a cheerful, low-pressure 3D construction
adventure in which a young player explores a friendly town, drives
colourful construction vehicles, operates simple machinery, and helps
build and improve the world.

The player is the owner-operator of a small construction crew. The town
begins with a compact construction yard and several unfinished
locations. By exploring and interacting with the world, the player
discovers projects such as clearing a building plot, moving soil,
repairing a road, placing building components, and completing a
playground.

The game is designed to be immediately understandable to a preschool
child. Controls are large and forgiving, tasks are short and tactile,
instructions are supported by animation and voice, and there are no fail
states, countdowns, lives, or punitive systems.

## 1.1 One-sentence pitch

A toddler-friendly 3D open-world construction playground where children
drive big machines, complete satisfying building activities, and watch
their own little town grow.

## 1.2 Product goals

1.  Deliver a polished, stable Android game with a satisfying driving
    feel.
2.  Make construction vehicles the main attraction.
3.  Let children freely explore without being forced into missions.
4.  Provide simple, physical-feeling construction interactions.
5.  Create a modular foundation for adding vehicles, projects,
    districts, and activities.
6.  Keep the experience safe, offline-first, and free from manipulative
    monetisation.

## 1.3 Non-goals for the initial release

-   Realistic construction simulation or complex vehicle physics.
-   Multiplayer, chat, user accounts, cloud saves, or online
    leaderboards.
-   Real-money purchases or advertising.
-   Large procedurally generated worlds.
-   Complex building placement or unrestricted creative sandbox tools.
-   Accurate real-world construction procedures.

------------------------------------------------------------------------

# 2. Design Pillars

## 2.1 Drive

Vehicles must feel fun and responsive at low speeds. Steering is
forgiving, acceleration is smooth, collisions are gentle, and the camera
keeps the vehicle visible.

## 2.2 Discover

The player can roam the connected town, find activity areas, drive along
roads, and approach projects in any order. There should be visual
landmarks and clear paths.

## 2.3 Build

Every construction activity should have a clear physical result: soil
moves, a load appears in a truck, a road becomes smooth, a component
snaps into place, or a building visibly progresses.

## 2.4 Celebrate

Use warm animations, cheerful sound, character reactions, and simple
visual rewards. Never shame the player or imply failure.

## 2.5 Preschool-first usability

A child should be able to understand the primary interaction by looking
at the screen and seeing an animated demonstration. Avoid text-heavy
instructions and small controls.

------------------------------------------------------------------------

# 3. Audience and Accessibility

## 3.1 Target audience

-   Primary: ages 2--6, with adult assistance as needed.
-   Secondary: parents and older siblings who may enjoy relaxed vehicle
    play.

The age range is a design target, not a developmental guarantee. Provide
settings and control options that accommodate different motor skills.

## 3.2 Usability requirements

-   Large touch targets, preferably at least comfortably thumb-sized on
    common phones.
-   High contrast between controls and the background.
-   No required reading.
-   Icons supported by animation and optional spoken prompts.
-   No rapid multi-touch sequences.
-   No precision timing.
-   No punishment for driving away or abandoning an activity.
-   Always provide a visible, simple way to leave an activity and return
    to free driving.
-   Pause and settings controls must be accessible to an adult.

## 3.3 Session design

Activities should be completable in roughly 30 seconds to 3 minutes, but
the player may continue exploring indefinitely. The game must be safe to
leave at any point.

------------------------------------------------------------------------

# 4. Core Game Structure

## 4.1 Core loop

1.  Explore the town.
2.  Notice a friendly activity marker or discover an interactive
    construction area.
3.  Drive or walk/transition into the activity vehicle.
4.  Perform one or more simple construction actions.
5.  See the environment visibly improve.
6.  Receive a celebratory animation and optional star reward.
7.  Continue exploring.

## 4.2 Free-play-first philosophy

The open world is the primary mode. Missions are invitations, not
obligations. The player can: - Drive any unlocked vehicle in accessible
areas. - Explore roads and open spaces. - Repeat completed activities. -
Ignore activity markers. - Return to the construction yard at any time.

## 4.3 Progression

Progression unlocks content but never blocks basic play. - Earn
**Builder Stars** by participating in activities. - Stars unlock
additional vehicles, cosmetic options, and optional project areas. - No
daily streaks, timers, energy systems, loot boxes, or limited-time
rewards. - Replaying an activity remains rewarding, but avoid farming
loops that pressure repeated play.

## 4.4 Save system

-   Local device save only for initial release.
-   Save unlocked vehicles, completed projects, selected vehicle, and
    settings.
-   Autosave at safe checkpoints and when returning to the yard.
-   Gracefully handle app suspension and unexpected termination.
-   No personal information required.

------------------------------------------------------------------------

# 5. World and Environment

## 5.1 World layout

Build one compact, connected town rather than separate level scenes. Use
a hub-and-spoke layout with the Construction Yard near the centre.

Initial world zones: 1. **Construction Yard** -- home base, vehicle
parking, vehicle selection, tutorial space. 2. **Town Road** -- a simple
road loop with intersections, gentle slopes, and landmarks. 3. **House
Plot** -- dig, move soil, deliver materials, and help assemble a small
house. 4. **Roadworks Area** -- clear debris, level ground, place road
sections, and roll the surface. 5. **Playground Site** -- move materials
and assemble a slide, swings, and a small play structure. 6. **Bridge
Worksite** -- a visually interesting later area; can be blocked by a
friendly progress gate in the first milestone.

Initial release should fully implement the Construction Yard, Town Road,
House Plot, and a small Roadworks Area. Playground and Bridge areas can
be visible but staged for later updates if schedule requires.

## 5.2 World scale

-   Compact, readable, and designed for mobile performance.
-   Driving from the yard to the first project should take approximately
    20--40 seconds.
-   Avoid long empty stretches.
-   Use roads, signs, coloured flags, cones, fences, and landmark props
    to guide players.
-   Keep sightlines open and avoid confusing maze-like layouts.

## 5.3 Visual direction

-   Stylised, toy-like 3D art.
-   Rounded silhouettes, friendly proportions, soft edges, bright but
    controlled colours.
-   Slightly exaggerated wheels, cabins, buckets, blades, and booms for
    readability.
-   Materials should be clean and appealing, not photorealistic.
-   Use a consistent scale across vehicles, buildings, props, and
    characters.
-   Avoid visual clutter around interactive objects.
-   Use subtle ambient animation: rotating beacons, swaying trees,
    moving clouds, birds, and idle vehicle motion.

## 5.4 World interaction

-   Non-interactive props provide atmosphere.
-   Interactive objects have clear visual feedback: outline, glow,
    bounce, icon, or animation.
-   Do not rely on colour alone to communicate an interaction.
-   Vehicles and objects should not become permanently stuck.
-   Use invisible soft boundaries or gentle steering guidance rather
    than harsh collision walls where possible.

------------------------------------------------------------------------

# 6. Camera

## 6.1 Primary camera

Third-person vehicle-follow camera positioned behind and slightly above
the active vehicle.

## 6.2 Camera behaviour

-   Smooth follow with configurable damping.
-   Look slightly ahead in the direction of travel.
-   Keep vehicle and nearby activity visible.
-   Avoid abrupt camera snapping.
-   Prevent camera clipping through buildings and terrain using
    collision handling.
-   Limit extreme pitch and roll.
-   Support gentle camera repositioning after reversing or turning.
-   When the player is stationary, settle into a pleasant showcase
    angle.
-   Do not rotate the camera unexpectedly during a construction
    interaction.

## 6.3 Camera controls

For initial release, use automatic camera control. Optional simple
swipe-to-orbit can be added as an accessibility setting only after core
controls are stable. Do not require camera manipulation to complete
activities.

------------------------------------------------------------------------

# 7. Vehicles

## 7.1 Initial vehicle roster

1.  **Excavator** -- signature vehicle; digging and scooping.
2.  **Dump Truck** -- carries soil and materials.
3.  **Bulldozer** -- pushes loose soil and clears light obstacles.
4.  **Road Roller** -- smooths prepared road surfaces.
5.  **Cement Mixer** -- delivers concrete for later content.
6.  **Mobile Crane** -- lifts oversized components for later content.

For the first playable milestone, implement three fully functional
vehicles: Excavator, Dump Truck, and Bulldozer. Road Roller may be
included if schedule allows.

## 7.2 Vehicle interaction model

-   The player approaches a vehicle and taps a large **Drive** button or
    the vehicle.
-   The camera transitions smoothly to the vehicle-follow position.
-   A simple on-screen control layout appears.
-   A large **Exit** button returns the player to the world/yard state.
-   Vehicle switching is available through a simple vehicle selector in
    the yard, not through a complex radial menu.

## 7.3 Driving controls

Recommended default layout: - Left side: large steering control (virtual
joystick or left/right buttons; choose after testing). - Right side:
large accelerator button. - Brake/reverse button below or beside
accelerator. - Optional horn button. - Contextual action button appears
only when relevant (e.g., Dig, Scoop, Push, Unload). - Exit button
always visible while driving.

Controls must support one-finger operation where practical. Avoid
requiring simultaneous steering and multiple action buttons for
toddlers.

## 7.4 Driving feel

-   Arcade handling, not simulation.
-   Smooth acceleration and deceleration.
-   Moderate top speed.
-   Wide turning radius adjusted per vehicle.
-   Automatic braking at very low speed when input is released.
-   Gentle collision response.
-   No vehicle damage, fuel, maintenance, or breakdown.
-   Reverse is slow and easy to engage.
-   Vehicles should recover from being stuck, e.g., a
    reset/return-to-road function.

## 7.5 Vehicle identity

Each vehicle should have: - Distinct silhouette and colour. - Animated
moving parts. - Idle engine loop and movement audio. - A short
celebratory sound on successful interaction. - Simple, readable
interaction animation. - Optional friendly face-like design details,
without making all vehicles look identical.

------------------------------------------------------------------------

# 8. Construction Activities

## 8.1 General activity design

Each activity should: - Have a clear starting point and end state. - Use
one primary action at a time. - Demonstrate the action visually. -
Provide generous interaction zones. - Accept imperfect input. - Give
immediate feedback. - Allow the player to stop and leave without losing
progress. - Avoid countdowns, failure states, and precision
requirements.

## 8.2 Excavator: Dig and Scoop

**Goal:** Move soil from a marked dig zone into a nearby dump truck or
stockpile.

Interaction sequence: 1. Park near the highlighted soil pile. 2. Tap the
large Dig/Scoop control. 3. Excavator boom and bucket perform a
simplified animated scoop. 4. Soil appears in the bucket or is
represented by a clear load visual. 5. Move to the truck or stockpile.
6. Tap Unload. 7. Soil transfers visibly and the target progress
increases.

Implementation can use scripted animation and state changes rather than
physically simulated loose soil. The result must look tactile and
satisfying.

## 8.3 Dump Truck: Load and Deliver

**Goal:** Carry soil or materials from one location to another. - Load
level is represented visually in the truck bed. - The truck can carry
one or more simplified loads. - At the target zone, a large Unload
button appears. - The bed tips with an animation and the material
appears at the target. - No complicated balancing or cargo physics.

## 8.4 Bulldozer: Clear and Push

**Goal:** Push loose soil or lightweight construction objects into a
marked zone. - Use a forgiving front interaction volume. - Objects slide
or move with simplified physics. - A progress indicator shows how much
of the area is cleared. - Prevent objects from launching, rolling
uncontrollably, or blocking the vehicle.

## 8.5 Road Roller: Smooth the Road

**Goal:** Drive over marked road sections. - Road segments change from
rough to smooth when the roller passes over them. - Use trigger zones
and material/mesh state transitions. - No precision driving required.

## 8.6 House Building

A staged, highly visual construction activity: 1. Clear the plot. 2.
Deliver materials. 3. Place foundation. 4. Snap wall sections into
place. 5. Add roof. 6. Add door, windows, and garden props. 7. Celebrate
the finished house.

Use guided snap points and large components. The child does not need to
build freely or align objects precisely.

## 8.7 Road Repair

1.  Clear cones/debris.
2.  Move soil or aggregate into the work area.
3.  Smooth the surface.
4.  Roll the road.
5.  Add simple road markings.
6.  Reopen the road with a cheerful celebration.

## 8.8 Playground Construction

Later activity: - Deliver parts. - Snap together slide, swings, and
climbing frame. - Add soft ground and decorative elements. -
Children/mascot characters celebrate from a safe distance.

------------------------------------------------------------------------

# 9. Characters and Feedback

## 9.1 Player representation

The player may be represented by a small friendly builder character who
appears in the yard and at activity locations. In the initial milestone,
a full walkable character is optional; vehicle entry can be a simple
interaction transition.

## 9.2 Supporting characters

A small cast of friendly town residents can provide visual feedback: -
Builder foreman mascot. - Friendly site helper. - Town residents who
react to completed projects.

Avoid complex dialogue. Use short voice lines and expressive animation.

## 9.3 Feedback language

Examples: - "Great digging!" - "Let's move the soil!" - "You did it!" -
"Look at the new road!" - "Wonderful building!"

Voice lines should be optional, short, positive, and not repeated
excessively. Include subtitles/icons where practical, but the game must
remain understandable without audio.

------------------------------------------------------------------------

# 10. UI and UX

## 10.1 Main menu

-   Large Play button.
-   Settings.
-   Continue/Reset progression options behind an adult-friendly
    confirmation.
-   No external links or store prompts in the child-facing flow.

## 10.2 In-world HUD

Driving HUD: - Steering. - Accelerator. - Brake/reverse. - Context
action. - Exit vehicle. - Optional horn.

HUD requirements: - Large hit areas. - Clear iconography. - Minimal
text. - Avoid covering the vehicle or target. - Respect screen safe
areas and different aspect ratios. - Support landscape orientation.

## 10.3 Activity prompts

-   Large floating icon above the target.
-   Optional short voice prompt.
-   Animated arrow or dotted path to the target.
-   One active instruction at a time.
-   No long text panels.

## 10.4 Settings

-   Music volume.
-   Sound effects volume.
-   Voice prompt toggle.
-   Control mode (joystick vs buttons, if both are implemented).
-   Camera sensitivity only if manual camera controls exist.
-   Reset progress with adult confirmation.

------------------------------------------------------------------------

# 11. Audio

## 11.1 Audio direction

Warm, playful, gentle and non-intrusive. - Soft upbeat background
music. - Distinct engine sounds per vehicle. - Hydraulic movement
sounds. - Soil, gravel, concrete, and roadwork sounds. - Positive
completion stingers. - UI tap feedback.

## 11.2 Audio rules

-   Avoid loud horns, harsh alarms, or sudden volume spikes.
-   Avoid continuous overlapping voice lines.
-   Provide separate music, effects, and voice volume controls.
-   Ensure the game is fully playable muted.

------------------------------------------------------------------------

# 12. Rewards and Progression

## 12.1 Builder Stars

Award a small number of stars for completing activity stages. Stars are
purely celebratory and unlock content.

## 12.2 Unlocks

Potential unlocks: - New vehicle colours. - Vehicle decals. -
Construction hats/mascot accessories. - New vehicle models. - Decorative
yard props. - Additional project zones.

## 12.3 Reward principles

-   No randomised paid rewards.
-   No loot boxes.
-   No streak pressure.
-   No ads.
-   No pay-to-progress.
-   Never remove earned rewards.

------------------------------------------------------------------------

# 13. Technical Design

## 13.1 Unity project baseline

-   Unity 6 LTS, using the current supported LTS patch at project
    creation.
-   C#.
-   Universal Render Pipeline (URP).
-   Android build target.
-   Landscape orientation.
-   Input System package.
-   TextMeshPro for any text.
-   Use Addressables only if content scale later justifies it; do not
    introduce unnecessary complexity in the first milestone.

## 13.2 Architecture principles

Use modular, data-driven systems with clear separation of concerns.
Avoid a single monolithic GameManager.

Suggested systems: - `GameBootstrap` -- initialises services and loads
the world. - `SaveSystem` -- local persistence and versioned save
data. - `WorldProgressionManager` -- completed projects and unlocks. -
`VehicleController` -- arcade movement and steering. -
`VehicleInteraction` -- entering/exiting and vehicle selection. -
`VehicleCameraController` -- third-person follow camera. -
`VehicleActionController` -- contextual vehicle actions. -
`ConstructionActivity` -- base activity lifecycle. -
`ExcavationActivity`, `DeliveryActivity`, `BulldozerActivity`, etc. -
`ActivityTrigger` -- detects player/vehicle proximity. - `HUDController`
-- touch controls and contextual buttons. - `AudioManager` -- audio
routing and settings. - `BuilderStarsManager` -- reward calculation and
display.

Use interfaces or events where appropriate. Keep vehicle configuration
in ScriptableObjects where useful.

## 13.3 Vehicle implementation

Use a robust arcade vehicle implementation suitable for mobile: - Either
a Rigidbody-based controller with carefully tuned forces and
constraints, or a custom kinematic controller. - Choose one approach and
document why. - Avoid unstable wheel physics if it adds complexity
without improving the feel. - Use simplified wheel visuals and steering
animations if necessary. - Use separate vehicle profiles for
acceleration, max speed, steering, braking, and reverse speed. - Support
slopes and uneven ground without tipping easily. - Keep the centre of
mass low. - Use collision layers to prevent unwanted interactions.

## 13.4 Activity implementation

Prefer deterministic, scripted interactions and trigger-driven progress
to complex physics simulations. Activities must be reliable and easy to
test.

## 13.5 Performance targets

Target smooth performance on mid-range Android devices: - Aim for 60 FPS
where feasible; provide stable 30 FPS fallback. - Keep draw calls,
material count, transparent effects, and real-time shadows controlled. -
Use baked lighting where practical. - Use LODs for larger assets if
needed. - Use simple colliders. - Pool frequently spawned effects and
props. - Avoid expensive per-frame allocations. - Profile on an actual
Android device early, not only in the Editor.

## 13.6 Memory and battery

-   Keep textures and meshes appropriately sized.
-   Avoid unnecessary background simulation.
-   Pause audio and reduce activity when app is suspended.
-   Do not keep expensive effects running off-screen.

## 13.7 Save data

Versioned JSON or another robust local format is acceptable. Include
migration handling for future save versions. Save only gameplay settings
and progression, not personal data.

------------------------------------------------------------------------

# 14. Safety and Child-Friendly Product Requirements

-   Offline-first; no account required.
-   No chat, user-generated content, social sharing, or location access.
-   No advertising SDK.
-   No external purchase flow in the child-facing experience.
-   No analytics that collect personal or identifying information.
-   No manipulative notifications or engagement loops.
-   Include an adult-accessible settings/reset area.
-   Provide a clear pause/exit path.
-   Avoid realistic depictions of dangerous construction practices; use
    toy-like, safe fantasy construction scenes.
-   Keep all construction interactions inside controlled work areas and
    away from moving traffic.
-   If future monetisation is considered, require a separate parent gate
    and privacy/legal review before implementation.

------------------------------------------------------------------------

# 15. Initial Release Scope

## Must-have

-   Main menu.
-   One connected construction town.
-   Construction yard.
-   Third-person vehicle-follow camera.
-   Three playable vehicles: excavator, dump truck, bulldozer.
-   Arcade driving and enter/exit.
-   Three construction activities: dig/scoop, load/deliver, push/clear.
-   One simple house-building project.
-   Builder Stars and basic unlocks.
-   Local save.
-   Touch HUD.
-   Android build and device testing.
-   Settings for audio.

## Should-have

-   Road roller and road smoothing activity.
-   Friendly builder mascot.
-   Voice prompts.
-   Vehicle colour customisation.
-   More environmental animation.

## Later

-   Playground, bridge, beach, mountain, and airport zones.
-   Cement mixer and mobile crane.
-   More house styles and town upgrades.
-   Optional manual camera orbit.
-   Additional languages and voice packs.

------------------------------------------------------------------------

# 16. Development Milestones

## Milestone 0 -- Project Foundation

-   Create Unity 6 URP project.
-   Configure Android and landscape.
-   Set up folder structure, input, quality profiles, and version
    control.
-   Establish art scale and camera prototype.
-   Produce a playable greybox yard.

**Exit criteria:** Project builds and launches on Android; camera and
basic scene are stable.

## Milestone 1 -- Driving Prototype

-   Implement one placeholder vehicle.
-   Add touch steering, acceleration, brake/reverse.
-   Add camera follow and collision avoidance.
-   Add enter/exit interaction.
-   Tune handling on a real Android device.

**Exit criteria:** A child can drive around the yard without getting
stuck or losing camera view.

## Milestone 2 -- Vehicle Roster

-   Add excavator, dump truck, and bulldozer.
-   Configure vehicle profiles.
-   Add visual moving parts and simple audio.
-   Add vehicle selection.

**Exit criteria:** All three vehicles can be entered, driven, exited,
and reset reliably.

## Milestone 3 -- Construction Interactions

-   Implement excavation/scoop.
-   Implement truck loading/unloading.
-   Implement bulldozer push/clear.
-   Add clear visual feedback and progress.

**Exit criteria:** Each interaction is repeatable, deterministic, and
works with touch controls.

## Milestone 4 -- Open World and First Project

-   Build the connected yard, road, house plot, and roadworks area.
-   Add navigation landmarks and activity triggers.
-   Implement house construction stages.
-   Add Builder Stars and project completion.

**Exit criteria:** Player can freely drive between areas and complete
the first project without a tutorial wall.

## Milestone 5 -- Polish and Android Release Candidate

-   Final art pass.
-   Audio, UI, accessibility and safe-area checks.
-   Save/load and app-resume testing.
-   Performance profiling.
-   Device compatibility testing.
-   Bug fixing and release build.

**Exit criteria:** Stable Android build with no blocker bugs and a
complete first-session experience.

------------------------------------------------------------------------

# 17. Acceptance Criteria

The first release is not complete until all of the following are true:

1.  The app launches reliably on supported Android devices.
2.  Landscape layout and safe areas work on common screen sizes.
3.  The player can enter, drive, and exit all initial vehicles.
4.  The camera follows smoothly and does not routinely clip through the
    environment.
5.  Steering and acceleration are responsive and forgiving.
6.  The vehicle can recover from common stuck states.
7.  Excavator digging produces a visible, understandable result.
8.  Dump truck loading and unloading works repeatedly.
9.  Bulldozer clearing works without unstable object physics.
10. House construction progresses through clear visual stages.
11. Progress and settings persist after app restart.
12. Activities can be abandoned and resumed without corrupting state.
13. There are no required timers, fail states, or punitive mechanics.
14. No network connection is required for core gameplay.
15. Performance is acceptable on a representative mid-range Android
    device.
16. UI buttons are large enough for preschool use and remain visible in
    landscape.

------------------------------------------------------------------------

# 18. Risks and Mitigations

  -----------------------------------------------------------------------
  Risk                                Mitigation
  ----------------------------------- -----------------------------------
  Vehicle physics feels difficult     Use arcade movement, low speed,
                                      wide steering tolerance, and
                                      real-device tuning

  Camera becomes disorienting         Automatic follow camera, damping,
                                      limited rotation, collision
                                      handling

  Scope becomes too large             Ship one compact town and three
                                      vehicles first

  Construction interactions become    Use scripted animations, triggers,
  technically expensive               and state changes rather than full
                                      simulation

  Toddler cannot understand controls  One primary action at a time, large
                                      controls, animated demonstrations

  Mobile performance suffers          URP, baked lighting, simple
                                      colliders, profiling, asset budgets

  Child gets stuck or loses progress  Reset-to-road, autosave, no fail
                                      states, activity resume support
  -----------------------------------------------------------------------

------------------------------------------------------------------------

# 19. Content Expansion Strategy

After the first release is stable, expand through modular additions: -
**New vehicles:** cement mixer, crane, road roller, loader, concrete
pump. - **New projects:** playground, bridge, fire station, school,
supermarket, park. - **New districts:** beach, forest, mountain,
airport. - **Customisation:** paint, decals, wheels, construction hats,
yard decorations. - **New interactions:** lift, pour, flatten, paint,
assemble, transport.

Each content pack should reuse existing vehicle and activity systems
rather than introduce a new architecture.

------------------------------------------------------------------------

# 20. North Star

The player should finish a short session feeling that they drove a big
machine, made something happen, and helped their little town become a
better place.

**Little Builders World is a construction playground first and a mission
game second.**
