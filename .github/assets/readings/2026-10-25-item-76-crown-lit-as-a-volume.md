The second part of item 76, a crown's diffuse light by normals bent out of it. Each card carries the
way out of the crown at it, the way the leaf area of the crown's grid, smoothed over a voxel each
side, falls fastest, leaning a quarter of the way toward the way from the crown's middle, folded
octahedrally into the mesh's second texture coordinates, Bevy's `UV_1`, which MeshData gains as
`SecondUvs`. The bridge's leaf material is Bevy's standard material extended by a fragment shader
of its own, which lights each pixel twice from Bevy's own lighting, once by the leaf's normal bent
half way toward the way out with no specular and nothing let through, for its diffuse light, and
once by the leaf's own normal turned round to face the eye with no diffuse light, for its specular
and the light let through it, and adds the two. The leaves are drawn forward whatever the camera.

Each crown with the leaf material from its cards with the way out and from the same cards without
it, which the material lights by their own normals throughout, as
`dotnet test BevyCSharp.Tests/BevyCSharp.Tests.csproj --filter
ACrownsDiffuseLightBentOutOfItShowsTheSunsSideAndMirrorsNoSky --logger "console;verbosity=detailed"`
prints, the leaves' mean brightness over the third of the crown toward a grazing sun and the third
away, and their mean color from below under a high sun.

| Crown | Own normals, far | Own normals, toward the sun | Bent, far | Bent, toward the sun |
|---|---|---|---|---|
| quaking aspen | 91.6 | 100.4 | 96.3 | 125.1 |
| California black oak | 126.3 | 137.3 | 118.4 | 160.2 |

The side toward the sun stands 28.8 brighter than the far side in the aspen's crown against 8.8 by
the leaves' own normals, and 41.8 in the oak's against 11.0. From below, the aspen's leaves are 99,
115 and 90 in red, green and blue by their own normals and 106, 123 and 97 bent, a little brighter
and the same green, where the bend baked into the cards' normals in the item's first trial turned
them blue toward the sky with white patches. The picture holds each pair, the leaves' own normals on
the left and bent on the right, the aspen and the oak in the grazing sun from the right and the
aspen from below.

The cost, as `TheLeavesLitAsACrownCostIsReadInAForest` prints it on the RTX 4070 Laptop GPU, 24
black tupelos at 1920 by 1080 under the atmosphere's sky, the opaque pass 5.19 ms with the leaves
lit by the standard material and 7.52 ms lit as a crown. In the feature test at 1280 by 720, read
as the forest's reading reads it, the opaque pass with the levels of detail goes from 0.22 to 0.28
ms in the meadow, from 0.33 to 0.42 in the forest and from 0.15 to 0.26 from above.
