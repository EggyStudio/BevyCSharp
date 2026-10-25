A planted tree is drawn in two levels of detail, its full level within two and a half of its
heights of the camera and its reduced level past three, Bevy's visibility range dithering one into
the other between. The reduced level has half the tube sides, leaves out the stems thinner where
they start than a twenty-fifth of the trunk, and draws every other leaf of the full level the
square root of two larger about its stalk. Its stems, as `dotnet test
BevyCSharp.Tests/BevyCSharp.Tests.csproj --filter AReducedTreeKeepsItsTrunkAndLimbsInFewerTriangles
--logger "console;verbosity=detailed"` prints them, are 4,408 triangles of the aspen's 12,482, 3,564
of the tupelo's 12,106 and 8,948 of the oak's 51,236, and its leaves half the cards.

Each tree alone against the sky at two and a half of its heights, from the side and from 37
degrees above, as `AReducedTreeCoversItsCrownAsTheFullOneDoesWhereTheyChange` prints them, the
pixels of leaf and the rest of the tree, whole and reduced.

| Tree | From | Leaves, whole | Leaves, reduced | The rest, whole | The rest, reduced |
|---|---|---|---|---|---|
| quaking aspen | the side | 3749 | 4057 | 977 | 801 |
| quaking aspen | above | 3414 | 3712 | 850 | 700 |
| black tupelo | the side | 2972 | 3324 | 3238 | 3085 |
| black tupelo | above | 2961 | 3289 | 2389 | 2183 |
| weeping willow | the side | 7178 | 7537 | 2923 | 2289 |
| weeping willow | above | 7782 | 8183 | 2344 | 1711 |
| California black oak | the side | 9886 | 10250 | 2542 | 1522 |
| California black oak | above | 9002 | 9145 | 2160 | 1314 |
| bush | the side | 12818 | 12566 | 2851 | 3380 |
| bush | above | 13429 | 13411 | 1612 | 1852 |

The reduced level's larger cards cover a little more, up to an eighth for the tupelo, and the twigs
it leaves out, which showed between the leaves as lines a pixel wide, cover less, the whole tree
within a tenth. The picture holds each pair, whole on the left and reduced on the right, from the
side and from above.

With the levels the trees near the camera in the feature test cast no shadow. Bevy 0.20 measures a
directional light's cascades from the camera they belong to by looking that camera up among the
render world's views by its main world entity, which finds nothing, and falls back to its shadow
origin, the entity carrying `ShadowLodOrigin`, else the first camera drawing to a window, else the
first camera of all. Offscreen that was a light probe's face camera, so a near tree's full level was
dithered out of the shadow by the probe's distance while its reduced level was left out of the
casters as too near the scene's camera. The bridge gives each 3D camera drawing the game's picture
`ShadowLodOrigin`, giving way to one the game gives itself, and the shadows came back.

The feature test's three views of the forest's reading, each read twice with the levels and twice
with every tree held at its full level by a build whose switching distance was out of reach, in
`./bcs open --feature-test --offscreen --frames 0 --frame-time 0.0166667 --hold loads,commands
--timings` with the camera set by `./bcs command look`, 120 frames waited and the passes read with
`./bcs command render.timings`, on the RTX 4070 Laptop GPU at 1280 by 720.

| View | Shadows, full | Shadows, levels | Prepass, full | Prepass, levels | Opaque, full | Opaque, levels |
|---|---|---|---|---|---|---|
| meadow | 2.31 to 2.76 ms | 3.02 to 3.49 ms | 0.60 to 0.61 ms | 0.97 to 1.50 ms | 0.43 to 0.44 ms | 0.22 ms |
| forest | 2.99 to 3.04 ms | 2.61 to 2.79 ms | 0.35 to 0.38 ms | 0.37 ms | 0.68 ms | 0.32 to 0.33 ms |
| above | 1.89 to 3.16 ms | 0.76 to 0.84 ms | 0.58 to 0.59 ms | 0.25 ms | 0.41 to 2.09 ms | 0.15 to 0.20 ms |

From above, where every tree stands past its switch, the shadows cost a quarter to a half of what
the full trees' do and the opaque pass under half. From inside the forest the opaque pass halves.
In the meadow the ring's trees stand in their bands, between 32 and 39 meters for an aspen, where
both levels draw, and the shadows and the prepass cost more than the full trees alone. A tree in
its band shows Bevy's dither as a speckle in the feature test, which draws with SMAA and has no
temporal antialiasing to blend it, darker than either level. The frame's whole GPU time swung
between 11 and 28 ms from run to run, more than the trees change, and is not read here.
