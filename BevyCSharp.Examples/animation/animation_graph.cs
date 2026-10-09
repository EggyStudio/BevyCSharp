// Bevy's animation_graph example, examples/animation/animation_graph.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Globalization;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Animations;

// Demonstrates animation blending with animation graphs. A fox plays its three clips at once,
// mixed by a graph drawn on screen, and dragging across a clip's node sets the weight it plays at.
//
// Bevy's loads its graph from Fox.animgraph.ron unless told not to, and builds the same graph in
// code where it is, as this does, the file not being among the assets here.
internal static class AnimationGraphExample
{
    private const string FoxPath = "models/animated/Fox.glb";
    private const string HelpText = "Click and drag an animation clip node to change its weight";

    // The clips' nodes in the graph, and the weights they play at.
    private static readonly uint[] ClipNodes = new uint[3];
    private static readonly float[] Weights = [1f, 1f, 1f];
    private static bool _weightsChanged = true;

    private static AssetHandle _graph;
    private static Entity _fox;
    private static Entity? _player;

    private readonly record struct NodeRect(float Left, float Bottom, float Width, float Height);
    private readonly record struct Line(float Left, float Bottom, float Length);

    // Each node's label, the clips' with their place among the weights, and where it is drawn.
    private static readonly (string Text, int Clip)[] NodeTypes = [("Idle", 0), ("Walk", 1), ("Root", -1), ("Blend\n0.5", -1), ("Run", 2)];
    private static readonly NodeRect[] NodeRects =
    [
        new(10.00f, 10.00f, 97.64f, 48.41f),
        new(10.00f, 78.41f, 97.64f, 48.41f),
        new(286.08f, 78.41f, 97.64f, 48.41f),
        new(148.04f, 112.61f, 97.64f, 48.41f),
        new(10.00f, 146.82f, 97.64f, 48.41f),
    ];
    private static readonly Line[] HorizontalLines =
    [
        new(107.64f, 34.21f, 158.24f), new(107.64f, 102.61f, 20.20f), new(107.64f, 171.02f, 20.20f),
        new(127.84f, 136.82f, 20.20f), new(245.68f, 136.82f, 20.20f), new(265.88f, 102.61f, 20.20f),
    ];
    private static readonly Line[] VerticalLines = [new(127.83f, 102.61f, 68.40f), new(265.88f, 34.21f, 102.61f)];

    public static void Build(App app)
    {
        app.Startup(SetupAssets, "animation_graph.SetupAssets");
        app.Startup(SetupScene, "animation_graph.SetupScene");
        app.Startup(SetupUi, "animation_graph.SetupUi");
        app.Update(InitAnimations, "animation_graph.InitAnimations");
        app.Update(HandleWeightDrag, "animation_graph.HandleWeightDrag");
        app.Update(UpdateUi, "animation_graph.UpdateUi");
        app.Update(SyncWeights, "animation_graph.SyncWeights");
    }

    // The graph, a blend at a half and the first clip at the root, the other two under the blend.
    private static void SetupAssets(BehaviorContext ctx)
    {
        (_player, _weightsChanged) = (null, true);
        Array.Fill(Weights, 1f);

        (_graph, var root) = Animation.CreateGraph();
        var blend = Animation.AddBlend(_graph, 0.5f, root);
        ClipNodes[0] = Animation.AddClip(_graph, Animation.LoadClip($"{FoxPath}#Animation0"), 1f, root);
        ClipNodes[1] = Animation.AddClip(_graph, Animation.LoadClip($"{FoxPath}#Animation1"), 1f, blend);
        ClipNodes[2] = Animation.AddClip(_graph, Animation.LoadClip($"{FoxPath}#Animation2"), 1f, blend);
    }

    private static void SetupScene(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render.SetAmbientLight((1f, 1f, 1f), 100f);
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-10f, 5f, 13f), new Vec3(0f, 1f, 0f), Vec3.UnitY));

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
        Ui.SpawnText(HelpText, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });

        var white = Color.FromSrgb(1f, 1f, 1f);
        for (var i = 0; i < NodeRects.Length; i++)
        {
            var (rect, (label, clip)) = (NodeRects[i], NodeTypes[i]);
            var container = Ui.SpawnNode(new UiSettings
            {
                Absolute = true,
                Bottom = Length.Px(rect.Bottom),
                Left = Length.Px(rect.Left),
                Height = Length.Px(rect.Height),
                Width = Length.Px(rect.Width),
                Align = UiAlign.Center,
                AlignContent = UiJustify.Center,
                Justify = UiJustify.Center,
                BorderColor = white,
                Interactive = clip >= 0,
            });
            var outline = ecs.Insert<OutlineRef>(container);
            (outline.Width, outline.Offset, outline.Color) = (new Val.Px(1f), new Val.Px(0f), white);

            if (clip >= 0)
            {
                ecs.Insert<RelativeCursorPositionRef>(container);
                ecs.Add(container, new ClipNode { Index = clip });

                var background = Ui.SpawnNode(new UiSettings
                {
                    Absolute = true,
                    Top = Length.Px(0f),
                    Left = Length.Px(0f),
                    Height = Length.Px(rect.Height),
                    Width = Length.Px(rect.Width),
                    Color = Color.FromSrgb8(0, 100, 0),
                });
                ecs.Add(background, new ClipNodeBackground());
                ecs.SetParent(background, container);
            }

            var text = Ui.SpawnText(label, new UiSettings { Color = Color.FromSrgb8(250, 235, 215) }, new UiTextSettings { FontSize = 16f, Justify = TextJustify.Center });
            ecs.SetParent(text, container);
        }

        foreach (var line in HorizontalLines)
        {
            Ui.SpawnNode(new UiSettings
            {
                Absolute = true,
                Bottom = Length.Px(line.Bottom),
                Left = Length.Px(line.Left),
                Height = Length.Px(0f),
                Width = Length.Px(line.Length),
                Border = new Sides(Length.Zero, Length.Zero, Length.Zero, Length.Px(1f)),
                BorderColor = white,
            });
        }

        foreach (var line in VerticalLines)
        {
            Ui.SpawnNode(new UiSettings
            {
                Absolute = true,
                Bottom = Length.Px(line.Bottom),
                Left = Length.Px(line.Left),
                Height = Length.Px(line.Length),
                Width = Length.Px(0f),
                Border = new Sides(Length.Px(1f), Length.Zero, Length.Zero, Length.Zero),
                BorderColor = white,
            });
        }
    }

    // Bevy's init_animations. The player the fox's scene brings is given the graph, and plays all
    // three clips over and over.
    private static void InitAnimations(BehaviorContext ctx)
    {
        if (_player is not null) return;

        var ecs = ctx.Ecs;
        foreach (var entity in ecs.Descendants(_fox))
        {
            if (ecs.Get<AnimationPlayerRef>(entity) is null) continue;

            Animation.SetGraph(entity, _graph);
            foreach (var node in ClipNodes) Animation.PlayNode(entity, node, repeat: true);
            _player = entity;
            break;
        }
    }

    // Bevy's handle_weight_drag, a clip's weight set from where across its node the pointer
    // presses.
    private static void HandleWeightDrag(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var node in ecs.EntitiesWith<ClipNode>())
        {
            if (Ui.InteractionOf(node) != UiInteraction.Pressed || ecs.Wrap<RelativeCursorPositionRef>(node).Normalized is not { } at) continue;
            Weights[ecs.GetOrDefault<ClipNode>(node).Index] = Math.Clamp(at.X, 0f, 1f);
            _weightsChanged = true;
        }
    }

    // Bevy's update_ui, each clip's node filled as far as its weight and labeled with it.
    private static void UpdateUi(BehaviorContext ctx)
    {
        if (!_weightsChanged) return;
        _weightsChanged = false;

        var ecs = ctx.Ecs;
        foreach (var node in ecs.EntitiesWith<ClipNode>())
        {
            var clip = ecs.GetOrDefault<ClipNode>(node).Index;
            foreach (var child in ecs.ChildrenOf(node))
            {
                if (ecs.Has<ClipNodeBackground>(child)) ecs.Wrap<NodeRef>(child).Width = new Val.Px(NodeRects[0].Width * Weights[clip]);
                else Ui.SetText(child, string.Create(CultureInfo.InvariantCulture, $"{NodeTypes[Array.FindIndex(NodeTypes, type => type.Clip == clip)].Text}\n{Weights[clip]:0.00}"));
            }
        }
    }

    // Bevy's sync_weights, each clip playing at its weight.
    private static void SyncWeights(BehaviorContext ctx)
    {
        if (_player is not { } player) return;
        for (var i = 0; i < ClipNodes.Length; i++)
        {
            if (!Animation.SetNodeWeight(player, ClipNodes[i], Weights[i]))
            {
                Animation.PlayNode(player, ClipNodes[i], repeat: true);
                Animation.SetNodeWeight(player, ClipNodes[i], Weights[i]);
            }
        }
    }
}

/// <summary>A clip's node as the graph is drawn, with its place among the weights.</summary>
[Behavior]
public partial struct ClipNode
{
    /// <summary>Which clip.</summary>
    public int Index;
}

/// <summary>The bar filling a clip's node as far as its weight.</summary>
[Behavior]
public partial struct ClipNodeBackground;
