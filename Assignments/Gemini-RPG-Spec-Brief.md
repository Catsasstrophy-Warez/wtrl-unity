# Assignment for Gemini: read the RPG spec and design `WTRL.RPG`

## Context (you have no memory of prior sessions on this project — read this in full)

"Wrench to Race Legends" (WTRL) is a mobile racing game that has just
expanded scope into a full open-world action/RPG/sim/racing game, and
pivoted from Swift/RealityKit to Unity. A new Unity project
(`WTRL-Unity/`) has a 15-assembly structure already scaffolded (see
`WTRL-Unity/PIVOT-PLAN.md` if you have access to it), including a
`WTRL.RPG` assembly at `Assets/WrenchToRaceLegends/RPG/` that currently
contains only an empty `.asmdef` file — **no RPG system exists yet
anywhere in the codebase.** This is the one module with no prior Swift
implementation to port; it has to be designed from research and reference
material, which is your task.

Development is split across three AI assistants — Claude, ChatGPT, and
Gemini (you) — each owning different modules. You own `WTRL.RPG`,
`WTRL.Career`, and `WTRL.UI` going forward. This assignment is the
onboarding read before any of that module code gets written.

## What already exists around the RPG layer (for context, not as your source)

The existing game has: seven hero-car generations, six persistent AI
rivals with a real "intimidation" system that ramps based on the
player's win/loss history against each rival individually (not a global
difficulty rating), a reputation stat that gates content, a
license/skill-test system, and a mentor-commentary system tied to
reputation tier. None of that is RPG-system code as such — it's the
existing simulation/career layer your new RPG systems need to sit
alongside and eventually integrate with (via `WTRL.Career`, which
already depends on both `WTRL.RPG` and `WTRL.Racing`/`WTRL.Events`/
`WTRL.Garage` in the assembly graph).

## Your source material

1. **The primary spec**: `racinggame/05-specifications/48-RPG-SYSTEMS-SPEC.md`
   (path relative to the project's research corpus — ask the project
   owner for this file's contents if you don't have direct file access).
   This is the actual design document for RPG systems in this game.
2. **Reference-only prior exploration**: inside the archived Rev16.1
   Unity project (`Desktop/racing game/archive/unity-revisions/
   WrenchToRaceLegends_Unity6_Rev16.1_FixesReapplied.zip`), there is an
   `Assets/WrenchToRaceLegends/Prototype~/RPG/` folder. **The trailing
   `~` means Unity excludes this folder from compilation — this code
   never compiled, never ran, and is not verified in any way.** Read it
   only as a record of prior design thinking to react to (agree, disagree,
   or improve on), never as working code to port or trust as-is. A prior
   audit of this exact project (`WHERE-WE-ARE.md`) found that treating
   `Prototype~` content as if it were real, working code was a genuine
   mistake made earlier in this project's history — don't repeat it.
3. If useful for tone/genre grounding: `racinggame/05-specifications/
   20-CONCEPTS.md` (overall systems concept doc) and `racinggame/
   08-trends-and-cross-genre/` (genre research, including a Royal Match
   nine-systems analysis — cross-genre progression-system precedent).

## Your task

1. Read `48-RPG-SYSTEMS-SPEC.md` in full and extract the actual design:
   what stats/skills/progression axes it specifies, how it's meant to
   interact with the existing reputation/rival-intimidation/license
   systems, and what's explicitly out of scope.
2. Read `Prototype~/RPG/` as reference-only prior thinking. Note where it
   agrees or usefully extends the spec, and where you'd deliberately
   diverge (say why).
3. Design `WTRL.RPG` as an actual Unity C# module: what types/classes it
   needs (stats, skill trees, quest/dialogue state if the spec calls for
   it, save-relevant data structures), and how `WTRL.Career` should
   consume it (a public API sketch — method/property signatures, not full
   implementations yet).
4. Consider mobile constraints explicitly: this needs to be a phone game.
   Flag anything in the spec or in `Prototype~/RPG/` that reads as
   PC/console-scale RPG complexity (deep dialogue trees, huge skill webs)
   and needs scoping down, versus what's appropriately sized already.

## What to produce

A single markdown design document,
**`Assignments/OUTPUT-Gemini-RPG-Design.md`** in this same `WTRL-Unity/`
folder (create the file at that exact path so it lands where the other
assistants — and the project owner — will look for it), containing:

- A summary of what `48-RPG-SYSTEMS-SPEC.md` actually specifies (so
  anyone reading your doc doesn't have to separately re-read the spec to
  follow your reasoning).
- What from `Prototype~/RPG/` is worth keeping as a design idea, clearly
  labeled as "never-compiled reference, not verified code."
- A concrete proposed structure for `WTRL.RPG`: the core types, their
  relationships, and a first-draft public API that `WTRL.Career` could
  build against (this becomes the seed for that assembly's
  `CONTRACT.md`).
- Explicit mobile-scoping calls: what's in v1, what's deferred.
- Open questions you couldn't resolve from the spec alone, for the
  project owner to answer.

Do not write the actual `WTRL.RPG` C# implementation yet — this is a
design and reporting task. Implementation follows once this design exists
and the project owner (or Claude, coordinating the merge) has reviewed it.
