using System.Collections.Generic;
using WTRL.Garage;

namespace WTRL.Career
{
    // Ported from SwiftRacer/Sources/WTRLCore/Persistence/CareerTransaction.swift.
    //
    // DEVIATION: the Swift original had 7 command cases; this port has 6.
    // `.recordEvidence(TestRunEvidence)` is deliberately NOT ported —
    // `TestRunEvidence` lives in `WTRL.Lab`, and `WTRL.Career`'s asmdef
    // has no dependency on `WTRL.Lab` (per PIVOT-PLAN.md's module graph).
    // This is the same "where does the type live" question BuildRecipe
    // already resolved once for WTRL.RPG/WTRL.Garage — flagged here as an
    // open question rather than silently dropped or resolved by adding an
    // asmdef dependency unilaterally. See CONTRACT.md.

    public enum CareerCommandKind { Earn, Spend, AcquirePart, Install, CompleteRace, RecordHistory }

    /// <summary>A discriminated-union-style command, matching the pattern
    /// already used for <c>WTRL.Vehicle.ShiftExperimentMode</c> since C#
    /// enums can't carry payloads. Construct via the static factory
    /// methods, not the fields directly.</summary>
    public readonly struct CareerCommand
    {
        public readonly CareerCommandKind Kind;
        public readonly int Money;
        public readonly int Reputation;
        public readonly string? PartId;
        public readonly string? VehicleId;
        public readonly InstalledComponent Component;
        public readonly string? RaceId;
        public readonly double Time;
        public readonly VehicleHistoryEvent? HistoryEvent;

        private CareerCommand(CareerCommandKind kind, int money = 0, int reputation = 0, string? partId = null,
            string? vehicleId = null, InstalledComponent component = default, string? raceId = null,
            double time = 0, VehicleHistoryEvent? historyEvent = null)
        {
            Kind = kind;
            Money = money;
            Reputation = reputation;
            PartId = partId;
            VehicleId = vehicleId;
            Component = component;
            RaceId = raceId;
            Time = time;
            HistoryEvent = historyEvent;
        }

        public static CareerCommand Earn(int money, int reputation) => new(CareerCommandKind.Earn, money: money, reputation: reputation);
        public static CareerCommand Spend(int amount) => new(CareerCommandKind.Spend, money: amount);
        public static CareerCommand AcquirePart(string partId) => new(CareerCommandKind.AcquirePart, partId: partId);
        public static CareerCommand Install(string vehicleId, InstalledComponent component) =>
            new(CareerCommandKind.Install, vehicleId: vehicleId, component: component);
        public static CareerCommand CompleteRace(string raceId, double time) =>
            new(CareerCommandKind.CompleteRace, raceId: raceId, time: time);
        public static CareerCommand RecordHistory(string vehicleId, VehicleHistoryEvent evt) =>
            new(CareerCommandKind.RecordHistory, vehicleId: vehicleId, historyEvent: evt);
    }

    /// <summary>Applies a batch of commands atomically: either every
    /// command in the batch succeeds and <paramref name="state"/> is
    /// updated to reflect all of them, or any single failure (currently
    /// only <see cref="CareerCommandKind.Spend"/> can fail, on
    /// insufficient funds) means NONE of the batch's effects are applied
    /// — <paramref name="state"/> is left completely untouched. This
    /// atomicity is the actual value of this type; the individual command
    /// handlers themselves are simple.</summary>
    public static class CareerTransaction
    {
        public static bool Apply(IReadOnlyList<CareerCommand> commands, CareerState state)
        {
            var candidate = state.Clone();
            foreach (var command in commands)
            {
                switch (command.Kind)
                {
                    case CareerCommandKind.Earn:
                        candidate.Money += command.Money;
                        candidate.Reputation += command.Reputation;
                        break;
                    case CareerCommandKind.Spend:
                        if (command.Money < 0 || candidate.Money < command.Money) return false;
                        candidate.Money -= command.Money;
                        break;
                    case CareerCommandKind.AcquirePart:
                        candidate.OwnedPartIds.Add(command.PartId!);
                        break;
                    case CareerCommandKind.Install:
                    {
                        if (!candidate.OwnedPartIds.Contains(command.Component.PartId)) return false;
                        if (!candidate.InstalledComponents.TryGetValue(command.VehicleId!, out var items))
                        {
                            items = new List<InstalledComponent>();
                            candidate.InstalledComponents[command.VehicleId!] = items;
                        }
                        items.RemoveAll(c => c.Slot == command.Component.Slot);
                        items.Add(command.Component);
                        break;
                    }
                    case CareerCommandKind.CompleteRace:
                    {
                        candidate.CompletedRaceIds.Add(command.RaceId!);
                        var existing = candidate.RaceRecords.TryGetValue(command.RaceId!, out var t) ? t : command.Time;
                        candidate.RaceRecords[command.RaceId!] = System.Math.Min(existing, command.Time);
                        break;
                    }
                    case CareerCommandKind.RecordHistory:
                    {
                        if (!candidate.VehicleHistory.TryGetValue(command.VehicleId!, out var events))
                        {
                            events = new List<VehicleHistoryEvent>();
                            candidate.VehicleHistory[command.VehicleId!] = events;
                        }
                        events.Add(command.HistoryEvent!);
                        break;
                    }
                }
            }

            state.CopyFrom(candidate);
            return true;
        }
    }
}
