using FogSoft.WinForm.Classes;
using log4net;
using System;
using System.Diagnostics;

namespace FogSoft.WinForm.DataAccess
{
    internal sealed class DbExecutionScope : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DbExecutionScope));

        private readonly string _procedureName;
        private readonly int _timeout;
        private readonly bool _cached;
        private readonly Stopwatch _sw;

        private int? _rows;

        //private const int INFO_THRESHOLD_MS = 1000;
        //private const int WARN_THRESHOLD_MS = 1000;

        private DbExecutionScope(
            string procedureName,
            int timeout,
            bool cached)
        {
            _procedureName = procedureName;
            _timeout = timeout;
            _cached = cached;
            _sw = Stopwatch.StartNew();
        }

        public static DbExecutionScope Start(
            string procedureName,
            int timeout,
            bool cached = false)
        {
            return new DbExecutionScope(procedureName, timeout, cached);
        }

        public void SetRows(int rows)
        {
            _rows = rows;
        }

        public void Dispose()
        {
            _sw.Stop();
            long elapsedMs = _sw.ElapsedMilliseconds;

            if (elapsedMs < ConfigurationUtil.StoredProcExecutionTimeThreshold)
                return;

            string msg =
                $"{_procedureName} {elapsedMs}ms " +
                $"timeout={_timeout}" +
                (_rows.HasValue ? $" rows={_rows}" : "") +
                (_cached ? " cached=true" : "");


            Log.Info(msg);
        }
    }
}
