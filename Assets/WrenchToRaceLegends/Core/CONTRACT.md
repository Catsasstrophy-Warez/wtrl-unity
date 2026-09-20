# WTRL.Core — contract

**Status: empty.** No types exist here yet. `WTRL.Vehicle`'s port (see
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
