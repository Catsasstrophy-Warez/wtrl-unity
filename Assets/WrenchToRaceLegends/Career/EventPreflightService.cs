using System.Collections.Generic;
using System.Linq;
using WTRL.Events;

namespace WTRL.Career
{
    public enum PreflightSeverity { Info, Warning, Blocker }

    public readonly struct PreflightCheck
    {
        public readonly string Code;
        public readonly PreflightSeverity Severity;
        public readonly string Message;

        public PreflightCheck(string code, PreflightSeverity severity, string message)
        {
            Code = code;
            Severity = severity;
            Message = message;
        }
    }

    /// <summary>
    /// New design, not a port: real, scoped implementation of the
    /// event-preflight gating the Rev16.1 audit recommended
    /// (`Assignments/OUTPUT-Rev16.1-Audit.md`) and every subsequent
    /// CONTRACT.md flagged as "real, valuable, explicitly NOT done."
    ///
    /// DELIBERATELY SCOPED DOWN from Rev16.1's full gate. That project's
    /// `EventPreflightService.Evaluate()` checked reputation, chapter,
    /// lineage, homologation/scrutineering, fuel plan, and loadout/
    /// transport — but chapter, lineage, homologation, fuel, and loadout
    /// are not systems that exist anywhere in this project. Building
    /// checks against data that doesn't exist would mean either
    /// inventing those systems unscoped (a much bigger task than "wire
    /// up preflight gating") or faking checks against fields that don't
    /// mean anything yet. Neither is honest. This implementation checks
    /// only what real data already backs: reputation
    /// (<see cref="RaceDefinition.ReputationRequired"/> vs
    /// <see cref="CareerState.Reputation"/>) and Safety Rating/license
    /// gating for knockout entry (<see cref="RPG.DriverLicenseState
    /// .PermitsKnockoutEntry"/>). Extending this to the other five axes
    /// is real future work, gated on those systems existing first — not
    /// on this method's shape.
    /// </summary>
    public static class EventPreflightService
    {
        public static IReadOnlyList<PreflightCheck> Evaluate(RaceDefinition race, CareerState state)
        {
            var checks = new List<PreflightCheck>();

            if (state.Reputation < race.ReputationRequired)
            {
                checks.Add(new PreflightCheck("reputation", PreflightSeverity.Blocker,
                    $"Requires {race.ReputationRequired} reputation (have {state.Reputation})."));
            }

            if (race.Format == RaceFormat.Knockout && !state.DriverLicense.PermitsKnockoutEntry(state.SafetyRating))
            {
                checks.Add(new PreflightCheck("safety-rating", PreflightSeverity.Blocker,
                    $"Safety Rating {state.SafetyRating.Rating:F0} is below the minimum required for knockout " +
                    $"entry at {state.DriverLicense.Grade} grade."));
            }

            foreach (var rivalId in race.RivalIds)
            {
                var memory = state.RivalBehavior.Memory(rivalId);
                if (memory.WinsAgainstPlayer > memory.LossesToPlayer && memory.Encounters > 0)
                {
                    checks.Add(new PreflightCheck($"rival-warning-{rivalId}", PreflightSeverity.Info,
                        $"{rivalId} has beaten you more than you've beaten them — expect an intimidation edge."));
                }
            }

            return checks;
        }

        public static bool CanEnter(RaceDefinition race, CareerState state) =>
            !Evaluate(race, state).Any(c => c.Severity == PreflightSeverity.Blocker);
    }
}
