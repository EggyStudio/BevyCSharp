Item 70's impostor, a planted tree past twenty of its heights drawn as one quad turned to the eye
and painted from three of sixty-four pictures of the whole tree. Each variant is pictured once an
app from eight by eight ways laid on a square folded over the upper half of the sphere, the square's
edge level with the ground, its full stems and leaves stood in a grid of frames under the world on
the cards' render layer, each copy turned so an orthographic camera sees it from its frame's way,
drawn unlit at twice the 512 texel atlas and halved, with the frames' leaves kept in the smaller
levels as the cards keep theirs. Two normal atlases go beside it, drawn on the CPU from the same
triangles, the normal a leaf's diffuse light is lit by, bent half way out of the crown as the leaf
material bends it, and the leaf's own normal turned to the frame's eye. The quad stands at the
middle of the tree's sphere square to the eye, and square to the light in the light's shadow. Each
pixel finds where its ray meets the planes of the three frames pictured nearest the way the tree is
seen from, the corners of the triangle of the folded square's grid that way falls in, blends them by
their nearness and their opacity, and lights the blend twice as the leaf material lights a leaf,
with the leaf set's mean surface. The impostor casts its shadow and takes none, since its own, cast
by a quad square to the light through the tree's middle, would darken whichever half of it stands
behind that quad. The picture holds each tree at twenty-eight of its heights through a narrow lens,
level and from a third of the way up the sky, as its impostor, its cards and its reduced level from
the left, and below a forest of the three species seen from a hill out to past a kilometer, beside
the sixty-four frames of the black tupelo's fourth variant.

As `dotnet test BevyCSharp.Tests/BevyCSharp.Tests.csproj --filter
AFarTreesImpostorCoversItsCrownAsItsCardsDo --logger "console;verbosity=detailed"` prints them, each
tree at twenty-eight of its heights in the sun under a pale sky, its impostor against its limbs and
cards drawn in its place.

| Tree | View | Impostor, pixels | Cards, pixels | Impostor, brightness | Cards, brightness |
|---|---|---|---|---|---|
| quaking aspen | level | 3075 | 2805 | 135 | 129 |
| quaking aspen | from above | 2886 | 2680 | 140 | 130 |
| black tupelo | level | 4848 | 4186 | 115 | 128 |
| black tupelo | from above | 4651 | 4026 | 122 | 128 |
| California black oak | level | 12131 | 11312 | 129 | 145 |
| California black oak | from above | 11594 | 10906 | 138 | 148 |

The impostor covers the crown at 1.06 to 1.16 of the cards and is as bright within an eighth. Seen
through the narrow lens it is softer than the cards, each frame drawn several times larger than it
was pictured, where past twenty of its heights in a 45 degree picture 1080 pixels high a tree stands
about as tall as its frame holds it.

One fault was found on the way. Lit fully rough at the cards' reflectance, the tupelo's and the
oak's impostors were 100 and 120 against their cards' 128 and 145 by the same command, and darker
than the reduced level with the sun put out, under the sky's light alone, which no turn of a normal
toward the sun changes. The light lost was the sky's sheen the leaves' own gloss gives them where
they are seen at a slant, and the leaf set's mean roughness and metallic mend it. A third atlas of
the leaves' own normals, lighting the light let through and the specular as the leaf material does
in place of the way to the eye, moved the tupelo from 102 to 100 and the oak from 117 to 120 by the
same command, and is kept as the leaf material's own lighting.

`AWayIsBlendedFromTheThreeFramesPicturedNearestIt` prints a way standing at most 22.6 degrees from
the frame it takes most from, the widest cells of the grid lying near the zenith, where the fold
stretches the square most. `AnImpostorsFramesHoldTheWholeTreeFromEveryWay` prints each preset's
sphere, the oak's 15.1 meters across its half in a frame 32.2 meters a side.

The cost, as `TheImpostorsCostIsReadInAFarForest` prints it on the RTX 4070 Laptop GPU, a thousand
black tupelos planted on a disc thirty-eight of their heights off, the nearest some 836 meters away,
at 1920 by 1080, the opaque pass 0.90 and 1.00 ms in two runs with the trees as limbs and cards and
0.55 and 0.49 ms as impostors, and the shadow pass 0.05 ms either way, the sun's shadow reaching no
tree so far off.
