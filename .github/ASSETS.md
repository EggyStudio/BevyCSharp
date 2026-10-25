# Asset requests

What the engine asks for in textures and reference models, written by the review and answered by
whoever brings the files. An answer is a folder under `.ref/`, which the repository ignores, and one
line an asset in the table at the end naming its source and its license. Nothing ships without both.
The engine keeps no asset as it came; every texture is cut, resized, packed and recolored into the
collection described below, so send the largest and cleanest source you have and let the import do
the rest.

## What the engine makes of a texture

The collection is `assets/textures` in the library, built by `build/textures.py` from a manifest that
names each source under `.ref/` and the recipe applied to it, so the import is repeatable. Every
entry is a set of three files, each a KTX2 with mipmaps and Basis Universal supercompression, read
by Bevy's loader and transcoded to the GPU's block format on load.

| File | Channels | Encoding |
|---|---|---|
| `albedo.ktx2` | color in RGB, opacity in A | ETC1S, UASTC where the alpha has edges |
| `normal.ktx2` | tangent-space normal in RG | UASTC |
| `surface.ktx2` | roughness in R, translucency in G, occlusion in B | ETC1S |

A bark set is tileable and no larger than 1024 by 1024, a strip 512 by 1024. A leaf set is one
atlas of up to eight leaves with alpha, no larger than 1024 by 1024, its translucency from the
source's subsurface color or derived from the albedo where the source has none. A species costs at
most 3 MB on disk and the whole collection at most 32 MB, which a test holds. THIRD-PARTY-NOTICES.md
carries every source's author and license from the manifest.

## Taken from the folders of 2026-10-25

Read in `.ref/Vegetation`, `.ref/vegetation2` and `.ref/vegetation3`. The models (OBJ and MAX files
of 125 to 427 MB) stay as reference for the grown trees' outlines and are not imported.

| Entry | Source | Maps in the source | For |
|---|---|---|---|
| bark, silver birch | vegetation2 Map(4), 4096 | color, normal, gloss, specular | the quaking aspen preset, its nearest kin |
| leaves, birch | vegetation2 Map(4), 4096 | color, opacity, normal, gloss | the aspen preset |
| bark, grey willow | vegetation2 text, 2048 by 4096 | color, normal | the weeping willow preset |
| bark, hornbeam | Vegetation Carpinus_Betulus Map, 2048 by 4096 | color, normal, gloss, occlusion, height | the bush preset and a hedge |
| leaves, hornbeam | Vegetation Carpinus_Betulus Map, 4096 | color, opacity, normal, gloss, subsurface | the bush preset |
| bark, silver maple | Vegetation Acer_Saccharinum Map, 4096 | color, normal, gloss, specular, occlusion | a maple preset |
| leaves, silver maple | Vegetation Acer_Saccharinum Map, 4096 | color, opacity, normal, gloss, subsurface | a maple preset |
| leaves, maple in fall | vegetation2 3diffrent Map, 4096 | color, normal, opacity, gloss, subsurface, three species | the seasonal tint of the maples |
| bark and needles, Scots pine | vegetation2 Map(2), 512 by 1024 | color, normal, opacity, subsurface | a pine preset |
| bark and needles, Monterey cypress | vegetation3 map, 512 by 2048 | color, normal, opacity | a cypress preset |
| bark, acacia | vegetation3 Map, 2048 | color, normal, opacity | a savanna tree |
| bark, European linden | vegetation2 Map(1), 1024 by 512 | color, normal, opacity, with a leaf | a linden preset |
| bark, red maple | vegetation2 map(2), 512 by 2048 | color, normal, opacity, with a leaf | the maple preset's second bark |
| leaves, alder scans | Vegetation Red_Alder, 48 scans with opacity | color, opacity | the black tupelo preset's oval leaves |
| leaves, sycamore scans | Vegetation Sycamore, 18 scans with opacity | color, opacity | the California black oak preset's lobed leaves, until an oak comes |
| leaves, fall scans | Vegetation Various-Fall-Leaves, 80 scans | color | leaf litter and the fall tints |

## Left out

| What | Why |
|---|---|
| Giant kelp, green algae and red algae scans | under water, not a tree's; kept in mind for the water's plants |
| the dried vegetation and the loose leaf set, 1024 cutouts | low in size and without normals; the scans above cover them |
| the indoor plant set and the HDR | not vegetation for a world |
| the bamboo set, 480 by 780 | too small to tile; a bamboo comes with a request below |
| the generic PBR sets in Vegetation/textures | unnamed; the coder reads their albedos and takes a ground or a moss if one is there |
| the material libraries (Corona, V-Ray, standard) and the MAX files | for other renderers; unreadable here |

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

## How to hand an asset over

Put the files in a folder under `.ref/` and add a row here. An asset without a source and a license
line is read for reference and never imported.

| Folder | Source | Author | License | Note |
|---|---|---|---|---|
| `.ref/Vegetation` | | | | the folder of 2026-10-25 |
| `.ref/vegetation2` | | | | the folder of 2026-10-25 |
| `.ref/vegetation3` | | | | the folder of 2026-10-25 |
