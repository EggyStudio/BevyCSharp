// Bevy's blend_modes example, examples/3d/blend_modes.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using System.Globalization;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Shows Bevy's blend modes, the same red sphere drawn opaque, blended, premultiplied, added and
// multiplied over a checkered floor, with keys for its alpha, the camera, HDR, lighting and color.
internal static class BlendModes
{
    private static Entity _camera;
    private static float _alpha;
    private static bool _unlit, _hdr;
    private static Random _random = new();

    public static void Build(App app)
    {
        (_alpha, _unlit, _hdr) = (0.9f, false, true);
        _random = new Random(19878367);

        app.Startup(Setup, "blend_modes.Setup");
        app.Update(Control, "blend_modes.ExampleControlSystem");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        // Bevy sets every material's alpha from the first frame on, so it starts at the alpha.
        var baseColor = Color.FromSrgb(0.9f, 0.2f, 0.3f) with { A = _alpha };
        var sphere = Render.CreateMesh(MeshShape.Sphere, 0.9f);

        var spheres = new List<Entity>();
        var modes = new[] { AlphaMode.Opaque, AlphaMode.Blend, AlphaMode.Premultiplied, AlphaMode.Add, AlphaMode.Multiply };
        for (var i = 0; i < modes.Length; i++)
        {
            var material = Render.CreateMaterial(new MaterialSettings { BaseColor = baseColor, AlphaMode = modes[i] });
            var entity = ecs.SpawnMesh(sphere, material, Transform.At(-4f + 2f * i, 0f, 0f));
            ecs.Add(entity, new ExampleControls { Unlit = true, Color = true });
            spheres.Add(entity);
        }

        // A checkered floor, which the keys recolor too but never light differently.
        var blackMaterial = Render.CreateMaterial(new MaterialSettings { BaseColor = (0f, 0f, 0f, _alpha) });
        var whiteMaterial = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, _alpha) });
        var plane = Render.CreateMesh(MeshShape.Plane, 2f, 2f);
        for (var x = -3; x < 4; x++)
        {
            for (var z = -3; z < 4; z++)
            {
                var tile = ecs.SpawnMesh(plane, (x + z) % 2 == 0 ? blackMaterial : whiteMaterial, Transform.At(x * 2f, -1f, z * 2f));
                ecs.Add(tile, new ExampleControls { Unlit = false, Color = true });
            }
        }

        ecs.SpawnPointLight(new Vec3(4f, 8f, 4f));

        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 2.5f, 10f), Vec3.Zero, Vec3.UnitY));
        Render.SetPostProcessing(_camera, new PostSettings { Hdr = _hdr });

        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraMono-Medium.ttf");
        var style = new UiTextSettings { Font = font };
        Ui.SpawnText(
            "Up / Down — Increase / Decrease Alpha\nLeft / Right — Rotate Camera\nH - Toggle HDR\nSpacebar — Toggle Unlit\nC — Randomize Colors",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) }, style);
        ecs.Add(Ui.SpawnText(Describe(), new UiSettings { Absolute = true, Top = Length.Px(12f), Right = Length.Px(12f) }, style), new BlendModesDisplay());

        // Each label's lines run up from a point over its sphere, which follows the sphere on screen.
        var orange = Color.FromSrgb(1f, 165f / 255f, 0f);
        var labelStyle = new UiTextSettings { Font = font, Wrap = TextWrap.NoWrap };
        string[] names = ["┌─ Opaque\n│\n│\n│\n│", "┌─ Blend\n│\n│\n│", "┌─ Premultiplied\n│\n│", "┌─ Add\n│", "┌─ Multiply"];
        for (var i = 0; i < names.Length; i++)
        {
            var label = Ui.SpawnNode(new UiSettings { Absolute = true });
            var text = Ui.SpawnText(names[i], new UiSettings { Absolute = true, Bottom = Length.Zero }, labelStyle);
            ecs.Wrap<TextColorRef>(text).Value = new Color(orange.R, orange.G, orange.B, orange.A);
            ecs.SetParent(text, label);
            ecs.Add(label, new ExampleLabel { Entity = spheres[i] });
        }
    }

    private static void Control(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var ecs = ctx.Ecs;
        var delta = ctx.Time.Delta;

        var changed = false;
        if (input.KeyDown(Key.ArrowUp)) (_alpha, changed) = (MathF.Min(_alpha + delta, 1f), true);
        else if (input.KeyDown(Key.ArrowDown)) (_alpha, changed) = (MathF.Max(_alpha - delta, 0f), true);

        if (input.KeyPressed(Key.Space)) (_unlit, changed) = (!_unlit, true);

        // Each controlled mesh's material given the alpha, a random color where it takes one, and
        // the lighting where it is one the keys change, through the mesh as Bevy reaches it, a
        // material the floor's tiles share being written for each, as it is in Bevy.
        var randomize = input.KeyPressed(Key.C);
        if (changed || randomize)
        {
            foreach (var entity in ecs.EntitiesWith<ExampleControls>())
            {
                var controls = ecs.GetOrDefault<ExampleControls>(entity);
                var material = Render.MaterialOf(ecs, entity);
                if (!Render.TryReadMaterial(material, out var settings) || settings is null) continue;

                var (r, g, b, _) = settings.BaseColor;
                if (controls.Color && randomize) (r, g, b) = (Linear(_random.NextSingle()), Linear(_random.NextSingle()), Linear(_random.NextSingle()));
                settings.BaseColor = (r, g, b, _alpha);
                if (controls.Unlit) settings.Unlit = _unlit;
                Render.WriteMaterial(material, settings);
            }
        }

        if (input.KeyPressed(Key.H))
        {
            _hdr = !_hdr;
            Render.SetPostProcessing(_camera, new PostSettings { Hdr = _hdr });
        }

        // Turned about the origin, as Bevy's rotate_around.
        var rotation = input.KeyDown(Key.ArrowLeft) ? delta : input.KeyDown(Key.ArrowRight) ? -delta : 0f;
        if (rotation != 0f)
        {
            var turn = Quat.FromRotationY(rotation);
            var camera = ecs.GetOrDefault<Transform>(_camera);
            ecs.Set(_camera, new Transform(turn * camera.Translation, turn * camera.Rotation, camera.Scale));
        }

        foreach (var label in ecs.EntitiesWith<ExampleLabel>())
        {
            var at = ecs.GetOrDefault<GlobalTransform>(ecs.GetOrDefault<ExampleLabel>(label).Entity).Translation + Vec3.UnitY;
            if (!Render.TryProject(_camera, at, out var x, out var y)) continue;
            var node = ecs.Wrap<NodeRef>(label);
            (node.Top, node.Left) = (new Val.Px(y), new Val.Px(x));
        }

        foreach (var display in ecs.EntitiesWith<BlendModesDisplay>()) Ui.SetText(display, Describe());
    }

    // Bevy's random sRGB channel, as linear.
    private static float Linear(float srgb) => Color.FromSrgb(srgb, 0f, 0f).R;

    private static string Describe() => string.Create(CultureInfo.InvariantCulture, $"  HDR: {(_hdr ? "ON " : "OFF")}\nAlpha: {_alpha:0.00}");
}

/// <summary>A mesh whose material the keys change, and which of their changes it takes.</summary>
[Behavior]
public partial struct ExampleControls
{
    /// <summary>Whether Space turns its lighting off and on.</summary>
    public bool Unlit;

    /// <summary>Whether C gives it a random color.</summary>
    public bool Color;
}

/// <summary>A label that follows a sphere on screen.</summary>
[Behavior]
public partial struct ExampleLabel
{
    /// <summary>The sphere.</summary>
    public Entity Entity;
}

/// <summary>
/// The text saying whether HDR is on and what the alpha is, Bevy's <c>ExampleDisplay</c> under
/// another name since auto_exposure's shares the namespace.
/// </summary>
[Behavior]
public partial struct BlendModesDisplay;
