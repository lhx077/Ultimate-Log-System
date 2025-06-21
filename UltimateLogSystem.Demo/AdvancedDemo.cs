using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Linq;
using UltimateLogSystem;
using UltimateLogSystem.Formatters;
using UltimateLogSystem.Writers;
using UltimateLogSystem.Handlers;

namespace UltimateLogSystem.Demo
{
    public class AdvancedDemo
    {
        public static async Task Run()
        {
            Console.WriteLine("终极日志系统高级演示");
            Console.WriteLine("====================");
            
            // 创建自定义日志级别
            var AuditLevel = CustomLogLevel.Create(15, "Audit");
            var SecurityLevel = CustomLogLevel.Create(25, "Security");
            
            // 创建SQLite连接
            var connectionString = "Data Source=logs.db;Version=3;";
            var connection = new SQLiteConnection(connectionString);
            connection.Open();
            
            // 创建日志配置
            var config = new LoggerConfiguration()
                .SetMinimumLevel(LogLevel.Trace)
                .SetDefaultCategory("AdvancedDemo")
                .AddConsoleWriter(new TextFormatter("[{timestamp}] [{level}] [{category}] {message}"))
                .AddFileWriter("logs/app.log")
                .AddFileWriter("logs/app.json", new JsonFormatter())
                .AddWriter(new DatabaseWriter(connection, "LogEntries", true))
                .AddWriter(new HttpWriter("https://example.com/api/logs"))
                .AddAction(entry => {
                    if (entry.Level.Equals(SecurityLevel))
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"安全警报: {entry.Message}");
                        Console.ResetColor();
                    }
                });
            
            // 创建日志记录器
            var logger = LoggerFactory.CreateLogger(config, "AdvancedLogger");
            
            // 记录不同级别的日志
            logger.Log(LogLevel.Info, "标准信息日志");
            logger.Log(AuditLevel, "审计日志");
            logger.Log(SecurityLevel, "安全日志");
            
            // 使用日志拦截器
            var interceptor = new LogInterceptor(
                entry => entry.Message.Contains("敏感"),
                entry => {
                    entry.Message = entry.Message.Replace("敏感", "***");
                }
            );
            
            // 记录包含敏感信息的日志
            var entry = new LogEntry(DateTime.Now, LogLevel.Info, "Security", "这条日志包含敏感信息");
            if (interceptor.Process(entry))
            {
                logger.Log(entry.Level, entry.Message, entry.Category, entry.Exception);
            }
            
            // 使用上下文属性
            using (var scope = new DisposableLogContext())
            {
                LogContext.SetProperty("RequestId", Guid.NewGuid().ToString());
                LogContext.SetProperty("UserId", "admin");
                
                logger.Info("开始处理请求");
                
                // 模拟处理请求
                await Task.Delay(100);
                
                logger.Info("请求处理完成");
            } // 离开作用域时自动清除上下文
            
            // 批量记录日志
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() => {
                    logger.Info($"并发日志 #{index}");
                }));
            }
            
            await Task.WhenAll(tasks);
            
            // 记录结构化数据
            var userData = new {
                Id = 12345,
                Username = "admin",
                Role = "Administrator",
                LoginCount = 42,
                LastLogin = DateTime.Now.AddDays(-1)
            };
            
            logger.LogStructured(LogLevel.Info, "用户 {Username} (ID: {Id}) 角色为 {Role} 登录，这是第 {LoginCount} 次登录", userData);
            
            // 使用性能监控
            var result = logger.Time(() => {
                // 模拟复杂计算
                int sum = 0;
                for (int i = 0; i < 1000000; i++)
                {
                    sum += i;
                }
                return sum;
            }, "复杂计算");
            
            logger.Info($"计算结果: {result}");
            
            // 导出日志示例
            var entries = new List<LogEntry>
            {
                new LogEntry(DateTime.Now, LogLevel.Info, "Export", "第一条导出日志"),
                new LogEntry(DateTime.Now.AddMinutes(1), LogLevel.Warning, "Export", "第二条导出日志"),
                new LogEntry(DateTime.Now.AddMinutes(2), LogLevel.Error, "Export", "第三条导出日志", new Exception("测试异常"))
            };
            
            LogExporter.ExportToTextFile(entries, "logs/export.log");
            LogExporter.ExportToJsonFile(entries, "logs/export.json");
            LogExporter.ExportToXmlFile(entries, "logs/export.xml");
            
            // 日志分析示例
            var countByLevel = LogAnalyzer.CountByLevel(entries);
            Console.WriteLine("\n按级别统计:");
            foreach (var pair in countByLevel)
            {
                Console.WriteLine($"  {pair.Key}: {pair.Value}条");
            }
            
            var errorLogs = LogAnalyzer.FindByLevel(entries, LogLevel.Error);
            Console.WriteLine($"\n错误日志数量: {errorLogs.Count()}条");
            
            // 关闭日志系统
            LoggerFactory.CloseAll();
            connection.Close();
            
            Console.WriteLine("\n高级演示完成");
        }
    }
    
    // 可释放的日志上下文
    public class DisposableLogContext : IDisposable
    {
        private readonly Dictionary<string, object?> _originalProperties;
        
        public DisposableLogContext()
        {
            _originalProperties = new Dictionary<string, object?>(LogContext.Properties);
        }
        
        public void Dispose()
        {
            LogContext.ClearProperties();
            
            foreach (var prop in _originalProperties)
            {
                LogContext.SetProperty(prop.Key, prop.Value);
            }
        }
    }
} 