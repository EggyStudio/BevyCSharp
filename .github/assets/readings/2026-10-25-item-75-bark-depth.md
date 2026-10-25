The bark's depth, a fourth file of each bark set, as `build/textures.py` imports it. The hornbeam's
comes from its source's height map, stretched from its narrow band of grays, and the silver birch's,
the broadleaf's and the weeping willow's from their normal maps' slopes, integrated through the
Fourier transform with the waves longer than a quarter of the picture faded out, which held the
willow's depth level where its normal map drifted across its width. Integrated so, the hornbeam's
normal map gives a height that follows its own height map more closely with the slope down a column
taken as the normal's Y than as minus it, which chose the sign.

| Set | Depth | On disk | Error |
|---|---|---|---|
| silver-birch-bark | 256 by 256 | 40 KB | 42.4 dB |
| broadleaf-bark | 128 by 512 | 40 KB | 38.0 dB |
| weeping-willow-bark | 128 by 512 | 42 KB | 39.9 dB |
| hornbeam-bark | 128 by 256 | 21 KB | 42.4 dB |

The tree material cuts the grooves a fortieth of a stem's circumference deep through twelve layers
of parallax occlusion, on bark alone. On the RTX 4070 Laptop GPU, 24 black tupelos at 1920 by 1080
drew their opaque pass in 5.09 to 5.22 ms with flat bark and 5.21 to 5.55 ms with depth over four
runs, as `dotnet test BevyCSharp.Tests/BevyCSharp.Tests.csproj --filter
TheDepthsCostIsReadInAForest --logger "console;verbosity=detailed"` prints them, the depth's cost a
tenth to a half of a millisecond where the leaves' cards hold most of the pass.
