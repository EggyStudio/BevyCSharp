// Bevy's extended_material example, examples/shader/extended_material.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;

namespace BevyCSharp.Examples.Shading;

// Demonstrates Bevy's standard material extended by a shader of its own: a red sphere lit as the
// standard material lights it, with its color changed before the lighting and the lit color cut
// into steps after it, under a light that circles it.
//
// Bevy extends its StandardMaterial with a type of the game's own. Here the shader, in Slang,
// describes the surface the standard material would and hands it to Bevy's lighting through
// bcs::light and bcs::finish, so the steps between them are the shader's.
internal static class ExtendedMaterial
{
    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;

            var material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/extended_material.slang"))
                .Set("base_color", new Vector4(1f, 0f, 0f, 1f))
                .Set("quantize_steps", 1u);
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Sphere, 1f), material, Transform.At(0f, 0.5f, 0f));

            // Bevy's default directional light, of ten thousand lux.
            var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
            ecs.Add(light, Transform.LookingAt(new Vec3(1f, 1f, 1f), Vec3.Zero, Vec3.UnitY));
            ecs.Add(light, new Rotate());

            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));
        }, "extended_material.Setup");
    }
}

/// <summary>The light, turned about its own vertical axis, which for a directional light turns the direction it shines from.</summary>
[Behavior]
public partial struct Rotate
{
    /// <summary>Turned a radian a second, as Bevy's <c>rotate_things</c> turns it.</summary>
    [OnUpdate]
    public void RotateThings(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = Quat.FromRotationY(ctx.Time.Delta) * transform.Rotation;
}
