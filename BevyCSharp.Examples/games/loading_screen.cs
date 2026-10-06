// Bevy's loading_screen example, examples/showcase/loading_screen.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Games;

// Shows a loading screen that waits for a level's assets to load and for the renderer to compile
// what it needs to draw them. Pressing 1 loads a fox and 2 a flight helmet, with the screen up
// until each can be seen.
internal static class LoadingScreen
{
    private enum LoadingState { LevelReady, LevelLoading }

    // Everything has to be ready this many frames running before the level counts as loaded, since
    // a pipeline can be asked for some frames after the last asset arrived.
    private const int ConfirmationFramesTarget = 5;

    private static LoadingState _loadingState;
    private static readonly List<AssetHandle> LoadingAssets = [];
    private static readonly List<Entity> LevelComponents = [];
    private static int _confirmationFrames;
    private static Entity _loadingScreen;

    public static void Build(App app)
    {
        (_loadingState, _confirmationFrames) = (LoadingState.LevelReady, 0);
        LoadingAssets.Clear();
        LevelComponents.Clear();
        app.Startup(Setup, "loading_screen.Setup");
        app.Startup(LoadLoadingScreen, "loading_screen.LoadLoadingScreen");
        app.Update(UpdateLoadingData, "loading_screen.UpdateLoadingData");
        app.Update(LevelSelection, "loading_screen.LevelSelection");
        app.Update(ctx => ctx.Ecs.Wrap<VisibilityRef>(_loadingScreen).Value = _loadingState == LoadingState.LevelLoading ? VisibilityRef.ValueVariant.Visible : VisibilityRef.ValueVariant.Hidden, "loading_screen.DisplayLoadingScreen");
    }

    // The prompt along the bottom.
    private static void Setup(BehaviorContext ctx)
    {
        var node = Ui.SpawnNode(new UiSettings { AlignSelf = UiAlignSelf.FlexEnd });
        ctx.Ecs.Wrap<NodeRef>(node).JustifySelf = NodeRef.JustifySelfVariant.Center;
        ctx.Ecs.SetParent(Ui.SpawnText("Press 1 or 2 to load a new scene.", new UiSettings(), 42f), node);
    }

    // A level is loaded only once the one before it is. Bevy runs the unloading and the loading
    // as one-shot systems it registered, and here they are called, which comes to the same.
    private static void LevelSelection(BehaviorContext ctx)
    {
        if (_loadingState != LoadingState.LevelReady) return;
        if (ctx.Input.KeyPressed(Key.Digit1))
        {
            UnloadCurrentLevel(ctx);
            LoadLevel1(ctx);
        }
        else if (ctx.Input.KeyPressed(Key.Digit2))
        {
            UnloadCurrentLevel(ctx);
            LoadLevel2(ctx);
        }
    }

    private static void UnloadCurrentLevel(BehaviorContext ctx)
    {
        _loadingState = LoadingState.LevelLoading;
        foreach (var entity in LevelComponents) ctx.Ecs.Despawn(entity);
        LevelComponents.Clear();
    }

    private static void LoadLevel1(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        LevelComponents.Add(ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(155f, 155f, 155f), new Vec3(0f, 40f, 0f), Vec3.UnitY)));
        var fox = AssetServer.LoadGltfScene("models/animated/Fox.glb");
        LoadingAssets.Add(fox);
        LevelComponents.Add(ecs.SpawnScene(fox));
        LevelComponents.Add(Light(ecs));
    }

    private static void LoadLevel2(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        LevelComponents.Add(ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(1f, 1f, 1f), new Vec3(0f, 0.2f, 0f), Vec3.UnitY)));
        var helmet = AssetServer.LoadGltfScene("models/FlightHelmet/FlightHelmet.gltf");
        LoadingAssets.Add(helmet);
        LevelComponents.Add(ecs.SpawnScene(helmet));
        LevelComponents.Add(Light(ecs));
    }

    private static Entity Light(EcsWorld ecs)
    {
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = true });
        ecs.Add(light, Transform.LookingAt(new Vec3(3f, 3f, 2f), Vec3.Zero, Vec3.UnitY));
        return light;
    }

    // While an asset is on its way, with all it depends on, or a pipeline is compiling, the count
    // starts over. Once neither has been so for some frames running, the level is ready.
    private static void UpdateLoadingData(BehaviorContext ctx)
    {
        if (LoadingAssets.Count > 0 || !Render.PipelinesReady())
        {
            _confirmationFrames = 0;
            LoadingAssets.RemoveAll(asset => AssetServer.StateWithDependenciesOf(asset) == AssetLoadState.Loaded);
        }
        else if (++_confirmationFrames == ConfirmationFramesTarget)
        {
            _loadingState = LoadingState.LevelReady;
        }
    }

    // A black screen saying it is loading, over the level, with a camera of its own drawn after
    // the level's.
    private static void LoadLoadingScreen(BehaviorContext ctx)
    {
        Render2d.SpawnCamera2d(order: 1);
        _loadingScreen = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Justify = UiJustify.Center, Align = UiAlign.Center, Color = (0f, 0f, 0f, 1f) });
        ctx.Ecs.SetParent(Ui.SpawnText("Loading...", new UiSettings(), 67f), _loadingScreen);
    }
}
