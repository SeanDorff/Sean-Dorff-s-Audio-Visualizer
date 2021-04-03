using CSCore.DSP;
using CSCore.SoundIn;

using System;

namespace WasAPI
{
    public class WasAPIAudio : IDisposable
    {
        private const FftSize fftSize = FftSize.Fft4096;
        private const float maxAudioVolume = 1.0f;

        private readonly WasapiCapture capture = new WasapiLoopbackCapture();
        private bool disposedValue;

        public void StartListen()
        {
            capture.Initialize();
            capture.Start();
        }

        public void StopListen()
        {
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
    }
}
