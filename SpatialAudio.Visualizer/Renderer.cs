using SpatialAudio.Telemetry;
using System.Text;

namespace SpatialAudio.Visualizer
{
    public static class Renderer
    {
        public const int CurveRows = 3;                            // braille cell-rows per curve
        public const int FrameHeight = 7 + 3 * (1 + CurveRows);    // 19
        const float HrtfRangeDb = 40f;                             // vertical window of the HRTF panels
        const float IldRangeDb = 12f;                              // +/- window of the ILD panel

        // braille dot -> bit, indexed [row 0..3][col 0..1] within a cell
        static readonly int[,] BrailleBits =
        {
            { 0x01, 0x08 },
            { 0x02, 0x10 },
            { 0x04, 0x20 },
            { 0x40, 0x80 },
        };

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
            sb.Append('█', filled);
            sb.Append('░', width - filled);
            return sb.ToString();
        }

        // Per-column log-spaced max: every column maps to a bin range, so there are no gaps.
        public static void SpectrumColumnsLog(float[] mag, float[] dest, float binHz, float minHz, float maxHz)
        {
            float logRange = MathF.Log(maxHz / minHz);
            int bins = mag.Length;
            for (int c = 0; c < dest.Length; c++)
            {
                float f0 = minHz * MathF.Exp(logRange * c / dest.Length);
                float f1 = minHz * MathF.Exp(logRange * (c + 1) / dest.Length);
                int b0 = Math.Clamp((int)(f0 / binHz), 0, bins - 1);
                int b1 = Math.Clamp((int)MathF.Ceiling(f1 / binHz), b0 + 1, bins);
                float max = mag[b0];
                for (int k = b0 + 1; k < b1; k++) if (mag[k] > max) max = mag[k];
                dest[c] = max;
            }
        }

        public static float ToDb(float value, float reference)
        {
            if (value <= 0) return -100f;
            return 20 * MathF.Log10(value / reference);
        }

        public static string[] RenderFrame(TelemetryFrame f, int width)
        {
            const float binHz = 48000f / 1024f;
            float[] colsL = new float[width];
            float[] colsR = new float[width];
            SpectrumColumnsLog(f.MagL, colsL, binHz, 100f, 16000f);
            SpectrumColumnsLog(f.MagR, colsR, binHz, 100f, 16000f);
            float reference = MathF.Max(colsL.Max(), colsR.Max());

            string separator = new string('-', width);
            string[] lines = new string[FrameHeight];
            lines[0] = separator;

            char[] bar = new char[width];
            Array.Fill(bar, ' ');
            bar[AzimuthColumn(f.AzimuthDeg, width)] = '●';
            lines[1] = new string(bar);

            string direction = f.AzimuthDeg < 0 ? "LEFT" : "RIGHT";
            lines[2] = $"az {f.AzimuthDeg,6:F1}° {direction,-5} dist {f.DistancePx,5:F0}px {f.WindowTitle}";

            lines[3] = separator;

            int barW = Math.Max(1, (width - 12) / 2);
            lines[4] = $"OUT L {FillBar(f.LevelL, barW)} R {FillBar(f.LevelR, barW)}";

            lines[5] = separator;

            lines[6] = "HRTF left ear      (100Hz  ->  16kHz)";
            string[] curveL = BrailleMagnitude(colsL, reference, width, CurveRows);
            for (int r = 0; r < CurveRows; r++) lines[7 + r] = curveL[r];

            lines[7 + CurveRows] = "HRTF right ear     (100Hz  ->  16kHz)";
            string[] curveR = BrailleMagnitude(colsR, reference, width, CurveRows);
            for (int r = 0; r < CurveRows; r++) lines[8 + CurveRows + r] = curveR[r];

            int ildHeader = 8 + 2 * CurveRows;
            lines[ildHeader] = "ILD   up = left louder   down = right louder   (100Hz -> 16kHz)";
            string[] curveD = BrailleDifference(colsL, colsR, width, CurveRows);
            for (int r = 0; r < CurveRows; r++) lines[ildHeader + 1 + r] = curveD[r];

            lines[FrameHeight - 1] = separator;

            return lines.Select(l => Fit(l, width)).ToArray();
        }

        static string[] BrailleMagnitude(float[] cols, float reference, int cellCols, int cellRows)
        {
            int dotW = cellCols * 2, dotH = cellRows * 4;
            int[,] cells = new int[cellRows, cellCols];
            int prevY = -1;
            for (int x = 0; x < dotW; x++)
            {
                int c = Math.Min(cols.Length - 1, x * cols.Length / dotW);
                float db = ToDb(cols[c], reference);
                float frac = Math.Clamp((db + HrtfRangeDb) / HrtfRangeDb, 0f, 1f);
                int y = (int)MathF.Round((1f - frac) * (dotH - 1));   // top = loud
                Plot(cells, ref prevY, x, y, cellCols, cellRows);
            }
            return RenderBraille(cells, cellCols, cellRows);
        }

        static string[] BrailleDifference(float[] colsL, float[] colsR, int cellCols, int cellRows)
        {
            int dotW = cellCols * 2, dotH = cellRows * 4;
            int[,] cells = new int[cellRows, cellCols];
            int prevY = -1;
            for (int x = 0; x < dotW; x++)
            {
                int c = Math.Min(colsL.Length - 1, x * colsL.Length / dotW);
                float d = (colsL[c] <= 0 || colsR[c] <= 0) ? 0f : ToDb(colsL[c], colsR[c]); // + = left louder
                d = Math.Clamp(d, -IldRangeDb, IldRangeDb);
                float frac = 0.5f - d / (2f * IldRangeDb);             // left louder -> above center
                int y = (int)MathF.Round(frac * (dotH - 1));
                Plot(cells, ref prevY, x, y, cellCols, cellRows);
            }
            return RenderBraille(cells, cellCols, cellRows);
        }

        // set the braille dots from the previous y to this y, so the curve is continuous
        static void Plot(int[,] cells, ref int prevY, int x, int y, int cellCols, int cellRows)
        {
            if (prevY < 0) prevY = y;
            int lo = Math.Min(prevY, y), hi = Math.Max(prevY, y);
            for (int yy = lo; yy <= hi; yy++)
            {
                int cx = x / 2, cy = yy / 4;
                if (cx >= 0 && cx < cellCols && cy >= 0 && cy < cellRows)
                    cells[cy, cx] |= BrailleBits[yy % 4, x % 2];
            }
            prevY = y;
        }

        static string[] RenderBraille(int[,] cells, int cellCols, int cellRows)
        {
            string[] lines = new string[cellRows];
            for (int r = 0; r < cellRows; r++)
            {
                var sb = new StringBuilder(cellCols);
                for (int c = 0; c < cellCols; c++) sb.Append((char)(0x2800 + cells[r, c]));
                lines[r] = sb.ToString();
            }
            return lines;
        }

        static string Fit(string s, int width)
        {
            return s.Length >= width ? s[..width] : s.PadRight(width);
        }
    }
}
