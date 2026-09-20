using System;
using System.Collections.Generic;
using System.Linq;

namespace WTRL.Vehicle
{
    /// <summary>
    /// Ported from SwiftRacer/Sources/WTRLCore/Simulation/VehicleSimulation.swift.
    /// The 120 Hz fixed-step deterministic vehicle model: wheels, suspension,
    /// tires, engine/clutch/shift, differentials, banking assist.
    ///
    /// DELIBERATE DEVIATION FROM THE SWIFT SOURCE: Swift's <c>step</c>
    /// resolved <c>TireDefinition</c>/<c>SuspensionDefinition</c> by reaching
    /// into a global static catalog (<c>CanonicalContent.tire(...)</c> /
    /// <c>CanonicalContent.suspension(...)</c>), with a hardcoded fallback
    /// (<c>CanonicalContent.tires[1]</c>) when a lookup missed. No such
    /// catalog exists in WTRL.Vehicle — that's WTRL.World/WTRL.Runtime's
    /// job once it exists (see PIVOT-PLAN.md's content-porting map) — so
    /// this port makes <paramref name="tire"/> and
    /// <paramref name="suspension"/> required, explicit parameters instead.
    /// This is a real behavior difference: the Swift version tolerates an
    /// unresolved id with a silent fallback; this version requires the
    /// caller to have already resolved one. Treat a caller that can't
    /// produce a <see cref="TireDefinition"/>/<see cref="SuspensionDefinition"/>
    /// as a bug at the call site, not something this function should paper
    /// over the way the Swift original did.
    /// </summary>
    public static class VehicleSimulation
    {
        public const double G = 9.80665;

        public static void Step(
            ref VehicleSimState state, VehicleInput input, VehicleDefinition vehicle, EngineDefinition engine,
            TransmissionDefinition transmission, VehicleTuning tuning, TireDefinition tire,
            SuspensionDefinition suspension, SurfaceDefinition? surface = null, double bankingDegrees = 0,
            ShiftExperimentMode shiftExperimentMode = default, double dt = 0)
        {
            if (dt <= 0 || state.Wheels.Length != 4 || state.Tires.Length != 4) return;

            var mass = Math.Max(1, vehicle.MassKg);
            var radius = Math.Max(0.2, vehicle.TireRadiusM);
            var speed = state.SpeedMps;

            int[] drivenIndices = vehicle.DriveLayout switch
            {
                DriveLayout.Fwd => new[] { 0, 1 },
                DriveLayout.Rwd => new[] { 2, 3 },
                _ => new[] { 0, 1, 2, 3 },
            };
            var drivenOmega = 0.0;
            foreach (var i in drivenIndices) drivenOmega += Math.Abs(state.Wheels[i].AngularVelocityRadS);
            drivenOmega /= drivenIndices.Length;

            ShiftController.Update(ref state, transmission, engine, input.Throttle,
                new ShiftCommand(input.Upshift, input.Downshift), shiftExperimentMode, dt);

            var ratio = PowertrainSolver.UpdateEngineSpeed(ref state, engine, transmission, drivenOmega, input.Throttle, dt);
            var engineTorque = EngineSolver.TorqueNm(engine, state.EngineRpm, input.Throttle, state.Damage.EngineWear);
            var axleTorque = engineTorque * ratio * 0.86 * state.Drivetrain.ClutchEngagement;

            var previousA = state.LongitudinalAcceleration;
            var staticFront = mass * G * vehicle.StaticFrontWeightFraction;
            var transfer = mass * previousA * vehicle.CgHeightM / Math.Max(0.5, vehicle.WheelbaseM);
            var frontLoad = Math.Max(100, staticFront - transfer);
            var rearLoad = Math.Max(100, mass * G - frontLoad);

            // Banked corners let part of gravity carry the turn instead of tire
            // friction alone — see the identical comment in the Swift source
            // for the scoped-simplification rationale (no track-geometry
            // input beyond a single track-wide banking angle).
            var bankAssistMps2 = G * Math.Sin(bankingDegrees * Math.PI / 180);
            var bankAssisted = state.LateralAcceleration -
                (state.LateralAcceleration < 0 ? -1.0 : 1.0) *
                Math.Min(Math.Abs(state.LateralAcceleration), Math.Max(0, bankAssistMps2));
            var lateralTransfer = mass * bankAssisted * vehicle.CgHeightM / Math.Max(0.5, vehicle.TrackWidthM);

            var loads = new[]
            {
                Math.Max(50, frontLoad / 2 - lateralTransfer / 4),
                Math.Max(50, frontLoad / 2 + lateralTransfer / 4),
                Math.Max(50, rearLoad / 2 - lateralTransfer / 4),
                Math.Max(50, rearLoad / 2 + lateralTransfer / 4),
            };

            var leftOmega = drivenIndices.Contains(0) ? state.Wheels[0].AngularVelocityRadS : state.Wheels[2].AngularVelocityRadS;
            var rightOmega = drivenIndices.Contains(1) ? state.Wheels[1].AngularVelocityRadS : state.Wheels[3].AngularVelocityRadS;

            var isFwd = vehicle.DriveLayout == DriveLayout.Fwd;
            var leftLoad = isFwd ? loads[0] : loads[2];
            var rightLoad = isFwd ? loads[1] : loads[3];
            var surfaceGrip = surface?.DryGripMultiplier ?? 1;

            var leftMu = TireSolver.GripCoefficient(vehicle.TireGripCoefficient, leftLoad,
                state.Tires[isFwd ? 0 : 2].TemperatureC, state.Tires[isFwd ? 0 : 2].Wear,
                tuning.TirePressureGripFactor, tire, surfaceGrip);
            var rightMu = TireSolver.GripCoefficient(vehicle.TireGripCoefficient, rightLoad,
                state.Tires[isFwd ? 1 : 3].TemperatureC, state.Tires[isFwd ? 1 : 3].Wear,
                tuning.TirePressureGripFactor, tire, surfaceGrip);

            var split = DifferentialSolver.Split(axleTorque, vehicle.Differential, tuning.DifferentialLock,
                leftOmega, rightOmega, leftMu * leftLoad * radius, rightMu * rightLoad * radius);
            state.Drivetrain.LeftDriveTorqueNm = split.Left;
            state.Drivetrain.RightDriveTorqueNm = split.Right;

            var longitudinalForce = 0.0;
            var steer = input.Steering * tuning.SteeringLockRadians;

            for (var i = 0; i < 4; i++)
            {
                state.Tires[i].NormalLoadN = loads[i];
                var wheelSurface = state.Wheels[i].AngularVelocityRadS * radius;
                state.Tires[i].SlipRatio = (wheelSurface - speed) / Math.Max(0.4, Math.Abs(speed));
                state.Tires[i].SlipAngle = i < 2
                    ? steer - Math.Atan2(state.LateralSpeedMps + state.YawRate * vehicle.WheelbaseM * 0.5, Math.Max(1, Math.Abs(speed)))
                    : -Math.Atan2(state.LateralSpeedMps - state.YawRate * vehicle.WheelbaseM * 0.5, Math.Max(1, Math.Abs(speed)));

                var mu = TireSolver.GripCoefficient(vehicle.TireGripCoefficient, loads[i], state.Tires[i].TemperatureC,
                    state.Tires[i].Wear, tuning.TirePressureGripFactor, tire, surfaceGrip);
                var forces = TireSolver.CombinedForces(state.Tires[i].SlipRatio, state.Tires[i].SlipAngle, loads[i], mu, tire);
                longitudinalForce += forces.Fx;

                double drive;
                if (drivenIndices.Contains(i))
                {
                    var share = i % 2 == 0 ? split.Left : split.Right;
                    drive = share / (vehicle.DriveLayout == DriveLayout.Awd ? 2 : 1);
                }
                else
                {
                    drive = 0;
                }

                var brakeShare = i < 2 ? tuning.BrakeBiasFront / 2 : (1 - tuning.BrakeBiasFront) / 2;
                var brakeTorque = vehicle.BrakeTorqueNm * input.Brake * brakeShare * (1 - state.Brakes.Fade)
                    + (input.Handbrake && i >= 2 ? vehicle.BrakeTorqueNm * 0.2 : 0);

                WheelSolver.Update(ref state.Wheels[i], vehicle.WheelInertiaKgm2, drive, brakeTorque, forces.Fx * radius, dt);

                if (Math.Abs(speed) < 0.2 && input.Throttle == 0 && input.Brake > 0.2)
                {
                    state.Wheels[i].AngularVelocityRadS = 0;
                }

                var slipEnergy = (Math.Abs(forces.Fx * (wheelSurface - speed)) + Math.Abs(forces.Fy * state.LateralSpeedMps)) * dt;
                state.Tires[i].TemperatureC += slipEnergy / 18_000 - (state.Tires[i].TemperatureC - 25) * 0.018 * tuning.CoolingFactor * dt;
                state.Tires[i].Wear = Math.Min(1, state.Tires[i].Wear + slipEnergy / 120_000_000);
            }

            var drag = AeroSolver.DragForceN(speed, vehicle.AeroDragCoefficientArea);
            var tireRr = tire?.RollingResistance ?? 0.014;
            var rolling = AeroSolver.RollingResistanceN(speed, mass, tireRr * (surface?.RollingResistanceMultiplier ?? 1));

            var averageMu = 0.0;
            for (var i = 0; i < 4; i++)
            {
                averageMu += TireSolver.GripCoefficient(vehicle.TireGripCoefficient, loads[i], state.Tires[i].TemperatureC,
                    state.Tires[i].Wear, tuning.TirePressureGripFactor, tire, surfaceGrip);
            }
            averageMu /= 4;

            ChassisSolver.Integrate(ref state, mass, longitudinalForce, drag, rolling, steer, vehicle.WheelbaseM,
                averageMu, state.Damage.SuspensionWear, dt);

            if (speed > 0 && state.SpeedMps < 0 && input.Brake > 0) state.SpeedMps = 0;
            if (Math.Abs(state.SpeedMps) < 0.03 && input.Throttle == 0 && input.Brake > 0) state.SpeedMps = 0;

            for (var i = 0; i < 4; i++)
            {
                var springRate = i < 2 ? suspension.FrontSpringRate : suspension.RearSpringRate;
                var dampingRatio = i < 2 ? suspension.FrontDampingRatio : suspension.RearDampingRatio;
                SuspensionSolver.Update(ref state.SuspensionCorners[i], loads[i],
                    i < 2 ? staticFront / 2 : (mass * G - staticFront) / 2,
                    springRate, Math.Sqrt(springRate * mass / 4) * dampingRatio,
                    Math.Max(15, mass / 4 - vehicle.UnsprungMassKgPerWheel),
                    suspension.BumpTravelM, suspension.ReboundTravelM, dt);
            }
            SuspensionSolver.ApplyAntiRoll(state.SuspensionCorners, suspension.FrontAntiRollRate, suspension.RearAntiRollRate, dt);

            state.Suspension.FrontCompressionM = (state.SuspensionCorners[0].DisplacementM + state.SuspensionCorners[1].DisplacementM) / 2;
            state.Suspension.RearCompressionM = (state.SuspensionCorners[2].DisplacementM + state.SuspensionCorners[3].DisplacementM) / 2;
            state.Suspension.PitchRadians = Math.Max(-0.12, Math.Min(0.12, -state.LongitudinalAcceleration / G * 0.08));
            state.Suspension.RollRadians = Math.Max(-0.14, Math.Min(0.14, state.LateralAcceleration / G * 0.09));

            var brakeForce = 0.0;
            foreach (var w in state.Wheels) brakeForce += w.BrakeTorqueNm / radius;
            var brakeEnergy = Math.Abs(brakeForce * speed) * dt;
            state.Brakes.FrontTemperatureC += brakeEnergy * tuning.BrakeBiasFront / 42_000
                - (state.Brakes.FrontTemperatureC - 25) * 0.025 * tuning.CoolingFactor * dt;
            state.Brakes.RearTemperatureC += brakeEnergy * (1 - tuning.BrakeBiasFront) / 35_000
                - (state.Brakes.RearTemperatureC - 25) * 0.03 * tuning.CoolingFactor * dt;
            state.Brakes.Fade = Math.Max(0, (Math.Max(state.Brakes.FrontTemperatureC, state.Brakes.RearTemperatureC) - 500) / 500);
            state.Brakes.PadWear = Math.Min(1, state.Brakes.PadWear + brakeEnergy / 1_800_000_000);

            state.Drivetrain.InputShaftRpm = state.EngineRpm;
            state.Drivetrain.OutputShaftRpm = drivenOmega * 60 / (2 * Math.PI);
            state.Drivetrain.LeftDriveTorqueNm = split.Left;
            state.Drivetrain.RightDriveTorqueNm = split.Right;
        }

        public static List<DiagnosticFinding> Diagnose(in VehicleSimState s)
        {
            var results = new List<DiagnosticFinding>();
            if (s.Brakes.Fade > 0.15)
            {
                results.Add(new DiagnosticFinding
                {
                    Id = "brake-fade", Severity = "warning", System = "Brakes",
                    Observation = "Brake temperatures are producing measurable fade.",
                    RecommendedAction = "Cool brakes; inspect pads, fluid and rotor condition.",
                });
            }
            if (s.Brakes.PadWear > 0.65)
            {
                results.Add(new DiagnosticFinding
                {
                    Id = "pad-wear", Severity = "service", System = "Brakes",
                    Observation = "Pad wear exceeds service threshold.",
                    RecommendedAction = "Measure remaining friction material and replace as required.",
                });
            }
            var maxT = double.MinValue;
            foreach (var t in s.Tires) maxT = Math.Max(maxT, t.TemperatureC);
            if (s.Tires.Length > 0 && maxT > 115)
            {
                results.Add(new DiagnosticFinding
                {
                    Id = "tire-overheat", Severity = "warning", System = "Tires",
                    Observation = "Tire temperature exceeds the useful grip window.",
                    RecommendedAction = "Reduce slip; inspect pressure and alignment setup.",
                });
            }
            if (s.Damage.EngineWear > 0.25)
            {
                results.Add(new DiagnosticFinding
                {
                    Id = "engine-wear", Severity = "service", System = "Engine",
                    Observation = "Accumulated high-RPM wear is reducing available torque.",
                    RecommendedAction = "Perform compression/leak-down style inspection and service worn components.",
                });
            }
            if (s.Damage.DifferentialWear > 0.20)
            {
                results.Add(new DiagnosticFinding
                {
                    Id = "diff-wear", Severity = "service", System = "Differential",
                    Observation = "Wheel-speed imbalance has accumulated differential wear.",
                    RecommendedAction = "Inspect lubricant, backlash and clutch/gear condition.",
                });
            }
            return results;
        }
    }

    /// <summary>Ported from Swift's <c>FixedStepClock</c>. Accumulates
    /// variable frame time and invokes <paramref name="body"/> a fixed
    /// number of times at exactly <see cref="Step"/> seconds each,
    /// clamping a single frame's contribution to 0.25s to avoid a
    /// "spiral of death" after a long stall (identical to the Swift
    /// source's <c>min(frameDelta, 0.25)</c>).</summary>
    public struct FixedStepClock
    {
        public readonly double Step;
        private double _accumulator;

        public FixedStepClock(double hz = 120)
        {
            Step = 1 / hz;
            _accumulator = 0;
        }

        public int Consume(double frameDelta, Action<double> body)
        {
            _accumulator += Math.Min(frameDelta, 0.25);
            var count = 0;
            while (_accumulator >= Step)
            {
                body(Step);
                _accumulator -= Step;
                count++;
            }
            return count;
        }
    }
}
