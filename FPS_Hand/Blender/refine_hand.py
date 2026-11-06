import bpy
import math
from mathutils import Vector
from pathlib import Path

root = Path(__file__).resolve().parent.parent
bpy.ops.wm.open_mainfile(filepath=str(root / "Blender/FPS_Hand_Source.blend"))
glove = bpy.data.objects["Hand_Glove"]
rig = bpy.data.objects["FPS_Hand"]
# Soft raised knuckles and tendons on the back of the glove.
knuckles = []
for name in ("Index_01", "Middle_01", "Ring_01", "Pinky_01"):
    bone = rig.data.bones[name]
    knuckles.append((bone.head_local.x, bone.head_local.y))

for vertex in glove.data.vertices:
    x, y, z = vertex.co
    if z <= 0:
        continue
    dorsal = min(1.0, z / .009)
    knuckle = sum(
        math.exp(-((x - kx) / .011)**2 - ((y - ky) / .013)**2)
        for kx, ky in knuckles
    )
    palm = math.exp(-((x + .002) / .031)**2 - ((y - .048) / .032)**2)
    vertex.co.z += dorsal * (.00075 * knuckle + .0006 * palm)

glove.data.update()
bpy.context.preferences.filepaths.save_version = 0
source = root / "Blender/FPS_Hand_Refined.blend"
bpy.ops.wm.save_as_mainfile(filepath=str(source))
bpy.ops.object.select_all(action="DESELECT")
rig.select_set(True)
for name in ("Hand_Glove", "Shirt_Cuff", "Suit_Sleeve"):
    bpy.data.objects[name].select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(
    filepath=str(root / "Export/FPS_Hand_Refined.fbx"),
    use_selection=True,
    add_leaf_bones=False,
    bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,
    use_tspace=True,
    axis_forward="-Z",
    axis_up="Y",
)
print("REFINED_HAND_VERTICES", len(glove.data.vertices))
