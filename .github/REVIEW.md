# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `0d30866`. Seven more 3D examples (`pccm`, `blend_modes`, `camera_sub_view`,
`color_grading`, `anisotropy`, `contact_shadows`, `transmission`) and the rows marked as waiting
on an extended material (`5839b71`) were taken on their descriptions and raised nothing. The
table stands at 129 written, 7 written in part, 101 that can be, 127 missing and 57 that do not
apply. The examples held 272 string-path calls when item 1 was written and hold 238 in the
working tree.

## Now

Item 1 is the owner's request of 2026-10-04 and goes on a group at a time. Items 5 to 7 are
taken from [SHARED.md](SHARED.md).

1. **Bevy's components are written with no string paths**, which the owner asked for on
   2026-10-04 on seeing `fog` set its `DistanceFog` by `".falloff.start"` and a number made into
   text. It comes before more examples are written, since each one adds paths to rewrite.
   - **What is there and unused.** `ReflectedGenerator` already emits a wrapper for each of
     Bevy's components from `BevyCSharp/Generated/bevy-components.tsv`, `DistanceFogRef` among
     them with `Color`, `DirectionalLightColor`, `DirectionalLightExponent` and `Falloff`.
     `BevyCSharp.Examples` holds 272 string-path calls in 53 files and no use of a wrapper.
   - **The gap.** A field inside an enum's variant has no property (COMPONENTS.md, what a
     wrapper leaves out). `schema.dump` writes each data-carrying variant's fields to the
     description, a line a field, from Bevy's own account of the enum. The generator emits for
     such an enum an abstract record with a nested record a variant,
     `FogFalloff.Linear(float Start, float End)` and `FogFalloff.Exponential(float Density)`,
     a variant with no data being a record with no fields, and the wrapper's property takes and
     returns it, so reading is a `switch` on the type and writing sets the variant and its
     fields together. Rust's `Option<T>` becomes a nullable `T?`. An enum with no data stays
     the C# enum it is.
   - **Values cross as values.** Where a wrapper's setter makes a number into text for the
     bridge to parse, it passes the number, so no `CultureInfo` is involved. What the setters
     do today is checked and said under Replies.
   - **The examples use the wrappers.** Every string-path call whose component and field a
     wrapper covers is rewritten, so `fog` reads
     `fog.Falloff = new FogFalloff.Linear(_start, _end);`. A string path stays only where no
     wrapper reaches, with a comment saying why, and a test lists the string-path calls in the
     examples and fails for one a wrapper covers.
   - **Documents.** `docs/components.md` leads with the wrappers and shows the union form, and
     COMPONENTS.md and TODO.md lose their entry on variant fields.
   - Verified by `fog` compiling with no string path and its capture not changing, a test that
     sets `DistanceFogRef.Falloff` to each variant and reads it back, and the count of
     string-path calls in the examples falling from 272 to the few a comment explains.
2. **Bevy's examples, what is left of the pass.** Math, then the rest of 3D, an example that
   needs something missing marked and passed over. A picture that differs from Bevy's for no
   known reason is taken down to the smallest scene that still differs and explained before
   the pass goes on, as `grid` was, since that is where the table finds a fault.
3. **The gaps, by how many rows each holds**, once the groups in item 1 are through, each
   bridged from Bevy with the examples it unlocks written in its batch. By the table as it
   stands: 2D meshes with their color materials (13 rows); an order among the systems of one
   Bevy's
   resources reached through reflection as its components are, which `UiScale` and others wait
   on and which is likely a small bridge for many rows; then text gizmos, editable text and
   Bevy's widgets.
   When the captures have settled, they are compared whole with checked-in references by the
   workflow, a small share of pixels allowed to differ between devices, as 3DEngine's
   `771f10e9` does for its scenes, so an example that stops drawing as it did fails a run.
4. **A build with no warnings, and a warning failing the workflow.** The managed build passes
   `-warnaserror` in the workflow once it is clean, with a warning that is right to keep turned
   off where it arises and its reason beside it, and `cargo` builds deny warnings the same way.
5. **A contact that says how hard its pair hit, and joints with limits**, from 3DEngine's
   `c5227118`: the speed a pair closed at on the contact's message, a ball joint kept within a
   cone, and a distance joint whose range changes after it is made.
6. **Two more of a character and a collider**, from 3DEngine's `52579d98` and `454e9276`: a
   character that crouches and stands from its component's height, and a collider that is the
   shape of the meshes an entity and those under it show, made once the model has loaded.
7. **A joint in a scene file, and a pad's sensors**, from 3DEngine's `e46058fc` and `73ce6326`:
   a joint as an entity naming its two bodies, so a level hangs a door where it stands, and a
   gamepad's gyro, accelerometer, touchpad and light where gilrs offers them. Two small things
   of the command line are checked in the same batch and taken if they are missing: `entity.set`
   writing a field that holds a list from items split by semicolons, a command that pretends
   files dropped on the window, a command's parameter with a default being left off, and a
   placed scene file spawned again when it is written while the level runs.

## Verdicts

None open.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and AGENTS.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **AGENTS.md's bullet on SHARED.md is the owner's.** They approved it on 2026-10-04, and it is
   committed like any other change.

## Replies


- Item 1. Done. What the wrappers' setters sent before this: a number as JSON text, written by
  `Utf8JsonWriter`, so no culture was involved, and parsed back by the bridge. Now a float, a
  whole number and a flag cross as numbers, through the bridge's `bcs_reflect_get_float`,
  `bcs_reflect_set_float` and their integer pair (ABI 183), each converting to the field's own
  Rust type and refusing a whole number that does not fit it. Vectors, strings and entities still
  cross as JSON, which is the only form the bridge has for them. `schema.dump` writes each
  variant's fields, and the generator makes the unions and the nullables as the item describes.
  A union is shared between every field of its Rust type, as `Val` is between a node's lengths. A
  struct inside an `Option` is a record of its fields flattened, so `SubCameraView` is
  `(FullSizeX, FullSizeY, Offset, SizeX, SizeY)`, since its `UVec2` comes as two rows. A component
  that is itself an enum, such as `Visibility`, now has a row and a `Value` property, which the
  inspector shows as a choice. A variant holding a handle, as a text's font source or a fog
  volume's density texture does, could not be chosen, Bevy registering no default for a handle,
  so the bridge makes one from the handle's own type data, and the wrapper writes the real handle
  after it. Every rewritten example was captured again and compared with its committed picture,
  and they agree but for what moves between runs. `EcsWorld.Wrap<T>` is added for a component the program put there
  itself, since a property cannot be set on what `Get<T>` returns. The examples went from 292
  string-path calls to 15, each beside a comment saying what no wrapper types: lists (box
  shadows, gradients), an enum inside a variant (a sprite's slicer, an orthographic projection)
  and a range of numbers (`VisibilityRange`). `ExampleStringPathTests` reads every example with
  Roslyn and fails on a call a wrapper covers.
