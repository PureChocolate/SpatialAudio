using System.IO;

namespace SpatialAudio
{
    internal class HrtfDatabase
    {

        public struct HRTFLocator : IEquatable<HRTFLocator>
        {
            public string Side;
            public int Elev;
            public int Az;

            public bool Equals(HRTFLocator other)
            {
                return Side.Equals(other.Side) && Elev == other.Elev && Az == other.Az;
            }

            public override bool Equals(object? obj)
            {
                return obj is HRTFLocator other && Equals(other);
            }

            public override int GetHashCode() 
            { 
                return HashCode.Combine(Side, Elev, Az);
            }
        }
        private static readonly string _folderPath = @"F:\Code\SpatialAudio\data\full\full";
        private static Dictionary<HRTFLocator, float[]> _dataFiles = [];
        public static void ReadData()
        {
            if (Directory.Exists(_folderPath))
            {
                var allDirectories = Directory.EnumerateDirectories(_folderPath, "elev*", SearchOption.AllDirectories);

                foreach (string directory in allDirectories)
                {
                    int elevations = 0;
                    foreach (var file in Directory.EnumerateFiles(directory))
                    {
                        elevations++;
                        HRTFLocator hrtfFile = ParseFileName(Path.GetFileName(file));
                        byte[] data = File.ReadAllBytes(file);
                        float[] vals = new float[512];
                        int v = 0;
                        for (int i = 0; i < data.Length-1; i += 2)
                        {
                            byte a = data[i];
                            byte b = data[i+1];

                            vals[v++] = ((short)(a << 8 | b)) / 32768f;
                        }
                        _dataFiles.Add(hrtfFile, vals);
                    }
                    Console.WriteLine($"{Path.GetFileNameWithoutExtension(directory)}: {elevations}");
                }
                Console.WriteLine();                
            }
        }

        private static HRTFLocator ParseFileName(String name)
        {
            string side = name.Substring(0,1);
            int e = name.IndexOf("e");
            int elev = int.Parse(name.Substring(1, e-1));
            int az = int.Parse(name.Substring(e + 1, 3));

            return new HRTFLocator { Side = side, Elev = elev, Az = az };
        }

        public static float[] GetIr(int elevation, int azimuth, string ear)
        {
            HRTFLocator locator = new HRTFLocator {Side = ear, Elev = elevation, Az = azimuth};
            return _dataFiles[locator];
        }
    }
}
