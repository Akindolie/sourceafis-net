// Part of SourceAFIS for .NET: https://sourceafis.machinezoo.com/net
using System;
using System.IO;

namespace SourceAFIS
{
    static class TestResources
    {
        static byte[] Load(string name)
        {
            using (var stream = typeof(TestResources).Assembly.GetManifestResourceStream($"SourceAFIS.Resources.{name}"))
            {
                if (stream == null)
                    throw new ArgumentException($"Resource '{name}' not found.");

                var data = new byte[stream.Length];
                int bytesRead = 0;
                int totalBytesRead = 0;

                while ((bytesRead = stream.Read(data, totalBytesRead, data.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;
                }

                if (totalBytesRead != data.Length)
                    throw new EndOfStreamException($"Expected {data.Length} bytes but only read {totalBytesRead} bytes.");

                return data;
            }
        }

        public static byte[] Png() => Load("probe.png");
        public static byte[] Jpeg() => Load("probe.jpeg");
        public static byte[] Bmp() => Load("probe.bmp");
        public static byte[] Probe() => Load("probe.png");
        public static byte[] Matching() => Load("matching.png");
        public static byte[] Nonmatching() => Load("nonmatching.png");
        public static byte[] ProbeGray() => Load("gray-probe.dat");
        public static byte[] MatchingGray() => Load("gray-matching.dat");
        public static byte[] NonmatchingGray() => Load("gray-nonmatching.dat");
        public static byte[] DevProbe() => Load("devprobe.tif");
        public static byte[] DevCandidate() => Load("devcandidate.tif");
        public static byte[] DevNonMatching() => Load("devnonmatching.tif");
    }
}