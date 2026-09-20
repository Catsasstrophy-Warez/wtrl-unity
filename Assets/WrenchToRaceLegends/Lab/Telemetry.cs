using System.Collections.Generic;

namespace WTRL.Lab
{
    // Ported from SwiftRacer/Sources/WTRLCore/Runtime/Telemetry.swift.

    public struct RuntimeTelemetrySample
    {
        public double SimulationSeconds;
        public double WallFrameDelta;
        public int FixedSteps;
        public double SpeedMps;
        public double EngineRpm;
        public int Gear;
        public double AverageTireSlip;
        public double LongitudinalAcceleration;
        public double LateralAcceleration;
        public double EngineTorqueNm;
        public double WheelTorqueNm;
        public double ClutchEngagement;
        public double FrontBrakeTemperatureC;
        public double RearBrakeTemperatureC;
        public double[] TireTemperaturesC;
        public double[] TireNormalLoadsN;
        public double[] SuspensionTravelM;
    }

    /// <summary>Fixed-capacity circular buffer of telemetry samples — ported
    /// from Swift's <c>RuntimeTelemetryRing</c>. Overwrites the oldest
    /// sample once full; <see cref="Samples"/> always returns them in
    /// chronological order regardless of the internal write position.</summary>
    public sealed class RuntimeTelemetryRing
    {
        private readonly RuntimeTelemetrySample?[] _storage;
        private int _writeIndex;
        private int _count;
        public int Capacity { get; }

        public RuntimeTelemetryRing(int capacity = 600)
        {
            Capacity = System.Math.Max(1, capacity);
            _storage = new RuntimeTelemetrySample?[Capacity];
        }

        public IReadOnlyList<RuntimeTelemetrySample> Samples
        {
            get
            {
                if (_count == 0) return System.Array.Empty<RuntimeTelemetrySample>();
                var start = _count == Capacity ? _writeIndex : 0;
                var result = new List<RuntimeTelemetrySample>(_count);
                for (var i = 0; i < _count; i++)
                {
                    var sample = _storage[(start + i) % Capacity];
                    if (sample.HasValue) result.Add(sample.Value);
                }
                return result;
            }
        }

        public void Append(RuntimeTelemetrySample sample)
        {
            _storage[_writeIndex] = sample;
            _writeIndex = (_writeIndex + 1) % Capacity;
            _count = System.Math.Min(_count + 1, Capacity);
        }
    }
}
