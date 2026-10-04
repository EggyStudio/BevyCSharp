# Materials and textures

How a surface looks, as Bevy's physically based material, and the images it is made from.

### Materials

A material takes settings, and its textures are image handles:

```csharp
var crate = Render.CreateMaterial(new MaterialSettings
{
    BaseColorTexture = AssetServer.Load(AssetKind.Image, "textures/crate.png"),
    NormalMap = AssetServer.Load(AssetKind.Image, "textures/crate-normal.png"),
    Roughness = 0.8f,
});

var glass = Render.CreateMaterial(new MaterialSettings
{
    BaseColor = (0.8f, 0.9f, 1f, 0.25f),
    AlphaMode = AlphaMode.Blend,
});
```

A texture is combined with its matching factor rather than replacing it, so a base color map on the
default white shows unchanged and tinting it is a matter of setting a color. The image need not have
finished loading, because the material holds a handle rather than pixels. Five maps are bound this
way: base color, normal, metallic-roughness, emissive and occlusion.

`AlphaMode` decides what happens where a material is not opaque. `Mask` draws a pixel or skips it,
deciding at `AlphaCutoff`, so the surface still writes depth and nothing has to be sorted, which
suits foliage and fences. `Blend` is real transparency, drawn after everything else and sorted back
to front. `Add` adds to what is behind, so it never darkens it. `DoubleSided` draws back faces, for
anything modeled as a single sheet, and `Unlit` shows the base color flat.

The finer surface is there as well. `Reflectance` is how much a non-metal reflects head on,
`Clearcoat` and `ClearcoatRoughness` lay a glossy varnish over the base, as on a car's paint, and
`Transmission`, `DiffuseTransmission`, `Thickness` and `RefractiveIndex` let light through, straight
as through glass or scattered as through a leaf:

```csharp
var water = Render.CreateMaterial(new MaterialSettings
{
    BaseColor = (0.6f, 0.8f, 0.9f, 1f),
    Roughness = 0.05f,
    Transmission = 0.9f,
    Thickness = 0.5f,
    RefractiveIndex = 1.33f,
    AttenuationDistance = 2f,               // a ray this far inside has taken on the color
    AttenuationColor = (0.2f, 0.6f, 0.7f, 1f),
});
```

`AttenuationDistance` and `AttenuationColor` tint light on its way through, so thick glass is
greener at its edge than its face. `AnisotropyStrength` and `AnisotropyRotation` stretch the
highlight along the mesh's tangents, as brushed metal's is stretched. Each of these has a map
beside it (`ClearcoatTexture`, `ClearcoatRoughnessTexture`, `ClearcoatNormalTexture`,
`TransmissionTexture`, `DiffuseTransmissionTexture`, `ThicknessTexture`, `AnisotropyTexture`), and a
glTF file's clearcoat, transmission and anisotropy extensions fill them as it loads.

A material file and a scene write these only where they differ from a plain material's, and the
editor's Material card keeps them in a Surface fold, with their maps in a Surface maps fold.

### Textures

How one is sampled is decided when it loads:

```csharp
var floor = AssetServer.LoadImage("textures/tiles.png", TextureSettings.Tiling);
var bumps = AssetServer.LoadImage("textures/tiles-normal.png", TextureSettings.Data);
```

`Tiling` repeats and filters linearly; `Data` filters linearly and reads the file as raw values
rather than as sRGB, as a normal, roughness or occlusion map needs. Individual settings are there
for anything else, including anisotropy, which is dropped rather than refused if the filters are not
all linear, because the graphics API treats that pair as a validation failure.

Tiling takes both halves. A mesh's UVs run from zero to one however large it is, so a repeating
texture still shows one stretched copy until the material scales them with `UvScale = (12f, 12f)`.
PNG, JPEG, WebP, BMP and TGA decode in every build, headless included, because that is work on data
rather than on a GPU.

---

Before this, [Drawing](drawing.md).
Next, [Shaders](shaders.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#3d-rendering). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#drawing). The [guide's contents](../README.md#guide) list every page.
