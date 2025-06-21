using System.Xml;
using System.Xml.Linq;

namespace UltimateLogSystem.Formatters
{
    /// <summary>
    /// XML格式化器
    /// </summary>
    public class XmlFormatter : ILogFormatter
    {
        public string Format(LogEntry entry)
        {
            var logElement = new XElement("LogEntry",
                new XElement("Timestamp", entry.Timestamp),
                new XElement("Level", entry.Level.ToString()),
                new XElement("Message", entry.Message)
            );
            
            if (entry.Category != null)
            {
                logElement.Add(new XElement("Category", entry.Category));
            }
            
            if (entry.Exception != null)
            {
                logElement.Add(new XElement("Exception", entry.Exception.ToString()));
            }
            
            if (entry.Properties.Count > 0)
            {
                var propsElement = new XElement("Properties");
                foreach (var prop in entry.Properties)
                {
                    propsElement.Add(new XElement("Property", 
                        new XAttribute("Name", prop.Key),
                        new XElement("Value", prop.Value?.ToString() ?? string.Empty)
                    ));
                }
                logElement.Add(propsElement);
            }
            
            return logElement.ToString();
        }
    }
} 