// Bevy's atmospheric_fog example, examples/3d/atmospheric_fog.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// This example showcases atmospheric fog.
//
// The fog is Bevy's DistanceFog, put on the camera through reflection, with its atmospheric falloff
// worked out here as Bevy's from_visibility_colors works it out.
internal static class AtmosphericFog
{
    private static Entity _camera;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;

            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-1f, 0.1f, 1f), Vec3.Zero, Vec3.UnitY));
            var fog = ecs.Insert<DistanceFogRef>(_camera);
            fog.Color = Color.FromSrgb(0.35f, 0.48f, 0.66f);
            fog.DirectionalLightColor = Color.FromSrgb(1f, 0.95f, 0.85f, 0.5f);
            fog.DirectionalLightExponent = 30f;

            // Up to fifteen units seen through it, an extinction color and an inscattering one.
            var (extinction, inscattering) = FromVisibilityColors(15f, Color.FromSrgb(0.35f, 0.5f, 0.66f), Color.FromSrgb(0.8f, 0.844f, 1f));
            fog.Falloff = new FogFalloff.Atmospheric(extinction, inscattering);

            var sunColor = Color.FromSrgb(0.98f, 0.95f, 0.82f);
            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Color = (sunColor.R, sunColor.G, sunColor.B), Shadows = true });
            ecs.Add(sun, Transform.LookingAt(Vec3.Zero, new Vec3(-0.15f, -0.05f, 0.25f), Vec3.UnitY));
            Render.SetShadowCascades(sun, maximum: 3f, firstBound: 0.3f);

            // A box around everything, which the fog colors as the sky.
            var sky = Color.FromHex("888888");
            var box = ecs.SpawnMesh(
                Render.CreateMesh(MeshShape.Cuboid, 2f, 1f, 1f),
                Render.CreateMaterial(new MaterialSettings { BaseColor = (sky.R, sky.G, sky.B, 1f), Unlit = true, DoubleSided = true }),
                new Transform(Vec3.Zero, Quat.Identity, new Vec3(20f)));
            Render.SetMeshFlags(ecs, box, MeshFlags.NoShadowCasting);

            Ui.SpawnText(
                "Press Spacebar to Toggle Atmospheric Fog.\nPress S to Toggle Directional Light Fog Influence.",
                new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
        });

        app.SpawnGltf("models/terrain/Mountains.gltf");

        app.Update(ctx =>
        {
            var fog = ctx.Ecs.Wrap<DistanceFogRef>(_camera);
            if (ctx.Input.KeyPressed(Key.Space)) fog.Color = fog.Color.WithAlpha(1f - fog.Color.A);
            if (ctx.Input.KeyPressed(Key.S)) fog.DirectionalLightColor = fog.DirectionalLightColor.WithAlpha(0.5f - fog.DirectionalLightColor.A);
        }, "atmospheric_fog.ToggleSystem");
    }

    // Bevy's FogFalloff::from_visibility_contrast_colors at its contrast threshold of a twentieth.
    private static (Vec3 Extinction, Vec3 Inscattering) FromVisibilityColors(float visibility, Color extinction, Color inscattering)
    {
        var koschmieder = -MathF.Log(0.05f) / visibility;
        var e = MathF.E;
        return (
            new Vec3(MathF.Pow(1f - extinction.R, e), MathF.Pow(1f - extinction.G, e), MathF.Pow(1f - extinction.B, e)) * koschmieder * MathF.Pow(extinction.A, e),
            new Vec3(MathF.Pow(inscattering.R, e), MathF.Pow(inscattering.G, e), MathF.Pow(inscattering.B, e)) * koschmieder * MathF.Pow(inscattering.A, e));
    }
}
