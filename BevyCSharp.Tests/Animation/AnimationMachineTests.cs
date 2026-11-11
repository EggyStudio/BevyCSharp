using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers <see cref="AnimationStateMachine"/>, the states a model's clips play in and the
/// conditions that move it between them, run on <c>assets/models/bend.gltf</c> as
/// <see cref="AnimationTests"/> draws it.
/// </summary>
[Collection("engine")]
public sealed class AnimationMachineTests
{

    /// <summary>Swaying until told to bend, by a trigger or a speed, and swaying again once the bend ends.</summary>
    private static AnimationStateMachine Machine() => new()
    {
        States =
        [
            new AnimationMachineState { Name = "Swaying", Clip = "Sway", Speed = 4f },
            new AnimationMachineState { Name = "Bending", Clip = "Bend", Repeat = false, Speed = 4f },
        ],
        Transitions =
        [
            new AnimationMachineTransition { From = "Swaying", To = "Bending", When = AnimationCondition.Triggered, Parameter = "Bend", Blend = 0.1f },
            new AnimationMachineTransition { From = "Swaying", To = "Bending", When = AnimationCondition.Above, Parameter = "Speed", Value = 2f },
            new AnimationMachineTransition { From = "Bending", To = "Swaying", When = AnimationCondition.Finished, Blend = 0f },
        ],
    };

    [Fact]
    public void AMachineNamingAStateItDoesNotHaveIsRefused()
    {
        var unnamed = Machine();
        unnamed.Transitions.Add(new AnimationMachineTransition { To = "Swaying", When = AnimationCondition.True });
        var lost = Machine();
        lost.Transitions.Add(new AnimationMachineTransition { From = "Falling", To = "Swaying" });
        var twice = Machine();
        twice.States.Add(new AnimationMachineState { Name = "Bending", Clip = "Sway" });

        Assert.Contains("no states", Assert.Throws<ArgumentException>(() => Animation.Run(Entity.None, new AnimationStateMachine())).Message);
        Assert.Contains("Falling", Assert.Throws<ArgumentException>(() => Animation.Run(Entity.None, lost)).Message);
        Assert.Contains("Two states are called Bending", Assert.Throws<ArgumentException>(() => Animation.Run(Entity.None, twice)).Message);
        Assert.Contains("no name", Assert.Throws<ArgumentException>(() => Animation.Run(Entity.None, unnamed)).Message);
        Assert.Contains("Wading", Assert.Throws<ArgumentException>(() => Animation.Run(Entity.None, new AnimationStateMachine
        {
            Start = "Wading",
            States = [new AnimationMachineState { Name = "Swaying", Clip = "Sway" }],
        })).Message);

        Assert.Throws<InvalidOperationException>(() => Animation.Trigger(Entity.None, "Bend"));
    }

    /// <summary>
    /// A machine kept in a file comes back with its states, its transitions and their conditions,
    /// as the editor writes them.
    /// </summary>
    [Fact]
    public void AMachineInAFileReadsBackWithItsStatesAndTransitions()
    {
        // Under an asset root of its own for this test alone, since the others load the strip
        // from the repository's.
        using var folder = new TestFolder("bcs-machine-");
        Directory.CreateDirectory(folder.Path);
        var was = Streaming.AssetRoot;
        Streaming.AssetRoot = folder.Path;
        DataAssets.ReloadAll();
        AssetIds.Reindex();

        try
        {
            ReadsBack(folder.Path);
        }
        finally
        {
            Streaming.AssetRoot = was;
            DataAssets.ReloadAll();
            AssetIds.Reindex();
        }
    }

    private static void ReadsBack(string root)
    {
        File.WriteAllText(
            Path.Combine(root, "strip.data.json"),
            """
            { "format": "bevycsharp.data.1", "type": "Bevy.AnimationStateMachine",
              "fields": {
                "Start": "Bending",
                "States": [ { "Name": "Swaying", "Clip": "Sway" }, { "Name": "Bending", "Clip": "Bend", "Repeat": false, "Speed": 2 } ],
                "Transitions": [ { "From": "Swaying", "To": "Bending", "When": "Above", "Parameter": "Speed", "Value": 2.5 } ] } }
            """);
        var id = AssetIds.IdOf("strip.data.json", create: true);

        var machine = DataAssets.Get(new DataRef<AnimationStateMachine>(id));

        Assert.Equal("Bending", machine.Start);
        Assert.Equal(["Swaying", "Bending"], machine.States.Select(state => state.Name));
        Assert.True(machine.States[0].Repeat);
        Assert.False(machine.States[1].Repeat);
        Assert.Equal(2f, machine.States[1].Speed);

        var transition = Assert.Single(machine.Transitions);
        Assert.Equal(AnimationCondition.Above, transition.When);
        Assert.Equal(2.5f, transition.Value);
        Assert.Equal(0.2f, transition.Blend);
    }

    /// <summary>
    /// Runs a test in an asset root of its own holding the strip and a machine of one state that
    /// bends it, with the machine's id, and puts the root back after.
    /// </summary>
    private static void InStripFolder(Action<string, ulong> test)
    {
        using var folder = new TestFolder("bcs-player-");
        Directory.CreateDirectory(Path.Combine(folder.Path, "models"));
        File.Copy(Path.Combine(EngineHarness.AssetDirectory, "models", "bend.gltf"), Path.Combine(folder.Path, "models", "bend.gltf"));
        File.WriteAllText(
            Path.Combine(folder.Path, "strip.data.json"),
            """
            { "format": "bevycsharp.data.1", "type": "Bevy.AnimationStateMachine",
              "fields": { "States": [ { "Name": "Bending", "Clip": "Bend", "Speed": 4 } ] } }
            """);

        var was = Streaming.AssetRoot;
        Streaming.AssetRoot = folder.Path;
        DataAssets.ReloadAll();
        AssetIds.Reindex();

        try
        {
            test(folder.Path, AssetIds.IdOf("strip.data.json", create: true));
        }
        finally
        {
            Streaming.AssetRoot = was;
            DataAssets.ReloadAll();
            AssetIds.Reindex();
        }
    }

    /// <summary>
    /// A model carrying a player that names a machine file runs that machine once it arrives, and
    /// stops when the player is taken off, while one naming no file is said on the log once and
    /// left as it is.
    /// </summary>
    [SkippableFact]
    [ExpectsError("bevy", "did not start")]
    public void APlayerInTheSceneRunsTheMachineItNames()
    {
        Needs.Renderer();

        InStripFolder((root, id) =>
        {
            var (placed, lost) = (Entity.None, Entity.None);
            string? running = null, lostState = "never read", after = "never read";

            var run = new PictureRun
            {
                AssetRoot = root,
                Scene = ecs =>
                {
                    placed = ecs.SpawnScene(AssetServer.LoadGltfScene("models/bend.gltf"));
                    ecs.Add(placed, new AnimationMachinePlayer { Machine = new DataRef<AnimationStateMachine>(id) });

                    lost = ecs.SpawnScene(AssetServer.LoadGltfScene("models/bend.gltf"));
                    ecs.Add(lost, new AnimationMachinePlayer { Machine = new DataRef<AnimationStateMachine>(987654321) });
                },
            };

            run.Until("bending", _ => Animation.MachineStateOf(placed) == "Bending")
                .Do("reading it", world =>
                {
                    running = Animation.StateOf(placed)?.Clip;
                    lostState = Animation.MachineStateOf(lost);
                    world.Resource<EcsWorld>().Remove<AnimationMachinePlayer>(placed);
                })
                .Wait(3)
                .Do("reading it again", _ => after = Animation.MachineStateOf(placed))
                .Go();

            Assert.Equal("Bend", running);
            Assert.Null(lostState);
            Assert.Null(after);
        });
    }

    /// <summary>
    /// A placed model's player is saved with the scene, by the machine file's id, and the scene
    /// loaded again runs the machine on the model it places.
    /// </summary>
    [SkippableFact]
    public void ASceneSavedWithAPlayerLoadsToARunningMachine()
    {
        Needs.Renderer();

        InStripFolder((root, id) =>
        {
            var file = Path.Combine(root, "level.scene.json");
            var (placed, loaded) = (Entity.None, Entity.None);

            var run = new PictureRun
            {
                AssetRoot = root,
                Scene = ecs =>
                {
                    placed = SceneInstances.Spawn(ecs, "models/bend.gltf");
                    ecs.Add(placed, new AnimationMachinePlayer { Machine = new DataRef<AnimationStateMachine>(id) });
                },
            };

            run.Until("bending", _ => Animation.MachineStateOf(placed) == "Bending")
                .Do("saving the scene and loading it again", world =>
                {
                    var ecs = world.Resource<EcsWorld>();
                    SceneFile.Save(ecs, file, entity => entity == placed);
                    ecs.Despawn(placed);
                    loaded = SceneFile.Load(ecs, file).Entities.Single(ecs.Has<AnimationMachinePlayer>);
                })
                .Until("bending again", _ => Animation.MachineStateOf(loaded) == "Bending")
                .Go();

            Assert.Contains("Bevy.AnimationMachinePlayer", File.ReadAllText(file));
            Assert.Null(Animation.MachineStateOf(placed));
        });
    }

    /// <summary>
    /// The machine starts once the model arrives, moves on a trigger and uses it up, moves on a
    /// number passing a value, and comes back when a clip that plays once ends.
    /// </summary>
    [SkippableFact]
    public void AMachineMovesOnATriggerAValueAndAClipEnding()
    {
        Needs.Renderer();

        var scene = Entity.None;
        var seen = new List<(string? State, string? Clip)>();
        string? afterTheTrigger = null;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                scene = ecs.SpawnScene(AssetServer.LoadGltfScene("models/bend.gltf"));
                Animation.Run(scene, Machine());
            },
            EachFrame = _ =>
            {
                var now = (Animation.MachineStateOf(scene), Animation.StateOf(scene)?.Clip);
                if (seen.Count == 0 || seen[^1] != now) seen.Add(now);
            },
        };

        run.Until("swaying", _ => Animation.MachineStateOf(scene) == "Swaying")
            .Do("triggering the bend", _ => Animation.Trigger(scene, "Bend"))
            .Until("bending", _ => Animation.MachineStateOf(scene) == "Bending")
            .Until("swaying once the bend ends", _ => Animation.MachineStateOf(scene) == "Swaying")
            .Wait(10)
            .Do("reading it", _ => afterTheTrigger = Animation.MachineStateOf(scene))
            .Do("speeding up", _ => Animation.SetParameter(scene, "Speed", 3f))
            .Until("bending again", _ => Animation.MachineStateOf(scene) == "Bending")
            .Go();

        // The trigger used up by the first bend, so the strip sways on until the speed passes 2.
        Assert.Equal("Swaying", afterTheTrigger);

        // Each state playing its own clip, the clip following the state.
        Assert.Contains(("Swaying", "Sway"), seen);
        Assert.Contains(("Bending", "Bend"), seen);
        Assert.Null(seen[0].State);
    }
}
