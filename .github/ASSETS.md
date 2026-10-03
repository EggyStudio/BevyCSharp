# Seeing and picking meshes, materials and textures

How the editor shows what an entity is drawn with, how a mesh or a material is picked, what the
asset browser's tiles show, and where a mesh or material made in place lives. What the bridge reads
back, the preview renderer, the two cards and the picker (§1, §2, §4), thumbnails (§3), and a
scene's resources, material and mesh files and Make unique (§5) are built, and the rest of this file is the
design, in the order it can be built. [COMPONENTS.md](COMPONENTS.md) covers
components in general and [SCENES.md](SCENES.md) how all of it is saved.

## What exists

- **"Drawn with"** (`BevyCSharp.Editor/Framework/EditorDrawn.cs`) is a section under the components
  with a Mesh row and a Material row. Each is a button naming the file, or "made here", that opens
  `PickerWindow` with three groups (§4): Bevy's primitives or a new material, what the scene already
  draws with, and the model files, searched and picked with the keyboard, and a pick is a step to
  undo. Under each row is its card (§1).
- **Thirteen of Bevy's primitives** (`MeshShape`: cuboid, sphere, plane, capsule, cylinder, cone,
  conical frustum, torus, circle, annulus, rectangle, triangle, tetrahedron) are made with
  `Render.CreateMesh`, which keeps the shape and measures beside the handle (`Render.RecipeOf`),
  and a material with `Render.CreateMaterial(MaterialSettings)`.
- **What comes back.** `Render.TryGetMeshInfo` gives a mesh's vertex and index counts, index width,
  topology, attributes and bounds without copying its vertices, `Render.TryReadMaterial` a standard
  material's settings whoever made it, `Render.MeshOf` and `MaterialOf` the handles an entity is
  drawn with, `Render.TryReadMesh` a mesh's triangles and `Render.TryGetBounds` an entity's bounds.
- **A scene keeps what was made in place.** A primitive, a mesh built vertex by vertex and a
  standard material made in memory are written into a scene as resources saying how to make them
  again, shared by an id within the file ([SCENES.md](SCENES.md), §3), and a material can be kept in
  a file of its own (§5).
- **The asset browser** (`AssetsTab`) shows tiles, an image as itself, a model as a thumbnail drawn
  once (§2) and everything else as an icon for its kind, under a search box, a chip per kind and a
  slider for the tile size (§3).
- **`PreviewRenderer`** (§2) draws the browser's selected glTF file and the two cards' pictures.
- **`PickerWindow`**, the window behind Add Entity and Add Component, is a searchable list of rows
  with an icon and a label.

## Why an entity has no mesh renderer

In Unity a mesh is drawn by an object with a MeshFilter holding the mesh and a MeshRenderer holding
the materials. In Bevy an entity carries two small components, `Mesh3d` and `MeshMaterial3d`, each
a handle to an asset shared by every entity using it, and the renderer finds every entity with
both, gathers them by mesh and material in a world of its own, and draws them in batches. The
entity is a row in that list, not an object that draws itself. So the editor draws the two
components as the two cards Unity has, and a card is a view over the component and its asset
rather than a component of the editor's own.

## 1. The Mesh and Material cards

An entity that draws shows two cards in the details, in the place "Drawn with" has.

Built in `BevyCSharp.Editor/Framework/DrawnCards.cs`, as an open fold under each row:

- **The Mesh card** shows the mesh alone in a plain gray, turned by dragging, with a Wireframe
  toggle, and what it is made of and where it came from: the file, a primitive's shape and measures
  (`Render.RecipeOf`), or "built in code", then vertices, triangles, indices and their width,
  topology, attributes and size.
- **The Material card** shows the material on a sphere, turned the same way, and its settings as
  rows (base color, metallic, roughness, emissive, alpha, double-sided, unlit), drawn and undone as
  a component's fields are. They are fields of a schema over the material, read through
  `Render.TryReadMaterial` and written through `Render.WriteMaterial` (`bcs_render_material_write`,
  ABI 157), which writes the settings over the material in place, so the fold says how many
  entities are drawn with it.

A primitive's measures are rows on the Mesh card, named for its shape (a cuboid's width, height and
depth, a torus's two radii), which rebuild the mesh in place through `Render.RebuildMesh`
(`bcs_mesh_rebuild`, ABI 158), so everything sharing it changes and keeps its handle, and a mesh
file is written again. The Material card's texture slots are rows too, each picked from the images
under the asset root. Not built: normals and the UV checker on the preview, a thumbnail beside each
texture slot, and sub-meshes. The design, all of it:

- **The Mesh card.**
  - **A live preview** of the mesh alone, turned by dragging, with toggles for wireframe, normals
    and a UV checker, as the Better Mesh package shows in Unity.
  - **What it is made of:** vertices, triangles, sub-meshes, which attributes it has (normals,
    tangents, UV sets, colors, joints and weights), whether its indices are 16 or 32 bit, and its
    bounds and size.
  - **Where it came from:** a file and its label, a primitive with its parameters (editable in
    place, rebuilding the mesh), or built in code.
  - **The picker** (§4) from the mesh's name, and the actions of §5.
- **The Material card.**
  - **A preview sphere** in the material, turnable like the mesh.
  - **Its settings as rows:** base color, metallic, roughness, emissive, alpha mode, double-sided,
    unlit, and the texture slots as small thumbnails, each a picker filtered to textures.
  - **A shader material** keeps the parameter rows it has today.
- **What the bridge reads** (built): `bcs_render_mesh_info` for the counts, attributes, index
  format and bounds, `bcs_render_material_read` for a `MaterialSettings` back from a
  `StandardMaterial`, `bcs_render_material_write` for writing one back, and the rest of Bevy's
  primitives, with each primitive's measures kept on the managed side beside its handle so it can
  be edited and saved as what it is. The wireframe is `Render.SetWireframe` on the preview's own
  entities, so only the picture shows edges.

The handles an entity holds are reflected reads of `Mesh3d` and `MeshMaterial3d`
(`EcsWorld.GetReflectedAsset`, [COMPONENTS.md](COMPONENTS.md) tier 1), which return the key the
program already holds, so they need no exports of their own.

## 2. One preview renderer

The cards, the asset browser and the picker all draw assets, and each drawing one its own way would
be three cameras, three lights and three framings to keep consistent.

Built as `PreviewRenderer` (`BevyCSharp.Editor/Framework/PreviewRenderer.cs`): four slots on render
layer bits 12 to 15, each asked for by a key every frame it is wanted, drawing a glTF scene, a mesh
in gray or a material on a sphere, framed so a ball round the subject fits from any angle, turned
by a drag, and put away after the panels draw when nothing asked for it. More keys than slots take
over the one asked for least recently. The details panel's picture of a selected file is one key of
it.

Thumbnails are built in `BevyCSharp.Editor/Framework/Thumbnails.cs`: a model tile asks for one by
being drawn, one file is shown in a slot of its own for thirty frames on a transparent background,
or the color the "Thumbnail background" setting gives, read back with `Render.BeginCapture` and
written with its alpha by `CapturedImage.ToPng` to `user://thumbnails/`, named by a hash of its
path, the time it was last written and the background. Under the player's directory rather than
`build/`, since `user://` loads through Bevy (SCENES.md §1) and a thumbnail is this machine's cache.
Models, mesh files and material files have them. `Render.Screenshot` writes no alpha, which is why
a thumbnail goes through a capture instead.

- **`PreviewRenderer`** is a small pool of render targets, each on a render layer bit of its own
  with its own camera and light. A request names what to draw (a mesh handle, a material on a
  sphere, a glTF scene, a texture) and gets an image back.
- **Live previews** (the cards, the selected tile) render every frame and turn with the mouse.
- **Thumbnails** render once, are captured to PNG in `build/thumbnails/`, keyed by the file's path
  and modification time, and are drawn from there through `ImGuiTextures.Load`. A few are rendered
  a frame, so opening a folder of a hundred models fills its tiles over a second rather than
  stalling for one.
- **Released when not drawn,** as `EditorPreview` does today, because every live target costs a
  render pass each frame whether anybody looks at it or not.

## 3. The asset browser

Built: model tiles show their thumbnails, and a search box, a chip per kind (models, images,
scenes, data, sounds, scripts, shaders) and a tile size slider sit over the tiles. Not built:
material tiles (there are no material files yet), sub-assets, badges, and the preview column's
facts.

- **Tiles show the asset.** A model or mesh is rendered, a material is a sphere in it, a texture is
  itself, and a scene, a data asset or a sound is its icon with a small badge naming its kind.
- **A glTF file opens** into its meshes, materials and textures as tiles of their own, so one of
  them can be dragged or picked without the rest, as Unity and Godot show a model's sub-assets.
- **Search and kinds.** A search box and a row of chips (models, meshes, materials, textures,
  scenes, data, audio, shaders) above the tiles, and a slider for the tile size.
- **The preview column** is a live `PreviewRenderer` view of the selected asset, with the same
  facts the Mesh card shows.

## 4. The picker is the browser

Picking a mesh is finding one among the assets, which the browser does, so the picker is
the browser's grid in a window rather than a dropdown of paths.

Built as a list rather than a grid: the Mesh and Material rows open `PickerWindow`, which gained
group headings (`PickerItem.Group`) and a label for its button, with "Built in", "In this scene" and
"Files" as below, each row an icon rather than a picture, since thumbnails are the next step. Not
built: `AssetGrid`, a glTF file's other meshes, and the same window for textures, sounds and data
assets.

- **`AssetGrid`** comes out of `AssetsTab`: the tiles, the search, the kind chips and the
  thumbnails. The tab draws it, and `PickerWindow` gains a grid mode that draws it too, with
  `PickerItem` gaining a picture.
- **A mesh field opens the grid** with its kind fixed and three groups:
  - **Built in:** Bevy's primitives, each made with default parameters, which the card then edits.
  - **In this scene:** every mesh already in memory and in use, so picking one shares it rather
    than making another.
  - **Files:** meshes, and the meshes inside glTF files.
- **The same window** picks materials, textures, sounds and `DataRef<T>` data assets, so every
  asset field in the editor is picked one way. `EditorWidgets.Picking` stays for choices that are
  not assets, such as an enum.
- **The keyboard works as in Add Entity:** typing searches, the arrows move, Enter picks, Escape
  closes.

## 5. Made in place, or an asset

Unity makes every mesh and material a file, and code that builds a cube has to save one somewhere
before a scene can keep it. Bevy does not ask for that, and a cube built in a script is a mesh with
no file, as `Render.CreateMesh` makes it today. Godot's answer fits both, and is taken here.

- **A sub-resource belongs to the scene** (built). A mesh or material made in place is saved
  inside the scene file as how to make it again: a primitive and its parameters, a
  `MaterialSettings`, or the `MeshData` a script built (`Render.DataOf`, kept beside the handle as a
  copy when the mesh is made). Two entities using one share it by an id within the scene.
- **Save as asset** writes it to a file (`*.material.json`, or `*.mesh.json` for a primitive or
  for generated geometry) and points every entity that used it at the file. Built: each card's
  "Save as asset" writes `MaterialFiles.SaveAs` or `MeshFiles.SaveAs`, named after the entity, into
  the folder the browser shows, and the asset is the file's from then on. A mesh file holds
  geometry as JSON rather than `.glb`, so one reader serves a primitive and a built mesh, and a
  primitive stays one, editable as its shape (`BevyCSharp/Assets/MeshFiles.cs`).
- **Make unique** copies a shared one for this entity alone, so changing its color leaves the
  others. Built for both, on the cards, as a step to undo.
- **Editing a file asset** changes the file, and every scene using it. The card says how many
  entities use it, and an edit to a file's material is written to the file as it is made.
- **Material files** (`*.material.json`, built in `BevyCSharp/Assets/MaterialFiles.cs`) are a
  `MaterialSettings` with texture references by id and path ([SCENES.md](SCENES.md) §2), read on the
  managed side into one material per file that every user shares, written by a scene by the file,
  offered by the picker and drawn as a sphere by the browser, and laid over the material in place
  when the file changes on disk while assets are watched.

## Order

Each step is usable on its own and tested before the next.

1. **Sub-assets and `AssetGrid`:** a glTF file opening into its meshes, materials and textures, and
   the picker's grid mode drawing the browser's tiles.
2. **Normals and the UV checker on the Mesh card's preview**, and a thumbnail beside each texture
   slot.
