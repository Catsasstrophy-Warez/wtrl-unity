# WTRL.Core — contract

**Status: one file, `IsExternalInitPolyfill.cs`** — added 2026-09-20,
the first time this project was opened in a real, licensed Unity
Editor. It's infrastructure, not game logic: a `public static class
IsExternalInit { }` polyfill that every other `init`/`record`-using
assembly needs present somewhere in its reference graph to compile
under Unity's .NET Standard 2.1 profile, which doesn't ship this .NET
5+ marker type in its BCL. Because every WTRL assembly already
references `WTRL.Core`, putting it here fixes the whole project at
once rather than requiring a copy per assembly. See the file's own doc
comment for why the related C# 11 `required`-keyword problem was
*not* also polyfilled here (a different, harder problem — resolved
instead by removing `required` usage project-wide; see `Career/`,
`Garage/`, `Lab/`, `RPG/CONTRACT.md`'s "Unity-Editor compile fix"
sections).

No game-logic types exist here yet. `WTRL.Vehicle`'s port (see
`Vehicle/CONTRACT.md`) turned out not to need anything from `Core` — it
uses only `System.Math` and plain arrays, no shared math/RNG utilities
were actually load-bearing for the physics port.

If you're about to add something here, first check whether it's really
shared across modules (the reason `Core` exists at all) or whether it
only serves one consumer, in which case it probably belongs in that
consumer's own assembly instead. Candidates that *would* belong here,
based on what SwiftRacer had at this layer: a deterministic hash/RNG
helper (SwiftRacer's `RivalDeterministicSample` — FNV-1a based, used so
AI behavior never calls the platform RNG inside a fixed-step sim), and
any shared value types more than one of `WTRL.Vehicle`/`WTRL.World`/
`WTRL.Racing` need identical copies of.

Update this file the moment something real lands here — an empty
contract for a non-empty assembly is worse than no contract at all.

## The deterministic hash/RNG helper this file suggested already exists -- in WTRL.Racing, not here (2026-09-20)

While closing a "what's left" roadmap item ("add a shared deterministic
hash/RNG helper to WTRL.Core"), found that `WTRL.Racing
.RivalDeterministicSample` (`Racing/RivalBehaviorRuntime.cs`) is
already exactly that helper -- a real FNV-1a port of SwiftRacer's
`RivalDeterministicSample`, already used by `TrackAiDriver`'s
intimidation-aware `Input` overload, already tested
(`RivalIntimidationTests.cs`). A duplicate `WTRL.Core.DeterministicSample`
was written and then deleted once this was found, rather than shipping
two competing implementations. Not moved into `WTRL.Core` either --
that would mean updating every existing call site and test for a purely
architectural tidiness gain, not a real fix. This file's own candidate
list is left as-is for now (still accurate as a description of what
*could* live here), but the actual helper genuinely already exists;
check `Racing/RivalBehaviorRuntime.cs` before writing a new one.
