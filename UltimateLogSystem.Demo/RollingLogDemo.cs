using System;
using System.Threading;
using System.Threading.Tasks;
using UltimateLogSystem;
using UltimateLogSystem.Formatters;

namespace UltimateLogSystem.Demo
{
    public static class RollingLogDemo
    {
        public static async Task Run()
        {
            Console.WriteLine("日志滚动演示");
            Console.WriteLine("===========");
            
            // 创建日志配置 - 使用较小的文件大小限制，便于测试滚动功能
            var config = new LoggerConfiguration()
                .SetMinimumLevel(LogLevel.Trace)
                .SetDefaultCategory("RollingDemo")
                .AddConsoleWriter(new TextFormatter("[{timestamp}] [{level}] [{category}] {message}"))
                // 传统方式 - 不使用日期滚动，仅按大小滚动
                .AddFileWriter(
                    "logs/size-rolling.log", 
                    new TextFormatter(), 
                    maxFileSize: 5 * 1024, // 仅5KB
                    maxRollingFiles: 3)
                // 新方式 - 使用日期滚动和大小滚动
                .AddFileWriterWithDailyRolling(
                    "logs/date-rolling.log", 
                    new TextFormatter(), 
                    maxFileSize: 5 * 1024, // 仅5KB
                    maxRollingFiles: 3,
                    useDailyRolling: true);
            
            // 创建日志记录器
            var logger = LoggerFactory.CreateLogger(config, "RollingDemo");
            
            Console.WriteLine("正在生成大量日志以触发滚动...");
            
            // 生成足够的日志以触发滚动
            for (int i = 1; i <= 1000; i++)
            {
                string message = $"这是测试日志消息 #{i}，包含足够的字符以便快速填满日志文件。" +
                                "这里有很多额外的文本内容，以确保我们能够快速达到文件大小限制并触发滚动机制。";
                
                logger.Info(message);
                
                // 每100条日志显示一次进度
                if (i % 100 == 0)
                {
                    Console.WriteLine($"已生成 {i} 条日志");
                }
                
                // 稍微暂停，避免过快生成
                if (i % 10 == 0)
                {
                    await Task.Delay(10);
                }
            }
            
            logger.Info("日志滚动测试完成");
            
            // 关闭日志系统
            LoggerFactory.CloseAll();
            
            Console.WriteLine("\n日志滚动测试完成。");
            Console.WriteLine("检查以下文件夹查看结果：");
            Console.WriteLine("- logs/size-rolling.log (及滚动文件)");
            Console.WriteLine("- logs/date-rolling.log (及滚动文件)");
            Console.WriteLine("\n按任意键返回主菜单...");
            Console.ReadKey();
        }
    }
} 