"""Prepara un modelo de Tripo para Unity con Blender (sin coste de créditos).

- Suelda las islas de la malla (si no, al reducir se abren grietas).
- Reduce polígonos (decimate colapsar) hasta un número de triángulos objetivo.
- Hornea el relieve de la malla original (~1,4 M triángulos) en un mapa de normales para la versión ligera.
- Escala a la altura real en metros y pone el pivote en el centro de la base.
- Exporta .gltf + .bin + texturas sueltas (<nombre>_Color.jpg, <nombre>_Normal.png) para el ajuste retro de Unity.

Uso:
  blender -b --factory-startup -P tools/tripo_prepare.py -- <entrada.glb> <carpeta_salida> <nombre> <triangulos> <altura_m> [grande]

"grande" añade "_Grande" a las texturas (torre, casa): el ajuste retro las deja a 1024 px en vez de 512.
"""
import os
import sys

import bpy
from mathutils import Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1:]
src, out_dir, name, target_tris, height = argv[0], argv[1], argv[2], int(argv[3]), float(argv[4])
big = len(argv) > 5 and argv[5].lower() == "grande"
tex_base = name + ("_Grande" if big else "")
bake_size = 2048
out_dir = os.path.abspath(out_dir)
os.makedirs(out_dir, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.abspath(src))

meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
bpy.ops.object.select_all(action="DESELECT")
for o in meshes:
    o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
obj = bpy.context.view_layer.objects.active
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def tri_count(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)


before = tri_count(obj)
# Tripo/glTF parte la malla en islas sueltas en cada costura de UV. Si se reduce así, cada isla
# encoge por su lado y se abren grietas por las que se ve el cielo. Se sueldan antes (las UV
# se conservan por esquina, así que la textura no cambia).
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.remove_doubles(threshold=0.0001)
bpy.ops.object.mode_set(mode="OBJECT")

# Copia de alta resolución para hornear el relieve.
high = obj.copy()
high.data = obj.data.copy()
bpy.context.collection.objects.link(high)

if before > target_tris:
    mod = obj.modifiers.new("Reducir", "DECIMATE")
    mod.decimate_type = "COLLAPSE"
    mod.ratio = target_tris / before
    mod.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
after = tri_count(obj)
# Las normales personalizadas de Tripo quedan rotas tras reducir: se recalculan suaves con
# corte en aristas vivas, y se usan las mismas para hornear y para exportar.
bpy.ops.object.select_all(action="DESELECT")
obj.select_set(True)
bpy.context.view_layer.objects.active = obj
if obj.data.has_custom_normals:
    bpy.ops.mesh.customdata_custom_splitnormals_clear()
bpy.ops.object.shade_smooth_by_angle(angle=1.0472)  # 60°

# Material propio para la versión ligera (la copia alta conserva el original).
src_mat = obj.data.materials[0]
mat = src_mat.copy()
mat.name = name
obj.data.materials[0] = mat
nodes, links = mat.node_tree.nodes, mat.node_tree.links
bsdf = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
for n in nodes:
    if n.type == "TEX_IMAGE" and n.image is not None and n.outputs["Color"].is_linked:
        n.image.name = tex_base + "_Color"

# --- Horneado del relieve: malla original -> malla ligera ---
normal_img = bpy.data.images.new(tex_base + "_Normal", bake_size, bake_size, alpha=False, float_buffer=False)
normal_img.colorspace_settings.name = "Non-Color"
bake_node = nodes.new("ShaderNodeTexImage")
bake_node.image = normal_img
bake_node.interpolation = "Linear"
for n in nodes:
    n.select = False
bake_node.select = True
nodes.active = bake_node

scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.device = "CPU"
scene.cycles.samples = 1
dims0 = obj.dimensions
size0 = max(dims0.x, dims0.y, dims0.z)
bake = scene.render.bake
bake.use_selected_to_active = True
bake.cage_extrusion = 0.01 * size0
bake.max_ray_distance = 0.03 * size0
bake.margin = 8
bake.normal_space = "TANGENT"

bpy.ops.object.select_all(action="DESELECT")
high.select_set(True)
obj.select_set(True)
bpy.context.view_layer.objects.active = obj
bpy.ops.object.bake(type="NORMAL")
normal_img.filepath_raw = os.path.join(out_dir, tex_base + "_Normal.png")
normal_img.file_format = "PNG"
normal_img.save()

# Conecta el mapa de normales al material para que se exporte.
nmap = nodes.new("ShaderNodeNormalMap")
links.new(bake_node.outputs["Color"], nmap.inputs["Color"])
links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])

bpy.data.objects.remove(high, do_unlink=True)

# Escala a la altura real (Blender: Z arriba) y pivote en el centro de la base.
bbox = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
zmin = min(v.z for v in bbox); zmax = max(v.z for v in bbox)
s = height / max(zmax - zmin, 1e-6)
obj.scale = (s, s, s)
bpy.ops.object.select_all(action="DESELECT")
obj.select_set(True)
bpy.context.view_layer.objects.active = obj
bpy.ops.object.transform_apply(scale=True)
bbox = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
cx = (min(v.x for v in bbox) + max(v.x for v in bbox)) / 2
cy = (min(v.y for v in bbox) + max(v.y for v in bbox)) / 2
zmin = min(v.z for v in bbox)
obj.data.transform(Matrix.Translation((-cx, -cy, -zmin)))
obj.location = (0, 0, 0)
obj.name = name
obj.data.name = name
mat.use_backface_culling = False  # doble cara: ramas finas y bordes sin huecos

bpy.ops.export_scene.gltf(
    filepath=os.path.join(out_dir, name + ".gltf"),
    export_format="GLTF_SEPARATE",
    export_image_format="AUTO",
    export_yup=True,
    export_tangents=True,  # las mismas tangentes con las que se horneó (MikkTSpace)
    use_selection=True,
    export_apply=True,
)
dims = obj.dimensions
print(f"TRIPO_PREPARE {name}: {before} -> {after} triangulos, relieve {bake_size}px, tamano {dims.x:.2f} x {dims.y:.2f} x {dims.z:.2f} m")
