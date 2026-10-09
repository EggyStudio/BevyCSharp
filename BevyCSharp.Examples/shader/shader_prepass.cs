// Bevy's shader_prepass example, examples/shader/shader_prepass.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Shading;

// Shows what the camera's prepass drew, its depth, its normals and its motion vectors, over a
// scene of a plane and three cubes, one turning, cycled with Space.
//
// Bevy reads the prepass in a material on a quad filling the view, made see-through to show the
// scene. A material here does not read the prepass, so a pass over the whole picture reads it,
// showing the picture as it is in the see-through setting, and before tonemapping, which Bevy's
// quad goes through as well.
internal static class ShaderPrepass
{
    private static readonly string[] Outputs = ["transparent", "depth", "normals", "motion vectors"];

    private static Entity _label;
    private static ShaderInstance _show;
    private static uint _view;

    public static void Build(App app)
    {
        app.Startup(Setup, "shader_prepass.Setup");

        app.Update(ctx =>
        {
            if (!ctx.Input.KeyPressed(Key.Space)) return;
            _view = (_view + 1) % 4;
            _show.Set("settings.show_depth", _view == 1 ? 1u : 0u)
                .Set("settings.show_normals", _view == 2 ? 1u : 0u)
                .Set("settings.show_motion_vectors", _view == 3 ? 1u : 0u);
            ctx.Ecs.Wrap<TextSpanRef>(_label).Value = $"Prepass Output: {Outputs[_view]}\n";
        }, "shader_prepass.TogglePrepassView");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _view = 0;

        // Without multisampling, as Bevy's camera, so the pass reads the prepass as one sample a
        // pixel.
        var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 3f, 5f), Vec3.Zero, Vec3.UnitY));
        Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
        Shaders.SetPrepass(camera, depth: true, normals: true, motion: true);
        _show = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Pass = "shaders/show_prepass.slang" }));
        Shaders.SetPasses(camera, new ShaderPass(_show));

        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);

        var icon = AssetServer.Load(AssetKind.Image, "branding/icon.png");
        var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
        var program = Shaders.CreateProgram("shaders/custom_material.slang");
        ShaderMaterial Custom(AlphaMode alpha) => Shaders.CreateMaterial(program, alpha)
            .Set("material_color", new Vector4(1f, 1f, 1f, 1f))
            .SetTexture("material_color_texture", icon);

        ecs.Add(ecs.SpawnMesh(cube, Custom(AlphaMode.Opaque), Transform.At(-1f, 0.5f, 0f)), new Rotates());
        ecs.SpawnMesh(cube, Render.CreateMaterial(new MaterialSettings { AlphaMode = AlphaMode.Mask, AlphaCutoff = 1f, BaseColorTexture = icon }), Transform.At(0f, 0.5f, 0f));
        ecs.SpawnMesh(cube, Custom(AlphaMode.Blend), Transform.At(1f, 0.5f, 0f));

        ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);

        var text = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        var style = new UiTextSettings();
        _label = Ui.SpawnTextSpan(text, "Prepass Output: transparent\n", style, (1f, 1f, 1f, 1f));
        foreach (var line in new[] { "\n\n", "Controls\n", "---------------\n", "Space - Change output\n" })
            Ui.SpawnTextSpan(text, line, style, (1f, 1f, 1f, 1f));
    }
}

/// <summary>The opaque cube, which swings about Z so the motion vectors have something to show.</summary>
[Behavior]
public partial struct Rotates
{
    /// <summary>Turned to a full turn and back as the sine of the time goes, as Bevy's <c>rotate</c> turns it.</summary>
    [OnUpdate]
    public void Rotate(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = Quat.FromRotationZ((MathF.Sin(ctx.Time.Elapsed) * 0.5f + 0.5f) * MathF.PI * 2f);
}
