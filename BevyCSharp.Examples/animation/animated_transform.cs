// Bevy's animated_transform example, examples/animation/animated_transform.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Animations;

// Creates and plays an animation defined by code that alters the transform of entities. A planet
// goes round a square, carrying a controller that turns it about its own axis, which carries a
// satellite that grows and shrinks and turns as well, each moved by a curve of one clip aimed at
// its name.
internal static class AnimatedTransform
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render.SetAmbientLight((1f, 1f, 1f), 150f);

        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 500_000f });
        ecs.Add(light, Transform.At(0f, 2.5f, 0f));

        // The targets, by the names on the path down to each.
        var planet = AnimationTarget.FromNames("planet");
        var orbitController = AnimationTarget.FromNames("planet", "orbit_controller");
        var satellite = AnimationTarget.FromNames("planet", "orbit_controller", "satellite");

        float[] quarters = [0f, 1f, 2f, 3f, 4f];
        Quat[] turns = [Quat.Identity, Quat.FromAxisAngle(Vec3.UnitY, MathF.PI / 2f), Quat.FromAxisAngle(Vec3.UnitY, MathF.PI), Quat.FromAxisAngle(Vec3.UnitY, MathF.PI * 1.5f), Quat.Identity];

        var clip = Animation.CreateClip();

        // The planet round a square.
        Animation.AddCurve(clip, planet, AnimationCurve.Translation(quarters,
            [new Vec3(1f, 0f, 1f), new Vec3(-1f, 0f, 1f), new Vec3(-1f, 0f, -1f), new Vec3(1f, 0f, -1f), new Vec3(1f, 0f, 1f)]));

        // The orbit controller turning, which carries the satellite round.
        Animation.AddCurve(clip, orbitController, AnimationCurve.Rotation(quarters, turns));

        // The satellite growing and shrinking twice a second, and turning on its own axis.
        float[] halves = [0f, 0.5f, 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f];
        Vec3[] sizes = [.. halves.Select((_, i) => new Vec3(i % 2 == 0 ? 0.8f : 1.2f))];
        Animation.AddCurve(clip, satellite, AnimationCurve.Scale(halves, sizes));
        Animation.AddCurve(clip, satellite, AnimationCurve.Rotation(quarters, turns));

        var (graph, node) = Animation.GraphFromClip(clip);

        // The planet plays it, over and over, for itself and the entities below it.
        var planetEntity = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Sphere, 0.5f), Render.CreateMaterial(Color.FromSrgb(0.8f, 0.7f, 0.6f)), Transform.Identity);
        ecs.SetName(planetEntity, "planet");
        Animation.PlayGraph(planetEntity, graph, node, repeat: true);
        Animation.Animate(planetEntity, planet, planetEntity);

        var controller = ecs.Spawn();
        ecs.Add(controller, Transform.Identity);
        ecs.Insert<Bevy.Reflected.VisibilityRef>(controller);
        ecs.SetName(controller, "orbit_controller");
        ecs.SetParent(controller, planetEntity);
        Animation.Animate(controller, orbitController, planetEntity);

        var satelliteEntity = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 0.5f, 0.5f, 0.5f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.9f, 0.3f)), Transform.At(1.5f, 0f, 0f));
        ecs.SetName(satelliteEntity, "satellite");
        ecs.SetParent(satelliteEntity, controller);
        Animation.Animate(satelliteEntity, satellite, planetEntity);
    }, "animated_transform.Setup");
}
