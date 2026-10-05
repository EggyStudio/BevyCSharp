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

    // A material the keys change, and whether its lighting is one of the things they change.
    private sealed record Controlled(AssetHandle Material, MaterialSettings Settings, bool Unlit);

    private static readonly List<Controlled> Materials = [];
    private static readonly List<(Entity Label, Entity Sphere)> Labels = [];
    private static Entity _camera, _display;
    private static float _alpha;
    private static bool _unlit, _hdr;
    private static Random _random = new();

    public static void Build(App app)
    {
        Materials.Clear();
        Labels.Clear();
        (_alpha, _unlit, _hdr) = (0.9f, false, true);
        _random = new Random(19878367);

        app.Startup(Setup, "blend_modes.Setup");
        app.Update(Control, "blend_modes.ExampleControlSystem");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        // Bevy sets every material's alpha from the first frame on, so it starts at the alpha.
        var baseColor = Scene.Srgb(0.9f, 0.2f, 0.3f) with { A = _alpha };
        var sphere = Render.CreateMesh(MeshShape.Sphere, 0.9f);

        var spheres = new List<Entity>();
        var modes = new[] { AlphaMode.Opaque, AlphaMode.Blend, AlphaMode.Premultiplied, AlphaMode.Add, AlphaMode.Multiply };
        for (var i = 0; i < modes.Length; i++)
        {
            var settings = new MaterialSettings { BaseColor = baseColor, AlphaMode = modes[i] };
            var material = Render.CreateMaterial(settings);
            Materials.Add(new Controlled(material, settings, Unlit: true));
            spheres.Add(ecs.Mesh(sphere, material, Transform.At(-4f + 2f * i, 0f, 0f)));
        }

        // A checkered floor, which the keys recolor too but never light differently.
        var black = new MaterialSettings { BaseColor = (0f, 0f, 0f, _alpha) };
        var white = new MaterialSettings { BaseColor = (1f, 1f, 1f, _alpha) };
        var blackMaterial = Render.CreateMaterial(black);
        var whiteMaterial = Render.CreateMaterial(white);
        Materials.Add(new Controlled(blackMaterial, black, Unlit: false));
        Materials.Add(new Controlled(whiteMaterial, white, Unlit: false));

        var plane = Render.CreateMesh(MeshShape.Plane, 2f, 2f);
        for (var x = -3; x < 4; x++)
            for (var z = -3; z < 4; z++)
                ecs.Mesh(plane, (x + z) % 2 == 0 ? blackMaterial : whiteMaterial, Transform.At(x * 2f, -1f, z * 2f));

        ecs.PointLight(new Vec3(4f, 8f, 4f));

        _camera = ecs.Camera(Transform.LookingAt(new Vec3(0f, 2.5f, 10f), Vec3.Zero, Vec3.UnitY));
        Render.SetPostProcessing(_camera, new PostSettings { Hdr = _hdr });

        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraMono-Medium.ttf");
        var style = new UiTextSettings { Font = font };
        Ui.SpawnText(
            "Up / Down — Increase / Decrease Alpha\nLeft / Right — Rotate Camera\nH - Toggle HDR\nSpacebar — Toggle Unlit\nC — Randomize Colors",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) }, style);
        _display = Ui.SpawnText(Describe(), new UiSettings { Absolute = true, Top = Length.Px(12f), Right = Length.Px(12f) }, style);

        // Each label's lines run up from a point over its sphere, which follows the sphere on screen.
        var orange = Scene.Srgb(1f, 165f / 255f, 0f);
        var labelStyle = new UiTextSettings { Font = font, Wrap = TextWrap.NoWrap };
        string[] names = ["┌─ Opaque\n│\n│\n│\n│", "┌─ Blend\n│\n│\n│", "┌─ Premultiplied\n│\n│", "┌─ Add\n│", "┌─ Multiply"];
        for (var i = 0; i < names.Length; i++)
        {
            var label = Ui.SpawnNode(new UiSettings { Absolute = true });
            var text = Ui.SpawnText(names[i], new UiSettings { Absolute = true, Bottom = Length.Zero }, labelStyle);
            ecs.Wrap<TextColorRef>(text).Value = new Color(orange.R, orange.G, orange.B, orange.A);
            ecs.SetParent(text, label);
            Labels.Add((label, spheres[i]));
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

        var randomize = input.KeyPressed(Key.C);
        if (changed || randomize)
        {
            foreach (var (material, settings, unlit) in Materials)
            {
                var (r, g, b, _) = settings.BaseColor;
                if (randomize) (r, g, b) = (Linear(_random.NextSingle()), Linear(_random.NextSingle()), Linear(_random.NextSingle()));
                settings.BaseColor = (r, g, b, _alpha);
                if (unlit) settings.Unlit = _unlit;
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

        foreach (var (label, sphere) in Labels)
        {
            var at = ecs.GetOrDefault<GlobalTransform>(sphere).Translation + Vec3.UnitY;
            if (!Render.TryProject(_camera, at, out var x, out var y)) continue;
            var node = ecs.Wrap<NodeRef>(label);
            (node.Top, node.Left) = (new Val.Px(y), new Val.Px(x));
        }

        Ui.SetText(_display, Describe());
    }

    // Bevy's random sRGB channel, as linear.
    private static float Linear(float srgb) => Color.FromSrgb(srgb, 0f, 0f).R;


    private static string Describe() => string.Create(CultureInfo.InvariantCulture, $"  HDR: {(_hdr ? "ON " : "OFF")}\nAlpha: {_alpha:0.00}");
}
