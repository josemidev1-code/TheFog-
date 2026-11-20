"""Maqueta de la cabaña en Blender para comprobar la colocación sin Unity.
Coordenadas de entrada = las locales de Unity que usa BosqueAsentamiento (x hacia la puerta, y arriba).
Unity (x, y, z) = Blender (-x, z, -y); giro Unity yaw = -giro Blender en Z.
blender -b --factory-startup -P maqueta.py -- <proyecto> <layout.json> <salida>
"""
import json
import math
import os
import sys

import bpy
from mathutils import Vector

proj, layout_path, out = sys.argv[sys.argv.index("--") + 1:][:3]
os.makedirs(out, exist_ok=True)
L = json.load(open(layout_path, encoding="utf-8"))
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(proj, "Assets/Models/Retro/Bosque/Edificios/Cabana.fbx"))
for n in L.get("quitar", []):
    o = bpy.data.objects.get(n)
    if o:
        bpy.data.objects.remove(o, do_unlink=True)


def bounds(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return lo, hi


for it in L["piezas"]:
    before = set(bpy.data.objects)
    p = os.path.join(proj, it["modelo"])
    if p.lower().endswith(".fbx"):
        bpy.ops.import_scene.fbx(filepath=p)
    else:
        bpy.ops.import_scene.gltf(filepath=p)
    new = [o for o in bpy.data.objects if o not in before]
    roots = [o for o in new if o.parent is None]
    piv = bpy.data.objects.new("P_" + it["nombre"], None)
    bpy.context.collection.objects.link(piv)
    for r in roots:
        r.parent = piv
    x, y, z = it["pos"]
    piv.location = (-x, -z, y)
    piv.rotation_euler = (0, 0, -math.radians(it.get("yaw", 0)))
    s = it.get("escala", 1)
    piv.scale = (s, s, s)
    bpy.context.view_layer.update()
    lo, hi = bounds([o for o in new if o.type == "MESH"])
    print(f"MAQ {it['nombre']}: unity x[{-hi.x:.2f},{-lo.x:.2f}] y[{lo.z:.2f},{hi.z:.2f}] z[{-hi.y:.2f},{-lo.y:.2f}]")

sc = bpy.context.scene
sc.render.engine = "BLENDER_WORKBENCH"
sc.display.shading.light = "STUDIO"
sc.display.shading.color_type = "TEXTURE"
sc.display.shading.show_cavity = True
sc.render.resolution_x, sc.render.resolution_y = 1280, 720
cd = bpy.data.cameras.new("c")
cam = bpy.data.objects.new("c", cd)
bpy.context.collection.objects.link(cam)
sc.camera = cam
for v in L["vistas"]:
    px, py, pz = v["cam"]
    mx, my, mz = v["mira"]
    pos, mira = Vector((-px, -pz, py)), Vector((-mx, -mz, my))
    cam.location = pos
    cam.rotation_euler = (mira - pos).to_track_quat("-Z", "Y").to_euler()
    if v.get("orto"):
        cd.type = "ORTHO"
        cd.ortho_scale = v["orto"]
    else:
        cd.type = "PERSP"
        cd.angle = math.radians(v.get("fov", 80))
    cd.clip_start, cd.clip_end = v.get("cerca", 0.05), 60
    sc.render.filepath = os.path.join(out, v["nombre"] + ".png")
    bpy.ops.render.render(write_still=True)
