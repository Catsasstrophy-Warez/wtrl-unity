# Rev16.1 audit

Written by Claude, not ChatGPT — ChatGPT/Gemini turned out not to be
available for this project, so both assignment briefs
(`ChatGPT-Rev16.1-Audit-Brief.md`, `Gemini-RPG-Spec-Brief.md`) were
completed by this session instead. See `PIVOT-PLAN.md`'s changelog. The
brief's own scope and verification discipline were followed as written.

## What's real and verified-by-reading

### Event preflight gating

`Assets/WrenchToRaceLegends/Runtime/EventPreflightService.cs` (real,
compiling C# — not under `Prototype~/`). `EventPreflightService.Evaluate(...)`
takes an `EventDefinition` plus career/garage/vehicle/capability/
homologation/loadout/transport state and returns an `EventPreflightReport`
— a list of `EventPreflightCheck` records (`checkId`, `label`, `passed`,
`severity` ∈ `{Info, Warning, Blocker}`, `detail`), with
`report.CanStart` true only if no check is both failed and a `Blocker`.

Checks actually performed, in order: event/career/vehicle data presence,
reputation ≥ required, chapter unlocked, vehicle lineage match (with an
explicit "bootstrap hero" carve-out for a fresh save with no registered
lineage vehicles yet), homologation/scrutineering (delegates to a
separate `ScrutineeringService.Evaluate`, itself gated on rule-set ID
matching), a fuel plan (delegates to `FuelStrategyService
.EstimateFuelForEvent`, only when `evt.requireFuelPlan` and the track has
real length/laps), loadout/transport capacity (delegates to
`EventLogisticsService.Validate`), and non-blocking `Warning`-severity
checks for recommended spares actually being packed.

This is a real, layered gate — not a single boolean. It's exactly the
"instanced event entry contract" `PIVOT-PLAN.md` already decided
`WTRL.Events` needs.

### Mobile input

`Assets/WrenchToRaceLegends/Vehicles/Input/`:
- `PlayerVehicleInput.cs` — `ControlScheme { Auto, Desktop, TiltTouch,
  TouchOnly }`, `MobileControlsActive` derived from the active scheme.
- `MobileInputMath.cs` — pure math, no Unity-object dependency beyond
  `Mathf`: `ApplyDeadzone` (normalizes past a dead zone rather than just
  clamping to zero inside it), `Shape` (exponent-curve response),
  `SpeedSensitivityScale` (linearly interpolates a steering-authority
  multiplier between two speed thresholds, floored at a minimum), and
  `ResolveTiltSteer` which composes all three: raw tilt → gain → deadzone
  → shape → speed-scale, each step clamped so an extreme input at any
  stage can't blow past ±1.
- `MobileTouchInputBridge.cs` — a thin `MonoBehaviour` with clamped
  setters (`SetSteer`/`SetThrottle`/`SetBrake`/`SetHandbrake`) meant to be
  wired to Unity UI buttons/sliders/joysticks, plus one-shot "consume"
  methods for shift/reset requests (`ConsumeShiftUp` etc. — read-and-clear,
  so a UI button press isn't held as state across frames).
- `MobileControlOverlay.cs`, `VehicleControlPerceptibility.cs` — not read
  in full for this pass; the former is presentation (the actual on-screen
  touch UI), the latter (per its name and the project's own validation
  report) instruments whether a setup change is perceptible to the
  player, feeding the "prove the input resolution problem, cheaply"
  concern `PROJECT-MAP-UNITY-MOBILE.md`'s risk section already covers.

This is a real, already-considered mobile input design — tilt steering
with dead zone, response curve, and speed-sensitivity, decoupled from the
touch-button bridge. Worth reusing the *shape* of `MobileInputMath`
directly in `WTRL.UI`/`WTRL.Runtime` once input is built — the math has
no dependency on anything else in this project.

### Tracks / race-flow

`Assets/WrenchToRaceLegends/Racing/RaceDirector/RaceDirector.cs` is a
`MonoBehaviour` (not portable pure C# — this is exactly why
`PIVOT-PLAN.md` calls for designing `WTRL.Events`'/`WTRL.Racing`'s
race-flow fresh rather than reusing this). Its state machine, from
`Core/GameTypes.cs`:

```
RaceState { Inactive, Loading, Staging, Countdown, Racing, Finishing, Results, Complete }
```

`RaceDirector` exposes `BeginRace(raceId)`, `CanBeginRace(out reason)`,
`OnCompetitorCheckpoint(RaceCompetitor)`, `CompleteResults()`, and
`event Action<RaceState> StateChanged` / `event Action<RaceCompetitor>
CompetitorFinished` for external systems (UI, audio) to react to. This
8-state shape (versus a naive 3-state Inactive/Racing/Finished) is
real, useful design — it separately accounts for asset loading, a staging
period before the countdown, a finishing grace period, and a results
display period, each independently observable. **Recommend reusing this
state shape for `WTRL.Events`' instanced-event lifecycle**, adapted from
a `MonoBehaviour` to a pure C# state machine `WTRL.Runtime` drives.

`Racing/Tracks/TrackDefinition.cs` (98 lines, not read in full) and
`TrackEngineeringIntelligence.cs` exist alongside it — track data and
what's presumably a route/racing-line analysis system. Not audited in
depth this pass; worth a closer read before `WTRL.World`'s track
representation is finalized.

### Editor content-builder pattern

`Assets/WrenchToRaceLegends/Editor/` — confirmed real `[MenuItem(...)]`
Unity editor menu commands, e.g.:
```csharp
[MenuItem("Tools/Wrench to Race Legends/Rev10/Build First Playable")]
public static void Build()
[MenuItem("Tools/Wrench to Race Legends/Rev10/Rebuild + Validate First Playable")]
```
in `Rev10FirstPlayableBuilder.cs`, plus separate `*Validator.cs` files per
content area (`Rev10ContentValidator`, `Rev14RivalContentValidator`,
`VehicleLabValidator`, `VerticalSliceValidator`, `StableAssetIdentity
Validator`, `TrackEngineeringProfiler`) and generator files
(`Rev14LivingCareerBuilder`, `VehicleLabBuilder`, `VerticalSliceBuilder`).
This confirms the pattern `PIVOT-PLAN.md` already flagged as worth
reusing: procedural content generation and validation both live as
editor-only tooling, invoked by hand from a menu, separate from runtime
code. Worth doing the same for `WTRL.World`'s hub-content builder and
`WTRL.Events`' instanced-event content once those exist.

## What's under `Prototype~/` (never compiled — reference only)

Not re-audited file-by-file this pass (the brief's step 2 was partially
covered by this same project's earlier `WHERE-WE-ARE.md` finding, already
cited in `PIVOT-PLAN.md`'s decision #2). Restating the load-bearing fact
plainly since it's easy to forget while skimming this project: **any file
under a path containing `Prototype~/` was never compiled, never ran, and
none of its claims about "what's implemented" are true of the shipping
game.** The same applies to `racinggame/code/prototype/*.cs` (this
session's earlier merge confirmed it's the same never-compiled content,
just also mirrored into the research corpus).

## Recommendations for `WTRL.World`/`WTRL.Events`

1. **Port the `RaceState` 8-state shape**, not the 3-state naive version,
   for `WTRL.Events`' instanced-event lifecycle. It already accounts for
   asset loading and a results-display period, both of which a mobile
   hub-world-plus-instanced-events structure genuinely needs (loading a
   separate instanced scene takes real time; a results screen needs to
   hold state after the race itself ends).
2. **Design `WTRL.Events`' entry gate directly against
   `EventPreflightService.Evaluate`'s check list** — reputation, chapter,
   lineage, homologation, fuel, loadout/spares — as the concrete contract
   for what "can this event start" means, adapted to whatever
   `WTRL.Career`/`WTRL.Garage` end up calling their equivalent data
   (`CareerData`/`GarageData`/`VehicleInstanceData` here don't exist in
   the new project's types yet).
3. **Port `MobileInputMath` close to verbatim** when `WTRL.UI`/
   `WTRL.Runtime` build player input — it's pure math, already correct,
   and has no dependency on anything specific to Rev16.1's content.
4. **Adopt the editor-menu content-builder pattern** for `WTRL.World`'s
   hub scene and whatever generates instanced-event scenes, mirroring
   `Rev10FirstPlayableBuilder`'s `[MenuItem]` + separate validator split.
5. Read `TrackDefinition.cs`/`TrackEngineeringIntelligence.cs` in full
   before finalizing `WTRL.World`'s track representation — not done this
   pass, flagged as open.

## What I found that looked off (verification discipline, per the brief)

Nothing in the four areas actually read contradicted its own revision
report's claims this pass — `EventPreflightService`, `MobileInputMath`,
and the editor `[MenuItem]` pattern are exactly as `CURRENT-PRODUCT-TRUTH
.md`/`REV16_IMPLEMENTATION_REPORT.md` describe them. The one substantive
caveat is structural, not a bug: `RaceDirector` being a `MonoBehaviour`
means none of it can be unit-tested or reused outside a live Unity scene
context the way `WTRL.Vehicle`/`WTRL.Racing`/`WTRL.Garage`/`WTRL.Lab`'s
pure-C# ports have been — which is exactly why this audit recommends
porting its *state shape*, not its code.
