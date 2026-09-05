using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// One way of telling what an entity is, by a component it carries.
/// </summary>
/// <param name="Mark">Part of a component's qualified name, matched anywhere in it.</param>
/// <param name="Icon">The picture a row wears when it matches, under the asset root.</param>
/// <param name="Order">Which answer wins when more than one matches. Lower is first.</param>
/// <remarks>
/// A table rather than a chain of ifs, for the same reason the menu is one: a game that spawns
/// something of its own should be able to say what it looks like in the hierarchy by adding a
/// line, without this file knowing the name of a single one of its types.
/// </remarks>
public sealed record EntityKind(string Mark, string Icon, int Order = 0);

/// <summary>
/// What an entity is, as far as a list of them is concerned.
/// </summary>
/// <remarks>
/// <para>
/// A tree of names is a tree of names; a tree with a camera, a light and a mesh in it is a scene.
/// The engine has no such notion — an entity is whatever components are on it — so the answer is
/// assembled here from the components it carries, and the first match in order wins.
/// </para>
/// <para>
/// Matched on the component's qualified name, and remembered per component id: naming a component
/// crosses the ABI and copies a string, the answer never changes while an app runs, and a
/// hierarchy would otherwise ask it thousands of times a second.
/// </para>
/// </remarks>
public static class EditorKinds
{
    private static readonly List<EntityKind> Kinds =
    [
        new("bevy_camera::components::Camera", "icons/ui/camera.png", 0),
        new("Camera3d", "icons/ui/camera.png", 1),
        new("Camera2d", "icons/ui/camera.png", 1),
        new("light::DirectionalLight", "icons/ui/light.png", 10),
        new("light::PointLight", "icons/ui/light.png", 10),
        new("light::SpotLight", "icons/ui/light.png", 10),
        new("Mesh3d", "icons/ui/mesh.png", 20),
        new("Mesh2d", "icons/ui/mesh.png", 20),
        new("Sprite", "icons/ui/image.png", 21),
        new("bevy_ui::", "icons/ui/interface.png", 30),
        new("extended_ui", "icons/ui/interface.png", 30),
    ];

    /// <summary>What an entity with nothing recognisable on it wears.</summary>
    public const string Plain = "icons/ui/entity.png";

    /// <summary>What an entity carrying a behavior of this project's own wears.</summary>
    public const string Scripted = "icons/ui/script.png";

    /// <summary>Every way of telling, in the order they are asked.</summary>
    public static IReadOnlyList<EntityKind> All
    {
        get
        {
            var sorted = new List<EntityKind>(Kinds);
            sorted.Sort((a, b) => a.Order.CompareTo(b.Order));

            return sorted;
        }
    }

    /// <summary>Adds a way of telling, which a game does for its own components.</summary>
    public static void Add(EntityKind kind)
    {
        ArgumentNullException.ThrowIfNull(kind);

        Kinds.Add(kind);
        Answers.Clear();
    }

    /// <summary>Forgets every added way, which a second editor in one process would need.</summary>
    public static void Reset() => Answers.Clear();

    /// <summary>
    /// The picture an entity's row wears.
    /// </summary>
    /// <remarks>
    /// A behavior of this project's own beats nothing at all but loses to a camera or a mesh: what
    /// a thing <em>is</em> is more use in a list than what it happens to be doing.
    /// </remarks>
    public static string IconFor(EcsWorld world, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(world);

        var best = int.MaxValue;
        var icon = Plain;

        foreach (var id in world.ComponentsOf(entity))
        {
            var answer = Answer(world, id);
            if (answer is null) continue;
            if (answer.Order >= best) continue;

            best = answer.Order;
            icon = answer.Icon;
        }

        if (best != int.MaxValue) return icon;

        // Nothing said what it is, so say what it does.
        foreach (var id in world.ComponentsOf(entity))
        {
            if (ComponentSchemas.For(id) is not null) return Scripted;
        }

        return Plain;
    }

    /// <summary>What a component id says about the entity carrying it, remembered once.</summary>
    private static EntityKind? Answer(EcsWorld world, int id)
    {
        if (Answers.TryGetValue(id, out var known)) return known;

        var name = world.ComponentName(id);
        EntityKind? found = null;

        foreach (var kind in All)
        {
            if (!name.Contains(kind.Mark, StringComparison.Ordinal)) continue;

            found = kind;
            break;
        }

        Answers[id] = found;
        return found;
    }

    /// <summary>Which ids answer to what. Held for the same reason the interface marks are.</summary>
    private static readonly Dictionary<int, EntityKind?> Answers = [];
}
