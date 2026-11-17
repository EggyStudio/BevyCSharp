#!/usr/bin/env python3
"""Makes Blender's glTF of a scene from NVIDIA's ORCA follow the archive's own texture convention.

    build/orca-gltf.py <blender.gltf> <textures folder> <out.gltf> [--scale 0.01]
        [--emissive PATTERN=STRENGTH ...] [--double-sided PATTERN ...]

ORCA's scenes, the Amazon Lumberyard Bistro among them, give each material up to four textures by
one name and a suffix, BaseColor with the opacity in its alpha, Specular with occlusion, roughness
and metalness in red, green and blue, Normal in DirectX's form, and Emissive. Blender's FBX import
reads Specular as a specular level and every alpha as a blend, so its glTF puts Specular under
KHR_materials_specular, blends all of it and leaves metalness at glTF's default of one. This pass
makes Specular each material's metallic-roughness texture and its occlusion texture, which read the
same channels as ORCA writes them, cuts a material out at half its alpha where its base color falls
below that anywhere, as Falcor's raster tests it, and keeps every other material opaque, so nothing
is blended. Both sides of a face are drawn where the material's name says `.DoubleSided`, as ORCA
marks its foliage, where it is cut out, or where --double-sided names it, and one side elsewhere, as
Falcor culls them, the closed shapes of the scene's glass and walls hiding their insides. Images are
pointed at the archive's textures where they lie, and those no material uses are dropped, so
build/make-scene-pack.py finds each one it is asked for. The normals' green stays as DirectX writes
it, for make-scene-pack.py's --directx-normals to turn over while it encodes them.

--scale sets the scale of the scene's root nodes, where Blender's import gives the FBX's units
another than the meters a game reads, and --emissive gives the materials whose names hold PATTERN,
in any case, KHR_materials_emissive_strength's STRENGTH, the first pattern that matches winning, so
the scene's lamps and signs glow as brightly as its lighting asks. --double-sided draws both sides
of the materials whose names hold PATTERN, a sheet seen from below such as an awning. Pillow reads
the textures, since the alpha is read from each base color.
"""
import argparse
import json
import os

from PIL import Image

# The suffixes of ORCA's textures, after the material's name and an underscore.
SUFFIXES = ('BaseColor', 'Specular', 'Normal', 'Emissive')


def stem(uri):
    """A texture's material name, its file name without the suffix and the extension."""
    name = os.path.splitext(os.path.basename(uri))[0]
    for suffix in SUFFIXES:
        if name.endswith('_' + suffix):
            return name[:-len(suffix) - 1], suffix
    return name, None


def cut_out(path):
    """Whether a base color's alpha falls below half on more than a thousandth of its texels."""
    image = Image.open(path)
    if 'A' not in image.getbands() and image.mode != 'P':
        return False
    histogram = image.convert('RGBA').getchannel('A').histogram()
    return sum(histogram[:128]) > sum(histogram) / 1000


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('gltf')
    parser.add_argument('textures')
    parser.add_argument('out')
    parser.add_argument('--scale', type=float)
    parser.add_argument('--emissive', nargs='*', default=[])
    parser.add_argument('--double-sided', nargs='*', default=[])
    args = parser.parse_args()

    with open(args.gltf, encoding='utf-8') as file:
        gltf = json.load(file)
    out_folder = os.path.dirname(os.path.abspath(args.out))
    files = {name.lower(): name for name in os.listdir(args.textures)}
    strengths = []
    for given in args.emissive:
        pattern, strength = given.rsplit('=', 1)
        strengths.append((pattern.lower(), float(strength)))

    def path_of(uri):
        name = os.path.basename(uri)
        return os.path.join(args.textures, files.get(name.lower(), name))

    # Each image by its material's name and suffix, and each texture by its image.
    images = {}
    for index, image in enumerate(gltf.get('images', [])):
        images[stem(image['uri'])] = index
    texture_of = {texture['source']: index for index, texture in enumerate(gltf.get('textures', []))}

    for material in gltf.get('materials', []):
        pbr = material.setdefault('pbrMetallicRoughness', {})
        base = pbr.get('baseColorTexture')
        name = stem(gltf['images'][gltf['textures'][base['index']]['source']]['uri'])[0] if base else None
        material.pop('extensions', None)
        pbr.pop('metallicFactor', None)
        pbr.pop('roughnessFactor', None)

        specular = images.get((name, 'Specular'))
        if specular is not None and specular in texture_of:
            pbr['metallicRoughnessTexture'] = {'index': texture_of[specular]}
            material['occlusionTexture'] = {'index': texture_of[specular]}
        else:
            pbr['metallicFactor'] = 0.0

        material.pop('alphaMode', None)
        material.pop('alphaCutoff', None)
        if base and cut_out(path_of(gltf['images'][gltf['textures'][base['index']]['source']]['uri'])):
            material['alphaMode'] = 'MASK'
            material['alphaCutoff'] = 0.5
        both = any(pattern.lower() in material['name'].lower() for pattern in args.double_sided)
        if material['name'].endswith('.DoubleSided') or material.get('alphaMode') == 'MASK' or both:
            material['doubleSided'] = True
        else:
            material.pop('doubleSided', None)

        if 'emissiveTexture' in material:
            for pattern, strength in strengths:
                if pattern in material['name'].lower():
                    extensions = material.setdefault('extensions', {})
                    extensions['KHR_materials_emissive_strength'] = {'emissiveStrength': strength}
                    break

    # The textures and images the materials use, in their order, and the rest dropped.
    used = []

    def keep(slot):
        if slot is not None:
            if slot['index'] not in used:
                used.append(slot['index'])
            slot['index'] = used.index(slot['index'])

    for material in gltf.get('materials', []):
        pbr = material['pbrMetallicRoughness']
        for slot in (pbr.get('baseColorTexture'), pbr.get('metallicRoughnessTexture'),
                     material.get('normalTexture'), material.get('occlusionTexture'),
                     material.get('emissiveTexture')):
            keep(slot)
    textures = [gltf['textures'][index] for index in used]
    sources = []
    for texture in textures:
        if texture['source'] not in sources:
            sources.append(texture['source'])
        texture['source'] = sources.index(texture['source'])
    gltf['textures'] = textures
    gltf['images'] = [gltf['images'][index] for index in sources]
    for image in gltf['images']:
        image['uri'] = os.path.relpath(path_of(image['uri']), out_folder).replace(os.sep, '/')
        image.pop('mimeType', None)

    if args.scale is not None:
        for root in gltf['scenes'][gltf.get('scene', 0)]['nodes']:
            gltf['nodes'][root]['scale'] = [args.scale] * 3

    extensions = [e for e in gltf.get('extensionsUsed', []) if e != 'KHR_materials_specular']
    if any('KHR_materials_emissive_strength' in m.get('extensions', {}) for m in gltf['materials']):
        extensions.append('KHR_materials_emissive_strength')
    gltf['extensionsUsed'] = sorted(set(extensions))
    if not gltf['extensionsUsed']:
        gltf.pop('extensionsUsed')
    required = [e for e in gltf.get('extensionsRequired', []) if e in gltf.get('extensionsUsed', [])]
    if required:
        gltf['extensionsRequired'] = required
    else:
        gltf.pop('extensionsRequired', None)

    with open(args.out, 'w', encoding='utf-8') as file:
        json.dump(gltf, file, separators=(',', ':'), sort_keys=True)

    materials = gltf['materials']
    print(f"{args.out}: {len(materials)} materials, {sum(m.get('alphaMode') == 'MASK' for m in materials)} cut out, "
          f"{sum('doubleSided' in m for m in materials)} drawn on both sides, "
          f"{sum('metallicRoughnessTexture' in m['pbrMetallicRoughness'] for m in materials)} with Specular, "
          f"{sum('emissiveTexture' in m for m in materials)} emissive, {len(gltf['images'])} images")


if __name__ == '__main__':
    main()
