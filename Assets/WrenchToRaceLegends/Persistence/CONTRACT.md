# WTRL.Persistence — contract

Ported from `SwiftRacer/Sources/WTRLCore/Persistence/CareerSave.swift`
(the save DTO shape and its lenient-decode discipline) and
`CareerSaveCodec`. Depends on `WTRL.Core` (empty), `WTRL.Vehicle`,
`WTRL.Garage`, `WTRL.Racing` (added by this port — see below),
`WTRL.Career`, `WTRL.RPG`, `WTRL.World`.

## Real asmdef change: added `WTRL.Racing`

`WTRL.Persistence`'s original asmdef (scaffolded before `WTRL.Racing`,
`WTRL.Career`, or `WTRL.RPG` existed) had no reference to `WTRL.Racing`.
`CareerState.RivalBehavior` (`WTRL.Career`) is typed as `WTRL.Racing
.RivalBehaviorRuntime`, and serializing/restoring its rival-memory
dictionary requires naming `WTRL.Racing.RivalMemory` directly
(`RivalBehaviorRuntime.Snapshot().Memories` /
`RivalBehaviorRuntime.Restore(...)`) — C# requires a direct reference to
the assembly that declares a type the moment your own code names that
type, even if it only reached you transitively through another
assembly's public API. **This is a mechanical fix, not an open
architecture question** the way `BuildRecipe`'s `WTRL.Garage`
dependency was for `WTRL.RPG` — `WTRL.Persistence`'s entire job is
serializing what `WTRL.Career` exposes, so it needs to see every type
`WTRL.Career` exposes. No cycle results (`WTRL.Racing` has no dependency
on `WTRL.Persistence`).

## Public API

`CareerSaveDto` — the save-file shape, matching Swift's `CareerSave`
field-for-field except where noted below. `CareerSaveCodec.Encode
(CareerState) -> string` / `Decode(string) -> CareerState`.

## Deliberate deviations from the Swift source

1. **System.Text.Json instead of `JSONEncoder`/`JSONDecoder`** — built
   into .NET/Unity's framework, no external package needed.
2. **`IncludeFields = true` is load-bearing, not incidental.**
   `InstalledComponent`/`RivalMemory` are ported as structs with public
   *fields* (matching their Swift originals' plain `var` properties, but
   this port used fields rather than auto-properties for those two —
   see `WTRL.Garage/Definitions.cs`/`WTRL.Racing/RivalBehaviorRuntime.cs`).
   System.Text.Json ignores fields by default; without this option, both
   types would silently serialize as empty objects. Caught by writing a
   real round-trip test before assuming this worked.
3. **`runEvidence`/`dynoRuns`/`ghostReplays` are NOT in the DTO.** The
   first two need `WTRL.Lab` types this assembly has no dependency on
   (same shape as `WTRL.Career`'s `.recordEvidence` deferral); the third
   needs a `DeterministicReplay` type that doesn't exist anywhere in this
   project yet. Flagged, not silently dropped — a save file written by
   this codec will NOT round-trip dyno history, test-run evidence, or
   ghost replays. Anyone adding those needs to decide the same kind of
   dependency question `BuildRecipe` already resolved once.
4. **The hero-gen1→hero-1965 rename and the "selected vehicle must be
   owned" safety invariant** are preserved exactly, applied on every
   decode regardless of save shape — same reasoning as the Swift
   original's own comment about why this can't be a separate legacy-path
   fallback once decoding is fully lenient.

## A real near-bug this port caught before it shipped

Adding `LicensePassed`/`SelectedVehicleId`/`OwnedVehicleIds`/the three
dyno slider fields to `CareerState` (needed for this DTO to have
something to read) initially did NOT update `CareerState.Clone()`/
`CopyFrom()` to include them. Since `Clone()` builds a new instance via
object-initializer syntax, any field not explicitly listed silently
resets to its declared default rather than copying from the source —
meaning **every single `CareerTransaction.Apply` call would have reset
the player's selected vehicle back to `"hero-1965"` and every dyno
slider back to its default**, regardless of what the player had
actually set. Caught while writing this port (the fields were added for
`CareerSaveDto`'s sake, and reviewing `Clone()` immediately after showed
the gap), fixed before any test was written against it, and left as an
explicit warning in `Clone()`'s own XML doc for the next person who adds
a field to `CareerState` without reading it.

## Verification

Same method as every prior assembly: a throwaway `dotnet build`/
`dotnet test` project containing all of `WTRL.Persistence`'s
dependencies. **0 errors, 0 warnings** on the first attempt. 7 new
tests (round-trip of core fields, installed components + vehicle
history, rival memory through `RivalBehaviorRuntime` specifically
verifying the derived intimidation state comes back correctly too, the
hero-gen1 rename, the owned-vehicle safety invariant on a minimal `{}`
save, garbage-JSON fallback to defaults rather than throwing, and dyno
slider round-trip) — no Swift-test equivalent existed to port, since
those tests exercised real hero-1965-catalog scenarios this port has no
catalog for. All pass. **70/70 passing project-wide** across all nine
real assemblies now shipped (Vehicle, Racing, Garage, Lab, RPG, World,
Events, Career, Persistence).
