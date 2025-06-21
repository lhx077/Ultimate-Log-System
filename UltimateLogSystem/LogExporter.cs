using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UltimateLogSystem.Formatters;
using UltimateLogSystem.Parsers;
using System.Linq;

namespace UltimateLogSystem
{
    /// <summary>
    /// 日志导出工具
    /// </summary>
    public static class LogExporter
    {
        /// <summary>
        /// 导出日志到文件
        /// </summary>
        public static void ExportToFile(IEnumerable<LogEntry> entries, string filePath, ILogFormatter formatter)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            
            foreach (var entry in entries)
            {
                writer.WriteLine(formatter.Format(entry));
            }
        }
        
        /// <summary>
        /// 导出日志到文本文件
        /// </summary>
        public static void ExportToTextFile(IEnumerable<LogEntry> entries, string filePath, string template = "[{timestamp}] [{level}] {message}")
        {
            ExportToFile(entries, filePath, new TextFormatter(template));
        }
        
        /// <summary>
        /// 导出日志到JSON文件
        /// </summary>
        public static void ExportToJsonFile(IEnumerable<LogEntry> entries, string filePath)
        {
            ExportToFile(entries, filePath, new JsonFormatter());
        }
        
        /// <summary>
        /// 导出日志到XML文件
        /// </summary>
        public static void ExportToXmlFile(IEnumerable<LogEntry> entries, string filePath)
        {
            ExportToFile(entries, filePath, new XmlFormatter());
        }
        
        /// <summary>
        /// 从文件导入日志
        /// </summary>
        public static IEnumerable<LogEntry> ImportFromFile(string filePath, ILogParser parser)
        {
            if (!File.Exists(filePath))
            {
                return Enumerable.Empty<LogEntry>();
            }
            
            return parser.ParseFile(filePath);
        }
        
        /// <summary>
        /// 从文本文件导入日志
        /// </summary>
        public static IEnumerable<LogEntry> ImportFromTextFile(string filePath, string pattern = @"\[(.*?)\] \[(.*?)\] (.*)")
        {
            return ImportFromFile(filePath, new TextLogParser(pattern));
        }
        
        /// <summary>
        /// 从JSON文件导入日志
        /// </summary>
        public static IEnumerable<LogEntry> ImportFromJsonFile(string filePath)
        {
            return ImportFromFile(filePath, new JsonLogParser());
        }
    }
} 