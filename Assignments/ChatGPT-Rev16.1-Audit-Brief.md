# Assignment for ChatGPT: audit the Rev16.1 Unity codebase

## Context (you have no memory of prior sessions on this project — read this in full)

"Wrench to Race Legends" (WTRL) is a mobile racing/RPG/sim game. The
project has just pivoted to a fresh Unity project (`WTRL-Unity/`) after
previously being built in Swift/RealityKit (`SwiftRacer/`, which is being
ported over, not discarded). Development is now split across three AI
assistants — Claude, ChatGPT (you), and Gemini — each owning different
modules of the new Unity project. You are being onboarded with a reading
task before writing any code, because the new project's world/event
architecture should be informed by what already exists rather than
designed blind.

There is an **archived, previously-built Unity codebase** relevant to
your future module ownership (`WTRL.World` and `WTRL.Events` — hub-world
streaming and instanced-event lifecycle) sitting at:

`Desktop/racing game/archive/unity-revisions/WrenchToRaceLegends_Unity6_Rev16.1_FixesReapplied.zip`

This is a real git repository (has its own `.git` folder), Unity
6000.0.58f2, 12 assemblies, ~108 C# files. It is **not** being resumed —
the new project starts clean (see `WTRL-Unity/PIVOT-PLAN.md` in this same
handoff if you have access to it) — but its architecture and validated
systems are the reference baseline for the new `WTRL.World`/`WTRL.Events`
modules.

**Important scope note already established**: this Unity project has a
`Assets/WrenchToRaceLegends/Prototype~/` folder. Unity excludes any folder
ending in `~` from compilation — that code is NOT part of the working
game, it never compiled, and none of its claims about "what's implemented"
are true of the shipped codebase. A prior audit (`WHERE-WE-ARE.md`, in the
same Desktop research folder) found that the project's own status
documents had been grading the wrong codebase for this exact reason.
**Do not attribute anything under `Prototype~/` to the real, compiling
game** in your audit — call it out separately if it's relevant reference
material, clearly labeled as never-compiled.

## What "compiles and drives" means here

The project's own `REV16_VALIDATION_REPORT.md` explicitly states Unity
6000.0.58f2 was not installed in whatever environment produced that
report, so its "PASS" results are structural (brace matching, GUID/meta
integrity, assembly-cycle checks, declared-test counts via regex) — not
an actual compile or test run. Treat every claim in the archive's own
`*_IMPLEMENTATION_REPORT.md` / `*_VALIDATION_REPORT.md` files the same
way: verify by reading the actual `.cs` source, not by trusting the
report's own summary.

## Your task

1. Extract the zip. Confirm what actually exists vs. what's under
   `Prototype~/` (excluded, never compiled).
2. Read the real (compiling) code under `Assets/WrenchToRaceLegends/`
   in these assemblies specifically, since they're your future ownership:
   - `Runtime/` — especially `EventPreflightService.cs`,
     `GameFlowController.cs`, `ScrutineeringService.cs` (event-entry
     gating: reputation/chapter/lineage/homologation/fuel/loadout checks).
   - `Vehicles/Input/` — `MobileInputMath.cs`, `MobileTouchInputBridge.cs`,
     `MobileControlOverlay.cs`, `PlayerVehicleInput.cs` (tilt+touch
     control scheme — relevant to how the player interacts with the hub
     world and instanced events).
   - `Racing/Tracks/`, `Racing/RaceDirector/` — whatever track/route and
     race-flow representation already exists.
   - `Editor/` — specifically the `Rev10FirstPlayableBuilder.cs`-style
     content-generation pattern (`Tools > Wrench to Race Legends > ...`
     editor menu that procedurally builds/validates a scene) — this
     pattern is explicitly called out as worth reusing for hub-world and
     instanced-event content builders in the new project.
3. Also skim (read the `.md` files, not the code) the top-level revision
   reports — `CURRENT-PRODUCT-TRUTH.md`, `REV16_IMPLEMENTATION_REPORT.md`,
   `ROADMAP-REV15-FORWARD.md` — for design intent and known limitations,
   cross-checking a handful of specific claims against the actual source
   rather than accepting the summary wholesale (same discipline as step 2).

## What to produce

A single markdown report, **`Assignments/OUTPUT-ChatGPT-Rev16.1-Audit.md`**
in this same `WTRL-Unity/` folder (create the file at that exact path so
it lands where the other assistants — and the project owner — will look
for it), containing:

- **What's real and verified-by-reading** in each of the four areas above
  (event preflight, mobile input, tracks/race-flow, editor content
  builders) — summarize the actual mechanism, not just "it exists."
  Include exact file paths and key type/method names so this is
  independently checkable, not just your paraphrase.
- **What's under `Prototype~/`** that looks relevant to `WTRL.World`/
  `WTRL.Events` even though it never compiled — flag it as reference-only
  design intent, not working code.
- **Specific, concrete recommendations** for how the new `WTRL.World` and
  `WTRL.Events` assemblies (in `WTRL-Unity/Assets/WrenchToRaceLegends/`)
  should adapt these patterns for a hub-world-plus-instanced-events
  structure (one persistent streamed hub, races/missions/encounters as
  separate instanced scenes entered/exited through a gated preflight
  check — this part of the architecture is already decided, don't
  re-litigate it, just design within it).
- Anything you found that looks like a real bug, dead code, or a claim in
  the revision reports that didn't hold up when you checked the actual
  source — call these out explicitly, the same verification discipline
  this whole project has been held to elsewhere.

Do not write any C# for the new project yet — this is a reading and
reporting task. Module ownership and actual porting come after this
report exists.
