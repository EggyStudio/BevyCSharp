// Bevy's animation_masks example, examples/animation/animation_masks.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Animations;

// Demonstrates animation masks. A fox's head, legs and tail each play the run, the walk, the idle
// or nothing, chosen by a row of buttons for each part. Every clip plays at once, laid over each
// other by an additive blend, and each clip's node masks out the parts not playing it.
internal static class AnimationMasks
{
    private const string FoxPath = "models/animated/Fox.glb";
    private const float MaskGroupButtonWidth = 250f;

    // The bones of each part, the path down to where it starts and on down it.
    private static readonly (string Prefix, string Suffix)[] MaskGroupPaths =
    [
        ("root/_rootJoint/b_Root_00/b_Hip_01/b_Spine01_02/b_Spine02_03", "b_Neck_04/b_Head_05"),
        ("root/_rootJoint/b_Root_00/b_Hip_01/b_Spine01_02/b_Spine02_03/b_LeftUpperArm_09", "b_LeftForeArm_010/b_LeftHand_011"),
        ("root/_rootJoint/b_Root_00/b_Hip_01/b_Spine01_02/b_Spine02_03/b_RightUpperArm_06", "b_RightForeArm_07/b_RightHand_08"),
        ("root/_rootJoint/b_Root_00/b_Hip_01/b_LeftLeg01_015", "b_LeftLeg02_016/b_LeftFoot01_017/b_LeftFoot02_018"),
        ("root/_rootJoint/b_Root_00/b_Hip_01/b_RightLeg01_019", "b_RightLeg02_020/b_RightFoot01_021/b_RightFoot02_022"),
        ("root/_rootJoint/b_Root_00/b_Hip_01/b_Tail01_012", "b_Tail02_013/b_Tail03_014"),
    ];

    // Bevy's AppState, the clip each part plays.
    private static readonly AnimationLabel[] Chosen = new AnimationLabel[6];

    private static Entity _fox;
    private static bool _graphMade;
    private static AssetHandle _graph;
    private static readonly uint[] Nodes = new uint[3];

    public static void Build(App app)
    {
        app.Startup(SetupScene, "animation_masks.SetupScene");
        app.Startup(SetupUi, "animation_masks.SetupUi");
        app.Update(SetupAnimationGraphOnceLoaded, "animation_masks.SetupAnimationGraphOnceLoaded");
        app.Update(HandleButtonToggles, "animation_masks.HandleButtonToggles");
        app.Update(UpdateUi, "animation_masks.UpdateUi");
    }

    private static void SetupScene(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _graphMade = false;
        Array.Fill(Chosen, AnimationLabel.Idle);
        Render.SetAmbientLight((1f, 1f, 1f), 100f);

        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-15f, 10f, 20f), new Vec3(0f, 1f, 0f), Vec3.UnitY));
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 10_000_000f, Shadows = true });
        ecs.Add(light, Transform.At(-4f, 8f, 13f));

        _fox = ecs.SpawnScene(AssetServer.LoadGltfScene(FoxPath));
        ecs.Set(_fox, new Transform(Vec3.Zero, Quat.Identity, new Vec3(0.07f)));

        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Circle, 7f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)),
            new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));
    }

    private static void SetupUi(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Ui.SpawnText("Click on a button to toggle animations for its associated bones",
            new UiSettings { Absolute = true, Left = Length.Px(12f), Top = Length.Px(12f) });

        var panel = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Absolute = true, RowGap = Length.Px(6f), Left = Length.Px(12f), Bottom = Length.Px(12f) });
        ecs.SetParent(NewMaskGroupControl(ecs, "Head", Length.Auto, 0), panel);
        foreach (var (left, right, leftGroup, rightGroup) in new[] { ("Left Front Leg", "Right Front Leg", 1u, 2u), ("Left Hind Leg", "Right Hind Leg", 3u, 4u) })
        {
            var row = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, ColumnGap = Length.Px(6f) });
            ecs.SetParent(NewMaskGroupControl(ecs, left, Length.Px(MaskGroupButtonWidth), leftGroup), row);
            ecs.SetParent(NewMaskGroupControl(ecs, right, Length.Px(MaskGroupButtonWidth), rightGroup), row);
            ecs.SetParent(row, panel);
        }
        ecs.SetParent(NewMaskGroupControl(ecs, "Tail", Length.Auto, 5), panel);
    }

    // A part's control, its name over a row of buttons, one for each clip and one for none, the
    // first lit as the part starts on it.
    private static Entity NewMaskGroupControl(EcsWorld ecs, string label, Length width, uint group)
    {
        var white = Color.FromSrgb(1f, 1f, 1f);
        var black = Color.FromSrgb(0f, 0f, 0f);

        var control = Ui.SpawnNode(new UiSettings
        {
            Border = Sides.All(Length.Px(1f)),
            Width = width,
            Direction = UiDirection.Column,
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Corners = Corners.All(Length.Px(3f)),
            BorderColor = white,
            Color = black,
        });

        var title = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Justify = UiJustify.Center, Align = UiAlign.Center, Color = black });
        ecs.SetParent(Ui.SpawnText(label, new UiSettings { Margin = Sides.Vertical(Length.Px(3f)), Color = Color.FromSrgb8(211, 211, 211) }, 14f), title);
        ecs.SetParent(title, control);

        var buttons = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Direction = UiDirection.Row,
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Border = new Sides(Length.Zero, Length.Px(1f), Length.Zero, Length.Zero),
            BorderColor = white,
        });
        ecs.SetParent(buttons, control);

        var first = true;
        foreach (var clip in new[] { AnimationLabel.Run, AnimationLabel.Walk, AnimationLabel.Idle, AnimationLabel.Off })
        {
            var button = Ui.SpawnNode(new UiSettings
            {
                Interactive = true,
                Grow = 1f,
                Border = first ? Sides.None : new Sides(Length.Px(1f), Length.Zero, Length.Zero, Length.Zero),
                BorderColor = white,
                Color = first ? white : black,
            });
            ecs.Add(button, new AnimationControl { GroupId = group, Label = clip });
            ecs.SetParent(Ui.SpawnText(clip.ToString(), new UiSettings { Grow = 1f, Margin = Sides.Vertical(Length.Px(3f)), Color = first ? black : white },
                new UiTextSettings { FontSize = 14f, Justify = TextJustify.Center }), button);
            ecs.SetParent(button, buttons);
            first = false;
        }

        return control;
    }

    // Bevy's setup_animation_graph_once_loaded. The player the fox's scene brings is given a graph
    // laying the three clips over each other, the first on the whole body and the others masked
    // out of every part, and each part's bones are put in their group. Bones in no group are no
    // longer animated at all, and all three clips play.
    private static void SetupAnimationGraphOnceLoaded(BehaviorContext ctx)
    {
        if (_graphMade) return;

        var ecs = ctx.Ecs;
        Entity? player = null;
        foreach (var entity in ecs.Descendants(_fox))
        {
            if (ecs.Get<AnimationPlayerRef>(entity) is not null)
            {
                player = entity;
                break;
            }
        }
        if (player is not { } found) return;

        (_graph, var root) = Animation.CreateGraph();
        var blend = Animation.AddBlend(_graph, 1f, root, additive: true);
        for (var i = 0; i < Nodes.Length; i++)
            Nodes[i] = Animation.AddClip(_graph, Animation.LoadClip($"{FoxPath}#Animation{i}"), 1f, blend, mask: i == 0 ? 0ul : 0x3f);

        var masked = new HashSet<AnimationTarget>();
        for (var group = 0; group < MaskGroupPaths.Length; group++)
        {
            var (prefix, suffix) = (MaskGroupPaths[group].Prefix.Split('/'), MaskGroupPaths[group].Suffix.Split('/'));
            for (var length = 0; length <= suffix.Length; length++)
            {
                var target = AnimationTarget.FromNames([.. prefix, .. suffix[..length]]);
                Animation.AddToMaskGroup(_graph, target, (uint)group);
                masked.Add(target);
            }
        }

        Animation.SetGraph(found, _graph);

        foreach (var entity in ecs.Descendants(_fox))
        {
            if (Animation.TargetOf(entity) is not { } target || masked.Contains(target)) continue;
            ecs.Get<AnimationTargetIdRef>(entity)?.Remove();
            ecs.Get<AnimatedByRef>(entity)?.Remove();
        }

        foreach (var node in Nodes) Animation.PlayNode(found, node, repeat: true);
        _graphMade = true;
    }

    // Bevy's handle_button_toggles. A press chooses a clip for its part, which every clip's node
    // then masks out but the one chosen.
    private static void HandleButtonToggles(BehaviorContext ctx)
    {
        if (!_graphMade) return;

        var ecs = ctx.Ecs;
        foreach (var button in ecs.EntitiesWith<AnimationControl>())
        {
            if (Ui.InteractionOf(button) != UiInteraction.Pressed) continue;
            var control = ecs.GetOrDefault<AnimationControl>(button);
            if (Chosen[control.GroupId] == control.Label) continue;
            Chosen[control.GroupId] = control.Label;

            for (var clip = 0; clip < Nodes.Length; clip++) Animation.SetNodeMask(_graph, Nodes[clip], MaskOf(clip));
        }
    }

    // The groups a clip's node leaves out, every part playing another clip or none, which is the
    // mask Bevy's reaches by setting and clearing one group's bit at each press.
    private static ulong MaskOf(int clip)
    {
        var mask = 0ul;
        for (var group = 0; group < Chosen.Length; group++)
            if ((int)Chosen[group] != clip) mask |= 1ul << group;
        return mask;
    }

    // Bevy's update_ui, the chosen button of each part lit and its label dark.
    private static void UpdateUi(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var button in ecs.EntitiesWith<AnimationControl>())
        {
            var control = ecs.GetOrDefault<AnimationControl>(button);
            var enabled = Chosen[control.GroupId] == control.Label;
            var (background, text) = enabled ? (Color.FromSrgb(1f, 1f, 1f), Color.FromSrgb(0f, 0f, 0f)) : (Color.FromSrgb(0f, 0f, 0f), Color.FromSrgb(1f, 1f, 1f));

            var fill = ecs.Wrap<BackgroundColorRef>(button);
            if (fill.Value != background) fill.Value = background;
            foreach (var child in ecs.ChildrenOf(button))
            {
                var color = ecs.Wrap<TextColorRef>(child);
                if (color.Value != text) color.Value = text;
            }
        }
    }
}

/// <summary>A button choosing the clip a part of the fox plays.</summary>
[Behavior]
public partial struct AnimationControl
{
    /// <summary>The part, its mask group.</summary>
    public uint GroupId;

    /// <summary>The clip it chooses.</summary>
    public AnimationLabel Label;
}

/// <summary>Which clip a part of the fox plays, in the order of the graph's nodes, or none.</summary>
public enum AnimationLabel
{
    /// <summary>The first clip, a fox at rest looking about.</summary>
    Idle = 0,

    /// <summary>The walk.</summary>
    Walk = 1,

    /// <summary>The run.</summary>
    Run = 2,

    /// <summary>None, the part held as it is.</summary>
    Off = 3,
}
