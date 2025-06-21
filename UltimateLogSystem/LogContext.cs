using System;
using System.Collections.Generic;
using System.Threading;

namespace UltimateLogSystem
{
    /// <summary>
    /// 日志上下文
    /// </summary>
    public static class LogContext
    {
        private static readonly AsyncLocal<Dictionary<string, object?>> _properties = new AsyncLocal<Dictionary<string, object?>>();
        
        /// <summary>
        /// 获取当前上下文属性
        /// </summary>
        public static Dictionary<string, object?> Properties
        {
            get
            {
                _properties.Value ??= new Dictionary<string, object?>();
                return _properties.Value;
            }
        }
        
        /// <summary>
        /// 设置上下文属性
        /// </summary>
        public static void SetProperty(string key, object? value)
        {
            Properties[key] = value;
        }
        
        /// <summary>
        /// 获取上下文属性
        /// </summary>
        public static object? GetProperty(string key)
        {
            return Properties.TryGetValue(key, out var value) ? value : null;
        }
        
        /// <summary>
        /// 清除所有上下文属性
        /// </summary>
        public static void ClearProperties()
        {
            Properties.Clear();
        }
        
        /// <summary>
        /// 为日志条目添加上下文属性
        /// </summary>
        public static void EnrichLogEntry(LogEntry entry)
        {
            foreach (var prop in Properties)
            {
                if (!entry.Properties.ContainsKey(prop.Key))
                {
                    entry.Properties[prop.Key] = prop.Value;
                }
            }
        }
    }
} 