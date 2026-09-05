using Bevy;
using BevyCSharp.Editor.Panels;

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
        Panels(camera);
        Spawning();
        Entities();
        View();
        Project();
        Toolbar();
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
            static _ => EditorShell.ShowMenu(string.Empty, MenuAt.X, MenuAt.Y),
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
            10));

        EditorToolbar.Add(new ToolbarButton(
            ToolbarSlot.Right,
            "icons/ui/info.png",
            static () => string.Empty,
            static _ =>
            {
                if (EditorShell.Find<InfoPanel>() is { } open)
                {
                    EditorShell.Hide(open);
                    return;
                }

                EditorShell.ShowAt(
                    new InfoPanel(),
                    EditorShell.Layout.Viewport.Right - 240f,
                    EditorShell.Layout.Viewport.Y + 44f);
            },
            static () => EditorShell.Find<InfoPanel>() is not null,
            0));
    }

    /// <summary>Where a menu opened from the toolbar goes: under the viewport's top left.</summary>
    private static (float X, float Y) MenuAt =>
        (EditorShell.Layout.Viewport.X + 4f, EditorShell.Layout.Viewport.Y + 36f);

    /// <summary>What can be opened, as toggles so the menu shows what already is.</summary>
    private static void Panels(Entity camera)
    {
        EditorMenu.Toggle(
            "Panels/World",
            static _ => EditorShell.ToggleShown(static () => new WorldPanel()),
            static () => EditorShell.Showing<WorldPanel>() is not null,
            0);

        EditorMenu.Toggle(
            "Panels/Data",
            static _ => EditorShell.ToggleShown(static () => new DataPanel()),
            static () => EditorShell.Showing<DataPanel>() is not null,
            1);

        EditorMenu.Toggle(
            "Panels/Assets",
            static _ =>
            {
                if (EditorTabs.Find("Assets") is { } tab) EditorTabs.Toggle(tab);
            },
            static () => EditorShell.Showing<AssetsPanel>() is not null,
            2);

        EditorMenu.Toggle(
            "Panels/Console",
            static _ =>
            {
                if (EditorTabs.Find("Console") is { } tab) EditorTabs.Toggle(tab);
            },
            static () => EditorShell.Showing<ConsolePanel>() is not null,
            3);

        EditorMenu.Toggle(
            "Panels/Rendering",
            _ => EditorShell.Toggle(() => new RenderingPanel(camera)),
            static () => EditorShell.Find<RenderingPanel>() is not null,
            4);

        EditorMenu.Toggle(
            "Panels/Info",
            static _ => EditorShell.Toggle(static () => new InfoPanel()),
            static () => EditorShell.Find<InfoPanel>() is not null,
            5);

        EditorMenu.Toggle(
            "Panels/Keys",
            static _ => EditorShell.Toggle(static () => new KeysPanel()),
            static () => EditorShell.Find<KeysPanel>() is not null,
            6);

        EditorMenu.Toggle(
            "Panels/Toolbar",
            static _ =>
            {
                EditorShell.Toggle(static () => new LeftBarPanel());
                EditorShell.Toggle(static () => new CentreBarPanel());
                EditorShell.Toggle(static () => new RightBarPanel());
                EditorShell.Toggle(static () => new BottomBarPanel());
            },
            static () => EditorShell.Find<CentreBarPanel>() is not null,
            7);

        EditorMenu.Toggle(
            "Panels/Tabs",
            static _ => EditorShell.ToggleShown(static () => new TabsPanel()),
            static () => EditorShell.Showing<TabsPanel>() is not null,
            8);
    }

    /// <summary>What can be put into the world.</summary>
    /// <remarks>
    /// Everything spawned here is named, because the world panel lists names and the world file
    /// matches entities up by them: something spawned and left unnamed could not be saved.
    /// </remarks>
    private static void Spawning()
    {
        EditorMenu.Command("Spawn/Empty", static world => Spawn(world, "Empty", null), 0);

        EditorMenu.Command(
            "Spawn/Cube",
            static world => Spawn(world, "Cube", MeshShape.Cuboid, 1f, 1f, 1f),
            1);

        EditorMenu.Command(
            "Spawn/Sphere",
            static world => Spawn(world, "Sphere", MeshShape.Sphere, 0.5f),
            2);

        EditorMenu.Command(
            "Spawn/Capsule",
            static world => Spawn(world, "Capsule", MeshShape.Capsule, 0.4f, 1f),
            3);

        EditorMenu.Command(
            "Spawn/Plane",
            static world => Spawn(world, "Plane", MeshShape.Plane, 4f, 4f),
            4);

        EditorMenu.Command(
            "Spawn/Light/Point",
            static world => Light(world, "Point light", LightKind.Point, 100_000f),
            5);

        EditorMenu.Command(
            "Spawn/Light/Spot",
            static world => Light(world, "Spot light", LightKind.Spot, 100_000f),
            6);

        EditorMenu.Command(
            "Spawn/Light/Directional",
            static world => Light(world, "Directional light", LightKind.Directional, 10_000f),
            7);
    }

    /// <summary>What can be done to whatever is selected.</summary>
    private static void Entities()
    {
        EditorMenu.Command(
            "Entity/Focus",
            static _ => FlyCameraFocus(),
            0);

        EditorMenu.Command(
            "Entity/Unparent",
            static world =>
            {
                var entity = EditorSelection.Current;
                if (entity.IsNone) return;

                var previous = world.ParentOf(entity);
                if (previous.IsNone) return;

                world.ClearParent(entity);
                EditorHistory.Record(
                    "unparent",
                    undo => undo.SetParent(entity, previous),
                    redo => redo.ClearParent(entity));
            },
            1);

        EditorMenu.Separator("Entity/-", 2);

        EditorMenu.Command(
            "Entity/Delete",
            static world =>
            {
                var entity = EditorSelection.Current;
                if (entity.IsNone) return;

                world.Despawn(entity);
                EditorSelection.Clear();
            },
            3);
    }

    /// <summary>What the editor shows, as opposed to what is in the world.</summary>
    private static void View()
    {
        EditorMenu.Toggle(
            "View/Interface entities",
            static _ => WorldPanel.ShowInterface = !WorldPanel.ShowInterface,
            static () => WorldPanel.ShowInterface,
            0);

        EditorMenu.Toggle(
            "View/Every entity",
            static _ => WorldPanel.ShowAll = !WorldPanel.ShowAll,
            static () => WorldPanel.ShowAll,
            1);

        EditorMenu.Toggle(
            "View/Snap to a grid",
            static _ => EditorTools.Snap = !EditorTools.Snap,
            static () => EditorTools.Snap,
            2);

        EditorMenu.Separator("View/-", 3);

        EditorMenu.Command(
            "View/Reset the layout",
            static _ => EditorShell.Layout.ResetAll(),
            4);
    }

    /// <summary>What keeps and restores the work.</summary>
    private static void Project()
    {
        EditorMenu.Command("Project/Save", EditorProject.Save, 0);
        EditorMenu.Command("Project/Load", EditorProject.Load, 1);
        EditorMenu.Separator("Project/-", 2);
        EditorMenu.Command("Project/Reload scripts", static _ => EditorScripts.Reload(), 3);
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
