using UnityEngine;
using WTRL.Vehicle;

namespace WTRL.Content
{
    /// <summary>
    /// Split out of ContentAssets.cs into its own file (2026-09-20) to
    /// work around a real, reproducible Unity Editor bug: when this type
    /// was one of six ScriptableObject types declared in a single file,
    /// every asset created from it got a malformed serialized script
    /// reference (`m_Script: {fileID: 0}`, `m_EditorClassIdentifier:
    /// WTRL.Content:WTRL.Content:VehicleDefinitionAsset` -- missing the
    /// namespace/class-name separator dot and the fileID entirely) no
    /// matter how many times the asset was recreated at a fresh path.
    /// The other five types in that same file (Engine/Transmission/
    /// Suspension/Tire/SurfaceDefinitionAsset) all serialized correctly.
    /// Root cause not fully diagnosed (suspected Unity-side fileID
    /// collision/degradation specific to this type's position or name
    /// among six ScriptableObject types in one file), but moving this
    /// type alone into its own file made newly-created assets serialize
    /// correctly on the next attempt -- confirmed by
    /// <c>HeroContentBuilder</c>'s generated `.asset` files.
    /// </summary>
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
