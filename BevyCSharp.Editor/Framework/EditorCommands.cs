using System.Globalization;
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
        // The four branches of the menu, so the top level wears pictures like every row under it
        // and sits in an order somebody decided rather than the alphabet's.
        EditorMenu.Branch("Entity", EditorIcons.Entity, 0);
        EditorMenu.Branch("Spawn", EditorIcons.Add, 1);
        EditorMenu.Branch("View", EditorIcons.View, 2);
        EditorMenu.Branch("Project", EditorIcons.Project, 3);

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
            ["origins", "center"],
            static () => EditorTools.Pivot == ToolPivot.Center ? "center" : "origins",
            static chosen => EditorTools.Pivot =
                chosen == "center" ? ToolPivot.Center : ToolPivot.Origins,
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

        // A box round what is selected, its own edges, or both. The box never hides what it marks
        // and the edges say exactly which thing was picked, which is the trade.
        EditorSettings.Choice(
            "Editor",
            "Selection outline",
            ["Box", "Mesh", "Both"],
            static () => SelectionOutline.Mark.ToString(),
            static chosen =>
            {
                if (Enum.TryParse<SelectionMark>(chosen, out var mark)) SelectionOutline.Mark = mark;
            },
            8);

        // How the editor is arranged, which is a preference like any other and saved with the
        // rest of them, so it opens the way it was left rather than the way it was written.
        EditorSettings.Heading("Editor", "Arrangement", 20);

        EditorSettings.Flag(
            "Editor",
            "Docked",
            static () => EditorShell.Docked,
            static on => EditorShell.Docked = on,
            21);

        EditorSettings.Number(
            "Editor",
            "Panel width",
            static () => EditorShell.PanelWidth,
            static width => EditorShell.PanelWidth = width,
            22);

        EditorSettings.Number(
            "Editor",
            "What the world gets of it",
            static () => EditorShell.WorldShare,
            static share => EditorShell.WorldShare = Math.Clamp(share, 0.15f, 0.85f),
            23);

        EditorSettings.Number(
            "Editor",
            "Tab height",
            static () => EditorShell.TabHeight,
            static height => EditorShell.TabHeight = MathF.Max(80f, height),
            24);

        // By name, because a number would mean a different panel the moment a game adds one of its
        // own.
        EditorSettings.Choice(
            "Editor",
            "Open panel",
            [Shut, .. EditorShell.Tabs.Select(static tab => tab.Name)],
            static () => EditorShell.OpenTab >= 0 && EditorShell.OpenTab < EditorShell.Tabs.Count
                ? EditorShell.Tabs[EditorShell.OpenTab].Name
                : Shut,
            static chosen =>
            {
                EditorShell.OpenTab = -1;
                if (chosen != Shut) EditorShell.Show(chosen);
            },
            25);

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

        // What the editor is made of, which is every table something registered itself in. A count
        // that reads as zero is a table nothing reached, which is worth knowing.
        EditorSettings.Heading("About", "BevyCSharp.Editor", 0);

        EditorSettings.Fact("About", "Component schemas", static () => Count(ComponentSchemas.All.Count), 1);
        EditorSettings.Fact("About", "Menu rows", static () => Count(EditorMenu.All.Count), 2);
        EditorSettings.Fact("About", "Toolbar buttons", static () => Count(EditorToolbar.All.Count), 3);
        EditorSettings.Fact("About", "Panels", static () => Count(EditorShell.Tabs.Count), 4);
        EditorSettings.Fact("About", "Settings", static () => Count(EditorSettings.All.Count), 5);
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
            EditorIcons.Menu,
            string.Empty,
            static _ => EditorFlyout.ToggleMenu(string.Empty, MenuAt.X, MenuAt.Y),
            0,
            "Menu  F1");

        EditorToolbar.Add(
            ToolbarSlot.Left,
            EditorIcons.Undo,
            string.Empty,
            static world => EditorHistory.Undo(world),
            1,
            "Undo  Ctrl+Z",
            static () => EditorHistory.CanUndo);

        EditorToolbar.Add(
            ToolbarSlot.Left,
            EditorIcons.Redo,
            string.Empty,
            static world => EditorHistory.Redo(world),
            2,
            "Redo  Ctrl+Y",
            static () => EditorHistory.CanRedo);

        EditorToolbar.Add(
            ToolbarSlot.Left,
            EditorIcons.Save,
            string.Empty,
            EditorProject.Save,
            3,
            "Save the project  Ctrl+S");

        foreach (var (_, tool, key) in EditorTools.Keys)
        {
            var chosen = tool;

            EditorToolbar.Add(new ToolbarButton(
                ToolbarSlot.Center,
                EditorIcons.For(tool),
                static () => string.Empty,
                _ => EditorTools.Current = chosen,
                () => EditorTools.Current == chosen,
                (int)tool,
                $"{tool}  {key}"));
        }

        // A word rather than a picture, because there is no picture of "the world's axes" that
        // anyone reads faster than the word for it.
        EditorToolbar.Add(new ToolbarButton(
            ToolbarSlot.Center,
            null,
            static () => EditorTools.Space == ToolSpace.Local ? "local" : "global",
            static _ => EditorTools.Space = EditorTools.Space == ToolSpace.Local
                ? ToolSpace.Global
                : ToolSpace.Local,
            static () => EditorTools.Space == ToolSpace.Local,
            9,
            "Handles on the thing's own axes  X"));

        // The other thing a drag on the handles has to be told, and a word for the same reason,
        // because no picture says "about each thing's own origin" faster than the word does.
        EditorToolbar.Add(new ToolbarButton(
            ToolbarSlot.Center,
            null,
            static () => EditorTools.Pivot == ToolPivot.Center ? "center" : "origins",
            static _ => EditorTools.Pivot = EditorTools.Pivot == ToolPivot.Center
                ? ToolPivot.Origins
                : ToolPivot.Center,
            static () => EditorTools.Pivot == ToolPivot.Center,
            10,
            "Turn and scale about the middle of what is picked"));

        EditorToolbar.Add(new ToolbarButton(
            ToolbarSlot.Center,
            EditorIcons.Snap,
            static () => string.Empty,
            static _ =>
            {
                EditorKeys.SnapLocked = !EditorKeys.SnapLocked;
                EditorTools.Snap = EditorKeys.SnapLocked;
            },
            static () => EditorTools.Snap,
            11,
            "Snap to a grid, which holding Control does as well"));

        // What the keys do here, kept out of the way until it is asked for. In the corner that is
        // for what describes the view rather than for what acts on it.
        EditorToolbar.Add(new ToolbarButton(
            ToolbarSlot.BottomRight,
            EditorIcons.Info,
            static () => string.Empty,
            static _ => ToolbarView.ShowKeys = !ToolbarView.ShowKeys,
            static () => ToolbarView.ShowKeys,
            0,
            "What the keys do here"));

    }

    /// <summary>Where a menu opened from the toolbar goes: under the viewport's top left.</summary>
    /// <remarks>
    /// Clear of the toolbar rather than against it. A flyout whose top edge meets the bottom edge of
    /// the button that opened it reads as one tall panel instead of two things.
    /// </remarks>
    private static (float X, float Y) MenuAt =>
        (EditorShell.Scene.X + ToolbarView.Inset,
            EditorShell.Scene.Y + ToolbarView.Inset + EditorSurface.Tall + EditorSurface.Air);

    /// <summary>What the open panel reads as when there is none.</summary>
    private const string Shut = "none";

    /// <summary>A number as a fact says it, which is the same wherever the editor is run.</summary>
    private static string Count(int many) => many.ToString(CultureInfo.InvariantCulture);

    /// <summary>What can be put into the world.</summary>
    /// <remarks>
    /// Everything spawned here is named, because the world panel lists names and the world file
    /// matches entities up by them, so something spawned and left unnamed could not be saved.
    /// </remarks>
    private static void Spawning()
    {
        EditorMenu.Command(
            "Spawn/Empty",
            static world => Spawn(world, "Empty", null),
            0,
            EditorIcons.Entity);

        EditorMenu.Command(
            "Spawn/Cube",
            static world => Spawn(world, "Cube", MeshShape.Cuboid, 1f, 1f, 1f),
            1,
            EditorIcons.Cube);

        EditorMenu.Command(
            "Spawn/Sphere",
            static world => Spawn(world, "Sphere", MeshShape.Sphere, 0.5f),
            2,
            EditorIcons.Mesh);

        EditorMenu.Command(
            "Spawn/Capsule",
            static world => Spawn(world, "Capsule", MeshShape.Capsule, 0.4f, 1f),
            3,
            EditorIcons.Mesh);

        EditorMenu.Command(
            "Spawn/Plane",
            static world => Spawn(world, "Plane", MeshShape.Plane, 4f, 4f),
            4,
            EditorIcons.Mesh);

        EditorMenu.Command(
            "Spawn/Light/Point",
            static world => Light(world, "Point light", LightKind.Point, 100_000f),
            5,
            EditorIcons.Light);

        EditorMenu.Command(
            "Spawn/Light/Spot",
            static world => Light(world, "Spot light", LightKind.Spot, 100_000f),
            6,
            EditorIcons.Light);

        EditorMenu.Command(
            "Spawn/Light/Directional",
            static world => Light(world, "Directional light", LightKind.Directional, 10_000f),
            7,
            EditorIcons.Light);
    }

    /// <summary>What can be done to whatever is selected.</summary>
    /// <remarks>
    /// Every one of these is about the selection, so every one of them says so by being dim while
    /// there is none. A row that can be pressed and does nothing teaches nothing.
    /// </remarks>
    private static void Entities()
    {
        EditorMenu.Command(
            "Entity/Focus",
            static _ => FlyCameraFocus(),
            0,
            EditorIcons.Select,
            "F",
            static () => EditorSelection.Any);

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
            EditorIcons.Remove,
            enabled: static () => EditorSelection.Any);

        EditorMenu.Separator("Entity/-", 2);

        EditorMenu.Command(
            "Entity/Delete",
            static world =>
            {
                foreach (var entity in EditorSelection.All.ToArray()) world.Despawn(entity);

                EditorSelection.Clear();
            },
            3,
            EditorIcons.Delete,
            "Del",
            static () => EditorSelection.Any);
    }

    /// <summary>What the editor shows, as opposed to what is in the world.</summary>
    private static void View()
    {
        // A row for every tab along the bottom, built from the list of them rather than written
        // out, so a tab a game adds appears here as well without anybody saying so twice.
        for (var index = 0; index < EditorShell.Tabs.Count; index++)
        {
            var which = index;

            EditorMenu.Toggle(
                $"View/Panels/{EditorShell.Tabs[index].Name}",
                _ => EditorShell.OpenTab = EditorShell.OpenTab == which ? -1 : which,
                () => EditorShell.OpenTab == which,
                which);
        }

        EditorMenu.Branch("View/Panels", EditorIcons.Interface, 0);

        EditorMenu.Toggle(
            "View/Ground grid",
            static _ => ViewportGizmos.ShowGrid = !ViewportGizmos.ShowGrid,
            static () => ViewportGizmos.ShowGrid,
            1,
            EditorIcons.Grid);

        EditorMenu.Toggle(
            "View/Snap to a grid",
            static _ => EditorTools.Snap = !EditorTools.Snap,
            static () => EditorTools.Snap,
            2,
            EditorIcons.Snap);

        EditorMenu.Toggle(
            "View/Handles on the thing's own axes",
            static _ => EditorTools.Space = EditorTools.Space == ToolSpace.Local
                ? ToolSpace.Global
                : ToolSpace.Local,
            static () => EditorTools.Space == ToolSpace.Local,
            3,
            EditorIcons.Move,
            "X");

        EditorMenu.Separator("View/-", 4);

    }

    /// <summary>What keeps and restores the work.</summary>
    private static void Project()
    {
        EditorMenu.Command("Project/Save", EditorProject.Save, 0, EditorIcons.Save, "Ctrl+S");
        EditorMenu.Command("Project/Load", EditorProject.Load, 1, icon: EditorIcons.Folder);
        EditorMenu.Separator("Project/-", 2);
        EditorMenu.Command(
            "Project/Reload scripts",
            static _ => EditorScripts.Reload(),
            3,
            EditorIcons.Script);
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
        var called = Unused(world, name);

        var where = Ahead(world);
        world.Add(entity, Transform.At(where.X, where.Y, where.Z));
        world.SetName(entity, called);
        build?.Invoke(entity);

        Finish(world, entity, called);
    }

    /// <summary>
    /// The name asked for, or the first one after it that nothing is called.
    /// </summary>
    /// <remarks>
    /// A name is how the editor tells one entity from another. The world file matches a saved
    /// entity back up by it, and a selection that survives a script reload is found again by it, so
    /// a second thing called Cube is a thing the editor confuses with the first.
    /// </remarks>
    /// <param name="world">The world to look in.</param>
    /// <param name="name">What it would be called.</param>
    private static string Unused(EcsWorld world, string name)
    {
        var taken = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entity in world.All())
        {
            if (world.NameOf(entity) is { Length: > 0 } called) taken.Add(called);
        }

        if (!taken.Contains(name)) return name;

        for (var next = 2; ; next++)
        {
            var tried = $"{name} {next}";

            if (!taken.Contains(tried)) return tried;
        }
    }

    /// <summary>Selects what was spawned and records the spawn.</summary>
    private static void Finish(EcsWorld world, Entity entity, string name)
    {
        EditorSelection.Select(entity);

        // Undoing a spawn is exact, since it despawns what was just made and nothing else has
        // heard of it yet. Redoing it cannot put the same entity back, so the redo says so by doing
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
