"""
Builds the smooth, textured vehicle models in Blender (headless). Run via Tools/blender/build_models.sh.
Positions are Unity metres (+Y up, +Z forward), matching the pivots VehicleBuilder wires to gameplay.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import lbw_blender as L  # noqa: E402
from lbw_blender import U, rbox, cyl, lathe, sphere, tube_arc, tub, arch, instance, unity_matrix, track_belt  # noqa: E402

# Shared trim palette (same hues as ArtLibrary).
STEEL, DARK, RUBBER, INK, WHITE = "#A7B1BC", "#596270", "#2F2E3A", "#2B2523", "#FFFFFF"


def looks(m):
    return {
        "paint": m.get("Paint", kind="paint", rough=0.45),
        "steel": m.get("Steel", STEEL, rough=0.35, dirt=0.25),
        "chrome": m.get("Chrome", "#D5DCE3", rough=0.18),
        "dark": m.get("DarkSteel", DARK, rough=0.5, dirt=0.35),
        "rubber": m.get("Rubber", RUBBER, rough=0.85, dirt=0.45),
        "ink": m.get("Ink", INK, rough=0.3),
        "white": m.get("White", WHITE, rough=0.15),
        "glass": m.get("Glass", kind="glass", rough=0.08),
        "cheek": m.get("Cheek", "#FF9AAE", rough=0.5),
        "head": m.get("HeadLight", "#FFF6C8", rough=0.1, emit=1.0),
        "tail": m.get("TailLight", "#F0384A", rough=0.15, emit=0.8),
        "amber": m.get("Indicator", "#FFA62B", rough=0.15, emit=0.8),
        "beacon": m.get("Beacon", "#FFA62B", rough=0.15, emit=0.6),
        "hazard": m.get("Hazard", kind="stripes", a="#FFD447", b=INK, scale=3.2, rough=0.4),
        "grille": m.get("Grille", kind="hbands", a="#3A3F4B", b="#6B7383", scale=9.0, rough=0.4),
        "plate": m.get("Plate", "#FFE27A", rough=0.4),
    }


def face(md, k, node, centre, spacing, size, z_out=0.0):
    """Big friendly eyes with pupils, shine and eyebrows (Unity +Z facing)."""
    cx, cy, cz = centre
    for side in (-1, 1):
        x = cx + side * spacing
        md.add(sphere("Eye", size, (x, cy, cz), (1.0, 1.18, 0.42)), node, "Trim", k["white"])
        md.add(sphere("Pupil", size * 0.52, (x - side * 0.03, cy - size * 0.1, cz + size * 0.3), (1, 1.12, 0.45), 18, 10),
               node, "Trim", k["ink"])
        md.add(sphere("Shine", size * 0.17, (x - side * 0.03 + size * 0.17, cy + size * 0.12, cz + size * 0.5), segs=10, rings=6),
               node, "Trim", k["white"])
        md.add(rbox("Brow", (size * 1.25, size * 0.2, 0.05), (x, cy + size * 1.5, cz + 0.02), size * 0.09, 2,
                    euler=(0, 0, -side * 9)), node, "Trim", k["ink"])


def wheel_proto(md, k, radius, width, name):
    """One chunky tyre with chevron lugs, dished rim, painted hub cap and bolts. Returns (objects, slot, look)."""
    R, W = radius, width
    parts = []
    # Tyre cross-section (radius, axial) as a rounded rectangle, revolved around the axle.
    r0, r1, hw, cr = R * 0.56, R * 0.9, W * 0.5, R * 0.14
    prof = [(r0, -hw + 0.02), (r0 + 0.04, -hw)]
    for i in range(7):
        t = math.radians(90 * i / 6)
        prof.append((r1 - cr + cr * math.sin(t), -hw + cr - cr * math.cos(t)))
    for i in range(7):
        t = math.radians(90 * i / 6)
        prof.append((r1 - cr + cr * math.cos(t), hw - cr + cr * math.sin(t)))
    prof += [(r0 + 0.04, hw), (r0, hw - 0.02), (r0, -hw + 0.02)]
    tyre = lathe(name + "Tyre", prof, (0, 0, 0), "x", segs=36)
    parts.append((tyre, "Trim", k["rubber"]))
    # Chevron lugs (one mesh, linked copies).
    lug = rbox(name + "Lug", (W * 0.5, R * 0.16, R * 0.24), (0, 0, 0), 0.03, 1)
    n = 11
    for row, side in enumerate((-1, 1)):
        for i in range(n):
            ang = 360.0 * (i + 0.5 * row) / n
            m = unity_matrix((0, 0, 0), (ang, 0, 0)) @ unity_matrix((side * W * 0.23, r1 + R * 0.02, 0), (0, side * 18, 0))
            parts.append((instance(lug, f"{name}Lug", m), "Trim", k["rubber"]))
    lug.matrix_world = unity_matrix((0, -100, 0))  # prototype parked (not exported)
    # Dished steel rim, both faces.
    rim = lathe(name + "Rim", [(0.0, -hw - 0.005), (R * 0.2, -hw - 0.005), (R * 0.26, -hw + 0.05), (R * 0.5, -hw + 0.04),
                               (R * 0.57, -hw + 0.01), (R * 0.57, hw - 0.01), (R * 0.5, hw - 0.04), (R * 0.26, hw - 0.05),
                               (R * 0.2, hw + 0.005), (0.0, hw + 0.005)], (0, 0, 0), "x", segs=32)
    parts.append((rim, "Trim", k["steel"]))
    cap = lathe(name + "Cap", [(0.0, -hw - 0.07), (R * 0.12, -hw - 0.065), (R * 0.19, -hw - 0.03), (R * 0.21, -hw + 0.02),
                               (R * 0.21, hw - 0.02), (R * 0.19, hw + 0.03), (R * 0.12, hw + 0.065), (0.0, hw + 0.07)],
                (0, 0, 0), "x", segs=24)
    parts.append((cap, "Secondary", k["paint"]))
    bolt = cyl(name + "Bolt", 0.035, W + 0.04, (0, 0, 0), "x", segs=8, bevel=0.012, bsegs=1)
    for i in range(5):
        a = 2 * math.pi * i / 5
        parts.append((instance(bolt, f"{name}Bolt", unity_matrix((0, math.cos(a) * R * 0.37, math.sin(a) * R * 0.37), (0, 0, 90))),
                      "Trim", k["dark"]))
    bolt.matrix_world = unity_matrix((0, -100, 0))
    for o in (lug, bolt):
        L.bpy.context.scene.collection.objects.unlink(o)
    return parts


def place_wheel(md, proto, node):
    w = md.world[node]
    for obj, slot, look in proto:
        md.add(instance(obj, obj.name, unity_matrix((w.x, w.y, w.z)) @ obj.matrix_world), node, slot, look)


# ================================================================================================= DUMP TRUCK
def dump_truck():
    L.reset()
    m = L.Mats()
    k = looks(m)
    md = L.Model("DumpTruck")
    B = "Body"
    # Pivots (must match the gameplay wiring in VehicleBuilder).
    wheels = []
    for side, tag in ((-1, "L"), (1, "R")):
        sp = md.node(f"SteerPivot_{tag}", B, (side * 1.05, 0.62, 1.75))
        wheels.append(md.node(f"Wheel_F{tag}", sp, (0, 0, 0)))
        wheels.append(md.node(f"Wheel_M{tag}", B, (side * 1.05, 0.62, -1.0)))
        wheels.append(md.node(f"Wheel_R{tag}", B, (side * 1.05, 0.62, -2.15)))
    bed = md.node("BedPivot", B, (0, 1.25, -2.55))
    beacon = md.node("Beacon", B, (0, 3.05, 1.85))

    # ---- wheels (one prototype, linked copies)
    proto = wheel_proto(md, k, 0.62, 0.5, "T")
    for w in wheels:
        place_wheel(md, proto, w)
    for obj, _, _ in proto:
        L.bpy.data.objects.remove(obj)

    # ---- chassis and running gear
    md.add(rbox("Chassis", (1.3, 0.36, 5.0), (0, 0.93, -0.2), 0.08, 2), B, "Trim", k["dark"])
    md.add(rbox("Axle", (1.7, 0.16, 0.16), (0, 0.62, 1.75), 0.06, 2), B, "Trim", k["dark"])
    for z in (-1.0, -2.15):
        md.add(rbox("Axle", (1.7, 0.18, 0.18), (0, 0.62, z), 0.06, 2), B, "Trim", k["dark"])
    md.add(rbox("Bogie", (1.5, 0.22, 1.5), (0, 0.85, -1.58), 0.08, 2), B, "Trim", k["dark"])

    # ---- cab: soft rounded block with a gently sloped, narrower top
    md.add(rbox("Cab", (2.36, 1.8, 1.72), (0, 2.12, 1.9), 0.38, 6, top=(0.93, 0.88), top_shift=-0.1), B, "Primary", k["paint"])
    for side in (-1, 1):
        md.add(arch("Fender", 0.7, 0.8, 0.66, (side * 1.07, 0.62, 1.75), -8, 188, 28), B, "Primary", k["paint"])
        md.add(rbox("Step", (0.36, 0.08, 0.42), (side * 1.12, 0.98, 0.95), 0.03, 2), B, "Trim", k["dark"])
    md.add(rbox("WindFrame", (2.06, 0.92, 0.06), (0, 2.47, 2.68), 0.12, 3, euler=(-3.5, 0, 0)), B, "Trim", k["ink"])
    md.add(rbox("Windscreen", (1.94, 0.8, 0.06), (0, 2.47, 2.71), 0.1, 3, euler=(-3.5, 0, 0)), B, "Trim", k["glass"])
    face(md, k, B, (0, 2.45, 2.75), 0.44, 0.22)
    for side in (-1, 1):
        md.add(sphere("Cheek", 0.15, (side * 0.85, 1.92, 2.73), (1, 0.62, 0.32)), B, "Trim", k["cheek"])
    md.add(tube_arc("Smile", 0.26, 0.035, (0, 2.08, 2.76), 205, 335, 18, 8), B, "Trim", k["ink"])
    md.add(rbox("Grille", (1.3, 0.42, 0.08), (0, 1.55, 2.76), 0.08, 3, top=(1.05, 1.0)), B, "Trim", k["grille"])
    md.add(rbox("Bumper", (2.5, 0.36, 0.34), (0, 1.02, 2.8), 0.16, 4), B, "Trim", k["chrome"])
    for side in (-1, 1):
        x = side * 0.88
        md.add(lathe("LightRim", [(0, 0.0), (0.17, 0.0), (0.19, 0.05), (0.17, 0.09), (0, 0.09)], (x, 1.55, 2.73), "z", 24),
               B, "Trim", k["chrome"])
        md.add(sphere("HeadLight", 0.14, (x, 1.55, 2.81), (1, 1, 0.45), 20, 10), B, "Trim", k["head"])
    md.add(rbox("Visor", (2.12, 0.09, 0.38), (0, 3.0, 2.66), 0.04, 2), B, "Trim", k["dark"])
    for x in (-0.5, 0.0, 0.5):
        md.add(sphere("RoofLight", 0.07, (x, 3.07, 2.78), (1.4, 0.8, 1)), B, "Trim", k["amber"])
    for side in (-1, 1):
        md.add(rbox("SideWindow", (0.06, 0.6, 0.7), (side * 1.12, 2.47, 1.82), 0.1, 3, euler=(0, 0, side * 2.5)), B, "Trim", k["glass"])
        md.add(rbox("DoorLine", (0.03, 1.15, 0.03), (side * 1.17, 1.95, 1.18), 0.012, 1), B, "Trim", k["ink"])
        md.add(rbox("Handle", (0.05, 0.06, 0.22), (side * 1.19, 1.82, 1.36), 0.025, 2), B, "Trim", k["chrome"])
        # Mirror on a little arm.
        md.add(cyl("MirrorArm", 0.025, 0.36, (side * 1.32, 2.28, 2.5), "x", 8, 0.0), B, "Trim", k["dark"])
        md.add(rbox("Mirror", (0.1, 0.44, 0.28), (side * 1.5, 2.3, 2.48), 0.05, 3), B, "Primary", k["paint"])
        md.add(rbox("MirrorGlass", (0.02, 0.36, 0.2), (side * 1.5, 2.3, 2.34), 0.02, 1, euler=(0, 90, 0)), B, "Trim", k["chrome"])
    # Beacon: base on the body, spinning dome on its pivot.
    md.add(lathe("BeaconBase", [(0, -0.06), (0.2, -0.06), (0.21, 0.0), (0.18, 0.03), (0, 0.03)], (0, 3.04, 1.85), "y", 24),
           B, "Trim", k["dark"])
    md.add(lathe("BeaconDome", [(0, 0.24), (0.06, 0.235), (0.12, 0.2), (0.15, 0.13), (0.16, 0.0), (0, 0.0)],
                 md.at(beacon, 0, 0.02, 0), "y", 24), beacon, "Trim", k["beacon"])
    md.add(rbox("Glint", (0.05, 0.12, 0.05), md.at(beacon, 0.12, 0.11, 0), 0.02, 1), beacon, "Trim", k["white"])
    # Exhaust stack behind the cab, fuel tank and tool box.
    md.add(cyl("Exhaust", 0.09, 2.1, (1.36, 2.25, 0.88), "y", 16, 0.02), B, "Trim", k["chrome"])
    md.add(cyl("ExhaustCap", 0.12, 0.16, (1.36, 3.3, 0.88), "y", 16, 0.03), B, "Trim", k["ink"])
    md.add(rbox("HeatShield", (0.1, 0.7, 0.24), (1.25, 2.1, 0.88), 0.04, 2), B, "Trim", k["dark"])
    md.add(cyl("FuelTank", 0.3, 0.9, (-1.0, 1.0, 0.3), "z", 28, 0.08, 3), B, "Trim", k["steel"])
    for z in (0.05, 0.55):
        md.add(cyl("Strap", 0.315, 0.06, (-1.0, 1.0, z), "z", 28, 0.0), B, "Trim", k["dark"])
    md.add(rbox("ToolBox", (0.5, 0.46, 0.8), (1.0, 1.0, 0.3), 0.07, 2), B, "Trim", k["dark"])
    md.add(rbox("ToolLid", (0.52, 0.06, 0.82), (1.0, 1.24, 0.3), 0.03, 1), B, "Primary", k["paint"])
    # Rear end (the driving view): bumper, lights, plate, mud flaps.
    md.add(rbox("RearBumper", (2.3, 0.24, 0.22), (0, 0.95, -2.78), 0.09, 3), B, "Trim", k["dark"])
    for side in (-1, 1):
        md.add(rbox("TailLight", (0.3, 0.18, 0.08), (side * 0.86, 0.97, -2.9), 0.04, 2), B, "Trim", k["tail"])
        md.add(rbox("Indicator", (0.14, 0.18, 0.08), (side * 0.6, 0.97, -2.9), 0.04, 2), B, "Trim", k["amber"])
        md.add(rbox("MudFlap", (0.5, 0.5, 0.05), (side * 1.05, 0.55, -2.84), 0.04, 2), B, "Trim", k["rubber"])
        md.add(cyl("FlapBadge", 0.1, 0.02, (side * 1.05, 0.6, -2.87), "z", 16, 0.0), B, "Trim", k["plate"])
    md.add(rbox("Plate", (0.56, 0.22, 0.04), (0, 0.97, -2.9), 0.04, 2), B, "Trim", k["plate"])
    for i in range(3):
        md.add(sphere("PlateDot", 0.04, (-0.16 + i * 0.16, 0.97, -2.925), (1, 1, 0.4), 10, 6), B, "Trim", k["ink"])

    # ---- tipping bed (pivot at the back hinge; floor runs +Z from it)
    md.add(tub("Bed", (2.4, 1.0, 3.5), md.at(bed, 0, 0, 1.75), 0.12, 0.06, 1.035), bed, "Secondary", k["paint"])
    md.add(rbox("HeadBoard", (2.36, 0.5, 0.16), md.at(bed, 0, 1.2, 3.42), 0.07, 3, top=(0.92, 1)), bed, "Secondary", k["paint"])
    for side in (-1, 1):
        md.add(rbox("TopRail", (0.17, 0.15, 3.52), md.at(bed, side * 1.215, 1.0, 1.75), 0.07, 3), bed, "Secondary", k["paint"])
        md.add(rbox("Stripe", (0.03, 0.13, 3.1), md.at(bed, side * 1.235, 0.62, 1.75), 0.015, 1), bed, "Trim", k["white"])
        for z in (0.45, 1.75, 3.05):
            md.add(rbox("Rib", (0.08, 0.92, 0.14), md.at(bed, side * 1.235, 0.5, z), 0.035, 2), bed, "Secondary", k["paint"])
        md.add(cyl("Hinge", 0.08, 0.2, md.at(bed, side * 1.05, 0.88, -0.02), "x", 12, 0.02), bed, "Trim", k["dark"])
    md.add(rbox("Chevrons", (2.0, 0.26, 0.03), md.at(bed, 0, 0.5, -0.02), 0.02, 1), bed, "Trim", k["hazard"])
    md.add(rbox("BedSill", (2.2, 0.14, 0.2), md.at(bed, 0, -0.05, 0.12), 0.05, 2), bed, "Trim", k["dark"])
    return md, {"Primary": "#FF595E", "Secondary": "#1982C4"}, [
        ((6.5, 4.2, 9.5), (0, 1.6, 0.2)),
        ((-4.0, 3.6, -9.0), (0, 1.5, -0.6)),
        ((9.5, 2.2, 0.5), (0, 1.5, 0.0)),
    ]


# ================================================================================================= shared: tracks, lights
def tracks(md, k, xs, length, height, width, tag):
    """Crawler tracks: scrolling belt (own UVs, 'Track' slot), inner frame, spinning toothed sprockets and road wheels."""
    B = "Body"
    r = height * 0.5
    # Prototypes at the origin (spin about X).
    sp_parts = []
    sp_parts.append((lathe(tag + "Sprocket", [(0, -0.05), (r * 0.62, -0.05), (r * 0.68, -0.02), (r * 0.68, 0.02), (r * 0.62, 0.05), (0, 0.05)],
                           (0, 0, 0), "x", 24), "Trim", k["steel"]))
    sp_parts.append((lathe(tag + "SprocketHub", [(0, -0.09), (r * 0.22, -0.09), (r * 0.26, -0.05), (r * 0.26, 0.05), (r * 0.22, 0.09), (0, 0.09)],
                           (0, 0, 0), "x", 16), "Secondary", k["paint"]))
    tooth = rbox(tag + "Tooth", (0.08, r * 0.2, r * 0.16), (0, 0, 0), 0.025, 1)
    for i in range(10):
        m = unity_matrix((0, 0, 0), (360.0 * i / 10, 0, 0)) @ unity_matrix((0, r * 0.74, 0))
        sp_parts.append((instance(tooth, tag + "Tooth", m), "Trim", k["steel"]))
    L.bpy.context.scene.collection.objects.unlink(tooth)
    rw_parts = [(lathe(tag + "Roller", [(0, -0.07), (r * 0.36, -0.07), (r * 0.4, -0.04), (r * 0.4, 0.04), (r * 0.36, 0.07), (0, 0.07)],
                       (0, 0, 0), "x", 18), "Trim", k["dark"]),
                (lathe(tag + "RollerCap", [(0, -0.1), (r * 0.15, -0.1), (r * 0.16, 0.0), (r * 0.15, 0.1), (0, 0.1)], (0, 0, 0), "x", 12),
                 "Trim", k["steel"])]
    for x in xs:
        side = 1 if x > 0 else -1
        belt = track_belt(f"Belt{x}", length, height, width, (x, 0, 0))
        md.add(belt, B, "Track", k["rubber"])
        md.nobake.add(belt)
        md.add(rbox("TrackFrame", (width * 0.72, height - 0.14, length - 0.2), (x, r, 0), (height - 0.14) * 0.48, 5), B, "Trim", k["dark"])
        ox = x + side * (width * 0.5 - 0.02)
        for i, z in enumerate((length * 0.5 - r, -(length * 0.5 - r))):
            node = md.node(f"Wheel_Sprocket{'LR'[side > 0]}{i}", B, (ox, r, z))
            place_wheel(md, sp_parts, node)
        for i in range(3):
            z = (i - 1) * (length - height) / 3.0
            node = md.node(f"Wheel_Roller{'LR'[side > 0]}{i}", B, (ox, r * 0.62, z))
            place_wheel(md, rw_parts, node)
    for obj, _, _ in sp_parts + rw_parts:
        L.bpy.data.objects.remove(obj)


def head_light(md, k, node, pos):
    x, y, z = pos
    md.add(lathe("LightRim", [(0, 0.0), (0.14, 0.0), (0.16, 0.04), (0.14, 0.08), (0, 0.08)], (x, y, z - 0.06), "z", 20), node, "Trim", k["chrome"])
    md.add(sphere("HeadLight", 0.115, (x, y, z + 0.01), (1, 1, 0.45), 18, 10), node, "Trim", k["head"])


def rear_lights(md, k, node, centre, spacing, scale=1.0):
    cx, cy, cz = centre
    for side in (-1, 1):
        md.add(rbox("TailLight", (0.3 * scale, 0.17 * scale, 0.08), (cx + side * spacing, cy, cz), 0.04, 2), node, "Trim", k["tail"])
        md.add(rbox("Indicator", (0.14 * scale, 0.17 * scale, 0.08), (cx + side * (spacing - 0.25 * scale), cy, cz), 0.04, 2), node, "Trim", k["amber"])


def beacon_on(md, k, parent_node, beacon, base_pos):
    md.add(lathe("BeaconBase", [(0, -0.06), (0.19, -0.06), (0.2, 0.0), (0.17, 0.03), (0, 0.03)], base_pos, "y", 24), parent_node, "Trim", k["dark"])
    md.add(lathe("BeaconDome", [(0, 0.24), (0.06, 0.235), (0.12, 0.2), (0.15, 0.13), (0.16, 0.0), (0, 0.0)],
                 md.at(beacon, 0, 0.0, 0), "y", 24), beacon, "Trim", k["beacon"])
    md.add(rbox("Glint", (0.05, 0.12, 0.05), md.at(beacon, 0.12, 0.1, 0), 0.02, 1), beacon, "Trim", k["white"])


def exhaust(md, k, node, base, height, radius=0.09):
    x, y, z = base
    md.add(cyl("Exhaust", radius, height, (x, y + height * 0.5, z), "y", 16, 0.02), node, "Trim", k["chrome"])
    md.add(cyl("ExhaustCap", radius * 1.35, 0.14, (x, y + height, z), "y", 16, 0.03), node, "Trim", k["ink"])


def glass_panel(md, k, node, size, pos, euler=None, frame=True):
    if frame:
        md.add(rbox("WinFrame", (size[0] + 0.1, size[1] + 0.1, size[2]), (pos[0], pos[1], pos[2]) if size[0] > size[2] else pos, 0.1, 3, euler=euler),
               node, "Trim", k["ink"])
    off = (0, 0, 0.03) if size[0] > size[2] else (0.03 * (1 if pos[0] > 0 else -1), 0, 0)
    md.add(rbox("Glass", size, (pos[0] + off[0], pos[1], pos[2] + off[2]), 0.08, 3, euler=euler), node, "Trim", k["glass"])


# ================================================================================================= EXCAVATOR
def excavator():
    L.reset()
    k = looks(L.Mats())
    md = L.Model("Excavator")
    B = "Body"
    cab = md.node("Cab", B, (0, 1.0, 0))
    boom = md.node("Boom", cab, (0.45, 1.15, 0.95))
    stick = md.node("Stick", boom, (0, 0, 3.1))
    bucket = md.node("Bucket", stick, (0, 0, 2.15))
    beacon = md.node("Beacon", cab, (-0.6, 2.36, 0.45))
    tracks(md, k, (-1.0, 1.0), 3.6, 0.85, 0.7, "X")
    md.add(rbox("Undercarriage", (1.4, 0.45, 2.4), (0, 0.75, 0), 0.15, 3), B, "Trim", k["dark"])
    md.add(cyl("Turntable", 0.85, 0.18, (0, 0.95, 0), "y", 36, 0.05), B, "Trim", k["dark"])

    def C(x, y, z):
        return md.at(cab, x, y, z)
    md.add(rbox("Deck", (2.5, 0.85, 2.7), C(0, 0.45, -0.1), 0.3, 5), cab, "Primary", k["paint"])
    md.add(rbox("Counterweight", (2.45, 0.85, 0.8), C(0, 0.55, -1.35), 0.36, 6, top=(0.96, 0.9)), cab, "Secondary", k["paint"])
    md.add(rbox("Hazard", (1.0, 0.2, 0.03), C(0, 0.55, -1.76), 0.03, 1), cab, "Trim", k["hazard"])
    rear_lights(md, k, cab, C(0, 0.55, -1.76), 0.82, 0.8)
    md.add(rbox("Cabin", (1.15, 1.42, 1.3), C(-0.6, 1.56, 0.45), 0.3, 6, top=(0.92, 0.86), top_shift=-0.06), cab, "Primary", k["paint"])
    glass_panel(md, k, cab, (0.86, 0.7, 0.05), C(-0.6, 1.78, 1.1), euler=(-3, 0, 0))
    face(md, k, cab, C(-0.6, 1.76, 1.16), 0.21, 0.15)
    for side in (-1, 1):
        md.add(sphere("Cheek", 0.1, C(-0.6 + side * 0.37, 1.3, 1.1), (1, 0.62, 0.32)), cab, "Trim", k["cheek"])
    md.add(tube_arc("Smile", 0.15, 0.028, C(-0.6, 1.36, 1.12), 210, 330, 14, 8), cab, "Trim", k["ink"])
    md.add(rbox("SideWindowL", (0.05, 0.66, 0.84), C(-1.165, 1.8, 0.42), 0.08, 3), cab, "Trim", k["glass"])
    md.add(rbox("SideWindowR", (0.05, 0.66, 0.84), C(-0.035, 1.8, 0.42), 0.08, 3), cab, "Trim", k["glass"])
    md.add(rbox("RearWindow", (0.78, 0.52, 0.05), C(-0.6, 1.85, -0.2), 0.08, 3), cab, "Trim", k["glass"])
    md.add(rbox("Handle", (0.05, 0.06, 0.22), C(-1.19, 1.35, 0.2), 0.025, 2), cab, "Trim", k["chrome"])
    for x in (-0.9, -0.3):
        md.add(rbox("WorkLight", (0.2, 0.14, 0.12), C(x, 2.31, 0.98), 0.05, 2), cab, "Trim", k["head"])
    md.add(rbox("EngineCover", (1.0, 0.1, 0.95), C(0.55, 0.92, -0.55), 0.04, 2), cab, "Trim", k["dark"])
    md.add(rbox("Vents", (0.8, 0.04, 0.7), C(0.55, 0.97, -0.55), 0.02, 1), cab, "Trim", k["grille"])
    exhaust(md, k, cab, C(0.95, 0.9, -0.95), 0.75, 0.08)
    md.add(cyl("Rail", 0.035, 1.3, C(1.12, 1.15, -0.1), "z", 10, 0.0), cab, "Trim", k["chrome"])
    for z in (-0.65, 0.45):
        md.add(cyl("RailPost", 0.03, 0.3, C(1.12, 1.0, z), "y", 8, 0.0), cab, "Trim", k["chrome"])
    beacon_on(md, k, cab, beacon, C(-0.6, 2.3, 0.45))

    def Bm(x, y, z):
        return md.at(boom, x, y, z)
    md.add(rbox("BoomArm", (0.42, 0.52, 3.3), Bm(0, 0.02, 1.55), 0.19, 5, top=(0.9, 0.96)), boom, "Primary", k["paint"])
    md.add(cyl("BoomPin", 0.2, 0.56, Bm(0, 0, 0), "x", 20, 0.04), boom, "Trim", k["dark"])
    md.add(cyl("BoomRamBody", 0.13, 0.9, Bm(0, -0.36, 0.45), "z", 16, 0.04), boom, "Trim", k["dark"])
    md.add(cyl("BoomRamRod", 0.075, 1.1, Bm(0, -0.36, 1.4), "z", 14, 0.02), boom, "Trim", k["chrome"])
    md.add(cyl("StickRamBody", 0.12, 0.8, Bm(0, 0.4, 1.75), "z", 16, 0.04), boom, "Trim", k["dark"])
    md.add(cyl("StickRamRod", 0.07, 0.95, Bm(0, 0.4, 2.55), "z", 14, 0.02), boom, "Trim", k["chrome"])
    for side in (-1, 1):
        md.add(rbox("Hose", (0.05, 0.05, 2.6), Bm(side * 0.13, 0.3, 1.5), 0.024, 2), boom, "Trim", k["ink"])

    def S(x, y, z):
        return md.at(stick, x, y, z)
    md.add(rbox("StickArm", (0.34, 0.42, 2.3), S(0, 0, 1.05), 0.15, 4), stick, "Secondary", k["paint"])
    md.add(cyl("StickPin", 0.24, 0.5, S(0, 0, 0), "x", 20, 0.04), stick, "Trim", k["dark"])
    md.add(cyl("BucketRamBody", 0.1, 0.6, S(0, 0.3, 0.75), "z", 14, 0.03), stick, "Trim", k["dark"])
    md.add(cyl("BucketRamRod", 0.06, 0.8, S(0, 0.3, 1.45), "z", 12, 0.02), stick, "Trim", k["chrome"])

    def K(x, y, z):
        return md.at(bucket, x, y, z)
    md.add(cyl("BucketPin", 0.19, 0.48, K(0, 0, 0), "x", 18, 0.04), bucket, "Trim", k["dark"])
    md.add(rbox("Shell", (1.0, 0.62, 0.8), K(0, -0.15, 0.38), 0.24, 5, bottom=(1.0, 0.8)), bucket, "Trim", k["dark"])
    md.add(rbox("Lip", (1.04, 0.1, 0.2), K(0, -0.4, 0.72), 0.04, 2), bucket, "Trim", k["steel"])
    for i in range(4):
        md.add(rbox("Tooth", (0.13, 0.11, 0.26), K(-0.36 + i * 0.24, -0.42, 0.85), 0.05, 2, top=(0.7, 0.7)), bucket, "Trim", k["steel"])
    return md, {"Primary": "#FFC93C", "Secondary": "#4B4E6D"}, [((7.5, 5.0, 9.0), (0, 1.8, 1.5)), ((-5.0, 4.0, -8.5), (0, 1.5, 0))]


# ================================================================================================= BULLDOZER
def bulldozer():
    L.reset()
    k = looks(L.Mats())
    md = L.Model("Bulldozer")
    B = "Body"
    beacon = md.node("Beacon", B, (0.55, 3.45, -0.75))
    tracks(md, k, (-1.05, 1.05), 3.6, 0.9, 0.75, "D")
    md.add(rbox("Body", (1.7, 1.1, 3.2), (0, 1.35, -0.1), 0.3, 5), B, "Primary", k["paint"])
    md.add(rbox("Hood", (1.55, 0.78, 1.35), (0, 2.0, 0.75), 0.3, 6, top=(0.9, 0.9)), B, "Primary", k["paint"])
    md.add(rbox("Cab", (1.6, 1.3, 1.4), (0, 2.55, -0.75), 0.3, 6, top=(0.93, 0.9)), B, "Primary", k["paint"])
    md.add(rbox("Roof", (1.9, 0.16, 1.7), (0, 3.28, -0.75), 0.07, 3), B, "Secondary", k["paint"])
    glass_panel(md, k, B, (1.2, 0.62, 0.05), (0, 2.74, -0.06))
    face(md, k, B, (0, 2.72, 0.0), 0.3, 0.17)
    for side in (-1, 1):
        md.add(sphere("Cheek", 0.11, (side * 0.6, 2.5, -0.03), (1, 0.6, 0.32)), B, "Trim", k["cheek"])
        md.add(rbox("SideWindow", (0.05, 0.6, 0.9), (side * 0.8, 2.72, -0.75), 0.08, 3), B, "Trim", k["glass"])
        md.add(rbox("Vent", (0.04, 0.42, 0.9), (side * 0.86, 1.38, -0.75), 0.02, 1), B, "Trim", k["grille"])
        md.add(rbox("Step", (0.3, 0.07, 0.4), (side * 0.95, 1.05, -1.2), 0.03, 1), B, "Trim", k["dark"])
    md.add(rbox("RearWindow", (1.15, 0.55, 0.05), (0, 2.76, -1.46), 0.08, 3), B, "Trim", k["glass"])
    md.add(rbox("Grille", (0.72, 0.44, 0.05), (0, 1.95, 1.43), 0.05, 2), B, "Trim", k["grille"])
    for side in (-1, 1):
        head_light(md, k, B, (side * 0.52, 2.2, 1.45))
    exhaust(md, k, B, (0.68, 2.3, 0.3), 0.95, 0.1)
    # Back: lights, grille, ripper.
    rear_lights(md, k, B, (0, 1.5, -1.72), 0.55, 0.85)
    md.add(rbox("RearGrille", (0.9, 0.22, 0.04), (0, 1.22, -1.71), 0.02, 1), B, "Trim", k["grille"])
    md.add(rbox("RipperBar", (1.5, 0.24, 0.3), (0, 1.0, -2.0), 0.1, 3), B, "Trim", k["dark"])
    for x in (-0.6, 0.6):
        md.add(rbox("RipperArm", (0.16, 0.2, 0.55), (x, 1.05, -1.8), 0.06, 2), B, "Trim", k["dark"])
    md.add(rbox("RipperShank", (0.3, 0.95, 0.34), (0, 0.62, -2.12), 0.1, 3, euler=(-12, 0, 0)), B, "Secondary", k["paint"])
    md.add(rbox("RipperTooth", (0.26, 0.26, 0.4), (0, 0.24, -2.22), 0.08, 2, euler=(20, 0, 0), top=(0.8, 0.8)), B, "Trim", k["steel"])
    beacon_on(md, k, B, beacon, (0.55, 3.4, -0.75))
    # Curved blade (concave to the front), cutting edge, end plates, push arms and rams.
    md.add(arch("Blade", 0.92, 1.04, 3.3, (0, 0.78, 3.32), 138, 222, 20), B, "Secondary", k["paint"])
    md.add(rbox("BladeEdge", (3.36, 0.14, 0.22), (0, 0.18, 2.52), 0.05, 2), B, "Trim", k["steel"])
    for x in (-1.68, 1.68):
        md.add(rbox("BladeCorner", (0.1, 1.15, 0.5), (x, 0.78, 2.5), 0.04, 2), B, "Trim", k["dark"])
    md.add(rbox("BladeBack", (2.6, 0.3, 0.16), (0, 0.85, 2.2), 0.07, 2), B, "Trim", k["dark"])
    for x in (-1.1, 1.1):
        md.add(rbox("BladeArm", (0.2, 0.26, 1.5), (x, 0.85, 1.55), 0.08, 3), B, "Trim", k["dark"])
        md.add(cyl("BladeRamBody", 0.1, 0.6, (x * 0.5, 1.6, 1.6), "z", 14, 0.03, euler=(22, 0, 0)), B, "Trim", k["dark"])
        md.add(cyl("BladeRamRod", 0.06, 0.7, (x * 0.5, 1.4, 2.1), "z", 12, 0.02, euler=(22, 0, 0)), B, "Trim", k["chrome"])
    return md, {"Primary": "#FF9F1C", "Secondary": "#5B5F73"}, [((7.0, 4.5, 9.0), (0, 1.5, 0.5)), ((-5.0, 4.0, -8.5), (0, 1.5, 0))]


# ================================================================================================= ROAD ROLLER
def road_roller():
    L.reset()
    k = looks(L.Mats())
    md = L.Model("RoadRoller")
    B = "Body"
    steer = md.node("SteerPivot", B, (0, 0.8, 1.5))
    drum = md.node("Wheel_Drum", steer, (0, 0, 0))
    rear = [md.node("Wheel_RL", B, (-0.95, 0.65, -1.4)), md.node("Wheel_RR", B, (0.95, 0.65, -1.4))]
    beacon = md.node("Beacon", B, (0, 3.45, -0.92))
    proto = wheel_proto(md, k, 0.65, 0.55, "R")
    for w in rear:
        place_wheel(md, proto, w)
    for obj, _, _ in proto:
        L.bpy.data.objects.remove(obj)

    def D(x, y, z):
        return md.at(drum, x, y, z)
    hw = 1.05
    md.add(lathe("Drum", [(0.5, -hw + 0.02), (0.74, -hw), (0.79, -hw + 0.04), (0.8, -hw + 0.1), (0.8, hw - 0.1), (0.79, hw - 0.04),
                          (0.74, hw), (0.5, hw - 0.02), (0.5, -hw + 0.02)], D(0, 0, 0), "x", 48), drum, "Trim", k["steel"])
    md.add(lathe("DrumEnd", [(0, -hw + 0.03), (0.52, -hw + 0.03), (0.52, hw - 0.03), (0, hw - 0.03)], D(0, 0, 0), "x", 32), drum, "Trim", k["dark"])
    for side in (-1, 1):
        md.add(lathe("DrumRing", [(0.42, 0.0), (0.6, 0.0), (0.62, 0.04), (0.6, 0.08), (0.42, 0.08), (0.42, 0.0)],
                     D(side * (hw - 0.05), 0, 0), "x", 32, euler=(0, 0, 0) if side > 0 else (0, 180, 0)), drum, "Secondary", k["paint"])
        md.add(cyl("DrumHub", 0.18, 0.12, D(side * (hw + 0.02), 0, 0), "x", 20, 0.04), drum, "Trim", k["chrome"])
        md.add(rbox("DrumMark", (0.06, 0.16, 0.16), D(side * (hw + 0.04), 0.3, 0), 0.04, 2), drum, "Trim", k["ink"])

    def St(x, y, z):
        return md.at(steer, x, y, z)
    md.add(rbox("Yoke", (2.45, 0.52, 0.66), St(0, 0.98, 0), 0.24, 5), steer, "Primary", k["paint"])
    for x in (-1.17, 1.17):
        md.add(rbox("YokeSide", (0.14, 1.15, 1.05), St(x, 0.42, 0), 0.07, 3, bottom=(1.0, 0.75)), steer, "Secondary", k["paint"])
    face(md, k, steer, St(0, 1.02, 0.34), 0.42, 0.16)
    for side in (-1, 1):
        md.add(sphere("Cheek", 0.1, St(side * 0.78, 0.85, 0.32), (1, 0.6, 0.32)), steer, "Trim", k["cheek"])
    md.add(tube_arc("Smile", 0.14, 0.03, St(0, 0.93, 0.33), 210, 330, 14, 8), steer, "Trim", k["ink"])
    md.add(rbox("Scraper", (2.1, 0.08, 0.12), St(0, 0.25, -0.88), 0.03, 1), steer, "Trim", k["dark"])

    md.add(rbox("Body", (1.7, 1.0, 2.3), (0, 1.4, -0.9), 0.3, 5, top=(0.94, 0.95)), B, "Primary", k["paint"])
    md.add(rbox("Neck", (0.8, 0.6, 1.1), (0, 1.55, 0.55), 0.2, 4), B, "Secondary", k["paint"])
    for side in (-1, 1):
        md.add(arch("Fender", 0.72, 0.8, 0.62, (side * 0.97, 0.65, -1.4), 0, 180, 24), B, "Primary", k["paint"])
        md.add(rbox("Vent", (0.04, 0.4, 0.8), (side * 0.86, 1.4, -1.0), 0.02, 1), B, "Trim", k["grille"])
    for x in (-0.78, 0.78):
        for z in (-0.25, -1.6):
            md.add(cyl("Post", 0.05, 1.35, (x, 2.55, z), "y", 10, 0.0), B, "Trim", k["dark"])
    md.add(rbox("Canopy", (1.9, 0.16, 1.8), (0, 3.28, -0.92), 0.07, 3), B, "Secondary", k["paint"])
    md.add(rbox("Seat", (0.7, 0.25, 0.6), (0, 2.05, -1.0), 0.1, 3), B, "Trim", k["rubber"])
    md.add(rbox("SeatBack", (0.7, 0.55, 0.15), (0, 2.4, -1.32), 0.07, 3, euler=(-8, 0, 0)), B, "Trim", k["rubber"])
    md.add(cyl("Column", 0.04, 0.5, (0, 2.1, -0.4), "y", 10, 0.0, euler=(-30, 0, 0)), B, "Trim", k["dark"])
    md.add(tube_arc("SteeringWheel", 0.2, 0.03, (0, 2.35, -0.45), 0, 360, 24, 8, euler=(60, 0, 0)), B, "Trim", k["ink"])
    exhaust(md, k, B, (0.45, 1.85, -1.85), 1.0, 0.08)
    for x in (-0.78, 0.78):
        md.add(rbox("CanopyLight", (0.18, 0.14, 0.12), (x, 3.13, -0.1), 0.05, 2), B, "Trim", k["head"])
    rear_lights(md, k, B, (0, 1.55, -2.07), 0.58, 0.85)
    md.add(rbox("RearGrille", (0.9, 0.22, 0.04), (0, 1.25, -2.06), 0.02, 1), B, "Trim", k["grille"])
    beacon_on(md, k, B, beacon, (0, 3.4, -0.92))
    return md, {"Primary": "#6BCB77", "Secondary": "#FFD447"}, [((7.0, 4.0, 8.0), (0, 1.4, 0.3)), ((-5.0, 4.0, -8.5), (0, 1.5, 0))]


BUILDERS = {"DumpTruck": dump_truck, "Excavator": excavator, "Bulldozer": bulldozer, "RoadRoller": road_roller}


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    want_preview = "--preview" in argv
    names = [a for a in argv if not a.startswith("--")] or list(BUILDERS)
    for name in names:
        md, tints, views = BUILDERS[name]()
        tex, em = L.bake(md)
        L.export(md)
        if want_preview:
            L.preview(md, tex, em, tints, f"{L.LOG_DIR}/{name}_preview", views)
    print("[LBW] build_models done")


main()
