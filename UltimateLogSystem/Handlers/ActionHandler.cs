using System;

namespace UltimateLogSystem.Handlers
{
    /// <summary>
    /// 回调处理器
    /// </summary>
    public class ActionHandler : ILogHandler
    {
        private readonly Action<LogEntry> _action;
        private readonly Func<LogEntry, bool> _predicate;
        
        public ActionHandler(Action<LogEntry> action, Func<LogEntry, bool>? predicate = null)
        {
            _action = action;
            _predicate = predicate ?? (_ => true);
        }
        
        public void Handle(LogEntry entry)
        {
            _action(entry);
        }
        
        public bool ShouldHandle(LogEntry entry)
        {
            return _predicate(entry);
        }
    }
} 