using System;
using System.Collections.Generic;  // 提供 Dictionary<,> 和 Queue<>
using System.IO;                   // 提供 StreamWriter
using System.Net.Http;   
using UltimateLogSystem.Formatters;         // 用于 HttpWriter
namespace UltimateLogSystem.Writers
{
    /// <summary>
    /// 彩色控制台输出
    /// </summary>
    public class ColorConsoleWriter : ILogWriter
    {
        private readonly ILogFormatter _formatter;
        private readonly Dictionary<object, ConsoleColor> _levelColors;
        
        public ColorConsoleWriter(ILogFormatter formatter)
        {
            _formatter = formatter;
            _levelColors = new Dictionary<object, ConsoleColor>
            {
                { LogLevel.Trace, ConsoleColor.Gray },
                { LogLevel.Debug, ConsoleColor.Blue },
                { LogLevel.Info, ConsoleColor.Green },
                { LogLevel.Warning, ConsoleColor.Yellow },
                { LogLevel.Error, ConsoleColor.Red },
                { LogLevel.Fatal, ConsoleColor.DarkRed }
            };
        }
        
        public void Write(LogEntry entry)
        {
            var originalColor = Console.ForegroundColor;
            
            try
            {
                if (_levelColors.TryGetValue(entry.Level, out var color))
                {
                    Console.ForegroundColor = color;
                }
                
                Console.WriteLine(_formatter.Format(entry));
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
        
        public void Flush() { } // 控制台无需刷新
        
        public void Dispose() { } // 控制台无需释放资源
    }
} 