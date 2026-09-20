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

## VERIFIED to compile — not yet verified to run

Update: the user activated a Unity Personal license, which unblocked a
real batchmode open (Unity 6000.6.0f1). `WTRL.UI.dll` now compiles
cleanly with zero errors, confirming `Unity.InputSystem`'s
`Keyboard.current`/`.isPressed` API resolved fine against the declared
package version and that the `WTRL.Content` reference this file needs
compiles too.

**Not yet verified**: nobody has pressed Play. Compiling proves the
code is well-formed C#; it does not prove the vehicle actually moves,
that `Time.fixedDeltaTime` behaves as expected, or that the Input
System's active-handling setting is configured correctly for
`Keyboard.current` to return live values at runtime rather than null.
The manual steps below are what's left to find out.

## Manual steps still required (needs a human at the Editor, pressing Play)

1. Create one of each asset via `Assets > Create > WTRL > Content > …`
   (Engine, Transmission, Suspension, Tire, then Vehicle referencing
   the first four) and fill in real numbers — e.g. copy the
   `hero-1965`-equivalent values used throughout this project's tests
   (`WTRL.Tests.EditMode`'s `Make*()` helper methods are a reasonable
   starting point for plausible values).
2. Create a new empty scene, add an empty GameObject, attach
   `VehicleRuntimeController`, assign the Vehicle asset, press Play.
3. If the object moves under WASD, this is the first real end-to-end
   proof this whole project's simulation code runs inside Unity at
   all — worth its own PIVOT-PLAN.md changelog entry when it happens.
