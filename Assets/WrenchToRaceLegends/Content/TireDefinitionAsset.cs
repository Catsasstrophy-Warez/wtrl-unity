using UnityEngine;
using WTRL.Vehicle;

namespace WTRL.Content
{
    /// <summary>See EngineDefinitionAsset.cs's doc comment: one
    /// ScriptableObject type per file, to avoid a real Unity Editor
    /// batchmode script-reference bug.</summary>
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
}
