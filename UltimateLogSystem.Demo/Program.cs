using System;
using System.IO;
using System.Threading.Tasks;
using UltimateLogSystem;
using UltimateLogSystem.Formatters;
using UltimateLogSystem.WebViewer;

namespace UltimateLogSystem.Demo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("终极日志系统演示程序");
            Console.WriteLine("====================");
            Console.WriteLine("1. 基础演示");
            Console.WriteLine("2. 高级演示");
            Console.WriteLine("3. Web查看器演示");
            Console.WriteLine("4. 日志滚动演示");
            Console.Write("请选择演示类型 (1/2/3/4): ");
            
            var key = Console.ReadKey();
            Console.WriteLine();
            Console.WriteLine();
            
            if (key.KeyChar == '4')
            {
                await RollingLogDemo.Run();
            }
            else if (key.KeyChar == '3')
            {
                await WebViewerDemo();
            }
            else if (key.KeyChar == '2')
            {
                await AdvancedDemo.Run();
            }
            else
            {
                // 创建日志配置
                var config = new LoggerConfiguration()
                    .SetMinimumLevel(LogLevel.Trace)
                    .SetDefaultCategory("Demo")
                    .AddConsoleWriter(new TextFormatter("[{timestamp}] [{level}] [{category}] {message}"))
                    .AddFileWriter("logs/app.log")
                    .AddFileWriter("logs/app.json", new JsonFormatter())
                    .AddFileWriterWithDailyRolling("logs/daily/app.log", new TextFormatter(), 1024 * 1024, 3) // 每天滚动，1MB文件大小限制
                    .AddAction(entry => {
                        if (entry.Level is LogLevel level && level >= LogLevel.Error)
                        {
                            Console.Beep(); // 错误时发出提示音
                        }
                    });
                
                // 创建日志记录器
                var logger = LoggerFactory.CreateLogger(config);
                
                // 记录不同级别的日志
                logger.Trace("这是一条跟踪日志");
                logger.Debug("这是一条调试日志");
                logger.Info("这是一条信息日志");
                logger.Warning("这是一条警告日志");
                logger.Error("这是一条错误日志");
                logger.Fatal("这是一条致命错误日志");
                
                // 使用自定义类别
                logger.Info("这是一条来自用户模块的日志", "UserModule");
                
                // 记录异常
                try
                {
                    throw new InvalidOperationException("发生了一个操作异常");
                }
                catch (Exception ex)
                {
                    logger.Error("操作失败", exception: ex);
                }
                
                // 使用上下文属性
                LogContext.SetProperty("UserId", "12345");
                LogContext.SetProperty("SessionId", Guid.NewGuid().ToString());
                
                logger.Info("用户登录成功");
                
                // 使用扩展方法
                logger.Time(() => {
                    // 模拟耗时操作
                    Task.Delay(1000).Wait();
                }, "耗时操作");
                
                // 结构化日志
                logger.LogStructured(LogLevel.Info, "用户 {UserName} 购买了 {ProductName}，价格: {Price}", 
                    new { UserName = "张三", ProductName = "终极日志系统", Price = 299.00 });
                
                // 安全执行
                logger.SafeExecute(() => {
                    throw new Exception("这个异常会被捕获并记录");
                }, "危险操作");
            }
            
            // 关闭日志系统
            LoggerFactory.CloseAll();
            
            Console.WriteLine("\n日志已保存到 logs 目录");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
        
        /// <summary>
        /// Web查看器演示
        /// </summary>
        static async Task WebViewerDemo()
        {
            Console.WriteLine("Web查看器演示");
            Console.WriteLine("=============");
            
            // 创建日志目录
            Directory.CreateDirectory("logs");
            
            // 创建日志配置
            var config = new LoggerConfiguration()
                .SetMinimumLevel(LogLevel.Trace)
                .SetDefaultCategory("WebDemo")
                .AddConsoleWriter(new TextFormatter("[{timestamp}] [{level}] [{category}] {message}"))
                .AddFileWriter("logs/web-demo.log")
                .AddFileWriter("logs/web-demo.json", new JsonFormatter())
                .AddSimpleWebViewer("logs", 5000); // 使用简单HTTP查看器
            
            // 创建日志记录器
            var logger = LoggerFactory.CreateLogger(config);
            
            Console.WriteLine("正在生成示例日志...");
            
            // 生成多条不同级别的日志
            var random = new Random();
            var categories = new[] { "用户", "系统", "数据库", "网络", "安全" };
            var messages = new[]
            {
                "操作成功完成",
                "用户登录系统",
                "数据库连接已建立",
                "网络连接超时",
                "发现潜在安全风险",
                "系统资源不足",
                "文件读取失败",
                "配置已更新",
                "任务已调度",
                "处理请求中..."
            };
            
            for (int i = 0; i < 200; i++)
            {
                var level = (LogLevel)random.Next(0, 6);
                var category = categories[random.Next(categories.Length)];
                var message = messages[random.Next(messages.Length)];
                
                // 添加一些随机属性
                LogContext.ClearProperties();
                LogContext.SetProperty("RequestId", Guid.NewGuid().ToString());
                LogContext.SetProperty("UserId", $"user-{random.Next(1, 100)}");
                LogContext.SetProperty("Duration", random.Next(1, 1000));
                LogContext.SetProperty("IP", $"192.168.1.{random.Next(1, 255)}");
                
                // 记录日志
                logger.Log(level, message, category);
                
                // 偶尔记录带异常的日志
                if (random.Next(10) == 0)
                {
                    try
                    {
                        switch (random.Next(3))
                        {
                            case 0:
                                throw new InvalidOperationException("操作无效");
                            case 1:
                                throw new ArgumentException("参数错误");
                            case 2:
                                throw new TimeoutException("操作超时");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("发生异常", category, ex);
                    }
                }
                
                // 模拟时间流逝
                await Task.Delay(10);
            }
            
            // 添加一些自定义级别的日志
            var auditLevel = CustomLogLevel.Create(15, "Audit");
            var securityLevel = CustomLogLevel.Create(25, "Security");
            
            logger.Log(auditLevel, "用户admin执行了删除操作", "审计");
            logger.Log(securityLevel, "检测到潜在的安全威胁", "安全");
            
            Console.WriteLine("示例日志已生成");
            Console.WriteLine("Web查看器已启动，请访问: http://localhost:5000");
            Console.WriteLine("按任意键停止Web查看器并退出...");
            Console.ReadKey();
            
            // 关闭日志系统
            LoggerFactory.CloseAll();
        }
    }
} 