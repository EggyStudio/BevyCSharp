// Bevy's 3d_text_gizmos example, examples/gizmos/3d_text_gizmos.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Gizmo;

// Shows text drawn as gizmos in 3D, three words turning about the vertical axis at different
// speeds.
internal static class TextGizmos3d
{
    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            ctx.Ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 10f), Vec3.Zero, Vec3.UnitY));
            Gizmos.Configure(width: 4f);
        }, "3d_text_gizmos.SetupCamera");

        app.Update(ctx =>
        {
            var t = 0.2f * ctx.Time.Elapsed;
            Gizmos.Text("Hello", new Vec3(0f, 1.5f, 0f), Quat.FromRotationY(-t), 1f, (0f, 0f), (1f, 0f, 0f, 1f));
            Gizmos.Text("Text", Vec3.Zero, Quat.FromRotationY(t + 0.25f), 1f, (0f, 0f), Color.FromSrgb8(255, 165, 0));
            Gizmos.Text("Gizmos", new Vec3(0f, -1.5f, 0f), Quat.FromRotationY(-t - 0.5f), 1f, (0f, 0f), (1f, 1f, 0f, 1f));
        }, "3d_text_gizmos.HelloWorld");
    }
}
