// Bevy's tilemap_chunk example, examples/2d/tilemap_chunk.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Shows a tilemap chunk rendered with a single draw call. A chunk of sixty-four by sixty-four tiles
// is drawn from the four layers of one image, a fifth of its cells empty, and fifty of its tiles
// change every tenth of a second. A red square runs along its bottom row and another stands still
// on a tile further in, placed by where the chunk says its tiles are.
//
// Bevy seeds a ChaCha generator with 42 so its tiles are the same each run. .NET's generator seeded
// with 42 is as fixed, with other tiles.
internal static class TilemapChunkExample
{
    private static readonly Color Red400 = Color.FromSrgb8(248, 113, 113);

    // Bevy's SeededRng resource, and the chunk Bevy's systems find as the one chunk there is.
    internal static Random Rng = new(42);
    internal static TilemapChunk Chunk = new();
    internal static Entity ChunkEntity;
    private static ushort _logged;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            Setup(ctx);
            SpawnFakePlayer(ctx);
        }, "tilemap_chunk.Setup");
        app.Update(LogTile, "tilemap_chunk.LogTile");
    }

    private static void Setup(BehaviorContext ctx)
    {
        Rng = new Random(42);
        _logged = 0;

        Chunk = new TilemapChunk
        {
            Width = 64,
            Height = 64,
            TileWidth = 8,
            TileHeight = 8,
            // The tileset is an array of tile textures, four stacked in the one image.
            Tileset = AssetServer.LoadImage("textures/array_texture.png", new TextureSettings { Layers = 4 }),
        };

        var tiles = new TileData?[Chunk.Count];
        for (var i = 0; i < tiles.Length; i++)
        {
            var index = Rng.Next(0, 5);
            tiles[i] = index == 0 ? null : TileData.FromTilesetIndex((ushort)(index - 1));
        }

        ChunkEntity = ctx.Ecs.Spawn();
        Render2d.SetTilemap(ctx.Ecs, ChunkEntity, Chunk, tiles);
        ctx.Ecs.Add(ChunkEntity, new UpdateTimer { Timer = GameTimer.FromSeconds(0.1f, TimerMode.Repeating) });

        Render2d.SpawnCamera2d();
    }

    private static void SpawnFakePlayer(BehaviorContext ctx)
    {
        var square = Render.CreateMesh(MeshShape.Rectangle, 8f, 8f);
        var material = Render2d.CreateMaterial(new ColorMaterialSettings { Color = Red400 });

        var origin = Chunk.TileTransform(0, 0).Translation.X;
        var destination = Chunk.TileTransform(63, 0).Translation.X;
        var player = Square(ctx.Ecs, square, material, 0, 0);
        ctx.Ecs.Add(player, new MovePlayer { Origin = origin, Destination = destination });

        // A second player, standing still, to show a tile's place away from the corner.
        Square(ctx.Ecs, square, material, 5, 6);
    }

    private static Entity Square(EcsWorld ecs, AssetHandle mesh, AssetHandle material, uint x, uint y)
    {
        var transform = Chunk.TileTransform(x, y);
        transform.Translation = transform.Translation with { Z = 1f };

        var entity = ecs.Spawn();
        ecs.Add(entity, transform);
        Render2d.SetMesh(ecs, entity, mesh);
        Render2d.SetMaterial(ecs, entity, material);
        return entity;
    }

    // Bevy's log_tile, which says so when the tile three across and four up changes its layer.
    private static void LogTile(BehaviorContext ctx)
    {
        if (ChunkEntity.IsNone || Render2d.TileAt(ctx.Ecs, ChunkEntity, Chunk.IndexOf(3, 4)) is not { } tile) return;
        if (tile.TilesetIndex == _logged) return;

        Console.WriteLine($"tile_data changed tile_data={tile}");
        _logged = tile.TilesetIndex;
    }
}

/// <summary>A chunk's timer, Bevy's <c>UpdateTimer</c>, which changes fifty of its tiles each time it runs out.</summary>
[Behavior]
public partial struct UpdateTimer
{
    /// <summary>A tenth of a second, repeating.</summary>
    public GameTimer Timer;

    /// <summary>Fifty tiles set to a random layer of the five Bevy picks among, as Bevy's <c>update_tilemap</c> sets them.</summary>
    [OnUpdate]
    public void UpdateTilemap(BehaviorContext ctx)
    {
        if (!Timer.Tick(ctx.Time.Delta).JustFinished) return;

        var rng = TilemapChunkExample.Rng;
        for (var i = 0; i < 50; i++)
        {
            var index = rng.Next(0, TilemapChunkExample.Chunk.Count);
            Render2d.SetTiles(ctx.Ecs, ctx.Entity, index, [TileData.FromTilesetIndex((ushort)rng.Next(0, 5))]);
        }
    }
}

/// <summary>The player that runs along the chunk's bottom row, Bevy's <c>MovePlayer</c>.</summary>
[Behavior]
public partial struct MovePlayer
{
    /// <summary>Where the row's first tile is across.</summary>
    public float Origin;

    /// <summary>Where its last tile is.</summary>
    public float Destination;

    /// <summary>Back and forth between the two with the sine of the time, as Bevy's <c>move_player</c> moves it.</summary>
    [OnUpdate]
    public readonly void Move(BehaviorContext ctx, ref Transform transform)
    {
        var t = (MathF.Sin(ctx.Time.Elapsed) + 1f) / 2f;
        transform.Translation = transform.Translation with { X = Origin + (Destination - Origin) * t };
    }
}
