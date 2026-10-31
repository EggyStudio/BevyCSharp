using System.Runtime.CompilerServices;

namespace Bevy;

// The prototype materials a level is blocked out in, the grid tinted by a color for each part of a
// level, so a level reads by color before it has a texture of its own.
public static partial class Modeling
{
    /// <summary>
    /// The named colors of the prototype materials, by the part of a level each is for, the
    /// material slots a level's faces are sorted into.
    /// </summary>
    /// <remarks>
    /// After Unreal's level prototyping palette, a floor and a wall nearly white, a ceiling whiter
    /// still and stairs, ramps and the parts that move the prototyping orange, with a door green, a
    /// window blue and a hazard red, as sRGB colors, as the prototype textures of the engines of the
    /// 2000s told a reader of a level what each surface was for. The floors and walls are near white
    /// rather than Unreal's dark and middle grays, which read as dull and muddy under the sky, a wall
    /// parting from the floor it stands on by the light on each. The same names in 3DEngine, whose
    /// palette is this one but for its grayer floors, walls and ceilings.
    /// </remarks>
    public static IReadOnlyDictionary<string, Color> PrototypeColors { get; } = new Dictionary<string, Color>(StringComparer.Ordinal)
    {
        ["floor"] = Color.FromSrgb(0.90f, 0.90f, 0.89f),
        ["wall"] = Color.FromSrgb(0.90f, 0.90f, 0.89f),
        ["ceiling"] = Color.FromSrgb(0.95f, 0.95f, 0.96f),
        ["stairs"] = Color.FromSrgb(0.96f, 0.46f, 0.13f),
        ["door"] = Color.FromSrgb(0.38f, 0.72f, 0.38f),
        ["window"] = Color.FromSrgb(0.36f, 0.62f, 0.92f),
        ["hazard"] = Color.FromSrgb(0.90f, 0.22f, 0.16f),
    };

    private static readonly ConditionalWeakTable<App, Dictionary<Color, AssetHandle>> Prototypes = new();
    private static readonly ConditionalWeakTable<App, StrongBox<AssetHandle>> Grids = new();

    /// <summary>The prototype material of a part of a level, the grid tinted by its color. Only valid inside a system.</summary>
    /// <param name="name">
    /// One of <see cref="PrototypeColors"/>, such as <c>wall</c>, which a material slot of that name
    /// is drawn with. Any other name, <see cref="EditMesh.DefaultSlot"/> among them, takes the
    /// grid as it is, white with light gray lines.
    /// </param>
    /// <returns>The material, the same one for the same name while the app runs.</returns>
    public static AssetHandle Prototype(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Prototype(PrototypeColors.TryGetValue(name, out var color) ? color : Color.White);
    }

    /// <summary>A prototype material of any color, the grid tinted by it. Only valid inside a system.</summary>
    /// <param name="color">The tint, which the grid's light face shows as it is and its lines as a darker tone of it.</param>
    /// <returns>The material, the same one for the same color while the app runs.</returns>
    /// <remarks>
    /// One grid image for every prototype material of an app, made the first time one is asked
    /// for, and a material for each color, so a level of many faces in a few slots draws with a
    /// few materials and one texture. Rough and not metal, so the light shows the shape and a
    /// highlight never hides the lines.
    /// </remarks>
    public static AssetHandle Prototype(Color color)
    {
        var app = App.Running ?? throw new InvalidOperationException("A prototype material is made inside a system, while an app runs.");

        var made = Prototypes.GetOrCreateValue(app);
        lock (made)
        {
            if (made.TryGetValue(color, out var known)) return known;

            var material = Render.CreateMaterial(new MaterialSettings
            {
                BaseColor = (color.R, color.G, color.B, color.A),
                BaseColorTexture = GridOf(app),
                Roughness = 0.85f,
            });

            made[color] = material;
            return material;
        }
    }

    /// <summary>
    /// A prototype's picture as a level editor shows it, the grid's cell at a size tinted by the
    /// prototype's color as its material multiplies its texture, four bytes a pixel in sRGB, rows
    /// from the top.
    /// </summary>
    /// <param name="tint">The prototype's color.</param>
    /// <param name="size">How many pixels a side, sixteen or more.</param>
    /// <remarks>The grid is read as sRGB, as the material samples it, multiplied in linear light and written as sRGB again.</remarks>
    internal static byte[] PrototypePicture(Color tint, int size)
    {
        var pixels = Images.GridPixels(size, PrototypeFace, PrototypeLine, PrototypeFine);
        var tints = new[] { tint.R, tint.G, tint.B };
        var tables = new byte[3][];
        for (var channel = 0; channel < 3; channel++)
        {
            tables[channel] = new byte[256];
            for (var value = 0; value < 256; value++)
            {
                var lit = Color.FromSrgb8((byte)value, (byte)value, (byte)value).R * tints[channel];
                tables[channel][value] = (byte)Math.Clamp(MathF.Round(new Color(lit, lit, lit, 1f).ToSrgb().X * 255f), 0f, 255f);
            }
        }

        for (var at = 0; at < pixels.Length; at += 4)
        for (var channel = 0; channel < 3; channel++) pixels[at + channel] = tables[channel][pixels[at + channel]];
        return pixels;
    }

    // The prototype grid's face, its lines a meter and a half meter apart with its mark, and its
    // quarters' lines, before a prototype's color tints them. The face is all but white, so a
    // prototype's color shows as itself, and the lines a light gray, so the grid measures a face
    // without darkening it, where the default grid's darker lines over a near white wall read as
    // dirt. A face of 0.99 rather than 1, since a white face tinted by a color of a whole tenth,
    // as the hazard's 0.9, lands a byte of TrenchBroom's picture on a half, which one system's
    // power function may round up and another's down.
    internal static Color PrototypeFace => Color.FromSrgb(0.99f, 0.99f, 0.99f);
    internal static Color PrototypeLine => Color.FromSrgb(0.6f, 0.6f, 0.6f);
    internal static Color PrototypeFine => Color.FromSrgb(0.82f, 0.82f, 0.82f);

    /// <summary>The grid image an app's prototype materials share, made the first time one is asked for.</summary>
    private static AssetHandle GridOf(App app) =>
        Grids.GetValue(app, _ => new StrongBox<AssetHandle>(Images.Grid(Images.DefaultPixelsPerMeter, PrototypeFace, PrototypeLine, PrototypeFine))).Value;
}
