namespace UltimateLogSystem.Formatters
{
    /// <summary>
    /// 文本格式化器
    /// </summary>
    public class TextFormatter : ILogFormatter
    {
        private readonly string _template;
        
        public TextFormatter(string template = "[{timestamp}] [{level}] {message}")
        {
            _template = template;
        }
        
        public string Format(LogEntry entry)
        {
            string result = _template
                .Replace("{timestamp}", entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                .Replace("{level}", entry.Level.ToString())
                .Replace("{message}", entry.Message);
                
            if(entry.Category != null && _template.Contains("{category}"))
            {
                result = result.Replace("{category}", entry.Category);
            }
            
            return result;
        }
    }
} 