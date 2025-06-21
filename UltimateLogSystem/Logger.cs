using System;
using System.Collections.Concurrent;
using System.Threading;

namespace UltimateLogSystem
{
    /// <summary>
    /// 日志记录器
    /// </summary>
    public class Logger : IDisposable
    {
        private readonly LoggerConfiguration _configuration;
        private readonly ConcurrentQueue<LogEntry> _pendingEntries = new ConcurrentQueue<LogEntry>();
        private readonly Thread _processingThread;
        private readonly ManualResetEventSlim _shutdownEvent = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _newEntryEvent = new ManualResetEventSlim(false);
        private bool _disposed;
        
        public Logger(LoggerConfiguration configuration)
        {
            _configuration = configuration;
            
            // 创建后台处理线程
            _processingThread = new Thread(ProcessLogEntries)
            {
                IsBackground = true,
                Name = "LogProcessingThread"
            };
            _processingThread.Start();
        }
        
        /// <summary>
        /// 创建日志条目
        /// </summary>
        private LogEntry CreateEntry(object level, string message, string? category = null, Exception? exception = null)
        {
            return new LogEntry(
                DateTime.Now,
                level,
                category ?? _configuration.DefaultCategory,
                message,
                exception
            );
        }
        
        /// <summary>
        /// 判断是否记录该级别的日志
        /// </summary>
        private bool ShouldLog(object level)
        {
            if (_configuration.MinimumLevel is LogLevel minLevel && level is LogLevel entryLevel)
            {
                return (int)entryLevel >= (int)minLevel;
            }
            
            if (_configuration.MinimumLevel is CustomLogLevel minCustomLevel && level is CustomLogLevel entryCustomLevel)
            {
                return entryCustomLevel.Value >= minCustomLevel.Value;
            }
            
            // 无法比较级别，默认记录
            return true;
        }
        
        /// <summary>
        /// 记录日志
        /// </summary>
        public void Log(object level, string message, string? category = null, Exception? exception = null)
        {
            if (!ShouldLog(level))
                return;
                
            var entry = CreateEntry(level, message, category, exception);
            _pendingEntries.Enqueue(entry);
            _newEntryEvent.Set();
        }
        
        /// <summary>
        /// 处理日志条目
        /// </summary>
        private void ProcessLogEntries()
        {
            try
            {
                while (!_shutdownEvent.IsSet)
                {
                    // 等待新日志或关闭信号
                    _newEntryEvent.Wait();
                    
                    while (_pendingEntries.TryDequeue(out var entry))
                    {
                        // 写入日志
                        foreach (var writer in _configuration.Writers)
                        {
                            try
                            {
                                writer.Write(entry);
                            }
                            catch
                            {
                                // 忽略写入错误
                            }
                        }
                        
                        // 处理日志
                        foreach (var handler in _configuration.Handlers)
                        {
                            try
                            {
                                if (handler.ShouldHandle(entry))
                                {
                                    handler.Handle(entry);
                                }
                            }
                            catch
                            {
                                // 忽略处理错误
                            }
                        }
                    }
                    
                    _newEntryEvent.Reset();
                }
            }
            catch
            {
                // 忽略处理线程异常
            }
        }
        
        /// <summary>
        /// 记录跟踪日志
        /// </summary>
        public void Trace(string message, string? category = null, Exception? exception = null)
        {
            Log(LogLevel.Trace, message, category, exception);
        }
        
        /// <summary>
        /// 记录调试日志
        /// </summary>
        public void Debug(string message, string? category = null, Exception? exception = null)
        {
            Log(LogLevel.Debug, message, category, exception);
        }
        
        /// <summary>
        /// 记录信息日志
        /// </summary>
        public void Info(string message, string? category = null, Exception? exception = null)
        {
            Log(LogLevel.Info, message, category, exception);
        }
        
        /// <summary>
        /// 记录警告日志
        /// </summary>
        public void Warning(string message, string? category = null, Exception? exception = null)
        {
            Log(LogLevel.Warning, message, category, exception);
        }
        
        /// <summary>
        /// 记录错误日志
        /// </summary>
        public void Error(string message, string? category = null, Exception? exception = null)
        {
            Log(LogLevel.Error, message, category, exception);
        }
        
        /// <summary>
        /// 记录致命错误日志
        /// </summary>
        public void Fatal(string message, string? category = null, Exception? exception = null)
        {
            Log(LogLevel.Fatal, message, category, exception);
        }
        
        /// <summary>
        /// 刷新所有日志输出
        /// </summary>
        public void Flush()
        {
            foreach (var writer in _configuration.Writers)
            {
                try
                {
                    writer.Flush();
                }
                catch
                {
                    // 忽略刷新错误
                }
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            
            // 通知处理线程关闭
            _shutdownEvent.Set();
            _newEntryEvent.Set();
            
            // 等待处理线程结束
            if (_processingThread.IsAlive)
            {
                _processingThread.Join(1000);
            }
            
            // 刷新并释放所有输出
            foreach (var writer in _configuration.Writers)
            {
                try
                {
                    writer.Flush();
                    writer.Dispose();
                }
                catch
                {
                    // 忽略释放错误
                }
            }
            
            _shutdownEvent.Dispose();
            _newEntryEvent.Dispose();
        }
    }
} 