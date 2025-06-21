using System;
using System.Collections.Generic;
using System.Linq;

namespace UltimateLogSystem
{
    /// <summary>
    /// 日志分析工具
    /// </summary>
    public static class LogAnalyzer
    {
        /// <summary>
        /// 按级别统计日志数量
        /// </summary>
        public static Dictionary<string, int> CountByLevel(IEnumerable<LogEntry> entries)
        {
            return entries
                .GroupBy(e => e.Level.ToString())
                .ToDictionary(g => g.Key, g => g.Count());
        }
        
        /// <summary>
        /// 按类别统计日志数量
        /// </summary>
        public static Dictionary<string, int> CountByCategory(IEnumerable<LogEntry> entries)
        {
            return entries
                .GroupBy(e => e.Category ?? "未分类")
                .ToDictionary(g => g.Key, g => g.Count());
        }
        
        /// <summary>
        /// 按时间段统计日志数量
        /// </summary>
        public static Dictionary<DateTime, int> CountByTimeInterval(IEnumerable<LogEntry> entries, TimeSpan interval)
        {
            return entries
                .GroupBy(e => new DateTime(
                    e.Timestamp.Year,
                    e.Timestamp.Month,
                    e.Timestamp.Day,
                    e.Timestamp.Hour,
                    (e.Timestamp.Minute / interval.Minutes) * interval.Minutes,
                    0))
                .ToDictionary(g => g.Key, g => g.Count());
        }
        
        /// <summary>
        /// 查找包含指定文本的日志
        /// </summary>
        public static IEnumerable<LogEntry> FindByText(IEnumerable<LogEntry> entries, string text, bool ignoreCase = true)
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            
            return entries.Where(e => e.Message.IndexOf(text, comparison) >= 0);
        }
        
        /// <summary>
        /// 查找指定时间范围的日志
        /// </summary>
        public static IEnumerable<LogEntry> FindByTimeRange(IEnumerable<LogEntry> entries, DateTime start, DateTime end)
        {
            return entries.Where(e => e.Timestamp >= start && e.Timestamp <= end);
        }
        
        /// <summary>
        /// 查找指定级别的日志
        /// </summary>
        public static IEnumerable<LogEntry> FindByLevel(IEnumerable<LogEntry> entries, object level)
        {
            string levelStr = level.ToString() ?? "";
            
            return entries.Where(e => e.Level.ToString() == levelStr);
        }
        
        /// <summary>
        /// 查找包含指定异常类型的日志
        /// </summary>
        public static IEnumerable<LogEntry> FindByExceptionType(IEnumerable<LogEntry> entries, Type exceptionType)
        {
            return entries.Where(e => e.Exception != null && 
                (e.Exception.GetType() == exceptionType || e.Exception.GetType().IsSubclassOf(exceptionType)));
        }
        
        /// <summary>
        /// 获取最频繁出现的日志消息
        /// </summary>
        public static IEnumerable<KeyValuePair<string, int>> GetMostFrequentMessages(IEnumerable<LogEntry> entries, int topCount = 10)
        {
            return entries
                .GroupBy(e => e.Message)
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                .OrderByDescending(pair => pair.Value)
                .Take(topCount);
        }
        
        /// <summary>
        /// 获取日志的时间分布统计
        /// </summary>
        public static Dictionary<int, int> GetHourDistribution(IEnumerable<LogEntry> entries)
        {
            var result = new Dictionary<int, int>();
            
            for (int i = 0; i < 24; i++)
            {
                result[i] = 0;
            }
            
            foreach (var entry in entries)
            {
                int hour = entry.Timestamp.Hour;
                result[hour]++;
            }
            
            return result;
        }
    }
} 