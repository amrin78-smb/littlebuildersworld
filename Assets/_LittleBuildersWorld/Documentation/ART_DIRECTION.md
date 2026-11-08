# Art Direction — Little Builders World

Toy-like, rounded, bright but balanced. Everything is built from rounded boxes, discs, domes and spheres
(`MeshFactory`), shaded with URP Simple Lit and a small shared palette. No textures except subtle speckle tiles
(grass, sand, dirt), the road, and the crawler-track cleats.

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
