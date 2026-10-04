using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json.Nodes;
using Bevy;
using Bevy.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Compiles a use of every attribute the generators act on and runs it, asserting it does what it
/// says when it should and nothing otherwise.
/// </summary>
/// <remarks>
/// <para>
/// The attributes are taken from the generators' own table (<see cref="RecognizedAttributes"/>), so
/// one added there with no case here fails, by name. An attribute nothing in the repository used
/// could stop compiling, or never compile, while every other test passed.
/// </para>
/// <para>
/// The probes are one source compiled once, at run time, with the same generators a game is
/// compiled with, as a game would write them, and each case registers only the behaviors it is
/// about in an engine of its own. Their counters are static fields, read back by name.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class GeneratorAttributeTests
{
    /// <summary>Every behavior the cases use, as a game would write them.</summary>
    private const string Source = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using Bevy;
        using System.Text.Json.Nodes;

        namespace Probes;

        [InitialState(Phase.A)]
        public enum Phase { A, B }

        [Behavior]
        public partial struct Marked { }

        public static class Ran
        {
            public static readonly Dictionary<string, List<int>> Frames = new();
            public static int Frame;
            public static bool Allowed;

            public static void Note(string what)
            {
                if (!Frames.TryGetValue(what, out var frames)) Frames[what] = frames = [];
                frames.Add(Frame);
            }
        }

        [Behavior]
        public partial struct Clock
        {
            [OnFirst] public static void Tick(BehaviorContext ctx)
            {
                Ran.Frame++;
                Ran.Allowed = Ran.Frame >= 3;
            }
        }

        [Behavior]
        public partial struct Stages
        {
            [OnStartup] public static void Startup(BehaviorContext ctx) => Ran.Note("OnStartup");
            [OnFirst] public static void First(BehaviorContext ctx) => Ran.Note("OnFirst");
            [OnPreUpdate] public static void PreUpdate(BehaviorContext ctx) => Ran.Note("OnPreUpdate");
            [OnFixedUpdate] public static void FixedUpdate(BehaviorContext ctx) => Ran.Note("OnFixedUpdate");
            [OnUpdate] public static void Update(BehaviorContext ctx) => Ran.Note("OnUpdate");
            [OnPostUpdate] public static void PostUpdate(BehaviorContext ctx) => Ran.Note("OnPostUpdate");
            [OnRender] public static void Render(BehaviorContext ctx) => Ran.Note("OnRender");
            [OnLast] public static void Last(BehaviorContext ctx) => Ran.Note("OnLast");
            [OnCleanup] public static void Cleanup(BehaviorContext ctx) => Ran.Note("OnCleanup");
        }

        [Behavior]
        public partial struct States
        {
            [OnPreUpdate] public static void Count(BehaviorContext ctx)
            {
                if (Ran.Frame == 3) ctx.SetState(Phase.B);
                if (Ran.Frame == 6) ctx.SetState(Phase.A);
                if (App.TryState<Phase>(out var phase) && phase == Phase.A) Ran.Note("InitialState");
            }

            [OnEnter(Phase.B)] public static void Entered(BehaviorContext ctx) => Ran.Note("OnEnter");
            [OnExit(Phase.B)] public static void Left(BehaviorContext ctx) => Ran.Note("OnExit");
            [OnUpdate, InState(Phase.B)] public static void During(BehaviorContext ctx) => Ran.Note("InState");
        }

        [Behavior]
        public partial struct Filtered
        {
            public int Value;

            [OnPreUpdate] public static void Count(BehaviorContext ctx)
            {
                if (Ran.Frame != 4) return;
                foreach (var row in ctx.Ecs.Query<Filtered>(markChanged: false))
                    if (ctx.Ecs.Has<Marked>(row.Entity)) ctx.Ecs.Set(row.Entity, new Unkept { Coins = 7 });
            }

            [OnUpdate, With(typeof(Marked))] public void Marked(BehaviorContext ctx) => Ran.Note("With:" + ctx.Ecs.NameOf(ctx.Entity));
            [OnUpdate, Without(typeof(Marked))] public void Plain(BehaviorContext ctx) => Ran.Note("Without:" + ctx.Ecs.NameOf(ctx.Entity));
            // Another component's change, since a behavior's own methods take it by reference and
            // so mark it changed every frame they run.
            [OnUpdate, Changed(typeof(Unkept))] public void Changed(BehaviorContext ctx) => Ran.Note("Changed:" + ctx.Ecs.NameOf(ctx.Entity));
        }

        [Behavior]
        public partial struct Conditional
        {
            public static bool Allowed(World world) => Ran.Allowed;

            [OnUpdate, RunIf(nameof(Allowed))] public static void When(BehaviorContext ctx) => Ran.Note("RunIf");
            [OnUpdate, ToggleKey(Key.F3)] public static void On(BehaviorContext ctx) => Ran.Note("ToggleKey:on");
            [OnUpdate, ToggleKey(Key.F4, DefaultEnabled = false)] public static void Off(BehaviorContext ctx) => Ran.Note("ToggleKey:off");
        }

        [Behavior, Persist]
        public partial struct Kept { public int Coins; }

        [Behavior]
        public partial struct Unkept { public int Coins; }

        [Behavior, FormerName("Elder"), DataVersion(2)]
        public partial struct Renamed
        {
            [FormerName("Max")] public float Most;
            public float Current;

            public static JsonObject Migrate(int from, JsonObject value)
            {
                if (from < 2) value["Current"] = (float?)value["Current"] * 10;
                return value;
            }
        }

        [DataAsset]
        public sealed class Stats
        {
            public float Damage = 3f;
        }

        [Flags]
        public enum Sides { None = 0, Left = 1, Right = 2 }

        [Behavior]
        public partial struct Shown
        {
            [Label("Shown name"), Tooltip("Said when pointed at"), Header("Above"), Unit("m"), Range(0, 10), Step(0.5)]
            public float Measured;

            [ReadOnly] public int Fixed;
            [Hidden] public int Secret;
            [Space, Separator] public int Apart;
            [Color] public Vec3 Tint;
            [Wide] public int Across;
            [Inline] public Vec3 Corner;
            [Foldout("Advanced", Open = false)] public int Folded;
            [Info("A note", Kind = NoteKind.Warning)] public int Noted;
            [Order(5)] public int Late;
            [Asset("image")] public AssetHandle Picture;
            public bool Toggle;
            [ShowIf(nameof(Toggle), true)] public int WhenOn;
            [HideIf(nameof(Toggle), true)] public int WhenOff;
            [OnValueChanged(nameof(Changed))] public int Watched;
            public Sides Sides;

            public static int ChangedCount;
            public void Changed() => ChangedCount++;

            [Button("Press me")] public void Press() => Ran.Note("Button");
        }

        public static class Commands
        {
            [Command("probe.say", "Says a word back")]
            public static string Say(string word) => "said " + word;
        }
        """;

    private static readonly Lazy<Assembly> Probes = new(Compile);

    /// <summary>The probes, compiled with the generators a game is and loaded once, initializers run.</summary>
    private static Assembly Compile()
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location));

        var compilation = CSharpCompilation.Create(
            "Probes",
            [CSharpSyntaxTree.ParseText(Source, path: "Probes.cs")],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(
            new BehaviorGenerator().AsSourceGenerator(),
            new StateGenerator().AsSourceGenerator(),
            new CommandGenerator().AsSourceGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var generated, out var generatorErrors);

        using var image = new MemoryStream();
        var result = generated.Emit(image);
        var errors = result.Diagnostics.Concat(generatorErrors).Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(result.Success, string.Join("\n", errors));

        image.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(image);
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        return assembly;
    }

    /// <summary>The frames each probe ran in, by what it noted.</summary>
    private static Dictionary<string, List<int>> Frames =>
        (Dictionary<string, List<int>>)Probes.Value.GetType("Probes.Ran")!.GetField("Frames")!.GetValue(null)!;

    private static List<int> FramesOf(string what) => Frames.TryGetValue(what, out var frames) ? frames : [];

    /// <summary>Clears the counters, then runs an engine with the probes' systems in it.</summary>
    /// <remarks>
    /// All of them, since the generator registers an assembly's behaviors together, and each case
    /// reads only the counters its own probe notes.
    /// </remarks>
    private static void Run(uint frames, Action<BehaviorContext>? setup = null) =>
        Run(new EngineHarness(frames: frames), setup);

    private static void Run(EngineHarness harness, Action<BehaviorContext>? setup)
    {
        var ran = Probes.Value.GetType("Probes.Ran")!;
        ((Dictionary<string, List<int>>)ran.GetField("Frames")!.GetValue(null)!).Clear();
        ran.GetField("Frame")!.SetValue(null, 0);
        ran.GetField("Allowed")!.SetValue(null, false);

        using (harness)
        {
            if (setup is not null) harness.OnContext(Stage.Startup, setup);

            foreach (var type in Probes.Value.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (method.GetCustomAttribute<GeneratedBehaviorRegistrationAttribute>() is not null)
                        method.Invoke(null, [harness.App]);
                }
            }

            harness.Run();
        }
    }

    private static ComponentSchema Schema(string name)
    {
        _ = Probes.Value;
        return ComponentSchemas.For("Probes." + name) ?? throw new InvalidOperationException($"No schema for Probes.{name}.");
    }

    private static FieldHints Hints(string field) => Schema("Shown").Field(field)!.Hints;

    /// <summary>What each attribute is checked by, by the generators' name for it.</summary>
    private static readonly Dictionary<string, Action> Cases = new(StringComparer.Ordinal)
    {
        [RecognizedAttributes.Behavior] = () => Assert.NotNull(Schema("Stages")),

        [RecognizedAttributes.OnStartup] = () =>
        {
            Run(5);
            Assert.Single(FramesOf("OnStartup"));
        },
        [RecognizedAttributes.OnFirst] = () => EveryFrame("OnFirst"),
        [RecognizedAttributes.OnPreUpdate] = () => EveryFrame("OnPreUpdate"),
        [RecognizedAttributes.OnUpdate] = () => EveryFrame("OnUpdate"),
        [RecognizedAttributes.OnPostUpdate] = () => EveryFrame("OnPostUpdate"),
        [RecognizedAttributes.OnRender] = () => EveryFrame("OnRender"),
        [RecognizedAttributes.OnLast] = () => EveryFrame("OnLast"),
        [RecognizedAttributes.OnFixedUpdate] = () =>
        {
            // Twice the frames a second the fixed step is, so it runs in about half of them.
            Run(new EngineHarness(frames: 40, fps: 240, fixedHz: 120), null);
            Assert.InRange(FramesOf("OnFixedUpdate").Count, 10, 30);
        },
        [RecognizedAttributes.OnCleanup] = () =>
        {
            Run(5);
            Assert.Single(FramesOf("OnCleanup"));
        },

        [RecognizedAttributes.InitialState] = () =>
        {
            Run(8);
            Assert.Contains(1, FramesOf("InitialState"));
        },
        [RecognizedAttributes.OnEnter] = () =>
        {
            Run(8);
            Assert.Single(FramesOf("OnEnter"));
        },
        [RecognizedAttributes.OnExit] = () =>
        {
            Run(8);
            Assert.Single(FramesOf("OnExit"));
        },
        [RecognizedAttributes.InState] = () =>
        {
            // Between the frame B is set and the frame A is, and in no other.
            Run(8);
            var during = FramesOf("InState");
            Assert.NotEmpty(during);
            Assert.All(during, frame => Assert.InRange(frame, 3, 7));
        },

        [RecognizedAttributes.With] = () =>
        {
            Run(4, Spawn);
            Assert.NotEmpty(FramesOf("With:marked"));
            Assert.Empty(FramesOf("With:plain"));
        },
        [RecognizedAttributes.Without] = () =>
        {
            Run(4, Spawn);
            Assert.NotEmpty(FramesOf("Without:plain"));
            Assert.Empty(FramesOf("Without:marked"));
        },
        [RecognizedAttributes.Changed] = () =>
        {
            // The marked one is changed on the fourth frame, and neither after their first.
            Run(7, Spawn);
            Assert.Contains(4, FramesOf("Changed:marked"));
            Assert.All(FramesOf("Changed:marked"), frame => Assert.True(frame <= 1 || frame == 4, $"ran on frame {frame}"));
            Assert.All(FramesOf("Changed:plain"), frame => Assert.True(frame <= 1, $"ran on frame {frame}"));
        },

        [RecognizedAttributes.RunIf] = () =>
        {
            Run(6);
            var when = FramesOf("RunIf");
            Assert.NotEmpty(when);
            Assert.All(when, frame => Assert.True(frame >= 3, $"ran on frame {frame}"));
        },
        [RecognizedAttributes.ToggleKey] = () =>
        {
            Run(4);
            Assert.NotEmpty(FramesOf("ToggleKey:on"));
            Assert.Empty(FramesOf("ToggleKey:off"));
        },

        [RecognizedAttributes.Persist] = () =>
        {
            Assert.True(Schema("Kept").Persisted);
            Assert.False(Schema("Unkept").Persisted);
        },
        [RecognizedAttributes.FormerName] = () =>
        {
            Assert.Same(Schema("Renamed"), ComponentSchemas.For("Probes.Elder"));
            Assert.Contains("Max", Schema("Renamed").Field("Most")!.Hints.FormerNames);
        },
        [RecognizedAttributes.DataVersion] = () =>
        {
            var schema = Schema("Renamed");
            Assert.Equal(2, schema.Version);
            var migrated = schema.Migrate!(1, new JsonObject { ["Current"] = 2f });
            Assert.Equal(20f, (float)migrated["Current"]!);
        },
        [RecognizedAttributes.DataAsset] = () =>
        {
            _ = Probes.Value;
            Assert.Contains("Probes.Stats", DataAssets.Types);
        },
        [RecognizedAttributes.Command] = () =>
        {
            _ = Probes.Value;
            Assert.Equal("said hello", ConsoleCommands.Run("probe.say hello"));
        },

        [RecognizedAttributes.Label] = () => Assert.Equal("Shown name", Hints("Measured").Label),
        [RecognizedAttributes.Tooltip] = () => Assert.Equal("Said when pointed at", Hints("Measured").Tooltip),
        [RecognizedAttributes.Header] = () => Assert.Equal("Above", Hints("Measured").Header),
        [RecognizedAttributes.Unit] = () => Assert.Equal("m", Hints("Measured").Unit),
        [RecognizedAttributes.Range] = () => Assert.Equal((0d, 10d), (Hints("Measured").Minimum!.Value, Hints("Measured").Maximum!.Value)),
        [RecognizedAttributes.Step] = () => Assert.Equal(0.5, Hints("Measured").Step),
        [RecognizedAttributes.ReadOnly] = () =>
        {
            Assert.True(Hints("Fixed").ReadOnly);
            Assert.False(Hints("Across").ReadOnly);
        },
        [RecognizedAttributes.Hidden] = () => Assert.True(Hints("Secret").Hidden),
        [RecognizedAttributes.Space] = () => Assert.True(Hints("Apart").Space),
        [RecognizedAttributes.Separator] = () => Assert.True(Hints("Apart").Separator),
        [RecognizedAttributes.Color] = () => Assert.True(Hints("Tint").Color),
        [RecognizedAttributes.Wide] = () => Assert.True(Hints("Across").Wide),
        [RecognizedAttributes.Inline] = () => Assert.True(Hints("Corner").Inline),
        [RecognizedAttributes.Foldout] = () => Assert.Equal(("Advanced", false), (Hints("Folded").Foldout, Hints("Folded").FoldoutOpen)),
        [RecognizedAttributes.Info] = () => Assert.Equal(("A note", NoteKind.Warning), (Hints("Noted").Note, Hints("Noted").NoteKind)),
        [RecognizedAttributes.Order] = () => Assert.Equal(5, Hints("Late").Order),
        [RecognizedAttributes.Asset] = () => Assert.Equal("image", Hints("Picture").Asset),
        [RecognizedAttributes.ShowIf] = () => Assert.Single(Hints("WhenOn").Conditions),
        [RecognizedAttributes.HideIf] = () => Assert.Single(Hints("WhenOff").Conditions),
        [RecognizedAttributes.OnValueChanged] = () => Assert.Equal(["Changed"], Hints("Watched").Changed),
        [RecognizedAttributes.Flags] = () => Assert.Equal(FieldKind.Flags, Schema("Shown").Field("Sides")!.Kind),
        [RecognizedAttributes.Button] = () =>
        {
            var button = Schema("Shown").Method("Press");
            Assert.NotNull(button);
            Assert.Equal("Press me", button!.Hints.Label);

            Run(2, ctx =>
            {
                var shown = ctx.Ecs.Spawn();
                Schema("Shown").Add(ctx.Ecs, shown);
                button.Run(ctx.Ecs, shown);
            });
            Assert.Single(FramesOf("Button"));
        },
    };

    /// <summary>Two entities carrying the filtered behavior, the first marked.</summary>
    private static void Spawn(BehaviorContext ctx)
    {
        var marked = ctx.Ecs.Spawn();
        ctx.Ecs.SetName(marked, "marked");
        Schema("Filtered").Add(ctx.Ecs, marked);
        Schema("Marked").Add(ctx.Ecs, marked);

        var plain = ctx.Ecs.Spawn();
        ctx.Ecs.SetName(plain, "plain");
        Schema("Filtered").Add(ctx.Ecs, plain);

        // Both carry the component the change is watched on, which only the marked one's changes.
        Schema("Unkept").Add(ctx.Ecs, marked);
        Schema("Unkept").Add(ctx.Ecs, plain);
    }

    private static void EveryFrame(string stage)
    {
        Run(5);
        Assert.InRange(FramesOf(stage).Count, 4, 6);
    }

    [Fact]
    public void EveryRecognizedAttributeHasACase()
    {
        // The table holds every name it declares, and every name has a case here.
        var declared = typeof(RecognizedAttributes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.Name != nameof(RecognizedAttributes.Namespace))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(declared.Order(StringComparer.Ordinal), RecognizedAttributes.All.Order(StringComparer.Ordinal));
        Assert.Empty(RecognizedAttributes.All.Where(name => !Cases.ContainsKey(name)));
    }

    public static TheoryData<string> Recognized() => [.. RecognizedAttributes.All];

    [Theory]
    [MemberData(nameof(Recognized))]
    public void EachAttributeDoesWhatItSays(string attribute)
    {
        Assert.True(Cases.TryGetValue(attribute, out var check), $"{attribute} has no case.");
        check!();
    }
}
