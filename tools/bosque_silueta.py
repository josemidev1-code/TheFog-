"""Silueta humana para el bosque: alguien de pie, quieto, con los brazos algo largos y la cabeza ladeada.
Solo se ve a contraluz entre la niebla o por las cámaras, así que es una figura oscura sin detalle.

  blender -b --factory-startup -P tools/bosque_silueta.py -- <salida.fbx> [<salida.blend>]
"""
import sys

import bmesh
import bpy

args = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)

# (nombre, posición, radio x/y de la piel, padre)
HUESOS = [
    ("pelvis", (0.00, 0.00, 0.98), (0.150, 0.105), None),
    ("vientre", (0.00, 0.01, 1.16), (0.135, 0.095), "pelvis"),
    ("pecho", (0.00, 0.03, 1.38), (0.165, 0.105), "vientre"),
    ("cuello", (0.01, 0.04, 1.54), (0.050, 0.050), "pecho"),
    ("cabeza", (0.04, 0.03, 1.66), (0.098, 0.105), "cuello"),
    ("coronilla", (0.07, 0.02, 1.79), (0.080, 0.090), "cabeza"),
    # brazos: caen rectos y llegan casi a la rodilla
    ("hombro_i", (0.215, 0.03, 1.44), (0.062, 0.060), "pecho"),
    ("codo_i", (0.245, 0.04, 1.10), (0.043, 0.043), "hombro_i"),
    ("muneca_i", (0.250, 0.01, 0.74), (0.032, 0.030), "codo_i"),
    ("mano_i", (0.250, 0.00, 0.58), (0.040, 0.022), "muneca_i"),
    ("hombro_d", (-0.215, 0.03, 1.44), (0.062, 0.060), "pecho"),
    ("codo_d", (-0.250, 0.04, 1.11), (0.043, 0.043), "hombro_d"),
    ("muneca_d", (-0.262, 0.01, 0.75), (0.032, 0.030), "codo_d"),
    ("mano_d", (-0.268, 0.00, 0.59), (0.040, 0.022), "muneca_d"),
    # piernas
    ("cadera_i", (0.095, 0.00, 0.92), (0.085, 0.085), "pelvis"),
    ("rodilla_i", (0.105, -0.01, 0.50), (0.058, 0.058), "cadera_i"),
    ("tobillo_i", (0.110, 0.02, 0.09), (0.040, 0.042), "rodilla_i"),
    ("pie_i", (0.120, -0.15, 0.035), (0.045, 0.030), "tobillo_i"),
    ("cadera_d", (-0.095, 0.00, 0.92), (0.085, 0.085), "pelvis"),
    ("rodilla_d", (-0.105, -0.01, 0.50), (0.058, 0.058), "cadera_d"),
    ("tobillo_d", (-0.110, 0.02, 0.09), (0.040, 0.042), "rodilla_d"),
    ("pie_d", (-0.120, -0.15, 0.035), (0.045, 0.030), "tobillo_d"),
]

mesh = bpy.data.meshes.new("Silueta")
bm = bmesh.new()
verts = {}
for name, pos, _, parent in HUESOS:
    verts[name] = bm.verts.new(pos)
    if parent:
        bm.edges.new((verts[parent], verts[name]))
bm.to_mesh(mesh)
bm.free()

obj = bpy.data.objects.new("Silueta", mesh)
bpy.context.scene.collection.objects.link(obj)
bpy.context.view_layer.objects.active = obj
obj.select_set(True)

skin = obj.modifiers.new("Piel", "SKIN")
for i, (_, _, radio, _) in enumerate(HUESOS):
    obj.data.skin_vertices[0].data[i].radius = radio
obj.data.skin_vertices[0].data[0].use_root = True
sub = obj.modifiers.new("Suave", "SUBSURF")
sub.levels = 1
bpy.ops.object.modifier_apply(modifier="Piel")
bpy.ops.object.modifier_apply(modifier="Suave")

mat = bpy.data.materials.new("Silueta")
obj.data.materials.append(mat)
for p in obj.data.polygons:
    p.use_smooth = True

tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
print("OK Silueta", tris, [round(c, 2) for c in obj.dimensions])
if len(args) > 1:
    bpy.ops.wm.save_as_mainfile(filepath=args[1])
bpy.ops.export_scene.fbx(
    filepath=args[0], use_selection=True, object_types={"MESH"},
    apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
    path_mode="STRIP", embed_textures=False, mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False)
