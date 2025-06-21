using System;

namespace UltimateLogSystem.WebViewer
{
    /// <summary>
    /// 日志配置Web查看器扩展
    /// </summary>
    public static class LoggerConfigurationExtensions
    {
        /// <summary>
        /// 添加Web查看器
        /// </summary>
        public static LoggerConfiguration AddSimpleWebViewer(
            this LoggerConfiguration configuration,
            string logDirectory,
            int port = 5000,
            string? username = null,
            string? password = null)
        {
            var viewer = new SimpleHttpLogViewer(logDirectory, port, username, password);
            viewer.Start();
            
            return configuration;
        }
    }
} 