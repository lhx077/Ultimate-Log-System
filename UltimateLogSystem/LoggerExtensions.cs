using System;
using System.Collections.Generic;

namespace UltimateLogSystem
{
    /// <summary>
    /// 日志记录器扩展
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// 使用上下文执行操作
        /// </summary>
        public static void WithContext(this Logger logger, Action action, Dictionary<string, object?> contextProperties)
        {
            var originalProperties = new Dictionary<string, object?>(LogContext.Properties);
            
            try
            {
                foreach (var prop in contextProperties)
                {
                    LogContext.SetProperty(prop.Key, prop.Value);
                }
                
                action();
            }
            finally
            {
                LogContext.ClearProperties();
                
                foreach (var prop in originalProperties)
                {
                    LogContext.SetProperty(prop.Key, prop.Value);
                }
            }
        }
        
        /// <summary>
        /// 使用上下文执行操作
        /// </summary>
        public static T WithContext<T>(this Logger logger, Func<T> func, Dictionary<string, object?> contextProperties)
        {
            var originalProperties = new Dictionary<string, object?>(LogContext.Properties);
            
            try
            {
                foreach (var prop in contextProperties)
                {
                    LogContext.SetProperty(prop.Key, prop.Value);
                }
                
                return func();
            }
            finally
            {
                LogContext.ClearProperties();
                
                foreach (var prop in originalProperties)
                {
                    LogContext.SetProperty(prop.Key, prop.Value);
                }
            }
        }
        
        /// <summary>
        /// 使用指定属性记录日志
        /// </summary>
        public static void LogWithProperties(this Logger logger, object level, string message, Dictionary<string, object?> properties, string? category = null, Exception? exception = null)
        {
            var entry = new LogEntry(DateTime.Now, level, category, message, exception);
            
            foreach (var prop in properties)
            {
                entry.Properties[prop.Key] = prop.Value;
            }
            
            LogContext.EnrichLogEntry(entry);
            
            logger.Log(level, message, category, exception);
        }
        
        /// <summary>
        /// 使用结构化数据记录日志
        /// </summary>
        public static void LogStructured(this Logger logger, object level, string messageTemplate, object values, string? category = null, Exception? exception = null)
        {
            var properties = new Dictionary<string, object?>();
            
            // 通过反射获取对象的属性
            var type = values.GetType();
            foreach (var prop in type.GetProperties())
            {
                properties[prop.Name] = prop.GetValue(values);
            }
            
            // 替换模板中的占位符
            string message = messageTemplate;
            foreach (var prop in properties)
            {
                message = message.Replace("{" + prop.Key + "}", prop.Value?.ToString() ?? "null");
            }
            
            logger.LogWithProperties(level, message, properties, category, exception);
        }
        
        /// <summary>
        /// 记录操作执行时间
        /// </summary>
        public static T Time<T>(this Logger logger, Func<T> action, string operationName, string? category = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                return action();
            }
            finally
            {
                stopwatch.Stop();
                logger.Info($"操作 '{operationName}' 执行时间: {stopwatch.ElapsedMilliseconds}ms", category);
            }
        }
        
        /// <summary>
        /// 记录操作执行时间
        /// </summary>
        public static void Time(this Logger logger, Action action, string operationName, string? category = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                action();
            }
            finally
            {
                stopwatch.Stop();
                logger.Info($"操作 '{operationName}' 执行时间: {stopwatch.ElapsedMilliseconds}ms", category);
            }
        }
        
        /// <summary>
        /// 安全执行操作并记录异常
        /// </summary>
        public static void SafeExecute(this Logger logger, Action action, string operationName, string? category = null)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                logger.Error($"执行操作 '{operationName}' 时发生异常", category, ex);
            }
        }
        
        /// <summary>
        /// 安全执行操作并记录异常
        /// </summary>
        public static T? SafeExecute<T>(this Logger logger, Func<T> func, string operationName, string? category = null, T? defaultValue = default)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                logger.Error($"执行操作 '{operationName}' 时发生异常", category, ex);
                return defaultValue;
            }
        }
    }
} 