# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `13eb521`. The glTF examples with `hello_world` (`bb440b9`) and the window examples
with Bevy's two window tests (`13eb521`) are settled on their replies, which were read.
`window_resizing` was set beside Bevy's and has its three sizes, its keys and its text, and says
where it differs, the size read each frame where Bevy reads a message for each resize. The two
replies name two things Bevy's examples lean on that are not bridged, an event that says a scene
is ready and the message a resize sends, which belong among the gaps of item 5. The table stands
at 222 written, 13 written in part, 26 that can be, 102 missing and 58 that do not apply.

[NORM.md](NORM.md) is new, and item 2 of the Now list is about it.

## Now

The owner asked that the work does not stop. A batch that ends is followed by the next item
here with no wait for a reply, and the list is long so that it does not run out. Items 4, 7 to 9
and 11 to 14 are taken from [SHARED.md](SHARED.md).

1. **The 26 rows that say `can be written` are written**, until that column is empty, many a
   batch, then the stress tests, which are also numbers for PERFORMANCE.md beside Bevy's own.
   An example that needs something missing has its row changed and is passed over. A picture
   that differs from Bevy's for no known reason is taken down to the smallest scene that still
   differs and explained before the pass goes on. The second paragraph of item 3 holds for each
   example written from here on, which Annex B of the norm has as B 4.
2. **The norm's checks.** [NORM.md](NORM.md) is new, at the owner's wish of 2026-10-05: the
   rules both engines keep, numbered, each with its reason and what checks it, 36 of them and four
   of the bridge's in Annex B. It reached this checkout with `13eb521`. One batch gives the rules
   their checks here. A class `NormTests` has a test for each rule the table under Conformance
   calls `to take` for BevyCSharp and a test or a setting can check, named for the rule as `N_1_3`
   is for N 1.3, its message beginning with the rule's number. A rule existing code does not keep
   gets its list, `build/norm/<number>.txt`, written from the test's own finding so the first
   list is exact, the test failing for a place not listed and for a line that no longer applies.
   One more test holds NORM.md and `NormTests` to each other. No file is rearranged in this
   batch. The lists are what is left, and a place on a list is mended when a batch next touches
   it.

   Measured from outside, N 1.3 lists 11 files of the library over 800 lines and eight or more of
   the bridge, N 1.4 all 132 test files, which sit at the root, and N 1.2 about 65 files. N 2.2
   and N 6.1 are settings, CS1591 as an error and warnings failing the workflow, which is item 6,
   and N 2.1 is the listing of item 13. N 2.5, N 2.6, N 2.7 and N 5.2 are more than this batch and
   keep `to take` until their own items. Annex B is the reviewing session's reading of this
   engine: its namespaces with their reasons, the list of packages still to be written in
   BUILDING.md, and four rules of the bridge. What is wrong in it is said under Replies with a
   line beginning `Rule:`, as is a rule read as wrong. The count of each list goes under Replies,
   and the table in the norm is brought up to them.
3. **An example is a program somebody could write on the package.** A picture in the README
   opens an example as the way to do a thing, and what opens is written in words the package
   does not have. `BevyCSharp.Examples/Example.cs` holds helpers, in its own words for what
   Bevy's examples say in one word and the bridge in several, and 208 of the 231 examples call
   `app.Startup`, 171 `app.Update`, 56 `Scene.Srgb8` and 25 `SpawnGltf`, beside a mesh spawned
   with its material and its place in one call, a point light, a camera, `Slerp` and `Hsl`. None
   of it compiles outside the examples project. Each of those helpers is something a game says
   in several calls too, so they become the package's own, named and documented as the rest is
   and in the cheatsheet, and `Example.cs` keeps only what drives an example for its capture.
   A check builds a handful of examples in a project of their own on the packed package, as the
   README's walk does, so one that leans on the examples project fails it.

   The README's first program is `[Behavior]` on a struct, which 8 examples use while 136 keep
   their state in static fields. Where Bevy's example keeps state on an entity, in a component
   with a system over it, as `cooldown` does with a timer on each button, the example here keeps
   it on the entity in a behavior, and a static field is for what Bevy keeps in a resource or a
   `Local`. The examples written are brought over a group at a time, EXAMPLES.md saying how many
   are, the helpers first since every example reads shorter for them.
4. **What a kinematic body carries, and a clock stepped by hand**, both from a red run of
   3DEngine's on Windows, mended there in `966c2c88`, `15fa305a` and `ee3b47dd`, and measured here
   before anything is changed. There the engine measured where the model put it, 1.75 against
   1.74 at 144 frames a second, and holds 2.00 at every rate since.
   `PhysicsWorld.Step` puts a kinematic body at its entity's place and gives it the distance
   moved since the last step over one step's time. An entity moved once a frame then has a body
   whose speed in a step is the frame's distance over a step's time, and zero in every further
   step of the frame. Friction is too weak to follow those jumps, so what rests on the body
   settles at the speed it has in most steps and not at its average. A model of one dimension
   gives, for a platform at 2.00 and Bevy's 64 steps a second, a crate at 2.12 at 60 frames a
   second, 1.82 at 144, 2.52 at 50 and 0.07 at 30. `Step` is public and takes its seconds, so a
   test moves a platform's entity once a frame, steps as many times as a frame of that length
   holds, and reads the crate's pace, as a table over 144, 75, 60, 50 and 30 frames a second and
   over frames of uneven length.
   If the engine agrees with the model, the body moves at its entity's speed, which is the
   distance the entity moved between two frames over the time between them, through every step
   of the next frame, and each step aims at the entity's last place moved on by that speed for
   the time simulated since. That holds 2.00 at every rate in the model. A turn gives the body no
   spin today, so nothing is carried round on a turning platform, and it is given the same way.
   An entity moved in fixed steps has to stay exact.
   The clock is Bevy's own `TimeUpdateStrategy::ManualDuration`, reached from C# and from `bcs`,
   so a test or a capture advances a set amount a frame and is the same on every machine.
   `Time.Step` runs frames at the delta they would have had, which is the machine's.
5. **The gaps, by how many rows each holds**, once item 1 is through, each bridged from Bevy
   with the examples it unlocks written in its batch: more of Bevy's WGSL reached as its
   lighting is (the deferred buffers, a decal's tag and a volume's voxels), the widgets' events
   as observers, keys observed as they reach a field, and what the table then names most. When
   the captures have settled, they are compared whole with checked-in references by the
   workflow, a small share of pixels allowed to differ between devices, as 3DEngine does for its
   scenes.
6. **A build with no warnings, and a warning failing the workflow.** The managed build passes
   `-warnaserror` in the workflow once it is clean, with a warning that is right to keep turned
   off where it arises and its reason beside it, and `cargo` builds deny warnings the same way.
7. **More of what bodies do**, from 3DEngine's `c5227118`, `8520dbe1`, `799a9d56`, `979c97be` and
   `53cd565f`: the speed a pair closed at on the contact's message, a ball joint kept within a
   cone, a distance joint whose range changes after it is made, bodies on layers whose pairs
   collide or not, which contacts, triggers, the character and rays follow, a body asleep waking
   when its layer changes, a body a game knows is fast swept over each step so it does not cross
   a thin wall, a slider joint with limits, a motor and its position, and how hard two touching
   bodies press, as the push alone and answered while they sleep.
8. **Two more of a character and a collider**, from 3DEngine's `52579d98` and `454e9276`: a
   character that crouches and stands from its component's height, and a collider that is the
   shape of the meshes an entity and those under it show, made once the model has loaded.
9. **A joint in a scene file, and a pad's sensors**, from 3DEngine's `e46058fc` and `73ce6326`:
   a joint as an entity naming its two bodies, so a level hangs a door where it stands, and a
   gamepad's gyro, accelerometer, touchpad and light where gilrs offers them. Two small things
   of the command line are checked in the same batch and taken if they are missing: `entity.set`
   writing a field that holds a list from items split by semicolons, a command that pretends
   files dropped on the window, a command's parameter with a default being left off, and a
   placed scene file spawned again when it is written while the level runs, under the entity
   that placed it and giving back what the old copy held (`5b2234d2`).
10. **The three shapes no wrapper types**, when a batch next touches the generator: a list inside
    a component (box shadows, gradients), an enum inside a variant (a sprite's slicer, an
    orthographic projection), and a range of numbers (`VisibilityRange`), which are the 13
    string paths the examples still hold.
11. **Three things 3DEngine's fourth game turned up, checked here** (`3c9c7ac8` in its checkout),
    each taken if it is missing and answered under Replies if it is not. A behavior method that
    writes a resource, draws interface or plays a sound while others run beside it on worker
    threads. A script compiled while the game runs naming the game's own types, with the scripts
    watched being the project's and not a copy in the build folder, which Courtyard would show.
    And a game written in behaviors alone with hundreds of entities, played by the workflow and
    profiled, which `games/Stress` measures and no game here plays.
12. **Two things nothing here has tried**, from 3DEngine's `044d2396` and `3442e2cd`, where each
    found faults at once. Courtyard and the stress program played for ten minutes by a script
    while managed memory, the bridge's allocations, entities and assets are read at intervals
    through a `bcs` command, anything that keeps climbing found and fixed, and a short form of
    the run in the workflow. And every loader given a missing, an empty, a cut short and a random
    file (scenes, data assets, saves, materials, meshes, images, models, sounds, shaders and
    scripts), each answering with a message that names the file and no exception or panic
    crossing the bridge, as one table in a test.
13. **The public surface written down, and release notes from the commits**, from 3DEngine's
    `fc5aef49`: a listing of every public type and member a tool writes from the built assembly,
    checked in, with a test that fails when the two differ, so a change to what a game calls is
    read as one, and the pack workflow writing the package's release notes from the commits
    since `build/version.txt` last changed. In the same batch it is checked whether a ray here
    stops at a sensor, which there threw a car's wheel and a character's ground check.
14. **A first game told from an empty folder, a step at a time**, from 3DEngine's `d5d2578d`.
    `docs/making-a-game.md` describes Courtyard finished, and nothing here walks a newcomer from
    an empty folder and the package to a small game in a dozen steps, each step a whole program
    the workflow builds and runs and the page is held to line for line.

## Verdicts

None open.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and AGENTS.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.
2. **AGENTS.md's bullet on SHARED.md is the owner's.** They approved it on 2026-10-04, and it is
   committed like any other change.
3. **A picture opens the example's own source, and not Bevy's live demo of it.** The owner chose
   it on 2026-10-05, and `57fc7e9` carried it out.
4. **AGENTS.md's bullet on NORM.md is the owner's, with the exception N 7.4 makes.** They approved
   both on 2026-10-05 in the reviewing session, with the plan for the norm. A working session
   that commits a change to its instruction file only on the owner's word in its own session
   is right to, and waits for that word.

## Replies
- Item 1, physics_in_fixed_timestep. Written, its player a behavior as item 3 asks, with
  `Time.FixedOverstep` added for it, Bevy's `overstep_fraction`, read from the engine when asked
  since the frame's time is taken before its fixed steps run, with a test. The norm's checks, item
  2, come next, and the rows left in item 1 after them.
- Item 2, the norm's checks. `NormTests` checks N 1.1 to 1.5, 3.3, 3.4, 4.1, 4.2, 4.5, 6.4,
  7.2 and B 3, and `NormAndItsTestsAgree` holds the class and NORM.md to each other. The lists, in
  `build/norm`, hold 348 places for N 1.2, 32 for N 1.3 (11 files of the library, 3 of the tests
  and 18 of the bridge), 136 for N 1.4, 4 for N 3.3, 55 for N 3.4, 3 for N 4.1, 5 for N 4.5 and
  17 for B 3. N 1.1, N 4.2 and N 7.2 hold with nothing listed, and N 1.5 holds since the rows it
  asked for are added to AGENTS.md, as its bullet allows. N 6.4 opens the package `BCS_PACKAGE`
  names, or the newest one in `build/package`, and the pack job now runs it on what it packed,
  which no run has tried yet. N 7.2 reads the commits after `3e694f0`.
- Rule: N 4.5 holds a capture to Bevy's window, and five examples ask for a window of their own as
  Bevy's do (`grid`, `headless_renderer`, `resizing`, `scale_factor_override`, `window_settings`),
  so they are listed for want of a rule that says the size the example asks for.
- Rule: B 3 lists 17 entry points that answer a constant, a flag or a count and cannot panic, such
  as `bcs_abi_version` and the `bcs_has_*` checks, which a rule might leave out by name.
- Rule: N 1.4 has no folder for what the tests share, `EngineFixture.cs`, `PictureRun.cs` and the
  like, which test no area of the library and sit at the root with the rest.
