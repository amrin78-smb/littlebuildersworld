"""
Smooth, textured environment props built in Blender (headless). Run via Tools/blender/build_env.sh [Group ...].
Each group bakes into ONE texture (Unity: one material for the whole group, so static batching works across all
of those props). Positions are Unity metres (+Y up, +Z forward) relative to each prop's origin.
"""
import math
import os
import sys

import bmesh

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import lbw_blender as L  # noqa: E402
from lbw_blender import Vector, rbox, cyl, lathe, sphere, blob, rock, unity_matrix, instance, sweep, circle, tube_arc  # noqa: E402
from mathutils import Matrix  # noqa: E402

PROP_DIR = L.REPO + "/Assets/_LittleBuildersWorld/Art/Models/Props"
PROP_TEX = L.REPO + "/Assets/_LittleBuildersWorld/Art/Textures/Props"
SPACING = 30.0  # props are laid out apart so their soft shadows don't touch


def tier(name, radius, height, base_y, lobes=8, segs=24):
    """Pine tier: a cone with a soft, scalloped, slightly drooping rim."""
    bm = bmesh.new()
    rings = []
    profile = [(1.0, 0.0), (0.97, 0.06), (0.7, 0.32), (0.42, 0.6), (0.18, 0.85), (0.0, 1.0)]
    for fr, fh in profile:
        ring = []
        for i in range(segs):
            a = 2 * math.pi * i / segs
            wob = 1.0 + (0.08 * math.sin(lobes * a) if fr > 0.9 else 0.0)
            droop = (0.08 * (1 - math.cos(lobes * a)) if fr > 0.9 else 0.0)
            r = radius * fr * wob
            ring.append(bm.verts.new(Vector((math.cos(a) * r, math.sin(a) * r, height * fh - droop))))
            if fr == 0.0:
                break
        rings.append(ring)
    for k in range(len(rings) - 1):
        a, b = rings[k], rings[k + 1]
        for i in range(segs):
            if len(b) == 1:
                bm.faces.new((a[i], a[(i + 1) % segs], b[0]))
            else:
                bm.faces.new((a[i], a[(i + 1) % segs], b[(i + 1) % segs], b[i]))
    bm.faces.new(list(reversed(rings[0])))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = L._link(name, bm)
    L._bake_modifiers(obj, None)
    return L._place(obj, (0, base_y, 0))


def patch(name, radius, center, sx=1.25, seed=1, rough=0.12, ring=None, y=0.0, rim_h=0.0, segs=40):
    """Irregular flat patch (centre fan); with ring=(inner, outer) builds a raised broken rim band instead."""
    import random
    rnd = random.Random(seed)
    ph = [rnd.uniform(0, 6.28) for _ in range(3)]

    def rad(a, base):
        return base * (1 + rough * (0.6 * math.sin(3 * a + ph[0]) + 0.3 * math.sin(5 * a + ph[1]) + 0.2 * math.sin(9 * a + ph[2])))
    bm = bmesh.new()
    if ring is None:
        c = bm.verts.new(Vector((0, 0, y)))
        rim = [bm.verts.new(Vector((math.cos(a) * rad(a, radius) * sx, math.sin(a) * rad(a, radius), y)))
               for a in (2 * math.pi * i / segs for i in range(segs))]
        for i in range(segs):
            bm.faces.new((c, rim[i], rim[(i + 1) % segs]))
    else:
        inner, outer = [], []
        for i in range(segs):
            a = 2 * math.pi * i / segs
            ri, ro = rad(a, ring[0]), rad(a, ring[1])
            inner.append(bm.verts.new(Vector((math.cos(a) * ri * sx, math.sin(a) * ri, y + rim_h))))
            outer.append(bm.verts.new(Vector((math.cos(a) * ro * sx, math.sin(a) * ro, y))))
        for i in range(segs):
            j = (i + 1) % segs
            bm.faces.new((inner[i], outer[i], outer[j], inner[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for f in bm.faces:
        if f.normal.z < 0:
            f.normal_flip()
    obj = L._link(name, bm)
    L._bake_modifiers(obj, None, weighted=False)
    return L._place(obj, center)


def at(md, x, y, z):
    o = md.world["Body"]
    return (o.x + x, o.y + y, o.z + z)


# ================================================================================================= NATURE
def nature(m):
    leaf = m.get("LeafRound", "#B9E88A", rough=0.75, kind="foliage", a="#2E8B3E", b="#8CD05E", scale=(1.6, 3.4))
    pine = m.get("LeafPine", "#8FD47A", rough=0.75, kind="foliage", a="#1F6B3A", b="#5DB565", scale=(0.6, 3.6))
    bushm = m.get("LeafBush", "#C6EE96", rough=0.75, kind="foliage", a="#3A9A40", b="#9BD86A", scale=(-0.3, 0.6))
    bark = m.get("Bark", rough=0.85, kind="bark", a="#6E4A30", b="#9A6A44", scale=6.0)
    apple = m.get("Apple", "#FF5A5F", rough=0.25)
    pink = m.get("FlowerPink", "#FF8FC7", rough=0.5)
    yellow = m.get("FlowerYellow", "#FFE066", rough=0.5)
    stone = m.get("Stone", "#9DC46A", rough=0.8, kind="rock", a="#8B8984", b="#B9B5AE", scale=3.0)
    log_end = m.get("LogEnd", rough=0.7, kind="rings", a="#D9A86C", b="#B07A48", scale=9.0)
    models = []

    # Round tree: flared trunk, branch stubs, one puffy canopy, a few apples.
    md = L.Model("TreeRound", (0 * SPACING, 0, 0))
    sway = md.node("Sway", "Body", (0, 0, 0))
    md.add(lathe("Trunk", [(0, 0), (0.46, 0), (0.36, 0.1), (0.27, 0.45), (0.22, 1.4), (0.2, 2.6), (0, 2.6)], at(md, 0, 0, 0), "y", 12), sway, "Trim", bark)
    md.add(cyl("Branch", 0.09, 0.8, at(md, 0.35, 2.2, 0.1), "y", 8, 0.0, euler=(0, 0, -45)), sway, "Trim", bark)
    md.add(blob("Canopy", [((0, 3.2, 0), 1.45), ((0.95, 3.6, 0.35), 1.05), ((-0.95, 3.45, -0.35), 1.0), ((0.15, 3.35, 1.0), 0.95),
                           ((-0.15, 4.15, -0.2), 1.0), ((0.35, 2.95, -0.95), 0.9), ((-0.7, 2.9, 0.7), 0.85)],
                at(md, 0, 0, 0), 0.16, target=420), sway, "Trim", leaf)
    for x, y, z in ((1.25, 3.0, 1.0), (-1.4, 3.2, 0.6), (0.3, 2.6, 1.55)):
        md.add(sphere("Apple", 0.16, at(md, x, y, z), segs=10, rings=6), sway, "Trim", apple)
    models.append(md)

    # Pine: three scalloped tiers on a short trunk.
    md = L.Model("TreePine", (1 * SPACING, 0, 0))
    sway = md.node("Sway", "Body", (0, 0, 0))
    md.add(lathe("Trunk", [(0, 0), (0.36, 0), (0.27, 0.1), (0.2, 0.5), (0.18, 1.4), (0, 1.4)], at(md, 0, 0, 0), "y", 10), sway, "Trim", bark)
    for i, (r, h, y) in enumerate(((1.75, 2.0, 0.95), (1.35, 1.9, 2.0), (0.95, 1.8, 3.05))):
        t = tier(f"Tier{i}", r, h, 0.0)
        t.matrix_world = unity_matrix(at(md, 0, y, 0), (0, i * 20, 0))
        md.add(t, sway, "Trim", pine)
    models.append(md)

    # Bush with a few flowers.
    md = L.Model("Bush", (2 * SPACING, 0, 0))
    md.add(blob("Bush", [((0, 0.45, 0), 1.05), ((0.6, 0.4, 0.2), 0.82), ((-0.55, 0.38, -0.2), 0.78), ((0.1, 0.75, -0.1), 0.78)],
                at(md, 0, 0, 0), 0.1, target=240), "Body", "Trim", bushm)
    for i, (x, y, z) in enumerate(((-0.3, 0.95, 0.45), (0.45, 0.95, 0.4), (0.95, 0.65, 0.1), (-0.95, 0.6, 0.2), (0.15, 1.15, -0.3))):
        md.add(sphere("Flower", 0.13, at(md, x, y, z), (1, 0.7, 1), 8, 5), "Body", "Trim", pink if i % 2 == 0 else yellow)
    models.append(md)

    # Debris rock (bulldozer pushes it): big lumpy stone and a pebble.
    md = L.Model("DebrisRock", (3 * SPACING, 0, 0))
    md.add(rock("Rock", 0.75, at(md, 0, 0.45, 0), (1.2, 0.8, 1.0), 3, 0.35, euler=(10, 30, 5), seed=3), "Body", "Trim", stone)
    md.add(rock("Pebble", 0.35, at(md, 0.62, 0.22, 0.32), (1, 0.8, 1), 2, 0.3, seed=7), "Body", "Trim", stone)
    models.append(md)

    # Debris log: bark, ringed ends, a twig with leaves.
    md = L.Model("DebrisLog", (4 * SPACING, 0, 0))
    md.add(cyl("Log", 0.4, 2.0, at(md, 0, 0.4, 0), "x", 18, 0.06, 2), "Body", "Trim", bark)
    for s in (-1, 1):
        md.add(cyl("End", 0.34, 0.03, at(md, s * 1.005, 0.4, 0), "x", 18, 0.0), "Body", "Trim", log_end)
    md.add(cyl("Twig", 0.05, 0.5, at(md, 0.3, 0.86, 0.05), "y", 6, 0.0, euler=(0, 0, -25)), "Body", "Trim", bark)
    for x, z in ((0.42, 0.12), (0.5, -0.05)):
        md.add(sphere("Leaf", 0.15, at(md, x, 1.08, z), (1.3, 0.45, 0.8), 10, 6), "Body", "Trim", leaf)
    models.append(md)
    return models


# ================================================================================================= STREET
def street(m):
    paint = m.get("WhiteWood", rough=0.5, kind="wood", a="#E6DED0", b="#FFFFFF", scale=2.5)
    dark = m.get("LampMetal", "#46566A", rough=0.4)
    glow = m.get("LampGlow", "#FFF3B0", rough=0.1, emit=1.2)
    gold = m.get("Gold", "#FFC93C", rough=0.25)
    stripes = m.get("BarrierStripes", kind="stripes", a="#FF7A1A", b="#FFFFFF", scale=1.6, rough=0.35)
    amber = m.get("Amber", "#FFA62B", rough=0.15, emit=0.8)
    crate = m.get("CrateWood", rough=0.7, kind="wood", a="#C98B5A", b="#E2AE78", scale=3.0)
    batten = m.get("CrateBatten", rough=0.7, kind="wood", a="#8A5A3A", b="#A8714A", scale=3.0)
    cone = m.get("Cone", "#FF7A1A", rough=0.3)
    white = m.get("Reflective", "#FFFFFF", rough=0.2)
    sign_blue = m.get("SignBlue", "#1982C4", rough=0.4)
    post_wood = m.get("PostWood", rough=0.7, kind="wood", a="#B07A48", b="#D09A64", scale=3.0)
    models = []

    # Fence: three round posts with pointed caps, two rails (cheap: it repeats ~70 times).
    md = L.Model("Fence", (0 * SPACING, 0, 0))
    post = cyl("Post", 0.1, 1.1, (0, 0, 0), "y", 8, 0.02, 1)
    cap = cyl("Cap", 0.11, 0.2, (0, 0, 0), "y", 8, 0.0, radius2=0.015)
    for x in (-2.0, 0.0, 2.0):
        md.add(instance(post, "Post", unity_matrix(at(md, x, 0.55, 0))), "Body", "Trim", paint)
        md.add(instance(cap, "Cap", unity_matrix(at(md, x, 1.2, 0))), "Body", "Trim", paint)
    for o in (post, cap):
        L.bpy.data.objects.remove(o)
    for y in (0.85, 0.45):
        md.add(rbox("Rail", (4.2, 0.16, 0.07), at(md, 0, y, 0.0), 0.03, 1), "Body", "Trim", paint)
    models.append(md)

    # Street lamp: stepped base, tapered pole, glowing lantern with a cap and a golden finial.
    md = L.Model("Lamp", (1 * SPACING, 0, 0))
    md.add(lathe("Base", [(0, 0), (0.28, 0), (0.28, 0.12), (0.2, 0.18), (0.18, 0.45), (0.12, 0.55), (0, 0.55)], at(md, 0, 0, 0), "y", 14), "Body", "Trim", dark)
    md.add(cyl("Pole", 0.08, 3.0, at(md, 0, 2.0, 0), "y", 10, 0.0, radius2=0.065), "Body", "Trim", dark)
    md.add(lathe("Collar", [(0, 0), (0.12, 0), (0.18, 0.08), (0.18, 0.14), (0, 0.14)], at(md, 0, 3.45, 0), "y", 14), "Body", "Trim", dark)
    md.add(lathe("Lantern", [(0, 0), (0.18, 0.02), (0.27, 0.2), (0.28, 0.32), (0.22, 0.46), (0, 0.48)], at(md, 0, 3.55, 0), "y", 16), "Body", "Trim", glow)
    md.add(lathe("Cap", [(0, 0.22), (0.1, 0.2), (0.3, 0.04), (0.33, 0.0), (0, 0.0)], at(md, 0, 4.0, 0), "y", 16), "Body", "Trim", dark)
    md.add(sphere("Finial", 0.07, at(md, 0, 4.27, 0), segs=10, rings=6), "Body", "Trim", gold)
    models.append(md)

    # Road barrier: striped board on A-frame legs, amber lamp.
    md = L.Model("Barrier", (2 * SPACING, 0, 0))
    for x in (-1.35, 1.35):
        for s in (-1, 1):
            md.add(rbox("Leg", (0.1, 1.05, 0.1), at(md, x, 0.5, s * 0.16), 0.03, 1, euler=(s * 14, 0, 0)), "Body", "Trim", paint)
        md.add(rbox("Foot", (0.14, 0.08, 0.7), at(md, x, 0.04, 0), 0.03, 1), "Body", "Trim", dark)
    md.add(rbox("Board", (3.1, 0.42, 0.08), at(md, 0, 0.9, 0), 0.05, 2), "Body", "Trim", stripes)
    md.add(rbox("BoardLow", (3.1, 0.2, 0.06), at(md, 0, 0.42, 0), 0.03, 1), "Body", "Trim", stripes)
    md.add(cyl("LightBase", 0.07, 0.1, at(md, 1.35, 1.16, 0), "y", 10, 0.0), "Body", "Trim", dark)
    md.add(sphere("Light", 0.14, at(md, 1.35, 1.3, 0), (1, 0.85, 1), 14, 8), "Body", "Trim", amber)
    models.append(md)

    # Debris crate: planked core with darker edge battens.
    md = L.Model("DebrisCrate", (3 * SPACING, 0, 0))
    md.add(rbox("Core", (1.04, 0.96, 1.04), at(md, 0, 0.5, 0), 0.04, 1), "Body", "Trim", crate)
    h = 0.52
    for sx in (-1, 1):
        for sz in (-1, 1):
            md.add(rbox("BattenV", (0.12, 1.0, 0.12), at(md, sx * h, 0.5, sz * h), 0.03, 1), "Body", "Trim", batten)
    for y in (0.04, 0.96):
        for sx in (-1, 1):
            md.add(rbox("BattenZ", (0.1, 0.1, 1.1), at(md, sx * h, y, 0), 0.03, 1), "Body", "Trim", batten)
            md.add(rbox("BattenX", (1.1, 0.1, 0.1), at(md, 0, y, sx * h), 0.03, 1), "Body", "Trim", batten)
    for sz in (-1, 1):
        md.add(rbox("Brace", (1.25, 0.1, 0.06), at(md, 0, 0.5, sz * 0.54), 0.02, 1, euler=(0, 0, 42)), "Body", "Trim", batten)
    models.append(md)

    # Traffic cone: rounded tip, two reflective bands, square base.
    md = L.Model("DebrisCone", (4 * SPACING, 0, 0))
    md.add(lathe("Cone", [(0, 1.0), (0.05, 0.99), (0.08, 0.95), (0.36, 0.1), (0.38, 0.08), (0, 0.08)], at(md, 0, 0, 0), "y", 20), "Body", "Trim", cone)
    for y0, y1 in ((0.32, 0.44), (0.6, 0.7)):
        r0, r1 = 0.08 + (0.95 - y0) / 0.85 * 0.28 + 0.008, 0.08 + (0.95 - y1) / 0.85 * 0.28 + 0.008
        md.add(lathe("Band", [(r0 - 0.01, y0), (r0, y0), (r1, y1), (r1 - 0.01, y1)], at(md, 0, 0, 0), "y", 20), "Body", "Trim", white)
    md.add(rbox("Base", (0.92, 0.1, 0.92), at(md, 0, 0.05, 0), 0.06, 2), "Body", "Trim", cone)
    models.append(md)

    # Sign post (the picture quads are added per instance in Unity).
    md = L.Model("SignPost", (5 * SPACING, 0, 0))
    md.add(rbox("Post", (0.18, 2.6, 0.18), at(md, 0, 1.3, 0), 0.05, 2), "Body", "Trim", post_wood)
    md.add(rbox("Board", (2.0, 1.2, 0.13), at(md, 0, 2.6, 0), 0.12, 3), "Body", "Trim", white)
    md.add(rbox("Frame", (2.12, 1.32, 0.1), at(md, 0, 2.6, 0), 0.15, 3), "Body", "Trim", post_wood)
    md.add(rbox("Arrow", (0.6, 0.22, 0.16), at(md, 0.55, 2.6, 0), 0.06, 2), "Body", "Trim", sign_blue)
    md.add(rbox("ArrowHead", (0.32, 0.32, 0.16), at(md, 0.85, 2.6, 0), 0.06, 2, euler=(0, 0, 45)), "Body", "Trim", sign_blue)
    models.append(md)

    # Pothole, three stages (roadworks): jagged hole -> gravel fill -> fresh tarmac patch.
    hole = m.get("HoleInside", rough=0.9, kind="radial", a="#2A1F16", b="#7A5A3E", scale=2.6, hexstr="#8C6A4A")
    broken = m.get("BrokenRoad", rough=0.8, kind="rock", a="#7E838D", b="#9AA0AB", scale=5.0, hexstr="#8E949F")
    grit = m.get("Gravel", rough=0.9, kind="rock", a="#9A958B", b="#C2BCB0", scale=12.0, hexstr="#B0AA9E")
    fresh = m.get("FreshTarmac", rough=0.35, kind="rock", a="#33363E", b="#40444D", scale=14.0, hexstr="#3A3E47")
    seam = m.get("Seam", "#5E636D", rough=0.6)
    md = L.Model("PotholeHole", (6 * SPACING, 0, 0))
    md.add(patch("Inside", 2.3, at(md, 0, 0, 0), seed=4, y=0.055), "Body", "Trim", hole)
    md.add(patch("Rim", 0, at(md, 0, 0, 0), seed=4, ring=(2.25, 2.75), y=0.05, rim_h=0.05), "Body", "Trim", broken)
    for i in range(9):
        a = i / 9 * 2 * math.pi + 0.3
        md.add(rock(f"Chunk{i}", 0.32, at(md, math.cos(a) * 2.45 * 1.25, 0.07, math.sin(a) * 2.45), (1.2, 0.3, 0.8), 1, 0.4,
                    euler=(0, -math.degrees(a), 0), seed=10 + i), "Body", "Trim", broken)
    for i in range(6):
        a = i * 2.1 + 0.5
        md.add(rock(f"Pebble{i}", 0.12, at(md, math.cos(a) * 1.2, 0.08, math.sin(a) * 0.9), (1, 0.6, 1), 1, 0.3, seed=30 + i), "Body", "Trim", grit)
    models.append(md)
    md = L.Model("PotholeRough", (7 * SPACING, 0, 0))
    md.add(rock("Heap", 2.5, at(md, 0, 0.0, 0), (1.3, 0.16, 1.0), 3, 0.25, seed=5), "Body", "Trim", grit)
    for i in range(6):
        a = i * 1.15
        md.add(rock(f"Lump{i}", 0.3, at(md, math.cos(a) * 1.5, 0.28, math.sin(a) * 1.2), (1, 0.7, 1), 2, 0.4, seed=40 + i), "Body", "Trim", grit)
    models.append(md)
    md = L.Model("PotholeSmooth", (8 * SPACING, 0, 0))
    md.add(patch("Patch", 2.6, at(md, 0, 0, 0), seed=9, rough=0.06, y=0.055), "Body", "Trim", fresh)
    md.add(patch("Seam", 0, at(md, 0, 0, 0), seed=9, rough=0.06, ring=(2.55, 2.75), y=0.05, rim_h=0.01), "Body", "Trim", seam)
    models.append(md)
    return models


# ================================================================================================= BUILDINGS
def prism(name, width, height, depth, center):
    """Gable end: triangle (base depth along Z, apex height) extruded along X."""
    bm = bmesh.new()
    hw, hd = width * 0.5, depth * 0.5
    v = [bm.verts.new(Vector((x, z, y))) for x in (-hw, hw) for (z, y) in ((-hd, 0.0), (hd, 0.0), (0.0, height))]
    bm.faces.new((v[0], v[1], v[2]))
    bm.faces.new((v[5], v[4], v[3]))
    for a, b in ((0, 1), (1, 2), (2, 0)):
        bm.faces.new((v[a], v[a + 3], v[b + 3], v[b]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = L._link(name, bm)
    L._bake_modifiers(obj, 0.04, 1)
    return L._place(obj, center)


def placed(obj, pos, yaw):
    """Moves a part built around the origin to pos with a Unity yaw."""
    obj.matrix_world = unity_matrix(pos, (0, yaw, 0)) @ obj.matrix_world
    return obj


def window(md, k, pos, yaw, flowers, shutter=None, w=1.3, h=1.2):
    """White frame, glass with highlights, cross bars, optional shutters and a flower box (faces -Z before yaw)."""
    parts = [(rbox("Frame", (w + 0.16, h + 0.16, 0.1), (0, 0, 0), 0.05, 2), k["white"]),
             (rbox("Glass", (w - 0.06, h - 0.06, 0.06), (0, 0, -0.03), 0.03, 1), k["glass"]),
             (rbox("BarV", (0.07, h - 0.04, 0.1), (0, 0, -0.06), 0.02, 1), k["white"]),
             (rbox("BarH", (w - 0.04, 0.07, 0.1), (0, 0, -0.06), 0.02, 1), k["white"]),
             (rbox("Sill", (w + 0.36, 0.1, 0.24), (0, -h * 0.5 - 0.12, -0.1), 0.03, 1), k["white"])]
    if shutter is not None:
        for s in (-1, 1):
            parts.append((rbox("Shutter", (0.42, h + 0.1, 0.06), (s * (w * 0.5 + 0.32), 0, -0.04), 0.03, 1), shutter))
    if flowers:
        parts.append((rbox("FlowerBox", (w + 0.1, 0.26, 0.3), (0, -h * 0.5 - 0.3, -0.2), 0.05, 2), k["box"]))
        for i in range(4):
            parts.append((sphere("Flower", 0.12, (-0.45 + i * 0.3, -h * 0.5 - 0.1, -0.22), (1, 0.8, 1), 8, 5),
                          (k["pink"], k["yellow"], k["red"], k["pink"])[i]))
        parts.append((rbox("Leaves", (w, 0.14, 0.24), (0, -h * 0.5 - 0.16, -0.2), 0.06, 2), k["leaves"]))
    for obj, mat in parts:
        md.add(placed(obj, pos, yaw), "Body", "Trim", mat)


def buildings(m):
    k = {
        "white": m.get("TrimWhite", "#FFFFFF", rough=0.4),
        "glass": m.get("Glass", kind="glass", rough=0.08),
        "box": m.get("BoxWood", rough=0.7, kind="wood", a="#A8714A", b="#C98B5A", scale=3.0),
        "pink": m.get("FlowerPink", "#FF8FC7", rough=0.5),
        "yellow": m.get("FlowerYellow", "#FFE066", rough=0.5),
        "red": m.get("FlowerRed", "#FF595E", rough=0.5),
        "leaves": m.get("BoxLeaves", "#5DB565", rough=0.8),
        "door": m.get("DoorWood", rough=0.6, kind="wood", a="#B07A48", b="#D09A64", scale=2.5),
        "gold": m.get("Gold", "#FFC93C", rough=0.25),
        "stone": m.get("Stone", rough=0.8, kind="rock", a="#A9A39A", b="#CFC8BD", scale=4.0, hexstr="#CFC8BD"),
        "brick": m.get("Brick", rough=0.8, kind="bricks", a="#D9654B", b="#C2533E", scale=2.0),
        "steel": m.get("Steel", "#A7B1BC", rough=0.35),
        "dark": m.get("DarkSteel", "#596270", rough=0.45),
        "darkwood": m.get("DarkWood", rough=0.7, kind="wood", a="#7A4E32", b="#9A6A44", scale=3.0),
    }
    models = []

    houses = (("TownHouseA", "#FFE3B3", "#E63946"), ("TownHouseB", "#BDE0FE", "#457B9D"),
              ("TownHouseC", "#FFC8DD", "#8E7DBE"), ("TownHouseD", "#CAFFBF", "#F4A261"))
    for i, (name, wall, roof) in enumerate(houses):
        md = L.Model(name, (i * SPACING, 0, 0))
        wm = m.get("Wall" + name, wall, rough=0.8, kind="siding", a=L_shade(wall, 0.9), b=wall, scale=2.6)
        rm = m.get("Roof" + name, roof, rough=0.6, kind="shingles", a=L_shade(roof, 0.72), b=roof, scale=2.2)
        o = lambda x, y, z: at(md, x, y, z)  # noqa: E731
        md.add(rbox("Walls", (7, 4, 6), o(0, 2, 0), 0.12, 2), "Body", "Trim", wm)
        md.add(rbox("Plinth", (7.12, 0.45, 6.12), o(0, 0.22, 0), 0.06, 2), "Body", "Trim", k["stone"])
        for sx in (-1, 1):
            for sz in (-1, 1):
                md.add(rbox("Corner", (0.22, 4.0, 0.22), o(sx * 3.47, 2.0, sz * 2.97), 0.05, 1), "Body", "Trim", k["white"])
        md.add(prism("Gable", 6.9, 2.05, 5.9, o(0, 3.98, 0)), "Body", "Trim", wm)
        md.add(rbox("RoofL", (7.7, 0.32, 4.45), o(0, 5.12, -1.5), 0.1, 3, euler=(-35, 0, 0)), "Body", "Trim", rm)
        md.add(rbox("RoofR", (7.7, 0.32, 4.45), o(0, 5.12, 1.5), 0.1, 3, euler=(35, 0, 0)), "Body", "Trim", rm)
        md.add(cyl("Ridge", 0.17, 7.8, o(0, 6.35, 0), "x", 12, 0.04), "Body", "Trim", rm)
        md.add(rbox("Chimney", (0.75, 1.8, 0.75), o(2.2, 6.0, 1.2), 0.05, 2), "Body", "Trim", k["brick"])
        md.add(rbox("ChimneyCap", (0.92, 0.16, 0.92), o(2.2, 6.95, 1.2), 0.05, 2), "Body", "Trim", k["stone"])
        # Front door with a little canopy and step.
        md.add(rbox("DoorFrame", (1.6, 2.45, 0.1), o(0, 1.22, -3.02), 0.06, 2), "Body", "Trim", k["white"])
        md.add(rbox("Door", (1.3, 2.2, 0.1), o(0, 1.1, -3.07), 0.06, 2), "Body", "Trim", k["door"])
        for y in (0.6, 1.55):
            md.add(rbox("Panel", (0.9, 0.62, 0.04), o(0, y, -3.13), 0.04, 1), "Body", "Trim", k["door"])
        md.add(sphere("Knob", 0.08, o(0.42, 1.1, -3.16), segs=10, rings=6), "Body", "Trim", k["gold"])
        md.add(rbox("Canopy", (2.0, 0.14, 0.8), o(0, 2.62, -3.35), 0.05, 2, euler=(-12, 0, 0)), "Body", "Trim", rm)
        md.add(rbox("Step", (1.9, 0.2, 0.7), o(0, 0.1, -3.35), 0.06, 2), "Body", "Trim", k["stone"])
        for x in (-2.3, 2.3):
            window(md, k, o(x, 2.4, -3.04), 0, True, rm)
        for s in (-1, 1):
            window(md, k, o(s * 3.54, 2.4, 0), -s * 90, False)
        window(md, k, o(0, 4.9, -3.0), 0, False, None, 0.8, 0.8)
        models.append(md)

    # Shed (yard): plank walls, corrugated roof, Z-braced door, window, hard-hat sign.
    md = L.Model("Shed", (4 * SPACING, 0, 0))
    o = lambda x, y, z: at(md, x, y, z)  # noqa: E731
    planks = m.get("ShedPlanks", "#FFE3B3", rough=0.8, kind="siding", a="#F0CF98", b="#FFE7BD", scale=2.0)
    tin = m.get("ShedRoof", "#1982C4", rough=0.4, kind="shingles", a="#136AA3", b="#2894DA", scale=6.0)
    sign = m.get("SignYellow", "#FFC93C", rough=0.4)
    hat = m.get("HatOrange", "#FF9F1C", rough=0.3)
    md.add(rbox("Walls", (6, 3.2, 4.5), o(0, 1.6, 0), 0.1, 2), "Body", "Trim", planks)
    md.add(rbox("Roof", (6.7, 0.25, 5.3), o(0, 3.42, 0.05), 0.08, 2, euler=(6, 0, 0)), "Body", "Trim", tin)
    for sx in (-1, 1):
        for sz in (-1, 1):
            md.add(rbox("Corner", (0.2, 3.2, 0.2), o(sx * 2.98, 1.6, sz * 2.23), 0.05, 1), "Body", "Trim", k["white"])
        md.add(cyl("Gutter", 0.07, 3.1, o(sx * 3.1, 1.6, -2.35), "y", 8, 0.0), "Body", "Trim", tin)
    md.add(rbox("DoorFrame", (1.5, 2.35, 0.08), o(-1.2, 1.17, -2.27), 0.05, 2), "Body", "Trim", k["white"])
    md.add(rbox("Door", (1.3, 2.2, 0.1), o(-1.2, 1.1, -2.31), 0.05, 2), "Body", "Trim", k["door"])
    md.add(rbox("Brace", (0.14, 2.3, 0.05), o(-1.2, 1.1, -2.37), 0.02, 1, euler=(0, 0, 28)), "Body", "Trim", k["darkwood"])
    for y in (0.35, 1.85):
        md.add(rbox("Rail", (1.25, 0.14, 0.05), o(-1.2, y, -2.37), 0.02, 1), "Body", "Trim", k["darkwood"])
    md.add(sphere("Knob", 0.08, o(-0.72, 1.1, -2.4), segs=10, rings=6), "Body", "Trim", k["gold"])
    window(md, k, o(1.3, 1.9, -2.28), 0, True, None, 1.3, 0.95)
    md.add(rbox("Sign", (2.6, 0.7, 0.1), o(0, 2.85, -2.32), 0.1, 3), "Body", "Trim", sign)
    md.add(lathe("SignHat", [(0, 0.3), (0.2, 0.27), (0.36, 0.12), (0.4, 0.0), (0.55, 0.0), (0.55, -0.04), (0, -0.04)],
                 o(0, 2.75, -2.38), "z", 18, euler=(0, 180, 0)), "Body", "Trim", hat)
    md.add(rbox("Bench", (1.4, 0.1, 0.45), o(1.5, 0.5, -2.6), 0.03, 1), "Body", "Trim", k["darkwood"])
    models.append(md)

    # Water tower: tapered legs with X-braces, rounded tank, white band, red cap, ladder, water-drop emblem.
    md = L.Model("WaterTower", (5 * SPACING, 0, 0))
    o = lambda x, y, z: at(md, x, y, z)  # noqa: E731
    tank = m.get("TowerTank", "#8AD8FF", rough=0.3, kind="hbands", a="#7FD0FA", b="#8AD8FF", scale=3.0)
    cap = m.get("TowerCap", "#FF595E", rough=0.35)
    for i in range(4):
        a = math.radians(45 + i * 90)
        x, z = math.cos(a) * 2.0, math.sin(a) * 2.0
        lean = 4.0
        md.add(cyl("Leg", 0.15, 7.2, o(x * 0.95, 3.6, z * 0.95), "y", 10, 0.03, euler=(math.sin(a) * -lean, 0, math.cos(a) * lean)), "Body", "Trim", k["dark"])
        md.add(cyl("Foot", 0.3, 0.2, o(x * 1.05, 0.1, z * 1.05), "y", 12, 0.04), "Body", "Trim", k["stone"])
    for i in range(4):
        a0, a1 = math.radians(45 + i * 90), math.radians(135 + i * 90)
        for y0, y1 in ((0.6, 3.4), (3.4, 6.2)):
            r0, r1 = 2.0 * (1.0 - 0.03 * y0 / 3), 2.0 * (1.0 - 0.03 * y1 / 3)
            p0 = Vector((math.cos(a0) * r0, y0, math.sin(a0) * r0))
            p1 = Vector((math.cos(a1) * r1, y1, math.sin(a1) * r1))
            for pa, pb in ((p0, p1), (Vector((p1.x, y0, p1.z)) * 1.0, Vector((p0.x, y1, p0.z)))):
                d = pb - pa
                mid = (pa + pb) * 0.5
                yaw = -math.degrees(math.atan2(d.z, d.x))
                pitch = math.degrees(math.atan2(d.y, math.hypot(d.x, d.z)))
                md.add(cyl("Brace", 0.05, d.length, o(mid.x, mid.y, mid.z), "x", 6, 0.0, euler=(0, yaw, pitch)), "Body", "Trim", k["steel"])
    md.add(lathe("Tank", [(0, 6.6), (1.6, 6.65), (2.45, 6.95), (2.6, 7.4), (2.6, 9.6), (0, 9.6)], o(0, 0, 0), "y", 36), "Body", "Trim", tank)
    md.add(lathe("Band", [(2.58, 8.1), (2.68, 8.15), (2.68, 8.55), (2.58, 8.6)], o(0, 0, 0), "y", 36), "Body", "Trim", k["white"])
    md.add(lathe("Cap", [(0, 11.0), (0.25, 10.97), (1.4, 10.6), (2.5, 9.85), (2.8, 9.6), (2.8, 9.5), (0, 9.5)], o(0, 0, 0), "y", 36), "Body", "Trim", cap)
    md.add(sphere("Finial", 0.18, o(0, 11.1, 0), segs=12, rings=8), "Body", "Trim", k["gold"])
    md.add(sphere("Drop", 0.5, o(0, 8.95, -2.62), (0.78, 1.0, 0.3), 16, 10), "Body", "Trim", k["white"])
    md.add(lathe("DropTip", [(0, 0.45), (0.12, 0.25), (0.28, 0.0), (0, 0.0)], o(0, 9.25, -2.64), "y", 12), "Body", "Trim", k["white"])
    for s in (-1, 1):
        md.add(cyl("LadderRail", 0.04, 6.8, o(s * 0.28, 3.4, -2.15), "y", 6, 0.0), "Body", "Trim", k["steel"])
    for i in range(14):
        md.add(cyl("Rung", 0.03, 0.56, o(0, 0.4 + i * 0.45, -2.15), "x", 6, 0.0), "Body", "Trim", k["steel"])
    models.append(md)

    # Windmill: tapered white tower on a stone base, red cap, balcony with railing, windows, door; sails spin ("Blades").
    md = L.Model("Windmill", (6 * SPACING, 0, 0))
    o = lambda x, y, z: at(md, x, y, z)  # noqa: E731
    blades = md.node("Blades", "Body", (0, 9.8, -1.7))
    plaster = m.get("MillWall", "#FFF1E6", rough=0.8, kind="siding", a="#F2E2D2", b="#FFF5EC", scale=0.9)
    red = m.get("MillCap", "#E63946", rough=0.5, kind="shingles", a="#C92F3B", b="#E63946", scale=3.0)
    canvas = m.get("Sail", "#FF595E", rough=0.6)
    canvas2 = m.get("SailWhite", "#FFFFFF", rough=0.6)
    md.add(lathe("Tower", [(0, 0), (3.25, 0), (3.2, 0.1), (2.35, 6.0), (1.75, 9.6), (0, 9.6)], o(0, 0, 0), "y", 32), "Body", "Trim", plaster)
    md.add(lathe("Base", [(0, 0), (3.45, 0), (3.45, 0.75), (3.25, 0.85), (0, 0.85)], o(0, 0, 0), "y", 32), "Body", "Trim", k["stone"])
    md.add(lathe("Cap", [(0, 11.4), (0.4, 11.35), (1.3, 10.9), (1.95, 10.1), (2.05, 9.6), (2.05, 9.45), (0, 9.45)], o(0, 0, 0), "y", 28), "Body", "Trim", red)
    md.add(sphere("Finial", 0.16, o(0, 11.5, 0), segs=10, rings=6), "Body", "Trim", k["gold"])
    md.add(lathe("Balcony", [(0, 8.0), (2.5, 8.0), (2.55, 8.06), (2.55, 8.14), (2.5, 8.2), (0, 8.2)], o(0, 0, 0), "y", 32), "Body", "Trim", k["darkwood"])
    md.add(lathe("Rail", [(2.43, 8.75), (2.5, 8.75), (2.5, 8.85), (2.43, 8.85), (2.43, 8.75)], o(0, 0, 0), "y", 32), "Body", "Trim", k["darkwood"])
    for i in range(12):
        a = 2 * math.pi * i / 12
        md.add(cyl("Baluster", 0.035, 0.6, o(math.cos(a) * 2.46, 8.5, math.sin(a) * 2.46), "y", 6, 0.0), "Body", "Trim", k["darkwood"])
    md.add(rbox("Door", (1.2, 2.1, 0.2), o(0, 1.05, -3.12), 0.1, 3, top=(0.75, 1.0)), "Body", "Trim", k["door"])
    for y in (4.2, 7.0):
        rad = 3.2 - (3.2 - 2.35) * min(1.0, y / 6.0) if y <= 6 else 2.35 - 0.6 * (y - 6.0) / 3.6
        window(md, k, o(0, y, -rad - 0.02), 0, False, None, 0.65, 0.8)
    B = lambda x, y, z: md.at(blades, x, y, z)  # noqa: E731
    md.add(sphere("Hub", 0.45, B(0, 0, 0), segs=16, rings=10), blades, "Trim", k["darkwood"])
    md.add(cyl("Axle", 0.18, 1.1, B(0, 0, 0.55), "z", 12, 0.03), blades, "Trim", k["darkwood"])
    for i in range(4):
        q = i * 90
        spar = rbox("Spar", (0.2, 5.4, 0.16), (0, 2.9, 0), 0.05, 2)
        spar.matrix_world = unity_matrix(B(0, 0, 0), (0, 0, q)) @ spar.matrix_world
        md.add(spar, blades, "Trim", k["darkwood"])
        sail = rbox("Sail", (1.0, 3.7, 0.06), (0.62, 3.4, 0.06), 0.04, 1)
        sail.matrix_world = unity_matrix(B(0, 0, 0), (0, 0, q)) @ sail.matrix_world
        md.add(sail, blades, "Trim", canvas if i % 2 == 0 else canvas2)
        for j in range(4):
            lat = rbox("Lath", (1.1, 0.06, 0.08), (0.62, 1.8 + j * 1.05, 0.1), 0.02, 1)
            lat.matrix_world = unity_matrix(B(0, 0, 0), (0, 0, q)) @ lat.matrix_world
            md.add(lat, blades, "Trim", k["darkwood"])
    models.append(md)
    return models


# ================================================================================================= shared bits
def rod(name, r, p0, p1, segs=10):
    """Round bar between two Unity points (no rotation maths: legs, rails, ropes, rungs)."""
    d = Vector(p1) - Vector(p0)
    ax = min(((1, 0, 0), (0, 1, 0), (0, 0, 1)), key=lambda a: abs(d.normalized().dot(Vector(a))))
    return sweep(name, [p0, p1], circle(r, segs), ref=ax)


def squash(obj, sx=1.0, sy=1.0, sz=1.0):
    """Scales a placed part about its own origin along Unity axes."""
    obj.matrix_world = obj.matrix_world @ Matrix.Diagonal((sx, sz, sy, 1.0))
    return obj


def rrect(w, h, r, n=3):
    """Rounded rectangle profile (closed), w along a, h along b."""
    pts = []
    for cx, cy, a0 in ((w / 2 - r, h / 2 - r, 0), (-w / 2 + r, h / 2 - r, 90), (-w / 2 + r, -h / 2 + r, 180), (w / 2 - r, -h / 2 + r, 270)):
        for i in range(n + 1):
            a = math.radians(a0 + 90 * i / n)
            pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return pts


def pennant(name, origin, length, height, wave=0.05, thick=0.012):
    """Waving triangular flag pointing +X from `origin` (its pole edge)."""
    ox, oy, oz = origin
    path = [(ox + length * t, oy, oz + wave * math.sin(t * 7.0)) for t in (i / 8 for i in range(9))]
    return sweep(name, path, rrect(thick * 2, height, thick * 0.9, 1), ref=(0, 0, 1), taper=lambda t: (1.0, max(0.06, 1.0 - t)))


def flower_disc(name, radius, center, lobes=5, segs=20, cup=0.03, thick=0.02):
    """Flat flower head: lobed petals (top + bottom fan), slightly cupped."""
    bm = bmesh.new()
    top_c = bm.verts.new(Vector((0, 0, thick)))
    bot_c = bm.verts.new(Vector((0, 0, 0)))
    top, bot = [], []
    for i in range(segs):
        a = 2 * math.pi * i / segs
        r = radius * (0.5 + 0.5 * abs(math.cos(lobes * a / 2)))
        z = cup * (r / radius) ** 2
        top.append(bm.verts.new(Vector((math.cos(a) * r, math.sin(a) * r, z + thick))))
        bot.append(bm.verts.new(Vector((math.cos(a) * r, math.sin(a) * r, z))))
    for i in range(segs):
        j = (i + 1) % segs
        bm.faces.new((top_c, top[i], top[j]))
        bm.faces.new((bot_c, bot[j], bot[i]))
        bm.faces.new((top[i], bot[i], bot[j], top[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = L._link(name, bm)
    L._bake_modifiers(obj, None, weighted=False)
    return L._place(obj, center)


def yaw_pt(x, z, yaw):
    a = math.radians(yaw)
    return x * math.cos(a) + z * math.sin(a), -x * math.sin(a) + z * math.cos(a)


# ================================================================================================= PLAY (playground)
def play(m):
    k = {
        "wood": m.get("PlayWood", rough=0.7, kind="wood", a="#C98B5A", b="#E2AE78", scale=3.0),
        "blue": m.get("PlayBlue", "#1982C4", rough=0.35),
        "red": m.get("PlayRed", "#FF595E", rough=0.35),
        "yellow": m.get("PlayYellow", "#FFC93C", rough=0.35),
        "green": m.get("PlayGreen", "#6BCB77", rough=0.35),
        "purple": m.get("PlayPurple", "#8E7DBE", rough=0.35),
        "orange": m.get("PlayOrange", "#FF9F1C", rough=0.35),
        "steel": m.get("PlaySteel", "#B8C2CC", rough=0.3),
        "dark": m.get("PlayDark", "#596270", rough=0.45),
        "rubber": m.get("PlayRubber", "#3A3945", rough=0.7),
        "sand": m.get("PlaySand", "#F6D98B", rough=0.95, kind="plain"),
        "sand2": m.get("PlaySandDark", "#E9C46A", rough=0.95, kind="plain"),
        "white": m.get("PlayWhite", "#FFFFFF", rough=0.4),
        "slide": m.get("SlideBed", "#FF595E", rough=0.15, kind="hbands", a="#F04A50", b="#FF6A6F", scale=1.2),
    }
    models = []

    # Sandpit: log-edged box, corner seats, lumpy sand, a castle with a flag, bucket and spade.
    md = L.Model("Sandpit", (0 * SPACING, 0, 0))
    o = lambda x, y, z: at(md, x, y, z)  # noqa: E731
    bucket = md.node("Bucket", "Body", (1.1, 0.25, -0.6))
    for pos, size in (((0, 0.2, 2.0), (5.2, 0.4, 0.3)), ((0, 0.2, -2.0), (5.2, 0.4, 0.3)), ((2.5, 0.2, 0), (0.3, 0.4, 4.3)), ((-2.5, 0.2, 0), (0.3, 0.4, 4.3))):
        md.add(rbox("Board", size, o(*pos), 0.1, 3), "Body", "Trim", k["wood"])
    for sx in (-1, 1):
        for sz in (-1, 1):
            md.add(rbox("Seat", (0.75, 0.08, 0.75), o(sx * 2.32, 0.43, sz * 1.82), 0.03, 2, euler=(0, 45, 0)), "Body", "Trim", k["wood"])
    md.add(rbox("Sand", (4.8, 0.24, 3.8), o(0, 0.13, 0), 0.08, 2), "Body", "Trim", k["sand"])
    for i, (x, z, r) in enumerate(((-1.4, -1.0, 0.7), (1.6, 0.9, 0.6), (0.2, 1.3, 0.45))):
        md.add(rock(f"Heap{i}", r, o(x, 0.24, z), (1.3, 0.28, 1.0), 2, 0.2, seed=60 + i), "Body", "Trim", k["sand2"])
    md.add(cyl("Tower", 0.3, 0.45, o(-1.0, 0.45, 0.4), "y", 16, 0.03), "Body", "Trim", k["sand2"])
    md.add(cyl("Tower2", 0.2, 0.3, o(-0.45, 0.38, 0.55), "y", 14, 0.03), "Body", "Trim", k["sand2"])
    for i in range(4):
        a = math.radians(45 + i * 90)
        md.add(rbox("Merlon", (0.13, 0.12, 0.13), o(-1.0 + math.cos(a) * 0.22, 0.73, 0.4 + math.sin(a) * 0.22), 0.02, 1), "Body", "Trim", k["sand2"])
    md.add(rod("FlagStick", 0.015, o(-1.0, 0.65, 0.4), o(-1.0, 1.1, 0.4), 6), "Body", "Trim", k["white"])
    md.add(pennant("Flag", o(-1.0, 1.0, 0.4), 0.28, 0.18, 0.02), "Body", "Trim", k["red"])
    B = lambda x, y, z: md.at(bucket, x, y, z)  # noqa: E731
    md.add(lathe("Pail", [(0, 0), (0.2, 0), (0.26, 0.38), (0.24, 0.38), (0.18, 0.04), (0, 0.04)], B(0, -0.13, 0), "y", 18), bucket, "Trim", k["red"])
    md.add(tube_arc("Handle", 0.25, 0.015, B(0, 0.25, 0), 0, 180, 14, 6), bucket, "Trim", k["yellow"])
    md.add(rbox("SpadeBlade", (0.22, 0.03, 0.26), o(0.35, 0.29, -1.15), 0.03, 2, euler=(0, 30, 0)), "Body", "Trim", k["blue"])
    md.add(rod("SpadeHandle", 0.025, o(0.42, 0.29, -1.02), o(0.6, 0.3, -0.7), 8), "Body", "Trim", k["yellow"])
    models.append(md)

    # Slide: tower with a pointed roof, curved chute to +X, ladder up the -X side.
    md = L.Model("Slide", (1 * SPACING, 0, 0))
    o = lambda x, y, z: at(md, x, y, z)  # noqa: E731
    for x in (-0.7, 0.7):
        for z in (-0.7, 0.7):
            md.add(cyl("Post", 0.08, 2.75, o(x, 1.375, z), "y", 12, 0.03), "Body", "Trim", k["blue"])
            md.add(sphere("Cap", 0.1, o(x, 2.78, z), segs=12, rings=8), "Body", "Trim", k["yellow"])
    md.add(rbox("Deck", (1.7, 0.14, 1.7), o(0, 2.0, 0), 0.04, 2), "Body", "Trim", k["wood"])
    for s in (-1, 1):
        md.add(rbox("Panel", (1.3, 0.5, 0.05), o(0, 2.35, s * 0.72), 0.04, 2), "Body", "Trim", k["yellow"])
        md.add(tube_arc("Porthole", 0.13, 0.025, o(0, 2.37, s * 0.75), 0, 360, 18, 6), "Body", "Trim", k["blue"])
        md.add(rod("Rail", 0.04, o(-0.7, 2.62, s * 0.7), o(0.7, 2.62, s * 0.7)), "Body", "Trim", k["blue"])
    md.add(lathe("Roof", [(0, 1.0), (0.12, 0.96), (0.95, 0.16), (1.12, 0.02), (1.1, -0.04), (0, -0.04)], o(0, 2.8, 0), "y", 24), "Body", "Trim", k["red"])
    md.add(sphere("Finial", 0.11, o(0, 3.85, 0), segs=12, rings=8), "Body", "Trim", k["yellow"])

    def chute_y(t):
        e = t * t * (3 - 2 * t)
        return 0.32 + 1.75 * (1 - e)
    path = [o(0.8 + 4.1 * t, chute_y(t), 0) for t in (i / 24 for i in range(25))]
    bed = [(-0.42, 0.32), (-0.42, 0.1), (-0.39, 0.03), (-0.32, 0.0), (0.32, 0.0), (0.39, 0.03), (0.42, 0.1), (0.42, 0.32),
           (0.48, 0.32), (0.48, 0.06), (0.43, -0.03), (0.35, -0.06), (-0.35, -0.06), (-0.43, -0.03), (-0.48, 0.06), (-0.48, 0.32)]
    md.add(sweep("Chute", path, bed, ref=(0, 0, 1)), "Body", "Trim", k["slide"])
    for s in (-1, 1):
        md.add(sweep("ChuteRail", path, circle(0.05, 10, s * 0.45, 0.34), ref=(0, 0, 1)), "Body", "Trim", k["yellow"])
    for x, t in ((2.9, 0.512), (4.5, 0.9)):
        y = chute_y(t) - 0.06
        for s in (-1, 1):
            md.add(rod("Leg", 0.05, o(x, 0, s * 0.38), o(x, y, s * 0.38), 8), "Body", "Trim", k["blue"])
    for s in (-1, 1):
        md.add(rod("LadderRail", 0.045, o(-1.72, 0, s * 0.42), o(-0.82, 2.3, s * 0.42), 10), "Body", "Trim", k["blue"])
    for i in range(5):
        t = (i + 0.6) / 5.6
        x, y = -1.72 + 0.9 * t, 2.3 * t
        md.add(rod("Rung", 0.035, o(x, y, -0.42), o(x, y, 0.42), 8), "Body", "Trim", k["yellow"])
    models.append(md)

    # Swings: green A-frame, two swings with ropes and curved rubber seats ("SwingPivot0/1" sway).
    md = L.Model("Swings", (2 * SPACING, 0, 0))
    o = lambda x, y, z: at(md, x, y, z)  # noqa: E731
    md.add(rod("TopBar", 0.085, o(-2.6, 2.72, 0), o(2.6, 2.72, 0), 14), "Body", "Trim", k["green"])
    for x in (-2.6, 2.6):
        md.add(sphere("End", 0.11, o(x, 2.72, 0), segs=12, rings=8), "Body", "Trim", k["yellow"])
    for x in (-2.45, 2.45):
        for s in (-1, 1):
            md.add(rod("Leg", 0.07, o(x, 2.72, 0), o(x, 0.02, s * 0.95), 12), "Body", "Trim", k["green"])
            md.add(cyl("Foot", 0.14, 0.08, o(x, 0.04, s * 0.95), "y", 12, 0.02), "Body", "Trim", k["dark"])
        md.add(rod("Brace", 0.04, o(x, 0.9, -0.64), o(x, 0.9, 0.64), 8), "Body", "Trim", k["green"])
    seats = (k["red"], k["yellow"])
    for i in range(2):
        pv = md.node(f"SwingPivot{i}", "Body", (-1.1 + i * 2.2, 2.62, 0))
        P = lambda x, y, z: md.at(pv, x, y, z)  # noqa: E731
        for x in (-0.35, 0.35):
            md.add(tube_arc("Hook", 0.1, 0.02, P(x, 0.1, 0), -40, 220, 12, 6, euler=(0, 90, 0)), pv, "Trim", k["steel"])
            md.add(rod("Rope", 0.022, P(x, 0.02, 0), P(x, -1.88, 0), 6), pv, "Trim", k["steel"])
        seat_path = [P(-0.45 + 0.9 * t, -1.88 - 0.07 * (1 - (2 * t - 1) ** 2), 0) for t in (j / 10 for j in range(11))]
        md.add(sweep("Seat", seat_path, rrect(0.4, 0.07, 0.03, 2), ref=(0, 0, 1)), pv, "Trim", seats[i])
    models.append(md)

    # Climbing frame: purple posts with ball caps, three levels of bars, wooden deck with a top rail, waving flag.
    md = L.Model("ClimbingFrame", (3 * SPACING, 0, 0))
    o = lambda x, y, z: at(md, x, y, z)  # noqa: E731
    flag = md.node("Flag", "Body", (1.1, 3.45, 1.1))
    for x in (-1.2, 1.2):
        for z in (-1.2, 1.2):
            md.add(cyl("Post", 0.09, 3.1, o(x, 1.55, z), "y", 12, 0.03), "Body", "Trim", k["purple"])
            md.add(sphere("Cap", 0.13, o(x, 3.12, z), segs=12, rings=8), "Body", "Trim", k["yellow"])
    bars = (k["red"], k["yellow"], k["blue"])
    for level, y in enumerate((0.6, 1.3, 2.0)):
        for s in (-1.2, 1.2):
            md.add(rod("BarX", 0.05, o(-1.2, y, s), o(1.2, y, s), 10), "Body", "Trim", bars[level])
            md.add(rod("BarZ", 0.05, o(s, y, -1.2), o(s, y, 1.2), 10), "Body", "Trim", bars[level])
    md.add(rbox("Deck", (2.5, 0.14, 2.5), o(0, 2.45, 0), 0.04, 2), "Body", "Trim", k["wood"])
    for s in (-1.2, 1.2):
        md.add(rod("TopRail", 0.045, o(-1.2, 2.95, s), o(1.2, 2.95, s), 10), "Body", "Trim", k["green"])
    md.add(rod("TopRailZ", 0.045, o(-1.2, 2.95, -1.2), o(-1.2, 2.95, 1.2), 10), "Body", "Trim", k["green"])
    md.add(rod("FlagPole", 0.03, o(1.1, 2.5, 1.1), o(1.1, 3.75, 1.1), 8), "Body", "Trim", k["white"])
    md.add(sphere("PoleTop", 0.06, o(1.1, 3.78, 1.1), segs=10, rings=6), "Body", "Trim", k["yellow"])
    md.add(pennant("Cloth", md.at(flag, 0.03, 0.0, 0), 0.7, 0.5, 0.05), flag, "Trim", k["red"])
    models.append(md)

    # Roundabout: four coloured quarters, a hub and four curved handle bars ("Spin" turns).
    md = L.Model("Roundabout", (4 * SPACING, 0, 0))
    o = lambda x, y, z: at(md, x, y, z)  # noqa: E731
    spin = md.node("Spin", "Body", (0, 0.3, 0))
    S = lambda x, y, z: md.at(spin, x, y, z)  # noqa: E731
    md.add(lathe("Base", [(0, 0), (0.5, 0), (0.46, 0.08), (0.26, 0.14), (0.22, 0.3), (0, 0.3)], o(0, 0, 0), "y", 18), "Body", "Trim", k["dark"])
    quarters = (k["yellow"], k["red"], k["blue"], k["green"])
    for q in range(4):
        md.add(lathe("Quarter", [(0, 0), (1.55, 0), (1.62, 0.05), (1.62, 0.13), (1.55, 0.18), (0, 0.18)], S(0, 0, 0), "y", 48,
                     angle=90.0, start=q * 90.0), spin, "Trim", quarters[q])
    md.add(lathe("Hub", [(0, 0.18), (0.42, 0.18), (0.36, 0.28), (0, 0.3)], S(0, 0, 0), "y", 20), spin, "Trim", k["orange"])
    md.add(rod("Pole", 0.06, S(0, 0.25, 0), S(0, 1.02, 0), 10), spin, "Trim", k["steel"])
    md.add(sphere("PoleTop", 0.1, S(0, 1.05, 0), segs=12, rings=8), spin, "Trim", k["red"])
    for q in range(4):
        a = math.radians(q * 90 + 45)
        ca, sa = math.cos(a), math.sin(a)
        pts = []
        for i in range(11):
            t = i / 10
            d = 0.1 + 1.2 * t
            pts.append(S(ca * d, 0.95 - 0.78 * t ** 1.6, sa * d))
        md.add(sweep("Handle", pts, circle(0.04, 8), ref=(-sa, 0, ca)), spin, "Trim", k["steel"])
    models.append(md)
    return models


# ================================================================================================= GARDEN
def garden(m):
    k = {
        "stone": m.get("GardenStone", rough=0.8, kind="rock", a="#B5AEA4", b="#D6CFC4", scale=4.0, hexstr="#D6CFC4"),
        "water": m.get("Water", "#5FC9F8", rough=0.08, kind="rings", a="#4DB8EC", b="#7DD6FF", scale=3.0),
        "jet": m.get("WaterJet", "#BDEBFF", rough=0.05, kind="plain"),
        "white": m.get("Foam", "#FFFFFF", rough=0.3),
        "wood": m.get("BenchWood", rough=0.7, kind="wood", a="#B07A48", b="#D09A64", scale=3.0),
        "iron": m.get("BenchIron", "#2E6B4F", rough=0.4),
        "pebble": m.get("PondStone", "#9DC46A", rough=0.8, kind="rock", a="#8B8984", b="#B9B5AE", scale=3.0),
        "bank": m.get("PondBank", "#C9A877", rough=0.9, kind="plain"),
        "pad": m.get("LilyPad", "#4CB050", rough=0.4, kind="plain"),
        "lily": m.get("Lily", "#FF8FC7", rough=0.5),
        "yellow": m.get("Yellow", "#FFE066", rough=0.5),
        "reed": m.get("Reed", "#5DAA4A", rough=0.7),
        "cattail": m.get("Cattail", "#7A4E32", rough=0.8),
        "duck": m.get("Duck", "#FFD447", rough=0.4),
        "beak": m.get("Beak", "#FF9F1C", rough=0.35),
        "ink": m.get("Ink", "#2B2D42", rough=0.2),
        "grass": m.get("Tuft", "#7CCB5E", rough=0.8, kind="foliage", a="#4FA845", b="#9BDD78", scale=(0.0, 0.45)),
    }
    models = []

    # Fountain: stone basin with rippling water, pedestal and bowl, eight arcing streams and a jet on top.
    md = L.Model("Fountain", (0 * SPACING, 0, 0))
    o = lambda x, y, z: at(md, x, y, z)  # noqa: E731
    md.add(lathe("Basin", [(0, 0), (2.25, 0), (2.3, 0.05), (2.3, 0.55), (2.36, 0.6), (2.36, 0.72), (2.26, 0.79), (2.02, 0.79),
                           (1.95, 0.72), (1.95, 0.2), (0, 0.2)], o(0, 0, 0), "y", 40), "Body", "Trim", k["stone"])
    md.add(cyl("Water", 1.97, 0.06, o(0, 0.58, 0), "y", 40, 0.0), "Body", "Trim", k["water"])
    md.add(lathe("Pedestal", [(0, 0.5), (0.48, 0.5), (0.48, 0.62), (0.32, 0.76), (0.24, 1.0), (0.3, 1.22), (0.22, 1.4), (0, 1.4)],
                 o(0, 0, 0), "y", 24), "Body", "Trim", k["stone"])
    md.add(lathe("Bowl", [(0, 1.34), (0.3, 1.36), (0.75, 1.48), (0.93, 1.62), (0.96, 1.7), (0.88, 1.71), (0.72, 1.6), (0, 1.57)],
                 o(0, 0, 0), "y", 32), "Body", "Trim", k["stone"])
    md.add(cyl("BowlWater", 0.84, 0.04, o(0, 1.63, 0), "y", 32, 0.0), "Body", "Trim", k["water"])
    md.add(lathe("Spout", [(0, 1.6), (0.16, 1.6), (0.1, 1.75), (0.12, 1.85), (0, 1.88)], o(0, 0, 0), "y", 16), "Body", "Trim", k["stone"])
    md.add(lathe("Jet", [(0, 1.84), (0.07, 1.9), (0.11, 2.1), (0.09, 2.35), (0.04, 2.5), (0, 2.53)], o(0, 0, 0), "y", 14), "Body", "Trim", k["jet"])
    md.add(lathe("JetCrown", [(0.02, 2.45), (0.16, 2.46), (0.28, 2.36), (0.34, 2.2), (0.3, 2.2), (0.24, 2.32), (0.14, 2.4), (0.02, 2.4)],
                 o(0, 0, 0), "y", 18), "Body", "Trim", k["jet"])
    for i in range(8):
        a = math.radians(i * 45 + 22.5)
        ca, sa = math.cos(a), math.sin(a)
        pts = []
        for j in range(9):
            t = j / 8
            d = 0.93 + 0.7 * t
            pts.append(o(ca * d, 1.7 + 0.18 * t - 1.25 * t * t, sa * d))
        md.add(sweep("Stream", pts, circle(0.045, 7), ref=(-sa, 0, ca), taper=lambda t: (1.0 + 0.4 * t, 1.0 + 0.4 * t)), "Body", "Trim", k["jet"])
        md.add(lathe("Splash", [(0.06, 0.6), (0.14, 0.63), (0.18, 0.6), (0.06, 0.6)], o(ca * 1.63, 0.0, sa * 1.63), "y", 10), "Body", "Trim", k["white"])
    models.append(md)

    # Duck pond: sandy bank, rippling water, stones, reeds with cattails, lily pads and two ducks ("Duck0/1" bob).
    md = L.Model("Pond", (1 * SPACING, 0, 0))
    o = lambda x, y, z: at(md, x, y, z)  # noqa: E731
    md.add(patch("Bank", 0, o(0, 0, 0), sx=1.3, seed=3, rough=0.06, ring=(3.7, 4.55), y=0.02, rim_h=0.05, segs=48), "Body", "Trim", k["bank"])
    md.add(patch("Water", 3.85, o(0, 0, 0), sx=1.3, seed=3, rough=0.06, y=0.05, segs=48), "Body", "Trim", k["water"])
    import random
    rnd = random.Random(11)
    for i in range(16):
        a = i / 16 * 2 * math.pi + rnd.uniform(-0.1, 0.1)
        r = 4.25 + rnd.uniform(-0.15, 0.15)
        s = rnd.uniform(0.28, 0.45)
        md.add(rock(f"Stone{i}", s, o(math.cos(a) * r * 1.3, 0.12, math.sin(a) * r), (1.4, 0.65, 1.0), 2, 0.3,
                    euler=(0, -math.degrees(a), 0), seed=80 + i), "Body", "Trim", k["pebble"])
    for c, (cx, cz) in enumerate(((-3.9, 1.6), (4.3, -1.2), (-1.2, -3.4))):
        for j in range(5):
            x, z = cx + rnd.uniform(-0.35, 0.35), cz + rnd.uniform(-0.3, 0.3)
            h = rnd.uniform(0.9, 1.45)
            lean = (rnd.uniform(-0.12, 0.12), rnd.uniform(-0.12, 0.12))
            md.add(rod("Reed", 0.025, o(x, 0.04, z), o(x + lean[0], h, z + lean[1]), 6), "Body", "Trim", k["reed"])
            if j % 2 == 0:
                md.add(sphere("Cattail", 0.055, o(x + lean[0] * 0.9, h - 0.12, z + lean[1] * 0.9), (1, 3.2, 1), 8, 6), "Body", "Trim", k["cattail"])
        for j in range(3):
            a = j * 2.1 + c
            pts = [o(cx + math.cos(a) * 0.5 * t * t, 0.04 + 0.75 * t, cz + math.sin(a) * 0.5 * t * t) for t in (i / 4 for i in range(5))]
            md.add(sweep("ReedLeaf", pts, [(0.05, 0), (0, 0.01), (-0.05, 0), (0, -0.01)], ref=(-math.sin(a), 0, math.cos(a)),
                         taper=lambda t: (max(0.1, 1 - t), 1.0)), "Body", "Trim", k["reed"])
    for i, (x, z, r) in enumerate(((2.2, 1.1, 0.55), (-2.6, -0.8, 0.5), (0.8, -2.0, 0.45), (2.9, -0.4, 0.38))):
        md.add(lathe("LilyPad", [(0, 0), (r, 0), (r, 0.03), (0, 0.03)], o(x, 0.07, z), "y", 20, angle=320.0, start=i * 70.0), "Body", "Trim", k["pad"])
        if i < 3:
            for p in range(6):
                a = p / 6 * 2 * math.pi
                md.add(sphere("Petal", 0.07, o(x + 0.12 + math.cos(a) * 0.08, 0.15, z + 0.08 + math.sin(a) * 0.08), (0.8, 0.6, 1.6), 8, 5,
                              euler=(25, -math.degrees(a) + 90, 0)), "Body", "Trim", k["lily"])
            md.add(sphere("LilyHeart", 0.05, o(x + 0.12, 0.17, z + 0.08), segs=8, rings=5), "Body", "Trim", k["yellow"])
    for i, ((nx, ny, nz), yaw, s) in enumerate((((-1.0, 0.12, 0.6), 40.0, 1.0), ((1.2, 0.12, -0.8), 190.0, 0.7))):
        node = md.node(f"Duck{i}", "Body", (nx, ny, nz))

        def D(x, y, z, node=node, yaw=yaw, s=s):
            rx, rz = yaw_pt(x * s, z * s, yaw)
            return md.at(node, rx, y * s, rz)
        md.add(sphere("Body", 0.4 * s, D(0, 0.2, 0), (1, 0.72, 1.3), 20, 12, euler=(0, yaw, 0)), node, "Trim", k["duck"])
        md.add(sphere("Tail", 0.16 * s, D(0, 0.36, -0.5), (0.8, 0.8, 1.3), 12, 8, euler=(-30, yaw, 0)), node, "Trim", k["duck"])
        for sd in (-1, 1):
            md.add(sphere("Wing", 0.24 * s, D(sd * 0.3, 0.27, -0.06), (0.35, 0.6, 1.1), 12, 8, euler=(-10, yaw, 0)), node, "Trim", k["duck"])
            md.add(sphere("Eye", 0.045 * s, D(sd * 0.12, 0.66, 0.5), (1, 1.2, 0.7), 10, 6, euler=(0, yaw, 0)), node, "Trim", k["ink"])
            md.add(sphere("Shine", 0.015 * s, D(sd * 0.12 + 0.012, 0.68, 0.54), segs=6, rings=4), node, "Trim", k["white"])
        md.add(sphere("Head", 0.25 * s, D(0, 0.6, 0.32), segs=18, rings=12), node, "Trim", k["duck"])
        md.add(sphere("Beak", 0.11 * s, D(0, 0.55, 0.57), (1.1, 0.42, 1.3), 12, 8, euler=(0, yaw, 0)), node, "Trim", k["beak"])
    models.append(md)

    # Park bench: wooden slats on green iron sides with arm rests.
    md = L.Model("Bench", (2 * SPACING, 0, 0))
    o = lambda x, y, z: at(md, x, y, z)  # noqa: E731
    for z in (-0.15, 0.0, 0.15):
        md.add(rbox("Slat", (2.0, 0.06, 0.13), o(0, 0.5, z), 0.025, 2), "Body", "Trim", k["wood"])
    for y in (0.74, 0.93):
        md.add(rbox("Back", (2.0, 0.14, 0.05), o(0, y, -0.28 - (y - 0.74) * 0.18), 0.02, 2, euler=(-10, 0, 0)), "Body", "Trim", k["wood"])
    for x in (-0.86, 0.86):
        md.add(rod("FrontLeg", 0.04, o(x, 0, 0.2), o(x, 0.68, 0.22), 8), "Body", "Trim", k["iron"])
        md.add(rod("BackLeg", 0.04, o(x, 0, -0.18), o(x, 1.02, -0.32), 8), "Body", "Trim", k["iron"])
        md.add(rod("SeatRail", 0.035, o(x, 0.45, -0.22), o(x, 0.45, 0.22), 8), "Body", "Trim", k["iron"])
        md.add(rbox("Arm", (0.08, 0.05, 0.5), o(x, 0.68, 0.0), 0.02, 2), "Body", "Trim", k["iron"])
        md.add(sphere("ArmEnd", 0.05, o(x, 0.68, 0.25), segs=10, rings=6), "Body", "Trim", k["iron"])
    models.append(md)

    # Meadow pieces (combined by the hundred in Unity): a grass tuft and five flower colours.
    md = L.Model("MeadowTuft", (3 * SPACING, 0, 0))
    for i in range(4):
        a = i / 4 * 2 * math.pi + 0.4
        ca, sa = math.cos(a), math.sin(a)
        h = 0.36 + 0.12 * ((i * 7) % 3) / 2
        pts = [at(md, ca * (0.05 + 0.14 * t * t), h * t, sa * (0.05 + 0.14 * t * t)) for t in (0.0, 0.5, 1.0)]
        md.add(sweep("Blade", pts, [(0.035, 0), (0, 0.01), (-0.035, 0), (0, -0.01)], ref=(-sa, 0, ca),
                     taper=lambda t: (max(0.12, 1 - 0.9 * t), 1.0)), "Body", "Trim", k["grass"])
    models.append(md)
    for i, col in enumerate(("#FF8FC7", "#FFE066", "#FFFFFF", "#B39CFF", "#FF7F6E")):
        md = L.Model(f"MeadowFlower{i}", ((4 + i) * SPACING, 0, 0))
        petal = m.get(f"Petal{i}", col, rough=0.5)
        md.add(flower_disc("Petals", 0.32, at(md, 0, 0.05, 0), 5, 15), "Body", "Trim", petal)
        md.add(sphere("Centre", 0.085, at(md, 0, 0.09, 0), (1, 0.5, 1), 6, 3), "Body", "Trim", k["yellow"])
        models.append(md)
    return models


# ================================================================================================= PEOPLE
PEOPLE = (
    # name, hair/hat, shirt, trousers, skin, scale, hard hat, hair style
    ("Foreman", "#FFC93C", "#FF9F1C", "#3D5A80", "#F1C27D", 1.0, True, ""),
    ("KidA", "#3A2E2A", "#FF595E", "#3D5A80", "#FFD5B0", 0.72, False, "pigtails"),
    ("KidB", "#8A5A3A", "#4EA8DE", "#3D5A80", "#C68A5C", 0.68, False, "tuft"),
    ("KidC", "#F4D35E", "#8AC926", "#3D5A80", "#F1C27D", 0.7, False, "ponytail"),
    ("Resident", "#6B4226", "#219EBC", "#2A9D8F", "#B07A50", 1.0, False, "short"),
)


def people(m):
    ink = m.get("Ink", "#2B2D42", rough=0.2)
    white = m.get("White", "#FFFFFF", rough=0.3)
    cheek = m.get("Cheek", "#FF9AA2", rough=0.6)
    mouth = m.get("Mouth", "#B5423C", rough=0.4)
    shoe = m.get("Shoe", "#5A4636", rough=0.6)
    sole = m.get("Sole", "#3A3945", rough=0.7)
    stripe = m.get("HiVis", "#FFF2B0", rough=0.3)
    belt = m.get("Belt", "#6B4A32", rough=0.6)
    gold = m.get("Gold", "#FFC93C", rough=0.25)
    models = []
    for n, (name, top, shirt_hex, trousers_hex, skin_hex, s, hard_hat, style) in enumerate(PEOPLE):
        md = L.Model(name, (n * SPACING * 0.5, 0, 0))
        shirt = m.get("Shirt" + name, shirt_hex, rough=0.75)
        legs = m.get("Trousers" + trousers_hex, trousers_hex, rough=0.8)
        skin = m.get("Skin" + skin_hex, skin_hex, rough=0.55)
        nose = m.get("Nose" + skin_hex, L_shade(skin_hex, 0.9), rough=0.55)
        hair = m.get(("Hat" if hard_hat else "Hair") + name, top, rough=0.3 if hard_hat else 0.6)
        P = lambda x, y, z: at(md, x * s, y * s, z * s)  # noqa: E731
        for sd in (-1, 1):
            md.add(rbox("Shoe", (0.27 * s, 0.15 * s, 0.4 * s), P(sd * 0.14, 0.1, 0.05), 0.07 * s, 3, top=(0.88, 0.85)), "Body", "Trim", shoe)
            md.add(rbox("Sole", (0.28 * s, 0.05 * s, 0.41 * s), P(sd * 0.14, 0.025, 0.05), 0.02 * s, 2), "Body", "Trim", sole)
            md.add(cyl("Leg", 0.12 * s, 0.48 * s, P(sd * 0.14, 0.4, 0), "y", 12, 0.03 * s), "Body", "Trim", legs)
        md.add(rbox("Hips", (0.56 * s, 0.2 * s, 0.36 * s), P(0, 0.64, 0), 0.09 * s, 3), "Body", "Trim", legs)
        md.add(squash(lathe("Torso", [(0, 0), (0.3 * s, 0), (0.34 * s, 0.08 * s), (0.35 * s, 0.3 * s), (0.33 * s, 0.5 * s), (0.26 * s, 0.64 * s),
                                      (0.12 * s, 0.7 * s), (0, 0.7 * s)], P(0, 0.62, 0), "y", 24), sz=0.7), "Body", "Trim", shirt)
        md.add(cyl("Neck", 0.1 * s, 0.14 * s, P(0, 1.36, 0), "y", 12, 0.0), "Body", "Trim", skin)
        if hard_hat:
            for y, r in ((0.27, 0.35), (0.45, 0.335)):
                md.add(squash(lathe("Stripe", [(r * s - 0.02 * s, -0.03 * s), (r * s + 0.008 * s, -0.03 * s), (r * s + 0.012 * s, 0),
                                               (r * s + 0.008 * s, 0.03 * s), (r * s - 0.02 * s, 0.03 * s)], P(0, 0.62 + y, 0), "y", 24), sz=0.7),
                       "Body", "Trim", stripe)
            md.add(squash(lathe("Belt", [(0.3 * s, -0.035 * s), (0.312 * s, -0.035 * s), (0.315 * s, 0), (0.312 * s, 0.035 * s), (0.3 * s, 0.035 * s)],
                                P(0, 0.66, 0), "y", 24), sz=0.72), "Body", "Trim", belt)
            md.add(rbox("Buckle", (0.1 * s, 0.07 * s, 0.03 * s), P(0, 0.66, 0.225), 0.01 * s, 1), "Body", "Trim", gold)
        else:
            md.add(squash(lathe("Band", [(0.33 * s, -0.035 * s), (0.352 * s, -0.035 * s), (0.355 * s, 0), (0.352 * s, 0.035 * s), (0.33 * s, 0.035 * s)],
                                P(0, 0.62 + 0.3, 0), "y", 24), sz=0.7), "Body", "Trim", white)
            md.add(sphere("Badge", 0.07 * s, P(0.12, 1.08, 0.235), (1, 1, 0.35), 12, 8), "Body", "Trim", white if name != "KidC" else gold)
        # Head and friendly face.
        md.add(sphere("Head", 0.4 * s, P(0, 1.7, 0), segs=28, rings=16), "Body", "Trim", skin)
        for sd in (-1, 1):
            md.add(sphere("Ear", 0.09 * s, P(sd * 0.39, 1.68, 0), (0.6, 1, 1), 10, 6), "Body", "Trim", skin)
            md.add(sphere("Eye", 0.075 * s, P(sd * 0.14, 1.75, 0.345), (1, 1.3, 0.55), 14, 8), "Body", "Trim", ink)
            md.add(sphere("Shine", 0.026 * s, P(sd * 0.14 + 0.025, 1.79, 0.39), segs=8, rings=5), "Body", "Trim", white)
            md.add(tube_arc("Brow", 0.08 * s, 0.017 * s, P(sd * 0.14, 1.8, 0.35), 60, 120, 8, 5), "Body", "Trim", ink if hard_hat else hair)
            md.add(sphere("Cheek", 0.075 * s, P(sd * 0.24, 1.6, 0.31), (1, 0.6, 0.4), 12, 6), "Body", "Trim", cheek)
        md.add(sphere("Nose", 0.06 * s, P(0, 1.65, 0.39), segs=10, rings=6), "Body", "Trim", nose)
        md.add(tube_arc("Smile", 0.1 * s, 0.024 * s, P(0, 1.6, 0.36), 205, 335, 12, 6, euler=(-12, 0, 0)), "Body", "Trim", mouth)
        if hard_hat:
            md.add(lathe("Hat", [(0, 0.36 * s), (0.16 * s, 0.35 * s), (0.31 * s, 0.28 * s), (0.42 * s, 0.12 * s), (0.44 * s, 0.02 * s),
                                 (0.56 * s, -0.01 * s), (0.57 * s, -0.05 * s), (0.42 * s, -0.04 * s), (0, -0.04 * s)], P(0, 1.82, 0), "y", 28),
                   "Body", "Trim", hair)
            md.add(tube_arc("Ridge", 0.36 * s, 0.035 * s, P(0, 1.82, 0), 25, 155, 14, 6, euler=(0, 90, 0)), "Body", "Trim", hair)
            md.add(sphere("HatBadge", 0.07 * s, P(0, 2.0, 0.35), (1, 1, 0.4), 12, 8), "Body", "Trim", white)
        else:
            md.add(lathe("Hair", [(0, 0.37 * s), (0.2 * s, 0.34 * s), (0.36 * s, 0.22 * s), (0.44 * s, 0.05 * s), (0.45 * s, 0), (0, 0)],
                         P(0, 1.73, -0.04), "y", 28, euler=(-18, 0, 0)), "Body", "Trim", hair)
            for x, y, z in ((-0.17, 1.95, 0.3), (0.0, 1.98, 0.33), (0.17, 1.95, 0.3)):
                md.add(sphere("Fringe", 0.11 * s, P(x, y, z), (1.3, 0.7, 0.8), 12, 8), "Body", "Trim", hair)
            if style == "pigtails":
                for sd in (-1, 1):
                    md.add(sphere("Pigtail", 0.15 * s, P(sd * 0.45, 1.72, -0.12), (0.9, 1.25, 0.9), 12, 8), "Body", "Trim", hair)
                    md.add(cyl("Tie", 0.065 * s, 0.06 * s, P(sd * 0.45, 1.9, -0.12), "y", 10, 0.0), "Body", "Trim", shirt)
            elif style == "tuft":
                md.add(sphere("Tuft", 0.14 * s, P(-0.04, 2.13, -0.02), (1, 1.1, 1), 12, 8), "Body", "Trim", hair)
                md.add(sphere("Tuft2", 0.09 * s, P(0.1, 2.22, -0.08), segs=10, rings=6), "Body", "Trim", hair)
            elif style == "ponytail":
                md.add(sphere("Ponytail", 0.16 * s, P(0, 1.78, -0.48), (0.85, 1.4, 0.9), 12, 8, euler=(25, 0, 0)), "Body", "Trim", hair)
                md.add(cyl("Tie", 0.08 * s, 0.05 * s, P(0, 1.98, -0.42), "z", 10, 0.0), "Body", "Trim", shirt)
            else:
                md.add(sphere("Part", 0.16 * s, P(0.2, 1.98, 0.12), (1.2, 0.8, 1.2), 12, 8), "Body", "Trim", hair)
        # Arms: the left hangs down, the right ("WaveArm") points up and waves.
        md.add(sphere("ShoulderL", 0.12 * s, P(-0.4, 1.22, 0), segs=12, rings=8), "Body", "Trim", shirt)
        md.add(rod("SleeveL", 0.1 * s, P(-0.41, 1.22, 0), P(-0.45, 0.96, 0), 12), "Body", "Trim", shirt)
        md.add(rod("ForearmL", 0.075 * s, P(-0.45, 0.98, 0), P(-0.49, 0.7, 0), 10), "Body", "Trim", skin)
        md.add(sphere("HandL", 0.11 * s, P(-0.5, 0.64, 0), segs=12, rings=8), "Body", "Trim", skin)
        arm = md.node("WaveArm", "Body", (0.42 * s, 1.25 * s, 0))
        A = lambda x, y, z: md.at(arm, x * s, y * s, z * s)  # noqa: E731
        md.add(sphere("Shoulder", 0.12 * s, A(0, 0, 0), segs=12, rings=8), arm, "Trim", shirt)
        md.add(rod("Sleeve", 0.1 * s, A(0, 0, 0), A(0, 0.3, 0), 12), arm, "Trim", shirt)
        md.add(rod("Forearm", 0.075 * s, A(0, 0.28, 0), A(0, 0.56, 0), 10), arm, "Trim", skin)
        md.add(sphere("Hand", 0.11 * s, A(0, 0.63, 0), segs=12, rings=8), arm, "Trim", skin)
        md.add(sphere("Thumb", 0.045 * s, A(-0.09, 0.6, 0.04), segs=8, rings=5), arm, "Trim", skin)
        models.append(md)
    return models


# ================================================================================================= GROUND (tileable textures)
GROUND_DIR = L.REPO + "/Assets/_LittleBuildersWorld/Art/Textures/Ground"
GROUND = {
    # name: (tile metres, base a, base b, light speck, dark speck, patch scale /m, speck scale /m)
    "Grass": (8.0, "#86CD67", "#9ADB7A", "#B4E894", "#6FBA55", 0.4, 10.0),
    "Dirt": (6.0, "#B07A4E", "#C08A5C", "#D6A57A", "#8E5F3B", 0.6, 5.0),
    "Sand": (6.0, "#DFC690", "#EAD6A6", "#F6E7C2", "#C8AD78", 0.5, 12.0),
}


def ground_textures(size=512):
    """Seamless ground tiles: 4D noise sampled on a torus, so the edges wrap exactly."""
    L.reset()
    scene = bpy_scene()
    os.makedirs(GROUND_DIR, exist_ok=True)
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UV")
    v = [bm.verts.new(Vector(p)) for p in ((0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0))]
    f = bm.faces.new(v)
    for loop, c in zip(f.loops, ((0, 0), (1, 0), (1, 1), (0, 1))):
        loop[uv].uv = c
    plane = L._link("Tile", bm)
    for name, (tile, a, b, light, dark, patch_s, speck_s) in GROUND.items():
        mat = L.bpy.data.materials.new("G" + name)
        nodes, links = mat.node_tree.nodes, mat.node_tree.links
        bsdf = nodes["Principled BSDF"]
        R = tile / (2 * math.pi)
        tc = nodes.new("ShaderNodeTexCoord")
        sep = nodes.new("ShaderNodeSeparateXYZ")
        links.new(tc.outputs["UV"], sep.inputs[0])

        def trig(src, op):
            mul = nodes.new("ShaderNodeMath")
            mul.operation = "MULTIPLY"
            mul.inputs[1].default_value = 2 * math.pi
            links.new(src, mul.inputs[0])
            t = nodes.new("ShaderNodeMath")
            t.operation = op
            links.new(mul.outputs[0], t.inputs[0])
            sc = nodes.new("ShaderNodeMath")
            sc.operation = "MULTIPLY"
            sc.inputs[1].default_value = R
            links.new(t.outputs[0], sc.inputs[0])
            return sc.outputs[0]
        comb = nodes.new("ShaderNodeCombineXYZ")
        links.new(trig(sep.outputs["X"], "COSINE"), comb.inputs[0])
        links.new(trig(sep.outputs["X"], "SINE"), comb.inputs[1])
        links.new(trig(sep.outputs["Y"], "COSINE"), comb.inputs[2])
        w = trig(sep.outputs["Y"], "SINE")

        def noise(scale, detail, kind="noise", seed=0.0):
            if kind == "noise":
                n = nodes.new("ShaderNodeTexNoise")
                n.noise_dimensions = "4D"
                n.inputs["Detail"].default_value = detail
                n.inputs["Roughness"].default_value = 0.55
            else:
                n = nodes.new("ShaderNodeTexVoronoi")
                n.voronoi_dimensions = "4D"
                n.inputs["Randomness"].default_value = 1.0
            n.inputs["Scale"].default_value = scale
            add = nodes.new("ShaderNodeMath")
            add.operation = "ADD"
            add.inputs[1].default_value = seed
            links.new(w, add.inputs[0])
            links.new(comb.outputs[0], n.inputs["Vector"])
            links.new(add.outputs[0], n.inputs["W"])
            return n

        def ramp(fac, stops):
            r = nodes.new("ShaderNodeValToRGB")
            els = r.color_ramp.elements
            while len(els) < len(stops):
                els.new(0.5)
            for el, (pos, col) in zip(els, stops):
                el.position = pos
                el.color = col
            links.new(fac, r.inputs["Fac"])
            return r.outputs["Color"]

        def mix(c0, c1, fac):
            mx = nodes.new("ShaderNodeMix")
            mx.data_type = "RGBA"
            links.new(fac, mx.inputs["Factor"])
            links.new(c0, mx.inputs[6])
            links.new(c1, mx.inputs[7])
            return mx.outputs[2]
        base = ramp(noise(patch_s, 4.0).outputs["Fac"], [(0.3, L.lin(a)), (0.7, L.lin(b))])
        fine = ramp(noise(patch_s * 6, 3.0, seed=3.0).outputs["Fac"], [(0.3, (0.93, 0.93, 0.93, 1)), (0.7, (1.06, 1.06, 1.06, 1))])
        mul = nodes.new("ShaderNodeMix")
        mul.data_type = "RGBA"
        mul.blend_type = "MULTIPLY"
        mul.inputs["Factor"].default_value = 1.0
        links.new(base, mul.inputs[6])
        links.new(fine, mul.inputs[7])
        col = mul.outputs[2]
        vl = noise(speck_s, 0, "voronoi", 7.0)
        light_mask = ramp(vl.outputs["Distance"], [(0.3, (1, 1, 1, 1)), (0.37, (0, 0, 0, 1))])
        lm = nodes.new("ShaderNodeMath")
        lm.operation = "MULTIPLY"
        lm.inputs[1].default_value = 0.8
        links.new(light_mask, lm.inputs[0])
        col = mix(col, ramp(vl.outputs["Color"], [(0.0, L.lin(light)), (1.0, L.lin(b))]), lm.outputs[0])
        vd = noise(speck_s * 0.7, 0, "voronoi", 13.0)
        dark_mask = ramp(vd.outputs["Distance"], [(0.28, (1, 1, 1, 1)), (0.34, (0, 0, 0, 1))])
        dm = nodes.new("ShaderNodeMath")
        dm.operation = "MULTIPLY"
        dm.inputs[1].default_value = 0.7
        links.new(dark_mask, dm.inputs[0])
        col = mix(col, ramp(vd.outputs["Color"], [(0.0, L.lin(dark)), (1.0, L.lin(a))]), dm.outputs[0])
        links.new(col, bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = 1.0
        bsdf.inputs["Base Color"].default_value = (0, 0, 0, 1)
        plane.data.materials.clear()
        plane.data.materials.append(mat)
        im = L.bpy.data.images.new("T_" + name, size, size, alpha=False)
        L._set_bake_target([mat], im)
        scene.render.engine = "CYCLES"
        scene.cycles.device = "CPU"
        scene.cycles.samples = 16
        L.bpy.ops.object.select_all(action="DESELECT")
        plane.select_set(True)
        L.bpy.context.view_layer.objects.active = plane
        L.bpy.ops.object.bake(type="EMIT", margin=0, use_clear=True)
        im.filepath_raw = f"{GROUND_DIR}/T_{name}.png"
        im.file_format = "PNG"
        im.save()
        print(f"[LBW] ground texture {name}: {size}px, tiles every {tile} m -> {im.filepath_raw}")


def bpy_scene():
    return L.bpy.context.scene


def L_shade(hexstr, f):
    h = hexstr.lstrip("#")
    return "#" + "".join(f"{max(0, min(255, int(int(h[i:i + 2], 16) * f))):02X}" for i in (0, 2, 4))


GROUPS = {"Nature": nature, "Street": street, "Buildings": buildings, "Play": play, "Garden": garden, "People": people}


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    want_preview = "--preview" in argv
    names = [a for a in argv if not a.startswith("--")] or list(GROUPS) + ["Ground"]
    for name in names:
        if name == "Ground":
            ground_textures()
            continue
        L.reset()
        models = GROUPS[name](L.Mats())
        g = L.Group("Env" + name, models)
        g.ao_distance = 0.6 if name == "People" else 1.5
        tex, em = L.bake(g, 2048 if name == "Buildings" else 1024, 48, PROP_TEX)
        total = sum(L.export(md, PROP_DIR) for md in models)
        print(f"[LBW] group {name}: {len(models)} props, {total} vertices")
        if want_preview:
            views = []
            for md in models:
                c = md.world["Body"]
                r = 3.4 if name == "People" else 7.0
                views.append(((c.x + r * 0.45, c.y + r * 0.5, c.z + r), (c.x, c.y + (0.9 if name == "People" else 1.2), c.z)))
            L.preview(g, tex, em, {}, f"{L.LOG_DIR}/Env{name}_preview", views)
    print("[LBW] build_env done")


main()
