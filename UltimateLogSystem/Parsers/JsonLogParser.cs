using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UltimateLogSystem.Parsers
{
    /// <summary>
    /// JSON日志解析器
    /// </summary>
    public class JsonLogParser : ILogParser
    {
        private readonly JsonSerializerOptions _options;
        
        public JsonLogParser(JsonSerializerOptions? options = null)
        {
            _options = options ?? new JsonSerializerOptions();
        }
        
        public IEnumerable<LogEntry> Parse(string logContent)
        {
            using StringReader reader = new StringReader(logContent);
            string? line;
            
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                    
                var entry = ParseJson(line);
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
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                    
                var entry = ParseJson(line);
                if (entry != null)
                {
                    yield return entry;
                }
            }
        }
        
        private LogEntry? ParseJson(string json)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;
                
                if (root.TryGetProperty("Timestamp", out var timestampElement) &&
                    root.TryGetProperty("Level", out var levelElement) &&
                    root.TryGetProperty("Message", out var messageElement))
                {
                    DateTime timestamp = timestampElement.GetDateTime();
                    string levelStr = levelElement.GetString() ?? "Info";
                    string message = messageElement.GetString() ?? "";
                    
                    string? category = null;
                    if (root.TryGetProperty("Category", out var categoryElement))
                    {
                        category = categoryElement.GetString();
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
                    
                    var entry = new LogEntry(timestamp, level, category, message);
                    
                    if (root.TryGetProperty("Properties", out var propsElement) && 
                        propsElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in propsElement.EnumerateObject())
                        {
                            entry.Properties[prop.Name] = prop.Value.GetString();
                        }
                    }
                    
                    return entry;
                }
            }
            catch
            {
                // 解析失败，返回null
            }
            
            return null;
        }
    }
} 