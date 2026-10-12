using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the shadows a light casts beyond a plain shadow map: contact shadows traced through the
/// depth buffer, and soft shadows whose penumbra widens with distance.
/// </summary>
[Collection("engine")]
public sealed class ShadowTests
{
    /// <summary>
    /// A cube on a floor under a low light, with no shadow map, darkens the floor beside it with
    /// contact shadows on and leaves every pixel no darker without.
    /// </summary>
    /// <remarks>
    /// The light casts no shadow map, so every pixel darker with contact shadows on than off can
    /// only have been darkened by one.
    /// </remarks>
    [SkippableFact]
    public void AContactShadowDarkensTheFloorBesideACube()
    {
        Needs.Renderer();

        var with = Picture(contact: true);
        var without = Picture(contact: false);

        var darker = 0;
        var lighter = 0;

        for (var i = 0; i < with.Pixels.Length; i += 4)
        {
            var difference = with.Pixels[i] - without.Pixels[i];

            if (difference < -30) darker++;
            if (difference > 30) lighter++;
        }

        Assert.True(darker > 20, $"only {darker} pixels came out darker with contact shadows");
        Assert.Equal(0, lighter);
    }

    private static CapturedImage Picture(bool contact)
    {
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 3f, 6f));
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Render.SetAmbientLight((1f, 1f, 1f), 0f);

                if (contact)
                {
                    Render.SetContactShadows(camera, new ContactShadowSettings(Steps: 32, Thickness: 0.2f, Length: 1.5f));
                }

                // Low, from the right, so the floor to the cube's left is where its shadow falls.
                var sun = Render.SpawnLight(new LightSettings
                {
                    Kind = LightKind.Directional,
                    Intensity = 8_000f,
                    Shadows = false,
                    ContactShadows = true,
                });
                ecs.Add(sun, Transform.LookingAt(new Vec3(4f, 1f, 0f), Vec3.Zero, Vec3.UnitY));

                var white = Render.CreateMaterial(1f, 1f, 1f);

                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 8f, 0.1f, 8f));
                Render.SetMaterial(ecs, floor, white);
                ecs.Add(floor, Transform.At(0f, -0.05f, 0f));

                var cube = ecs.Spawn();
                Render.SetMesh(ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f));
                Render.SetMaterial(ecs, cube, white);
                ecs.Add(cube, Transform.At(0f, 0.5f, 0f));
            },
        };

        run.Wait(30).Capture("picture").Go();
        return run.Picture("picture");
    }

    /// <summary>
    /// A slab hanging over a floor casts a shadow with a hard edge from a spot light with no size,
    /// and one that fades through many grays from a spot light given one.
    /// </summary>
    /// <remarks>
    /// A spot light, since the size of one is its radius in world units, while a directional
    /// light's penumbra is worked out in its shadow map's depth and showed none in a scene this
    /// small at any size tried.
    /// </remarks>
    [SkippableFact]
    public void ASoftShadowFadesWhereAHardOneCuts()
    {
        Needs.Renderer();

        var hard = Grays(PictureOfAHangingSlab(softness: 0f));
        var soft = Grays(PictureOfAHangingSlab(softness: 5f));

        Assert.True(soft > hard + 30, $"the soft shadow had {soft} pixels between light and dark, the hard one {hard}");
    }

    /// <summary>How many pixels are neither the shadow's dark nor the lit floor's light.</summary>
    private static int Grays(CapturedImage picture) =>
        PictureRun.Count(picture, (r, g, b) => r > 40 && r < 170 && Math.Abs(r - g) < 20 && Math.Abs(r - b) < 20);

    private static CapturedImage PictureOfAHangingSlab(float softness)
    {
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 6f, 3f));
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Render.SetAmbientLight((1f, 1f, 1f), 0f);

                var lamp = Render.SpawnLight(new LightSettings
                {
                    Kind = LightKind.Spot,
                    Intensity = 2_000_000f,
                    Range = 20f,
                    InnerAngle = 0.6f,
                    OuterAngle = 0.8f,
                });
                ecs.Add(lamp, Transform.LookingAt(new Vec3(0.3f, 5f, 0.2f), Vec3.Zero, Vec3.UnitZ));
                Render.SetSoftShadows(lamp, softness);

                var white = Render.CreateMaterial(1f, 1f, 1f);

                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 8f, 0.1f, 8f));
                Render.SetMaterial(ecs, floor, white);
                ecs.Add(floor, Transform.At(0f, -0.05f, 0f));

                // A flat slab held high, so the shadow falls far from what casts it.
                var slab = ecs.Spawn();
                Render.SetMesh(ecs, slab, Render.CreateMesh(MeshShape.Cuboid, 1.2f, 0.1f, 1.2f));
                Render.SetMaterial(ecs, slab, white);
                ecs.Add(slab, Transform.At(0f, 2.5f, 0f));
            },
        };

        run.Wait(30).Capture("picture").Go();
        return run.Picture("picture");
    }
}
