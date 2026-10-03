using SpatialAudio.Telemetry;
namespace SpatialAudio
{
    internal static class EngineTelemetry
    {
        static readonly TelemetryFrame _frame = new();

        public static TelemetryFrame Sample()
        {
            var (az, dist, title) = WindowTracker.GetFocusedInfo();
            Spatializer.CurrentAzimuthDeg = az;

            _frame.Sequence++;
            _frame.AzimuthDeg = az;
            _frame.DistancePx = dist;
            _frame.WindowTitle = title;
            _frame.LevelL = Spatializer.OutputLevelL;
            _frame.LevelR = Spatializer.OutputLevelR;

            Spatializer.GetHRTFMagnitude(_frame.MagL, _frame.MagR);
            return _frame;
        }
    }
}
