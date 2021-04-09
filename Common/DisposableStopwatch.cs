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
#if (DEBUG)
            this.name = name;
            this.displayStartFinished = displayStartFinished;
            if (this.displayStartFinished)
                Debug.WriteLine("[" + this.name + "]: Started");
            Start();
#endif
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
#if (DEBUG)
                if (disposing)
                    if (displayStartFinished)
                        Debug.WriteLine("[{0}]: Finished -> {1}", name, Elapsed);
                    else
                        Debug.WriteLine("[{0}]: {1}", name, Elapsed);
#endif

                disposedValue = true;
            }
        }

        public void Dispose()
        {
#if (DEBUG)
            Stop();
#endif
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
