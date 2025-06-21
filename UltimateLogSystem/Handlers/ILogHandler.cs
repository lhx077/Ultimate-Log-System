namespace UltimateLogSystem.Handlers
{
    /// <summary>
    /// 日志处理器接口
    /// </summary>
    public interface ILogHandler
    {
        /// <summary>
        /// 处理日志条目
        /// </summary>
        void Handle(LogEntry entry);
        
        /// <summary>
        /// 检查是否应该处理该日志条目
        /// </summary>
        bool ShouldHandle(LogEntry entry);
    }
} 