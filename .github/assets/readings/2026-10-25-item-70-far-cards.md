Item 70's far level, a planted tree past six of its heights drawn as its sixteen thickest stems,
with a third of their sides, and the branches and leaves each one carries through thinner stems on
two crossed cards. A card's plane is the one its leaves spread most in, upright, facing out of the
crown, and the second card is turned a quarter round its upright from the first, since one card
alone thins to a line seen along it. Each variant's cards are pictured once an app, the stems and
leaves of each card stood in a cell of a grid under the world on a render layer of their own, turned
to face an orthographic camera and scaled to fill the cell, drawn unlit into a 512 texel atlas,
read back, and given their smaller levels with each card's leaves kept as the leaf sets' levels
keep theirs. A normal map of the leaves each card pictures, drawn on the CPU from the same cards and
stems in the card's frame, goes beside it, and the cards are lit by the leaf material with the leaf
set's mean translucency and occlusion, which the import records, fully rough, since a texel of a
card stands for many leaves turned every way. The picture holds each tree's far level on the left
of its reduced level at eight of its heights, and below each tree's atlas of the fourth variant.

As `dotnet test BevyCSharp.Tests/BevyCSharp.Tests.csproj --filter
AFarTreeOfCardsCoversItsCrownAsTheReducedOneDoes --logger "console;verbosity=detailed"` prints
them, each tree at eight of its heights in the sun under a pale sky, the far level against the
reduced level built by hand at the same place, turn and size.

| Tree | Far level, pixels | Reduced, pixels | Far level, brightness | Reduced, brightness |
|---|---|---|---|---|
| quaking aspen | 735 | 774 | 113 | 135 |
| black tupelo | 1115 | 1372 | 127 | 125 |
| California black oak | 3020 | 3036 | 124 | 139 |

The cards cover the crown within a fifth and are as bright within a sixth, a little darker where
the leaves' own turning lights them less than a card's average does. Three faults were found on the
way. A single card a branch covered the aspen's crown at 0.6 of the reduced level and thinned the
tupelo's, which the crossed card mends. The flat pictures lit by the cards' own face were up to half
again as bright as the leaves, which the normal map and the set's surface mend. And the camera's
picture is read back sRGB, which read as linear made every card brighter still.

The cost, as `TheFarLevelsCostIsReadInADistantForest` prints it on the RTX 4070 Laptop GPU, 48 black
tupelos planted on a disc 160 meters off at 1920 by 1080, the shadow pass 0.97 ms with the trees in
their reduced level and 0.42 ms in their far one, the opaque pass 3.20 and 2.95 ms, the far cards'
overdraw lit twice as the leaves are.
