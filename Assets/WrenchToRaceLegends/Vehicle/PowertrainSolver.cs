using System;

namespace WTRL.Vehicle
{
    // Ported from SwiftRacer/Sources/WTRLCore/Simulation/PowertrainSolver.swift.

    public struct ShiftCommand
    {
        public bool Upshift;
        public bool Downshift;

        public ShiftCommand(bool upshift = false, bool downshift = false)
        {
            Upshift = upshift;
            Downshift = downshift;
        }
    }

    /// <summary>Ported from Swift's <c>ShiftController.ExperimentMode</c> enum
    /// with an associated value (<c>case fixedGear(Int)</c>). C# enums can't
    /// carry payloads, so this is a small discriminated-union-style struct
    /// instead — <see cref="Normal"/> is the default/normal case,
    /// <see cref="FixedGear"/> constructs the fixed-gear-lock case.</summary>
    public readonly struct ShiftExperimentMode : IEquatable<ShiftExperimentMode>
    {
        public readonly bool IsFixedGear;
        public readonly int FixedGearNumber;

        private ShiftExperimentMode(bool isFixedGear, int fixedGearNumber)
        {
            IsFixedGear = isFixedGear;
            FixedGearNumber = fixedGearNumber;
        }

        public static readonly ShiftExperimentMode Normal = new ShiftExperimentMode(false, 0);
        public static ShiftExperimentMode FixedGear(int gear) => new ShiftExperimentMode(true, gear);

        public bool Equals(ShiftExperimentMode other) => IsFixedGear == other.IsFixedGear && FixedGearNumber == other.FixedGearNumber;
        public override bool Equals(object? obj) => obj is ShiftExperimentMode other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(IsFixedGear, FixedGearNumber);
    }

    public static class ShiftController
    {
        public static void Update(ref VehicleSimState state, TransmissionDefinition transmission,
            EngineDefinition engine, double throttle, ShiftCommand command = default,
            ShiftExperimentMode experimentMode = default, double dt = 0)
        {
            if (transmission.Ratios.Count == 0) return;
            state.Gear = Math.Max(1, Math.Min(transmission.Ratios.Count, state.Gear));

            if (experimentMode.IsFixedGear)
            {
                state.Gear = Math.Max(1, Math.Min(transmission.Ratios.Count, experimentMode.FixedGearNumber));
                state.Drivetrain.ShiftTimer = 0;
                state.Drivetrain.ShiftDuration = 0;
                state.Drivetrain.ClutchEngagement = 1;
                state.Drivetrain.ShiftMode = ShiftMode.Manual;
                return;
            }

            if (state.Drivetrain.ShiftTimer > 0)
            {
                state.Drivetrain.ShiftTimer = Math.Max(0, state.Drivetrain.ShiftTimer - dt);
                var progress = 1 - state.Drivetrain.ShiftTimer / Math.Max(0.001, state.Drivetrain.ShiftDuration);
                if (transmission.Kind == TransmissionKind.DualClutch)
                {
                    if (state.Drivetrain.ActiveClutch == 0)
                    {
                        state.Drivetrain.ClutchAEngagement = Math.Max(0, 1 - progress);
                        state.Drivetrain.ClutchBEngagement = Math.Min(1, progress);
                    }
                    else
                    {
                        state.Drivetrain.ClutchBEngagement = Math.Max(0, 1 - progress);
                        state.Drivetrain.ClutchAEngagement = Math.Min(1, progress);
                    }
                    state.Drivetrain.ClutchEngagement = Math.Max(state.Drivetrain.ClutchAEngagement, state.Drivetrain.ClutchBEngagement);
                    if (state.Drivetrain.ShiftTimer == 0) state.Drivetrain.ActiveClutch = 1 - state.Drivetrain.ActiveClutch;
                }
                else
                {
                    state.Drivetrain.ClutchEngagement = Math.Max(0.05, progress);
                }
                return;
            }

            state.Drivetrain.ClutchEngagement = 1;
            if (transmission.Kind == TransmissionKind.DualClutch)
            {
                state.Drivetrain.PreselectedGear = Math.Min(transmission.Ratios.Count, state.Gear + 1);
                if (state.Drivetrain.ActiveClutch == 0)
                {
                    state.Drivetrain.ClutchAEngagement = 1;
                    state.Drivetrain.ClutchBEngagement = 0;
                }
                else
                {
                    state.Drivetrain.ClutchAEngagement = 0;
                    state.Drivetrain.ClutchBEngagement = 1;
                }
            }

            var autoUp = state.EngineRpm > engine.RedlineRpm * (throttle > 0.75 ? 0.92 : 0.82);
            var autoDown = state.EngineRpm < Math.Max(engine.IdleRpm * 1.8, engine.RedlineRpm * 0.34) && throttle > 0.18;

            int target;
            if (command.Upshift || (state.Drivetrain.ShiftMode == ShiftMode.Automatic && autoUp))
            {
                target = Math.Min(transmission.Ratios.Count, state.Gear + 1);
            }
            else if (command.Downshift || (state.Drivetrain.ShiftMode == ShiftMode.Automatic && autoDown))
            {
                target = Math.Max(1, state.Gear - 1);
            }
            else
            {
                return;
            }

            if (target == state.Gear) return;
            state.Gear = target;
            state.Drivetrain.ShiftDuration = transmission.ShiftDuration;
            state.Drivetrain.ShiftTimer = state.Drivetrain.ShiftDuration;
            state.Drivetrain.ClutchEngagement = 0.05;
        }
    }

    public static class PowertrainSolver
    {
        public static double UpdateEngineSpeed(ref VehicleSimState state, EngineDefinition engine,
            TransmissionDefinition transmission, double drivenOmega, double throttle, double dt)
        {
            var gi = Math.Max(0, Math.Min(transmission.Ratios.Count - 1, state.Gear - 1));
            var ratio = transmission.Ratios[gi] * transmission.FinalDrive;
            var coupledRpm = Math.Abs(drivenOmega) * 60 / (2 * Math.PI) * ratio;
            var freeTarget = engine.IdleRpm + Math.Max(0, throttle) * (engine.RedlineRpm - engine.IdleRpm) * 0.72;
            var coupling = Math.Max(0, Math.Min(1, state.Drivetrain.ClutchEngagement));
            var target = Math.Max(engine.IdleRpm,
                Math.Min(engine.RedlineRpm, freeTarget * (1 - coupling) + coupledRpm * coupling));
            var response = coupling > 0.8 ? 18.0 : 7.0;
            state.EngineRpm += (target - state.EngineRpm) * Math.Min(1, response * dt);
            state.EngineRpm = Math.Max(engine.IdleRpm, Math.Min(engine.RedlineRpm, state.EngineRpm));
            state.Drivetrain.InputShaftRpm += (state.EngineRpm - state.Drivetrain.InputShaftRpm) * Math.Min(1, (6 + coupling * 14) * dt);
            state.Drivetrain.OutputShaftRpm = ratio > 0 ? state.Drivetrain.InputShaftRpm / Math.Max(0.001, transmission.Ratios[gi]) : 0;
            return ratio;
        }
    }
}
