// Bevy's entity_disabling example, examples/ecs/entity_disabling.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Text;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Ecs;

// Demonstrates entity disabling. Three named shapes stand in a row, and a click on one gives it
// Bevy's Disabled, which hides it from every query that does not ask for disabled entities, so it is
// neither drawn nor listed among the named entities at the top right. Space enables them all again.
internal static class EntityDisabling
{
    private const float XExtent = 900f;

    public static void Build(App app)
    {
        app.Startup(SetupScene, "entity_disabling.SetupScene");
        app.Startup(DisplayInstructions, "entity_disabling.DisplayInstructions");
        app.Update(ListAllNamedEntities, "entity_disabling.ListAllNamedEntities");
        app.Update(ReenableEntitiesOnSpace, "entity_disabling.ReenableEntitiesOnSpace");
    }

    // Meshes are picked, as Bevy's adds MeshPickingPlugin.
    public static void Configure(Config config) => config.MeshPicking = true;

    private static void SetupScene(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        // Bevy's global observer, which disables whatever was clicked that may be.
        ecs.Observe<Pointer<Click>>(on =>
        {
            if (on.Ecs.Has<DisableOnClick>(on.Event.Entity)) on.Ecs.Insert<DisabledRef>(on.Event.Entity);
        });

        (string Name, AssetHandle Mesh)[] shapes =
        [
            ("Annulus", Render.CreateMesh(MeshShape.Annulus, 25f, 50f)),
            ("Bestagon", Render.CreateMesh(MeshShape.RegularPolygon, 50f, 6f)),
            ("Rhombus", Render.CreateMesh(MeshShape.Rhombus, 75f, 100f)),
        ];

        for (var i = 0; i < shapes.Length; i++)
        {
            var shape = ecs.Spawn();
            ecs.SetName(shape, shapes[i].Name);
            ecs.Add(shape, new DisableOnClick());
            ecs.Add(shape, Transform.At(-XExtent / 2f + (float)i / (shapes.Length - 1) * XExtent, 0f, 0f));
            Render2d.SetMesh(ecs, shape, shapes[i].Mesh);
            Render2d.SetMaterial(ecs, shape, Render2d.CreateMaterial(new ColorMaterialSettings { Color = Color.FromHsl(360f * i / shapes.Length, 0.95f, 0.7f) }));
        }
    }

    private static void DisplayInstructions(BehaviorContext ctx) =>
        Ui.SpawnText(
            "Click an entity to disable it.\n\nPress Space to re-enable all disabled entities.",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });

    // Every named entity a query sees, which leaves out the disabled ones, sorted by name and
    // written as Bevy's Debug writes a Name, at the top right.
    private static void ListAllNamedEntities(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var names = ecs.All()
            .Where(entity => ecs.Get<DisabledRef>(entity) is null)
            .Select(ecs.NameOf)
            .OfType<string>()
            .Order(StringComparer.Ordinal);

        var text = new StringBuilder("Named entities found:\n");
        foreach (var name in names) text.Append('"').Append(name).Append("\"\n");

        var shown = ecs.EntitiesWith<EntityNameText>();
        if (shown.Length > 0)
        {
            Ui.SetText(shown[0], text.ToString());
            return;
        }

        var spawned = Ui.SpawnText("", new UiSettings { Absolute = true, Top = Length.Px(12f), Right = Length.Px(12f) });
        ecs.Add(spawned, new EntityNameText());
    }

    // Space takes Disabled off every entity that has it, which only a query asking for disabled
    // entities finds.
    private static void ReenableEntitiesOnSpace(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space)) return;

        var ecs = ctx.Ecs;
        foreach (var entity in ecs.All()) ecs.Get<DisabledRef>(entity)?.Remove();
    }
}

/// <summary>A shape a click disables.</summary>
[Behavior]
public partial struct DisableOnClick;

/// <summary>
/// The text listing the named entities, spawned the first frame it is looked for.
/// </summary>
[Behavior]
public partial struct EntityNameText;
