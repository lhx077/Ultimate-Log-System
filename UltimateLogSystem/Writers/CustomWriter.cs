using System;
using System.Collections.Generic;  // 提供 Dictionary<,> 和 Queue<>
using System.IO;                   // 提供 StreamWriter
using System.Net.Http;            // 用于 HttpWriter
namespace UltimateLogSystem.Writers
{
    /// <summary>
    /// 自定义日志输出
    /// </summary>
    public class CustomWriter : ILogWriter
    {
        private readonly Action<LogEntry> _writeAction;
        private readonly Action? _flushAction;
        private readonly Action? _disposeAction;
        
        public CustomWriter(
            Action<LogEntry> writeAction,
            Action? flushAction = null,
            Action? disposeAction = null)
        {
            _writeAction = writeAction;
            _flushAction = flushAction;
            _disposeAction = disposeAction;
        }
        
        public void Write(LogEntry entry)
        {
            _writeAction(entry);
        }
        
        public void Flush()
        {
            _flushAction?.Invoke();
        }
        
        public void Dispose()
        {
            _disposeAction?.Invoke();
        }
    }
} 