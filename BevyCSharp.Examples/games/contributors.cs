// Bevy's contributors example, examples/showcase/contributors.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using System.Diagnostics;
using System.Text;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Games;

// Shows each contributor to the code as a bouncing Bevy bird, and every three seconds brings the
// next one forward with their name and how many commits they made. The names come from the git
// log of the checkout the example runs in, or a list of two names where there is none.
internal static class Contributors
{
    internal const float Gravity = 9.821f * 100f, SpriteSize = 75f;
    private const float SelectedZOffset = 100f, ShowcaseTimerSecs = 3f;
    private static readonly string[] ContributorsList = ["Carter Anderson", "And Many More"];

    // The names and commits, which a contributor's entity points into, since a component here
    // holds no string.
    internal static List<(string Name, int Commits)> Names = [];

    // Bevy's ContributorSelection and SelectionTimer resources, the birds in the order they are
    // shown, the one shown now, and the three seconds each is shown for.
    private static List<Entity> _order = [];
    private static int _index;
    internal static GameTimer SelectionTimer;

    // Bevy's SharedRng, seeded so the birds fall the same way every run.
    private static Random _random = new(1022316311);

    public static void Build(App app)
    {
        app.Startup(SetupContributorSelection, "contributors.SetupContributorSelection");
        app.Startup(Setup, "contributors.Setup");
    }

    private static void SetupContributorSelection(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        (_random, _index, _order) = (new Random(1022316311), 0, []);
        SelectionTimer = GameTimer.FromSeconds(ShowcaseTimerSecs, TimerMode.Repeating);
        Names = ContributorsOrFallback();
        var texture = AssetServer.Load(AssetKind.Image, "branding/icon.png");
        for (var i = 0; i < Names.Count; i++)
        {
            var at = Transform.At(Range(-400f, 400f), Range(0f, 400f), (float)_random.NextDouble());
            var direction = Range(-1f, 1f);
            var hue = NameToHue(Names[i].Name);

            var entity = ecs.Spawn();
            ecs.Add(entity, at);
            ecs.Add(entity, new Contributor { Index = i, Hue = hue });
            ecs.Add(entity, new ContributorVelocity { Translation = new Vec3(direction * 500f, 0f, 0f), Rotation = -direction * 5f });

            // Some birds face the other way, for variety.
            Render2d.SetSprite(ecs, entity, texture, new SpriteSettings { Size = (SpriteSize, SpriteSize), Color = Deselected(hue), FlipX = _random.Next(2) == 1 });
            _order.Add(entity);
        }
    }

    private static void Setup(BehaviorContext ctx)
    {
        Render2d.SpawnCamera2d();
        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
        var display = Ui.SpawnText("Contributor showcase", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) }, new UiTextSettings { Font = font, FontSize = 60f });
        Ui.SpawnTextSpan(display, string.Empty, new UiTextSettings { Font = font, FontSize = 30f }, (1f, 1f, 1f, 1f));
        ctx.Ecs.Add(display, new ContributorDisplay());
    }

    // The bird shown now let go, and the next brought forward and written on the display in its
    // own color.
    internal static void Select(BehaviorContext ctx, Entity display)
    {
        var ecs = ctx.Ecs;
        Paint(ecs, _order[_index], selected: false);
        _index = (_index + 1) % _order.Count;

        var entity = _order[_index];
        var color = Paint(ecs, entity, selected: true);
        var (name, commits) = Names[ecs.GetOrDefault<Contributor>(entity).Index];
        Ui.SetText(display, name);
        ecs.Wrap<TextSpanRef>(ecs.ChildrenOf(display)[0]).Value = $"\n{commits} commit{(commits > 1 ? "s" : "")}";
        ecs.Wrap<TextColorRef>(display).Value = color;
    }

    // A selected bird is bright and a hundred in front of the rest, and one let go is dim and
    // back among them.
    private static Color Paint(EcsWorld ecs, Entity entity, bool selected)
    {
        var hue = ecs.GetOrDefault<Contributor>(entity).Hue;
        var color = selected ? Color.FromHsl(hue, 0.9f, 0.7f) : Deselected(hue);
        ecs.Wrap<SpriteRef>(entity).Color = color;

        var at = ecs.GetOrDefault<Transform>(entity);
        at.Translation.Z += selected ? SelectedZOffset : -SelectedZOffset;
        ecs.Set(entity, at);
        return color;
    }

    private static Color Deselected(float hue) => Color.FromHsl(hue, 0.3f, 0.2f).WithAlpha(0.92f);

    internal static float Range(float low, float high) => low + (float)_random.NextDouble() * (high - low);

    // The authors of every commit in the checkout and how many each made, or the two names a
    // thousand times over where git is not there to ask or the workflow takes the picture.
    private static List<(string, int)> ContributorsOrFallback()
    {
        var fallback = Enumerable.Range(0, 1000).Select(i => (ContributorsList[i % ContributorsList.Length], 1)).ToList();
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true") return fallback;
        try
        {
            using var git = Process.Start(new ProcessStartInfo("git", ["--no-pager", "log", "--pretty=format:%an"])
            {
                WorkingDirectory = AppContext.BaseDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            if (git is null) return fallback;
            var authors = git.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            git.WaitForExit();
            if (git.ExitCode != 0 || authors.Length == 0) return fallback;
            return authors.GroupBy(name => name).Select(group => (group.Key, group.Count())).ToList();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return fallback;
        }
    }

    // A hue for each name that stays the same from run to run, which string's own hash does not,
    // since .NET seeds it afresh in each process. FNV-1a over the name's bytes is used instead.
    private static float NameToHue(string name)
    {
        var hash = 14695981039346656037UL;
        foreach (var b in Encoding.UTF8.GetBytes(name)) hash = (hash ^ b) * 1099511628211UL;
        return hash / (float)ulong.MaxValue * 360f;
    }
}

/// <summary>The text showing the contributor brought forward.</summary>
[Behavior]
public partial struct ContributorDisplay
{
    /// <summary>
    /// Every three seconds the next contributor brought forward, last of the four Bevy chains,
    /// for determinism alone.
    /// </summary>
    [OnUpdate]
    [After("Contributor.Collisions")]
    public void Selection(BehaviorContext ctx)
    {
        if (Contributors.SelectionTimer.Tick(ctx.Time.Delta).JustFinished) Contributors.Select(ctx, ctx.Entity);
    }
}

/// <summary>A contributor's bird, which bounces about the window.</summary>
[Behavior]
public partial struct Contributor
{
    /// <summary>The contributor's place among the names.</summary>
    public int Index;

    /// <summary>The hue the bird is drawn in, the same for a name from run to run.</summary>
    public float Hue;

    /// <summary>
    /// Sent up again from the floor toward a height somewhere between two fifths of the window and
    /// a bird below its top, and turned back at a side or the ceiling.
    /// </summary>
    [OnUpdate]
    [After("ContributorVelocity.Movement")]
    public void Collisions(BehaviorContext ctx, ref Transform transform, ref ContributorVelocity velocity)
    {
        var (width, height) = Window.Size();
        if (width == 0 || height == 0) return;
        var (maxX, maxY) = ((width - Contributors.SpriteSize) / 2f, (height - Contributors.SpriteSize) / 2f);
        var maxBounceHeight = MathF.Max(height - Contributors.SpriteSize * 2f, 0f);
        var minBounceHeight = maxBounceHeight * 0.4f;

        if (transform.Translation.Y < -maxY)
        {
            transform.Translation.Y = -maxY;
            velocity.Translation.Y = MathF.Sqrt(Contributors.Range(minBounceHeight, maxBounceHeight) * Contributors.Gravity * 2f);
        }

        if (transform.Translation.Y > maxY)
        {
            transform.Translation.Y = maxY;
            velocity.Translation.Y = -velocity.Translation.Y;
        }

        if (transform.Translation.X < -maxX || transform.Translation.X > maxX)
        {
            transform.Translation.X = Math.Clamp(transform.Translation.X, -maxX, maxX);
            velocity.Translation.X = -velocity.Translation.X;
            velocity.Rotation = -velocity.Rotation;
        }
    }
}

/// <summary>
/// How fast a bird moves and turns, Bevy's <c>Velocity</c> under another name since breakout's
/// shares the namespace.
/// </summary>
[Behavior]
public partial struct ContributorVelocity
{
    /// <summary>How fast it moves, in units a second.</summary>
    public Vec3 Translation;

    /// <summary>How fast it turns about Z, in radians a second.</summary>
    public float Rotation;

    /// <summary>Pulled down by gravity, first of the four Bevy chains.</summary>
    [OnUpdate]
    public void Gravity(BehaviorContext ctx) => Translation.Y -= Contributors.Gravity * ctx.Time.Delta;

    /// <summary>Moved and turned by its speeds over the frame.</summary>
    [OnUpdate]
    [After("ContributorVelocity.Gravity")]
    public void Movement(BehaviorContext ctx, ref Transform transform)
    {
        transform.Translation += Translation * ctx.Time.Delta;
        transform.Rotation = Quat.FromRotationZ(Rotation * ctx.Time.Delta) * transform.Rotation;
    }
}
