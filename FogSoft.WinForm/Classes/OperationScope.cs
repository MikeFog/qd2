using log4net;
using System;
using System.Diagnostics;

namespace FogSoft.WinForm.Classes
{
    /// <summary>
    /// Lightweight timing scope for application-level operations.
    /// Mirrors DbExecutionScope (FogSoft.WinForm.DataAccess) but targets C#-layer entry points.
    /// Logs at Debug level on Dispose — always logs if Debug is enabled, no threshold.
    /// </summary>
    public sealed class OperationScope : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OperationScope));

        private readonly string _name;
        private readonly Stopwatch _sw;

        private OperationScope(string name)
        {
            _name = name;
            _sw = Stopwatch.StartNew();
        }

        public static OperationScope Start(string name)
            => new OperationScope(name);

        public void Dispose()
        {
            _sw.Stop();
            if (Log.IsDebugEnabled)
                Log.Debug($"{_name} {_sw.ElapsedMilliseconds}ms");
        }
    }
}
