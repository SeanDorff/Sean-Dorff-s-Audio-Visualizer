using System;

namespace Common
{
    public static class SpectrumDataHelper
    {
        public static float GetCurrentLoudness(ref float[] spectrumData)
        {
            const float FRACTION = 15;
            int scanLimit = (int)(spectrumData.Length / FRACTION);
            float loudness = 0.0f;
            for (int i = 0; i < scanLimit; i++)
                loudness += DeNullifiedSpectrumData(ref spectrumData, i);
            return Math.Clamp(loudness / FRACTION, 0.0f, 1.0f);
        }
        public static float DeNullifiedSpectrumData(ref float[] spectrumData, int i) => (spectrumData != null) ? spectrumData[i] : 0.0f;
    }
}
