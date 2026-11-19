"""Saca de la torre del pack (Torre_Vigia.fbx) la cocina, para llevarla a la casa de Daniel.

  blender -b --factory-startup -P tools/bosque_cocina.py -- <Torre_Vigia.fbx> <carpeta_salida>

La encimera de la cabina es una sola malla en L de 5,8 m, pero está hecha por módulos (hay vértices en cada junta),
así que se parte por las juntas sin inventar geometría:
  Prop_Cocina.fbx       fregadero + módulo estrecho + módulo de cajones (2,46 m), para la casa.
  Prop_Banco_Torre.fbx  dos módulos sin fregadero (2,46 m), el banco de trabajo que se queda en la cabina.
  Prop_Cocina_Gas.fbx   la cocina de gas (el objeto «Stove» de la cabina).
Los cantos que quedan al aire se tapan copiando las caras del canto original (mismas UV).
"""
import os
import sys

import bmesh
import bpy
from mathutils import Vector

src, out_dir = sys.argv[-2:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
EPS = 1e-3


def hornear(o):
    mw = o.matrix_world.copy()
    o.parent = None
    o.data = o.data.copy()
    o.data.transform(mw)
    o.matrix_world.identity()


def export(o, name):
    o.name = name
    o.data.name = name
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(out_dir, name + ".fbx"), use_selection=True, object_types={"MESH"},
        apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        path_mode="STRIP", embed_textures=False, mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False)


def centrar(o):
    xs = [v.co.x for v in o.data.vertices]; ys = [v.co.y for v in o.data.vertices]; zs = [v.co.z for v in o.data.vertices]
    off = Vector(((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, min(zs)))
    for v in o.data.vertices:
        v.co -= off
    return [round(max(a) - min(a), 2) for a in (xs, ys, zs)]


def tramo(encimera, x0, x1, name, tapa_min, tapa_max):
    o = encimera.copy()
    o.data = encimera.data.copy()
    bpy.context.scene.collection.objects.link(o)
    bm = bmesh.new()
    bm.from_mesh(o.data)
    uvl = bm.loops.layers.uv.active
    xmax = max(v.co.x for v in bm.verts)
    # Los cortes van exactamente por una junta de la malla.
    xs = sorted({v.co.x for v in bm.verts})
    x0 = min(xs, key=lambda a: abs(a - x0))
    x1 = min(xs, key=lambda a: abs(a - x1))
    # Plantilla del canto: las caras del extremo libre original (normal +x).
    plantilla = [[(l.vert.co.copy(), l[uvl].uv.copy()) for l in f.loops]
                 for f in bm.faces if all(abs(v.co.x - xmax) < EPS for v in f.verts)]
    fuera = [f for f in bm.faces if any(v.co.x < x0 - EPS or v.co.x > x1 + EPS for v in f.verts)]
    bmesh.ops.delete(bm, geom=fuera, context="FACES")

    def tapa(x, invertir):
        for loops in plantilla:
            orden = list(reversed(loops)) if invertir else loops
            f = bm.faces.new([bm.verts.new((x, co.y, co.z)) for co, _ in orden])
            for l, (_, uv) in zip(f.loops, orden):
                l[uvl].uv = uv

    if tapa_max and abs(x1 - xmax) > EPS:
        tapa(x1, False)
    if tapa_min:
        tapa(x0, True)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bm.normal_update()
    bm.to_mesh(o.data)
    bm.free()
    tam = centrar(o)
    export(o, name)
    print("OK", name, tam, len(o.data.polygons), "caras")


enc = next(o for o in bpy.context.scene.objects if o.name.startswith("Kitchen_table"))
hornear(enc)
juntas = sorted({round(v.co.x, 2) for v in enc.data.vertices})
print("juntas en x:", juntas)
tramo(enc, -0.76, 1.70, "Prop_Cocina", True, False)
tramo(enc, -1.99, 0.47, "Prop_Banco_Torre", True, True)

gas = next(o for o in bpy.context.scene.objects if o.name == "Stove")
hornear(gas)
print("OK Prop_Cocina_Gas", centrar(gas))
export(gas, "Prop_Cocina_Gas")
