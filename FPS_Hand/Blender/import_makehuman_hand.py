"""Extract MakeHuman's right hand and fit it to the project's viewmodel rig.

Run in Blender background mode with -- <base.obj> <default.mhskel>
<default_weights.mhw> [skin.png]. See FPS_Hand/README.md for sources.
"""

import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

project = Path(__file__).resolve().parents[2]
hand_dir = project / "FPS_Hand"
asset_dir = project / "Assets/JustOneMorePeel/Art/FPS_Hand"
obj_path, rig_path, weights_path, *texture_paths = map(Path, sys.argv[sys.argv.index("--") + 1:])
skin_texture = texture_paths[0] if texture_paths else None

positions, texcoords, body_faces = [], [], []
group = ""
for line in obj_path.read_text(encoding="utf-8").splitlines():
    fields = line.split()
    if not fields:
        continue
    kind, *data = fields
    if kind == "v":
        positions.append(Vector(map(float, data[:3])))
    elif kind == "vt":
        texcoords.append(tuple(map(float, data[:2])))
    elif kind == "g":
        group = data[0]
    elif kind == "f" and group == "body":
        body_faces.append([tuple(int(i) - 1 for i in entry.split("/")[:2]) for entry in data])

skeleton = json.loads(rig_path.read_text(encoding="utf-8"))
source_bones = skeleton["bones"]
source_weights = json.loads(weights_path.read_text(encoding="utf-8"))["weights"]


def joint(bone, end):
    indices = skeleton["joints"][source_bones[bone][end]]
    return sum((positions[i] for i in indices), Vector()) / len(indices)


origin = joint("wrist.R", "head")
along = (joint("finger3-1.R", "head") - origin).normalized()
across = joint("finger5-1.R", "head") - joint("finger2-1.R", "head")
across = (across - along * across.dot(along)).normalized()
dorsal = across.cross(along)


def hand_space(position):
    displacement = position - origin
    return Vector((displacement.dot(across), displacement.dot(along), displacement.dot(dorsal))) * .1


local = [hand_space(position) for position in positions]
faces = [
    face for face in body_faces
    if all(positions[index].x < -3.65 and local[index].y > -.065 for index, _ in face)
]
used = sorted({index for face in faces for index, _ in face})
remap = {index: i for i, index in enumerate(used)}
assert 200 < len(used) < 2000, f"Unexpected right-arm cut: {len(used)} vertices"

digits = ("Thumb", "Index", "Middle", "Ring", "Pinky")
bone_names = {
    f"finger{digit}-{segment}.R": f"{name}_{segment:02}"
    for digit, name in enumerate(digits, 1) for segment in range(1, 4)
}
digit_axes = {}
for digit, name in enumerate(digits, 1):
    base = hand_space(joint(f"finger{digit}-1.R", "head"))
    tip = hand_space(joint(f"finger{digit}-3.R", "tail"))
    digit_axes[name] = (base, (tip - base).normalized(), .08 if digit == 1 else .16)


def shorten(point, digit):
    base, axis, amount = digit_axes[digit]
    return axis * max(0, (point - base).dot(axis)) * amount


skin_offsets = [Vector() for _ in positions]
for source_name, target_name in bone_names.items():
    digit = target_name.split("_")[0]
    for index, weight in source_weights[source_name]:
        skin_offsets[index] += shorten(local[index], digit) * weight
for index in used:
    point = local[index]
    point -= skin_offsets[index]
    # Bring the real forearm axis into the jacket opening without reshaping the palm.
    point.z += min(.023, max(0, (.006 - point.y) * .36))

bpy.ops.wm.open_mainfile(filepath=str(hand_dir / "Blender/FPS_Hand_Source.blend"))
rig = bpy.data.objects["FPS_Hand"]
rig.animation_data_clear()
bpy.data.objects.remove(bpy.data.objects["Hand_Glove"], do_unlink=True)
mesh = bpy.data.meshes.new("MakeHuman right hand")
mesh.from_pydata([local[index] for index in used], [],
                 [[remap[index] for index, _ in face] for face in faces])
mesh.update()
skin = bpy.data.objects.new("Hand_Skin", mesh)
bpy.context.scene.collection.objects.link(skin)
skin.parent = rig
uv = mesh.uv_layers.new(name="Skin UV")
for polygon, face in zip(mesh.polygons, faces):
    polygon.use_smooth = True
    for loop, (_, coord) in zip(polygon.loop_indices, face):
        uv.data[loop].uv = texcoords[coord]

material = bpy.data.materials.new("M_Hand_Skin")
material.diffuse_color = (.47, .27, .18, 1)
material.use_nodes = True
surface = material.node_tree.nodes.get("Principled BSDF")
surface.inputs["Base Color"].default_value = material.diffuse_color
surface.inputs["Roughness"].default_value = .49
mesh.materials.append(material)
if skin_texture is not None:
    # Preserve the original atlas's texel density without importing a whole body texture.
    import numpy as np

    image = bpy.data.images.load(str(skin_texture))
    width, height = image.size[:]
    coords = [texcoords[coord] for face in faces for _, coord in face]
    x0 = max(0, math.floor(min(u for u, _ in coords) * width) - 12)
    x1 = min(width, math.ceil(max(u for u, _ in coords) * width) + 12)
    y0 = max(0, math.floor(min(v for _, v in coords) * height) - 12)
    y1 = min(height, math.ceil(max(v for _, v in coords) * height) + 12)
    pixels = np.empty(width * height * 4, dtype=np.float32)
    image.pixels.foreach_get(pixels)
    cropped = bpy.data.images.new("Hand albedo", x1 - x0, y1 - y0, alpha=False)
    cropped.pixels.foreach_set(pixels.reshape(height, width, 4)[y0:y1, x0:x1].flatten())
    cropped.filepath_raw = str(asset_dir / "HandAlbedo.png")
    cropped.file_format = "PNG"
    cropped.save()
    cropped.pack()
    bpy.data.images.remove(image)
    node = material.node_tree.nodes.new("ShaderNodeTexImage")
    node.image = cropped
    material.node_tree.links.new(node.outputs["Color"], surface.inputs["Base Color"])
    for datum in uv.data:
        u, v = datum.uv
        datum.uv = ((u * width - x0) / (x1 - x0), (v * height - y0) / (y1 - y0))

weights = {index: {} for index in used}
for source_name, influences in source_weights.items():
    group_name = bone_names.get(source_name)
    if group_name is None and source_name.endswith(".R"):
        if source_name.startswith(("wrist", "metacarpal")):
            group_name = "Wrist"
        elif source_name.startswith("lowerarm"):
            group_name = "Forearm"
    if group_name is None:
        continue
    for index, influence in influences:
        if index in weights:
            skin_weights = weights[index]
            skin_weights[group_name] = skin_weights.get(group_name, 0) + influence

groups = {name: skin.vertex_groups.new(name=name) for name in ("Forearm", "Wrist", *bone_names.values())}
for index, influences in weights.items():
    total = sum(influences.values())
    assert total > .98, f"Right hand vertex {index} has incomplete source weights ({total:.4f})"
    cuff_weight = min(1, max(0, (-local[index].y - .012) / .026))
    for name, weight in influences.items():
        groups[name].add([remap[index]], weight / total * (1 - cuff_weight), "REPLACE")
    if cuff_weight > 0:
        groups["Forearm"].add([remap[index]], cuff_weight, "ADD")

bpy.ops.object.select_all(action="DESELECT")
skin.select_set(True)
bpy.context.view_layer.objects.active = skin
smooth = skin.modifiers.new("Smooth skin surface", "SUBSURF")
smooth.levels = 1
bpy.ops.object.modifier_apply(modifier=smooth.name)
skin.select_set(False)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode="EDIT")
for source_name, name in bone_names.items():
    bone = rig.data.edit_bones[name]
    digit = name.split("_")[0]
    head = hand_space(joint(source_name, "head"))
    tail = hand_space(joint(source_name, "tail"))
    bone.head = head - shorten(head, digit)
    bone.tail = tail - shorten(tail, digit)
    bone.align_roll(Vector((0, 0, 1)))
    if name.endswith("_03"):
        tip = rig.data.edit_bones.new(name[:-3] + "Tip")
        tip.head = bone.tail
        tip.tail = bone.tail + (bone.tail - bone.head).normalized() * .006
        tip.parent = bone
        tip.use_deform = False

# The socket lies over the centre of the palm on the palmar side.
socket = rig.data.edit_bones["GripSocket"]
socket.head = Vector((0, .059, -.029))
socket.tail = socket.head + Vector((0, .025, 0))
bpy.ops.object.mode_set(mode="OBJECT")
deform = skin.modifiers.new("Skeleton", "ARMATURE")
deform.object = rig

# Tailor a hollow shirt cuff around the sculpted wrist rather than capping the
# opening with a disk. The return at the lip provides the cloth thickness.
cuff = bpy.data.objects["Shirt_Cuff"]
segments = 32
profile = (
    (-.072, .039, .030), (-.063, .038, .029), (-.041, .032, .026),
    (-.026, .030, .025), (-.023, .0295, .0245), (-.023, .0265, .022),
    (-.027, .0267, .022), (-.048, .029, .024), (-.072, .036, .027),
)
cloth_points, cloth_faces = [], []
for y, radius_x, radius_z in profile:
    for segment in range(segments):
        angle = segment * math.tau / segments
        cloth_points.append((radius_x * math.cos(angle), y, radius_z * math.sin(angle)))
for ring in range(len(profile)):
    current = ring * segments
    following = (ring + 1) % len(profile) * segments
    for segment in range(segments):
        next_segment = (segment + 1) % segments
        cloth_faces.append((current + segment, following + segment,
                            following + next_segment, current + next_segment))
cuff_mesh = bpy.data.meshes.new("Hollow shirt cuff")
cuff_mesh.from_pydata(cloth_points, [], cloth_faces)
cuff_mesh.materials.append(cuff.data.materials[0])
cuff.data = cuff_mesh
for face in cuff_mesh.polygons:
    face.use_smooth = True
cuff.vertex_groups.new(name="Forearm").add(list(range(len(cloth_points))), 1.0, "REPLACE")

# Lengthen the fitted jacket so it reaches beyond the bottom of the view.
sleeve = bpy.data.objects["Suit_Sleeve"]
for vertex in sleeve.data.vertices:
    extension = max(0.0, (-vertex.co.y - .105) / .125)
    vertex.co.y -= .24 * extension
    vertex.co.x *= 1 + .06 * extension
    vertex.co.z *= 1 + .08 * extension

bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(hand_dir / "Blender/FPS_Hand_Refined.blend"))
for obj in (skin, bpy.data.objects["Shirt_Cuff"], sleeve):
    obj.select_set(True)
export = hand_dir / "Export/FPS_Hand_Refined.fbx"
bpy.ops.export_scene.fbx(filepath=str(export), use_selection=True, add_leaf_bones=False,
                         bake_anim=False, use_tspace=True, axis_forward="-Z", axis_up="Y")
print(f"Right hand: {len(mesh.vertices)} vertices, {len(mesh.polygons)} faces, five digit chains")
