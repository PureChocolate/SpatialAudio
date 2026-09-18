namespace SpatialAudio.Telemetry
{
    public sealed class TelemetryFrame
    {
        public const int SpectrumBins = 512;
        public long Sequence { get; set; }
        public float AzimuthDeg { get; set; }
        public float DistancePx { get; set; }
        public string WindowTitle { get; set; } = "";
        public float LevelL { get; set; }
        public float LevelR { get; set; }
        public float[] MagL { get; set; } = new float[SpectrumBins];
        public float[] MagR { get; set; } = new float[SpectrumBins];
    }
}
