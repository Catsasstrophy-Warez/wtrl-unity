using UnityEngine;
using WTRL.Vehicle;

namespace WTRL.Content
{
    /// <summary>See EngineDefinitionAsset.cs's doc comment: one
    /// ScriptableObject type per file, to avoid a real Unity Editor
    /// batchmode script-reference bug.</summary>
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
}
