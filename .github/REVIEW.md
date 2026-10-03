# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `d70281c`. The skips (`e857326`) are settled, by `Needs` and the count the
headless run reported. The pack file (`d70281c`) was read whole and raised verdict 2.

## Now

1. **Continue TODO.md in its own order.** Nothing read so far argues for changing it. The two
   verdicts are minor and go in with whatever next touches their files.

## Verdicts

1. **A computed state's rule outlives its app** (`ea35b3c`, `BevyCSharp/Core/ComputedRules.cs`).
   `Rules` is a static dictionary keyed by slot and never cleared, so the function given to
   `AddComputedState` and whatever it captured stay reachable after the app is disposed, and a
   later app in the same process that claims the same slot without a rule inherits the earlier
   one if the bridge asks for it. Whether the bridge can ask is to be checked in `states.rs`. If
   it can, the app clears its slots on dispose. If it cannot, the remarks on `ComputedRules` say
   why. This is minor and goes in with whatever next touches states.
2. **A pack is trusted further than it is checked** (`d70281c`, `BevyCSharp/Assets/AssetPack.cs`).
   `AssetPack.Write` records each file's length from `FileInfo` and copies the file afterward, so
   a file saved in between (the editor exports while its project is open) is written at a length
   the table does not hold, and every file after it is read from the wrong offset with no error.
   The copy is to be checked against the recorded length and the write refused when they differ.
   `AssetPack.Open` checks `offset + length > size`, which a damaged table overflows past, and
   passes a damaged count to the dictionary's constructor, so a damaged pack fails with an
   overflow or an out-of-memory error instead of the `InvalidDataException` the other checks
   give. The length is to be compared as `length > size - offset`, and the count bounded by what
   the file's size could hold. Verified by a test for each: a file that grows during a write, and
   a table with an offset near `long.MaxValue`.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and CLAUDE.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.

## Replies
