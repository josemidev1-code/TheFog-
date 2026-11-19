"""Separa el pack "Vegetation" de Elbolilloduro (CC0) en un FBX por pieza para Unity.

Cada pieza sale con el pivote en el centro de su base, en el origen y con las transformaciones aplicadas.
Las texturas no se incrustan: Unity usa las de Assets/Models/Retro/Bosque/Texturas (materiales por nombre).

Uso:
  blender -b --factory-startup -P tools/bosque_split_pack.py -- <Vegetation.fbx> <carpeta_salida>
"""
import os
import sys

import bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
src, out_dir = os.path.abspath(argv[0]), os.path.abspath(argv[1])
os.makedirs(out_dir, exist_ok=True)

# nombre en el pack -> nombre en el proyecto
PIEZAS = {
    # pinos (seco / con aguja)
    "Tree_07": "Pino_A_Seco", "Tree_07_V": "Pino_A",
    "Tree_08": "Pino_B_Seco", "Tree_08_V": "Pino_B",
    "Tree_09": "Pino_C_Seco", "Tree_09.001": "Pino_C",
    "Cylinder.006": "Pino_D_Seco", "Cylinder": "Pino_D",
    # árboles de hoja (desnudo / con hoja)
    "Tree": "Arbol_A_Seco", "Tree_V": "Arbol_A",
    "Tree_01": "Arbol_B_Seco", "Tree_01_V": "Arbol_B",
    "Tree_02": "Arbol_C_Seco", "Tree_02_V": "Arbol_C",
    "Tree_03": "Arbol_D_Seco", "Tree_03_V": "Arbol_D",
    "Tree_04": "Arbol_E_Seco", "Tree_04_V": "Arbol_E",
    "Tree_05": "Arbol_F_Seco", "Tree_05_V": "Arbol_F",
    "Tree_06": "Arbol_G_Seco", "Tree_06_V": "Arbol_G",
    # arbustos y hierba
    "Tree_B": "Arbusto_A_Seco", "Tree_B.001": "Arbusto_A",
    "Plane.014": "Arbusto_B_Seco", "Plane.016": "Arbusto_C_Seco",
    "Grass_P.002": "Hierba_A_Seca", "Grass_P.003": "Hierba_B_Seca", "Grass_P.005": "Hierba_C_Seca",
    "Grass_P.001": "Hierba_A", "Grass_P.004": "Hierba_B", "Grass_P.006": "Hierba_C",
    "Grass_P.007": "Hierba_Mancha_Seca", "Grass_P.014": "Hierba_Mancha",
    # rocas (limpia / con musgo)
    "Rock": "Roca_00", "Rock_V": "Roca_00_Musgo", "Rock_01": "Roca_01", "Cube.008": "Roca_01_Musgo",
    "Rock_02": "Roca_02", "Rock_02_V": "Roca_02_Musgo", "Rock_03": "Roca_03", "Rock_03_V": "Roca_03_Musgo",
    "Rock_04": "Roca_04", "Rock_04_V": "Roca_04_Musgo", "Rock_05": "Roca_05", "Rock_05_V": "Roca_05_Musgo",
    "Rock_06": "Roca_06", "Rock_06_V": "Roca_06_Musgo", "Rock_07": "Roca_07", "Rock_07_V": "Roca_07_Musgo",
    "Rock_08": "Roca_08", "Rock_08_V": "Roca_08_Musgo", "Rock_09": "Roca_09", "Rock_09_V": "Roca_09_Musgo",
    "Rock_10": "Roca_10", "Rock_10_V": "Roca_10_Musgo",
    # troncos caídos, tocones y ramas
    "Trunk": "Tronco_A", "Trunk_V": "Tronco_A_Musgo", "Trunk.001": "Tocon_A", "Trunk_V.001": "Tocon_A_Musgo",
    "Trunk_01": "Tronco_B", "Trunk_01_V": "Tronco_B_Musgo", "Trunk_02": "Tronco_C", "Trunk_02_V": "Tronco_C_Musgo",
    "Branch_C": "Rama_Caida",
}

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
objs = {o.name: o for o in bpy.context.scene.objects if o.type == "MESH"}

for pack_name, name in PIEZAS.items():
    o = objs.get(pack_name)
    if o is None:
        print("FALTA", pack_name)
        continue
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    vs = [v.co for v in o.data.vertices]
    mn = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
    mx = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
    # Árboles: el pivote va en el tronco (los vértices más bajos), no en el centro de la copa.
    low = [v for v in vs if v.z < mn.z + 0.05 * (mx.z - mn.z)]
    cx = sum(v.x for v in low) / len(low)
    cy = sum(v.y for v in low) / len(low)
    if not name.startswith(("Pino", "Arbol_")):
        cx, cy = (mn.x + mx.x) / 2, (mn.y + mx.y) / 2
    for v in o.data.vertices:
        v.co -= Vector((cx, cy, mn.z))
    o.name = name
    o.data.name = name
    tris = sum(len(p.vertices) - 2 for p in o.data.polygons)
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(out_dir, name + ".fbx"), use_selection=True, object_types={"MESH"},
        apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        path_mode="STRIP", embed_textures=False, mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False)
    print("OK", name, tris, [round(c, 2) for c in (mx - mn)], [m.name for m in o.data.materials if m])
