using log4net;
using System;
using System.Diagnostics;
using System.Reflection;

namespace FogSoft.WinForm.Classes
{
    public static class PerformanceManager
    {
        private static readonly ILog Log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static IDisposable LogExecutionTime(string procedureName)
        {
            return new ExecutionTimeScope(procedureName);
        }

        private sealed class ExecutionTimeScope : IDisposable
        {
            private readonly string _procedureName;
            private readonly Stopwatch _stopwatch;

            public ExecutionTimeScope(string procedureName)
            {
                _procedureName = procedureName;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                Log.Info($"[PERF] {_procedureName} executed in {_stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }
}
