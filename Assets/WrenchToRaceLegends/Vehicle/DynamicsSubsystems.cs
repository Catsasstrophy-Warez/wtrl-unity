using System;
using System.Linq;

namespace WTRL.Vehicle
{
    // Ported from SwiftRacer/Sources/WTRLCore/Simulation/DynamicsSubsystems.swift.
    // Arithmetic is intended to be identical to the Swift source, term for
    // term — see CONTRACT.md for the verification approach (golden-value
    // tests ported from WTRLCoreTests, not just "looks equivalent").

    public static class EngineSolver
    {
        public static double TorqueNm(EngineDefinition engine, double rpm, double throttle, double wear)
        {
            var points = engine.TorqueCurve.OrderBy(p => p.Rpm).ToArray();
            double baseTorque;
            if (points.Length >= 2)
            {
                if (rpm <= points[0].Rpm)
                {
                    baseTorque = points[0].TorqueNm;
                }
                else if (rpm >= points[^1].Rpm)
                {
                    baseTorque = points[^1].TorqueNm;
                }
                else
                {
                    var value = points[0].TorqueNm;
                    for (var i = 0; i < points.Length - 1; i++)
                    {
                        if (rpm >= points[i].Rpm && rpm <= points[i + 1].Rpm)
                        {
                            var f = (rpm - points[i].Rpm) / Math.Max(1, points[i + 1].Rpm - points[i].Rpm);
                            value = points[i].TorqueNm + (points[i + 1].TorqueNm - points[i].TorqueNm) * f;
                        }
                    }
                    baseTorque = value;
                }
            }
            else
            {
                var x = Math.Max(0, Math.Min(1, rpm / engine.RedlineRpm));
                baseTorque = engine.PeakTorqueLbFt * 1.35582 * Math.Max(0.52, 1 - Math.Pow((x - 0.62) / 0.62, 2) * 0.48);
            }
            return baseTorque * Math.Max(0, throttle) * Math.Max(0.45, 1 - wear * 0.45);
        }
    }

    public static class TireSolver
    {
        public static double GripCoefficient(double baseGrip, double loadN, double temperatureC, double wear,
            double pressureFactor, TireDefinition? tire = null, double surfaceGrip = 1)
        {
            const double loadReference = 3500.0;
            var sensitivity = tire?.LoadSensitivity ?? 0.06;
            var loadSensitivity = Math.Pow(Math.Max(0.25, loadN / loadReference), -sensitivity);
            var optimal = tire?.OptimalTemperatureC ?? 85;
            var window = Math.Max(20, tire?.TemperatureWindowC ?? 70);
            var tempFactor = Math.Max(0.58, 1 - Math.Abs(temperatureC - optimal) / (window * 2.5));
            var wearFactor = Math.Max(0.52, 1 - wear * 0.48);
            var pressureSensitivity = tire?.PressureSensitivity ?? 0.12;
            var pressure = Math.Max(0.75, 1 - Math.Abs(pressureFactor - 1) * pressureSensitivity);
            return baseGrip * loadSensitivity * tempFactor * wearFactor * pressure * surfaceGrip;
        }

        public static (double Fx, double Fy) CombinedForces(double slipRatio, double slipAngle, double normalLoadN,
            double mu, TireDefinition? tire = null)
        {
            var kx = tire?.LongitudinalStiffness ?? 8.0;
            var ky = tire?.CorneringStiffness ?? 5.0;
            var peakSr = Math.Max(0.04, tire?.PeakSlipRatio ?? 0.12);
            var peakSa = Math.Max(0.04, tire?.PeakSlipAngleRadians ?? 0.11);
            var sx = Math.Tanh(slipRatio / peakSr * kx * 0.12);
            var sy = Math.Tanh(slipAngle / peakSa * ky * 0.12);
            var magnitude = Math.Sqrt(sx * sx + sy * sy);
            if (magnitude <= 1e-9) return (0, 0);
            var scale = Math.Min(1.0, 1.0 / magnitude) * mu * normalLoadN;
            return (sx * scale, sy * scale);
        }
    }

    public static class DifferentialSolver
    {
        public static (double Left, double Right) Split(double totalTorque, DifferentialKind kind, double lockAmount,
            double leftOmega, double rightOmega, double leftCapacityNm = double.MaxValue,
            double rightCapacityNm = double.MaxValue)
        {
            var delta = rightOmega - leftOmega;
            var half = totalTorque * 0.5;
            switch (kind)
            {
                case DifferentialKind.Open:
                {
                    var common = Math.Min(Math.Min(Math.Abs(half), leftCapacityNm), rightCapacityNm) *
                                 (totalTorque >= 0 ? 1 : -1);
                    return (common, common);
                }
                case DifferentialKind.Locked:
                    return (half - delta * 8, half + delta * 8);
                case DifferentialKind.ClutchLsd:
                {
                    var transfer = Math.Max(-Math.Abs(totalTorque) * 0.22,
                        Math.Min(Math.Abs(totalTorque) * 0.22, delta * 18 * Math.Max(0.1, lockAmount)));
                    return (
                        Math.Max(-leftCapacityNm, Math.Min(leftCapacityNm, half - transfer)),
                        Math.Max(-rightCapacityNm, Math.Min(rightCapacityNm, half + transfer)));
                }
                case DifferentialKind.TorqueBiasing:
                {
                    var transfer = Math.Max(-Math.Abs(totalTorque) * 0.30, Math.Min(Math.Abs(totalTorque) * 0.30, delta * 12));
                    return (
                        Math.Max(-leftCapacityNm, Math.Min(leftCapacityNm, half - transfer)),
                        Math.Max(-rightCapacityNm, Math.Min(rightCapacityNm, half + transfer)));
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }

    public static class SuspensionSolver
    {
        public static void Update(ref CornerSuspensionState corner, double targetLoadN, double staticLoadN,
            double springRate, double damping, double? effectiveMassKg, double bumpLimitM, double reboundLimitM,
            double dt)
        {
            var target = (targetLoadN - staticLoadN) / Math.Max(1, springRate);
            var effectiveMass = Math.Max(15, effectiveMassKg ?? staticLoadN / 9.80665);
            var accel = (target - corner.DisplacementM) * springRate / effectiveMass
                        - corner.VelocityMps * damping / effectiveMass;
            corner.VelocityMps += accel * dt;
            corner.DisplacementM += corner.VelocityMps * dt;
            corner.DisplacementM = Math.Max(-Math.Abs(reboundLimitM), Math.Min(Math.Abs(bumpLimitM), corner.DisplacementM));
            corner.SpringForceN = corner.DisplacementM * springRate;
            corner.DamperForceN = corner.VelocityMps * damping;
        }

        public static void ApplyAntiRoll(CornerSuspensionState[] corners, double frontRate, double rearRate, double dt)
        {
            if (corners.Length != 4) return;
            var frontDelta = corners[0].DisplacementM - corners[1].DisplacementM;
            var rearDelta = corners[2].DisplacementM - corners[3].DisplacementM;
            var frontCorrection = frontDelta * frontRate * dt / 9000;
            var rearCorrection = rearDelta * rearRate * dt / 9000;
            corners[0].VelocityMps -= frontCorrection;
            corners[1].VelocityMps += frontCorrection;
            corners[2].VelocityMps -= rearCorrection;
            corners[3].VelocityMps += rearCorrection;
        }
    }

    public static class AeroSolver
    {
        public static double DragForceN(double speedMps, double coefficientArea, double airDensity = 1.225) =>
            0.5 * airDensity * coefficientArea * speedMps * Math.Abs(speedMps);

        public static double RollingResistanceN(double speedMps, double massKg, double coefficient)
        {
            if (speedMps == 0) return 0;
            return coefficient * massKg * 9.80665 * (speedMps > 0 ? 1 : -1);
        }
    }

    public static class WheelSolver
    {
        public static void Update(ref WheelState wheel, double inertiaKgm2, double driveTorqueNm, double brakeTorqueNm,
            double reactionTorqueNm, double dt)
        {
            wheel.DriveTorqueNm = driveTorqueNm;
            wheel.BrakeTorqueNm = brakeTorqueNm;
            wheel.TireReactionTorqueNm = reactionTorqueNm;
            var sign = wheel.AngularVelocityRadS >= 0 ? 1.0 : -1.0;
            wheel.AngularAccelerationRadS2 = (driveTorqueNm - brakeTorqueNm * sign - reactionTorqueNm) / Math.Max(0.5, inertiaKgm2);
            wheel.AngularVelocityRadS += wheel.AngularAccelerationRadS2 * dt;
        }
    }

    public static class ChassisSolver
    {
        public static void Integrate(ref VehicleSimState state, double massKg, double longitudinalForceN,
            double dragForceN, double rollingForceN, double steeringRadians, double wheelbaseM,
            double lateralGripCoefficient, double suspensionWear, double dt)
        {
            state.LongitudinalAcceleration = (longitudinalForceN - dragForceN - rollingForceN) / Math.Max(1, massKg);
            state.SpeedMps += state.LongitudinalAcceleration * dt;
            var idealYaw = Math.Abs(state.SpeedMps) > 0.1
                ? state.SpeedMps / Math.Max(0.5, wheelbaseM) * Math.Tan(steeringRadians)
                : 0;
            var demandedLat = Math.Abs(state.SpeedMps * idealYaw);
            var saturation = demandedLat > 0 ? Math.Min(1, lateralGripCoefficient * 9.80665 / demandedLat) : 1;
            state.YawRate = idealYaw * saturation * (1 - suspensionWear * 0.25);
            state.LateralAcceleration = state.SpeedMps * state.YawRate;
            state.LateralSpeedMps += (state.LateralAcceleration - state.LateralSpeedMps * 2.5) * dt;
            state.HeadingRadians += state.YawRate * dt;
            state.X += -Math.Sin(state.HeadingRadians) * state.SpeedMps * dt;
            state.Z += -Math.Cos(state.HeadingRadians) * state.SpeedMps * dt;
        }
    }
}
