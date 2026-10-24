using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

// The field about an edge two blocks meet along, 3DEngine's test of its edge leak's mend.
public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// 3DEngine's voxel game's room of stone whose wall steps a block east halfway, its two halves
    /// meeting along one edge alone, with a glowstone west of the edge and the eye east of it, so no
    /// ray from the glowstone reaches what the eye sees but through the edge itself. The field's cells
    /// about the edge lay inside the blocks on one diagonal and outside them on the other, the
    /// distance between them never below zero along the edge, and in 3DEngine the march from the floor
    /// to the glowstone saw half of it through the edge, the floor about the edge's foot 10 sRGB levels
    /// at 640 by 360 where 7 with the edge closed by a block and none in a path-traced reference. With
    /// the cells about such an edge held inside, the floor reads as with the edge closed.
    /// </summary>
    [SkippableFact]
    public void TwoBlocksMeetingAlongAnEdgeCloseItToAGlowLightsLight()
    {
        Needs.Shaders();
        var (open, shut) = (BrightestAboutTheEdge(closed: false), BrightestAboutTheEdge(closed: true));
        Assert.True(open <= shut + 2, $"no light passes the edge the wall's halves meet along, the brightest about its foot {open} levels with it open and {shut} with it closed");
    }

    // The brightest luminance about the foot of the edge in the room, the wall's halves meeting along
    // it or a block closing it, at 640 by 360 at High with the screen's probes left out, as 3DEngine
    // reads it, where the light through the edge fanned over the floor.
    private static int BrightestAboutTheEdge(bool closed)
    {
        const uint Width = 640, Height = 360;
        const float Fov = 70f;
        var glowstone = (X: 15, Y: 5, Z: -14);
        // The game's eye 1.62 over the block it stands on, facing 225 degrees and 32 down.
        var eye = new Vector3(21.2f, 6.62f, -20.2f);
        float heading = 225 * MathF.PI / 180, pitch = -32 * MathF.PI / 180;
        var forward = new Vector3(MathF.Sin(heading) * MathF.Cos(pitch), MathF.Sin(pitch), -MathF.Cos(heading) * MathF.Cos(pitch));

        // A shell a block thick from 10 4 -22 to 22 11 -10, and the wall's halves.
        var stone = new HashSet<(int X, int Y, int Z)>();
        for (var x = 10; x <= 22; x++)
        for (var y = 4; y <= 11; y++)
        for (var z = -22; z <= -10; z++)
            if (x is 10 or 22 || y is 4 or 11 || z is -22 or -10)
                stone.Add((x, y, z));
        for (var y = 5; y <= 10; y++)
        {
            for (var z = -21; z <= -16; z++) stone.Add((16, y, z));
            for (var z = -15; z <= -11; z++) stone.Add((17, y, z));
            if (closed) stone.Add((16, y, -15));
        }

        var run = new PictureRun
        {
            Width = Width,
            Height = Height,
            Frames = 2400,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f), FieldOfView = Fov });
                ecs.Add(camera, Transform.LookingAt(new Vec3(eye.X, eye.Y, eye.Z), new Vec3(eye.X + forward.X, eye.Y + forward.Y, eye.Z + forward.Z), Vec3.UnitY));
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                var section = ecs.Spawn();
                Render.SetMesh(ecs, section, Render.CreateMesh(Section(stone, [glowstone])));
                Render.SetMaterial(ecs, section, Render.CreateMaterial(0.22f, 0.22f, 0.22f, roughness: 1f));
                var glowing = ecs.Spawn();
                Render.SetMesh(ecs, glowing, Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f));
                Render.SetMaterial(ecs, glowing, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0.47f, 0.32f, 0.1f, 1f),
                    Roughness = 1f,
                    Emissive = (4f, 2.31f, 0.75f, 0f),
                }));
                ecs.Add(glowing, Transform.At(glowstone.X + 0.5f, glowstone.Y + 0.5f, glowstone.Z + 0.5f));
                Render.SetSceneField(4, 0.25f, 2);
                Render.SetGlobalIllumination(GlobalIllumination.High);
                Render.SetGlobalIlluminationScreenProbes(false);
            },
        };
        run.Until("settled", _ => Settled(2))
            .Until("traced", _ => WorldProbes.TracedFrames >= 90)
            .Until("drawn", _ => EngineDrawsOn(true))
            .Capture("room")
            .Go();

        var picture = run.Picture("room");
        var foot = Projected(eye, forward, Fov, Width, Height, new Vector3(17, 5, -15));
        var most = 0;
        for (var y = Math.Max(0, (int)foot.Y - 50); y < Math.Min((int)Height, (int)foot.Y + 150); y++)
        for (var x = Math.Max(0, (int)foot.X - 125); x < Math.Min((int)Width, (int)foot.X + 125); x++)
        {
            var at = (y * (int)Width + x) * 4;
            most = Math.Max(most, (picture.Pixels[at] * 299 + picture.Pixels[at + 1] * 587 + picture.Pixels[at + 2] * 114) / 1000);
        }
        return most;
    }

    // Where a point falls on a picture of a camera at `eye` looking along `forward` with a vertical
    // field of view of `fov` degrees, in pixels from the top left.
    private static Vector2 Projected(Vector3 eye, Vector3 forward, float fov, uint width, uint height, Vector3 point)
    {
        var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        var up = Vector3.Cross(right, forward);
        var toward = point - eye;
        var depth = Vector3.Dot(toward, forward);
        var scale = 1f / MathF.Tan(fov * MathF.PI / 360f);
        var (x, y) = (Vector3.Dot(toward, right) / depth * scale * height / width, Vector3.Dot(toward, up) / depth * scale);
        return new Vector2((x * 0.5f + 0.5f) * width, (0.5f - y * 0.5f) * height);
    }

    // The faces of the blocks given, a unit cube each at its corner, where no block of their own or of
    // those drawn apart lies against them, each face's corners turning about its outward way.
    private static MeshData Section(HashSet<(int X, int Y, int Z)> blocks, (int X, int Y, int Z)[] apart)
    {
        var positions = new List<Vec3>();
        var indices = new List<uint>();
        (int X, int Y, int Z)[] ways = [(1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)];
        foreach (var block in blocks)
            foreach (var way in ways)
            {
                var beside = (block.X + way.X, block.Y + way.Y, block.Z + way.Z);
                if (blocks.Contains(beside) || apart.Contains(beside)) continue;
                var n = new Vector3(way.X, way.Y, way.Z);
                // Two ways along the face whose cross is the outward way, from the face's corner.
                var u = MathF.Abs(n.X) > 0 ? new Vector3(0, 1, 0) : new Vector3(1, 0, 0);
                var v = Vector3.Cross(n, u);
                if (Vector3.Dot(Vector3.Cross(u, v), n) < 0) (u, v) = (v, u);
                var middle = new Vector3(block.X + 0.5f, block.Y + 0.5f, block.Z + 0.5f) + n * 0.5f;
                var corner = middle - (u + v) * 0.5f;
                var first = (uint)positions.Count;
                foreach (var c in (ReadOnlySpan<Vector3>)[corner, corner + u, corner + u + v, corner + v])
                    positions.Add(new Vec3(c.X, c.Y, c.Z));
                indices.AddRange([first, first + 1, first + 2, first, first + 2, first + 3]);
            }
        return new MeshData { Positions = [.. positions], Indices = [.. indices] };
    }
}
