using Bevy;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers the input focus moved between interface nodes by direction, and the edges a game draws.</summary>
/// <remarks>
/// Three buttons in a row and a fourth under the first, each reached from the others by where it
/// is on the screen, as Bevy's <c>AutoDirectionalNavigation</c> has it, until an edge says other.
/// </remarks>
[Collection("engine")]
public sealed class NavigationTests
{
    /// <summary>
    /// The focus moves to the nearest node each way and nowhere where none lies, and an edge drawn,
    /// blocked or looped goes before the nearest node until the edges are cleared.
    /// </summary>
    [SkippableFact]
    public void TheFocusMovesToTheNearestNodeEachWayAndAnEdgeGoesFirst()
    {
        Needs.Renderer();

        var (a, b, c, d) = (Entity.None, Entity.None, Entity.None, Entity.None);
        var frame = 0;
        var moves = new List<(string Step, Entity? To)>();

        using var app = new App(Config.OffscreenFor(640, 480, frames: 8));
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            Render2d.SpawnCamera2d();

            Entity Button(float left, float top)
            {
                var button = Ui.SpawnNode(new UiSettings
                {
                    Absolute = true,
                    Left = Length.Px(left),
                    Top = Length.Px(top),
                    Width = Length.Px(100f),
                    Height = Length.Px(60f),
                    Interactive = true,
                });
                ecs.Insert<AutoDirectionalNavigationRef>(button);
                return button;
            }

            (a, b, c, d) = (Button(20f, 100f), Button(200f, 100f), Button(380f, 100f), Button(20f, 300f));
            Ui.Focus(a);
        }, "Test.Setup"));

        app.AddSystem(Stage.Update, new SystemDescriptor(_ =>
        {
            // Once the layout has placed the buttons, which a node's place on the screen needs.
            if (++frame != 4) return;
            void Step(string name, CompassOctant way) => moves.Add((name, Navigation.Move(way)));

            Step("east of a", CompassOctant.East);
            Step("east of b", CompassOctant.East);
            Step("east of c, nothing", CompassOctant.East);

            Navigation.BlockEdge(c, CompassOctant.West);
            Step("west of c, blocked", CompassOctant.West);

            Navigation.AddEdge(c, a, CompassOctant.East);
            Step("east of c, by the edge", CompassOctant.East);
            Step("south of a", CompassOctant.South);

            // Each to the next going north and back south, and round from the last, so north of
            // the top one is the bottom one, where the screen has nothing.
            Navigation.AddEdges([a, d], CompassOctant.North, looping: true);
            Step("north of d", CompassOctant.North);
            Step("north of a, round the loop", CompassOctant.North);

            Navigation.Clear();
            Step("north of d, cleared", CompassOctant.North);
            Step("north of a, nothing", CompassOctant.North);
        }, "Test.Navigate"));

        Assert.Equal(0, app.Run());

        List<(string, Entity?)> expected =
        [
            ("east of a", b), ("east of b", c), ("east of c, nothing", null), ("west of c, blocked", null),
            ("east of c, by the edge", a), ("south of a", d), ("north of d", a), ("north of a, round the loop", d),
            ("north of d, cleared", a), ("north of a, nothing", null),
        ];
        Assert.Equal(expected, moves);
    }

    /// <summary>A direction is found from a stick or the arrows held, the diagonal where two are, and none from nothing.</summary>
    [Theory]
    [InlineData(0f, 1f, CompassOctant.North)]
    [InlineData(1f, 1f, CompassOctant.NorthEast)]
    [InlineData(1f, 0f, CompassOctant.East)]
    [InlineData(1f, -1f, CompassOctant.SouthEast)]
    [InlineData(0f, -1f, CompassOctant.South)]
    [InlineData(-1f, -1f, CompassOctant.SouthWest)]
    [InlineData(-1f, 0f, CompassOctant.West)]
    [InlineData(-1f, 1f, CompassOctant.NorthWest)]
    [InlineData(0.9f, 0.1f, CompassOctant.East)]
    public void ADirectionFallsInTheOctantAroundIt(float x, float y, CompassOctant octant)
    {
        Assert.Equal(octant, CompassOctants.Of(new Vec2(x, y)));
        Assert.Null(CompassOctants.Of(Vec2.Zero));
    }
}
