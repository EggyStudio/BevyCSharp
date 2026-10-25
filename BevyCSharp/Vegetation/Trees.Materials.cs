using System.Runtime.CompilerServices;

namespace Bevy;

// The materials a tree is drawn with, its bark's and its leaves', from the species' sets of the
// collection or from the pictures the generator makes, each made once an app for each look.
public static partial class Trees
{
    private static readonly ConditionalWeakTable<App, Dictionary<string, AssetHandle>> Made = new();

    /// <summary>The material a species' bark is drawn with. Only valid inside a system.</summary>
    /// <param name="species">The species, whose <see cref="TreeSpecies.BarkSet"/> or <see cref="TreeSpecies.BarkColor"/> it is made from.</param>
    /// <returns>The material, the same one for the same bark while the app runs.</returns>
    /// <remarks>
    /// <para>
    /// The bark set's images where the collection has the set the species names, the surface giving
    /// the occlusion and the roughness, and otherwise a bark of ridges made from noise, tinted by the
    /// species' bark color. Either repeats once round each stem and along it in lengths of the stem's
    /// circumference, as <see cref="EditMesh.Tube"/> lays its coordinates, so a thick trunk's bark is
    /// coarser than a twig's.
    /// </para>
    /// <para>
    /// The bark takes depth by parallax occlusion from the set's depth, or the noise's, its grooves
    /// cut a fortieth of the stem's circumference deep, about two and a half centimeters on a trunk
    /// thirty across, through twelve layers at most, so its ridges hide the grooves behind them as the
    /// eye moves round a trunk while the stem's outline stays round.
    /// </para>
    /// </remarks>
    public static AssetHandle BarkMaterial(TreeSpecies species)
    {
        ArgumentNullException.ThrowIfNull(species);
        var set = species.BarkSet is { } name && TreeTextures.Find(name) is { Kind: TreeTextureKind.Bark } found ? found : null;
        var key = set is null ? $"bark {Key(species.BarkColor)}" : $"bark {set.Name}";
        return Once(key, () =>
        {
            if (set is not null)
            {
                var (albedo, normal, surface) = set.Load();
                return Render.CreateMaterial(Deep(new MaterialSettings
                {
                    BaseColorTexture = albedo,
                    NormalMap = normal,
                    OcclusionTexture = surface,
                    MetallicRoughnessTexture = surface,
                    Roughness = 1f,
                    Metallic = 1f,
                }, set.LoadDepth()));
            }

            var (color, bumps, depth) = Generated("bark pictures", () =>
            {
                var (pixels, normals, depths) = TreeLooks.Bark();
                return (Image(pixels, TreeLooks.BarkWidth, TreeLooks.BarkHeight, srgb: true),
                    Image(normals, TreeLooks.BarkWidth, TreeLooks.BarkHeight, srgb: false),
                    Image(depths, TreeLooks.BarkWidth, TreeLooks.BarkHeight, srgb: false));
            });
            var tint = species.BarkColor;
            return Render.CreateMaterial(Deep(new MaterialSettings
            {
                BaseColor = (tint.R, tint.G, tint.B, 1f),
                BaseColorTexture = color,
                NormalMap = bumps,
                Roughness = 0.9f,
            }, depth));
        });
    }

    /// <summary>The material a species' leaves are drawn with, turned toward its fall color by a share. Only valid inside a system.</summary>
    /// <param name="species">The species, whose <see cref="TreeSpecies.LeafSet"/>, or leaf shape and colors, it is made from.</param>
    /// <param name="fall">How far into the fall the leaves are, from 0, summer, to 1, the species' <see cref="TreeSpecies.FallColor"/>.</param>
    /// <returns>The material, the same one for the same leaves and share while the app runs.</returns>
    /// <remarks>
    /// <para>
    /// The cards of <see cref="BuildLeaves"/> are cut out by the leaves' opacity at one half and drawn
    /// from both sides, and light shows through them by their translucency, the leaf set's where it
    /// has one, so a crown lit from behind lets the light through at its edges.
    /// </para>
    /// <para>
    /// A leaf set's pictures are tinted so their mean color becomes the fall color as the share
    /// reaches one, and the generated leaves are drawn in the species' leaf color turning to its fall
    /// color. The share is kept to a hundredth, so a game easing it over a season makes a hundred
    /// materials at most.
    /// </para>
    /// </remarks>
    public static AssetHandle LeafMaterial(TreeSpecies species, float fall = 0f)
    {
        ArgumentNullException.ThrowIfNull(species);
        fall = MathF.Round(Math.Clamp(float.IsFinite(fall) ? fall : 0f, 0f, 1f), 2);
        var set = species.LeafSet is { } name && TreeTextures.Find(name) is { Kind: TreeTextureKind.Leaves } found ? found : null;
        var key = set is null
            ? $"leaves {species.LeafShape} {species.LeafScaleX} {Key(species.LeafColor)} {Key(species.FallColor)} {fall}"
            : $"leaves {set.Name} {Key(species.FallColor)} {fall}";

        return Once(key, () =>
        {
            if (set is not null)
            {
                var (albedo, normal, surface) = set.Load();
                var mean = set.MeanColor;
                (float, float, float, float) tint = (Toward(1f, species.FallColor.R / MathF.Max(mean.R, 1e-3f), fall), Toward(1f, species.FallColor.G / MathF.Max(mean.G, 1e-3f), fall), Toward(1f, species.FallColor.B / MathF.Max(mean.B, 1e-3f), fall), 1f);
                return Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = tint,
                    BaseColorTexture = albedo,
                    NormalMap = normal,
                    OcclusionTexture = surface,
                    MetallicRoughnessTexture = surface,
                    DiffuseTransmissionTexture = surface,
                    DiffuseTransmission = 1f,
                    Roughness = 1f,
                    Metallic = 1f,
                    AlphaMode = AlphaMode.Mask,
                    DoubleSided = true,
                });
            }

            var atlas = Generated($"leaf pictures {species.LeafShape} {species.LeafScaleX}", () => Image(TreeLooks.Leaves(species.LeafShape, species.LeafScaleX), TreeLooks.LeafSize, TreeLooks.LeafSize, srgb: true));
            var (summer, autumn) = (species.LeafColor, species.FallColor);
            return Render.CreateMaterial(new MaterialSettings
            {
                BaseColor = (Toward(summer.R, autumn.R, fall), Toward(summer.G, autumn.G, fall), Toward(summer.B, autumn.B, fall), 1f),
                BaseColorTexture = atlas,
                DiffuseTransmission = 0.4f,
                Roughness = 0.6f,
                AlphaMode = AlphaMode.Mask,
                DoubleSided = true,
            });
        });
    }

    // How deep the bark's grooves are, in its texture's width, which is a stem's circumference, and
    // the most layers its parallax occlusion searches.
    private const float BarkDepth = 1f / 40f;
    private const float BarkLayers = 12f;

    /// <summary>A bark's settings with its depth for parallax occlusion, where it has one.</summary>
    private static MaterialSettings Deep(MaterialSettings settings, AssetHandle depth)
    {
        if (depth == AssetHandle.None) return settings;
        settings.DepthMap = depth;
        settings.ParallaxDepthScale = BarkDepth;
        settings.ParallaxMethod = ParallaxMethod.Occlusion;
        settings.ParallaxLayers = BarkLayers;
        return settings;
    }

    private static float Toward(float from, float to, float share) => from + (to - from) * share;

    private static string Key(Color color) => $"{color.R:R},{color.G:R},{color.B:R}";

    /// <summary>A material made once for a key in the running app.</summary>
    private static AssetHandle Once(string key, Func<AssetHandle> make)
    {
        var app = App.Running ?? throw new InvalidOperationException("A tree's material is made inside a system, while an app runs.");
        var made = Made.GetOrCreateValue(app);
        lock (made)
        {
            if (!made.TryGetValue(key, out var handle)) made[key] = handle = make();
            return handle;
        }
    }

    // The generated pictures, made once an app for each kind, under keys of their own beside the materials'.
    private static readonly ConditionalWeakTable<App, Dictionary<string, object>> Pictures = new();

    private static T Generated<T>(string key, Func<T> make) where T : notnull
    {
        var app = App.Running!;
        var made = Pictures.GetOrCreateValue(app);
        lock (made)
        {
            if (!made.TryGetValue(key, out var value)) made[key] = value = make();
            return (T)value;
        }
    }

    /// <summary>A picture made in code with its levels, repeating and filtered between texels and levels.</summary>
    private static AssetHandle Image(byte[] pixels, int width, int height, bool srgb)
    {
        var image = Render.CreateImage(pixels, (uint)width, (uint)height, srgb, mipmaps: true);
        Render.SetSampler(image, new TextureSettings
        {
            Wrap = TextureWrap.Repeat,
            MagFilter = TextureFilter.Linear,
            MinFilter = TextureFilter.Linear,
            MipmapFilter = TextureFilter.Linear,
            Anisotropy = 8,
            Srgb = srgb,
        });
        return image;
    }
}
