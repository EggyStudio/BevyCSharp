# Assets and models

Loading what a game is made of, from its folder, its assembly or a pack, and placing the models a
glTF file holds.

## Assets

```csharp
var mesh = AssetServer.Load(AssetKind.Mesh, "models/ship.gltf");

if (mesh.IsLoaded) { }
AssetServer.Release(mesh);
```

Loading is asynchronous, so `Load` returns as soon as the request is queued and the handle
reports `Loading` until the file has been read.

Paths resolve against `Config.AssetRoot`, and it is worth setting. Left unset, Bevy looks for an
`assets` directory beside the running executable, which for a .NET app is whichever host launched
it, so under `dotnet test` or `dotnet exec` that is the host rather than the assembly and assets
copied next to the DLL are not found. Naming the directory outright is the only way to be sure:

```csharp
AssetRoot = Path.Combine(AppContext.BaseDirectory, "assets")
```

A game can carry its assets inside its own assembly instead, built with
`-p:BevyCSharpEmbedAssets=true`, which compiles the asset folder in as resources named under
`assets/`, apart from the scripts and shaders the game compiles from their files. A file is then read from the folder first and from the assembly when the folder has
none, by Bevy and by the managed side's own reads alike, since the app hands the bridge a reader
over the same resources as it starts. The assembly looked in is the entry one, or
`Config.AssetAssembly` where something else starts the process, such as a test runner.

A game too large for that ships a pack, one file of its assets that is read a part at a time:

```csharp
AssetPack.Write("assets", "bin/Export/linux-x64/assets.pack", file => !file.EndsWith(".cs"));
```

An app reads `assets.pack` beside its executable, or the one `Config.AssetPack` names, after the
folder and before the assembly, on both sides of the bridge.

Streaming is the other way to read. It reads parts of large files, a piece at a time, while the game
runs, and texture and geometry streaming read their tiles and clusters with it.

```csharp
var tile = Streaming.Read("world.pages", offset: page * PageBytes, length: PageBytes, priority: onScreen ? 1 : 0);

// Every frame, until it arrives:
if (Streaming.TryTake(tile, out var bytes)) Shaders.WriteImage<byte>(cache, bytes, x, y, 128, 128);
```

Reads run on the thread pool, relative to the asset directory, and `TryTake` hands finished ones
over until `Streaming.BytesPerFrame` bytes have gone out this frame, so a burst of reads finishing at
once becomes uploads spread over a few frames rather than a hitch; the first read of a frame is
always handed over, however large. A finished read of higher priority goes first, and a file that
could not be read throws from `TryTake`.

A loaded mesh or image is changed in place as Bevy changes one, so everything drawn with it
changes too. `Render.TryReadMesh` and `Render.WriteMesh` read a mesh's triangles and write new ones
over the same handle, and `Render.TryReadImage` and `Render.WriteImagePixels` read an image's
texels, row after row, and write them back:

```csharp
if (Render.TryReadImage(portrait, out var pixels))
    Render.WriteImagePixels(portrait, pixels!.Data.Select(b => (byte)(255 - b)).ToArray());   // inverted
```

An image reads only while the app keeps a copy of its texels, which a loaded picture and one made
with `Render.CreateImage` do, and a compressed one is refused, since its texels are blocks.

## Models

A glTF file holds many assets, so one is named with a label after the path. `LoadGltfMesh` builds
that label, and what comes back is an ordinary mesh handle:

```csharp
var hull = AssetServer.LoadGltfMesh("models/ship.gltf");        // mesh 0, primitive 0
Render.SetMesh(ctx.Ecs, entity, hull);
Render.SetMaterial(ctx.Ecs, entity, Render.CreateMaterial(0.6f, 0.6f, 0.62f));
```

A glTF mesh is a named group and it is the primitive inside it that carries geometry, which is
why both indices exist; a file exported as one object is mesh 0, primitive 0. Needs a render
build, since the loader comes with the renderer.

A file's own arrangement of its meshes is a scene, and spawning one produces the entities the
artist laid out:

```csharp
var root = ctx.Ecs.SpawnScene(AssetServer.LoadGltfScene("models/ship.gltf"));
```

The root comes back at once and fills in when the asset has loaded, so no children on the first
frame is normal rather than a failure. Wait for the `WorldInstanceReady` message naming the root,
posted once the entities are in the world, not for the `WorldInstance` component, which marks the
spawn as done but can appear a frame before the entities are visible.

`App.SpawnGltf` does all of that for a scene spawned as the app starts, and hands the root over
once the scene is in, where `EcsWorld.Descendants` walks it nearer entities first:

```csharp
app.SpawnGltf("models/ship.gltf", (ctx, root) =>
{
    foreach (var part in ctx.Ecs.Descendants(root))
        if (ctx.Ecs.NameOf(part) == "Sail") Render.SetMaterial(ctx.Ecs, part, canvas);
});
```

A mesh, a material and a place make one entity in one call, as Bevy's bundle of the three does:

```csharp
ctx.Ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid), Render.CreateMaterial(Color.FromSrgb8(124, 144, 255)), Transform.At(0f, 0.5f, 0f));
```

Compose on top of what a file describes by patching it after it spawns. Bevy's own `bsn!` does the
same at compile time in Rust, and the ECS surface here does it at runtime:

```csharp
foreach (var child in ctx.Ecs.ChildrenOf(root))
{
    ctx.Ecs.Add(child, Transform.At(3f, 0f, 0f));   // override what the artist set
    ctx.Ecs.Add(child, new Selectable());           // add what the file knows nothing about
}
```

`Add` replaces a component whole, which is right for one the game owns and wrong for one an artist
part-filled in. `Patch` changes the fields it names and leaves the rest, and `PatchTree` does it to
an entity and everything under it, which usually suits a model:

```csharp
// Keep where the artist put each part, and halve how large the whole model is.
ctx.Ecs.PatchTree<Transform>(root, (ref Transform t) => t.Scale = Vec3.One * 0.5f);
```

That is the same field-by-field merge Bevy's `bsn!` does between two scenes, done at runtime rather
than at compile time.

`.scn` and `.scn.ron` worlds load as the same asset through `AssetKind.Scene`, so `SpawnScene`
takes either.

A file's animation clips play on the entity its scene was spawned under, by the names the file
gives them, a skinned character's bones and a propeller's spin alike:

```csharp
var fox = ctx.Ecs.SpawnScene(AssetServer.LoadGltfScene("models/fox.glb"));

// A few frames later, once the model has arrived:
if (Animation.TryClips(fox, out var clips))
    Animation.Play(fox, "Walk", new AnimationSettings { Repeat = true });

// And a run, faded in over a fifth of a second:
Animation.Play(fox, "Run", new AnimationSettings { Repeat = true, Speed = 1.2f, Blend = 0.2f });

foreach (var ended in ctx.Read<AnimationFinished>())
    Console.WriteLine($"{ended.Clip} ended on {ended.Scene}");
```

`TryClips` and `Play` answer false while the model is still loading, and asking again next frame
is the protocol. Clips are listed in the file's order, one the file left unnamed as Bevy labels it
(`Animation1`), and a file animating two separate things plays a clip on both. `Pause`, `Resume`,
`Seek`, `SetSpeed` and `Stop` act on what plays, and `StateOf` reads it back. `Times` plays a clip a
number of times before it holds its last pose, and `SetRepeat` changes that while it plays without
starting it over, zero being for ever. A clip that does not repeat posts `AnimationFinished` when
it reaches its end. The
console's `anim.clips`, `anim.play` and `anim.stop` do the same from the editor or `bcs`. One clip
plays at a time, fading from the last, so masks, additive layers and a state machine over clips
are left to the game.

A clip is also made in code, as Bevy's examples make one, a curve for each property it moves,
aimed at an entity by the names on the path down to it. A curve is values sampled at times, or two
values eased between by one of Bevy's easing functions, and moves a transform's translation,
rotation or scale, an interface node's scale or rotation, or a text's color. A player plays a graph
made from the clip, and each entity a curve is aimed at carries its target and the player:

```csharp
var clip = Animation.CreateClip();
var door = AnimationTarget.FromNames("door");
Animation.AddCurve(clip, door, AnimationCurve.Rotation(Quat.Identity, Quat.FromRotationY(1.5f), EaseFunction.BackOut, 0.6f));
var (graph, node) = Animation.GraphFromClip(clip);
Animation.PlayGraph(doorEntity, graph, node);
Animation.Animate(doorEntity, door, player: doorEntity);
```

A clip made once moves every set of entities named the same way, as a model's clip moves every
copy of the model.

A file's own materials load too, in a windowed run:

```csharp
Render.SetMaterial(ctx.Ecs, entity, AssetServer.LoadGltfMaterial("models/ship.gltf"));
```

A glTF material loads as a `GltfMaterial`, which describes a material rather than being one the
renderer draws with, and the translation between them belongs to the renderer. A windowless run
has no renderer and nothing to draw, so it has no translated material either; use
`CreateMaterial` if a run without a window needs one at all.

Bevy's own handle is generic and reference counted, and neither property survives a trip through
a C ABI, so C# holds a key into a table on the engine side that owns the real handle. Holding one
keeps the asset loaded; `Release` gives up that reference. The key carries a generation as well
as a slot index, so a released handle does not start naming whatever later took its slot. It
names nothing instead, and every call that takes one refuses it rather than carrying on without
whatever it pointed at.

`Mesh` and `Image` load in any build. `StandardMaterial`, `Gltf`, `Audio` and `Font` need a render
build, and asking for one without it reports which build would support it. Scenes load too. `Scene`
is a trait in 0.19 and the loadable asset behind `.scn`, `.scn.ron` and a glTF file's scenes is
`WorldAsset`, which `ctx.Ecs.SpawnScene` spawns.

---

Before this, [Scenes and saves](scenes-and-saves.md).
Next, [Drawing](drawing.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#assets). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#assets-and-models). The [guide's contents](../README.md#guide) list every page.
