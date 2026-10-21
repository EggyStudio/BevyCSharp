using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the material slots, added, renamed and taken away with their faces given the first,
/// their looks given in the recipe's lines and made again from it, the materials made from them,
/// and a room drawn with a wall of blocks and a wooden floor from the examples' textures.
/// </summary>
[Collection("engine")]
public sealed class MaterialSlotTests
{
    private static int FaceFacing(EditMesh mesh, Vec3 way) => Enumerable.Range(0, mesh.FaceCount).First(face => (mesh.Normal(face) - way).Length < 1e-4f);

    [Fact]
    public void SlotsAreAddedRenamedAndTakenAwayTheFacesOfOneTakenAwayGivenTheFirst()
    {
        var mesh = new EditMesh().Box(new Vec3(2f));
        var (top, front) = (FaceFacing(mesh, Vec3.UnitY), FaceFacing(mesh, Vec3.UnitZ));

        mesh.SlotAdd("trim").SlotAdd("glass");
        mesh.SelectFaces([top]).Material("trim");
        mesh.SelectFaces([front]).Material("glass");
        Assert.Equal(new[] { "default", "trim", "glass" }, mesh.Slots);

        mesh.SlotRename("trim", "edge");
        Assert.Equal("edge", mesh.SlotOf(top));

        // Taken away, its faces go to the first slot and the slots after it move down by one, the
        // front still in its own.
        mesh.SlotRemove("edge");
        Assert.Equal(new[] { "default", "glass" }, mesh.Slots);
        Assert.Equal("default", mesh.SlotOf(top));
        Assert.Equal("glass", mesh.SlotOf(front));

        Assert.Throws<InvalidOperationException>(() => mesh.SlotRemove("default"));
        Assert.Throws<ArgumentException>(() => mesh.SlotAdd("glass"));
        Assert.Throws<ArgumentException>(() => mesh.SlotRename("glass", "default"));
        Assert.Throws<ArgumentException>(() => mesh.SlotRemove("absent"));

        Assert.Equal(
            new[] { "slot add trim", "slot add glass", "select face " + top, "material trim", "select face " + front, "material glass", "slot rename trim edge", "slot remove edge" },
            mesh.Recipe.Skip(1));
    }

    [Fact]
    public void ALookIsGivenByTheRecipesLinesAndMadeAgainFromThem()
    {
        var lines = new[]
        {
            "box 2 2 2",
            "slot color wall 0.8 0.3 0.2",
            "slot texture wall textures/wall.png",
            "slot normal-map wall textures/wall_normal.png",
            "slot occlusion-map wall textures/wall_occlusion.png",
            "slot metallic-roughness-map wall textures/wall_metal.png",
            "slot emissive wall 2 1 0",
            "slot emissive-map wall textures/wall_glow.png",
            "slot metallic wall 0.25",
            "slot roughness wall 0.75",
            "slot color glass 0.5 0.7 1 0.4",
            "select +y",
            "material wall",
        };

        var mesh = new EditMesh();
        foreach (var line in lines) mesh.Run(line);
        Assert.Equal(lines, mesh.Recipe);

        var wall = mesh.LookOf("wall");
        Assert.Equal(new Vec4(0.8f, 0.3f, 0.2f, 1f), wall.Color);
        Assert.Equal("textures/wall.png", wall.Texture);
        Assert.Equal("textures/wall_normal.png", wall.NormalMap);
        Assert.Equal("textures/wall_occlusion.png", wall.OcclusionMap);
        Assert.Equal("textures/wall_metal.png", wall.MetallicRoughnessMap);
        Assert.Equal(new Vec3(2f, 1f, 0f), wall.Emissive);
        Assert.Equal("textures/wall_glow.png", wall.EmissiveMap);
        Assert.Equal((0.25f, 0.75f), (wall.Metallic, wall.Roughness));
        Assert.Equal(0.4f, mesh.LookOf("glass").Color!.Value.W);
        Assert.True(mesh.LookOf("default").IsPrototype);

        // The recipe read again gives the same looks, and a texture taken away leaves the rest.
        var made = EditMesh.FromRecipe(mesh.RecipeText);
        Assert.Equal(wall, made.LookOf("wall"));
        Assert.Equal(mesh.Slots, made.Slots);
        Assert.Null(made.Run("slot texture wall none").LookOf("wall").Texture);
        Assert.Equal("textures/wall_normal.png", made.LookOf("wall").NormalMap);

        Assert.Throws<ArgumentOutOfRangeException>(() => mesh.Run("slot color wall 2 0 0"));
        Assert.Throws<ArgumentOutOfRangeException>(() => mesh.Run("slot roughness wall -1"));
        Assert.Throws<FormatException>(() => mesh.Run("slot paint wall"));
    }

    [Fact]
    public void AnUndoMakesTheFirstSlotDefaultAgainAndAJoinedModelBringsTheLookOfASlotThisOneLacks()
    {
        var mesh = new EditMesh().Box(new Vec3(1f)).SlotRename("default", "floor").SlotColor("floor", 1f, 0f, 0f);
        mesh.Undo();
        mesh.Undo();
        Assert.Equal("default", mesh.Slots[0]);
        Assert.True(mesh.LookOf("default").IsPrototype);

        using var folder = new TestFolder("bcs-slots-");
        var path = folder.File("pillar.model");
        new EditMesh().Box(new Vec3(1f)).SelectAll().Material("stone").SlotTexture("stone", "textures/stone.png").SlotColor("default", 0f, 1f, 0f).Save(path);

        // The pillar's stone comes with its look, and its default takes this mesh's, which has one.
        var hall = new EditMesh().Box(new Vec3(4f)).Join(path);
        Assert.Equal("textures/stone.png", hall.LookOf("stone").Texture);
        Assert.True(hall.LookOf("default").IsPrototype);
    }

    [SkippableFact]
    public void ASlotsMaterialIsItsNamesPrototypeUntilGivenALookAndOneLookMakesOneMaterial()
    {
        Needs.Renderer();

        AssetHandle[] materials = [];
        var (prototype, sameLook, otherLook) = (false, false, false);

        using var harness = new EngineHarness(frames: 1);
        harness.OnContext(Stage.Startup, _ =>
        {
            var mesh = new EditMesh().Box(new Vec3(1f)).SlotAdd("wall").SlotColor("red", 1f, 0f, 0f).SlotColor("rust", 1f, 0f, 0f).SlotColor("blue", 0f, 0f, 1f);
            materials = Modeling.Materials(mesh);
            prototype = materials[1] == Modeling.Prototype("wall") && materials[0] == Modeling.Prototype("default");
            sameLook = materials[2] == materials[3];
            otherLook = materials[4] != materials[2];
        });
        harness.Run();

        Assert.Equal(5, materials.Length);
        Assert.True(prototype, "a slot with no look is not drawn with its name's prototype");
        Assert.True(sameLook, "two slots of one look were given two materials");
        Assert.True(otherLook, "two looks were given one material");
    }

    /// <summary>
    /// A room made by its recipe, its walls a texture of blocks with its normal map and its floor
    /// wood, both from the examples' textures, drawn from inside under a lamp, the floor brown and
    /// the wall showing the blocks' colors.
    /// </summary>
    [SkippableFact]
    public void ARoomIsDrawnWithAWallOfBlocksAndAWoodenFloorFromTheExamplesTextures()
    {
        Needs.Renderer();
        var assets = Path.Combine(CheatsheetTests.RepositoryRoot(), "BevyCSharp.Examples", "bevy-assets");
        Skip.IfNot(File.Exists(Path.Combine(assets, "textures", "parallax_example", "cube_color.png")), "the examples' textures are not fetched, which build/fetch-bevy-assets.sh does");

        var room = EditMesh.FromRecipe("""
            model 1
            room 6 3 5 0.2
            select +y
            material floor
            slot texture floor models/MixedLightingExample/SheenChairWoodBaseColor.jpg
            select +z
            material wall
            slot texture wall textures/parallax_example/cube_color.png
            slot normal-map wall textures/parallax_example/cube_normal.png
            select +z
            uv density 640 640
            """);

        var run = new PictureRun
        {
            Width = 160,
            Height = 120,
            AssetRoot = assets,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d();
                ecs.Add(camera, Transform.LookingAt(new Vec3(3f, 1.4f, 4.6f), new Vec3(3f, 0.9f, 0.2f), Vec3.UnitY));

                var lamp = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 400_000f, Shadows = false });
                ecs.Add(lamp, Transform.At(3f, 2.4f, 3f));

                var meshes = Modeling.Meshes(room);
                var materials = Modeling.Materials(room);
                for (var slot = 0; slot < meshes.Length; slot++)
                {
                    if (meshes[slot] == AssetHandle.None) continue;
                    var part = ecs.Spawn();
                    ecs.Add(part, Transform.Identity);
                    Render.SetMesh(ecs, part, meshes[slot]);
                    Render.SetMaterial(ecs, part, materials[slot]);
                }
            },
        };

        // Time for both textures to load from disk before the picture.
        run.Wait(60).Capture("room").Go();
        var picture = run.Picture("room");

        // The floor, the bottom of the picture below the wall's foot at about row 81, brown, its
        // red well above its green and its green above its blue, where the floor's prototype gray
        // would have the three near one another.
        var (r, g, b, _) = Mean(picture, 40, 96, 120, 116);
        Assert.True(r > g + 30 && g > b + 15, $"the floor reads red {r}, green {g} and blue {b}");

        // The wall shows the blocks, whose purple ones have more blue than green, which neither
        // the wood nor the wall's warm gray prototype has anywhere.
        var purple = Count(picture, 0, 76, (red, green, blue) => blue > green);
        var underfoot = Count(picture, 88, 120, (red, green, blue) => blue > green);
        Assert.True(purple > 500, $"the wall shows {purple} pixels of more blue than green");
        Assert.True(underfoot < 20, $"the floor shows {underfoot} pixels of more blue than green");
    }

    /// <summary>How many pixels of some rows of a picture pass a test.</summary>
    private static int Count(CapturedImage picture, uint top, uint bottom, Func<byte, byte, byte, bool> test)
    {
        var count = 0;
        for (var y = top; y < bottom; y++)
        {
            for (var x = 0u; x < picture.Width; x++)
            {
                var (red, green, blue, _) = picture.At(x, y);
                if (test(red, green, blue)) count++;
            }
        }

        return count;
    }

    /// <summary>The mean color of a rectangle of a picture.</summary>
    private static (int R, int G, int B, int A) Mean(CapturedImage picture, uint left, uint top, uint right, uint bottom)
    {
        long r = 0, g = 0, b = 0, a = 0, count = 0;
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                var (pr, pg, pb, pa) = picture.At(x, y);
                (r, g, b, a, count) = (r + pr, g + pg, b + pb, a + pa, count + 1);
            }
        }

        return ((int)(r / count), (int)(g / count), (int)(b / count), (int)(a / count));
    }
}
