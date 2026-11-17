"""Turns an FBX into glTF inside Blender, run by build/fbx-to-gltf.sh.

    blender -b --factory-startup --python build/fbx-to-gltf.py -- <in.fbx> <out.gltf>

The FBX is imported with its custom normals and without its animation, and written as glTF with its
buffer beside it, the textures pointed at where the FBX names them rather than copied, since the
scene pack's encoding reads each from its source, and its cameras left out, since a camera in a
scene a game spawns would draw the game twice. Its lights are written as KHR_lights_punctual, which
holds points, spots and suns, so each light Blender read is said by its kind before the export, and
one of a kind glTF has no light for is said as left out, so the scene's own record can name the
lights that did not carry over.
"""
import sys
from collections import Counter

import bpy

fbx, out = sys.argv[sys.argv.index('--') + 1:][:2]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx, use_anim=False, use_image_search=False, use_custom_normals=True)

kinds = Counter(obj.data.type for obj in bpy.data.objects if obj.type == 'LIGHT')
carried = {'POINT': 'point', 'SPOT': 'spot', 'SUN': 'directional'}
for kind, count in sorted(kinds.items()):
    print(f'{count} {kind.lower()} lights, ' + (f'written as {carried[kind]}' if kind in carried else 'left out'))
if not kinds:
    print('no lights')

meshes = [obj for obj in bpy.data.objects if obj.type == 'MESH']
triangles = sum(sum(len(polygon.vertices) - 2 for polygon in obj.data.polygons) for obj in meshes)
print(f'{len(meshes)} meshes, {triangles} triangles, {len(bpy.data.materials)} materials, Blender {bpy.app.version_string}')

bpy.ops.export_scene.gltf(
    filepath=out, export_format='GLTF_SEPARATE', export_keep_originals=True, export_cameras=False,
    export_lights=True, export_animations=False, export_yup=True, export_apply=False,
    export_extras=False, export_tangents=False, export_import_convert_lighting_mode='SPEC')
