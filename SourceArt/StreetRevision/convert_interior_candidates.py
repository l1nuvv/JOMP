import bpy
from pathlib import Path

root = Path(__file__).resolve().parents[2]
source = root / 'SourceArt/InteriorCandidates'
destination = root / 'Assets/JustOneMorePeel/Art/InteriorCandidates'
for name in ('bookcaseopen', 'bookcaseclosed', 'books', 'desk'):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=str(source / (name + '.glb')))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    for obj in meshes:
        print('INTERIOR', name, obj.name, 'dimensions', tuple(round(v, 3) for v in obj.dimensions),
              'materials', [(m.name, tuple(round(v, 3) for v in m.diffuse_color)) for m in obj.data.materials])
    bpy.ops.export_scene.fbx(filepath=str(destination / (name + '.fbx')),
                             object_types={'MESH'}, add_leaf_bones=False,
                             bake_anim=False, path_mode='AUTO')
