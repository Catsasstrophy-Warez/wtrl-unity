# WTRL-Unity — Project Status

**Edited in place each session. Never appended to.** For the running
history, see `CHANGELOG.md`. For architecture/decisions, see
`PIVOT-PLAN.md`. This file answers one question: what state is this
project actually in, right now, honestly.

Last updated: 2026-09-20.

## The one-sentence version

**This is a real, tested, compiling simulation core and a working
vertical-slice scene — not a feature-complete game.** By actual content
volume, it is closer to a tech demo than a shippable product. That is
not a criticism of the engineering (the core is genuinely solid); it is
a statement about how much authored content exists versus how much the
full design calls for.

## Test/build health (verified this session, not carried forward from memory)

- 142/142 EditMode tests, 28/28 PlayMode tests, all passing.
- 16 assemblies, 92 C# files, zero reference cycles.
- 7 real scenes (`VerticalSlice.unity` + 6 `Scenes/Tracks/*.unity`) build
  cleanly from a batchmode Editor.
- Zero self-intersecting-polygon warnings on the 2 accepted vehicle
  models (fixed this session; was 17).
- **No visual/screenshot QA has ever been possible in this environment.**
  Every verification above is compile passes, automated test assertions,
  or direct inspection of saved scene/asset files (GUIDs, shader
  keywords, texture import settings) — never a human or a screenshot.
  Treat anything visual (does it look good, does a material read
  correctly, is a mesh's silhouette right) as genuinely unverified until
  someone opens the Editor and looks.

## Real content, by the numbers

| Category | Real / Total | Note |
|---|---|---|
| Vehicles with in-game `WTRL.Content` assets | 2 / 159 researched | Hero (`hero-1965`) and rival (`marsh-gen1`) only |
| Gameplay-functional Garage parts | 3 / 1,560 researched | `PartCatalogImporter` reads the full corpus but produces inert research records, not `PartDefinition`s |
| Build recipes | 35 / 35 | Fully ported from the real Swift source — this one's actually done |
| Tracks with real Blender meshes wired into Unity | 8 / 8 documented | But only Foundry Row has full VerticalSlice-scene parity (garage/dyno/HUD/audio); the other 7 have a minimal track-only scene |
| Track/facility names that are corpus-sourced (not invented this project) | 5 / 8 | The 3 "original road-course" names and dimensions are this project's own invention, disclosed as such in code comments |
| Vehicle-art visual acceptance gates met | 2 / 10 | See `racinggame/BlenderPipeline/REFERENCE-MODELING-ACCEPTANCE.md` for the gate list; gates 1,2,5,6,7,9,10 need a human's visual judgment to ever close, not just more headless work |
| Surfaces with real PBR (normal/AO/metallic-smoothness) maps | 3 / 3 ground-level + 0 / 2 vehicles | Track asphalt, track barriers, ground grass have real maps; vehicle paint/glass/trim do not |
| Scenery beyond barrier walls (trees, buildings, poles) | 0 | Nothing exists in any scene |

## What actually works end to end right now

- A player can (in principle — never manually driven, only test-driven)
  drive `HeroVehicle` around Foundry Row Circuit under real fixed-step
  physics, with touch/tilt input, an orbit/pinch-zoom camera, and audio
  rig.
- A `MarshVehicle` AI opponent drives the same physics under
  `AiVehicleSession`, optionally with intimidation behavior (braking
  bias, defensive jitter, pass suppression) if a caller supplies
  `RivalIntimidationState` — nothing currently does (that specific
  piece is still dormant; intimidation needs a career-level loss count
  as input, which isn't wired to a live session yet).
- Completing laps (via an arcade-style start-zone-radius heuristic, not
  a real finish-line crossing) advances a real `RaceFlowController`
  through Loading → Racing → Results, shows a real `ResultsScreen`, and
  on "Continue" updates real `CareerState` (best time, completed-race
  list, a `SafetyEvent.EventCompletedZeroIncidents` credit) and saves it
  to a real file via `CareerSaveCodec`.
- Real contact/overtake/win detection now runs live between
  `HeroVehicle` and `MarshVehicle` every frame in `VerticalSlice.unity`
  (`RaceSessionController` + `ContactDetector`/`TrackProgress`/
  `OvertakeTracker`/`LapProgressTracker`), and a real win/loss/overtake
  updates `RivalMemory` and reputation on race completion — this was
  the #1 item on this file's own "what would most change the picture
  next" list as of the last update; it's done now.
- Real localization (English only), analytics (opt-out by default, one
  call site), and device-quality-tier infrastructure exist but are
  thin — each is a real, working foundation, not a finished system.

## Known process issue (fixed going forward, not retroactively)

Found **5 instances in one session** of a CONTRACT.md claiming a gap was
open when it had actually already been closed in an earlier session —
the closing work just never edited the original "known gaps" bullet.
Standing rule now documented in `CHANGELOG.md`'s header: closing a gap
must strike the original claim in place, never just append a new
"CLOSED" note further down. If you find a 6th instance, that rule isn't
being followed — say so.

## What would most change the picture next

Roughly in order of unblocking value, expanded on fully in
`CHANGELOG.md`'s most recent entries:
1. Contact/win-detection wired into a live per-frame caller (the
   detection primitives already exist and are tested — `ContactDetector`,
   `TrackProgress`, `OvertakeTracker` — nothing calls them yet).
2. Batch-converting the 1,560-entry parts research corpus into real,
   balanced `PartDefinition`s — needs human judgment on numbers, not
   just more code.
3. A human opening the Unity Editor and actually looking at any of
   this — nothing visual has been confirmed by eyes at any point in
   this project's history in this environment.
