using System.Globalization;
using Bevy.Physics;

namespace Bevy;

/// <summary>
/// Builds a level file into a scene, its brushes meshed with their collision and its entities
/// spawned by class.
/// </summary>
/// <remarks>
/// <para>
/// Each entity of the level becomes an entity under the level's root, named by its
/// <c>targetname</c> or its class and carrying <see cref="LevelKeys"/>. The <c>worldspawn</c>'s
/// brushes, and each brush entity's, are built through an <see cref="EditMesh"/>, a slot a
/// material, each face given the texture coordinates its axes, offsets and scales say, and drawn
/// as the slot of its material's name is (<see cref="Modeling.Materials"/>), so a face of the
/// material <c>wall</c> is drawn as a wall's prototype. They collide as their triangles, a static
/// body of <see cref="ColliderShape.Mesh"/>.
/// </para>
/// <para>
/// A point entity stands at its <c>origin</c>, turned by its <c>angle</c>, the yaw in degrees, or
/// its <c>angles</c>, the pitch, the yaw and the roll, so its forward, minus Z, looks the way the
/// file's angles face, where TrenchBroom draws its arrow. A brush entity stands unturned at its
/// <c>origin</c> where it has one, which a door turns about, and at the middle of its brushes
/// otherwise, its brushes where the file puts them. The
/// class says what else it is (<see cref="Register"/>), and a class nothing registered stands as
/// its place and its keys for a game to read.
/// </para>
/// <para>
/// A face's material is read over its picture's texels, as TrenchBroom draws it. A material under
/// <see cref="PrototypeFolder"/>, as <c>proto/floor</c>, is drawn as the prototype of the rest of
/// its name over a picture of <see cref="PrototypePixels"/>, which <see cref="WritePrototypePictures"/>
/// writes for TrenchBroom, so a face of a sixteenth's scale repeats once a meter in both. A material
/// with a picture under the asset root's <c>textures</c> folder, a PNG or a JPEG of its name, is
/// drawn with it over its own pixels, and any other is drawn as the slot of its name over a picture
/// of the level's units a meter, so at a scale of one it repeats once a meter.
/// </para>
/// </remarks>
public static class Levels
{
    /// <summary>
    /// The folder of a level's materials a prototype's picture stands in, so a face of
    /// <c>proto/floor</c> is drawn as the <c>floor</c> prototype (<see cref="Modeling.Prototype(string)"/>).
    /// </summary>
    public const string PrototypeFolder = "proto/";

    /// <summary>
    /// How many texels a side a prototype's picture has, as <see cref="WritePrototypePictures"/>
    /// writes them for a level editor, a meter's cell of the grid.
    /// </summary>
    public const int PrototypePixels = 512;

    private static readonly Lock Registry = new();
    private static readonly Dictionary<string, Action<EcsWorld, Entity, LevelEntity>> Classes = new(StringComparer.Ordinal)
    {
        ["tree"] = PlantTree,
    };

    /// <summary>The classes registered, the engine's own and those a game registered, in order.</summary>
    public static IReadOnlyList<string> ClassNames
    {
        get
        {
            lock (Registry) return [.. Classes.Keys.Order(StringComparer.Ordinal)];
        }
    }

    /// <summary>
    /// Says how an entity of a class is spawned, called with the entity placed under the level and
    /// carrying <see cref="LevelKeys"/>, after its brushes are built where it has any.
    /// </summary>
    /// <param name="className">The class, as <c>info_player_start</c>, replacing one registered before.</param>
    /// <param name="spawn">What makes the entity what the class is, from the world, the entity and the level's entity.</param>
    /// <remarks>The engine registers <c>tree</c>, a tree of the <c>species</c> key's name and the <c>seed</c> key's seed (<see cref="Tree"/>).</remarks>
    public static void Register(string className, Action<EcsWorld, Entity, LevelEntity> spawn)
    {
        ArgumentException.ThrowIfNullOrEmpty(className);
        ArgumentNullException.ThrowIfNull(spawn);
        lock (Registry) Classes[className] = spawn;
    }

    /// <summary>Reads a level file and builds it into the world. Only valid inside a system.</summary>
    /// <param name="ecs">The world.</param>
    /// <param name="path">The file, relative to the asset root unless it is a full path.</param>
    /// <param name="place">Where the level's root stands, the file's origin, at the world's origin where none is given.</param>
    /// <returns>The level's root, under which every entity of it stands.</returns>
    /// <exception cref="FileNotFoundException">There is no such file.</exception>
    /// <exception cref="FormatException">The file is not map text, or a key of it is not what its class reads.</exception>
    public static Entity Load(EcsWorld ecs, string path, Transform? place = null) => Spawn(ecs, Level.Load(path), Path.GetFileNameWithoutExtension(path), place);

    /// <summary>Builds a level into the world. Only valid inside a system.</summary>
    /// <param name="ecs">The world.</param>
    /// <param name="level">The level.</param>
    /// <param name="name">What the root is named, the level's.</param>
    /// <param name="place">
    /// Where the level's root stands, the file's origin, at the world's origin where none is given,
    /// so a level drawn with its floor at zero stands on a ground at another height. Its bodies are
    /// put where it stands as they are made, so the root is placed here rather than moved after.
    /// </param>
    /// <returns>The level's root, under which every entity of it stands, so despawning it takes the level away.</returns>
    /// <exception cref="FormatException">A key is not what its class reads, as an <c>origin</c> of other than three numbers, or a brush closes no solid.</exception>
    public static Entity Spawn(EcsWorld ecs, Level level, string name = "level", Transform? place = null)
    {
        ArgumentNullException.ThrowIfNull(ecs);
        ArgumentNullException.ThrowIfNull(level);

        var units = level.UnitsPerMeter;
        var root = ecs.Spawn();
        ecs.Add(root, place ?? Transform.Identity);
        ecs.Add(root, Visibility.Inherited);
        ecs.SetName(root, name);

        for (var index = 0; index < level.Entities.Count; index++)
        {
            var source = level.Entities[index];
            var entity = ecs.Spawn();
            ecs.SetParent(entity, root);
            var className = source.ClassName;
            var targetName = source["targetname"] ?? "";
            ecs.SetName(entity, targetName.Length > 0 ? targetName : className.Length > 0 ? className : $"entity {index}");
            ecs.Add(entity, new LevelKeys { ClassName = className, TargetName = targetName, Keys = KeysText(source) });

            try
            {
                Place(ecs, entity, source, units, index);
                if (source.Brushes.Count > 0) Build(ecs, entity, source, units, index);

                Action<EcsWorld, Entity, LevelEntity>? spawn;
                lock (Registry) spawn = Classes.GetValueOrDefault(className);
                spawn?.Invoke(ecs, entity, source);
            }
            catch (InvalidOperationException fault)
            {
                throw new FormatException($"The level's entity {index}, {className}: {fault.Message}", fault);
            }
        }

        return root;
    }

    /// <summary>
    /// Writes each prototype's picture as TrenchBroom shows it, under a game's folder, for a level
    /// editor to draw a face of <c>proto/floor</c> as the engine draws it.
    /// </summary>
    /// <param name="folder">The game's folder, TrenchBroom's game path, under whose <c>textures/proto</c> the pictures go.</param>
    /// <returns>The pictures' full paths, one a name of <see cref="Modeling.PrototypeColors"/>.</returns>
    /// <remarks>
    /// Each is the grid's cell of <see cref="PrototypePixels"/> a side tinted by its prototype's color,
    /// as its material draws it unlit, so with a face scale of a sixteenth, which the game config
    /// beside the engine's levels gives a new face, a cell spans 32 units, a meter at the default
    /// units a meter, in TrenchBroom as in the engine.
    /// </remarks>
    public static IReadOnlyList<string> WritePrototypePictures(string folder)
    {
        ArgumentException.ThrowIfNullOrEmpty(folder);
        var into = Path.Combine(folder, "textures", PrototypeFolder.TrimEnd('/'));
        Directory.CreateDirectory(into);
        var written = new List<string>();
        foreach (var (name, color) in Modeling.PrototypeColors)
        {
            var path = Path.Combine(into, name + ".png");
            File.WriteAllBytes(path, PngWriter.Still(PrototypePixels, PrototypePixels, Modeling.PrototypePicture(color, PrototypePixels)));
            written.Add(path);
        }

        return written;
    }

    /// <summary>The keyvalues an entity's <see cref="LevelKeys"/> holds, in the level file's order.</summary>
    /// <param name="keys">The component.</param>
    public static IReadOnlyList<KeyValuePair<string, string>> KeysOf(in LevelKeys keys) =>
        Level.Parse("{\n" + keys.Keys.Value + "}\n").Entities[0].Keys;

    /// <summary>An entity's keyvalues as the level file writes them, a line each.</summary>
    private static string KeysText(LevelEntity entity)
    {
        var copy = new Level();
        var only = new LevelEntity();
        only.Keys.AddRange(entity.Keys);
        copy.Entities.Add(only);
        var text = copy.Write();
        var open = text.IndexOf("{\n", StringComparison.Ordinal) + 2;
        return text[open..text.LastIndexOf('}')];
    }

    /// <summary>
    /// Places an entity, a point entity at its origin turned by its angles, a brush entity unturned
    /// at its origin or its brushes' middle, and <c>worldspawn</c> where the level's root is.
    /// </summary>
    private static void Place(EcsWorld ecs, Entity entity, LevelEntity source, double units, int index)
    {
        var at = source["origin"] is { } origin ? Point(origin, "origin", index).Meters(units)
            : source.Brushes.Count > 0 && source.ClassName != "worldspawn" ? Middle(source, units)
            : Vec3.Zero;
        // A brush entity's brushes are where the file puts them, so it stands unturned whatever its angles.
        var turn = source.Brushes.Count > 0 ? Quat.Identity : Turn(source, index);
        ecs.Add(entity, new Transform(at, turn, Vec3.One));
        ecs.Add(entity, Visibility.Inherited);
    }

    /// <summary>
    /// The turn an entity's angles give, the file's turn carried onto Y up and then a quarter about Y,
    /// so the entity's minus Z looks along the file's X turned by the angles, as Quake faces an
    /// entity along X.
    /// </summary>
    private static Quat Turn(LevelEntity source, int index)
    {
        static float Radians(double degrees) => (float)(degrees * Math.PI / 180);
        double pitch = 0, yaw = 0, roll = 0;
        if (source["angles"] is { } angles) (pitch, yaw, roll) = Three(angles, "angles", index);
        else if (source["angle"] is { } angle) yaw = Number(angle, "angle", index);

        // The file's yaw about Z, pitch about Y and roll about X are, Y up, a yaw about Y, a pitch
        // about minus Z and a roll about X.
        var turned = Quat.FromRotationY(Radians(yaw)) * Quat.FromAxisAngle(-Vec3.UnitZ, Radians(pitch)) * Quat.FromRotationX(Radians(roll));
        return turned * Quat.FromRotationY(-MathF.PI / 2);
    }

    /// <summary>The middle of the box round an entity's brushes, in meters.</summary>
    private static Vec3 Middle(LevelEntity source, double units)
    {
        var corners = source.Brushes.SelectMany(brush => brush.Polygons()).OfType<LevelPoint[]>().SelectMany(polygon => polygon).ToList();
        if (corners.Count == 0) return Vec3.Zero;
        var low = new LevelPoint(corners.Min(p => p.X), corners.Min(p => p.Y), corners.Min(p => p.Z));
        var high = new LevelPoint(corners.Max(p => p.X), corners.Max(p => p.Y), corners.Max(p => p.Z));
        return ((low + high) * 0.5).Meters(units);
    }

    /// <summary>
    /// Builds an entity's brushes into an edit mesh about the entity's place, draws each slot as a
    /// part under it where there is a renderer, and makes it collide as its triangles.
    /// </summary>
    /// <remarks>
    /// The collision is the edit mesh's triangles as a static body given in code, where the world
    /// simulates bodies, rather than a collider fitted to what is drawn, which a build with no
    /// renderer cannot fit, so a level collides the same with a renderer and without, and a server
    /// or a headless test walks it. The components stay beside the body, which the simulation
    /// leaves to the body given in code, and fit what is drawn where the simulation starts after.
    /// </remarks>
    private static void Build(EcsWorld ecs, Entity entity, LevelEntity source, double units, int index)
    {
        var at = ecs.TryGet<Transform>(entity, out var placed) ? placed.Translation : Vec3.Zero;
        var mesh = Mesh(source, units, at);
        if (mesh.FaceCount == 0) return;

        if (App.HasRenderer)
        {
            var meshes = Modeling.Meshes(mesh);
            var materials = Modeling.Materials(mesh);
            for (var slot = 0; slot < meshes.Length; slot++)
            {
                if (meshes[slot] == AssetHandle.None) continue;
                var part = ecs.Spawn();
                ecs.Add(part, Transform.Identity);
                ecs.Add(part, Visibility.Inherited);
                Render.SetMesh(ecs, part, meshes[slot]);
                Render.SetMaterial(ecs, part, materials[slot]);
                ecs.SetParent(part, entity);
                ecs.SetName(part, $"{ecs.NameOf(entity)}, {mesh.Slots[slot]}");
            }
        }

        ecs.Add(entity, new RigidBody { Kind = BodyKind.Static });
        ecs.Add(entity, new Collider { Shape = ColliderShape.Mesh });
        if (App.Running?.World.TryResource<PhysicsWorld>() is { } physics && !physics.Has(entity))
            physics.Add(entity, Modeling.Collider(mesh), BodyKind.Static, Colliders.WorldOf(ecs, entity));
    }

    /// <summary>
    /// An entity's brushes as an edit mesh in meters about a place, a slot each material, each face's
    /// corners wound as Bevy's front faces are and given the texture coordinates its axes say over
    /// its material's picture.
    /// </summary>
    internal static EditMesh Mesh(LevelEntity source, double units, Vec3 at)
    {
        var mesh = new EditMesh();
        var materials = new Dictionary<string, Material>(StringComparer.Ordinal);
        Material Of(string name) => materials.TryGetValue(name, out var known) ? known : materials[name] = MaterialOf(name, units);

        var first = source.Brushes.SelectMany(brush => brush.Faces).Select(face => Of(face.Material).Slot).FirstOrDefault();
        if (first is not null && first != EditMesh.DefaultSlot) mesh.SlotRename(EditMesh.DefaultSlot, first);

        foreach (var brush in source.Brushes)
        {
            var polygons = brush.Polygons();
            var vertices = new Dictionary<LevelPoint, int>();
            for (var f = 0; f < brush.Faces.Count; f++)
            {
                if (polygons[f] is not { } polygon) continue;
                var face = brush.Faces[f];
                var material = Of(face.Material);

                // The file winds a face clockwise seen from outside, and Bevy draws the side its
                // corners run counter-clockwise round, so they are taken backwards.
                var corners = new int[polygon.Length];
                var uvs = new Vec2[polygon.Length];
                for (var i = 0; i < polygon.Length; i++)
                {
                    var point = polygon[polygon.Length - 1 - i];
                    if (!vertices.TryGetValue(point, out var vertex)) vertices[point] = vertex = mesh.AddCorner(point.Meters(units) - at);
                    corners[i] = vertex;
                    uvs[i] = Texel(face, point, material.Width, material.Height);
                }

                mesh.AddPolygon(corners, material.Slot, uvs);
            }
        }

        foreach (var material in materials.Values)
        {
            if (material.Picture is { } picture && mesh.Slots.Contains(material.Slot)) mesh.SlotTexture(material.Slot, picture);
        }

        return mesh;
    }

    /// <summary>
    /// What a level's material is drawn as, the slot it is drawn in, its picture's size in texels and
    /// the picture where it has one of its own.
    /// </summary>
    private readonly record struct Material(string Slot, double Width, double Height, string? Picture);

    /// <summary>
    /// A material by its name, a prototype under <see cref="PrototypeFolder"/> as the prototype of the
    /// rest of its name with a picture of <see cref="PrototypePixels"/>, a name with a picture under
    /// the asset root's <c>textures</c> folder drawn with it, and any other name as the slot of its
    /// name with a picture of the level's units a meter.
    /// </summary>
    private static Material MaterialOf(string name, double units)
    {
        if (name.StartsWith(PrototypeFolder, StringComparison.Ordinal) && name.Length > PrototypeFolder.Length)
            return new Material(name[PrototypeFolder.Length..], PrototypePixels, PrototypePixels, null);

        foreach (var extension in new[] { ".png", ".jpg", ".jpeg" })
        {
            var path = $"textures/{name}{extension}";
            if (AssetFiles.Exists(path) && PictureSize(AssetFiles.ReadAllBytes(path)) is { } size) return new Material(name, size.Width, size.Height, path);
        }

        return new Material(name, units, units, null);
    }

    /// <summary>A PNG's or a JPEG's width and height from its header, or nothing for other bytes.</summary>
    internal static (int Width, int Height)? PictureSize(byte[] bytes)
    {
        // A PNG's header chunk holds its width and height as big-endian four byte numbers.
        ReadOnlySpan<byte> png = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
        if (bytes.Length >= 24 && bytes.AsSpan(0, 8).SequenceEqual(png))
            return (System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16)), System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20)));

        // A JPEG's size is in its start of frame, after the segments before it, each a marker and a length.
        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8) return null;
        for (var at = 2; at + 9 < bytes.Length;)
        {
            if (bytes[at] != 0xFF) return null;
            var marker = bytes[at + 1];
            var length = bytes[at + 2] << 8 | bytes[at + 3];
            if (marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC)) return (bytes[at + 7] << 8 | bytes[at + 8], bytes[at + 5] << 8 | bytes[at + 6]);
            at += 2 + length;
        }

        return null;
    }

    /// <summary>
    /// Where a point of a face falls on its material, its texel along each axis, scaled and moved,
    /// over the texels its picture spans each way.
    /// </summary>
    internal static Vec2 Texel(LevelFace face, LevelPoint point, double width, double height) =>
        new((float)((LevelPoint.Dot(point, face.UAxis) / face.ScaleU + face.UOffset) / width),
            (float)((LevelPoint.Dot(point, face.VAxis) / face.ScaleV + face.VOffset) / height));

    /// <summary>A tree of the <c>species</c> key's name and the <c>seed</c> key's seed.</summary>
    private static void PlantTree(EcsWorld ecs, Entity entity, LevelEntity source)
    {
        var seed = source["seed"] is { } said ? uint.TryParse(said, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : throw new FormatException($"A tree's seed is {said}, where a whole number is meant.") : 0u;
        ecs.Add(entity, new Tree { Species = source["species"] ?? "", Seed = seed });
    }

    private static LevelPoint Point(string text, string key, int index)
    {
        var (x, y, z) = Three(text, key, index);
        return new LevelPoint(x, y, z);
    }

    private static (double, double, double) Three(string text, string key, int index)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) throw new FormatException($"The level's entity {index} has the {key} {text}, where three numbers are meant.");
        return (Number(parts[0], key, index), Number(parts[1], key, index), Number(parts[2], key, index));
    }

    private static double Number(string text, string key, int index) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)
            ? value
            : throw new FormatException($"The level's entity {index} has the {key} {text}, where a number is meant.");
}
