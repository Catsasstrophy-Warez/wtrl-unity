using System;
using System.Collections.Generic;

namespace WTRL.Vehicle
{
    // Ported from SwiftRacer/Sources/WTRLCore/Content/Definitions.swift and
    // AdvancedDefinitions.swift. These are content records (looked up by id
    // in whatever catalog owns them — no static global catalog exists in
    // this assembly, deliberately: see CONTRACT.md). Ported as C# `record`
    // types (not the mutable structs SimulationState.cs uses for per-frame
    // state): Swift's structs here were value types that callers routinely
    // copy-and-tweak (`var vehicle = ...; vehicle.tireGripCoefficient *=
    // 1.07`, e.g. in VehicleConfigurationResolver). A C# `record` gives the
    // same "immutable, but cheaply derive a modified copy" shape via `with`
    // expressions, without the reference-type aliasing risk a plain mutable
    // class would introduce (see WTRL.Garage's resolver for the `with`
    // usage this was added for).

    public enum DriveLayout { Rwd, Fwd, Awd }

    public enum DifferentialKind { Open, ClutchLsd, Locked, TorqueBiasing }

    public enum BrakeArchitecture { Drum, FrontDiscRearDrum, FourWheelDisc, PerformanceDisc }

    public enum TransmissionKind { Manual, Automatic, DualClutch }

    public enum SurfaceKind { Asphalt, OldAsphalt, Concrete, PreparedDrag, Gravel, WetAsphalt }

    public readonly struct TorquePoint
    {
        public readonly double Rpm;
        public readonly double TorqueNm;
        public TorquePoint(double rpm, double torqueNm)
        {
            Rpm = rpm;
            TorqueNm = torqueNm;
        }
    }

    public sealed record EngineDefinition
    {
        public string Id { get; }
        public string Name { get; init; }
        public double DisplacementLiters { get; init; }
        public double PeakPowerHp { get; init; }
        public double PeakTorqueLbFt { get; init; }
        public double RedlineRpm { get; init; } = 6000;
        public double IdleRpm { get; init; } = 750;
        public IReadOnlyList<TorquePoint> TorqueCurve { get; init; } = Array.Empty<TorquePoint>();

        public EngineDefinition(string id, string name, double displacementLiters, double peakPowerHp,
            double peakTorqueLbFt)
        {
            Id = id;
            Name = name;
            DisplacementLiters = displacementLiters;
            PeakPowerHp = peakPowerHp;
            PeakTorqueLbFt = peakTorqueLbFt;
        }
    }

    public sealed record TransmissionDefinition
    {
        public string Id { get; }
        public string Name { get; init; }
        public IReadOnlyList<double> Ratios { get; init; }
        public double FinalDrive { get; init; }
        public TransmissionKind Kind { get; init; } = TransmissionKind.Manual;
        public double ShiftDuration { get; init; } = 0.28;

        public TransmissionDefinition(string id, string name, IReadOnlyList<double> ratios, double finalDrive)
        {
            Id = id;
            Name = name;
            Ratios = ratios;
            FinalDrive = finalDrive;
        }
    }

    public sealed record SuspensionDefinition
    {
        public string Id { get; }
        public string Name { get; init; }
        public string FrontLayout { get; init; }
        public string RearLayout { get; init; }
        public double FrontSpringRate { get; init; } = 35_000;
        public double RearSpringRate { get; init; } = 32_000;
        public double FrontDampingRatio { get; init; } = 0.55;
        public double RearDampingRatio { get; init; } = 0.55;
        public double FrontAntiRollRate { get; init; } = 12_000;
        public double RearAntiRollRate { get; init; } = 10_000;
        public double BumpTravelM { get; init; } = 0.10;
        public double ReboundTravelM { get; init; } = 0.10;

        public SuspensionDefinition(string id, string name, string frontLayout, string rearLayout)
        {
            Id = id;
            Name = name;
            FrontLayout = frontLayout;
            RearLayout = rearLayout;
        }
    }

    public sealed record TireDefinition
    {
        public string Id { get; }
        public string Name { get; init; }
        public double LongitudinalStiffness { get; init; }
        public double CorneringStiffness { get; init; }
        public double PeakSlipRatio { get; init; }
        public double PeakSlipAngleRadians { get; init; }
        public double LoadSensitivity { get; init; } = 0.06;
        public double OptimalTemperatureC { get; init; } = 85;
        public double TemperatureWindowC { get; init; } = 70;
        public double RollingResistance { get; init; } = 0.014;
        public double PressureSensitivity { get; init; } = 0.12;

        public TireDefinition(string id, string name, double longitudinalStiffness, double corneringStiffness,
            double peakSlipRatio, double peakSlipAngleRadians)
        {
            Id = id;
            Name = name;
            LongitudinalStiffness = longitudinalStiffness;
            CorneringStiffness = corneringStiffness;
            PeakSlipRatio = peakSlipRatio;
            PeakSlipAngleRadians = peakSlipAngleRadians;
        }
    }

    public sealed record SurfaceDefinition
    {
        public string Id { get; }
        public SurfaceKind Kind { get; init; }
        public double DryGripMultiplier { get; init; }
        public double RollingResistanceMultiplier { get; init; } = 1;
        public double Roughness { get; init; } = 0.2;

        public SurfaceDefinition(string id, SurfaceKind kind, double dryGripMultiplier)
        {
            Id = id;
            Kind = kind;
            DryGripMultiplier = dryGripMultiplier;
        }
    }

    public sealed record VehicleDefinition
    {
        public string Id { get; }
        public string Generation { get; init; }
        public string Name { get; init; }
        public double MassKg { get; init; }
        public double WheelbaseM { get; init; }
        public double TrackWidthM { get; init; } = 1.55;
        public double CgHeightM { get; init; } = 0.55;
        public double StaticFrontWeightFraction { get; init; } = 0.55;
        public double TireGripCoefficient { get; init; } = 1.0;
        public double TireRadiusM { get; init; } = 0.33;
        public string TireDefinitionId { get; init; } = "street-performance";
        public double WheelInertiaKgm2 { get; init; } = 1.25;
        public double UnsprungMassKgPerWheel { get; init; } = 22;
        public string EngineId { get; init; }
        public string TransmissionId { get; init; }
        public string SuspensionId { get; init; }
        public DriveLayout DriveLayout { get; init; } = DriveLayout.Rwd;
        public DifferentialKind Differential { get; init; } = DifferentialKind.Open;
        public BrakeArchitecture BrakeArchitecture { get; init; } = BrakeArchitecture.FourWheelDisc;
        public double BrakeTorqueNm { get; init; } = 6500;
        public double AeroDragCoefficientArea { get; init; } = 0.75;

        public VehicleDefinition(string id, string generation, string name, double massKg, double wheelbaseM,
            string engineId, string transmissionId, string suspensionId)
        {
            Id = id;
            Generation = generation;
            Name = name;
            MassKg = massKg;
            WheelbaseM = wheelbaseM;
            EngineId = engineId;
            TransmissionId = transmissionId;
            SuspensionId = suspensionId;
        }
    }
}
