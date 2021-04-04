﻿using CSCore;
using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.Streams;

using System;

namespace WasAPI
{
    public class WasAPIAudio : IDisposable
    {
        private const FftSize C_FftSize = FftSize.Fft16384;
        private const float maxAudioValue = 1.0f;

        private readonly int spectrumSize;
        private readonly int minFrequency;
        private readonly int maxFrequency;

        private readonly WasapiCapture capture = new WasapiLoopbackCapture();
        private SoundInSource soundInSource;
        private BasicSpectrumProvider basicSpectrumProvider;
        private readonly Action<float[]> receiveAudio;
        private LineSpectrum lineSpectrum;
        private SingleBlockNotificationStream singleBlockNotificationStream;
        private IWaveSource realtimeSource;

        private bool disposedValue;

        public WasAPIAudio(int spectrumSize, int minFrequency, int maxFrequency, Action<float[]> receiveAudio)
        {
            this.spectrumSize = spectrumSize;
            this.minFrequency = minFrequency;
            this.maxFrequency = maxFrequency;
            this.receiveAudio = receiveAudio;
        }

        public void StartListen()
        {
            capture.Initialize();
            soundInSource = new SoundInSource(capture);
            basicSpectrumProvider = new BasicSpectrumProvider(soundInSource.WaveFormat.Channels, soundInSource.WaveFormat.SampleRate, C_FftSize);
            lineSpectrum = new LineSpectrum(C_FftSize, minFrequency, maxFrequency)
            {
                SpectrumProvider = basicSpectrumProvider,
                BarCount = spectrumSize,
                UseAverage = true,
                IsXLogScale = true,
                ScalingStrategy = EScalingStrategy.Linear
            };

            capture.Start();

            ISampleSource sampleSource = soundInSource.ToSampleSource();

            singleBlockNotificationStream = new SingleBlockNotificationStream(sampleSource);
            realtimeSource = singleBlockNotificationStream.ToWaveSource();

            byte[] buffer = new byte[realtimeSource.WaveFormat.BytesPerSecond / 32];

            soundInSource.DataAvailable += (s, ea) =>
            {
                while (realtimeSource.Read(buffer, 0, buffer.Length) > 0)
                {
                    var spectrumData = lineSpectrum.GetSpectrumData(maxAudioValue);

                    if (spectrumData != null)
                    {
                        receiveAudio?.Invoke(spectrumData);
                    }
                }
            };

            singleBlockNotificationStream.SingleBlockRead += SingleBlockNotificationStream_SingleBlockRead;
        }

        public void StopListen()
        {
            if (capture.RecordingState == RecordingState.Recording)
                capture.Stop();
            capture.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    StopListen();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void SingleBlockNotificationStream_SingleBlockRead(object sender, SingleBlockReadEventArgs e)
        {
            basicSpectrumProvider.Add(e.Left, e.Right);
        }
    }
}
