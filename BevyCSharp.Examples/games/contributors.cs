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
    // A contributor's name and commits, kept beside the entity, since a component here holds no
    // string.
    internal struct Contributor
    {
        public int Index;
        public float Hue;
    }

    internal struct Velocity
    {
        public Vec3 Translation;
        public float Rotation;
    }

    private const float Gravity = 9.821f * 100f, SpriteSize = 75f, SelectedZOffset = 100f, ShowcaseTimerSecs = 3f;
    private static readonly string[] ContributorsList = ["Carter Anderson", "And Many More"];

    private static List<(string Name, int Commits)> _contributors = [];
    private static List<Entity> _order = [];
    private static int _index;
    private static float _timer;
    private static Entity _display, _commits;

    // Seeded, so the birds fall the same way every run, as Bevy's shared generator makes them.
    private static Random _random = new(1022316311);

    public static void Build(App app)
    {
        app.Startup(SetupContributorSelection, "contributors.SetupContributorSelection");
        app.Startup(Setup, "contributors.Setup");

        // Bevy chains these for determinism alone.
        app.Update(ApplyGravity, "contributors.Gravity");
        app.Update(Movement, "contributors.Movement");
        app.Update(Collisions, "contributors.Collisions");
        app.Update(Selection, "contributors.Selection");
    }

    private static void SetupContributorSelection(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        (_random, _index, _timer, _order) = (new Random(1022316311), 0, 0f, []);
        _contributors = ContributorsOrFallback();
        var texture = AssetServer.Load(AssetKind.Image, "branding/icon.png");
        for (var i = 0; i < _contributors.Count; i++)
        {
            var at = Transform.At(Range(-400f, 400f), Range(0f, 400f), (float)_random.NextDouble());
            var direction = Range(-1f, 1f);
            var hue = NameToHue(_contributors[i].Name);

            var entity = ecs.Spawn();
            ecs.Add(entity, at);
            ecs.Add(entity, new Contributor { Index = i, Hue = hue });
            ecs.Add(entity, new Velocity { Translation = new Vec3(direction * 500f, 0f, 0f), Rotation = -direction * 5f });

            // Some birds face the other way, for variety.
            Render2d.SetSprite(ecs, entity, texture, new SpriteSettings { Size = (SpriteSize, SpriteSize), Color = Deselected(hue), FlipX = _random.Next(2) == 1 });
            _order.Add(entity);
        }
    }

    private static void Setup(BehaviorContext ctx)
    {
        Render2d.SpawnCamera2d();
        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
        _display = Ui.SpawnText("Contributor showcase", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) }, new UiTextSettings { Font = font, FontSize = 60f });
        _commits = Ui.SpawnTextSpan(_display, string.Empty, new UiTextSettings { Font = font, FontSize = 30f }, (1f, 1f, 1f, 1f));
    }

    // Every three seconds the bird shown steps back and the next comes forward, its name and
    // commits written in its own color.
    private static void Selection(BehaviorContext ctx)
    {
        _timer += ctx.Time.Delta;
        if (_timer < ShowcaseTimerSecs) return;
        _timer -= ShowcaseTimerSecs;

        var ecs = ctx.Ecs;
        Paint(ecs, _order[_index], selected: false);
        _index = (_index + 1) % _order.Count;

        var entity = _order[_index];
        var color = Paint(ecs, entity, selected: true);
        var (name, commits) = _contributors[ecs.GetOrDefault<Contributor>(entity).Index];
        Ui.SetText(_display, name);
        ecs.Wrap<TextSpanRef>(_commits).Value = $"\n{commits} commit{(commits > 1 ? "s" : "")}";
        ecs.Wrap<TextColorRef>(_display).Value = color;
    }

    // A selected bird is bright and a hundred in front of the rest, and one let go is dim and
    // back among them.
    private static Color Paint(EcsWorld ecs, Entity entity, bool selected)
    {
        var hue = ecs.GetOrDefault<Contributor>(entity).Hue;
        var (r, g, b, a) = selected ? Scene.Hsl(hue, 0.9f, 0.7f) : Deselected(hue);
        var color = new Color(r, g, b, a);
        ecs.Wrap<SpriteRef>(entity).Color = color;

        var at = ecs.GetOrDefault<Transform>(entity);
        at.Translation.Z += selected ? SelectedZOffset : -SelectedZOffset;
        ecs.Set(entity, at);
        return color;
    }

    private static (float R, float G, float B, float A) Deselected(float hue) => Scene.Hsl(hue, 0.3f, 0.2f) with { A = 0.92f };

    private static void ApplyGravity(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var entity in ecs.EntitiesWith<Velocity>())
        {
            var velocity = ecs.GetOrDefault<Velocity>(entity);
            velocity.Translation.Y -= Gravity * ctx.Time.Delta;
            ecs.Set(entity, velocity);
        }
    }

    // A bird that reaches the floor is sent up again toward a height somewhere between two fifths
    // of the window and a bird below its top, and one at a side or the ceiling turns back.
    private static void Collisions(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var (width, height) = Window.Size();
        if (width == 0 || height == 0) return;
        var (maxX, maxY) = ((width - SpriteSize) / 2f, (height - SpriteSize) / 2f);
        var maxBounceHeight = MathF.Max(height - SpriteSize * 2f, 0f);
        var minBounceHeight = maxBounceHeight * 0.4f;

        foreach (var entity in ecs.EntitiesWith<Contributor>())
        {
            var (at, velocity) = (ecs.GetOrDefault<Transform>(entity), ecs.GetOrDefault<Velocity>(entity));
            if (at.Translation.Y < -maxY)
            {
                at.Translation.Y = -maxY;
                velocity.Translation.Y = MathF.Sqrt(Range(minBounceHeight, maxBounceHeight) * Gravity * 2f);
            }

            if (at.Translation.Y > maxY)
            {
                at.Translation.Y = maxY;
                velocity.Translation.Y = -velocity.Translation.Y;
            }

            if (at.Translation.X < -maxX || at.Translation.X > maxX)
            {
                at.Translation.X = Math.Clamp(at.Translation.X, -maxX, maxX);
                velocity.Translation.X = -velocity.Translation.X;
                velocity.Rotation = -velocity.Rotation;
            }

            ecs.Set(entity, at);
            ecs.Set(entity, velocity);
        }
    }

    private static void Movement(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var delta = ctx.Time.Delta;
        foreach (var entity in ecs.EntitiesWith<Velocity>())
        {
            var (at, velocity) = (ecs.GetOrDefault<Transform>(entity), ecs.GetOrDefault<Velocity>(entity));
            at.Translation += velocity.Translation * delta;
            at.Rotation = Quat.FromRotationZ(velocity.Rotation * delta) * at.Rotation;
            ecs.Set(entity, at);
        }
    }

    private static float Range(float low, float high) => low + (float)_random.NextDouble() * (high - low);

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
