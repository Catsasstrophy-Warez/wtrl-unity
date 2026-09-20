# WTRL.UI — contract

Was an empty stub assembly (asmdef only) until this pass. Now contains
the project's first `MonoBehaviour` — the smallest possible end-to-end
vertical slice connecting a `WTRL.Content` asset to `WTRL.Runtime` to
an actual Unity `Transform`.

## Public API

`VehicleRuntimeController` (`MonoBehaviour`) — takes a
`VehicleDefinitionAsset` (see `WTRL.Content/CONTRACT.md`), constructs a
`WTRLRuntime` in `Awake`, reads WASD/arrow keys via the new Input
System in `FixedUpdate`, calls `WTRLRuntime.Advance`, and copies the
resulting simulated X/Z/heading onto its own `transform`. Exposes
`CurrentSnapshot` for a future HUD (not wired to anything yet).

Added `Unity.InputSystem` and `WTRL.Content` to this assembly's asmdef
references to support it.

## Explicitly out of scope for this pass

No visuals (no mesh, no wheels, no camera follow), no HUD, no scene
file, no ScriptableObject asset instances. This is deliberately the
narrowest possible slice — a box-with-a-script proving the full chain
compiles and runs, not a playable vehicle. Adding those is the natural
next step once this compiles cleanly.

## UNVERIFIED — same caveat as WTRL.Content

This file references `UnityEngine`/`UnityEngine.InputSystem`, so it
cannot be verified via this project's usual throwaway `dotnet
build`/`dotnet test` method. It has never been compiled. See
`Content/CONTRACT.md` for the full explanation of why (Unity installed
in this environment is unlicensed; a batchmode open attempt hung
waiting for interactive license activation and was killed rather than
left running).

Known things that need real-Editor confirmation, not assumed correct:
- The exact `Keyboard.current` / `.isPressed` API surface, which
  depends on the Input System package version actually resolving
  (`com.unity.inputsystem: 1.11.2` is declared in `Packages/
  manifest.json` but has never been fetched/resolved by an Editor).
- Whether the project's Input System is even configured for the "new"
  backend vs. "both" — if `Player Settings > Active Input Handling` is
  still on the legacy-only default, this script will compile but the
  `Keyboard.current` API will still work (that API exists regardless of
  the active handling setting for reading, though events/callbacks
  differ) -- worth a first-run sanity check regardless.

## Manual steps still required (cannot be done without a licensed Editor)

1. Open the project once and confirm zero Console errors.
2. Create one of each asset via `Assets > Create > WTRL > Content > …`
   (Engine, Transmission, Suspension, Tire, then Vehicle referencing
   the first four) and fill in real numbers — e.g. copy the
   `hero-1965`-equivalent values used throughout this project's tests
   (`WTRL.Tests.EditMode`'s `Make*()` helper methods are a reasonable
   starting point for plausible values).
3. Create a new empty scene, add an empty GameObject, attach
   `VehicleRuntimeController`, assign the Vehicle asset, press Play.
4. If it compiles and the object moves under WASD, this is the first
   real end-to-end proof this whole project's simulation code runs
   inside Unity at all.
