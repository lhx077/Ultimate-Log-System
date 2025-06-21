using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace UltimateLogSystem
{
    /// <summary>
    /// 日志条目
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// 日志时间戳
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// 日志级别
        /// </summary>
        public object Level { get; }
        
        /// <summary>
        /// 日志类别
        /// </summary>
        public string? Category { get; }
        
        /// <summary>
        /// 日志消息
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception? Exception { get; }
        
        /// <summary>
        /// 额外属性
        /// </summary>
        public Dictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();
        
        public LogEntry(DateTime timestamp, object level, string? category, string message, Exception? exception = null)
        {
            Timestamp = timestamp;
            Level = level;
            Category = category;
            Message = message;
            Exception = exception;
        }
    }
} 