using System.Runtime.InteropServices;

namespace SpatialAudio.Telemetry
{
    public static class TelemetryCodec
    {
        public static void WriteFrame(Stream s, TelemetryFrame f)
        {
            using MemoryStream m = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(m);

            bw.Write(f.Sequence);
            bw.Write(f.AzimuthDeg);
            bw.Write(f.DistancePx);
            bw.Write(f.WindowTitle);
            bw.Write(f.LevelL);
            bw.Write(f.LevelR);
            bw.Write(TelemetryFrame.SpectrumBins);
            bw.Write(MemoryMarshal.AsBytes(f.MagL.AsSpan()));
            bw.Write(MemoryMarshal.AsBytes(f.MagR.AsSpan()));
            bw.Flush();
            byte[] len = (BitConverter.GetBytes((int)m.Length));
            s.Write(len, 0, 4);
            s.Write(m.GetBuffer(), 0, (int)m.Length);
            s.Flush();
        }

        public static TelemetryFrame? ReadFrame(Stream s)
        {
            byte[] header = new byte[4];
            int payload;
            int read = s.ReadAtLeast(header, 4, throwOnEndOfStream: false);
            if (read == 0) return null;
            else if (read < 4) throw new InvalidDataException("Corrupted header, incomplete data.");
            else payload = BitConverter.ToInt32(header, 0);
            
            if(payload <= 0) throw new InvalidDataException("no proper payload length");
            
            byte[] buffer = new byte[payload];
            s.ReadExactly(buffer, 0, payload);

            MemoryStream m = new MemoryStream(buffer);
            BinaryReader br = new BinaryReader(m);
            TelemetryFrame f = new TelemetryFrame();
            f.Sequence = br.ReadInt64();
            f.AzimuthDeg = br.ReadSingle();
            f.DistancePx = br.ReadSingle();
            f.WindowTitle = br.ReadString();
            f.LevelL = br.ReadSingle();
            f.LevelR = br.ReadSingle();

            int bins = br.ReadInt32();
            if (bins != TelemetryFrame.SpectrumBins) throw new InvalidDataException("Wrong bins");

            br.Read(MemoryMarshal.AsBytes(f.MagL.AsSpan()));
            br.Read(MemoryMarshal.AsBytes(f.MagR.AsSpan()));

            return f;
        }
    }
}
