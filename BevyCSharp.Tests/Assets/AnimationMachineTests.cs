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
