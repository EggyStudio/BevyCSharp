// Bevy's clustered_decals example, examples/3d/clustered_decals.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

using Visibility = Bevy.Reflected.VisibilityRef.ValueVariant;

// Demonstrates clustered decals, which affix decals to surfaces, here two of Bevy's icon on a
// turning cube, drawn by a shader that tints each by its tag, the first red and the second blue.
// Drag to move the camera or the chosen decal round the cube, and while a decal is chosen the two
// number inputs scale it and roll it.
internal static class ClusteredDecals
{
    private const float MoveSpeed = 0.008f;
    internal const float RustEpsilon = 1.1920929e-7f;

    // Bevy's AppStatus resource, what dragging moves, and whether the drag under way began on a
    // number input, which then sets its number and moves nothing.
    private static DecalSelectionKind _selection;
    private static bool _dragOnInput;

    public static void Configure(Config config) => config.Feathers = true;

    public static void Build(App app)
    {
        (_selection, _dragOnInput) = (DecalSelectionKind.Camera, false);

        app.Startup(Setup, "clustered_decals.Setup");
        app.Update(DrawGizmos, "clustered_decals.DrawGizmos");
        app.Update(HandleDragAsMovement, "clustered_decals.HandleDragAsMovement");
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

        // The choice of what dragging moves, and the inputs that scale and roll a decal, hidden
        // while the camera is chosen, as it is first.
        var column = Radio.MainUiNode();
        Ui.SelfUpdate(Radio.FeathersOptionButtons(ecs, column, "Drag to Move",
            [(DecalSelectionKind.Camera, "Camera"), (DecalSelectionKind.DecalA, "Decal A"), (DecalSelectionKind.DecalB, "Decal B")], 0), UiWidgetKind.RadioGroup);
        foreach (var row in new[]
        {
            NumberInputs.NumberInputF32<AppNumberInput>(ecs, column, "Scale Multiplier", new AppNumberInput { Kind = AppNumberInputKind.Scale }, 1f, 2, 0.05f, 10f),
            // Short of π at each end, as Bevy's is, since a roll worked out again may come back as
            // either. Rust's f32::EPSILON is the gap above one, which .NET's float.Epsilon is not.
            NumberInputs.NumberInputF32<AppNumberInput>(ecs, column, "Roll (-π to π)", new AppNumberInput { Kind = AppNumberInputKind.Roll }, 0f, 2, -MathF.PI + RustEpsilon, MathF.BitDecrement(MathF.PI)),
        })
            ecs.Wrap<VisibilityRef>(row).Value = Visibility.Hidden;
        ecs.Observe<ValueChange<Entity>>(on => HandleSelectionChange(on.Ecs, on.Event.Value));
        ecs.Observe<ValueChange<float>>(on => HandleValueChangeNumberInput(on.Ecs, on.Event.Source, on.Event.Value));

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
        ecs.Add(decal, new BaseScale { Value = transform.Scale });
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

    // Bevy's handle_value_change_number_input, the chosen decal scaled from where it began, or
    // rolled with its yaw and pitch kept, and the value written back to the input.
    private static void HandleValueChangeNumberInput(EcsWorld ecs, Entity source, float value)
    {
        if (_selection == DecalSelectionKind.Camera || !ecs.TryGet<AppNumberInput>(source, out var input)) return;

        var entity = Selected(ecs, _selection);
        var transform = ecs.GetOrDefault<Transform>(entity);
        if (input.Kind == AppNumberInputKind.Scale) transform.Scale = ecs.GetOrDefault<BaseScale>(entity).Value * value;
        else
        {
            var euler = transform.Rotation.ToEuler();
            transform.Rotation = Quat.FromEuler(euler.X, euler.Y, value);
        }

        ecs.Set(entity, transform);
        Feathers.SetNumber(source, value);
    }

    // Bevy's handle_selection_change, the number inputs hidden while the camera is chosen, and
    // otherwise shown with the scale and roll of the decal chosen.
    private static void HandleSelectionChange(EcsWorld ecs, Entity button)
    {
        if (!Radio.OptionValue<DecalSelectionKind>(button, out var selection)) return;
        _selection = selection;

        var entity = Selected(ecs, selection);
        var transform = ecs.GetOrDefault<Transform>(entity);
        foreach (var input in ecs.EntitiesWith<AppNumberInput>())
        {
            var camera = selection == DecalSelectionKind.Camera;
            ecs.Wrap<VisibilityRef>(ecs.ParentOf(input)).Value = camera ? Visibility.Hidden : Visibility.Inherited;
            if (camera) continue;

            Feathers.SetNumber(input, ecs.GetOrDefault<AppNumberInput>(input).Kind == AppNumberInputKind.Scale
                ? transform.Scale.X / ecs.GetOrDefault<BaseScale>(entity).Value.X
                : transform.Rotation.ToEuler().Z);
        }

        foreach (var help in ecs.EntitiesWith<ClusteredDecalsHelpText>()) Ui.SetText(help, HelpText());
    }

    // Bevy's handle_drag_as_movement, the chosen thing moved round the middle at the same distance,
    // keeping the roll it had, unless the drag began on a number input.
    private static void HandleDragAsMovement(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var ecs = ctx.Ecs;
        if (input.MousePressed(MouseButton.Left)) _dragOnInput = OverNumberInput(ecs);
        if (!input.MouseDown(MouseButton.Left) || _dragOnInput) return;

        var (dx, dy) = input.MouseDelta;
        if (dx == 0f && dy == 0f) return;

        var entity = Selected(ecs, _selection);
        var transform = ecs.GetOrDefault<Transform>(entity);
        var position = transform.Translation;
        var radius = position.Length;
        var theta = MathF.Acos(position.Y / radius);
        var phi = MathF.Sign(position.Z) * MathF.Acos(position.X / MathF.Sqrt(position.X * position.X + position.Z * position.Z));

        // The camera moves the other way to a decal.
        var (phiFactor, thetaFactor) = _selection == DecalSelectionKind.Camera ? (1f, -1f) : (-1f, 1f);
        phi += phiFactor * dx * MoveSpeed;
        theta = Math.Clamp(theta + thetaFactor * dy * MoveSpeed, 0.001f, MathF.PI - 0.001f);

        var roll = transform.Rotation.ToEuler().Z;
        var at = radius * new Vec3(MathF.Sin(theta) * MathF.Cos(phi), MathF.Cos(theta), MathF.Sin(theta) * MathF.Sin(phi));
        var looking = Transform.LookingAt(at, Vec3.Zero, Vec3.UnitY).Rotation.ToEuler();
        ecs.Set(entity, transform with { Translation = at, Rotation = Quat.FromEuler(looking.X, looking.Y, roll) });
    }

    // Whether the pointer is over a number input, which Feathers marks hovered on the input or
    // within it.
    internal static bool OverNumberInput(EcsWorld ecs)
    {
        foreach (var input in ecs.EntitiesWith<AppNumberInput>())
            if (Hovered(input)) return true;
        return false;

        bool Hovered(Entity entity) => ecs.Get<HoveredRef>(entity)?.Value == true || ecs.ChildrenOf(entity).Any(Hovered);
    }

    private static string HelpText() => _selection switch
    {
        DecalSelectionKind.Camera => "Click and drag to move camera.",
        _ => $"Click and drag to move/scale/rotate {(_selection == DecalSelectionKind.DecalA ? "decal A" : "decal B")}.\n"
            + "To scale/rotate, start the drag within the corresponding number input.\n"
            + "To move, start the drag anywhere else in the example.",
    };
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

/// <summary>Which of a decal's or light's settings a number input sets, as Bevy's <c>AppNumberInput</c> names them.</summary>
public enum AppNumberInputKind { Scale, Roll }

/// <summary>
/// Which setting a number input of clustered_decals or light_textures sets, Bevy's
/// <c>AppNumberInput</c>, which both examples have.
/// </summary>
[Behavior]
public partial struct AppNumberInput
{
    /// <summary>Which.</summary>
    public AppNumberInputKind Kind;
}

/// <summary>
/// The scale a decal or light began with, which a number input of clustered_decals or
/// light_textures multiplies, Bevy's <c>BaseScale</c>.
/// </summary>
[Behavior]
public partial struct BaseScale
{
    /// <summary>The scale.</summary>
    public Vec3 Value;
}
