using SpatialAudio.Telemetry;
using System.Text;

namespace SpatialAudio.Visualizer
{
    public static class Renderer
    {
        public const int frameHeight = 9;
        static readonly char[] Ramp = { '▁', '▂', '▃', '▄', '▅', '▆', '▇', '█' };
        public static int AzimuthColumn(float az, int width)
        {
            return (int)Math.Clamp(MathF.Round((az + 90) * (width - 1) / 180f), 0, width - 1);
        }

        public static (int col, int row) MapToCell(int x, int y, int left, int top, int width, int height, int cols, int rows)
        {
            int col = Math.Clamp((x - left) * cols / width, 0, cols - 1);
            int row = Math.Clamp((y - top) * rows / height, 0, rows - 1);
            return (col, row);
        }

        public static string FillBar(double fraction, int width)
        {
            int filled = (int)Math.Clamp(Math.Round(fraction * width), 0, width);
            StringBuilder sb = new StringBuilder(width);
            sb.Append('█',filled);
            sb.Append('░', width-filled);

            return sb.ToString();
        }

        public static void SpectrumColumns(float[] mag, float[] dest)
        {
            for(int c = 0; c < dest.Length; c++)
            {
                int start = c * mag.Length / dest.Length;
                int end = (c + 1) * mag.Length / dest.Length;
                ReadOnlySpan<float> slice = mag.AsSpan(start, end - start);

                float maxVal = float.MinValue;

                for(int i = 0; i < slice.Length; i++)
                {
                    if (slice[i] > maxVal) maxVal = slice[i];
                }
                dest[c] = maxVal;
            }
        }

        public static float ToDb(float value, float reference)
        {
            if (value <= 0) return -100f;
            return 20 * MathF.Log10(value/reference);
        }

        public static string[] RenderFrame(TelemetryFrame f, int width)
        {
            string[] ret = new string[frameHeight];
            string seperator = new string('-', width);
            ret[0] = seperator;
            ret[3] = seperator;
            ret[5] = seperator;
            ret[8] = seperator;

            char[] chars = new char[width];
            Array.Fill(chars, ' ');
            int dotIndex = AzimuthColumn(f.AzimuthDeg, width);
            if(dotIndex >= 0 && dotIndex < width) chars[dotIndex] = '●';
            ret[1] = new string(chars);

            string direction = f.AzimuthDeg < 0 ? "LEFT" : "RIGHT";
            ret[2] = $"az {f.AzimuthDeg,6:F1}° {direction,-5} dist {f.DistancePx,5:F0}px {f.WindowTitle}";

            int barW = (width - 12) / 2;
            ret[4] = $"OUT L {FillBar(f.LevelL, barW)} R {FillBar(f.LevelR, barW)}";

            ret[6] = "HRTF L " + SpectrumLine(f.MagL, width - 7);
            ret[7] = "HRTF R " + SpectrumLine(f.MagR, width - 7);

            return ret.Select(l => Fit(l,width)).ToArray();
        }

        static string SpectrumLine(float[] mag, int count)
        {
            float[] cols = new float[count];
            SpectrumColumns(mag, cols);
            float max = cols.Max();
            if (max <= 0) return new string('_', count);
            StringBuilder sB = new StringBuilder(count);
            foreach( float v in cols)
            {
                float db = ToDb(v, max);
                float frac = Math.Clamp((db + 60f) / 60f, 0, 1);
                sB.Append(Ramp[(int)MathF.Round(frac * 7)]);
            }

            return sB.ToString();
        }

        static string Fit(string s, int width)
        {
            return s.Length >= width ? s[..width] : s.PadRight(width);
        }
    }
}
