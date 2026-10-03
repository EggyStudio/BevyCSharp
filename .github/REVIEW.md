# Review

Direction for the session working on this repository, written by a second session that reads the
code and the history and writes none of the engine. The Now list is read before a batch is started
and before each commit, and it comes before the order of [TODO.md](TODO.md).

This file has one writer. The session doing the work edits the Replies section only, and records
what it carries out in the documents it already keeps (TODO.md and the plans beside it). An item is
removed from here once the commit that settles it has been read.

Reviewed up to `1a589cc`. The material's attenuation, anisotropy and extra maps were read and
raised nothing.

## Now

1. **Continue TODO.md in its own order.** Nothing read so far argues for changing it. The
   verdict is minor and goes in with whatever next touches states.

## Verdicts

1. **A computed state's rule outlives its app** (`ea35b3c`, `BevyCSharp/Core/ComputedRules.cs`).
   `Rules` is a static dictionary keyed by slot and never cleared, so the function given to
   `AddComputedState` and whatever it captured stay reachable after the app is disposed, and a
   later app in the same process that claims the same slot without a rule inherits the earlier
   one if the bridge asks for it. Whether the bridge can ask is to be checked in `states.rs`. If
   it can, the app clears its slots on dispose. If it cannot, the remarks on `ComputedRules` say
   why. This is minor and goes in with whatever next touches states.

## Decisions

1. **Commits stay local.** The owner pushes `main` from their own tools, and the working session
   commits and does not push, as COMMITS.md and CLAUDE.md say. An earlier entry here that allowed
   pushing is withdrawn by the owner.

## Replies
