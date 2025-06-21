namespace UltimateLogSystem.Formatters
{
    /// <summary>
    /// 日志格式化器接口
    /// </summary>
    public interface ILogFormatter
    {
        /// <summary>
        /// 格式化日志条目
        /// </summary>
        string Format(LogEntry entry);
    }
} 