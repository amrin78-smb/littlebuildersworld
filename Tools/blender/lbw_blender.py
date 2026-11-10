"""
Little Builders World - shared Blender helpers for the scripted (offline, original) vehicle models.

All positions/sizes passed to helpers are in UNITY coordinates (metres, +Y up, +Z forward); U() maps them to
Blender (+Z up). The exporter maps back. Every model is: smooth bevelled parts -> one UV atlas -> Cycles bakes
(base colour, ambient occlusion, roughness, emission) -> one texture per vehicle + a mesh pack (.bytes) that
the Unity generator (Scripts/Editor/BlenderModels.cs) turns into meshes under named pivots.
"""
import bpy
import bmesh
import math
import os
import struct
from collections import OrderedDict
from mathutils import Matrix, Vector

import numpy as np

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MODEL_DIR = REPO + "/Assets/_LittleBuildersWorld/Art/Models/Vehicles"
TEX_DIR = REPO + "/Assets/_LittleBuildersWorld/Art/Textures/Vehicles"
LOG_DIR = REPO + "/Logs/Blender"


def U(x, y, z):
    return Vector((x, z, y))


def lin(hexstr):
    h = hexstr.lstrip("#")
    out = []
    for i in (0, 2, 4):
        c = int(h[i:i + 2], 16) / 255.0
        out.append(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4)
    return (out[0], out[1], out[2], 1.0)


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    if scene.world is None:
        scene.world = bpy.data.worlds.new("World")
    return scene


# ---------------------------------------------------------------------------------------------------------------- materials
class Mats:
    """Blender look materials (what gets baked). Each part also names a Unity slot: Primary, Secondary or Trim."""

    def __init__(self):
        self.cache = {}

    def get(self, key, hexstr="#FFFFFF", rough=0.5, kind="plain", emit=0.0, dirt=0.0, a=None, b=None, scale=8.0):
        if key in self.cache:
            return self.cache[key]
        m = bpy.data.materials.new(key)
        nt = m.node_tree
        nodes, links = nt.nodes, nt.links
        bsdf = nodes["Principled BSDF"]
        bsdf.inputs["Roughness"].default_value = rough
        colour_out = None
        if kind == "paint":
            # Near-white so the Unity tint (paint colour) shows true; a whisper of mottling.
            colour_out = self._mottle(nodes, links, (0.93, 0.93, 0.93, 1.0), (1.0, 1.0, 1.0, 1.0), 4.0)
        elif kind == "plain":
            colour_out = self._mottle(nodes, links, self._shade(lin(hexstr), 0.94), lin(hexstr), 5.0)
        elif kind == "stripes":
            # Diagonal bands (hazard chevrons), object space.
            colour_out = self._bands(nodes, links, lin(a), lin(b), scale, "DIAGONAL")
        elif kind == "hbands":
            colour_out = self._bands(nodes, links, lin(a), lin(b), scale, "Z")
        elif kind == "glass":
            tc = nodes.new("ShaderNodeTexCoord")
            sep = nodes.new("ShaderNodeSeparateXYZ")
            links.new(tc.outputs["Object"], sep.inputs[0])
            ramp = nodes.new("ShaderNodeValToRGB")
            ramp.color_ramp.elements[0].position = 0.0
            ramp.color_ramp.elements[0].color = lin("#7FCDEE")
            ramp.color_ramp.elements[1].position = 1.0
            ramp.color_ramp.elements[1].color = lin("#D6F3FF")
            mr = nodes.new("ShaderNodeMapRange")
            mr.inputs["From Min"].default_value = -0.5
            mr.inputs["From Max"].default_value = 0.5
            links.new(sep.outputs["Z"], mr.inputs["Value"])
            links.new(mr.outputs["Result"], ramp.inputs["Fac"])
            # Two soft white highlight streaks.
            wave = nodes.new("ShaderNodeTexWave")
            wave.bands_direction = "DIAGONAL"
            wave.inputs["Scale"].default_value = 1.3
            wave.inputs["Distortion"].default_value = 0.0
            links.new(tc.outputs["Object"], wave.inputs["Vector"])
            st = nodes.new("ShaderNodeValToRGB")
            st.color_ramp.interpolation = "CONSTANT"
            st.color_ramp.elements[0].position = 0.0
            st.color_ramp.elements[0].color = (0, 0, 0, 1)
            st.color_ramp.elements[1].position = 0.86
            st.color_ramp.elements[1].color = (1, 1, 1, 1)
            links.new(wave.outputs["Fac"], st.inputs["Fac"])
            mix = nodes.new("ShaderNodeMix")
            mix.data_type = "RGBA"
            links.new(st.outputs["Color"], mix.inputs["Factor"])
            links.new(ramp.outputs["Color"], mix.inputs[6])
            mix.inputs[7].default_value = (0.92, 0.98, 1.0, 1.0)
            colour_out = mix.outputs[2]
        if dirt > 0.0 and colour_out is not None:
            colour_out = self._dirt(nodes, links, colour_out, dirt)
        if colour_out is not None:
            links.new(colour_out, bsdf.inputs["Base Color"])
        if emit > 0.0:
            bsdf.inputs["Emission Color"].default_value = lin(hexstr)
            bsdf.inputs["Emission Strength"].default_value = emit
        m["lbw_kind"] = kind
        self.cache[key] = m
        return m

    @staticmethod
    def _shade(c, f):
        return (c[0] * f, c[1] * f, c[2] * f, 1.0)

    @staticmethod
    def _mottle(nodes, links, c0, c1, scale):
        noise = nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = scale
        noise.inputs["Detail"].default_value = 3.0
        tc = nodes.new("ShaderNodeTexCoord")
        links.new(tc.outputs["Object"], noise.inputs["Vector"])
        ramp = nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].position = 0.3
        ramp.color_ramp.elements[0].color = c0
        ramp.color_ramp.elements[1].position = 0.7
        ramp.color_ramp.elements[1].color = c1
        links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
        return ramp.outputs["Color"]

    @staticmethod
    def _bands(nodes, links, ca, cb, scale, direction):
        tc = nodes.new("ShaderNodeTexCoord")
        wave = nodes.new("ShaderNodeTexWave")
        wave.wave_type = "BANDS"
        wave.bands_direction = direction
        wave.wave_profile = "SAW"
        wave.inputs["Scale"].default_value = scale
        wave.inputs["Distortion"].default_value = 0.0
        links.new(tc.outputs["Object"], wave.inputs["Vector"])
        ramp = nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.interpolation = "CONSTANT"
        ramp.color_ramp.elements[0].position = 0.0
        ramp.color_ramp.elements[0].color = ca
        ramp.color_ramp.elements[1].position = 0.5
        ramp.color_ramp.elements[1].color = cb
        links.new(wave.outputs["Fac"], ramp.inputs["Fac"])
        return ramp.outputs["Color"]

    @staticmethod
    def _dirt(nodes, links, colour_out, amount):
        """Soft dusty band near the ground (world height), broken up by noise."""
        geo = nodes.new("ShaderNodeNewGeometry")
        sep = nodes.new("ShaderNodeSeparateXYZ")
        links.new(geo.outputs["Position"], sep.inputs[0])
        mr = nodes.new("ShaderNodeMapRange")
        mr.inputs["From Min"].default_value = 0.15
        mr.inputs["From Max"].default_value = 1.1
        mr.inputs["To Min"].default_value = amount
        mr.inputs["To Max"].default_value = 0.0
        links.new(sep.outputs["Z"], mr.inputs["Value"])
        noise = nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = 3.0
        links.new(geo.outputs["Position"], noise.inputs["Vector"])
        mul = nodes.new("ShaderNodeMath")
        mul.operation = "MULTIPLY"
        links.new(mr.outputs["Result"], mul.inputs[0])
        links.new(noise.outputs["Fac"], mul.inputs[1])
        mix = nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        links.new(mul.outputs[0], mix.inputs["Factor"])
        links.new(colour_out, mix.inputs[6])
        mix.inputs[7].default_value = lin("#9C7B5B")
        return mix.outputs[2]


# ---------------------------------------------------------------------------------------------------------------- model
class Model:
    """Collects parts: each is (object, pivot node name, Unity slot)."""

    def __init__(self, name):
        self.name = name
        self.parts = []
        self.nodes = OrderedDict()  # name -> (parent, unity local pos)
        self.world = {"Body": Vector((0, 0, 0))}
        self.nobake = set()  # objects that keep their own UVs and a scene material (e.g. the scrolling track belt)

    def node(self, name, parent, pos):
        self.nodes[name] = (parent, Vector(pos))
        self.world[name] = self.world[parent] + Vector(pos)
        return name

    def add(self, obj, node, slot, mat):
        obj.data.materials.clear()
        obj.data.materials.append(mat)
        self.parts.append((obj, node, slot))
        return obj

    def at(self, node, x, y, z):
        """Unity world position of a point given in a node's local space."""
        w = self.world[node]
        return (w.x + x, w.y + y, w.z + z)


def _link(name, bm):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    for p in me.polygons:
        p.use_smooth = True
    return obj


def _rot_unity(euler):
    """Unity Euler (degrees, applied Z, X, Y) -> Blender rotation matrix (mirror-converted)."""
    rx, ry, rz = (math.radians(a) for a in euler)
    # Unity axis X->Blender X, Y->Blender Z, Z->Blender Y; handedness flip negates angles.
    mx = Matrix.Rotation(-rx, 4, "X")
    my = Matrix.Rotation(-ry, 4, "Z")
    mz = Matrix.Rotation(-rz, 4, "Y")
    return my @ mx @ mz


def _place(obj, center, euler=None, base=None):
    m = Matrix.Translation(U(*center))
    if euler is not None:
        m = m @ _rot_unity(euler)
    if base is not None:
        m = m @ _rot_unity(base)
    obj.matrix_world = m
    return obj


def _bake_modifiers(obj, bevel=None, segs=3, angle=None, weighted=True):
    if bevel and bevel > 0:
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = segs
        mod.use_clamp_overlap = True
        if angle is not None:
            mod.limit_method = "ANGLE"
            mod.angle_limit = math.radians(angle)
        else:
            mod.limit_method = "NONE"
    if weighted:
        wn = obj.modifiers.new("Weighted", "WEIGHTED_NORMAL")
        wn.mode = "FACE_AREA"
        wn.weight = 50
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(obj.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
    old = obj.data
    obj.modifiers.clear()
    obj.data = me
    bpy.data.meshes.remove(old)
    return obj


def rbox(name, size, center, radius=0.05, segs=3, euler=None, top=(1.0, 1.0), top_shift=0.0, bottom=(1.0, 1.0)):
    """Rounded box. size/center in Unity metres. top=(x scale, z scale) of the top face, top_shift moves it in Z."""
    sx, sy, sz = size
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        x, y, z = v.co  # blender: y = unity z (depth), z = unity y (height)
        if z > 0:
            x *= top[0]
            y = y * top[1] + top_shift / sz
        else:
            x *= bottom[0]
            y *= bottom[1]
        v.co = Vector((x * sx, y * sz, z * sy))
    obj = _link(name, bm)
    _bake_modifiers(obj, min(radius, 0.49 * min(sx, sy, sz)), segs)
    return _place(obj, center, euler)


def cyl(name, radius, length, center, axis="x", segs=24, bevel=0.03, bsegs=2, radius2=None, euler=None):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs, radius1=radius,
                          radius2=radius if radius2 is None else radius2, depth=length)
    obj = _link(name, bm)
    _bake_modifiers(obj, bevel, bsegs, angle=40)
    return _place(obj, center, euler, _axis(axis))


def _axis(axis):
    # Helpers build along Blender Z = Unity Y; this turns that axis onto Unity X / Y / Z.
    return {"y": (0, 0, 0), "x": (0, 0, 90), "z": (90, 0, 0)}[axis]


def lathe(name, profile, center, axis="x", segs=32, angle=360.0, start=0.0, euler=None):
    """Surface of revolution. profile: [(radius, height)] in order; closed rings if first/last radius > 0."""
    bm = bmesh.new()
    verts = [bm.verts.new((r, 0.0, h)) for r, h in profile]
    edges = [bm.edges.new((verts[i], verts[i + 1])) for i in range(len(verts) - 1)]
    closed = profile[0][0] > 1e-6 and profile[-1][0] > 1e-6 and angle >= 360.0 and \
        (abs(profile[0][0] - profile[-1][0]) < 1e-6 and abs(profile[0][1] - profile[-1][1]) < 1e-6)
    if closed:
        bmesh.ops.remove_doubles(bm, verts=verts, dist=1e-6)
    geom = list(bm.verts) + list(bm.edges)
    if start:
        bmesh.ops.rotate(bm, verts=list(bm.verts), cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(start), 3, "Z"))
    bmesh.ops.spin(bm, geom=geom, cent=(0, 0, 0), axis=(0, 0, 1), angle=math.radians(angle),
                   steps=max(3, int(segs * angle / 360.0)), use_duplicate=False)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = _link(name, bm)
    _bake_modifiers(obj, None)
    return _place(obj, center, euler, _axis(axis))


def sphere(name, radius, center, scale=(1, 1, 1), segs=24, rings=14, euler=None):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=radius)
    sx, sy, sz = scale
    for v in bm.verts:
        v.co = Vector((v.co.x * sx, v.co.y * sz, v.co.z * sy))
    obj = _link(name, bm)
    _bake_modifiers(obj, None, weighted=False)
    return _place(obj, center, euler)


def tube_arc(name, major, minor, center, a0, a1, segs=20, ring=10, euler=None):
    """A bent tube (smile, handles) in the Unity XY plane facing +Z. Angles in degrees, 0 = +X, 270 = down."""
    bm = bmesh.new()
    rings = []
    for i in range(segs + 1):
        a = math.radians(a0 + (a1 - a0) * i / segs)
        c = Vector((math.cos(a) * major, 0.0, math.sin(a) * major))
        radial = Vector((math.cos(a), 0.0, math.sin(a)))
        side = Vector((0.0, 1.0, 0.0))
        rings.append([bm.verts.new(c + (radial * math.cos(t) + side * math.sin(t)) * minor)
                      for t in (2 * math.pi * k / ring for k in range(ring))])
    for i in range(segs):
        for k in range(ring):
            bm.faces.new((rings[i][k], rings[i][(k + 1) % ring], rings[i + 1][(k + 1) % ring], rings[i + 1][k]))
    for end, sign in ((rings[0], -1), (rings[-1], 1)):
        f = bm.faces.new(end if sign > 0 else list(reversed(end)))
        bmesh.ops.poke(bm, faces=[f])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = _link(name, bm)
    _bake_modifiers(obj, None, weighted=False)
    return _place(obj, center, euler)


def tub(name, size, center, wall=0.12, radius=0.06, top_flare=1.04):
    """Open-topped box (dump bed): outer size, walls grow inward."""
    sx, sy, sz = size
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for f in [f for f in bm.faces if f.normal.z > 0.9]:
        bm.faces.remove(f)
    for v in bm.verts:
        x, y, z = v.co
        if z > 0:
            x *= top_flare
        v.co = Vector((x * sx, y * sz, (z + 0.5) * sy))
    obj = _link(name, bm)
    sol = obj.modifiers.new("Solid", "SOLIDIFY")
    sol.thickness = wall
    sol.offset = -1.0
    sol.use_even_offset = True
    _bake_modifiers(obj, radius, 3, angle=30)
    return _place(obj, center)


def arch(name, r_in, r_out, width, center, a0, a1, segs=24):
    """Wheel-arch fender around the Unity X axis: solid ring sector from angle a0 to a1 (0 = forward, 90 = up)."""
    hw = width * 0.5
    bm = bmesh.new()
    rings = []
    for i in range(segs + 1):
        a = math.radians(a0 + (a1 - a0) * i / segs)
        d = Vector((0.0, math.cos(a), math.sin(a)))  # blender: y = unity z, z = unity y
        rings.append([bm.verts.new(Vector((x, 0, 0)) + d * r) for x, r in
                      ((-hw, r_in), (hw, r_in), (hw, r_out), (-hw, r_out))])
    for i in range(segs):
        for k in range(4):
            bm.faces.new((rings[i][k], rings[i][(k + 1) % 4], rings[i + 1][(k + 1) % 4], rings[i + 1][k]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = _link(name, bm)
    _bake_modifiers(obj, min(0.04, (r_out - r_in) * 0.4), 2, angle=35)
    return _place(obj, center)


def instance(src, name, matrix):
    """Linked duplicate (shares mesh and UVs: identical bolts/lugs/wheels cost texture space once)."""
    obj = src.copy()
    obj.name = name
    bpy.context.scene.collection.objects.link(obj)
    obj.matrix_world = matrix
    return obj


def unity_matrix(pos, euler=None):
    m = Matrix.Translation(U(*pos))
    if euler is not None:
        m = m @ _rot_unity(euler)
    return m


# ---------------------------------------------------------------------------------------------------------------- bake
def _set_bake_target(mats, image):
    for m in mats:
        nodes = m.node_tree.nodes
        n = nodes.get("LBW_Bake")
        if n is None:
            n = nodes.new("ShaderNodeTexImage")
            n.name = "LBW_Bake"
        n.image = image
        for other in nodes:
            other.select = False
        n.select = True
        nodes.active = n


def bake(model, size=1024, ao_samples=64):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes.get("Background")
    if bg is not None:
        bg.inputs["Color"].default_value = (1, 1, 1, 1)
    scene.world.light_settings.distance = 1.2  # AO reach (m)

    objs = [p[0] for p in model.parts if p[0] not in model.nobake]
    meshes = []
    uv_objs = []
    for o in objs:
        if o.data not in meshes:
            meshes.append(o.data)
            uv_objs.append(o)
        if not o.data.uv_layers:
            o.data.uv_layers.new(name="UV")
    bpy.ops.object.select_all(action="DESELECT")
    for o in uv_objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = uv_objs[0]
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(55), island_margin=0.0, area_weight=0.0, scale_to_bounds=False)
    bpy.ops.uv.pack_islands(margin=0.004, rotate=True)
    bpy.ops.object.mode_set(mode="OBJECT")

    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    mats = list({m for o in objs for m in o.data.materials})

    def img(n, non_color):
        im = bpy.data.images.new(f"{model.name}_{n}", size, size, alpha=False, float_buffer=False)
        if non_color:
            im.colorspace_settings.name = "Non-Color"
        return im

    out = {}
    passes = [("color", "DIFFUSE", False, 4, {"pass_filter": {"COLOR"}}),
              ("ao", "AO", True, ao_samples, {}),
              ("rough", "ROUGHNESS", True, 1, {}),
              ("emit", "EMIT", False, 1, {})]
    for key, kind, nc, samples, kw in passes:
        im = img(key, nc)
        _set_bake_target(mats, im)
        scene.cycles.samples = samples
        bpy.ops.object.bake(type=kind, margin=8, margin_type="EXTEND", use_clear=True, **kw)
        out[key] = np.array(im.pixels[:], dtype=np.float32).reshape(size, size, 4)
        print(f"[LBW] baked {key}")

    col = out["color"][:, :, :3]
    ao = np.clip(out["ao"][:, :, :1], 0, 1)
    # Light 3x3 blur hides AO sampling noise; soft toy-like contact shadows.
    pad = np.pad(ao, ((1, 1), (1, 1), (0, 0)), mode="edge")
    ao = sum(pad[y:y + size, x:x + size] for y in range(3) for x in range(3)) / 9.0
    shade = 1.0 - 0.55 * (1.0 - ao)
    rgb = np.clip(col * shade, 0, 1)
    smooth = np.clip(1.0 - out["rough"][:, :, :1], 0, 1)
    final = np.concatenate([rgb, smooth], axis=2)
    os.makedirs(TEX_DIR, exist_ok=True)
    tex = bpy.data.images.new(f"T_{model.name}", size, size, alpha=True)
    tex.pixels[:] = final.ravel()
    tex.filepath_raw = f"{TEX_DIR}/T_{model.name}.png"
    tex.file_format = "PNG"
    tex.save()
    emit = np.clip(out["emit"][:, :, :3], 0, 1)
    em = bpy.data.images.new(f"T_{model.name}_E", size, size, alpha=False)
    em.pixels[:] = np.concatenate([emit, np.ones((size, size, 1), np.float32)], axis=2).ravel()
    em.filepath_raw = f"{TEX_DIR}/T_{model.name}_E.png"
    em.file_format = "PNG"
    em.save()
    return tex, em


# ---------------------------------------------------------------------------------------------------------------- export
def export(model):
    """Mesh pack: nodes (pivots) + one mesh per (node, slot). Vertices are pivot-local, Unity axes."""
    os.makedirs(MODEL_DIR, exist_ok=True)
    groups = OrderedDict()
    for obj, node, slot in model.parts:
        groups.setdefault((node, slot), []).append(obj)
    dg = bpy.context.evaluated_depsgraph_get()
    total = 0
    blob = bytearray()

    def s(text):
        b = text.encode("utf-8")
        blob.extend(struct.pack("<i", len(b)))
        blob.extend(b)

    blob.extend(b"LBWM")
    blob.extend(struct.pack("<i", 1))
    blob.extend(struct.pack("<i", len(model.nodes)))
    for name, (parent, pos) in model.nodes.items():
        s(name)
        s(parent)
        blob.extend(struct.pack("<3f", pos.x, pos.y, pos.z))
    blob.extend(struct.pack("<i", len(groups)))
    for (node, slot), objs in groups.items():
        pivot = model.world[node]
        verts, norms, uvs, tris = [], [], [], []
        index = {}
        for obj in objs:
            eo = obj.evaluated_get(dg)
            me = eo.to_mesh()
            me.calc_loop_triangles()
            mw = obj.matrix_world
            nm = mw.to_3x3().inverted().transposed()
            uvd = me.uv_layers.active.data
            cn = me.corner_normals
            for tri in me.loop_triangles:
                # Mirroring the axes (Blender right-handed -> Unity left-handed) flips winding: emit reversed.
                for li in reversed(tri.loops):
                    co = mw @ me.vertices[me.loops[li].vertex_index].co
                    n = (nm @ cn[li].vector).normalized()
                    uv = uvd[li].uv
                    p = (co.x - pivot.x, co.z - pivot.y, co.y - pivot.z)
                    nn = (n.x, n.z, n.y)
                    key = (round(p[0], 5), round(p[1], 5), round(p[2], 5), round(nn[0], 3), round(nn[1], 3),
                           round(nn[2], 3), round(uv.x, 5), round(uv.y, 5))
                    i = index.get(key)
                    if i is None:
                        i = len(verts)
                        index[key] = i
                        verts.append(p)
                        norms.append(nn)
                        uvs.append((uv.x, uv.y))
                    tris.append(i)
            eo.to_mesh_clear()
        s(f"{node}_{slot}")
        s(node)
        s(slot)
        blob.extend(struct.pack("<i", len(verts)))
        for p in verts:
            blob.extend(struct.pack("<3f", *p))
        for n in norms:
            blob.extend(struct.pack("<3f", *n))
        for uv in uvs:
            blob.extend(struct.pack("<2f", *uv))
        blob.extend(struct.pack("<i", len(tris)))
        blob.extend(struct.pack(f"<{len(tris)}i", *tris))
        total += len(verts)
        print(f"[LBW] mesh {node}_{slot}: {len(verts)} verts, {len(tris) // 3} tris")
    with open(f"{MODEL_DIR}/{model.name}.bytes", "wb") as f:
        f.write(blob)
    print(f"[LBW] {model.name}: {len(groups)} meshes, {total} vertices -> {MODEL_DIR}/{model.name}.bytes")
    return total


# ---------------------------------------------------------------------------------------------------------------- preview
def preview(model, tex, em, tints, path, views):
    """Cycles render with the baked texture (as Unity will show it) for quick review."""
    scene = bpy.context.scene
    final = {}
    for slot in ("Primary", "Secondary", "Trim"):
        m = bpy.data.materials.new(f"Final_{slot}")
        nodes, links = m.node_tree.nodes, m.node_tree.links
        bsdf = nodes["Principled BSDF"]
        t = nodes.new("ShaderNodeTexImage")
        t.image = tex
        mix = nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        mix.blend_type = "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(t.outputs["Color"], mix.inputs[6])
        mix.inputs[7].default_value = lin(tints.get(slot, "#FFFFFF"))
        links.new(mix.outputs[2], bsdf.inputs["Base Color"])
        bsdf.inputs["Roughness"].default_value = 0.45
        if slot == "Trim":
            e = nodes.new("ShaderNodeTexImage")
            e.image = em
            links.new(e.outputs["Color"], bsdf.inputs["Emission Color"])
            bsdf.inputs["Emission Strength"].default_value = 0.6
        final[slot] = m
    tm = bpy.data.materials.new("Final_Track")
    tm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = lin("#3A3945")
    final["Track"] = tm
    for obj, node, slot in model.parts:
        obj.data.materials.clear()
        obj.data.materials.append(final[slot])
    # Ground, sun, sky.
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, 0))
    ground = bpy.context.active_object
    gm = bpy.data.materials.new("Ground")
    gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = lin("#F2D99A")
    ground.data.materials.append(gm)
    sun_data = bpy.data.lights.new("Sun", "SUN")
    sun_data.energy = 3.2
    sun_data.angle = math.radians(8)
    sun = bpy.data.objects.new("Sun", sun_data)
    scene.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(40), 0, math.radians(35))
    scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = lin("#BFE6FF")
    scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.9
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = 50
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    scene.cycles.samples = 32
    scene.cycles.use_denoising = True
    scene.render.resolution_x = 800
    scene.render.resolution_y = 600
    scene.view_settings.view_transform = "Standard"
    os.makedirs(LOG_DIR, exist_ok=True)
    for i, (pos, look) in enumerate(views):
        cam.location = U(*pos)
        d = U(*look) - cam.location
        cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = f"{path}_{i}.png"
        bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=f"{LOG_DIR}/{model.name}.blend")


def track_belt(name, length, height, width, center, thick=0.09, cleat_spacing=0.35):
    """Crawler belt swept around a stadium path (bottom at y=0). Own UVs: u across, v along the loop (scrolls)."""
    r = height * 0.5
    zb, zf = -(length * 0.5 - r), length * 0.5 - r
    path, norm = [], []
    straight, arc = 10, 14
    for i in range(straight):
        path.append((zb + (zf - zb) * i / straight, 0.0)); norm.append((0.0, -1.0))
    for i in range(arc + 1):
        a = -math.pi / 2 + math.pi * i / arc
        path.append((zf + math.cos(a) * r, r + math.sin(a) * r)); norm.append((math.cos(a), math.sin(a)))
    for i in range(1, straight):
        path.append((zf + (zb - zf) * i / straight, height)); norm.append((0.0, 1.0))
    for i in range(arc + 1):
        a = math.pi / 2 + math.pi * i / arc
        path.append((zb + math.cos(a) * r, r + math.sin(a) * r)); norm.append((math.cos(a), math.sin(a)))
    path.append(path[0]); norm.append(norm[0])
    along = [0.0]
    for i in range(1, len(path)):
        along.append(along[-1] + math.dist(path[i - 1], path[i]))
    perim = along[-1]
    repeats = max(1.0, round(perim / (cleat_spacing * 8.0)))
    hw = width * 0.5
    # Cross-section (x across, d outward), rounded corners; d = 0 is the path, belt grows inward.
    sec = [(-hw + 0.03, 0.0), (-hw * 0.5, 0.012), (0.0, 0.016), (hw * 0.5, 0.012), (hw - 0.03, 0.0),
           (hw, -0.03), (hw, -thick + 0.02), (hw - 0.03, -thick), (-hw + 0.03, -thick), (-hw, -thick + 0.02), (-hw, -0.03)]
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new("UV")
    rings, uvs = [], {}
    for i, ((z, y), (nz, ny)) in enumerate(zip(path, norm)):
        v = along[i] / perim * repeats
        ring = []
        for x, d in sec:
            vert = bm.verts.new(Vector((x, z + nz * d, y + ny * d)))  # blender: (x, unity z, unity y)
            uvs[vert] = (min(1.0, max(0.0, (x + hw) / width)), v)
            ring.append(vert)
        rings.append(ring)
    n = len(sec)
    for i in range(len(rings) - 1):
        for k in range(n):
            f = bm.faces.new((rings[i][k], rings[i + 1][k], rings[i + 1][(k + 1) % n], rings[i][(k + 1) % n]))
            for loop in f.loops:
                loop[uvl].uv = uvs[loop.vert]
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = _link(name, bm)
    _bake_modifiers(obj, None)
    return _place(obj, center)
