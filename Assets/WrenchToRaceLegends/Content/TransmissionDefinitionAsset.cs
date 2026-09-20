using UnityEngine;
using WTRL.Vehicle;

namespace WTRL.Content
{
    /// <summary>See EngineDefinitionAsset.cs's doc comment: one
    /// ScriptableObject type per file, to avoid a real Unity Editor
    /// batchmode script-reference bug.</summary>
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
}
