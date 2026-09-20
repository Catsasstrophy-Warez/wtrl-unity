using System;
using System.Collections.Generic;
using System.Linq;

namespace WTRL.Events
{
    // Ported from SwiftRacer/Sources/WTRLCore/Runtime/RaceRuntime.swift.
    // This is the real, working race-flow state machine PIVOT-PLAN.md
    // pointed to as the correct reference -- explicitly NOT the stale
    // RaceDisciplineRuntime/TrackGeometryRuntime lineage rejected across
    // several external uploads this project absorbed (see
    // RaceDefinition.cs's header comment). 4 phases here
    // (staged/countdown/running/finished); the archived Rev16.1 project's
    // RaceDirector uses an 8-state shape (see Assignments/OUTPUT-
    // Rev16.1-Audit.md) that adds Loading and Results phases around this
    // core -- worth layering on top in WTRL.Runtime once scene-loading
    // and a results-display flow actually exist, rather than widening
    // this already-real, already-tested state machine speculatively now.

    public enum RacePhase { Staged, Countdown, Running, Finished }

    public enum RacePenaltyKind { FalseStart, MissedCheckpoint, Shortcut, Collision }

    public sealed class RacePenalty
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public RacePenaltyKind Kind { get; }
        public double Seconds { get; }

        public RacePenalty(RacePenaltyKind kind, double seconds)
        {
            Kind = kind;
            Seconds = seconds;
        }
    }

    public sealed class RaceRuntimeState
    {
        public string RaceId { get; }
        public RacePhase Phase { get; set; } = RacePhase.Staged;
        public int Lap { get; set; }
        public int Sector { get; set; }
        public double Elapsed { get; set; }
        public double CurrentLapStart { get; set; }
        public double? BestLap { get; set; }
        public List<double> LapTimes { get; } = new();
        public List<double> SectorTimes { get; } = new();
        public List<RacePenalty> Penalties { get; } = new();
        public bool FalseStart { get; set; }
        public double CountdownRemaining { get; set; }

        public RaceRuntimeState(string raceId)
        {
            RaceId = raceId;
        }

        public double ClassifiedTime => Elapsed + Penalties.Sum(p => p.Seconds);
    }

    public static class RaceRules
    {
        public static void BeginCountdown(RaceRuntimeState state, double seconds = 3)
        {
            state.Phase = RacePhase.Countdown;
            state.CountdownRemaining = Math.Max(0, seconds);
            state.Elapsed = 0;
        }

        public static void Start(RaceRuntimeState state)
        {
            state.Phase = RacePhase.Running;
            state.Elapsed = 0;
            state.CurrentLapStart = 0;
            state.CountdownRemaining = 0;
        }

        public static void Advance(RaceRuntimeState state, double dt)
        {
            var safe = Math.Max(0, dt);
            if (state.Phase == RacePhase.Countdown)
            {
                state.CountdownRemaining = Math.Max(0, state.CountdownRemaining - safe);
                if (state.CountdownRemaining == 0) Start(state);
            }
            else if (state.Phase == RacePhase.Running)
            {
                state.Elapsed += safe;
            }
        }

        public static void RegisterFalseStart(RaceRuntimeState state)
        {
            state.FalseStart = true;
            state.Penalties.Add(new RacePenalty(RacePenaltyKind.FalseStart, 2));
        }

        public static void CompleteSector(RaceRuntimeState state)
        {
            if (state.Phase != RacePhase.Running) return;
            state.Sector += 1;
            state.SectorTimes.Add(state.Elapsed);
        }

        public static void CompleteLap(RaceRuntimeState state, RaceDefinition race)
        {
            if (state.Phase != RacePhase.Running) return;
            var lapTime = state.Elapsed - state.CurrentLapStart;
            if (lapTime > 0)
            {
                state.LapTimes.Add(lapTime);
                state.BestLap = Math.Min(state.BestLap ?? lapTime, lapTime);
            }
            state.CurrentLapStart = state.Elapsed;
            state.Sector = 0;
            state.Lap += 1;
            if (state.Lap >= race.Laps) state.Phase = RacePhase.Finished;
        }
    }

    public readonly struct DragSplit
    {
        public readonly string Label;
        public readonly double DistanceM;
        public readonly double Elapsed;
        public readonly double SpeedMps;

        public DragSplit(string label, double distanceM, double elapsed, double speedMps)
        {
            Label = label;
            DistanceM = distanceM;
            Elapsed = elapsed;
            SpeedMps = speedMps;
        }
    }

    public sealed class DragRaceRuntimeState
    {
        public bool Staged { get; set; }
        public bool Green { get; set; }
        public double? ReactionTime { get; set; }
        public double Elapsed { get; set; }
        public double DistanceM { get; set; }
        public List<DragSplit> Splits { get; } = new();
        public bool Finished { get; set; }
    }

    public static class DragRaceRules
    {
        public static readonly IReadOnlyList<(string Label, double DistanceM)> SplitDistances = new (string, double)[]
        {
            ("60 ft", 18.288),
            ("330 ft", 100.584),
            ("1/8 mile", 201.168),
            ("1000 ft", 304.8),
            ("1/4 mile", 402.336),
        };

        public static void Stage(DragRaceRuntimeState state) => state.Staged = true;

        public static void Green(DragRaceRuntimeState state, double reactionTime)
        {
            state.Green = true;
            state.ReactionTime = Math.Max(0, reactionTime);
        }

        public static void Advance(DragRaceRuntimeState state, double speedMps, double dt)
        {
            if (!state.Green || state.Finished) return;
            var safeDt = Math.Max(0, dt);
            state.Elapsed += safeDt;
            state.DistanceM += Math.Max(0, speedMps) * safeDt;

            foreach (var (label, distance) in SplitDistances)
            {
                if (state.DistanceM >= distance && !state.Splits.Any(s => s.Label == label))
                {
                    state.Splits.Add(new DragSplit(label, distance, state.Elapsed, speedMps));
                    if (distance >= 402.336) state.Finished = true;
                }
            }
        }
    }
}
