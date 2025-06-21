using System.Collections.Generic;

namespace UltimateLogSystem.Parsers
{
    /// <summary>
    /// 日志解析器接口
    /// </summary>
    public interface ILogParser
    {
        /// <summary>
        /// 解析日志文本为日志条目
        /// </summary>
        IEnumerable<LogEntry> Parse(string logContent);
        
        /// <summary>
        /// 从文件解析日志条目
        /// </summary>
        IEnumerable<LogEntry> ParseFile(string filePath);
    }
} 