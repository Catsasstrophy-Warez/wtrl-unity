using System;

namespace WTRL.Vehicle
{
    // Ported from SwiftRacer/Sources/WTRLCore/Simulation/VehicleSimulation.swift
    // and DynamicsSubsystems.swift. These are the per-frame mutable state
    // types — ported as C# structs (value semantics) to match Swift's
    // struct-with-`inout`-mutation model. VehicleSimState is mutated via
    // `ref` parameters through VehicleSimulation.Step, the same way Swift
    // passes `state: inout VehicleSimState`.

    public struct VehicleInput : IEquatable<VehicleInput>
    {
        public double Throttle;
        public double Brake;
        public double Steering;
        public bool Handbrake;
        public bool Upshift;
        public bool Downshift;

        public VehicleInput(double throttle = 0, double brake = 0, double steering = 0,
            bool handbrake = false, bool upshift = false, bool downshift = false)
        {
            Throttle = Math.Max(-1, Math.Min(1, throttle));
            Brake = Math.Max(0, Math.Min(1, brake));
            Steering = Math.Max(-1, Math.Min(1, steering));
            Handbrake = handbrake;
            Upshift = upshift;
            Downshift = downshift;
        }

        public bool Equals(VehicleInput other) =>
            Throttle.Equals(other.Throttle) && Brake.Equals(other.Brake) && Steering.Equals(other.Steering) &&
            Handbrake == other.Handbrake && Upshift == other.Upshift && Downshift == other.Downshift;
        public override bool Equals(object? obj) => obj is VehicleInput other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Throttle, Brake, Steering, Handbrake, Upshift, Downshift);
    }

    public struct TireState
    {
        public double TemperatureC;
        public double Wear;
        public double SlipRatio;
        public double SlipAngle;
        public double NormalLoadN;

        public static TireState Default => new TireState { TemperatureC = 25.0 };
    }

    public struct BrakeState
    {
        public double FrontTemperatureC;
        public double RearTemperatureC;
        public double PadWear;
        public double Fade;

        public static BrakeState Default => new BrakeState { FrontTemperatureC = 25.0, RearTemperatureC = 25.0 };
    }

    public struct DamageState
    {
        public double EngineWear;
        public double GearboxWear;
        public double DifferentialWear;
        public double SuspensionWear;
        public double TireDamage;
        public double BrakeDamage;
    }

    public struct SuspensionState
    {
        public double FrontCompressionM;
        public double RearCompressionM;
        public double RollRadians;
        public double PitchRadians;
    }

    public struct WheelState
    {
        public double AngularVelocityRadS;
        public double AngularAccelerationRadS2;
        public double DriveTorqueNm;
        public double BrakeTorqueNm;
        public double TireReactionTorqueNm;
    }

    public enum ShiftMode { Automatic, Manual }

    public struct DrivetrainState
    {
        public double ClutchEngagement;
        public double InputShaftRpm;
        public double OutputShaftRpm;
        public double LeftDriveTorqueNm;
        public double RightDriveTorqueNm;
        public ShiftMode ShiftMode;
        public double ShiftTimer;
        public double ShiftDuration;
        public double ClutchAEngagement;
        public double ClutchBEngagement;
        public int ActiveClutch;
        public int PreselectedGear;

        public static DrivetrainState Default => new DrivetrainState
        {
            ClutchEngagement = 1.0,
            InputShaftRpm = 800.0,
            ShiftMode = ShiftMode.Automatic,
            ShiftDuration = 0.28,
            ClutchAEngagement = 1.0,
            PreselectedGear = 2,
        };
    }

    public struct CornerSuspensionState
    {
        public double DisplacementM;
        public double VelocityMps;
        public double SpringForceN;
        public double DamperForceN;
    }

    /// <summary>
    /// Ported from Swift's <c>VehicleSimState</c>, where the four-element
    /// arrays (<c>tires</c>, <c>wheels</c>, <c>suspensionCorners</c>) are
    /// Swift <c>Array</c> value types — copying a Swift struct containing
    /// them deep-copies the arrays. C# arrays are reference types, so a C#
    /// struct containing them does NOT get that for free: copying a
    /// <see cref="VehicleSimState"/> value (assignment, passing by value,
    /// a LINQ projection) leaves both copies pointing at the SAME
    /// underlying arrays, and mutating one through
    /// <see cref="WTRL.Vehicle.VehicleSimulation.Step"/> will silently
    /// mutate the other too. This bit a real test pattern in SwiftRacer
    /// (running the same starting state through two different inputs and
    /// comparing results) — anyone doing that here MUST call
    /// <see cref="Clone"/> for each independent run, not a plain
    /// assignment.
    /// </summary>
    public struct VehicleSimState
    {
        public double SpeedMps;
        public double LateralSpeedMps;
        public double HeadingRadians;
        public double YawRate;
        public double X;
        public double Z;
        public double EngineRpm;
        public int Gear;
        public double LongitudinalAcceleration;
        public double LateralAcceleration;
        public TireState[] Tires;
        public WheelState[] Wheels;
        public DrivetrainState Drivetrain;
        public CornerSuspensionState[] SuspensionCorners;
        public BrakeState Brakes;
        public SuspensionState Suspension;
        public DamageState Damage;

        public static VehicleSimState Default()
        {
            return new VehicleSimState
            {
                EngineRpm = 800.0,
                Gear = 1,
                Tires = new[] { TireState.Default, TireState.Default, TireState.Default, TireState.Default },
                Wheels = new WheelState[4],
                Drivetrain = DrivetrainState.Default,
                SuspensionCorners = new CornerSuspensionState[4],
                Brakes = BrakeState.Default,
                Suspension = new SuspensionState(),
                Damage = new DamageState(),
            };
        }

        /// <summary>Deep copy — see the class-level remarks on why a plain
        /// assignment or pass-by-value is NOT sufficient here.</summary>
        public VehicleSimState Clone()
        {
            var copy = this;
            copy.Tires = (TireState[])Tires.Clone();
            copy.Wheels = (WheelState[])Wheels.Clone();
            copy.SuspensionCorners = (CornerSuspensionState[])SuspensionCorners.Clone();
            return copy;
        }
    }

    public struct VehicleTuning
    {
        public double SteeringLockRadians;
        public double BrakeBiasFront;
        public double DifferentialLock;
        public double TirePressureGripFactor;
        public double CoolingFactor;

        public static VehicleTuning Default => new VehicleTuning
        {
            SteeringLockRadians = 0.55,
            BrakeBiasFront = 0.68,
            DifferentialLock = 0.35,
            TirePressureGripFactor = 1.0,
            CoolingFactor = 1.0,
        };
    }

    public struct DiagnosticFinding
    {
        public string Id;
        public string Severity;
        public string System;
        public string Observation;
        public string RecommendedAction;
    }
}
