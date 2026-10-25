using System.Runtime.CompilerServices;

namespace Bevy;

// The pictures of a variant's far cards, taken once an app by a camera of their own: each card's
// stems and leaves stood in a cell of a grid far under the world, turned to face the camera and
// scaled to fill the cell, drawn unlit into an atlas, read back, and given their smaller levels here
// with each card's leaves kept in them, as the leaf sets' levels keep theirs.
public static partial class Trees
{
    // The render layer only the camera taking the pictures sees, so the scene's cameras, its lights'
    // shadows and its probes never see the branches stood for it.
    private const int CardLayer = 30;

    // The atlas a variant's cards are pictured into, texels a side. The far level draws a tree some
    // tens of pixels tall, a card a share of that, so a cell of a hundred texels or more is finer
    // than the picture it is drawn into.
    private const int CardAtlasSize = 512;

    // Where the branches stand to be pictured, a grid of a unit a cell far under the world, and how
    // far apart two variants' grids stand when pictured at once.
    private static readonly Vec3 CardStage = new(0f, -20_000f, 0f);
    private const float CardStageApart = 64f;

    // How many frames a variant's pictures may take before the tree is dressed without its far
    // level, with a warning in the log.
    private const int CardPicturesLongest = 900;

    private static readonly ConditionalWeakTable<App, Dictionary<(TreeSpecies, int), CardPictures>> TakenPictures = new();
    private static readonly ConditionalWeakTable<App, StrongBox<long>> Ticks = new();

    private enum CardStep { Waiting, Capturing, Leveling, Done, Failed }

    /// <summary>A variant's pictures as they are taken, and what the far level is drawn with once they are.</summary>
    private sealed class CardPictures
    {
        public CardStep Step;
        public int Frames;
        public long Advanced = -1;
        public Entity Camera = Entity.None;
        public AssetHandle Target = AssetHandle.None;
        public readonly List<Entity> Stood = [];
        public readonly List<AssetHandle> Awaited = [];
        public Capture Ticket;
        public Task<(byte[] Colors, byte[] Normals, uint Levels)>? Levels;

        public AssetHandle Limbs = AssetHandle.None;
        public AssetHandle Quads = AssetHandle.None;
        public AssetHandle Material = AssetHandle.None;

        /// <summary>Whether the far level is drawn, its pictures taken.</summary>
        public bool Drawn => Step == CardStep.Done;
    }

    /// <summary>
    /// Takes a step toward a variant's pictures, at most one a frame, and answers them once they are
    /// taken or have failed, or nothing while they are being taken.
    /// </summary>
    private static CardPictures? Pictured(EcsWorld ecs, App app, TreeSpecies species, int variant, Grown grown)
    {
        var all = TakenPictures.GetOrCreateValue(app);
        if (!all.TryGetValue((species, variant), out var pictures))
        {
            all[(species, variant)] = pictures = new CardPictures();
            if (grown.Cards is null) pictures.Step = CardStep.Failed;
            else Stand(ecs, pictures, grown.Cards, species, all.Count - 1);
        }

        var tick = Ticks.GetOrCreateValue(app).Value;
        if (pictures.Step is CardStep.Done or CardStep.Failed || pictures.Advanced == tick) return pictures.Step is CardStep.Done or CardStep.Failed ? pictures : null;
        pictures.Advanced = tick;
        pictures.Frames++;

        if (pictures.Frames > CardPicturesLongest)
        {
            Log.Warn($"The far cards of a tree's variant {variant}, its leaves {species.LeafSet ?? "generated"}, were not pictured in {CardPicturesLongest} frames, and its trees are drawn without them.");
            Unstand(ecs, pictures);
            pictures.Step = CardStep.Failed;
            return pictures;
        }

        switch (pictures.Step)
        {
            // Pictured once what they are drawn with has loaded and every pipeline is made, a few
            // frames after the branches are stood, so the camera has seen them.
            case CardStep.Waiting:
                if (pictures.Frames >= 4 && Render.PipelinesReady()
                    && pictures.Awaited.All(image => AssetServer.StateOf(image) is AssetLoadState.Loaded or AssetLoadState.Failed))
                {
                    pictures.Ticket = Render.BeginCapture(pictures.Target);
                    pictures.Step = CardStep.Capturing;
                }

                break;

            case CardStep.Capturing:
                if (Render.TryReadCapture(pictures.Ticket, out var picture))
                {
                    Unstand(ecs, pictures);
                    var plan = grown.Cards!;
                    pictures.Levels = Task.Run(() => CardLevels(picture!.Pixels, plan));
                    pictures.Step = CardStep.Leveling;
                }

                break;

            case CardStep.Leveling when pictures.Levels!.IsCompleted:
                if (pictures.Levels.IsFaulted)
                {
                    EngineLog.Error(app, "trees", $"[BevyCSharp] The far cards' levels were not made. {pictures.Levels.Exception?.InnerException?.Message}", pictures.Levels.Exception);
                    pictures.Step = CardStep.Failed;
                    break;
                }

                var (colors, normals, levels) = pictures.Levels.Result;
                var atlas = Render.CreateImageLevels(colors, CardAtlasSize, CardAtlasSize, srgb: true, levels);
                var bumps = Render.CreateImageLevels(normals, CardAtlasSize, CardAtlasSize, srgb: false, levels);
                foreach (var image in new[] { atlas, bumps })
                {
                    Render.SetSampler(image, new TextureSettings
                    {
                        Wrap = TextureWrap.Clamp,
                        MagFilter = TextureFilter.Linear,
                        MinFilter = TextureFilter.Linear,
                        MipmapFilter = TextureFilter.Linear,
                        Anisotropy = 4,
                        Srgb = false,
                    });
                }

                // Lit as the leaves are on average, their set's mean occlusion and translucency,
                // and by the normals of the leaves each card pictures. A texel of a card stands for
                // many leaves turned every way, whose highlights spread as one far rougher surface's,
                // so the card is fully rough and reflects as little as a leaf's waxed skin does.
                var surface = species.LeafSet is { } set && TreeTextures.Find(set) is { Kind: TreeTextureKind.Leaves } found
                    ? found.MeanSurface
                    : (Occlusion: 1f, Roughness: 0.6f, Metallic: 0f, Translucency: 0.4f);
                pictures.Material = Render.CreateLeafMaterial(new MaterialSettings
                {
                    BaseColor = (surface.Occlusion, surface.Occlusion, surface.Occlusion, 1f),
                    BaseColorTexture = atlas,
                    NormalMap = bumps,
                    DiffuseTransmission = surface.Translucency,
                    Roughness = 1f,
                    Reflectance = 0.3f,
                    AlphaMode = AlphaMode.Mask,
                    DoubleSided = true,
                });
                pictures.Limbs = Created(grown.Cards!.Limbs);
                pictures.Quads = Created(grown.Cards.Quads);
                pictures.Step = CardStep.Done;
                return pictures;
        }

        return pictures.Step == CardStep.Failed ? pictures : null;
    }

    /// <summary>
    /// Stands a variant's cards' stems and leaves in a grid of cells far under the world, each
    /// turned so its card faces the camera and scaled to fill its cell, drawn unlit so the pictures
    /// hold their colors and the far level is lit as it is drawn, and spawns the camera that
    /// pictures them into the atlas.
    /// </summary>
    private static void Stand(EcsWorld ecs, CardPictures pictures, CardPlan plan, TreeSpecies species, int slot)
    {
        var (leafImage, leafTint, barkImage, barkTint) = CardLooks(species, pictures.Awaited);
        var leaves = Render.CreateMaterial(new MaterialSettings
        {
            BaseColor = leafTint,
            BaseColorTexture = leafImage,
            Unlit = true,
            AlphaMode = AlphaMode.Mask,
            DoubleSided = true,
        });
        var bark = Render.CreateMaterial(new MaterialSettings { BaseColor = barkTint, BaseColorTexture = barkImage, Unlit = true });

        var middle = CardStage + new Vec3(slot * CardStageApart, 0f, 0f);
        var half = plan.Across * 0.5f;
        foreach (var card in plan.Cards)
        {
            // The card's right, up and face onto the cell's, and its middle onto the cell's.
            var cell = middle + new Vec3(card.Column + 0.5f - half, half - card.Row - 0.5f, 0f);
            var turn = Quat.FromBasis(card.Right, card.Up, card.Face).Conjugate;
            var place = cell - turn * card.Middle * card.Scale;
            foreach (var (data, material) in new[] { (card.Stems, bark), (card.Leaves, leaves) })
            {
                if (data.Positions.Length == 0) continue;
                var part = ecs.Spawn();
                ecs.Add(part, new Transform(place, turn, new Vec3(card.Scale)));
                Render.SetMesh(ecs, part, Render.CreateMesh(data));
                Render.SetMaterial(ecs, part, material);
                Render.SetLayers(ecs, part, 1u << CardLayer);
                Render.SetMeshFlags(ecs, part, MeshFlags.NoShadowCasting);
                pictures.Stood.Add(part);
            }
        }

        pictures.Target = Render.CreateTarget(CardAtlasSize, CardAtlasSize);
        pictures.Camera = Render.SpawnCamera3d(new CameraSettings
        {
            Projection = CameraProjection.Orthographic,
            Height = plan.Across,
            Clear = ClearMode.Custom,
            ClearColor = (0f, 0f, 0f, 0f),
            Order = -50,
            Layers = 1u << CardLayer,
        });
        ecs.Add(pictures.Camera, Transform.LookingAt(middle + new Vec3(0f, 0f, 10f), middle, Vec3.UnitY));
        Render.SetCameraTarget(pictures.Camera, pictures.Target);
        Render.SetPostProcessing(pictures.Camera, new PostSettings { Hdr = false, Msaa = 1, Tonemapper = Tonemapper.None, Dither = false });
    }

    /// <summary>Despawns what was stood for a variant's pictures and their camera.</summary>
    private static void Unstand(EcsWorld ecs, CardPictures pictures)
    {
        foreach (var part in pictures.Stood.Append(pictures.Camera).Where(ecs.IsAlive)) ecs.Despawn(part);
        pictures.Stood.Clear();
        pictures.Camera = Entity.None;
    }

    /// <summary>
    /// The pictures and tints a species' leaves and bark are pictured with, its sets' where the
    /// collection has them and the generated ones otherwise, noting the images still loading.
    /// </summary>
    private static (AssetHandle Leaves, (float, float, float, float) LeafTint, AssetHandle Bark, (float, float, float, float) BarkTint) CardLooks(TreeSpecies species, List<AssetHandle> awaited)
    {
        AssetHandle leaves, bark;
        (float, float, float, float) leafTint = (1f, 1f, 1f, 1f), barkTint = (1f, 1f, 1f, 1f);
        if (species.LeafSet is { } leafSet && TreeTextures.Find(leafSet) is { Kind: TreeTextureKind.Leaves } leafFound)
        {
            leaves = leafFound.Load().Albedo;
            awaited.Add(leaves);
        }
        else
        {
            leaves = LeafPictures(species);
            leafTint = (species.LeafColor.R, species.LeafColor.G, species.LeafColor.B, 1f);
        }

        if (species.BarkSet is { } barkSet && TreeTextures.Find(barkSet) is { Kind: TreeTextureKind.Bark } barkFound)
        {
            bark = barkFound.Load().Albedo;
            awaited.Add(bark);
        }
        else
        {
            bark = BarkPictures().Color;
            barkTint = (species.BarkColor.R, species.BarkColor.G, species.BarkColor.B, 1f);
        }

        return (leaves, leafTint, bark, barkTint);
    }

    /// <summary>
    /// The atlas's levels from its picture, the color under its clear texels taken from the
    /// pictured ones nearest so no dark rim shows as it shrinks, each level the mean of the four
    /// above it as light, and each level's opacity scaled so the share of each card's rectangle
    /// past one half, read between the level's texels as the far level samples it, stays the full
    /// size's on average, as the leaf sets' levels keep their leaves.
    /// </summary>
    private static (byte[] Colors, byte[] Normals, uint Levels) CardLevels(byte[] pixels, CardPlan plan)
    {
        const int size = CardAtlasSize;
        var bled = Bled(pixels, size);
        var chain = Render.MipChain(bled, size, size, srgb: true, out var levels);

        // Each card's rectangle in the full size's texels.
        var rectangles = plan.Cards.Select(card =>
        {
            var (left, top) = CellPlace(card, -card.HalfWidth, card.HalfHeight, plan.Across);
            var (right, bottom) = CellPlace(card, card.HalfWidth, -card.HalfHeight, plan.Across);
            return (Left: left * size, Top: top * size, Right: right * size, Bottom: bottom * size);
        }).ToArray();

        var offset = 0L;
        var target = 0f;
        for (var level = 0; level < levels; level++)
        {
            var width = Math.Max(1, size >> level);
            var alpha = new float[width * width];
            for (var i = 0; i < alpha.Length; i++) alpha[i] = chain[offset + i * 4 + 3] / 255f;

            if (level == 0) target = SampledShare(alpha, width, rectangles, 1f);
            else
            {
                var (low, high) = (0.25f, 16f);
                for (var step = 0; step < 18; step++)
                {
                    var middle = (low + high) / 2f;
                    if (SampledShare(alpha, width, rectangles, middle) < target) low = middle;
                    else high = middle;
                }

                for (var i = 0; i < alpha.Length; i++) chain[offset + i * 4 + 3] = (byte)MathF.Min(255f, MathF.Round(alpha[i] * high * 255f));
            }

            offset += (long)width * width * 4;
        }

        return (chain, Render.MipChain(CardNormals(plan), size, size, srgb: false, out _), levels);
    }

    /// <summary>
    /// The normals of what each card pictures, its stems' and leaves' triangles drawn here in its
    /// cell as the camera draws them, the nearest one's normal kept at each texel, in the card's own
    /// right, up and face, turned to the face where it faced away, as a picture's back face is seen
    /// turned round, and folded into a texel as a normal map holds it. A texel nothing covers faces
    /// straight out of the card.
    /// </summary>
    /// <remarks>
    /// The leaf sets' opacity is the GPU's alone, so the normals are drawn whole for each card, and
    /// a texel the leaves cover only at a cut corner takes their normal while the color picture
    /// leaves it clear, which the color's own opacity then cuts away as it draws.
    /// </remarks>
    private static byte[] CardNormals(CardPlan plan)
    {
        const int size = CardAtlasSize;
        var normals = new byte[size * size * 4];
        for (var i = 0; i < size * size; i++) (normals[i * 4], normals[i * 4 + 1], normals[i * 4 + 2], normals[i * 4 + 3]) = ((byte)128, (byte)128, (byte)255, (byte)255);
        var nearest = new float[size * size];
        Array.Fill(nearest, float.NegativeInfinity);

        foreach (var card in plan.Cards)
        {
            // The card's cell, which nothing it pictures is drawn past.
            var cell = size / (float)plan.Across;
            var (cellLeft, cellTop) = ((int)(card.Column * cell), (int)(card.Row * cell));
            var (cellRight, cellBottom) = ((int)((card.Column + 1) * cell), (int)((card.Row + 1) * cell));

            foreach (var mesh in new[] { card.Stems, card.Leaves })
            {
                if (mesh.Positions.Length == 0 || mesh.Normals is null) continue;
                var points = mesh.Positions.Select(place =>
                {
                    var away = place - card.Middle;
                    var (u, v) = CellPlace(card, Vec3.Dot(away, card.Right), Vec3.Dot(away, card.Up), plan.Across);
                    return (X: u * size, Y: v * size, Z: Vec3.Dot(away, card.Face));
                }).ToArray();
                var corners = mesh.Indices ?? [.. Enumerable.Range(0, points.Length).Select(i => (uint)i)];
                for (var t = 0; t + 2 < corners.Length; t += 3)
                {
                    var (a, b, c) = ((int)corners[t], (int)corners[t + 1], (int)corners[t + 2]);
                    var (pa, pb, pc) = (points[a], points[b], points[c]);
                    var area = (pb.X - pa.X) * (pc.Y - pa.Y) - (pc.X - pa.X) * (pb.Y - pa.Y);
                    if (MathF.Abs(area) < 1e-8f) continue;
                    var normal = mesh.Normals[a] + mesh.Normals[b] + mesh.Normals[c];
                    var inCard = new Vec3(Vec3.Dot(normal, card.Right), Vec3.Dot(normal, card.Up), Vec3.Dot(normal, card.Face));
                    if (inCard.LengthSquared < 1e-12f) continue;
                    inCard = inCard.Normalized;
                    if (inCard.Z < 0f) inCard = -inCard;

                    var x0 = Math.Max(cellLeft, (int)MathF.Floor(MathF.Min(pa.X, MathF.Min(pb.X, pc.X))));
                    var x1 = Math.Min(cellRight - 1, (int)MathF.Ceiling(MathF.Max(pa.X, MathF.Max(pb.X, pc.X))));
                    var y0 = Math.Max(cellTop, (int)MathF.Floor(MathF.Min(pa.Y, MathF.Min(pb.Y, pc.Y))));
                    var y1 = Math.Min(cellBottom - 1, (int)MathF.Ceiling(MathF.Max(pa.Y, MathF.Max(pb.Y, pc.Y))));
                    for (var y = y0; y <= y1; y++)
                    for (var x = x0; x <= x1; x++)
                    {
                        var (px, py) = (x + 0.5f, y + 0.5f);
                        var wa = ((pb.X - px) * (pc.Y - py) - (pc.X - px) * (pb.Y - py)) / area;
                        var wb = ((pc.X - px) * (pa.Y - py) - (pa.X - px) * (pc.Y - py)) / area;
                        var wc = 1f - wa - wb;
                        if (wa < 0f || wb < 0f || wc < 0f) continue;
                        var depth = wa * pa.Z + wb * pb.Z + wc * pc.Z;
                        var at = y * size + x;
                        if (depth <= nearest[at]) continue;
                        nearest[at] = depth;
                        normals[at * 4] = (byte)MathF.Round((inCard.X * 0.5f + 0.5f) * 255f);
                        normals[at * 4 + 1] = (byte)MathF.Round((inCard.Y * 0.5f + 0.5f) * 255f);
                        normals[at * 4 + 2] = (byte)MathF.Round((inCard.Z * 0.5f + 0.5f) * 255f);
                    }
                }
            }
        }

        return normals;
    }

    /// <summary>
    /// The cards' mean share of their rectangles past one half, a level's opacity scaled and read
    /// between its texels on a grid over each rectangle as the far level samples it.
    /// </summary>
    private static float SampledShare(float[] alpha, int width, IReadOnlyList<(float Left, float Top, float Right, float Bottom)> rectangles, float scale)
    {
        var shares = 0f;
        var toLevel = width / (float)CardAtlasSize;
        foreach (var (left, top, right, bottom) in rectangles)
        {
            var (across, down) = (Math.Clamp((int)(right - left), 1, 24), Math.Clamp((int)(bottom - top), 1, 24));
            var past = 0;
            for (var j = 0; j < down; j++)
            for (var i = 0; i < across; i++)
            {
                var x = (left + (i + 0.5f) * (right - left) / across) * toLevel - 0.5f;
                var y = (top + (j + 0.5f) * (bottom - top) / down) * toLevel - 0.5f;
                if (Between(alpha, width, x, y) * scale >= 0.5f) past++;
            }

            shares += past / (float)(across * down);
        }

        return rectangles.Count > 0 ? shares / rectangles.Count : 0f;
    }

    /// <summary>A level's opacity read between its four nearest texels, its edges held.</summary>
    private static float Between(float[] alpha, int width, float x, float y)
    {
        var (x0, y0) = ((int)MathF.Floor(x), (int)MathF.Floor(y));
        var (fx, fy) = (x - x0, y - y0);
        float At(int i, int j) => alpha[Math.Clamp(j, 0, width - 1) * width + Math.Clamp(i, 0, width - 1)];
        return (At(x0, y0) * (1f - fx) + At(x0 + 1, y0) * fx) * (1f - fy) + (At(x0, y0 + 1) * (1f - fx) + At(x0 + 1, y0 + 1) * fx) * fy;
    }

    /// <summary>
    /// The picture with each clear texel's color the mean of its pictured neighbors', spread out a
    /// texel a pass from the pictured ones, and the rest the mean of every pictured texel.
    /// </summary>
    private static byte[] Bled(byte[] pixels, int size)
    {
        var bled = (byte[])pixels.Clone();
        var filled = new bool[size * size];
        var (sumR, sumG, sumB, count) = (0L, 0L, 0L, 0L);
        for (var i = 0; i < filled.Length; i++)
        {
            filled[i] = pixels[i * 4 + 3] >= 128;
            if (!filled[i]) continue;
            (sumR, sumG, sumB, count) = (sumR + pixels[i * 4], sumG + pixels[i * 4 + 1], sumB + pixels[i * 4 + 2], count + 1);
        }

        for (var pass = 0; pass < 8; pass++)
        {
            var next = (bool[])filled.Clone();
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var i = y * size + x;
                if (filled[i]) continue;
                var (r, g, b, n) = (0, 0, 0, 0);
                foreach (var (dx, dy) in (ReadOnlySpan<(int, int)>)[(-1, 0), (1, 0), (0, -1), (0, 1)])
                {
                    var (nx, ny) = (x + dx, y + dy);
                    if (nx < 0 || ny < 0 || nx >= size || ny >= size || !filled[ny * size + nx]) continue;
                    var at = (ny * size + nx) * 4;
                    (r, g, b, n) = (r + bled[at], g + bled[at + 1], b + bled[at + 2], n + 1);
                }

                if (n == 0) continue;
                (bled[i * 4], bled[i * 4 + 1], bled[i * 4 + 2]) = ((byte)(r / n), (byte)(g / n), (byte)(b / n));
                next[i] = true;
            }

            filled = next;
        }

        if (count > 0)
        {
            for (var i = 0; i < filled.Length; i++)
            {
                if (filled[i]) continue;
                (bled[i * 4], bled[i * 4 + 1], bled[i * 4 + 2]) = ((byte)(sumR / count), (byte)(sumG / count), (byte)(sumB / count));
            }
        }

        return bled;
    }
}
