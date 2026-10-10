// Bevy's mines example, examples/showcase/mines.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Games;

// A minesweeper in the interface. A menu offers three sizes of field, a left click on a tile reveals
// it, opening every tile around one with no mine beside it, a right click flags it, and a mine
// clicked ends the game. Bevy's field is shuffled by rand and this one by .NET's random numbers,
// so the same game is not dealt.
internal static class Mines
{
    private const float TileSize = 30f;
    private const float TileGap = 6f;
    private const float BoardPadding = 15f;

    private static readonly Color Background = Color.FromSrgb8(0, 100, 0);
    private static readonly Color TileBorder = Color.FromSrgb8(46, 139, 87);
    private static readonly Color HoveredTileBorder = Color.FromSrgb8(0, 255, 0);
    private static readonly Color MildDanger = Color.FromSrgb8(255, 255, 0);
    private static readonly Color SomeDanger = Color.FromSrgb8(255, 165, 0);
    private static readonly Color VeryDanger = Color.FromSrgb8(255, 69, 0);
    private static readonly Color MostDanger = Color.FromSrgb8(255, 0, 0);

    private enum GameState { Menu, Playing }

    // What each button does, as Bevy's ButtonAction, by the button.
    private static readonly Dictionary<Entity, Action<BehaviorContext>> Actions = [];

    // Where each covered tile is, as Bevy's TileCell, by the tile.
    private static readonly Dictionary<Entity, (int X, int Y)> Tiles = [];

    private static MineField _field = MineField.Of(Difficulty.Normal);
    private static bool _gameOver;
    private static Entity _root = Entity.None;

    private enum Difficulty { Easy, Normal, Hard }

    public static void Build(App app)
    {
        // Here rather than at startup, since the menu is entered before startup runs.
        Actions.Clear();
        Tiles.Clear();
        (_field, _gameOver) = (MineField.Of(Difficulty.Normal), false);

        app.AddState(GameState.Menu);
        app.Startup(_ => Render2d.SpawnCamera2d(), "mines.Setup");

        app.AddStateSystem(GameState.Menu, entering: true, new SystemDescriptor(world => SetupMenu(world.Resource<EcsWorld>()), "mines.SetupMenu"));
        app.AddStateSystem(GameState.Playing, entering: true, new SystemDescriptor(world => SetupGame(world.Resource<EcsWorld>()), "mines.SetupGame"));
    }

    private static void SetupMenu(EcsWorld ecs)
    {
        Actions.Clear();
        var menu = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Direction = UiDirection.Column,
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            RowGap = Length.Px(20f),
        });
        ecs.DespawnOnExit(menu, GameState.Menu);
        ecs.SetParent(Ui.SpawnText("Bevy Mines", new UiSettings(), 35f), menu);

        var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, RowGap = Length.Px(10f) });
        ecs.SetParent(column, menu);
        foreach (var (label, difficulty) in new[] { ("easy", Difficulty.Easy), ("normal", Difficulty.Normal), ("hard", Difficulty.Hard) })
            ecs.SetParent(TextButton(ecs, label, ctx => NewGame(ctx, difficulty)), column);
    }

    private static void SetupGame(EcsWorld ecs)
    {
        _root = Ui.SpawnNode(new UiSettings { Margin = Sides.All(Length.Auto) });
        ecs.DespawnOnExit(_root, GameState.Playing);
        Rebuild(ecs);
    }

    // The board and the line under it, made again after every move, as Bevy's rebuild_game_ui.
    private static void Rebuild(EcsWorld ecs)
    {
        foreach (var child in ecs.ChildrenOf(_root)) ecs.Despawn(child);
        Actions.Clear();
        Tiles.Clear();

        var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, RowGap = Length.Px(20f) });
        ecs.SetParent(column, _root);

        var board = Ui.SpawnNode(new UiSettings
        {
            Display = UiDisplay.Grid,
            Padding = Sides.All(Length.Px(BoardPadding)),
            Corners = Corners.All(Length.Px(25f)),
            RowGap = Length.Px(TileGap),
            ColumnGap = Length.Px(TileGap),
            Color = Background,
        });
        UiGrid.Set(board, new GridSettings { Columns = [Track.Px(TileSize).Repeated(_field.Width)], Rows = [Track.Px(TileSize).Repeated(_field.Height)] });
        ecs.SetParent(board, column);

        foreach (var (x, y, tile) in _field.All())
        {
            Entity node;
            if (_gameOver && tile.Mined)
            {
                node = Ui.SpawnNode(Tile((0f, 0f, 0f, 0f)));
                Ignore(ecs, node);
                ecs.SetParent(Ui.SpawnText("*", new UiSettings { Color = MostDanger }, 24f), node);
            }
            else if (_gameOver || tile.Revealed)
            {
                var mines = _field.AdjacentMines(x, y);
                if (mines == 0) continue;

                var color = mines switch { 1 => MildDanger, 2 => SomeDanger, 3 => VeryDanger, _ => MostDanger };
                node = Ui.SpawnNode(Tile(color));
                Ignore(ecs, node);
                ecs.SetParent(Ui.SpawnText(mines.ToString(), new UiSettings { Color = color }, 20f), node);
            }
            else
            {
                node = Ui.SpawnNode(Tile(TileBorder));
                Tiles[node] = (x, y);
                Listen(ecs, node);
                if (tile.Flagged) ecs.SetParent(Flag(ecs, 20f), node);
            }

            UiGrid.Place(node, new GridPlacement { Column = x + 1, Row = y + 1 });
            ecs.SetParent(node, board);
        }

        var bar = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Px(50f),
            Margin = Sides.Vertical(Length.Px(15f)),
            Justify = UiJustify.SpaceBetween,
            Align = UiAlign.Center,
        });
        ecs.SetParent(bar, column);

        var counter = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, ColumnGap = Length.Px(3f) });
        ecs.SetParent(counter, bar);
        ecs.SetParent(Flag(ecs, 28f), counter);
        ecs.SetParent(Ui.SpawnText((_field.MineCount - _field.FlagCount).ToString(), new UiSettings(), 24f), counter);

        if (!_field.Cleared && !_gameOver) return;

        var message = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Absolute = true,
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
        });
        Ignore(ecs, message);
        ecs.SetParent(Ui.SpawnText(_field.Cleared ? "mines cleared!" : "boom!", new UiSettings(), 24f), message);
        ecs.SetParent(message, bar);
        ecs.SetParent(TextButton(ecs, "new game", ctx => ctx.SetState(GameState.Menu)), bar);
    }

    // A tile of the board, its border in the color given.
    private static UiSettings Tile((float R, float G, float B, float A) border) => new()
    {
        Border = Sides.All(Length.Px(4f)),
        Corners = Corners.All(Length.Px(8f)),
        Justify = UiJustify.Center,
        Align = UiAlign.Center,
        Color = Background,
        BorderColor = border,
    };

    // A button that is its own label, as Bevy's text_button.
    private static Entity TextButton(EcsWorld ecs, string label, Action<BehaviorContext> action)
    {
        var button = Ui.SpawnText(label, new UiSettings
        {
            MinWidth = Length.Px(144f),
            Border = Sides.All(Length.Px(3f)),
            Corners = Corners.All(Length.Px(8f)),
            Sizing = BoxSizing.ContentBox,
            BorderColor = TileBorder,
        }, new UiTextSettings { FontSize = 24f, Justify = TextJustify.Center, LineHeight = 40f, LineHeightInPixels = true });
        // A text node's color is its text's, so its background is set apart.
        ecs.Insert<BackgroundColorRef>(button).Value = Background;
        Actions[button] = action;
        Listen(ecs, button);
        return button;
    }

    private static Entity Flag(EcsWorld ecs, float size)
    {
        var flag = Ui.SpawnNode(new UiSettings { Width = Length.Px(size), Height = Length.Px(size) });
        Ui.SetImage(flag, new UiImageSettings { Image = AssetServer.Load(AssetKind.Image, "textures/flag.png") });
        Ignore(ecs, flag);
        return flag;
    }

    // Neither hovered nor clicked, nor hiding what is under it, as Bevy's Pickable::IGNORE.
    private static void Ignore(EcsWorld ecs, Entity node)
    {
        var pickable = ecs.Insert<PickableRef>(node);
        (pickable.IsHoverable, pickable.ShouldBlockLower) = (false, false);
    }

    private static void NewGame(BehaviorContext ctx, Difficulty difficulty)
    {
        (_field, _gameOver) = (MineField.Of(difficulty), false);
        ctx.SetState(GameState.Playing);
    }

    // A button's or a covered tile's clicks and hovers, which Bevy's observers take for every entity
    // and look the entity up in its queries.
    private static void Listen(EcsWorld ecs, Entity node)
    {
        ecs.Observe<Pointer<Click>>(node, OnClick);
        ecs.Observe<Pointer<Over>>(node, on => Hover(on.Ecs, on.Entity, true));
        ecs.Observe<Pointer<Out>>(node, on => Hover(on.Ecs, on.Entity, false));
    }

    // A click on a button does what it says, and one on a covered tile reveals or flags it.
    private static void OnClick(On<Pointer<Click>> on)
    {
        var target = on.Entity;

        if (Actions.TryGetValue(target, out var action))
        {
            if (on.Event.Event.Button == PointerButton.Primary) action(on.Context);
            return;
        }

        if (_gameOver || _field.Cleared || !Tiles.TryGetValue(target, out var at)) return;

        switch (on.Event.Event.Button)
        {
            case PointerButton.Primary when _field.Mined(at.X, at.Y):
                _gameOver = true;
                break;
            case PointerButton.Primary:
                _field.Reveal(at.X, at.Y);
                break;
            case PointerButton.Secondary:
                _field.ToggleFlag(at.X, at.Y);
                break;
            default:
                return;
        }

        Rebuild(on.Ecs);
    }

    // A button's or a covered tile's border lights up under the pointer.
    private static void Hover(EcsWorld ecs, Entity target, bool over)
    {
        // The pointer leaves a button as the click on it takes it away, which hears of it gone.
        if (!ecs.IsAlive(target) || !Actions.ContainsKey(target) && !Tiles.ContainsKey(target)) return;

        var border = ecs.Wrap<BorderColorRef>(target);
        var color = over ? HoveredTileBorder : TileBorder;
        (border.Top, border.Bottom, border.Left, border.Right) = (color, color, color, color);
        if (Actions.ContainsKey(target)) ecs.Wrap<BackgroundColorRef>(target).Value = Background;
    }

    // The field, a tile at a time, as Bevy's MineField.
    private sealed class MineField
    {
        private readonly (bool Mined, bool Revealed, bool Flagged)[] _tiles;

        private MineField(int width, int height, int mines)
        {
            (Width, Height) = (width, height);
            _tiles = new (bool, bool, bool)[width * height];
            for (var i = 0; i < mines; i++) _tiles[i].Mined = true;
            Random.Shared.Shuffle(_tiles);
        }

        public int Width { get; }
        public int Height { get; }

        public int FlagCount => _tiles.Count(tile => tile.Flagged);
        public int MineCount => _tiles.Count(tile => tile.Mined);

        // Every mine flagged and every other tile revealed.
        public bool Cleared => _tiles.All(tile => tile.Revealed && !tile.Mined || tile.Mined && tile.Flagged);

        public static MineField Of(Difficulty difficulty) => difficulty switch
        {
            Difficulty.Easy => new MineField(15, 10, 15),
            Difficulty.Hard => new MineField(25, 15, 75),
            _ => new MineField(20, 15, 50),
        };

        public IEnumerable<(int X, int Y, (bool Mined, bool Revealed, bool Flagged) Tile)> All()
        {
            for (var y = 0; y < Height; y++)
                for (var x = 0; x < Width; x++)
                    yield return (x, y, _tiles[y * Width + x]);
        }

        public bool Mined(int x, int y) => _tiles[y * Width + x].Mined;

        public int AdjacentMines(int x, int y) => Adjacent(x, y).Count(at => Mined(at.X, at.Y));

        // Reveals a tile, and every tile around one with no mine beside it, as far as that goes.
        public void Reveal(int x, int y)
        {
            var open = new Stack<(int X, int Y)>([(x, y)]);
            while (open.TryPop(out var at))
            {
                ref var tile = ref _tiles[at.Y * Width + at.X];
                if (tile.Revealed) continue;

                tile.Revealed = true;
                if (AdjacentMines(at.X, at.Y) == 0)
                    foreach (var next in Adjacent(at.X, at.Y)) open.Push(next);
            }
        }

        // Takes a flag off, or puts one on while there are fewer flags than mines.
        public void ToggleFlag(int x, int y)
        {
            ref var tile = ref _tiles[y * Width + x];
            if (tile.Flagged) tile.Flagged = false;
            else if (FlagCount < MineCount) tile.Flagged = true;
        }

        private IEnumerable<(int X, int Y)> Adjacent(int x, int y)
        {
            foreach (var (dx, dy) in new[] { (1, 1), (1, 0), (1, -1), (0, 1), (0, -1), (-1, 1), (-1, 0), (-1, -1) })
            {
                var (ax, ay) = (x + dx, y + dy);
                if (ax >= 0 && ax < Width && ay >= 0 && ay < Height) yield return (ax, ay);
            }
        }
    }
}
