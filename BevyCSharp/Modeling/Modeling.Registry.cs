namespace Bevy;

// The models the console's model commands reach by name, those the commands made and those a game
// named, and what draws each one shown.
public static partial class Modeling
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, Named> Models = new(StringComparer.Ordinal);
    private static App? _modelsApp;

    /// <summary>A model by its name, with the entity drawing it where it is shown.</summary>
    internal sealed class Named(EditMesh mesh)
    {
        public readonly EditMesh Mesh = mesh;
        public Entity Root = Entity.None;
        public readonly List<Entity> Parts = [];
        public int DrawnVersion = -1;
    }

    /// <summary>
    /// Gives a mesh a name the console's model commands reach it by, so a game's level is shaped from
    /// the terminal and by a drive, <c>model.cut level window 1 1 2 0.9</c> cutting a window in it.
    /// </summary>
    /// <param name="name">The name, one word, which takes the place of a model of that name.</param>
    /// <param name="mesh">The mesh, which the commands change where the game holds it.</param>
    /// <remarks>
    /// The names last while the app runs, and the next app starts with none, since what drew them was
    /// the last app's.
    /// </remarks>
    public static void Register(string name, EditMesh mesh)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(mesh);
        if (name.Any(char.IsWhiteSpace)) throw new ArgumentException("A model's name is one word.", nameof(name));

        lock (Gate)
        {
            Current();
            Models[name] = new Named(mesh);
        }
    }

    /// <summary>The model of a name, or nothing where none has it.</summary>
    /// <param name="name">The name the commands or <see cref="Register"/> gave it.</param>
    public static EditMesh? Find(string name)
    {
        lock (Gate) return Current().TryGetValue(name, out var named) ? named.Mesh : null;
    }

    /// <summary>The names of the models, in order.</summary>
    public static IReadOnlyList<string> Names
    {
        get
        {
            lock (Gate) return [.. Current().Keys.Order(StringComparer.Ordinal)];
        }
    }

    /// <summary>A model by its name with what draws it, or nothing.</summary>
    internal static Named? Entry(string name)
    {
        lock (Gate) return Current().GetValueOrDefault(name);
    }

    /// <summary>Forgets a model, leaving what draws it, where it is shown, for the caller to take away.</summary>
    internal static bool Forget(string name)
    {
        lock (Gate) return Current().Remove(name);
    }

    /// <summary>
    /// Draws a model where a point says, each slot a child entity in its slot's material,
    /// written again in place after each change. Only valid inside a system.
    /// </summary>
    internal static void Show(EcsWorld world, Named named, Vec3 at)
    {
        if (named.Root.IsNone)
        {
            named.Root = world.Spawn();
            world.SetName(named.Root, "Model");
        }

        world.Add(named.Root, Transform.At(at.X, at.Y, at.Z));
        Redraw(world, named);
    }

    /// <summary>Outlines what the modeling windows are on where the run draws, and writes each shown model's meshes again where it changed since they were last written, once a frame.</summary>
    internal static void Tick(EcsWorld world, bool draws)
    {
        OutlineWindows(draws);

        Named[] shown;
        lock (Gate)
        {
            if (Models.Count == 0) return;
            shown = [.. Current().Values.Where(named => !named.Root.IsNone && named.DrawnVersion != named.Mesh.Version)];
        }

        foreach (var named in shown) Redraw(world, named);
    }

    private static void Redraw(EcsWorld world, Named named)
    {
        var meshes = Meshes(named.Mesh);
        var materials = Materials(named.Mesh);

        // A slot's part made once its first face is in it, its mesh written in place from then on,
        // and its mesh, material and name set again on each change, since a slot renamed, given a
        // look or taken away, which moves the slots after it down by one, changes what each draws.
        for (var slot = 0; slot < meshes.Length; slot++)
        {
            while (named.Parts.Count <= slot) named.Parts.Add(Entity.None);
            if (meshes[slot] == AssetHandle.None) continue;

            var part = named.Parts[slot];
            if (part.IsNone)
            {
                part = named.Parts[slot] = world.Spawn();
                world.Add(part, Transform.Identity);
                world.SetParent(part, named.Root);
            }

            Render.SetMesh(world, part, meshes[slot]);
            Render.SetMaterial(world, part, materials[slot]);
            world.SetName(part, $"Model {named.Mesh.Slots[slot]}");
        }

        for (var slot = named.Parts.Count - 1; slot >= meshes.Length; slot--)
        {
            if (!named.Parts[slot].IsNone) world.Despawn(named.Parts[slot]);
            named.Parts.RemoveAt(slot);
        }

        named.DrawnVersion = named.Mesh.Version;
    }

    /// <summary>The models, emptied where another app runs than the one they were named in.</summary>
    private static Dictionary<string, Named> Current()
    {
        var app = App.Running;
        if (app is not null && !ReferenceEquals(app, _modelsApp))
        {
            Models.Clear();
            _modelsApp = app;
        }

        return Models;
    }
}
