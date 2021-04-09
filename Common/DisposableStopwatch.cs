using System;
using System.Diagnostics;

namespace Common
{
    public class DisposableStopwatch : Stopwatch, IDisposable
    {
        private bool disposedValue;
        private readonly bool displayStartFinished;
        private readonly string name;

        public DisposableStopwatch(string name, bool displayStartFinished = false)
        {
            this.name = name;
            this.displayStartFinished = displayStartFinished;
            if (this.displayStartFinished)
                Debug.WriteLine("[" + this.name + "]: Started");
            Start();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    if (displayStartFinished)
                        Debug.WriteLine("[{0}]: Finished -> {1}", name, Elapsed);
                    else
                        Debug.WriteLine("[{0}]: {1}", name, Elapsed);

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Stop();
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
