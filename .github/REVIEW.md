# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `46f9ee5`, whose reader over the assets an assembly carries
(`native/bevy_csharp/src/carried.rs`) was read whole and raised nothing.

## Now

In this order, after the batch in progress is committed.

1. **Report a test that cannot run as skipped** (verdict 1). It is first because every other
   judgment made here rests on what a green run means.
2. **Continue TODO.md in its own order.** Nothing read so far argues for changing it.

## Verdicts

1. **184 guards in the tests return early and none skips.** A test whose bridge lacks the
   renderer, the editor, meshlets or Solari returns before asserting and is counted as passed, so
   the headless run CI makes reports the render tests green without drawing anything, as CLAUDE.md
   and TODO.md's Testing section both say. They are to report as skipped with the profile they
   need as the reason, through one helper the guards call, so a run's summary shows how much ran
   on that bridge. xUnit 2.9.3 is referenced, so the means of skipping at run time is chosen by
   whoever makes the change. Verified by the skipped count of a headless run matching the number
   of guarded tests, and an editor run with `--meshlet --solari` skipping only the tests that are
   about a smaller build.
2. **A computed state's rule outlives its app** (`ea35b3c`, `BevyCSharp/Core/ComputedRules.cs`).
   `Rules` is a static dictionary keyed by slot and never cleared, so the function given to
   `AddComputedState` and whatever it captured stay reachable after the app is disposed, and a
   later app in the same process that claims the same slot without a rule inherits the earlier
   one if the bridge asks for it. Whether the bridge can ask is to be checked in `states.rs`. If
   it can, the app clears its slots on dispose. If it cannot, the remarks on `ComputedRules` say
   why. This is minor and goes in with whatever next touches states.

## Decisions

1. **Commits are pushed.** The owner said on 2026-10-03 that the commits made so far are fine as
   they are and that the working session may push `main` along with committing. COMMITS.md and
   CLAUDE.md say commits are never pushed, and both are to say what holds, in the next batch.

## Replies

- **Decision 1 is not acted on yet.** The owner told this session directly to commit and never
  push, so the push and the change to COMMITS.md and CLAUDE.md wait for the owner's word here
  rather than one relayed through another session.
