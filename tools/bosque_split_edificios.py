"""Separa del pack Vegetation (Elbolilloduro, CC0) la torre de vigilancia, la cabaña y los objetos sueltos.

  blender -b --factory-startup -P tools/bosque_split_edificios.py -- <Vegetation.fbx> <carpeta_salida>

Torre_Vigia.fbx y Cabana.fbx conservan cada pieza como objeto aparte (puerta, muebles, cristales) con el
origen del conjunto en el centro de la base. Los objetos sueltos salen como Prop_<nombre>.fbx con el pivote abajo.
"""
import os
import re
import sys

import bpy
from mathutils import Vector

src, out_dir = sys.argv[-2:]
os.makedirs(out_dir, exist_ok=True)

VEG = re.compile(r"^(Tree|Grass|Rock|Trunk|Branch|Plane|Cylinder|Tocon)")
# No se usan: coche de sheriff (no encaja en España), armas y munición, rocas de nieve.
FUERA = re.compile(r"^(Car_sheriff|Cube\.23[89]|Cube\.24[23]|Tire\.00[4-7]|Steering_wheel\.001|Weatherby|Ammunition|Cube\.008|Cube\.014|Cube\.015)")
COCHE = re.compile(r"^(Car$|Car_Door|Cube\.237|Glove_box|Pedal|Seating|Steering_wheel$|Tire$|Tire\.00[0-3])")

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)


def bbox(o):
    v = [o.matrix_world @ Vector(c) for c in o.bound_box]
    return (Vector((min(p.x for p in v), min(p.y for p in v), min(p.z for p in v))),
            Vector((max(p.x for p in v), max(p.y for p in v), max(p.z for p in v))))


def export(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(out_dir, name + ".fbx"), use_selection=True, object_types={"MESH"},
        apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        path_mode="STRIP", embed_textures=False, mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False)


def hornear(o):
    """Deja la malla en coordenadas de mundo y el objeto sin transformaciones ni padre."""
    mw = o.matrix_world.copy()
    o.parent = None
    o.data = o.data.copy()
    o.data.transform(mw)
    o.matrix_world.identity()


todos = [o for o in bpy.context.scene.objects if o.type == "MESH" and not VEG.match(o.name) and not FUERA.match(o.name)]
grupos = {"Torre_Vigia": [], "Cabana": [], "Coche": []}
sueltos = []
for o in todos:
    mn, mx = bbox(o)
    c = (mn + mx) / 2
    if COCHE.match(o.name):
        grupos["Coche"].append(o)
    elif -54 < c.x < -33 and -61 < c.y < -40:
        grupos["Torre_Vigia"].append(o)
    elif -33.5 < c.x < -22 and -52.5 < c.y < -43.5:
        grupos["Cabana"].append(o)
    else:
        sueltos.append(o)

for o in todos:
    hornear(o)
bpy.context.view_layer.update()

SUELO = {"Torre_Vigia": None, "Cabana": -0.2, "Coche": None}  # cabaña: el origen va al nivel del suelo interior
for name, objs in grupos.items():
    mns = [bbox(o) for o in objs]
    mn = Vector((min(b[0].x for b in mns), min(b[0].y for b in mns), min(b[0].z for b in mns)))
    mx = Vector((max(b[1].x for b in mns), max(b[1].y for b in mns), max(b[1].z for b in mns)))
    base = SUELO[name] if SUELO[name] is not None else mn.z
    off = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, base))
    for o in objs:
        for v in o.data.vertices:
            v.co -= off
        o.name = o.name.replace(".", "_")
    export(objs, name)
    print("OK", name, len(objs), [round(c, 2) for c in (mx - mn)])
    for o in objs:
        print("   ", o.name, [m.name for m in o.data.materials if m])

hechos = set()
for o in sueltos:
    base = o.name.split(".")[0]
    if base in hechos:
        continue
    hechos.add(base)
    mn, mx = bbox(o)
    off = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
    for v in o.data.vertices:
        v.co -= off
    o.name = "Prop_" + base
    o.data.name = o.name
    export([o], o.name)
    print("OK", o.name, [round(c, 2) for c in (mx - mn)], [m.name for m in o.data.materials if m])
