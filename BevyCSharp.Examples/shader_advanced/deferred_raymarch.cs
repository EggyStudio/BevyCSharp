// Bevy's deferred_raymarch example, examples/shader_advanced/deferred_raymarch.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Shading;

// Writes a raymarched signed distance field directly into the deferred G-buffer, so Bevy's
// standard deferred lighting shades it as if it were a mesh, beside a mesh cube on a ground plane
// that catches the field's shadow.
//
// Bevy adds pipelines and render systems of its own for the full-screen pass into the G-buffer and
// the one into each shadow view. Here the camera draws a program of one triangle over the whole
// view inside its prepass, into the G-buffer and the lighting pass ids, and the program's shadow
// stage draws the same triangle into the shadow maps.
internal static class DeferredRaymarch
{
    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;

            // Everything through the deferred pipeline.
            Render.SetDeferredRendering(true);

            var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(4f, 3f, 6f), new Vec3(0f, 0.2f, 0f), Vec3.UnitY));
            // Deferred rendering needs MSAA off.
            Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
            Shaders.SetPrepass(camera, depth: true, deferred: true);
            Render.SetAmbientLight(camera, (1f, 1f, 1f), 200f);
            ecs.Add(camera, new FreeCamera());

            // A ground plane that catches the field's shadow.
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 20f, 20f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.At(0f, -1.5f, 0f));

            // A mesh cube.
            ecs.SpawnMesh(
                Render.CreateMesh(MeshShape.Cuboid, 1.2f, 1.2f, 1.2f),
                Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb(0.2f, 0.4f, 0.9f), Roughness = 0.4f }),
                Transform.At(2.2f, -0.9f, 0.5f));

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 8_000f, Shadows = true });
            ecs.Add(sun, Transform.LookingAt(new Vec3(4f, 8f, 4f), Vec3.Zero, Vec3.UnitY));

            var field = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
            {
                DrawVertex = "shaders/deferred_raymarch.slang",
                DrawFragment = "shaders/deferred_raymarch.slang",
                DrawShadow = "shaders/deferred_raymarch.slang",
            }));

            // Writing depth lets the field sort against the meshes, and the lighting reads which
            // pass lights each pixel beside the G-buffer.
            Shaders.SetViewDraws(camera, ViewDraw.Fixed(field, FramePoint.InPrepass, vertices: 3) with
            {
                Targets = ["gbuffer", "lighting_pass"],
                CastsShadows = true,
            });
        }, "deferred_raymarch.Setup");
    }
}
