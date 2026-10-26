# Asset requests

What the engine asks for in textures and reference models, written by the review and answered by
whoever brings the files. An answer is a folder under `.ref/`, which the repository ignores; the
review sorts what is usable into `.vegetation` and the import takes it from there. The engine keeps
no asset as it came; every texture is cut, resized, packed and recolored into the collection
described below, so send the largest and cleanest source you have and let the import do the rest.

## What the engine makes of a texture

The collection is `BevyCSharp/Vegetation/Textures` in the library, embedded in its assembly, built
by `build/textures.py` from a manifest that names each source under `.ref/` and the recipe applied
to it, so the import is repeatable. Every entry is a set of three files, each a KTX2 with mipmaps,
block-compressed for the GPU and supercompressed with zstd, read by Bevy's own loader.

| File | Channels | Encoding |
|---|---|---|
| `albedo.ktx2` | color in RGB, opacity in A | BC7 |
| `normal.ktx2` | tangent-space normal in RG | BC5 |
| `surface.ktx2` | occlusion in R, roughness in G, metallic in B, translucency in A, as Bevy reads them | BC7 |

A bark set is tileable and no larger than 512 by 1024 or 512 square, its normal and surface at half
that. A leaf set is one atlas of up to eight leaves with alpha, no larger than 1024 by 1024, its
translucency from the source's subsurface color or derived from the albedo where the source has
none. A species costs at most 3 MB on disk and the whole collection at most 32 MB, which a test
holds.

## The sorted sources

The hidden folder `.vegetation` holds the usable textures of the first delivery, sorted by kind so
the import reads them by path: `bark/<species>`, `leaves/<species>`, `branches/<card>` and
`scans/<species>`, a set a folder, its files named `albedo`, `opacity`, `normal`, `gloss` or
`specular`, `subsurface`, `ao` and `height` as the source had them. Its README lists every set and
what each gives. Fifteen barks, thirteen leaf sets with the fall maples and the ginkgo, sixteen
branch and cap cards, and scans of alder, sycamore and fall leaves. The tree models beside them stay
reference for the grown trees' outlines and are not imported.

## Requested

What the presets lack and what a forest needs beside trees. A set is color, normal and roughness or
gloss, with opacity and a subsurface or translucency map for leaves, at 2048 or larger, as PNG or
JPEG, tileable where it is bark or ground.

| What | For | Form |
|---|---|---|
| willow leaves, narrow and drooping | the weeping willow preset | a leaf set or scans with alpha |
| oak leaves and oak bark | the California black oak preset | a leaf set and a tileable bark |
| black tupelo bark | the tupelo preset | a tileable bark |
| fern fronds | the fern preset | scans with alpha, several fronds |
| grass blades and a meadow tuft | the feature test's meadow and a forest floor | cards with alpha |
| leaf litter and moss | the forest floor | tileable ground sets |
| ivy or a climbing vine | walls and trunks | a leaf set with alpha |
| a spruce or fir branch with its bark | a conifer beside the pine | a branch card set and a tileable bark |
| a palm frond and trunk | a tropical preset | a frond set and a tileable bark |
| a small level saved by TrenchBroom, the Generic game in the Valve format, a few brushes with one face's texture rotated and scaled, a point entity with two keys and a brush entity | the level loader's fixture of item 65, so the round trip is read against the tool's own file | the .map text file as saved |

## How to hand an asset over

Put the files in a folder under `.ref/` and say so; the review sorts what is usable into
`.vegetation` and the import takes it from there.

| Delivery | Holds | Sorted |
|---|---|---|
| 2026-10-25, three folders | fifteen trees' sets, leaf scans, twelve models | yes, 284 files |
