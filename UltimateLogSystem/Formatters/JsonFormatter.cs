using System.Text.Json;

namespace UltimateLogSystem.Formatters
{
    /// <summary>
    /// JSON格式化器
    /// </summary>
    public class JsonFormatter : ILogFormatter
    {
        private readonly JsonSerializerOptions _options;
        
        public JsonFormatter(JsonSerializerOptions? options = null)
        {
            _options = options ?? new JsonSerializerOptions 
            { 
                WriteIndented = true 
            };
        }
        
        public string Format(LogEntry entry)
        {
            var logObject = new
            {
                Timestamp = entry.Timestamp,
                Level = entry.Level.ToString(),
                Category = entry.Category,
                Message = entry.Message,
                Exception = entry.Exception?.ToString(),
                Properties = entry.Properties
            };
            
            return JsonSerializer.Serialize(logObject, _options);
        }
    }
} 