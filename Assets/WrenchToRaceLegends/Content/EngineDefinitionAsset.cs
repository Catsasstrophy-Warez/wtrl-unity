using UnityEngine;
using WTRL.Vehicle;

namespace WTRL.Content
{
    /// <summary>
    /// One ScriptableObject wrapper type per file -- see
    /// VehicleDefinitionAsset.cs's doc comment for why: a real, reproduced
    /// Unity Editor bug means only the first ScriptableObject type
    /// declared in a .cs file reliably gets a correct serialized script
    /// reference when created via AssetDatabase.CreateAsset in this
    /// environment's batchmode. Every WTRL.Content type was split out of
    /// the original combined ContentAssets.cs for this reason.
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
}
