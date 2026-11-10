# Art Direction — Little Builders World

Toy-like, rounded, bright but balanced. Everything is built from rounded boxes, discs, domes and spheres
(`MeshFactory`), shaded with URP Simple Lit and a small shared palette. No textures except subtle speckle tiles
(grass, sand, dirt), the road, and the crawler-track cleats.

**Smooth vehicle models (Blender).** `Tools/blender/build_models.sh` builds vehicles in Blender from a script
(bevelled, smooth-shaded parts, chunky treaded tyres, bigger faces with brows and a smile), unwraps them into one
atlas and bakes a 1024² texture: base colour × soft ambient occlusion, gloss in alpha, plus a glow mask for lights.
Paintable parts are baked near-white and tinted by the material colour, so the paint shop still works. The mesh
pack keeps the same named pivots (wheels, steer pivots, bed, beacon), so gameplay code is unchanged; without the
pack the procedural vehicle is used. Crawler belts keep their own UVs so the tread still scrolls; sprockets and
road wheels spin. Vertices: dump truck ~33k (six wheels), excavator ~16k, bulldozer ~15k, roller ~16k; 3 materials
+ the track material each, one texture per vehicle. Run Blender, then
`Tools/setup.sh`.

## Scale (1 unit = 1 m)

| Thing | Size |
|---|---|
| Dump truck | 2.5 w × 5.6 l × 3.0 h, wheels r 0.62 |
| Excavator | 2.6 w × 3.6 l (tracks), cab 2.3 h, arm reach ~5.5 m |
| Bulldozer | 2.9 w (blade 3.3) × 4.6 l × 3.3 h |
| Road roller | 2.45 w × 4.6 l × 3.3 h, drum r 0.8 |
| Roads | 10 m (loop), 9 m (spurs) |
| Builder figure | ~2.6 m at 1.6× scale (readable at camera distance) |
| Town | loop road ±60 m; play area ±97 m |

Vehicles are slightly exaggerated (big wheels, tall cabs, big blades/buckets) and each has a face (eyes on the
windscreen) — friendly but not identical: different body shapes and colours.

## Detail pass (v0.3.0)

* **Seen from behind first.** The camera follows the vehicle, so every machine has a readable back: tail lights
  and indicators (slightly emissive), grilles, plates, hazard chevrons, mud flaps, a bulldozer ripper.
* Friendly faces: eyes plus rosy cheeks on every machine; people have big heads, open smiles, cheeks, shoes.
* Treaded tyres with coloured hub caps (secondary colour, repaints with the vehicle).
* World dressing: rolling hills on the horizon (no flat edge), a duck pond, flowers and grass tufts, framed
  windows with flower boxes, real-looking potholes, patterned play mats, red windmill sails, glowing lamps.

### Budget rules (Redmi A3, PowerVR GE8320)

* Rounded-box bevel detail follows bevel size (≤0.05 m → chamfer, ≤0.1 m → 2 steps, else 3); small spheres use
  fewer facets. Little parts never use the full-detail meshes.
* Vehicle parts that share a parent and material are merged into one mesh at build time (`VehicleBuilder.MergeParts`).
* Hundreds of meadow pieces are a few combined meshes, no shadows. Static scenery is static-batched.
* Check with `Tools/showcase.sh` (renders `Logs/Showcase/*.png` and logs scene vertex counts). Town after the
  detail pass: ~490k vertices total in the scene (before: ~513k).
* No post-processing / bloom; emission is used sparingly for lights.

## Palette

| Use | Hex |
|---|---|
| Excavator yellow | `#FFC93C` (+ slate `#4B4E6D`) |
| Dump truck red / blue bed | `#FF595E` / `#1982C4` |
| Bulldozer orange | `#FF9F1C` (+ grey blade `#5B5F73`) |
| Road roller green / yellow canopy | `#6BCB77` / `#FFD447` |
| Grass | `#8ED46F` speckled |
| Yard sand | `#E2CB98` speckled |
| Dirt / soil | `#B98355` / `#8B5A3C` |
| Asphalt / fresh tarmac | `#5C616D` / `#4A4E58` |
| Sky | `#A8DEFF` (fog `#BFE6FF`) |
| UI ink (outlines, text) | `#3A2E2A`; cream panels `#FFF7E8` |
| Positive (Go, check) | `#6BCB77`; Back `#FF9F1C`; steering `#1982C4`; Exit `#FF595E` |

Rules: colour is never the only cue — every interactive thing also has a shape (icon, ring, marker, arrow) and
motion (bob, pulse). Controls keep a dark outline so they read on any background.

## UI

Reference resolution 1920×1080, safe-area aware. Driving buttons 220–300 px; smallest child-facing button 150 px.
Child-facing UI uses icons only; words appear only on grown-up screens (settings, parent gate, pause labels).
Font: Fredoka SemiBold (SIL OFL).

## Ambient life

Windmill blades, beacons on driven vehicles, swaying trees, drifting clouds, waving flags, a waving resident at
the finished house, chimney smoke, balloon arch bob. All in one `AmbientMotion` component.
