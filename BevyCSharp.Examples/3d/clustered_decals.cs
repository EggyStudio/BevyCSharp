// Bevy's clustered_decals example, examples/3d/clustered_decals.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

using Visibility = Bevy.Reflected.VisibilityRef.ValueVariant;

// Demonstrates clustered decals, which affix decals to surfaces, here two of Bevy's icon on a
// turning cube, drawn by a shader that tints each by its tag, the first red and the second blue.
// Drag to move the camera or the chosen decal round the cube, or over the Scale and Roll buttons to
// change what dragging does.
internal static class ClusteredDecals
{
    private const float MoveSpeed = 0.008f;
    private const float ScaleSpeed = 0.05f;
    private const float RollSpeed = 0.01f;

    // Bevy's AppStatus resource, what dragging moves and how.
    private static DecalSelectionKind _selection;
    private static DragModeKind _dragMode;
    private static RadioButtons<DecalSelectionKind>? _selections;
    private static Entity _dragButtons;

    public static void Build(App app)
    {
        (_selection, _dragMode) = (DecalSelectionKind.Camera, DragModeKind.Move);

        app.Startup(Setup, "clustered_decals.Setup");
        app.Update(DrawGizmos, "clustered_decals.DrawGizmos");
        app.Update(HandleButtons, "clustered_decals.HandleButtons");
        app.Update(ProcessDrag, "clustered_decals.ProcessDrag");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // The cube the decals are projected onto, silver and turned a little to be more
        // interesting, drawn by the shader that tints each decal by its tag.
        var silver = Color.FromSrgb8(192, 192, 192);
        var material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/custom_clustered_decal.slang"))
            .Set("base_color", new Vector4(silver.R, silver.G, silver.B, 1f));
        var turned = new Transform(Vec3.Zero, Quat.FromRotationY(MathF.PI / 3f), Vec3.One);
        ecs.Add(ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 3f, 3f, 3f), material, turned), new Rotate());

        var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 2.5f, 9f), Vec3.Zero, Vec3.UnitY));
        ecs.Add(camera, new DecalSelection { Kind = DecalSelectionKind.Camera });

        // Bevy's default directional light.
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
        ecs.Add(light, Transform.LookingAt(new Vec3(4f, 8f, 4f), Vec3.Zero, Vec3.UnitY));

        var icon = AssetServer.Load(AssetKind.Image, "branding/icon.png");
        SpawnDecal(ecs, icon, tag: 1, InitialTransform(new Vec3(1f, 3f, 5f), Vec3.Zero, 1.1f), DecalSelectionKind.DecalA);
        SpawnDecal(ecs, icon, tag: 2, InitialTransform(new Vec3(-2f, -1f, 4f), Vec3.Zero, 2f), DecalSelectionKind.DecalB);

        _selections = new RadioButtons<DecalSelectionKind>(ecs, RadioButtons<DecalSelectionKind>.Column(), "Drag to Move",
            [(DecalSelectionKind.Camera, "Camera"), (DecalSelectionKind.DecalA, "Decal A"), (DecalSelectionKind.DecalB, "Decal B")],
            _selection);

        _dragButtons = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Direction = UiDirection.Row,
            Right = Length.Px(10f),
            Bottom = Length.Px(10f),
            ColumnGap = Length.Px(6f),
        });
        ecs.Add(DragButton(ecs, "Scale"), new DragMode { Kind = DragModeKind.Scale });
        ecs.Add(DragButton(ecs, "Roll"), new DragMode { Kind = DragModeKind.Roll });
        ShowDragButtons(ecs);

        ecs.Add(Ui.SpawnText(HelpText(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) }), new ClusteredDecalsHelpText());
    }

    private static void SpawnDecal(EcsWorld ecs, AssetHandle image, uint tag, Transform transform, DecalSelectionKind kind)
    {
        var decal = ecs.Spawn();
        ecs.Add(decal, transform);
        var clustered = ecs.Insert<ClusteredDecalRef>(decal);
        clustered.BaseColorTexture = image;
        clustered.Tag = tag;
        ecs.Add(decal, new DecalSelection { Kind = kind });
    }

    // A decal halfway from a point to what it looks at, as long as the way between and as wide as
    // it is asked, facing along that way, as Bevy's calculate_initial_decal_transform makes it.
    private static Transform InitialTransform(Vec3 start, Vec3 lookingAt, float size)
    {
        var direction = lookingAt - start;
        var center = start + direction * 0.5f;
        return Transform.LookingAt(center, center + direction, Vec3.UnitY) with { Scale = new Vec3(size * 0.5f, size * 0.5f, direction.Length) };
    }

    // The thing of a kind, found by its DecalSelection as Bevy's queries find it.
    private static Entity Selected(EcsWorld ecs, DecalSelectionKind kind)
    {
        foreach (var entity in ecs.EntitiesWith<DecalSelection>())
            if (ecs.GetOrDefault<DecalSelection>(entity).Kind == kind) return entity;
        return Entity.None;
    }

    // A button that changes what dragging does while the pointer is over it, as Bevy's does.
    private static Entity DragButton(EcsWorld ecs, string label)
    {
        var button = Ui.SpawnNode(new UiSettings
        {
            Interactive = true,
            Border = Sides.All(Length.Px(1f)),
            BorderColor = (1f, 1f, 1f, 1f),
            Color = (0f, 0f, 0f, 1f),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Padding = new Sides(Length.Px(12f), Length.Px(6f), Length.Px(12f), Length.Px(6f)),
            Corners = Corners.All(Length.Px(6f)),
        });
        ecs.SetParent(button, _dragButtons);
        ecs.SetParent(Ui.SpawnText(label, new UiSettings(), 18f), button);
        return button;
    }

    // Each decal's box, the unit cube its transform scales, orange red round the first and lime
    // round the second.
    private static void DrawGizmos(BehaviorContext ctx)
    {
        foreach (var entity in ctx.Ecs.EntitiesWith<DecalSelection>())
        {
            var color = ctx.Ecs.GetOrDefault<DecalSelection>(entity).Kind switch
            {
                DecalSelectionKind.DecalA => Color.FromSrgb8(255, 69, 0),
                DecalSelectionKind.DecalB => Color.FromSrgb8(0, 255, 0),
                _ => (Color?)null,
            };
            if (color is not { } outline) continue;

            var global = ctx.Ecs.GetOrDefault<GlobalTransform>(entity);
            Gizmos.Box(global.Translation, global.Rotation, global.Scale, outline);
        }
    }

    private static void HandleButtons(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var changed = false;

        if (_selections!.Pressed(out var selection) && selection != _selection)
        {
            _selection = selection;
            _selections.Select(ecs, selection);
            ShowDragButtons(ecs);
            changed = true;
        }

        // Over Scale or Roll dragging scales or rolls, and anywhere else it moves, unless a drag is
        // under way.
        if (!ctx.Input.MouseDown(MouseButton.Left))
        {
            var mode = DragModeKind.Move;
            foreach (var button in ecs.EntitiesWith<DragMode>())
                if (Ui.InteractionOf(button) == UiInteraction.Hovered) mode = ecs.GetOrDefault<DragMode>(button).Kind;

            if (mode != _dragMode)
            {
                _dragMode = mode;
                TrySetCursor(mode == DragModeKind.Move ? CursorShape.Default : CursorShape.ResizeHorizontal);
                changed = true;
            }
        }

        if (changed) foreach (var help in ecs.EntitiesWith<ClusteredDecalsHelpText>()) Ui.SetText(help, HelpText());
    }

    // Scale and Roll are for a decal, and hidden while the camera is chosen.
    private static void ShowDragButtons(EcsWorld ecs) =>
        ecs.Wrap<VisibilityRef>(_dragButtons).Value = _selection == DecalSelectionKind.Camera ? Visibility.Hidden : Visibility.Visible;

    private static void ProcessDrag(BehaviorContext ctx)
    {
        var input = ctx.Input;
        if (!input.MouseDown(MouseButton.Left)) return;

        var ecs = ctx.Ecs;
        var entity = Selected(ecs, _selection);
        var (dx, dy) = input.MouseDelta;
        var transform = ecs.GetOrDefault<Transform>(entity);

        switch (_dragMode)
        {
            case DragModeKind.Move:
                // Round the middle at the same distance, keeping the roll it had.
                var position = transform.Translation;
                var radius = position.Length;
                var theta = MathF.Acos(position.Y / radius);
                var phi = MathF.Sign(position.Z) * MathF.Acos(position.X / MathF.Sqrt(position.X * position.X + position.Z * position.Z));
                var (phiFactor, thetaFactor) = _selection == DecalSelectionKind.Camera ? (1f, -1f) : (-1f, 1f);
                phi += phiFactor * dx * MoveSpeed;
                theta = Math.Clamp(theta + thetaFactor * dy * MoveSpeed, 0.001f, MathF.PI - 0.001f);

                var roll = transform.Rotation.ToEuler().Z;
                var at = radius * new Vec3(MathF.Sin(theta) * MathF.Cos(phi), MathF.Cos(theta), MathF.Sin(theta) * MathF.Sin(phi));
                var looking = Transform.LookingAt(at, Vec3.Zero, Vec3.UnitY).Rotation.ToEuler();
                transform = transform with { Translation = at, Rotation = Quat.FromEuler(looking.X, looking.Y, roll) };
                break;

            case DragModeKind.Scale:
                transform.Scale *= 1f + dx * ScaleSpeed;
                break;

            case DragModeKind.Roll:
                var euler = transform.Rotation.ToEuler();
                transform.Rotation = Quat.FromEuler(euler.X, euler.Y, euler.Z + dx * RollSpeed);
                break;
        }

        ecs.Set(entity, transform);
    }

    private static string HelpText()
    {
        var mode = _dragMode.ToString().ToLowerInvariant();
        var selection = _selection switch
        {
            DecalSelectionKind.Camera => "camera",
            DecalSelectionKind.DecalA => "decal A",
            _ => "decal B",
        };
        return $"Click and drag to {mode} {selection}";
    }

    // An offscreen run has no window to set a cursor on.
    private static void TrySetCursor(CursorShape shape)
    {
        try { Window.SetCursorShape(shape); }
        catch (Bevy.Interop.BevyNativeException) { }
    }
}

/// <summary>What dragging moves, as Bevy's clustered_decals <c>Selection</c> enum names them.</summary>
public enum DecalSelectionKind { Camera, DecalA, DecalB }

/// <summary>
/// A thing dragging can move, and which of them it is, Bevy's <c>Selection</c> under another name
/// since light_textures' shares the namespace.
/// </summary>
[Behavior]
public partial struct DecalSelection
{
    /// <summary>Which.</summary>
    public DecalSelectionKind Kind;
}

/// <summary>
/// The help text, Bevy's <c>HelpText</c> under another name since color_grading's shares the
/// namespace.
/// </summary>
[Behavior]
public partial struct ClusteredDecalsHelpText;
