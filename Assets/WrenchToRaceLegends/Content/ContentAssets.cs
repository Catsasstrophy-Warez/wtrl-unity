using System.Collections.Generic;
using UnityEngine;
using WTRL.Vehicle;

namespace WTRL.Content
{
    /// <summary>
    /// Inspector-editable ScriptableObject wrappers around WTRL.Vehicle's
    /// plain-C# `record` definitions, so a designer can author an
    /// EngineDefinition/TransmissionDefinition/etc. as a .asset file and
    /// drag it into a scene, instead of hand-writing one in code.
    ///
    /// This does NOT reintroduce the "no content catalog" fallback pattern
    /// every WTRL assembly's CONTRACT.md deliberately avoids: there is
    /// still no static lookup-by-id anywhere here. Each asset converts
    /// itself to its plain record via <c>ToDefinition()</c>; whatever
    /// scene code needs a definition holds a direct reference to the
    /// asset (via an inspector field), exactly the way `VehicleSimulation
    /// .Step`/`WTRLRuntime.Advance` already require it to be passed in
    /// explicitly rather than resolved by id.
    ///
    /// UNVERIFIED: this assembly references UnityEngine, so unlike every
    /// other WTRL assembly it cannot be compiled/tested via the throwaway
    /// `dotnet build`/`dotnet test` method used everywhere else in this
    /// project. It has not been compiled by anything yet -- Unity Editor
    /// in this environment is installed but unlicensed (see
    /// Content/CONTRACT.md). Treat this file as unverified until the
    /// first licensed Editor open compiles it for real.
    /// </summary>
    [CreateAssetMenu(fileName = "EngineDefinition", menuName = "WTRL/Content/Engine Definition")]
    public sealed class EngineDefinitionAsset : ScriptableObject
    {
        public string id;
        public string displayName;
        public double displacementLiters;
        public double peakPowerHp;
        public double peakTorqueLbFt;
        public double redlineRpm = 6000;
        public double idleRpm = 750;

        public EngineDefinition ToDefinition() => new(id, displayName, displacementLiters, peakPowerHp, peakTorqueLbFt)
        {
            RedlineRpm = redlineRpm,
            IdleRpm = idleRpm,
        };
    }

    [CreateAssetMenu(fileName = "TransmissionDefinition", menuName = "WTRL/Content/Transmission Definition")]
    public sealed class TransmissionDefinitionAsset : ScriptableObject
    {
        public string id;
        public string displayName;
        public double[] ratios = System.Array.Empty<double>();
        public double finalDrive;
        public TransmissionKind kind = TransmissionKind.Manual;
        public double shiftDuration = 0.28;

        public TransmissionDefinition ToDefinition() => new(id, displayName, ratios, finalDrive)
        {
            Kind = kind,
            ShiftDuration = shiftDuration,
        };
    }

    [CreateAssetMenu(fileName = "SuspensionDefinition", menuName = "WTRL/Content/Suspension Definition")]
    public sealed class SuspensionDefinitionAsset : ScriptableObject
    {
        public string id;
        public string displayName;
        public string frontLayout;
        public string rearLayout;
        public double frontSpringRate = 35_000;
        public double rearSpringRate = 32_000;
        public double frontDampingRatio = 0.55;
        public double rearDampingRatio = 0.55;
        public double frontAntiRollRate = 12_000;
        public double rearAntiRollRate = 10_000;

        public SuspensionDefinition ToDefinition() => new(id, displayName, frontLayout, rearLayout)
        {
            FrontSpringRate = frontSpringRate,
            RearSpringRate = rearSpringRate,
            FrontDampingRatio = frontDampingRatio,
            RearDampingRatio = rearDampingRatio,
            FrontAntiRollRate = frontAntiRollRate,
            RearAntiRollRate = rearAntiRollRate,
        };
    }

    [CreateAssetMenu(fileName = "TireDefinition", menuName = "WTRL/Content/Tire Definition")]
    public sealed class TireDefinitionAsset : ScriptableObject
    {
        public string id;
        public string displayName;
        public double longitudinalStiffness;
        public double corneringStiffness;
        public double peakSlipRatio;
        public double peakSlipAngleRadians;

        public TireDefinition ToDefinition() => new(id, displayName, longitudinalStiffness, corneringStiffness,
            peakSlipRatio, peakSlipAngleRadians);
    }

    [CreateAssetMenu(fileName = "VehicleDefinition", menuName = "WTRL/Content/Vehicle Definition")]
    public sealed class VehicleDefinitionAsset : ScriptableObject
    {
        public string id;
        public string generation;
        public string displayName;
        public double massKg;
        public double wheelbaseM;

        [Header("Referenced sub-definitions (all required -- no fallback)")]
        public EngineDefinitionAsset engine;
        public TransmissionDefinitionAsset transmission;
        public SuspensionDefinitionAsset suspension;
        public TireDefinitionAsset tire;

        public DriveLayout driveLayout = DriveLayout.Rwd;
        public DifferentialKind differential = DifferentialKind.Open;
        public BrakeArchitecture brakeArchitecture = BrakeArchitecture.FourWheelDisc;
        public double brakeTorqueNm = 6500;
        public double aeroDragCoefficientArea = 0.75;

        public VehicleDefinition ToDefinition() => new(id, generation, displayName, massKg, wheelbaseM,
            engine.id, transmission.id, suspension.id)
        {
            DriveLayout = driveLayout,
            Differential = differential,
            BrakeArchitecture = brakeArchitecture,
            BrakeTorqueNm = brakeTorqueNm,
            AeroDragCoefficientArea = aeroDragCoefficientArea,
            TireDefinitionId = tire.id,
        };
    }
}
