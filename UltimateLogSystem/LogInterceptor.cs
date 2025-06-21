using System;

namespace UltimateLogSystem
{
    /// <summary>
    /// 日志拦截器
    /// </summary>
    public class LogInterceptor
    {
        private readonly Func<LogEntry, bool> _predicate;
        private readonly Action<LogEntry> _action;
        
        public LogInterceptor(Func<LogEntry, bool> predicate, Action<LogEntry> action)
        {
            _predicate = predicate;
            _action = action;
        }
        
        /// <summary>
        /// 处理日志条目
        /// </summary>
        public bool Process(LogEntry entry)
        {
            if (_predicate(entry))
            {
                _action(entry);
                return true;
            }
            
            return false;
        }
    }
} 