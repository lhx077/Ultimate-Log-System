using System;
using System.Collections.Generic;  // 提供 Dictionary<,> 和 Queue<>
using System.IO;                   // 提供 StreamWriter
using System.Net.Http;            // 用于 HttpWriter
namespace UltimateLogSystem
{
    /// <summary>
    /// 日志工厂
    /// </summary>
    public static class LoggerFactory
    {
        private static readonly Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>();
        private static readonly object _lock = new object();
        
        /// <summary>
        /// 创建日志记录器
        /// </summary>
        public static Logger CreateLogger(LoggerConfiguration configuration, string? name = null)
        {
            lock (_lock)
            {
                name ??= "Default";
                
                if (_loggers.TryGetValue(name, out var existingLogger))
                {
                    existingLogger.Dispose();
                }
                
                var logger = new Logger(configuration);
                _loggers[name] = logger;
                
                return logger;
            }
        }
        
        /// <summary>
        /// 获取日志记录器
        /// </summary>
        public static Logger GetLogger(string? name = null)
        {
            lock (_lock)
            {
                name ??= "Default";
                
                if (_loggers.TryGetValue(name, out var logger))
                {
                    return logger;
                }
                
                throw new InvalidOperationException($"Logger '{name}' not found. Create it first with CreateLogger.");
            }
        }
        
        /// <summary>
        /// 关闭所有日志记录器
        /// </summary>
        public static void CloseAll()
        {
            lock (_lock)
            {
                foreach (var logger in _loggers.Values)
                {
                    logger.Dispose();
                }
                
                _loggers.Clear();
            }
        }
    }
} 