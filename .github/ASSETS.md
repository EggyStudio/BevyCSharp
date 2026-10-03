# Seeing and picking meshes, materials and textures

How the editor shows what an entity is drawn with, how a mesh or a material is picked, what the
asset browser's tiles show, and where a mesh or material made in place lives. What the bridge reads
back (step 1 of the order) and a scene's resources (part of step 5) are built, and the rest of this
file is the design, in the order it can be built. [COMPONENTS.md](COMPONENTS.md) covers
components in general and [SCENES.md](SCENES.md) how all of it is saved.

## What exists

- **"Drawn with"** (`BevyCSharp.Editor/Framework/EditorDrawn.cs`) is a section under the components
  with a Mesh row and a Material row. Each is a dropdown (`EditorWidgets.Picking`) of the
  `.gltf`, `.glb` and `.obj` files under the asset root, up to two hundred, or "made here" for one
  built in memory. It offers none of Bevy's primitives, nothing already in memory, and no material
  files, because there are none.
- **Thirteen of Bevy's primitives** (`MeshShape`: cuboid, sphere, plane, capsule, cylinder, cone,
  conical frustum, torus, circle, annulus, rectangle, triangle, tetrahedron) are made with
  `Render.CreateMesh`, which keeps the shape and measures beside the handle (`Render.RecipeOf`),
  and a material with `Render.CreateMaterial(MaterialSettings)`.
- **What comes back.** `Render.TryGetMeshInfo` gives a mesh's vertex and index counts, index width,
  topology, attributes and bounds without copying its vertices, `Render.TryReadMaterial` a standard
  material's settings whoever made it, `Render.MeshOf` and `MaterialOf` the handles an entity is
  drawn with, `Render.TryReadMesh` a mesh's triangles and `Render.TryGetBounds` an entity's bounds.
- **A scene keeps what was made in place.** A primitive and a standard material made in memory are
  written into a scene as resources saying how to make them again, shared by an id within the file
  ([SCENES.md](SCENES.md), §3).
- **The asset browser** (`AssetsTab`) shows 96 pixel tiles, an image as itself and everything else
  as an icon for its kind, with no search.
- **One preview** (`EditorPreview`) draws a selected glTF file into a 256 pixel image, with a camera
  and a light of its own on render layer 12, framed by the model's bounds, and put away when no
  panel asks for it.
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
  `StandardMaterial`, and the rest of Bevy's primitives, with each primitive's measures kept on the
  managed side beside its handle so it can be edited and saved as what it is. Still to add is a
  wireframe through Bevy's `WireframePlugin`, on the preview's layer only.

The handles an entity holds are reflected reads of `Mesh3d` and `MeshMaterial3d`
(`EcsWorld.GetReflectedAsset`, [COMPONENTS.md](COMPONENTS.md) tier 1), which return the key the
program already holds, so they need no exports of their own.

## 2. One preview renderer

The cards, the asset browser and the picker all draw assets, and each drawing one its own way would
be three cameras, three lights and three framings to keep consistent.

- **`PreviewRenderer`** grows out of `EditorPreview`: a small pool of render targets, each on a
  render layer bit of its own with its own camera and light. A request names what to draw (a mesh
  handle, a material on a sphere, a glTF scene, a texture) and gets an image back.
- **Live previews** (the cards, the selected tile) render every frame and turn with the mouse.
- **Thumbnails** render once, are captured to PNG in `build/thumbnails/`, keyed by the file's path
  and modification time, and are drawn from there through `ImGuiTextures.Load`. A few are rendered
  a frame, so opening a folder of a hundred models fills its tiles over a second rather than
  stalling for one.
- **Released when not drawn,** as `EditorPreview` does today, because every live target costs a
  render pass each frame whether anybody looks at it or not.

## 3. The asset browser

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

- **A sub-resource belongs to the scene** (built for primitives and standard materials). A mesh or
  material made in place is saved inside the scene file as how to make it again: a primitive and
  its parameters, a `MaterialSettings`, or, not yet, the raw `MeshData` for geometry a script
  generated. Two entities using one share it by an id within the scene.
- **Save as asset** writes it to a file (`*.material.json`, `*.mesh.json` for a primitive, `.glb` for
  generated geometry) and points every entity that used it at the file.
- **Make unique** copies a shared one for this entity alone, so changing its color leaves the
  others.
- **Editing a file asset** changes the file, and every scene using it. The card says how many
  entities use it before the first change, and Make unique is the way to change one.
- **Material files** (`*.material.json`) are a `MaterialSettings` with texture references
  ([SCENES.md](SCENES.md) §2), read on the managed side into `CreateMaterial` and reloaded when the
  file changes.

## Order

Each step is usable on its own and tested before the next.

1. **`PreviewRenderer` and the two cards,** with the preview's wireframe. Tested by an offscreen picture run where a preview
   target holds something other than its clear color.
2. **`AssetGrid` and the picker's grid mode,** replacing the mesh and material dropdowns.
3. **Thumbnails and the browser:** tiles, sub-assets, search and kinds.
4. **Raw geometry as a sub-resource, Save as asset, Make unique and material files.**
