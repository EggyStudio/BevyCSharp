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
/// A material's picture spans as many texels as the level has units a meter, so at a scale of one a
/// face's material repeats once a meter, as the prototype materials' grid does.
/// </para>
/// </remarks>
public static class Levels
{
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
    /// <returns>The level's root, under which every entity of it stands.</returns>
    /// <exception cref="FileNotFoundException">There is no such file.</exception>
    /// <exception cref="FormatException">The file is not map text, or a key of it is not what its class reads.</exception>
    public static Entity Load(EcsWorld ecs, string path) => Spawn(ecs, Level.Load(path), Path.GetFileNameWithoutExtension(path));

    /// <summary>Builds a level into the world. Only valid inside a system.</summary>
    /// <param name="ecs">The world.</param>
    /// <param name="level">The level.</param>
    /// <param name="name">What the root is named, the level's.</param>
    /// <returns>The level's root, under which every entity of it stands, so despawning it takes the level away.</returns>
    /// <exception cref="FormatException">A key is not what its class reads, as an <c>origin</c> of other than three numbers, or a brush closes no solid.</exception>
    public static Entity Spawn(EcsWorld ecs, Level level, string name = "level")
    {
        ArgumentNullException.ThrowIfNull(ecs);
        ArgumentNullException.ThrowIfNull(level);

        var units = level.UnitsPerMeter;
        var root = ecs.Spawn();
        ecs.Add(root, Transform.Identity);
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
    /// part under it and makes it collide as its triangles.
    /// </summary>
    private static void Build(EcsWorld ecs, Entity entity, LevelEntity source, double units, int index)
    {
        var at = ecs.TryGet<Transform>(entity, out var placed) ? placed.Translation : Vec3.Zero;
        var mesh = Mesh(source, units, at);
        if (mesh.FaceCount == 0) return;

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

        ecs.Add(entity, new RigidBody { Kind = BodyKind.Static });
        ecs.Add(entity, new Collider { Shape = ColliderShape.Mesh });
    }

    /// <summary>
    /// An entity's brushes as an edit mesh in meters about a place, a slot each material, each face's
    /// corners wound as Bevy's front faces are and given the texture coordinates its axes say.
    /// </summary>
    internal static EditMesh Mesh(LevelEntity source, double units, Vec3 at)
    {
        var mesh = new EditMesh();
        var first = source.Brushes.SelectMany(brush => brush.Faces).Select(face => face.Material).FirstOrDefault();
        if (first is not null && first != EditMesh.DefaultSlot) mesh.SlotRename(EditMesh.DefaultSlot, first);

        foreach (var brush in source.Brushes)
        {
            var polygons = brush.Polygons();
            var vertices = new Dictionary<LevelPoint, int>();
            for (var f = 0; f < brush.Faces.Count; f++)
            {
                if (polygons[f] is not { } polygon) continue;
                var face = brush.Faces[f];

                // The file winds a face clockwise seen from outside, and Bevy draws the side its
                // corners run counter-clockwise round, so they are taken backwards.
                var corners = new int[polygon.Length];
                var uvs = new Vec2[polygon.Length];
                for (var i = 0; i < polygon.Length; i++)
                {
                    var point = polygon[polygon.Length - 1 - i];
                    if (!vertices.TryGetValue(point, out var vertex)) vertices[point] = vertex = mesh.AddCorner(point.Meters(units) - at);
                    corners[i] = vertex;
                    uvs[i] = Texel(face, point, units);
                }

                mesh.AddPolygon(corners, face.Material, uvs);
            }
        }

        return mesh;
    }

    /// <summary>
    /// Where a point of a face falls on its material, its texel along each axis, scaled and moved,
    /// over the texels the material's picture spans, a level's units a meter.
    /// </summary>
    internal static Vec2 Texel(LevelFace face, LevelPoint point, double units) =>
        new((float)((LevelPoint.Dot(point, face.UAxis) / face.ScaleU + face.UOffset) / units),
            (float)((LevelPoint.Dot(point, face.VAxis) / face.ScaleV + face.VOffset) / units));

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
