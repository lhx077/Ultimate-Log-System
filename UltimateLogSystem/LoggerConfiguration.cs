using System;
using System.Collections.Generic;
using UltimateLogSystem.Formatters;
using UltimateLogSystem.Writers;
using UltimateLogSystem.Handlers;

namespace UltimateLogSystem
{
    /// <summary>
    /// 日志配置
    /// </summary>
    public class LoggerConfiguration
    {
        public object MinimumLevel { get; set; } = LogLevel.Info;
        public List<ILogWriter> Writers { get; } = new List<ILogWriter>();
        public List<ILogHandler> Handlers { get; } = new List<ILogHandler>();
        public string? DefaultCategory { get; set; }
        
        /// <summary>
        /// 设置最小日志级别
        /// </summary>
        public LoggerConfiguration SetMinimumLevel(object level)
        {
            MinimumLevel = level;
            return this;
        }
        
        /// <summary>
        /// 添加控制台输出
        /// </summary>
        public LoggerConfiguration AddConsoleWriter(ILogFormatter? formatter = null)
        {
            Writers.Add(new ColorConsoleWriter(formatter ?? new TextFormatter()));
            return this;
        }
        
        /// <summary>
        /// 添加文件输出
        /// </summary>
        public LoggerConfiguration AddFileWriter(
            string filePath,
            ILogFormatter? formatter = null,
            long maxFileSize = 10 * 1024 * 1024,
            int maxRollingFiles = 5)
        {
            Writers.Add(new RollingFileWriter(filePath, formatter, maxFileSize, maxRollingFiles));
            return this;
        }
        
        /// <summary>
        /// 添加自定义输出
        /// </summary>
        public LoggerConfiguration AddWriter(ILogWriter writer)
        {
            Writers.Add(writer);
            return this;
        }
        
        /// <summary>
        /// 添加处理器
        /// </summary>
        public LoggerConfiguration AddHandler(ILogHandler handler)
        {
            Handlers.Add(handler);
            return this;
        }
        
        /// <summary>
        /// 添加邮件通知处理器
        /// </summary>
        public LoggerConfiguration AddEmailNotification(
            string smtpServer,
            int smtpPort,
            string fromEmail,
            string toEmail,
            string username,
            string password,
            object? levelToHandle = null)
        {
            levelToHandle ??= LogLevel.Error;
            Handlers.Add(new EmailNotificationHandler(
                smtpServer, smtpPort, fromEmail, toEmail, username, password, levelToHandle));
            return this;
        }
        
        /// <summary>
        /// 添加回调处理器
        /// </summary>
        public LoggerConfiguration AddAction(Action<LogEntry> action, Func<LogEntry, bool>? predicate = null)
        {
            Handlers.Add(new ActionHandler(action, predicate));
            return this;
        }
        
        /// <summary>
        /// 设置默认类别
        /// </summary>
        public LoggerConfiguration SetDefaultCategory(string category)
        {
            DefaultCategory = category;
            return this;
        }
    }
} 