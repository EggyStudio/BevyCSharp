The first part of item 76, each leaf's depth in its crown. The leaves' area is gathered into a grid
of 24 voxels across the crown's longest side, and the sky a leaf sees is the light let through
along the zenith and a ring of eight ways a third of the way down from it by Beer and Lambert's law,
the leaf area times the half of it facing a ray, weighted as a cosine weights the sky. It is written
as each card's vertex gray, from the whole of the leaf's color where the sky reaches it to half
where none does, which Bevy multiplies into the base color, so it darkens the ambient, the indirect
and the transmitted light, and the direct sun a little, which the shadow map darkens as well. The
cards keep their own normals, turned round to face the eye as Bevy's double-sided materials turn
them, and nothing is added a pixel.

The presets' leaves, as `dotnet test BevyCSharp.Tests/BevyCSharp.Tests.csproj --filter
ALeafDeepInACrownTakesLessOfTheSkyThanOneOnItsRim --logger "console;verbosity=detailed"` prints
them, a deep leaf within a third of the crown's middle and the upper rim's past eighty-five
hundredths of its bounding ellipsoid.

| Leaves | Darkest | Deep, mean | Upper rim, mean |
|---|---|---|---|
| silver-birch-leaves, the aspen | 0.57 | 0.72 | 0.89 |
| red-alder-leaves, the tupelo | 0.54 | 0.71 | 0.89 |
| weeping-willow-leaves | 0.61 | 0.78 | 0.92 |
| shingle-oak-leaves, the oak | 0.69 | 0.79 | 0.96 |
| hornbeam-leaves, the bush | 0.52 | 0.55 | 0.87 |

Drawn, the change is gentle. The aspen and the tupelo in the sun under a pale sky, drawn with the
gray and without it from the same place, keep 0.93 and 0.95 of their brightness through the middle
of their outlines and 0.97 at their edges, as
`ACrownShadedByTheSkyItSeesDarkensItsMiddleMoreThanItsEdge` prints, the leaves seen through a
crown's middle being its rim's in front as well as its deep ones.
The picture holds the pair, without the gray on the left and with it on the right.

The bend of each leaf's normal toward the way out of the crown was tried baked into the cards'
normals and is left for the item's second part. Bevy turns a double-sided card's normal round to
face the eye, so a baked bend lit half the cards inward, and drawn with one normal for both sides
the bent normals of a crown seen from below pointed up at the sky and mirrored it, a gray and blue
sheen across the crown and white patches on the sun's side. The second part bends the normal for
the diffuse light alone in a leaf material of the library's, the specular and the transmitted light
keeping the card's own.
