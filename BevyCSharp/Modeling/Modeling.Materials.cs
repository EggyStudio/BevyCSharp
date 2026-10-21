using System.Runtime.CompilerServices;

namespace Bevy;

// The materials an edit mesh's slots are drawn with, each made from its slot's look, the prototype
// material of the slot's name until a color, a texture or a map is given it.
public static partial class Modeling
{
    private static readonly ConditionalWeakTable<App, Dictionary<(string Name, SlotLook Look), AssetHandle>> Looks = new();
    private static readonly ConditionalWeakTable<App, Dictionary<(string Path, bool Data), AssetHandle>> SlotImages = new();

    /// <summary>The material each slot of a mesh is drawn with, in the order of its slots. Only valid inside a system.</summary>
    /// <param name="mesh">The mesh.</param>
    /// <returns>A material for each of <see cref="EditMesh.Slots"/>, which the meshes <see cref="Meshes"/> answers are drawn with one for one.</returns>
    /// <remarks>
    /// Made once for each look while the app runs, so a system asks for them every frame for
    /// nothing, and slots of one look share a material. A slot whose look gives nothing is drawn
    /// with <see cref="Prototype(string)"/> of its name, so a level blocked out before it has
    /// textures reads by color.
    /// </remarks>
    public static AssetHandle[] Materials(EditMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        var materials = new AssetHandle[mesh.Slots.Count];
        for (var slot = 0; slot < materials.Length; slot++) materials[slot] = Material(mesh.Slots[slot], mesh.LookAt(slot));
        return materials;
    }

    /// <summary>The material a look makes, for a slot of a name. Only valid inside a system.</summary>
    /// <param name="name">The slot's name, whose prototype color tints the grid where the look gives neither a color nor a texture.</param>
    /// <param name="look">The look.</param>
    /// <returns>The material, the same one for the same look while the app runs.</returns>
    /// <remarks>
    /// <para>
    /// The texture, or the grid where there is none, is tinted by the color, white where none is
    /// given, and a color with an alpha below one draws the faces blended. A slot with neither a
    /// texture nor a roughness is as rough as a prototype material, so the light shows the grid's
    /// lines, and one with a texture as rough as Bevy's default.
    /// </para>
    /// <para>
    /// Every texture is loaded once for the app, repeating, the picture and the emissive map as
    /// colors and the other maps as data, so a normal map is read as the numbers it holds. A
    /// texture loads in the background, and the faces draw untextured for the frames until it has.
    /// </para>
    /// </remarks>
    public static AssetHandle Material(string name, SlotLook look)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(look);
        if (look.IsPrototype) return Prototype(name);

        var app = App.Running ?? throw new InvalidOperationException("A slot's material is made inside a system, while an app runs.");
        var made = Looks.GetOrCreateValue(app);
        lock (made)
        {
            // The name counts only where it tints the grid, so slots of one look share a material.
            var byName = look.Color is null && look.Texture is null;
            var key = (byName ? name : "", look);
            if (made.TryGetValue(key, out var known)) return known;

            var color = look.Color is { } given ? Color.FromSrgb(given.X, given.Y, given.Z, given.W)
                : byName && PrototypeColors.TryGetValue(name, out var named) ? named : Color.White;
            var emissive = look.Emissive ?? (look.EmissiveMap is null ? Vec3.Zero : Vec3.One);

            var material = Render.CreateMaterial(new MaterialSettings
            {
                BaseColor = (color.R, color.G, color.B, color.A),
                BaseColorTexture = look.Texture is { } texture ? SlotImage(app, texture, data: false) : GridOf(app),
                NormalMap = look.NormalMap is { } normal ? SlotImage(app, normal, data: true) : AssetHandle.None,
                OcclusionTexture = look.OcclusionMap is { } occlusion ? SlotImage(app, occlusion, data: true) : AssetHandle.None,
                MetallicRoughnessTexture = look.MetallicRoughnessMap is { } metal ? SlotImage(app, metal, data: true) : AssetHandle.None,
                EmissiveTexture = look.EmissiveMap is { } glow ? SlotImage(app, glow, data: false) : AssetHandle.None,
                Emissive = (emissive.X, emissive.Y, emissive.Z, 1f),
                Metallic = look.Metallic ?? 0f,
                Roughness = look.Roughness ?? (look.Texture is null ? 0.85f : 0.5f),
                AlphaMode = color.A < 1f ? AlphaMode.Blend : AlphaMode.Opaque,
            });

            made[key] = material;
            return material;
        }
    }

    /// <summary>How wide the texture a look shows is, in pixels, the grid's where it has none, or nothing while it loads.</summary>
    internal static int? TextureWidth(SlotLook look)
    {
        if (look.Texture is not { } path) return Images.DefaultPixelsPerMeter;
        if (App.Running is not { } app) return null;

        return Render.TryImageSize(SlotImage(app, path, data: false), out var width, out _, out _) && width > 0 ? (int)width : null;
    }

    /// <summary>
    /// A line of the recipe with what it leaves to the engine given, a density's texture width read
    /// from the texture of the first selected face's slot, as the console's command and the modeling
    /// window run a density typed with one number.
    /// </summary>
    /// <exception cref="InvalidOperationException">The texture is still loading, so its width is not known.</exception>
    internal static string Completed(EditMesh mesh, string line)
    {
        var words = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words is not ["uv", "density", var density]) return line;

        var first = Enumerable.Range(0, mesh.FaceCount).FirstOrDefault(mesh.IsFaceSelected, -1);
        var slot = first >= 0 ? mesh.SlotOf(first) : mesh.Slots[0];
        var width = TextureWidth(mesh.LookOf(slot)) ?? throw new InvalidOperationException(App.Running is null
            ? $"No app runs to load the texture of the slot {slot}, so its width is not known. Give it after the density, as uv density {density} <width>."
            : $"The texture of the slot {slot} is still loading, so its width is not known yet. Give it after the density, as uv density {density} <width>.");

        return $"uv density {density} {width}";
    }

    /// <summary>The picture a look shows, its texture or the grid, which the modeling window draws the coordinates over. Only valid inside a system.</summary>
    internal static AssetHandle PictureOf(SlotLook look)
    {
        var app = App.Running ?? throw new InvalidOperationException("A slot's picture is loaded inside a system, while an app runs.");
        return look.Texture is { } path ? SlotImage(app, path, data: false) : GridOf(app);
    }

    /// <summary>A texture a slot's look names, loaded once for the app and repeating, as colors or as data.</summary>
    private static AssetHandle SlotImage(App app, string path, bool data)
    {
        var loaded = SlotImages.GetOrCreateValue(app);
        lock (loaded)
        {
            if (loaded.TryGetValue((path, data), out var known)) return known;

            var settings = data ? TextureSettings.Data : TextureSettings.Tiling;
            settings.Wrap = TextureWrap.Repeat;
            return loaded[(path, data)] = AssetServer.LoadImage(path, settings);
        }
    }
}
