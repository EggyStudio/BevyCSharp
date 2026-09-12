using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Which way the world is facing, in the corner of the scene.
/// </summary>
/// <remarks>
/// <para>
/// Drawn flat, in the interface, rather than as lines in the world. A cross put into the scene a
/// few centimetres in front of the camera and off to one side is seen through the same perspective
/// as everything else, and perspective at the edge of a wide view shears it: the arms come out at
/// angles that say nothing about where the world is pointing. What a person reads this for is the
/// direction of three axes, so it is drawn the way a direction is drawn, with no depth at all.
/// </para>
/// <para>
/// The camera's own basis does the work: a world axis seen through the camera is that axis measured
/// against the camera's right, up and forward, and the first two of those are the position on
/// screen. The third only says which arms are in front.
/// </para>
/// </remarks>
public static class OrientationGizmo
{
    /// <summary>How wide the whole thing is, in logical pixels.</summary>
    public const float Size = 84f;

    private static readonly (string Name, Vec3 Axis, Vector4 Color)[] Axes =
    [
        ("X", new Vec3(1f, 0f, 0f), new Vector4(0.91f, 0.30f, 0.36f, 1f)),
        ("Y", new Vec3(0f, 1f, 0f), new Vector4(0.49f, 0.78f, 0.30f, 1f)),
        ("Z", new Vec3(0f, 0f, 1f), new Vector4(0.28f, 0.56f, 0.93f, 1f)),
    ];

    /// <summary>Draws it at a point, which is the middle of the cross.</summary>
    public static void Draw(BehaviorContext ctx, Vector2 middle)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var camera = EditorSelection.Camera;
        if (camera.IsNone) return;

        var view = ctx.Ecs.GetOrDefault<GlobalTransform>(camera);
        // Under the panels rather than over them: it belongs to the scene, and a panel dragged
        // across it should cover it like anything else the scene is showing.
        var draw = ImGui.GetBackgroundDrawList();

        var reach = (Size * 0.5f) - 10f;
        var knob = 7f;

        // Every arm, both halves, ordered back to front so the near ones are drawn over the far.
        var arms = new List<(Vector2 At, float Depth, Vector4 Color, string Name, bool Positive)>();

        foreach (var (name, axis, color) in Axes)
        {
            foreach (var way in new[] { 1f, -1f })
            {
                var world = axis * way;

                // Against the camera's own basis: right and up place it, forward says how far in
                // front it is. Bevy's camera looks down its negative Z, so a smaller Z is nearer.
                var right = Vec3.Dot(world, view.XAxis);
                var up = Vec3.Dot(world, view.YAxis);
                var ahead = -Vec3.Dot(world, view.ZAxis);

                arms.Add((
                    middle + new Vector2(right * reach, -up * reach),
                    ahead,
                    color,
                    way > 0f ? name : string.Empty,
                    way > 0f));
            }
        }

        arms.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        foreach (var arm in arms)
        {
            // Which half is solid says which way is positive, not which way is nearer: an axis
            // pointing a little away from the camera still points the way it points, and dimming
            // it because of that is the gizmo saying the world turned over.
            //
            // Depth decides only what is drawn over what, which is what the sort above is for.
            var packed = ImGui.GetColorU32(
                arm.Positive ? arm.Color : EditorTheme.Alpha(arm.Color, 0.4f));

            if (arm.Positive)
            {
                draw.AddLine(middle, arm.At, packed, 2f);
                draw.AddCircleFilled(arm.At, knob, packed);

                var size = ImGui.CalcTextSize(arm.Name);

                draw.AddText(
                    arm.At - (size * 0.5f),
                    ImGui.GetColorU32(new Vector4(0.04f, 0.04f, 0.04f, 1f)),
                    arm.Name);

                continue;
            }

            // The other half is a ring: it says where the axis went without competing with the end
            // somebody is reading.
            draw.AddCircle(arm.At, knob * 0.8f, packed, 0, 1.6f);
        }
    }
}
