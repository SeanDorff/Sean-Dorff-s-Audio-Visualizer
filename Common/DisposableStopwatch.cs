using System;
using System.Diagnostics;

namespace Common
{
    /// <summary>
    /// <para>
    /// This class extends <see cref="System.Diagnostics.Stopwatch"/> with the <see cref="System.IDisposable"/> interface.
    /// </para>
    /// <para>
    /// Upon being instantiated it will start a <see cref="System.Diagnostics.Stopwatch"/>.
    /// </para>
    /// <para>
    /// When the class is being disposed the <see cref="System.Diagnostics.Stopwatch"/> is being stopped and the elapsed time will be written to the debug output.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The class turns off its main functionality using preprocessor statements. If your not running a debug build it will do almost nothing.
    /// </remarks>
    public class DisposableStopwatch : Stopwatch, IDisposable
    {
        private bool disposedValue;
        private readonly bool displayStartFinished;
        private readonly string name;

        /// <summary>
        /// Constructor returning an already started <see cref="System.Diagnostics.Stopwatch"/>.
        /// </summary>
        /// <param name="name">The of the <see cref="System.Diagnostics.Stopwatch"/> used in debug output.</param>
        /// <param name="displayStartFinished">Toggles display of start an finish in debug output. Default is <c>false</c>.</param>
        /// <example>
        /// <code>
        /// using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true))
        /// {
        ///    Console.WriteLine("Hello World!");
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        ///   <para>
        ///   If <paramref name="displayStartFinished"/> is <c>false</c> only the measured running time of the <see cref="System.Diagnostics.Stopwatch"/> will be displayed in debug output.
        ///   </para>
        ///   <para>
        ///   The easiest way to use this class is by calling it in a <c>using</c> clause, e.g. <c>using (new DisposableStopwatch(MethodBase.GetCurrentMethod().Name, true)</c>.
        ///   </para>
        /// </remarks>
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
