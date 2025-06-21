using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace UltimateLogSystem.Parsers
{
    /// <summary>
    /// 文本日志解析器
    /// </summary>
    public class TextLogParser : ILogParser
    {
        private readonly Regex _logPattern;
        
        public TextLogParser(string pattern = @"\[(.*?)\] \[(.*?)\] (.*)")
        {
            _logPattern = new Regex(pattern, RegexOptions.Compiled);
        }
        
        public IEnumerable<LogEntry> Parse(string logContent)
        {
            using StringReader reader = new StringReader(logContent);
            string? line;
            
            while ((line = reader.ReadLine()) != null)
            {
                var entry = ParseLine(line);
                if (entry != null)
                {
                    yield return entry;
                }
            }
        }
        
        public IEnumerable<LogEntry> ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                yield break;
            }
            
            using StreamReader reader = new StreamReader(filePath);
            string? line;
            
            while ((line = reader.ReadLine()) != null)
            {
                var entry = ParseLine(line);
                if (entry != null)
                {
                    yield return entry;
                }
            }
        }
        
        private LogEntry? ParseLine(string line)
        {
            var match = _logPattern.Match(line);
            if (!match.Success || match.Groups.Count < 4)
            {
                return null;
            }
            
            string timestampStr = match.Groups[1].Value;
            string levelStr = match.Groups[2].Value;
            string message = match.Groups[3].Value;
            
            if (!DateTime.TryParse(timestampStr, out DateTime timestamp))
            {
                timestamp = DateTime.Now;
            }
            
            object level;
            if (Enum.TryParse<LogLevel>(levelStr, true, out var logLevel))
            {
                level = logLevel;
            }
            else
            {
                level = CustomLogLevel.Create(0, levelStr);
            }
            
            return new LogEntry(timestamp, level, null, message);
        }
    }
} 