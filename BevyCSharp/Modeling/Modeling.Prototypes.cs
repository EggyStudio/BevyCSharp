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
    /// Unreal's level prototyping palette, a floor a dark gray, a wall a light gray, a ceiling a
    /// lighter one and stairs, ramps and the parts that move the prototyping orange, with a door
    /// green, a window blue and a hazard red, as sRGB colors, as the prototype textures of the
    /// engines of the 2000s told a reader of a level what each surface was for. The same names and
    /// colors in 3DEngine, so a level blocked out in either reads the same.
    /// </remarks>
    public static IReadOnlyDictionary<string, Color> PrototypeColors { get; } = new Dictionary<string, Color>(StringComparer.Ordinal)
    {
        ["floor"] = Color.FromSrgb(0.31f, 0.31f, 0.32f),
        ["wall"] = Color.FromSrgb(0.71f, 0.71f, 0.70f),
        ["ceiling"] = Color.FromSrgb(0.82f, 0.82f, 0.83f),
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
    /// grid's own gray.
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

    /// <summary>The grid image an app's prototype materials share, made the first time one is asked for.</summary>
    private static AssetHandle GridOf(App app) => Grids.GetValue(app, _ => new StrongBox<AssetHandle>(Images.Grid())).Value;
}
