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
from lbw_blender import Vector, rbox, cyl, lathe, sphere, blob, rock, unity_matrix, instance  # noqa: E402

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


def L_shade(hexstr, f):
    h = hexstr.lstrip("#")
    return "#" + "".join(f"{max(0, min(255, int(int(h[i:i + 2], 16) * f))):02X}" for i in (0, 2, 4))


GROUPS = {"Nature": nature, "Street": street, "Buildings": buildings}


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    want_preview = "--preview" in argv
    names = [a for a in argv if not a.startswith("--")] or list(GROUPS)
    for name in names:
        L.reset()
        models = GROUPS[name](L.Mats())
        g = L.Group("Env" + name, models)
        g.ao_distance = 1.5
        tex, em = L.bake(g, 2048 if name == "Buildings" else 1024, 48, PROP_TEX)
        total = sum(L.export(md, PROP_DIR) for md in models)
        print(f"[LBW] group {name}: {len(models)} props, {total} vertices")
        if want_preview:
            n = len(models)
            L.preview(g, tex, em, {}, f"{L.LOG_DIR}/Env{name}_preview",
                      [((i * SPACING + 4.0, 3.5, 8.0), (i * SPACING, 1.8, 0.0)) for i in range(n)])
    print("[LBW] build_env done")


main()
