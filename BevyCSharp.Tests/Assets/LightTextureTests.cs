using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a picture a light shines through, which Bevy draws only where its light textures were
/// built in.
/// </summary>
/// <remarks>
/// A light texture that is not built in is accepted and drawn as no texture at all, so the light
/// looks as it would without one and nothing reports why. Only a picture of what the light did
/// tells the two apart.
/// </remarks>
[Collection("engine")]
public sealed class LightTextureTests
{
    /// <summary>
    /// A spot light's cookie, black on one side and white on the other, leaves the floor dark on
    /// the one side and lit on the other.
    /// </summary>
    [SkippableFact]
    public void ASpotLightsCookieShadesTheFloorAsItsPictureDoes()
    {
        Needs.Renderer();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f) });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 6f, 0.01f), Vec3.Zero, Vec3.UnitY));
                Render.SetAmbientLight((1f, 1f, 1f), 0f);

                var floor = ecs.Spawn();
                ecs.Add(floor, Transform.Identity);
                Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Plane, 10f, 10f));
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Roughness = 1f }));

                // Left half black and right half white, a black border around both so nothing
                // leaks past the edge.
                const int Size = 64;
                var pixels = new byte[Size * Size * 4];
                for (var y = 0; y < Size; y++)
                {
                    for (var x = 0; x < Size; x++)
                    {
                        var lit = x >= Size / 2 && x < Size - 4 && y >= 4 && y < Size - 4;
                        var at = (y * Size + x) * 4;
                        pixels[at] = pixels[at + 1] = pixels[at + 2] = lit ? (byte)255 : (byte)0;
                        pixels[at + 3] = 255;
                    }
                }

                var spot = Render.SpawnLight(new LightSettings { Kind = LightKind.Spot, Intensity = 2_000_000f, Range = 20f, OuterAngle = 0.6f, InnerAngle = 0.5f, Shadows = false });
                ecs.Add(spot, Transform.LookingAt(new Vec3(0f, 4f, 0f), Vec3.Zero, Vec3.UnitZ));
                Render.SetLightCookie(spot, Render.CreateImage(pixels, Size, Size, srgb: false));
            },
        };

        run.Wait(ShaderMaterialTests.Settled).Capture("lit").Go();

        var picture = run.Picture("lit");
        int Brightness(uint from, uint to)
        {
            var sum = 0;
            for (var y = picture.Height / 4; y < picture.Height * 3 / 4; y++)
                for (var x = from; x < to; x++)
                    sum += picture.At(x, y).R;
            return sum;
        }

        var left = Brightness(picture.Width / 8, picture.Width / 2);
        var right = Brightness(picture.Width / 2, picture.Width * 7 / 8);
        Assert.True(Math.Max(left, right) > 4 * Math.Max(1, Math.Min(left, right)),
            $"the floor is as bright under the cookie's black half as under its white half ({left} against {right})");
    }
}
