using System;

namespace Common
{
    public sealed class SpectrumDataHelper
    {
        private static readonly SpectrumDataHelper instance = new();
        private static float[] spectrumData = Array.Empty<float>();

        public static SpectrumDataHelper Instance { get => instance; }
        public static float[] SpectrumData { get => spectrumData; set => SetSpectrumData(ref value); }

        private static void SetSpectrumData(ref float[] data)
        {
            if (spectrumData.Length != data.Length)
                spectrumData = new float[data.Length];
            spectrumData = data;
        }

        public static float GetCurrentLoudness()
        {
            const float FRACTION = 15;
            int scanLimit = (int)(spectrumData.Length / FRACTION);
            float loudness = 0.0f;
            for (int i = 0; i < scanLimit; i++)
                loudness += DeNullifiedSpectrumData(i);
            return Math.Clamp(loudness / FRACTION, 0.0f, 1.0f);
        }
        public static float DeNullifiedSpectrumData(int i) => (spectrumData != null && spectrumData.Length > i) ? spectrumData[i] : 0.0f;
    }
}
