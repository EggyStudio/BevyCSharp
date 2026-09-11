using Bevy;
using BevyCSharp.Editor.Behaviors;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Everything the editor can be told to do, written down once.
/// </summary>
/// <remarks>
/// <para>
/// The menu is a table of paths, so this is the table. The hamburger shows the whole of it, a
/// right click on the world shows the part under <c>Spawn</c>, a right click on an entity shows
/// the part under <c>Entity</c>, and a key runs one row directly. None of those four know about
/// each other.
/// </para>
/// <para>
/// A game using this editor adds its own rows the same way, which is what makes the shipped set a
/// starting point rather than the product: <c>EditorMenu.Command("Spawn/Enemy", …)</c> is the
/// whole of adding one, and it appears in the menu, in the right click and in the search.
/// </para>
/// </remarks>
public static class EditorCommands
{
    /// <summary>Fills the menu and the toolbar, given the camera the panels bind against.</summary>
    public static void Register(Entity camera)
    {
        Spawning();
        Entities();
        View();
        Project();
        Toolbar();
        Settings();
    }

    /// <summary>
    /// What goes on the settings sheet.
    /// </summary>
    /// <remarks>
    /// The editor's own preferences and what it can say about the project. Everything here is a
    /// line in a table, so a game adds a page of its own by adding lines and never touches the
    /// panel that draws them.
    /// </remarks>
    private static void Settings()
    {
        EditorSettings.Heading("Editor", "Handles", 0);

        EditorSettings.Choice(
            "Editor",
            "Axes",
            ["global", "local"],
            static () => EditorTools.Space == ToolSpace.Local ? "local" : "global",
            static chosen => EditorTools.Space =
                chosen == "local" ? ToolSpace.Local : ToolSpace.Global,
            1);

        EditorSettings.Choice(
            "Editor",
            "What several things turn about",
            ["origins", "centre"],
            static () => EditorTools.Pivot == ToolPivot.Centre ? "centre" : "origins",
            static chosen => EditorTools.Pivot =
                chosen == "centre" ? ToolPivot.Centre : ToolPivot.Origins,
            2);

        EditorSettings.Flag(
            "Editor",
            "Snap to a grid",
            static () => EditorKeys.SnapLocked,
            static on =>
            {
                EditorKeys.SnapLocked = on;
                EditorTools.Snap = on;
            },
            2);

        EditorSettings.Number(
            "Editor", "Move step (m)", static () => EditorTools.MoveStep,
            static value => EditorTools.MoveStep = MathF.Max(0.001f, value), 3);

        EditorSettings.Number(
            "Editor", "Turn step (deg)", static () => EditorTools.RotateStep,
            static value => EditorTools.RotateStep = MathF.Max(0.1f, value), 4);

        EditorSettings.Number(
            "Editor", "Stretch step", static () => EditorTools.ScaleStep,
            static value => EditorTools.ScaleStep = MathF.Max(0.001f, value), 5);

        EditorSettings.Flag(
            "Editor", "Draw the ground grid",
            static () => ViewportGizmos.ShowGrid,
            static on => ViewportGizmos.ShowGrid = on, 6);

        EditorSettings.Number(
            "Editor", "Grid height", static () => ViewportGizmos.GridHeight,
            static value => ViewportGizmos.GridHeight = value, 7);

        EditorSettings.Heading("Project", "Where things are", 0);

        EditorSettings.Fact("Project", "Assets", static () => EditorPaths.Assets, 1);
        EditorSettings.Fact("Project", "World file", static () => EditorPaths.World, 2);

        EditorSettings.Heading("Project", "Scripts", 10);

        EditorSettings.Fact(
            "Project",
            "Behaviors from scripts",
            static () => $"{EditorScripts.Registered} registration(s)",
            11);

        EditorSettings.Fact(
            "Project", "Last build", static () => EditorScripts.LastError ?? "no errors", 12);

        EditorSettings.Action("Project", "Reload now", EditorScripts.Reload, 13);

        EditorSettings.Heading("About", "BevyCSharp.Editor", 0);

        EditorSettings.Fact("About", "Component schemas", static () => ComponentSchemas.All.Count.ToString(), 3);
        EditorSettings.Fact("About", "Menu rows", static () => EditorMenu.All.Count.ToString(), 4);
    }

    /// <summary>
    /// What floats in the viewport's corners.
    /// </summary>
    /// <remarks>
    /// The menu on the left, the tools in the middle, what the editor is doing on the right. A
    /// game adds its own the same way, and the corner panels never change.
    /// </remarks>
    private static void Toolbar()
    {
        EditorToolbar.Add(
            ToolbarSlot.Left,
            "icons/ui/menu.png",
            string.Empty,
            static _ => EditorShell.ToggleMenu(string.Empty, MenuAt.X, MenuAt.Y),
            0);

        EditorToolbar.Add(
            ToolbarSlot.Left,
            "icons/ui/undo.png",
            string.Empty,
            static world => EditorHistory.Undo(world),
            1);

        EditorToolbar.Add(
            ToolbarSlot.Left,
            "icons/ui/redo.png",
            string.Empty,
            static world => EditorHistory.Redo(world),
            2);

        EditorToolbar.Add(
            ToolbarSlot.Left,
            "icons/ui/save.png",
            string.Empty,
            EditorProject.Save,
            3);

        foreach (var (_, tool, _) in EditorTools.Keys)
        {
            var chosen = tool;

            EditorToolbar.Add(new ToolbarButton(
                ToolbarSlot.Centre,
                $"icons/ui/{tool.ToString().ToLowerInvariant()}.png",
                static () => string.Empty,
                _ => EditorTools.Current = chosen,
                () => EditorTools.Current == chosen,
                (int)tool));
        }

        // A word rather than a picture, because there is no picture of "the world's axes" that
        // anyone reads faster than the word for it.
        EditorToolbar.Add(new ToolbarButton(
            ToolbarSlot.Centre,
            null,
            static () => EditorTools.Space == ToolSpace.Local ? "local" : "global",
            static _ => EditorTools.Space = EditorTools.Space == ToolSpace.Local
                ? ToolSpace.Global
                : ToolSpace.Local,
            static () => EditorTools.Space == ToolSpace.Local,
            9));

        // The other thing a drag on the handles has to be told, and a word for the same reason: no
        // picture says "about each thing's own origin" faster than the word does.
        EditorToolbar.Add(new ToolbarButton(
            ToolbarSlot.Centre,
            null,
            static () => EditorTools.Pivot == ToolPivot.Centre ? "centre" : "origins",
            static _ => EditorTools.Pivot = EditorTools.Pivot == ToolPivot.Centre
                ? ToolPivot.Origins
                : ToolPivot.Centre,
            static () => EditorTools.Pivot == ToolPivot.Centre,
            10));

        EditorToolbar.Add(new ToolbarButton(
            ToolbarSlot.Centre,
            "icons/ui/snap.png",
            static () => string.Empty,
            static _ =>
            {
                EditorKeys.SnapLocked = !EditorKeys.SnapLocked;
                EditorTools.Snap = EditorKeys.SnapLocked;
            },
            static () => EditorTools.Snap,
            11));

    }

    /// <summary>Where a menu opened from the toolbar goes: under the viewport's top left.</summary>
    /// <remarks>
    /// Clear of the toolbar rather than against it. A flyout whose top edge meets the bottom edge of
    /// the button that opened it reads as one tall panel instead of two things.
    /// </remarks>
    private static (float X, float Y) MenuAt =>
        (EditorShell.Scene.X + 4f, EditorShell.Scene.Y + 46f);

    /// <summary>What can be put into the world.</summary>
    /// <remarks>
    /// Everything spawned here is named, because the world panel lists names and the world file
    /// matches entities up by them: something spawned and left unnamed could not be saved.
    /// </remarks>
    private static void Spawning()
    {
        EditorMenu.Command(
            "Spawn/Empty",
            static world => Spawn(world, "Empty", null),
            0,
            "icons/ui/entity.png");

        EditorMenu.Command(
            "Spawn/Cube",
            static world => Spawn(world, "Cube", MeshShape.Cuboid, 1f, 1f, 1f),
            1,
            "icons/ui/cube.png");

        EditorMenu.Command(
            "Spawn/Sphere",
            static world => Spawn(world, "Sphere", MeshShape.Sphere, 0.5f),
            2,
            "icons/ui/mesh.png");

        EditorMenu.Command(
            "Spawn/Capsule",
            static world => Spawn(world, "Capsule", MeshShape.Capsule, 0.4f, 1f),
            3,
            "icons/ui/mesh.png");

        EditorMenu.Command(
            "Spawn/Plane",
            static world => Spawn(world, "Plane", MeshShape.Plane, 4f, 4f),
            4,
            "icons/ui/mesh.png");

        EditorMenu.Command(
            "Spawn/Light/Point",
            static world => Light(world, "Point light", LightKind.Point, 100_000f),
            5,
            "icons/ui/light.png");

        EditorMenu.Command(
            "Spawn/Light/Spot",
            static world => Light(world, "Spot light", LightKind.Spot, 100_000f),
            6,
            "icons/ui/light.png");

        EditorMenu.Command(
            "Spawn/Light/Directional",
            static world => Light(world, "Directional light", LightKind.Directional, 10_000f),
            7,
            "icons/ui/light.png");
    }

    /// <summary>What can be done to whatever is selected.</summary>
    private static void Entities()
    {
        EditorMenu.Command(
            "Entity/Focus",
            static _ => FlyCameraFocus(),
            0,
            "icons/ui/select.png");

        EditorMenu.Command(
            "Entity/Unparent",
            static world =>
            {
                // Everything selected, because a menu opened over three chosen things is about
                // the three. One selected is the ordinary case and behaves as it always did.
                foreach (var entity in EditorSelection.All.ToArray())
                {
                    var previous = world.ParentOf(entity);
                    if (previous.IsNone) continue;

                    var moved = entity;

                    world.ClearParent(moved);
                    EditorHistory.Record(
                        "unparent",
                        undo => undo.SetParent(moved, previous),
                        redo => redo.ClearParent(moved));
                }
            },
            1,
            "icons/ui/remove.png");

        EditorMenu.Separator("Entity/-", 2);

        EditorMenu.Command(
            "Entity/Delete",
            static world =>
            {
                foreach (var entity in EditorSelection.All.ToArray()) world.Despawn(entity);

                EditorSelection.Clear();
            },
            3,
            "icons/ui/delete.png");
    }

    /// <summary>What the editor shows, as opposed to what is in the world.</summary>
    private static void View()
    {
        EditorMenu.Toggle(
            "View/Ground grid",
            static _ => ViewportGizmos.ShowGrid = !ViewportGizmos.ShowGrid,
            static () => ViewportGizmos.ShowGrid,
            1,
            "icons/ui/grid.png");

        EditorMenu.Toggle(
            "View/Snap to a grid",
            static _ => EditorTools.Snap = !EditorTools.Snap,
            static () => EditorTools.Snap,
            2,
            "icons/ui/snap.png");

        EditorMenu.Toggle(
            "View/Handles on the thing's own axes",
            static _ => EditorTools.Space = EditorTools.Space == ToolSpace.Local
                ? ToolSpace.Global
                : ToolSpace.Local,
            static () => EditorTools.Space == ToolSpace.Local,
            3,
            "icons/ui/move.png");

        EditorMenu.Separator("View/-", 4);

    }

    /// <summary>What keeps and restores the work.</summary>
    private static void Project()
    {
        EditorMenu.Command("Project/Save", EditorProject.Save, 0, icon: "icons/ui/save.png");
        EditorMenu.Command("Project/Load", EditorProject.Load, 1, icon: "icons/ui/folder.png");
        EditorMenu.Separator("Project/-", 2);
        EditorMenu.Command(
            "Project/Reload scripts",
            static _ => EditorScripts.Reload(),
            3,
            "icons/ui/script.png");
    }

    /// <summary>Spawns a mesh in front of the camera.</summary>
    private static void Spawn(
        EcsWorld world, string name, string shape, float a = 1f, float b = 1f, float c = 1f)
    {
        Spawn(world, name, entity =>
        {
            Render.SetMesh(world, entity, Render.CreateMesh(shape, a, b, c));
            Render.SetMaterial(world, entity, Render.CreateMaterial(0.72f, 0.72f, 0.75f));
        });
    }

    /// <summary>Spawns a light, which the engine builds rather than something this side attaches.</summary>
    private static void Light(EcsWorld world, string name, LightKind kind, float intensity)
    {
        var entity = Render.SpawnLight(new LightSettings { Kind = kind, Intensity = intensity });

        world.Add(entity, Transform.LookingAt(Ahead(world), Vec3.Zero, Vec3.UnitY));
        world.SetName(entity, name);
        Finish(world, entity, name);
    }

    /// <summary>Spawns something named, selects it, and records how to take it back.</summary>
    private static void Spawn(EcsWorld world, string name, Action<Entity>? build)
    {
        var entity = world.Spawn();

        var where = Ahead(world);
        world.Add(entity, Transform.At(where.X, where.Y, where.Z));
        world.SetName(entity, name);
        build?.Invoke(entity);

        Finish(world, entity, name);
    }

    /// <summary>Selects what was spawned and records the spawn.</summary>
    private static void Finish(EcsWorld world, Entity entity, string name)
    {
        EditorSelection.Select(entity);

        // Undoing a spawn is exact: despawning what was just made, which nothing else has heard
        // of yet. Redoing it cannot put the same entity back, so the redo says so by doing
        // nothing rather than by spawning something that looks like it and is not.
        EditorHistory.Record(
            $"spawn {name}",
            undo => undo.Despawn(entity),
            static _ => { });
    }

    /// <summary>
    /// A point a little in front of the camera, which is where a new thing belongs.
    /// </summary>
    /// <remarks>
    /// Spawning at the origin puts things inside each other and out of view. Every editor puts a
    /// new object where the person is looking, and where the person is looking is the camera.
    /// </remarks>
    private static Vec3 Ahead(EcsWorld world)
    {
        var camera = EditorSelection.Camera;
        if (camera.IsNone || !world.TryGet<Transform>(camera, out var transform)) return Vec3.Zero;

        return transform.Translation + (transform.Rotation * new Vec3(0f, 0f, -5f));
    }

    /// <summary>Points the camera at the selection, which is what F does.</summary>
    /// <remarks>
    /// The camera reads this on its next update rather than being moved from here, because where
    /// the camera is is the fly camera's business and two things writing one transform is how a
    /// camera ends up fighting itself.
    /// </remarks>
    private static void FlyCameraFocus() => Behaviors.FlyCamera.FrameWanted = true;
}
