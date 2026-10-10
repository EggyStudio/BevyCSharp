// Bevy's extended_material_bindless example, examples/shader/extended_material_bindless.rs at
// v0.20.0, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;

namespace BevyCSharp.Examples.Shading;

// Demonstrates a standard material extended by a shader of its own that reads an image and a
// color, a gray sphere tinted by a red checkerboard, lit by Bevy and turning.
//
// Bevy's draws the extension bindless, its values and image in arrays shared by every material of
// its kind, which is how it batches them. The bridge's materials each have their bindings, which
// draws the same picture.
internal static class ExtendedMaterialBindless
{
    private static Entity _sphere;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;

            // Tailwind's gray 600, multiplied by CSS red and the checkerboard.
            var gray = Color.FromHex("#4b5563");
            var material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/extended_material_bindless.slang"))
                .Set("base_color", new Vector4(gray.R, gray.G, gray.B, 1f))
                .Set("modulate_color", new Vector4(1f, 0f, 0f, 1f))
                .SetTexture("modulate_texture", AssetServer.Load(AssetKind.Image, "textures/uv_checker_bw.png"));
            _sphere = ecs.SpawnMesh(Render.CreateMesh(MeshShape.UvSphere, 1f, 20f, 20f), material, Transform.At(0f, 0.5f, 0f));

            var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
            ecs.Add(light, Transform.LookingAt(new Vec3(1f, 1f, 1f), Vec3.Zero, Vec3.UnitY));

            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));
        }, "extended_material_bindless.Setup");

        // Bevy's EulerRot::YXZ of minus the time, three quarters of a turn and nothing.
        app.Update(ctx =>
        {
            var at = ctx.Ecs.GetOrDefault<Transform>(_sphere);
            ctx.Ecs.Set(_sphere, at with { Rotation = Quat.FromRotationY(-ctx.Time.Elapsed) * Quat.FromRotationX(MathF.PI / 2f * 3f) });
        }, "extended_material_bindless.RotateSphere");
    }
}
